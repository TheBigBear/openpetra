//
// DO NOT REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
//
// @Authors:
//       christiank, timop
//
// Copyright 2004-2021 by OM International
//
// This file is part of OpenPetra.org.
//
// OpenPetra.org is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// OpenPetra.org is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with OpenPetra.org.  If not, see <http://www.gnu.org/licenses/>.
//
using System;
using System.Data;
using System.Data.Odbc;
using System.Globalization;
using System.IO;
using System.Security.Principal;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

using Ict.Common;
using Ict.Common.DB;
using Ict.Common.IO;
using Ict.Common.Exceptions;
using Ict.Common.Verification;
using Ict.Common.Remoting.Server;
using Ict.Common.Remoting.Shared;
using Ict.Common.Session;
using Ict.Petra.Shared;
using Ict.Petra.Shared.Security;
using Ict.Petra.Shared.Interfaces.Plugins.MSysMan;
using Ict.Petra.Shared.MSysMan.Data;
using Ict.Petra.Server.App.Core;
using Ict.Petra.Server.App.Core.Security;
using Ict.Petra.Server.MSysMan.Data.Access;
using Ict.Petra.Server.MSysMan.Common.WebConnectors;
using Ict.Petra.Server.MSysMan.Maintenance.WebConnectors;
using Ict.Petra.Server.MSysMan.Security.UserManager.WebConnectors;

namespace Ict.Petra.Server.MSysMan.Security.UserManager.WebConnectors
{
    /// <summary>
    /// The TUserManager class provides access to the security-related information
    /// of Users of a Petra DB.
    /// </summary>
    /// <remarks>
    /// Calls methods that have the same name in the
    /// Ict.Petra.Server.App.Core.Security.UserManager Namespace to perform its
    /// functionality!
    ///
    /// This is required in two places,
    /// because it is needed before the appdomain is loaded and therefore cannot be in MSysMan;
    /// and it is needed here to make it available to the client via MSysMan remotely
    /// </remarks>
    public class TUserManagerWebConnector
    {
        /// <summary>
        /// load the plugin assembly for authentication
        /// </summary>
        [NoRemoting]
        public static IUserAuthentication LoadAuthAssembly(string AUserAuthenticationMethod)
        {
            // namespace of the class TUserAuthentication, eg. Plugin.AuthenticationPhpBB
            // the dll has to be in the normal application directory
            string Namespace = AUserAuthenticationMethod;
            string NameOfDll = TAppSettingsManager.ApplicationDirectory + Path.DirectorySeparatorChar + Namespace + ".dll";
            string NameOfClass = Namespace + ".TUserAuthentication";

            // dynamic loading of dll
            System.Reflection.Assembly assemblyToUse = System.Reflection.Assembly.LoadFrom(NameOfDll);
            System.Type CustomClass = assemblyToUse.GetType(NameOfClass);

            return (IUserAuthentication)Activator.CreateInstance(CustomClass);
        }

        /// <summary>
        /// load details of user
        /// </summary>
        [NoRemoting]
        internal static SUserRow LoadUser(String AUserID, out TPetraPrincipal APetraPrincipal, TDBTransaction ATransaction)
        {
            SUserRow ReturnValue = LoadUser(AUserID, ATransaction);

            APetraPrincipal = new TPetraPrincipal(AUserID, TGroupManager.LoadUserGroups(
                    AUserID, ATransaction), TModuleAccessManager.LoadUserModules(AUserID, ATransaction));
            if (!ReturnValue.IsPartnerKeyNull())
            {
                APetraPrincipal.PartnerKey = ReturnValue.PartnerKey;
            }

/*
 *          TLogging.LogAtLevel (8, "APetraPrincipal.IsTableAccessOK(tapMODIFY, 'p_person'): " +
 *                  APetraPrincipal.IsTableAccessOK(TTableAccessPermission.tapMODIFY, "p_person").ToString());
 */
            return ReturnValue;
        }

        /// <summary>
        /// Loads the details of the user from the s_user DB Table.
        /// </summary>
        /// <param name="AUserID">User ID to load the details for.</param>
        /// <param name="ATransaction">Instantiated DB Transaction.</param>
        /// <returns>s_user record of the User (if the user exists).</returns>
        /// <exception cref="EUserNotExistantException">Throws <see cref="EUserNotExistantException"/> if the user
        /// doesn't exist!</exception>
        [NoRemoting]
        private static SUserRow LoadUser(String AUserID, TDBTransaction ATransaction)
        {
            // Check if user exists in s_user DB Table
            if (!SUserAccess.Exists(AUserID, ATransaction))
            {
                throw new EUserNotExistantException(StrInvalidUserIDPassword);
            }

            // User exists, so load User record
            SUserTable UserDT = SUserAccess.LoadByPrimaryKey(AUserID, ATransaction);

            return UserDT[0];
        }

        /// <summary>
        /// Application and Database should have the same version, otherwise all sorts of things can go wrong.
        /// this is specific to the OpenPetra database, for all other databases it will just ignore the database version check
        /// </summary>
        private static void CheckDatabaseVersion(TDataBase ADataBase)
        {
            TDBTransaction ReadTransaction = new TDBTransaction();
            DataTable Tbl = null;

            if (TAppSettingsManager.GetValue("action", string.Empty, false) == "patchDatabase")
            {
                // we want to upgrade the database, so don't check for the database version
                return;
            }

            TDataBase db = DBAccess.Connect("CheckDatabaseVersion", ADataBase);
            db.ReadTransaction(ref ReadTransaction,
                delegate
                {
                    // now check if the database is 'up to date'; otherwise run db patch against it
                    Tbl = ReadTransaction.DataBaseObj.SelectDT(
                        "SELECT s_default_value_c FROM PUB_s_system_defaults WHERE s_default_code_c = 'CurrentDatabaseVersion'",
                        "Temp", ReadTransaction, new OdbcParameter[0]);
                });

            if (Tbl.Rows.Count == 0)
            {
                return;
            }
        }

        /// <summary>
        /// Authenticate a user.
        /// </summary>
        /// <param name="AUserID">User ID.</param>
        /// <param name="APassword">Password.</param>
        /// <param name="AClientComputerName">Name of the Client Computer that the authentication request came from.</param>
        /// <param name="AClientIPAddress">IP Address of the Client Computer that the authentication request came from.</param>
        /// <param name="ASystemEnabled">True if the system is enabled, otherwise false.</param>
        /// <param name="ATransaction">Instantiated DB Transaction.</param>
        [NoRemoting]
        public static bool PerformUserAuthentication(String AUserID, String APassword,
            string AClientComputerName, string AClientIPAddress, out Boolean ASystemEnabled,
            TDBTransaction ATransaction)
        {
            SUserRow UserDR;
            DateTime LoginDateTime;
            TPetraPrincipal PetraPrincipal = null;
            string UserAuthenticationMethod = TAppSettingsManager.GetValue("UserAuthenticationMethod", "OpenPetraDBSUser", false);
            IUserAuthentication AuthenticationAssembly;
            string AuthAssemblyErrorMessage;

            Int32 AProcessID = -1;

            ASystemEnabled = true;

            CheckDatabaseVersion(ATransaction.DataBaseObj);

            string EmailAddress = AUserID;

            try
            {
                UserDR = LoadUser(AUserID, out PetraPrincipal, ATransaction);
            }
            catch (EUserNotExistantException)
            {
                // pass ATransaction
                UserInfo.SetUserInfo(new TPetraPrincipal("SYSADMIN"));

                // Logging
                TLoginLog.AddLoginLogEntry(AUserID, TLoginLog.LOGIN_STATUS_TYPE_LOGIN_ATTEMPT_FOR_NONEXISTING_USER,
                    String.Format(Catalog.GetString(
                            "User with User ID '{0}' attempted to log in, but there is no user account for this user! "),
                        AUserID) + String.Format(ResourceTexts.StrRequestCallerInfo, AClientComputerName, AClientIPAddress),
                    out AProcessID, ATransaction);

                // Only now throw the Exception!
                throw;
            }

            // pass ATransaction
            UserInfo.SetUserInfo(PetraPrincipal);

            if (AUserID == "SELFSERVICE")
            {
                APassword = String.Empty;
            }
            else if ((AUserID == "SYSADMIN") && TSession.HasVariable("ServerAdminToken"))
            {
                // Login via server admin console authenticated by file token
                APassword = String.Empty;
            }
            //
            // (1) Check user-supplied password
            //
            else if (UserAuthenticationMethod == "OpenPetraDBSUser")
            {
                if (!TPasswordHelper.EqualsAntiTimingAttack(
                             Convert.FromBase64String(
                                  CreateHashOfPassword(APassword, UserDR.PasswordSalt, UserDR.PwdSchemeVersion)), 
                             Convert.FromBase64String(UserDR.PasswordHash)))
                {
                    // The password that the user supplied is wrong!!! --> Save failed user login attempt!
                    // If the number of permitted failed logins in a row gets exceeded then also lock the user account!
                    SaveFailedLogin(AUserID, UserDR, AClientComputerName, AClientIPAddress, ATransaction);

                    if (UserDR.AccountLocked
                        && (Convert.ToBoolean(UserDR[SUserTable.GetAccountLockedDBName(), DataRowVersion.Original]) != UserDR.AccountLocked))
                    {
                        // User Account just got locked!
                        throw new EUserAccountGotLockedException(StrInvalidUserIDPassword);
                    }
                    else
                    {
                        throw new EPasswordWrongException(StrInvalidUserIDPassword);
                    }
                }
            }
            else
            {
                AuthenticationAssembly = LoadAuthAssembly(UserAuthenticationMethod);

                if (!AuthenticationAssembly.AuthenticateUser(EmailAddress, APassword, out AuthAssemblyErrorMessage))
                {
                    // The password that the user supplied is wrong!!! --> Save failed user login attempt!
                    // If the number of permitted failed logins in a row gets exceeded then also lock the user account!
                    SaveFailedLogin(AUserID, UserDR, AClientComputerName, AClientIPAddress, ATransaction);

                    if (UserDR.AccountLocked
                        && (Convert.ToBoolean(UserDR[SUserTable.GetAccountLockedDBName(), DataRowVersion.Original]) != UserDR.AccountLocked))
                    {
                        // User Account just got locked!
                        throw new EUserAccountGotLockedException(StrInvalidUserIDPassword);
                    }
                    else
                    {
                        throw new EPasswordWrongException(AuthAssemblyErrorMessage);
                    }
                }
            }

            //
            // (2) Check if the User Account is Locked or if the user is 'Retired'. If either is true then deny the login!!!
            //
            // IMPORTANT: We perform these checks only AFTER the check for the correctness of the password so that every
            // log-in attempt that gets rejected on grounds of a wrong password takes the same amount of time (to help prevent
            // an attack vector called 'timing attack')
            if (UserDR.AccountLocked || UserDR.Retired)
            {
                if ((AUserID == "SYSADMIN") && TSession.HasVariable("ServerAdminToken"))
                {
                    // this is ok. we need to be able to activate the sysadmin account on SetInitialSysadminEmail
                }
                else if (UserDR.AccountLocked)
                {
                    // Logging
                    TLoginLog.AddLoginLogEntry(AUserID, TLoginLog.LOGIN_STATUS_TYPE_LOGIN_ATTEMPT_FOR_LOCKED_USER,
                        Catalog.GetString("User attempted to log in, but the user account was locked! ") +
                        String.Format(ResourceTexts.StrRequestCallerInfo, AClientComputerName, AClientIPAddress),
                        out AProcessID, ATransaction);

                    // Only now throw the Exception!
                    throw new EUserAccountLockedException(StrInvalidUserIDPassword);
                }
                else
                {
                    // Logging
                    TLoginLog.AddLoginLogEntry(AUserID, TLoginLog.LOGIN_STATUS_TYPE_LOGIN_ATTEMPT_FOR_RETIRED_USER,
                        Catalog.GetString("User attempted to log in, but the user is retired! ") +
                        String.Format(ResourceTexts.StrRequestCallerInfo, AClientComputerName, AClientIPAddress),
                        out AProcessID, ATransaction);

                    // Only now throw the Exception!
                    throw new EUserRetiredException(StrInvalidUserIDPassword);
                }
            }

            //
            // (3) Check SystemLoginStatus (whether the general use of the OpenPetra application is enabled/disabled) in the
            // SystemStatus table (this table always holds only a single record)
            //
            SSystemStatusTable SystemStatusDT;

            SystemStatusDT = SSystemStatusAccess.LoadAll(ATransaction);

            if (SystemStatusDT[0].SystemLoginStatus)
            {
                ASystemEnabled = true;
            }
            else
            {
                ASystemEnabled = false;

                // TODO: Check for Security Group membership might need reviewal when security model of OpenPetra might get reviewed...
                if (PetraPrincipal.IsInGroup("SYSADMIN"))
                {
                    PetraPrincipal.LoginMessage =
                        String.Format(StrSystemDisabled1,
                            SystemStatusDT[0].SystemDisabledReason) + Environment.NewLine + Environment.NewLine +
                        StrSystemDisabled2Admin;
                }
                else
                {
                    TLoginLog.AddLoginLogEntry(AUserID, TLoginLog.LOGIN_STATUS_TYPE_LOGIN_ATTEMPT_WHEN_SYSTEM_WAS_DISABLED,
                        Catalog.GetString("User wanted to log in, but the System was disabled. ") +
                        String.Format(ResourceTexts.StrRequestCallerInfo, AClientComputerName, AClientIPAddress),
                        out AProcessID, ATransaction);

                    TLoginLog.RecordUserLogout(AUserID, AProcessID, ATransaction);

                    throw new ESystemDisabledException(String.Format(StrSystemDisabled1,
                            SystemStatusDT[0].SystemDisabledReason) + Environment.NewLine + Environment.NewLine +
                        String.Format(StrSystemDisabled2, StringHelper.DateToLocalizedString(SystemStatusDT[0].SystemAvailableDate.Value),
                            SystemStatusDT[0].SystemAvailableDate.Value.AddSeconds(SystemStatusDT[0].SystemAvailableTime).ToShortTimeString()));
                }
            }

            //
            // (3b) Check if the license is valid
            //
            string LicenseCheckUrl = TAppSettingsManager.GetValue("LicenseCheck.Url", String.Empty, false);
            string LicenseUser = TAppSettingsManager.GetValue("Server.DBName");

            if ((AUserID == "SYSADMIN") && TSession.HasVariable("ServerAdminToken"))
            {
                // don't check for the license, since this is called when upgrading the server as well.
                LicenseCheckUrl = String.Empty;
            }

            if ((LicenseCheckUrl != String.Empty) && (LicenseUser != "openpetra"))
            {
                string url = LicenseCheckUrl;

                if (url.EndsWith('='))
                {
                    url += LicenseUser;
                }

                string result = THTTPUtils.ReadWebsite(url);

                bool valid = result.Contains("\"valid\":true");
                bool gratis = result.Contains("\"gratis\":true");

                if (!valid && !gratis)
                {
                    TLoginLog.AddLoginLogEntry(AUserID, TLoginLog.LOGIN_STATUS_TYPE_LOGIN_ATTEMPT_WHEN_SYSTEM_WAS_DISABLED,
                        Catalog.GetString("User wanted to log in, but the license is expired. ") +
                        String.Format(ResourceTexts.StrRequestCallerInfo, AClientComputerName, AClientIPAddress),
                        out AProcessID, ATransaction);

                    TLoginLog.RecordUserLogout(AUserID, AProcessID, ATransaction);

                    throw new ELicenseExpiredException("LICENSE_EXPIRED");
                }
            }

            //
            // (4) Save successful login!
            //
            LoginDateTime = DateTime.Now;
            UserDR.LastLoginDate = LoginDateTime;
            UserDR.LastLoginTime = Conversions.DateTimeToInt32Time(LoginDateTime);
            UserDR.FailedLogins = 0;  // this needs resetting!

            // Upgrade the user's password hashing scheme if it is older than the current password hashing scheme
            if (APassword != String.Empty && UserDR.PwdSchemeVersion < TPasswordHelper.CurrentPasswordSchemeNumber)
            {
                TMaintenanceWebConnector.SetNewPasswordHashAndSaltForUser(UserDR, APassword,
                    AClientComputerName, AClientIPAddress, ATransaction);
            }

            SaveUser(AUserID, (SUserTable)UserDR.Table, ATransaction);

            // TODO: Check for Security Group membership might need reviewal when security model of OpenPetra might get reviewed...

            if (PetraPrincipal.IsInGroup("SYSADMIN"))
            {
                TLoginLog.AddLoginLogEntry(AUserID, TLoginLog.LOGIN_STATUS_TYPE_LOGIN_SUCCESSFUL_SYSADMIN,
                    Catalog.GetString("User login - SYSADMIN privileges. ") +
                    String.Format(ResourceTexts.StrRequestCallerInfo, AClientComputerName, AClientIPAddress),
                    out AProcessID, ATransaction);
            }
            else
            {
                TLoginLog.AddLoginLogEntry(AUserID, TLoginLog.LOGIN_STATUS_TYPE_LOGIN_SUCCESSFUL,
                    Catalog.GetString("User login. ") +
                    String.Format(ResourceTexts.StrRequestCallerInfo, AClientComputerName, AClientIPAddress),
                    out AProcessID, ATransaction);
            }

            PetraPrincipal.ProcessID = AProcessID;
            AProcessID = 0;

            //
            // (5) Check if a password change is requested for this user
            //
            if (UserDR.PasswordNeedsChange)
            {
                // The user needs to change their password before they can use OpenPetra
                PetraPrincipal.LoginMessage = SharedConstants.LOGINMUSTCHANGEPASSWORD;
            }

            return true;
        }

        /// <summary>
        /// Save a failed user login attempt. If the number of permitted failed logins in a row gets exceeded then the
        /// user account gets Locked, too!
        /// </summary>
        /// <param name="AUserID">User ID.</param>
        /// <param name="UserDR">s_user DataRow of the user.</param>
        /// <param name="AClientComputerName">Name of the Client Computer that the authentication request came from.</param>
        /// <param name="AClientIPAddress">IP Address of the Client Computer that the authentication request came from.</param>
        /// <param name="ATransaction">Instantiated DB Transaction.</param>
        private static void SaveFailedLogin(string AUserID, SUserRow UserDR,
            string AClientComputerName, string AClientIPAddress, TDBTransaction ATransaction)
        {
            int AProcessID;
            int FailedLoginsUntilAccountGetsLocked =
                new TSystemDefaults().GetInt32Default(SharedConstants.SYSDEFAULT_FAILEDLOGINS_UNTIL_ACCOUNT_GETS_LOCKED, 10);
            bool AccountLockedAtThisAttempt = false;

            // Console.WriteLine('PetraPrincipal.PetraIdentity.FailedLogins: ' + PetraPrincipal.PetraIdentity.FailedLogins.ToString +
            // '; PetraPrincipal.PetraIdentity.AccountLocked: ' + PetraPrincipal.PetraIdentity.AccountLocked.ToString);

            UserDR.FailedLogins++;
            UserDR.FailedLoginDate = DateTime.Now;
            UserDR.FailedLoginTime = Conversions.DateTimeToInt32Time(UserDR.FailedLoginDate.Value);

            // Check if User Account should be Locked due to too many successive failed log-in attempts
            if ((UserDR.FailedLogins >= FailedLoginsUntilAccountGetsLocked)
                && ((!UserDR.AccountLocked)))
            {
                // Lock User Account (this user will no longer be able to log in until a Sysadmin resets this flag!)
                UserDR.AccountLocked = true;
                AccountLockedAtThisAttempt = true;

                TUserAccountActivityLog.AddUserAccountActivityLogEntry(UserDR.UserId,
                    TUserAccountActivityLog.USER_ACTIVITY_PERMITTED_FAILED_LOGINS_EXCEEDED,
                    String.Format(Catalog.GetString(
                            "The permitted number of failed logins in a row got exceeded and the user account for the user {0} got locked! ") +
                        String.Format(ResourceTexts.StrRequestCallerInfo, AClientComputerName, AClientIPAddress),
                        UserDR.UserId), ATransaction);
            }

            // Logging
            TLoginLog.AddLoginLogEntry(AUserID,
                AccountLockedAtThisAttempt ? TLoginLog.LOGIN_STATUS_TYPE_LOGIN_ATTEMPT_PWD_WRONG_ACCOUNT_GOT_LOCKED :
                TLoginLog.LOGIN_STATUS_TYPE_LOGIN_ATTEMPT_PWD_WRONG,
                String.Format(Catalog.GetString("User supplied wrong password{0}!  (Failed Logins: now {1}; " +
                        "Account Locked: now {2}, User Retired: {3}) "),
                    (AccountLockedAtThisAttempt ?
                     Catalog.GetString("; because the permitted number of failed logins in a row got exceeded the user account " +
                         "for the user got locked! ") : String.Empty),
                    UserDR.FailedLogins, UserDR.AccountLocked, UserDR.Retired) +
                String.Format(ResourceTexts.StrRequestCallerInfo, AClientComputerName, AClientIPAddress),
                out AProcessID, ATransaction);

            SaveUser(AUserID, (SUserTable)UserDR.Table, ATransaction);
        }

        /// <summary>
        /// Call this Method when a log-in is attempted for a non-existing user (!) so that the time that is spent on
        /// 'authenticating' them is as long as is spent on authenticating existing users. This is done so that an attacker
        /// that tries to perform user authentication with 'username guessing' cannot easily tell that the user doesn't exist by
        /// checking the time in which the server returns an error (this is an attack vector called 'timing attack')!
        /// </summary>
        [NoRemoting]
        public static void SimulatePasswordAuthenticationForNonExistingUser()
        {
            string UserAuthenticationMethod = TAppSettingsManager.GetValue("UserAuthenticationMethod", "OpenPetraDBSUser", false);

            if (UserAuthenticationMethod == "OpenPetraDBSUser")
            {
                TUserManagerWebConnector.CreateHashOfPassword("wrongPassword",
                    Convert.ToBase64String(TPasswordHelper.CurrentPasswordScheme.GetNewPasswordSalt()),
                    TPasswordHelper.CurrentPasswordSchemeNumber);
            }
            else
            {
                IUserAuthentication auth = TUserManagerWebConnector.LoadAuthAssembly(UserAuthenticationMethod);

                string ErrorMessage;

                auth.AuthenticateUser("wrongUser", "wrongPassword", out ErrorMessage);
            }
        }

        #region Resourcestrings

        private static readonly string StrSystemDisabled1 = Catalog.GetString("OpenPetra is currently disabled due to {0}.");

        private static readonly string StrSystemDisabled2 = Catalog.GetString("It will be available on {0} at {1}.");

        private static readonly string StrSystemDisabled2Admin = Catalog.GetString("Proceed with caution.");

        private static readonly string StrInvalidUserIDPassword = Catalog.GetString("Invalid User ID or Password.");

        #endregion

        /// <summary>
        /// create hash of password and the salt.
        /// replacement for FormsAuthentication.HashPasswordForStoringInConfigFile
        /// which is part of System.Web.dll and not available in the client profile of .net v4.0
        /// </summary>
        /// <param name="APassword">Password (plain-text).</param>
        /// <param name="ASalt">Salt for 'salting' the password hash. MUST be obtained from the same Password Helper
        /// Class version that gets called in this Method - the Class gets chosen in this Method by evaluating
        /// <paramref name="APasswordSchemeVersion"/></param>.
        /// <param name="APasswordSchemeVersion">Version of the Password Hashing Scheme.</param>
        /// <returns>Password Hash of <paramref name="APassword"/> according to
        /// <paramref name="APasswordSchemeVersion"/> and the passed-in <paramref name="ASalt"/>.</returns>
        [NoRemoting]
        public static string CreateHashOfPassword(string APassword, string ASalt, int APasswordSchemeVersion)
        {
            if (APasswordSchemeVersion == 0)
            {
                // SHA1 - DO NOT USE ANYMORE as this password hash is not considered safe nowadays!
                return BitConverter.ToString(
                    SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(String.Concat(APassword,
                                                       ASalt)))).Replace("-", "");
            }
            else
            {
                return TPasswordHelper.GetPasswordSchemeHelperForVersion(APasswordSchemeVersion).GetPasswordHash(
                        APassword, Convert.FromBase64String(ASalt));
            }
        }

        /// <summary>
        /// Causes an immediately reload of the UserInfo that is stored in the session
        /// </summary>
        [RequireModulePermission("USER")]
        public static bool ReloadUserInfo()
        {
            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("ReloadUserInfo");
            TPetraPrincipal UserDetails = null;
            bool SubmitOK = false;

            try
            {
                db.WriteTransaction(ref Transaction, ref SubmitOK,
                    delegate
                    {
                        LoadUser(UserInfo.GetUserInfo().UserID, out UserDetails, Transaction);
                    });

                UserInfo.SetUserInfo(UserDetails);

                SubmitOK = true;
            }
            catch (Exception Exp)
            {
                TLogging.Log("Exception occured in ReloadCachedUserInfo: " + Exp.ToString());
                throw;
            }

            return true;
        }

        /// <summary>
        /// save user details (last login time, failed logins etc)
        /// </summary>
        [NoRemoting]
        private static Boolean SaveUser(String AUserID, SUserTable AUserDataTable, TDBTransaction ATransaction)
        {
            if ((AUserDataTable != null) && (AUserDataTable.Rows.Count > 0))
            {
                try
                {
                    SUserAccess.SubmitChanges(AUserDataTable, ATransaction);
                }
                catch (Exception Exc)
                {
                    TLogging.Log("An Exception occured during the saving of a User:" + Environment.NewLine + Exc.ToString());

                    throw;
                }
            }
            else
            {
                // nothing to save!
                return false;
            }

            return true;
        }

        /// <summary>
        /// Queues a ClientTask for reloading of the UserInfo for all connected Clients
        /// with a certain UserID.
        ///
        /// </summary>
        /// <param name="AUserID">UserID for which the ClientTask should be queued
        /// </param>
        [RequireModulePermission("USER")]
        public static void SignalReloadCachedUserInfo(String AUserID)
        {
            TClientManager.QueueClientTask(AUserID,
                SharedConstants.CLIENTTASKGROUP_USERINFOREFRESH,
                "",
                null, null, null, null,
                1,
                -1);
        }
    }
}

namespace Ict.Petra.Server.MSysMan.Maintenance.UserManagement
{
    /// <summary>
    /// this manager is called from Server.App.Core
    /// </summary>
    public class TUserManager : IUserManager
    {
        /// <summary>
        /// Set the password
        /// </summary>
        /// <remarks>Gets called from TServerManager.SetPassword() Method, which is used to 
        /// set the initial password for SYSADMIN.</remarks>
        public bool SetPassword(string AUserID, string APassword)
        {
            TVerificationResultCollection VerificationResult;
            return TMaintenanceWebConnector.SetUserPassword(AUserID, APassword, true, true, string.Empty, string.Empty, out VerificationResult);
        }

        /// <summary>
        /// Lock the SYSADMIN user
        /// </summary>
        /// <remarks>Gets called from TServerManager.LockSysadmin() Method, which is used to 
        /// lock the SYSADMIN user while the instance is not assigned to a customer yet.</remarks>
        public bool LockSysadmin()
        {
            return TMaintenanceWebConnector.LockSysadmin();
        }

        /// <summary>
        /// Adds a new user.
        /// </summary>
        /// <remarks>Gets called from TServerManager.AddUser() Method, which in turn gets utilised by the
        /// PetraMultiStart.exe application for the creation of test users for that application.</remarks>
        public bool AddUser(string AUserID, string APassword = "")
        {
            string UserID;
            return TMaintenanceWebConnector.CreateUser(AUserID,
                APassword,
                string.Empty,
                string.Empty,
                string.Empty,
                TMaintenanceWebConnector.DEMOMODULEPERMISSIONS,
                string.Empty,
                out UserID);
        }

        /// <summary>
        /// Authenticate a user.
        /// </summary>
        /// <param name="AUserID">User ID.</param>
        /// <param name="APassword">Password.</param>
        /// <param name="AClientComputerName">Name of the Client Computer that the authentication request came from.</param>
        /// <param name="AClientIPAddress">IP Address of the Client Computer that the authentication request came from.</param>
        /// <param name="ASystemEnabled">True if the system is enabled, otherwise false.</param>
        /// <param name="ATransaction">Instantiated DB Transaction.</param>
        public bool PerformUserAuthentication(string AUserID, string APassword,
            string AClientComputerName, string AClientIPAddress,
            out Boolean ASystemEnabled,
            TDBTransaction ATransaction)
        {
            return TUserManagerWebConnector.PerformUserAuthentication(AUserID, APassword, AClientComputerName, AClientIPAddress,
                out ASystemEnabled, ATransaction);
        }

        /// <summary>
        /// Call this Method when a log-in is attempted for a non-existing user (!) so that the time that is spent on
        /// 'authenticating' them is as long as is spent on authenticating existing users. This is done so that an attacker
        /// that tries to perform user authentication with 'username guessing' cannot easily tell that the user doesn't exist by
        /// checking the time in which the server returns an error (this is an attack vector called 'timing attack')!
        /// </summary>
        public void SimulatePasswordAuthenticationForNonExistingUser()
        {
            TUserManagerWebConnector.SimulatePasswordAuthenticationForNonExistingUser();
         }
    }
}
