//
// DO NOT REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
//
// @Authors:
//       timop
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
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Data.Odbc;
using System.Xml;
using System.IO;
using System.Globalization;
using System.Text;

using Ict.Common;
using Ict.Common.Data;
using Ict.Common.DB;
using Ict.Common.DB.Exceptions;
using Ict.Common.Exceptions;
using Ict.Common.IO;
using Ict.Common.Remoting.Server;
using Ict.Common.Remoting.Shared;
using Ict.Common.Verification;

using Ict.Petra.Shared;
using Ict.Petra.Shared.MCommon.Data;
using Ict.Petra.Shared.MFinance;
using Ict.Petra.Shared.MFinance.Account.Data;
using Ict.Petra.Shared.MFinance.AP.Data;
using Ict.Petra.Shared.MFinance.AR.Data;
using Ict.Petra.Shared.MFinance.Gift.Data;
using Ict.Petra.Shared.MFinance.GL.Data;
using Ict.Petra.Shared.MPartner.Partner.Data;
using Ict.Petra.Shared.MPartner;
using Ict.Petra.Shared.MSysMan.Data;

using Ict.Petra.Server.App.Core.Security;
using Ict.Petra.Server.App.Core;
using Ict.Petra.Server.MCommon.Data.Cascading;
using Ict.Petra.Server.MCommon.Data.Access;
using Ict.Petra.Server.MFinance.Account.Data.Access;
using Ict.Petra.Server.MFinance.AP.Data.Access;
using Ict.Petra.Server.MFinance.Cacheable;
using Ict.Petra.Server.MFinance.Common;
using Ict.Petra.Server.MFinance.Gift.Data.Access;
using Ict.Petra.Server.MFinance.GL.Data.Access;
using Ict.Petra.Server.MPartner.DataAggregates;
using Ict.Petra.Server.MPartner.Partner.Data.Access;
using Ict.Petra.Server.MSysMan.Data.Access;

namespace Ict.Petra.Server.MFinance.Setup.WebConnectors
{
    /// <summary>
    /// setup the account hierarchy, cost centre hierarchy, and other data relevant for a General Ledger
    /// </summary>
    public partial class TGLSetupWebConnector
    {
        /// <summary>
        /// returns general ledger information
        /// </summary>
        /// <param name="ALedgerNumber"></param>
        /// <returns></returns>
        [RequireModulePermission("FINANCE-1")]
        public static GLSetupTDS LoadLedgerInfo(Int32 ALedgerNumber)
        {
            #region Validate Arguments

            if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - Ledger number must be greater than 0"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }

            #endregion Validate Arguments

            GLSetupTDS MainDS = new GLSetupTDS();

            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("LoadLedgerInfo");

            try
            {
                db.ReadTransaction(
                    ref Transaction,
                    delegate
                    {
                        ALedgerAccess.LoadByPrimaryKey(MainDS, ALedgerNumber, Transaction);
                        AAccountingSystemParameterAccess.LoadByPrimaryKey(MainDS, ALedgerNumber, Transaction);
                        AAccountingPeriodAccess.LoadViaALedger(MainDS, ALedgerNumber, Transaction);
                    });

                #region Validate Data

                if ((MainDS.ALedger == null) || (MainDS.ALedger.Count == 0))
                {
                    throw new EFinanceSystemDataTableReturnedNoDataException(String.Format(Catalog.GetString(
                                "Function:{0} Ledger data for Ledger number {1} does not exist or could not be accessed!"),
                            Utilities.GetMethodName(true),
                            ALedgerNumber));
                }
                else if ((MainDS.AAccountingSystemParameter == null) || (MainDS.AAccountingSystemParameter.Count == 0))
                {
                    throw new EFinanceSystemDataTableReturnedNoDataException(String.Format(Catalog.GetString(
                                "Function:{0} - Accounting System Parameter data for Ledger number {1} does not exist or could not be accessed!"),
                            Utilities.GetMethodName(true),
                            ALedgerNumber));
                }
                else if ((MainDS.AAccountingPeriod == null) || (MainDS.AAccountingPeriod.Count == 0))
                {
                    throw new EFinanceSystemDataTableReturnedNoDataException(String.Format(Catalog.GetString(
                                "Function:{0} - Accounting Period data for Ledger number {1} does not exist or could not be accessed!"),
                            Utilities.GetMethodName(true),
                            ALedgerNumber));
                }

                #endregion Validate Data

                // Accept row changes here so that the Client gets 'unmodified' rows
                MainDS.AcceptChanges();

                // Remove all Tables that were not filled with data before remoting them.
                MainDS.RemoveEmptyTables();
            }
            catch (Exception ex)
            {
                TLogging.LogException(ex, Utilities.GetMethodSignature());
                throw;
            }

            db.CloseDBConnection();

            return MainDS;
        }

        /// <summary>
        /// returns general ledger settings
        /// </summary>
        /// <param name="ALedgerNumber"></param>
        /// <param name="ACalendarStartDate"></param>
        /// <param name="ACurrencyChangeAllowed"></param>
        /// <param name="ACalendarChangeAllowed"></param>
        /// <returns></returns>
        [RequireModulePermission("FINANCE-1")]
        public static GLSetupTDS LoadLedgerSettings(Int32 ALedgerNumber, out DateTime ACalendarStartDate,
            out bool ACurrencyChangeAllowed, out bool ACalendarChangeAllowed)
        {
            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("LoadLedgerSettings");

            #region Validate Arguments

            if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format("Function:{0} - Ledger number must be greater than 0",
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }

            #endregion Validate Arguments

            ACalendarStartDate = DateTime.MinValue;
            ACurrencyChangeAllowed = false;
            ACalendarChangeAllowed = false;

            DateTime CalendarStartDate = ACalendarStartDate;
            bool CurrencyChangeAllowed = ACurrencyChangeAllowed;
            bool CalendarChangeAllowed = ACalendarChangeAllowed;

            GLSetupTDS MainDS = new GLSetupTDS();

            try
            {
                db.ReadTransaction(
                    ref Transaction,
                    delegate
                    {
                        ALedgerAccess.LoadByPrimaryKey(MainDS, ALedgerNumber, Transaction);
                        AAccountingSystemParameterAccess.LoadByPrimaryKey(MainDS, ALedgerNumber, Transaction);
                        ALedgerInitFlagAccess.LoadViaALedger(MainDS, ALedgerNumber, null, Transaction);

                        #region Validate Data

                        //ALedgerInitFlag is optional so no need to check
                        //TODO confirm this

                        if ((MainDS.ALedger == null) || (MainDS.ALedger.Count == 0))
                        {
                            throw new EFinanceSystemDataTableReturnedNoDataException(String.Format(Catalog.GetString(
                                        "Ledger Data for Ledger number {0} does not exist!"), ALedgerNumber));
                        }
                        else if ((MainDS.AAccountingSystemParameter == null) || (MainDS.AAccountingSystemParameter.Count == 0))
                        {
                            throw new EFinanceSystemDataTableReturnedNoDataException(String.Format(Catalog.GetString(
                                        "AccountingSystemParameter Data for Ledger number {0} does not exist!"), ALedgerNumber));
                        }

                        #endregion Validate Data

                        // retrieve calendar start date (start date of financial year)
                        AAccountingPeriodTable CalendarTable = AAccountingPeriodAccess.LoadByPrimaryKey(ALedgerNumber, 1, Transaction);

                        if (CalendarTable.Count > 0)
                        {
                            CalendarStartDate = ((AAccountingPeriodRow)CalendarTable.Rows[0]).PeriodStartDate;
                        }

                        // now check if currency change would be allowed
                        CurrencyChangeAllowed = true;

                        if ((AJournalAccess.CountViaALedger(ALedgerNumber, Transaction) > 0)
                            || (AGiftBatchAccess.CountViaALedger(ALedgerNumber, Transaction) > 0))
                        {
                            // don't allow currency change if journals or gift batches exist
                            CurrencyChangeAllowed = false;
                        }

                        if (AGiftBatchAccess.CountViaALedger(ALedgerNumber, Transaction) > 0)
                        {
                            // don't allow currency change if journals exist
                            CurrencyChangeAllowed = false;
                        }

                        if (CurrencyChangeAllowed)
                        {
                            // don't allow currency change if there are foreign currency accounts for this ledger
                            AAccountTable TemplateTable;
                            AAccountRow TemplateRow;
                            StringCollection TemplateOperators;

                            TemplateTable = new AAccountTable();
                            TemplateRow = TemplateTable.NewRowTyped(false);
                            TemplateRow.LedgerNumber = ALedgerNumber;
                            TemplateRow.ForeignCurrencyFlag = true;
                            TemplateOperators = new StringCollection();
                            TemplateOperators.Add("=");

                            if (AAccountAccess.CountUsingTemplate(TemplateRow, TemplateOperators, Transaction) > 0)
                            {
                                CurrencyChangeAllowed = false;
                            }
                        }

                        // now check if calendar change would be allowed
                        CalendarChangeAllowed = IsCalendarChangeAllowed(ALedgerNumber, Transaction.DataBaseObj);
                    });

                ACalendarStartDate = CalendarStartDate;
                ACurrencyChangeAllowed = CurrencyChangeAllowed;
                ACalendarChangeAllowed = CalendarChangeAllowed;

                // Accept row changes here so that the Client gets 'unmodified' rows
                MainDS.AcceptChanges();

                // Remove all Tables that were not filled with data before remoting them.
                MainDS.RemoveEmptyTables();
            }
            catch (EFinanceSystemDataTableReturnedNoDataException ex)
            {
                throw new EFinanceSystemDataTableReturnedNoDataException(String.Format("Function:{0} - {1}",
                        Utilities.GetMethodName(true),
                        ex.Message));
            }
            catch (Exception ex)
            {
                TLogging.LogException(ex, Utilities.GetMethodSignature());
                throw;
            }

            db.CloseDBConnection();

            return MainDS;
        }

        /// <summary>
        /// returns true if calendar change is allowed for given ledger
        /// </summary>
        /// <param name="ALedgerNumber"></param>
        /// <returns></returns>
        [RequireModulePermission("FINANCE-1")]
        public static bool IsCalendarChangeAllowed(Int32 ALedgerNumber)
        {
            return IsCalendarChangeAllowed(ALedgerNumber, null);
        }

        /// <summary>
        /// returns true if calendar change is allowed for given ledger
        /// </summary>
        /// <param name="ALedgerNumber"></param>
        /// <param name="ADataBase">An instantiated <see cref="TDataBase" /> object, or null. If null gets passed
        /// then the Method executes DB commands with a new Database connection</param>
        /// <returns></returns>
        private static bool IsCalendarChangeAllowed(Int32 ALedgerNumber, TDataBase ADataBase)
        {
            #region Validate Arguments

            if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format("Function:{0} - Ledger number must be greater than 0",
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }

            #endregion Validate Arguments

            Boolean CalendarChangeAllowed = true;

            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("IsCalendarChangeAllowed", ADataBase);

            try
            {
                db.ReadTransaction(
                    ref Transaction,
                    delegate
                    {
                        if ((ABatchAccess.CountViaALedger(ALedgerNumber, Transaction) > 0)
                            || (AGiftBatchAccess.CountViaALedger(ALedgerNumber, Transaction) > 0))
                        {
                            // don't allow calendar change if any batch for this ledger exists
                            CalendarChangeAllowed = false;
                        }
                    });
            }
            catch (Exception ex)
            {
                TLogging.LogException(ex, Utilities.GetMethodSignature());
                throw;
            }

            if (ADataBase == null)
            {
                db.CloseDBConnection();
            }

            return CalendarChangeAllowed;
        }

        /// <summary>
        /// returns number of accounting periods for given ledger
        /// </summary>
        /// <param name="ALedgerNumber"></param>
        /// <returns></returns>
        [RequireModulePermission("FINANCE-1")]
        public static int NumberOfAccountingPeriods(Int32 ALedgerNumber)
        {
            #region Validate Arguments

            if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format("Function:{0} - Ledger number must be greater than 0",
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }

            #endregion Validate Arguments

            int NumberOfAccountingPeriods = 0;

            ALedgerTable LedgerTable = null;
            ALedgerRow LedgerRow = null;

            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("NumberOfAccountingPeriods");

            try
            {
                db.ReadTransaction(
                    ref Transaction,
                    delegate
                    {
                        LedgerTable = ALedgerAccess.LoadByPrimaryKey(ALedgerNumber, Transaction);
                    });

                #region Validate Data

                if ((LedgerTable == null) || (LedgerTable.Count == 0))
                {
                    throw new EFinanceSystemDataTableReturnedNoDataException(String.Format(Catalog.GetString(
                                "Function:{0} - Ledger data for Ledger number {1} does not exist or could not be accessed!"),
                            Utilities.GetMethodName(true),
                            ALedgerNumber));
                }

                #endregion Validate Data

                LedgerRow = (ALedgerRow)LedgerTable.Rows[0];
                NumberOfAccountingPeriods = LedgerRow.NumberOfAccountingPeriods;
            }
            catch (Exception ex)
            {
                TLogging.LogException(ex, Utilities.GetMethodSignature());
                throw;
            }

            db.CloseDBConnection();

            return NumberOfAccountingPeriods;
        }

        /// <summary>
        /// Returns true if specified subsystem is activated for a given Ledger.
        /// </summary>
        /// <param name="ALedgerNumber"></param>
        /// <param name="ASubsystemCode"></param>
        /// <param name="ADataBase">An instantiated <see cref="TDataBase" /> object, or null (default = null). If null gets passed
        /// then the Method executes DB commands with a new Database connection</param>
        /// <returns>True if specified subsystem is activated for a given Ledger, otherwise false.</returns>
        private static bool IsSubsystemActivated(Int32 ALedgerNumber, String ASubsystemCode, TDataBase ADataBase = null)
        {
            #region Validate Arguments

            if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }
            else if (ASubsystemCode.Length == 0)
            {
                throw new ArgumentException(String.Format(Catalog.GetString("Function:{0} - The Subsystem Code is empty!"),
                        Utilities.GetMethodName(true)));
            }

            #endregion Validate Arguments

            Boolean Activated = false;

            ASystemInterfaceTable TemplateTable;
            ASystemInterfaceRow TemplateRow;
            StringCollection TemplateOperators;

            TemplateTable = new ASystemInterfaceTable();
            TemplateRow = TemplateTable.NewRowTyped(false);
            TemplateRow.LedgerNumber = ALedgerNumber;
            TemplateRow.SubSystemCode = ASubsystemCode;
            TemplateRow.SetUpComplete = true;
            TemplateOperators = new StringCollection();
            TemplateOperators.Add("=");

            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("IsSubsystemActivated", ADataBase);

            db.ReadTransaction(
                ref Transaction,
                delegate
                {
                    if (ASystemInterfaceAccess.CountUsingTemplate(TemplateRow, TemplateOperators, Transaction) > 0)
                    {
                        Activated = true;
                    }
                });

            if (ADataBase == null)
            {
                db.CloseDBConnection();
            }

            return Activated;
        }

        /// <summary>
        /// Gets the active/deactivated state of the Accounts Payable (AP) subsystem and of the Gift Processing subsystem for
        /// a given Ledger.
        /// </summary>
        /// <param name="ALedgerNumber"></param>
        /// <param name="AAccountsPayableSubsystemActivated">True if the Accounts Payable (AP) subsystem is activated
        /// for the Ledger specified with <paramref name="ALedgerNumber"/>, otherwise false.</param>
        /// <param name="AGiftProcessingSubsystemActivated">True if the Gift Processing subsystem is activated
        /// for the Ledger specified with <paramref name="ALedgerNumber"/>, otherwise false.</param>
        [RequireModulePermission("FINANCE-1")]
        public static void GetActivatedSubsystems(Int32 ALedgerNumber,
            out bool AAccountsPayableSubsystemActivated, out bool AGiftProcessingSubsystemActivated)
        {
            TDBTransaction DBTransaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("GetActivatedSubsystems");
            bool AccountsPayableSubsystemActivated = false;
            bool GiftProcessingSubsystemActivated = false;

            db.ReadTransaction(
                ref DBTransaction,
                delegate
                {
                    AccountsPayableSubsystemActivated = IsSubsystemActivated(ALedgerNumber,
                        CommonAccountingSubSystemsEnum.AP.ToString(), DBTransaction.DataBaseObj);

                    GiftProcessingSubsystemActivated = IsSubsystemActivated(ALedgerNumber,
                        CommonAccountingSubSystemsEnum.GR.ToString(), DBTransaction.DataBaseObj);
                });

            db.CloseDBConnection();

            AAccountsPayableSubsystemActivated = AccountsPayableSubsystemActivated;
            AGiftProcessingSubsystemActivated = GiftProcessingSubsystemActivated;
        }

        /// <summary>
        /// Returns true if the Gift Processing subsystem is activated for a given Ledger.
        /// </summary>
        /// <returns>True if the Gift Processing subsystem is activated for a given Ledger, otherwise false.</returns>
        [NoRemoting]
        public static bool IsGiftProcessingSubsystemActivated(Int32 ALedgerNumber, TDataBase ADataBase = null)
        {
            return IsSubsystemActivated(ALedgerNumber, CommonAccountingSubSystemsEnum.GR.ToString(), ADataBase);
        }

        /// <summary>
        /// Returns true if the Accounts Payable (AP) subsystem is activated for a given Ledger.
        /// </summary>
        /// <returns>True if the Accounts Payable (AP) subsystem is activated for a given Ledger, otherwise false.</returns>
        [NoRemoting]
        public static bool IsAccountsPayableSubsystemActivated(Int32 ALedgerNumber, TDataBase ADataBase = null)
        {
            return IsSubsystemActivated(ALedgerNumber, CommonAccountingSubSystemsEnum.AP.ToString(), ADataBase);
        }

        /// <summary>
        /// activate subsystem for gift processing for given ledger
        /// </summary>
        [RequireModulePermission("FINANCE-3")]
        public static void ActivateGiftProcessingSubsystem(Int32 ALedgerNumber,
            Int32 AStartingReceiptNumber, TDataBase ADataBase = null)
        {
            #region Validate Arguments

            if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }

            #endregion Validate Arguments

            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("ActivateGiftProcessingSubsystem", ADataBase);
            bool SubmissionOK = false;

            try
            {
                db.WriteTransaction(ref Transaction, ref SubmissionOK,
                    delegate
                    {
                        // if subsystem already active then no need to go further
                        if (!IsGiftProcessingSubsystemActivated(ALedgerNumber, db))
                        {
                            // create or update account for Creditor's Control

                            // make sure transaction type exists for gift processing subsystem
                            ATransactionTypeTable TemplateTransactionTypeTable;
                            ATransactionTypeRow TemplateTransactionTypeRow;
                            StringCollection TemplateTransactionTypeOperators;

                            TemplateTransactionTypeTable = new ATransactionTypeTable();
                            TemplateTransactionTypeRow = TemplateTransactionTypeTable.NewRowTyped(false);
                            TemplateTransactionTypeRow.LedgerNumber = ALedgerNumber;
                            TemplateTransactionTypeRow.SubSystemCode = CommonAccountingSubSystemsEnum.GR.ToString();
                            TemplateTransactionTypeOperators = new StringCollection();
                            TemplateTransactionTypeOperators.Add("=");

                            if (ATransactionTypeAccess.CountUsingTemplate(TemplateTransactionTypeRow, TemplateTransactionTypeOperators,
                                    Transaction) == 0)
                            {
                                ATransactionTypeTable TransactionTypeTable;
                                ATransactionTypeRow TransactionTypeRow;

                                TransactionTypeTable = new ATransactionTypeTable();
                                TransactionTypeRow = TransactionTypeTable.NewRowTyped();
                                TransactionTypeRow.LedgerNumber = ALedgerNumber;
                                TransactionTypeRow.SubSystemCode = CommonAccountingSubSystemsEnum.GR.ToString();
                                TransactionTypeRow.TransactionTypeCode = CommonAccountingTransactionTypesEnum.GR.ToString();
                                TransactionTypeRow.DebitAccountCode = MFinanceConstants.CASH_ACCT; // "CASH";
                                TransactionTypeRow.CreditAccountCode = MFinanceConstants.ACCOUNT_GIFT; // "GIFT";
                                TransactionTypeRow.TransactionTypeDescription = MFinanceConstants.TRANS_TYPE_GIFT_PROCESSING; // "Gift Processing";
                                TransactionTypeRow.SpecialTransactionType = true;
                                TransactionTypeTable.Rows.Add(TransactionTypeRow);
                                ATransactionTypeAccess.SubmitChanges(TransactionTypeTable, Transaction);
                            }

                            ASystemInterfaceTable SystemInterfaceTable = null;
                            ASystemInterfaceRow SystemInterfaceRow = null;

                            SystemInterfaceTable = ASystemInterfaceAccess.LoadByPrimaryKey(ALedgerNumber,
                                CommonAccountingSubSystemsEnum.GR.ToString(),
                                Transaction);

                            #region Validate Data 1

                            if (SystemInterfaceTable == null)
                            {
                                throw new EFinanceSystemDataTableReturnedNoDataException(String.Format(Catalog.GetString(
                                            "System Interface Table data for Ledger number {0} does not exist!"), ALedgerNumber));
                            }

                            #endregion Validate Data 1

                            if (SystemInterfaceTable.Count == 0)
                            {
                                SystemInterfaceRow = SystemInterfaceTable.NewRowTyped();
                                SystemInterfaceRow.LedgerNumber = ALedgerNumber;
                                SystemInterfaceRow.SubSystemCode = CommonAccountingSubSystemsEnum.GR.ToString();
                                SystemInterfaceRow.SetUpComplete = true;
                                SystemInterfaceTable.Rows.Add(SystemInterfaceRow);
                            }
                            else
                            {
                                SystemInterfaceRow = (ASystemInterfaceRow)SystemInterfaceTable.Rows[0];
                                SystemInterfaceRow.SetUpComplete = true;
                            }

                            ASystemInterfaceAccess.SubmitChanges(SystemInterfaceTable, Transaction);

                            // now set the starting receipt number
                            ALedgerTable LedgerTable = ALedgerAccess.LoadByPrimaryKey(ALedgerNumber, Transaction);

                            #region Validate Data 2

                            if ((LedgerTable == null) || (LedgerTable.Count == 0))
                            {
                                throw new EFinanceSystemDataTableReturnedNoDataException(String.Format(Catalog.GetString(
                                            "Ledger Data for Ledger number {0} does not exist!"), ALedgerNumber));
                            }

                            #endregion Validate Data 2

                            ALedgerRow LedgerRow = (ALedgerRow)LedgerTable.Rows[0];
                            LedgerRow.LastHeaderRNumber = AStartingReceiptNumber;

                            ALedgerAccess.SubmitChanges(LedgerTable, Transaction);
                        }

                        SubmissionOK = true;
                    });
            }
            catch (EFinanceSystemDataTableReturnedNoDataException ex)
            {
                throw new EFinanceSystemDataTableReturnedNoDataException(String.Format("Function:{0} - {1}",
                        Utilities.GetMethodName(true),
                        ex.Message));
            }
            catch (Exception ex)
            {
                TLogging.LogException(ex, Utilities.GetMethodSignature());
                throw;
            }

            if (ADataBase == null)
            {
                db.CloseDBConnection();
            }
        }

        /// <summary>
        /// activate subsystem for accounts payable for given ledger
        /// </summary>
        /// <param name="ALedgerNumber"></param>
        [RequireModulePermission("FINANCE-3")]
        public static void ActivateAccountsPayableSubsystem(Int32 ALedgerNumber)
        {
            #region Validate Arguments

            if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }

            #endregion Validate Arguments

            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("ActivateAccountsPayableSubsystem");
            bool SubmissionOK = false;

            try
            {
                db.WriteTransaction(
                    ref Transaction,
                    ref SubmissionOK,
                    delegate
                    {
                        // if subsystem already active then no need to go further
                        if (!IsAccountsPayableSubsystemActivated(ALedgerNumber, db))
                        {
                            // make sure transaction type exists for accounts payable subsystem
                            ATransactionTypeTable TemplateTransactionTypeTable;
                            ATransactionTypeRow TemplateTransactionTypeRow;
                            StringCollection TemplateTransactionTypeOperators;

                            TemplateTransactionTypeTable = new ATransactionTypeTable();
                            TemplateTransactionTypeRow = TemplateTransactionTypeTable.NewRowTyped(false);
                            TemplateTransactionTypeRow.LedgerNumber = ALedgerNumber;
                            TemplateTransactionTypeRow.SubSystemCode = CommonAccountingSubSystemsEnum.AP.ToString();
                            TemplateTransactionTypeOperators = new StringCollection();
                            TemplateTransactionTypeOperators.Add("=");

                            if (ATransactionTypeAccess.CountUsingTemplate(TemplateTransactionTypeRow, TemplateTransactionTypeOperators,
                                    Transaction) == 0)
                            {
                                ATransactionTypeTable TransactionTypeTable;
                                ATransactionTypeRow TransactionTypeRow;

                                TransactionTypeTable = new ATransactionTypeTable();
                                TransactionTypeRow = TransactionTypeTable.NewRowTyped();
                                TransactionTypeRow.LedgerNumber = ALedgerNumber;
                                TransactionTypeRow.SubSystemCode = CommonAccountingSubSystemsEnum.AP.ToString();
                                TransactionTypeRow.TransactionTypeCode = CommonAccountingTransactionTypesEnum.INV.ToString();
                                TransactionTypeRow.DebitAccountCode = MFinanceConstants.ACCOUNT_BAL_SHT;
                                TransactionTypeRow.CreditAccountCode = MFinanceConstants.ACCOUNT_CREDITORS;
                                TransactionTypeRow.TransactionTypeDescription = "Input Creditor's Invoice";
                                TransactionTypeRow.SpecialTransactionType = true;
                                TransactionTypeTable.Rows.Add(TransactionTypeRow);

                                ATransactionTypeAccess.SubmitChanges(TransactionTypeTable, Transaction);
                            }

                            // create or update system interface record for accounts payable
                            ASystemInterfaceTable SystemInterfaceTable;
                            ASystemInterfaceRow SystemInterfaceRow;
                            SystemInterfaceTable = ASystemInterfaceAccess.LoadByPrimaryKey(ALedgerNumber,
                                CommonAccountingSubSystemsEnum.AP.ToString(),
                                Transaction);

                            if (SystemInterfaceTable.Count == 0)
                            {
                                SystemInterfaceRow = SystemInterfaceTable.NewRowTyped();
                                SystemInterfaceRow.LedgerNumber = ALedgerNumber;
                                SystemInterfaceRow.SubSystemCode = CommonAccountingSubSystemsEnum.AP.ToString();
                                SystemInterfaceRow.SetUpComplete = true;
                                SystemInterfaceTable.Rows.Add(SystemInterfaceRow);
                            }
                            else
                            {
                                SystemInterfaceRow = (ASystemInterfaceRow)SystemInterfaceTable.Rows[0];
                                SystemInterfaceRow.SetUpComplete = true;
                            }

                            ASystemInterfaceAccess.SubmitChanges(SystemInterfaceTable, Transaction);

                            SubmissionOK = true;
                        }
                    });
            }
            catch (Exception ex)
            {
                TLogging.LogException(ex, Utilities.GetMethodSignature());
                throw;
            }

            db.CloseDBConnection();
        }

        /// <summary>
        /// returns true if subsystem can be deactivated for given ledger
        /// </summary>
        /// <param name="ALedgerNumber"></param>
        /// <param name="ASubsystemCode"></param>
        /// <returns></returns>
        [RequireModulePermission("FINANCE-1")]
        private static bool CanSubsystemBeDeactivated(Int32 ALedgerNumber, String ASubsystemCode)
        {
            #region Validate Arguments

            if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }
            else if (ASubsystemCode.Length == 0)
            {
                throw new ArgumentException(String.Format(Catalog.GetString("Function:{0} - The Subsystem code is empty!"),
                        Utilities.GetMethodName(true)));
            }

            #endregion Validate Arguments

            Boolean Result = false;

            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("CanSubsystemBeDeactivated");

            try
            {
                db.ReadTransaction(
                    ref Transaction,
                    delegate
                    {
                        if (ASubsystemCode == CommonAccountingSubSystemsEnum.GR.ToString())
                        {
                            // for gift processing don't allow to deactivate if 'Posted' or 'Unposted' gift batches exist
                            AGiftBatchTable TemplateGiftBatchTable;
                            AGiftBatchRow TemplateGiftBatchRow;
                            StringCollection TemplateGiftBatchOperators;

                            TemplateGiftBatchTable = new AGiftBatchTable();
                            TemplateGiftBatchRow = TemplateGiftBatchTable.NewRowTyped(false);
                            TemplateGiftBatchRow.LedgerNumber = ALedgerNumber;
                            TemplateGiftBatchRow.BatchStatus = MFinanceConstants.BATCH_POSTED;
                            TemplateGiftBatchOperators = new StringCollection();
                            TemplateGiftBatchOperators.Add("=");

                            if (AGiftBatchAccess.CountUsingTemplate(TemplateGiftBatchRow, TemplateGiftBatchOperators, Transaction) == 0)
                            {
                                Result = true;
                            }

                            if (!Result)
                            {
                                TemplateGiftBatchRow.BatchStatus = MFinanceConstants.BATCH_UNPOSTED;

                                if (AGiftBatchAccess.CountUsingTemplate(TemplateGiftBatchRow, TemplateGiftBatchOperators, Transaction) == 0)
                                {
                                    Result = true;
                                }
                            }
                        }

                        if (!Result)
                        {
                            AJournalTable TemplateJournalTable;
                            AJournalRow TemplateJournalRow;
                            StringCollection TemplateJournalOperators;

                            TemplateJournalTable = new AJournalTable();
                            TemplateJournalRow = TemplateJournalTable.NewRowTyped(false);
                            TemplateJournalRow.LedgerNumber = ALedgerNumber;
                            TemplateJournalRow.SubSystemCode = ASubsystemCode;
                            TemplateJournalOperators = new StringCollection();
                            TemplateJournalOperators.Add("=");

                            ARecurringJournalTable TemplateRJournalTable;
                            ARecurringJournalRow TemplateRJournalRow;
                            StringCollection TemplateRJournalOperators;

                            TemplateRJournalTable = new ARecurringJournalTable();
                            TemplateRJournalRow = TemplateRJournalTable.NewRowTyped(false);
                            TemplateRJournalRow.LedgerNumber = ALedgerNumber;
                            TemplateRJournalRow.SubSystemCode = ASubsystemCode;
                            TemplateRJournalOperators = new StringCollection();
                            TemplateRJournalOperators.Add("=");

                            // do not allow to deactivate subsystem if journals already exist
                            if ((AJournalAccess.CountUsingTemplate(TemplateJournalRow, TemplateJournalOperators, Transaction) == 0)
                                && (ARecurringJournalAccess.CountUsingTemplate(TemplateRJournalRow, TemplateRJournalOperators, Transaction) == 0))
                            {
                                Result = true;
                            }
                        }
                    });
            }
            catch (Exception ex)
            {
                TLogging.LogException(ex, Utilities.GetMethodSignature());
                throw;
            }

            db.CloseDBConnection();

            return Result;
        }

        /// <summary>
        /// returns true if gift processing subsystem can be deactivated for given ledger
        /// </summary>
        /// <param name="ALedgerNumber"></param>
        /// <returns></returns>
        [RequireModulePermission("FINANCE-1")]
        public static bool CanGiftProcessingSubsystemBeDeactivated(Int32 ALedgerNumber)
        {
            return CanSubsystemBeDeactivated(ALedgerNumber, CommonAccountingSubSystemsEnum.GR.ToString());
        }

        /// <summary>
        /// returns true if accounts payable subsystem can be deactivated for given ledger
        /// </summary>
        /// <param name="ALedgerNumber"></param>
        /// <returns></returns>
        [RequireModulePermission("FINANCE-1")]
        public static bool CanAccountsPayableSubsystemBeDeactivated(Int32 ALedgerNumber)
        {
            return CanSubsystemBeDeactivated(ALedgerNumber, CommonAccountingSubSystemsEnum.AP.ToString());
        }

        /// <summary>
        /// deactivate given subsystem for given ledger
        /// </summary>
        /// <param name="ALedgerNumber"></param>
        /// <param name="ASubsystemCode"></param>
        /// <returns></returns>
        [RequireModulePermission("FINANCE-3")]
        private static bool DeactivateSubsystem(Int32 ALedgerNumber, String ASubsystemCode)
        {
            #region Validate Arguments

            if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }
            else if (ASubsystemCode.Length == 0)
            {
                throw new ArgumentException(String.Format(Catalog.GetString("Function:{0} - The Subsystem code is empty!"),
                        Utilities.GetMethodName(true)));
            }

            #endregion Validate Arguments

            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("DeactivateSubsystem");
            Boolean SubmissionOK = false;

            try
            {
                db.WriteTransaction(
                    ref Transaction,
                    ref SubmissionOK,
                    delegate
                    {
                        ASystemInterfaceAccess.DeleteByPrimaryKey(ALedgerNumber, ASubsystemCode, Transaction);

                        SubmissionOK = true;
                    });
            }
            catch (Exception ex)
            {
                TLogging.LogException(ex, Utilities.GetMethodSignature());
                throw;
            }

            db.CloseDBConnection();

            return SubmissionOK;
        }

        /// <summary>
        /// deactivate subsystem for gift processing for given ledger
        /// </summary>
        /// <param name="ALedgerNumber"></param>
        /// <returns></returns>
        [RequireModulePermission("FINANCE-3")]
        public static bool DeactivateGiftProcessingSubsystem(Int32 ALedgerNumber)
        {
            return DeactivateSubsystem(ALedgerNumber, CommonAccountingSubSystemsEnum.GR.ToString());
        }

        /// <summary>
        /// deactivate subsystem for accounts payable for given ledger
        /// </summary>
        /// <param name="ALedgerNumber"></param>
        /// <returns></returns>
        [RequireModulePermission("FINANCE-3")]
        public static bool DeactivateAccountsPayableSubsystem(Int32 ALedgerNumber)
        {
            return DeactivateSubsystem(ALedgerNumber, CommonAccountingSubSystemsEnum.AP.ToString());
        }

        /// <summary>
        /// returns all account hierarchies available for this ledger
        /// </summary>
        /// <param name="ALedgerNumber"></param>
        /// <returns></returns>
        [RequireModulePermission("FINANCE-1")]
        public static GLSetupTDS LoadAccountHierarchies(Int32 ALedgerNumber)
        {
            #region Validate Arguments

            if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }

            #endregion Validate Arguments

            GLSetupTDS MainDS = new GLSetupTDS();

            // create template for AGeneralLedgerMaster
            TCacheable CachePopulator = new TCacheable();

            System.Type TypeofTable = null;

            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("LoadAccountHierarchies");

            try
            {
                ALedgerTable ledger = (ALedgerTable)CachePopulator.GetCacheableTable(TCacheableFinanceTablesEnum.LedgerDetails,
                    "",
                    false,
                    ALedgerNumber,
                    out TypeofTable);

                #region Validate Data

                if ((ledger == null) || (ledger.Count == 0))
                {
                    throw new EFinanceSystemDataTableReturnedNoDataException(String.Format(Catalog.GetString(
                                "Function:{0} - Ledger data for Ledger number {1} does not exist or could not be accessed!"),
                            Utilities.GetMethodName(true),
                            ALedgerNumber));
                }

                #endregion Validate Data

                int year = ledger[0].CurrentFinancialYear;
                string costCentreCode = "[" + ALedgerNumber + "]";

                AGeneralLedgerMasterRow template = new AGeneralLedgerMasterTable().NewRowTyped(false);
                template.LedgerNumber = ALedgerNumber;
                template.Year = year;
                template.CostCentreCode = costCentreCode;

                db.ReadTransaction(
                    ref Transaction,
                    delegate
                    {
                        ALedgerAccess.LoadByPrimaryKey(MainDS, ALedgerNumber, Transaction);
                        ACurrencyAccess.LoadAll(MainDS, Transaction);
                        AAccountHierarchyAccess.LoadViaALedger(MainDS, ALedgerNumber, Transaction);
                        AAccountHierarchyDetailAccess.LoadViaALedger(MainDS, ALedgerNumber, Transaction);
                        AAccountAccess.LoadViaALedger(MainDS, ALedgerNumber, Transaction);
                        AAccountPropertyAccess.LoadViaALedger(MainDS, ALedgerNumber, Transaction);
                        AAnalysisTypeAccess.LoadViaALedger(MainDS, ALedgerNumber, Transaction);
                        AAnalysisAttributeAccess.LoadViaALedger(MainDS, ALedgerNumber, Transaction);
                        AFreeformAnalysisAccess.LoadViaALedger(MainDS, ALedgerNumber, Transaction);
                        AFeesReceivableAccess.LoadViaALedger(MainDS, ALedgerNumber, Transaction);
                        AFeesPayableAccess.LoadViaALedger(MainDS, ALedgerNumber, Transaction);
                        AGeneralLedgerMasterAccess.LoadUsingTemplate(MainDS, template, Transaction);
                        ASuspenseAccountAccess.LoadViaALedger(MainDS, ALedgerNumber, Transaction);
                        ATransactionTypeAccess.LoadViaALedger(MainDS, ALedgerNumber, Transaction);
                    });

                // set Account BankAccountFlag if there exists a property
                foreach (AAccountPropertyRow accProp in MainDS.AAccountProperty.Rows)
                {
                    if ((accProp.PropertyCode == MFinanceConstants.ACCOUNT_PROPERTY_BANK_ACCOUNT) && (accProp.PropertyValue == "true"))
                    {
                        MainDS.AAccount.DefaultView.RowFilter = String.Format("{0}='{1}'",
                            AAccountTable.GetAccountCodeDBName(),
                            accProp.AccountCode);
                        GLSetupTDSAAccountRow acc = (GLSetupTDSAAccountRow)MainDS.AAccount.DefaultView[0].Row;
                        acc.BankAccountFlag = true;
                        MainDS.AAccount.DefaultView.RowFilter = "";
                    }
                }

                // set Account SuspenseAccountFlag if there exists a property
                foreach (ASuspenseAccountRow suspenseAccountRow in MainDS.ASuspenseAccount.Rows)
                {
                    GLSetupTDSAAccountRow AccountRow =
                        (GLSetupTDSAAccountRow)MainDS.AAccount.Rows.Find(new object[] { ALedgerNumber, suspenseAccountRow.SuspenseAccountCode });
                    AccountRow.SuspenseAccountFlag = true;
                }

                // Don't include any AnalysisType for which there are no values set
                MainDS.AFreeformAnalysis.DefaultView.Sort = AFreeformAnalysisTable.GetAnalysisTypeCodeDBName(); // "a_analysis_type_code_c";

                foreach (AAnalysisTypeRow typeRow in MainDS.AAnalysisType.Rows)
                {
                    Int32 Idx = MainDS.AFreeformAnalysis.DefaultView.Find(typeRow.AnalysisTypeCode);

                    if (Idx < 0)
                    {
                        typeRow.Delete();
                    }
                }

                // add the YTD Actuals to each account
                foreach (AGeneralLedgerMasterRow generalLedgerMasterRow in MainDS.AGeneralLedgerMaster.Rows)
                {
                    GLSetupTDSAAccountRow AccountRow =
                        (GLSetupTDSAAccountRow)MainDS.AAccount.Rows.Find(new object[] { ALedgerNumber, generalLedgerMasterRow.AccountCode });
                    AccountRow.YtdActualBase = generalLedgerMasterRow.YtdActualBase;

                    if (AccountRow.ForeignCurrencyFlag)
                    {
                        AccountRow.YtdActualForeign = generalLedgerMasterRow.YtdActualForeign;
                    }
                }

                // Accept row changes here so that the Client gets 'unmodified' rows
                MainDS.AcceptChanges();

                // Remove all Tables that were not filled with data before remoting them.
                MainDS.RemoveEmptyTables();
            }
            catch (Exception ex)
            {
                TLogging.LogException(ex, Utilities.GetMethodSignature());
                throw;
            }

            db.CloseDBConnection();

            return MainDS;
        }

        private static string InsertNodeIntoHTMLTreeView(GLSetupTDS AMainDS,
            Int32 ALedgerNumber,
            AAccountHierarchyDetailRow ADetailRow,
            bool AIsRootNode = false)
        {
            StringBuilder result = new StringBuilder();

            AAccountRow AccountRow = (AAccountRow)AMainDS.AAccount.Rows.Find(
                new object[] { ALedgerNumber, ADetailRow.ReportingAccountCode });

            string nodeLabel = ADetailRow.ReportingAccountCode;

            if (!AccountRow.IsAccountCodeShortDescNull())
            {
                nodeLabel += " (" + AccountRow.AccountCodeShortDesc + ")";
            }

            if (AIsRootNode)
            {
                result.Append("<ul><li id='acct" + AccountRow.AccountCode + "'><span><i class=\"icon-folder-open\"></i>" + nodeLabel + "</span><ul>");
            }
            else if (!AccountRow.PostingStatus)
            {
                result.Append("<li id='acct" + AccountRow.AccountCode + "'><span><i class=\"icon-minus-sign\"></i>" + nodeLabel + "</span><ul>");
            }
            else if (AccountRow.PostingStatus)
            {
                result.Append("<li id='acct" + AccountRow.AccountCode + "'><span><i class=\"icon-leaf\"></i>" + nodeLabel + "</span></<li>");
            }

            // Now add the children of this node:
            DataView view = new DataView(AMainDS.AAccountHierarchyDetail);
            view.Sort = AAccountHierarchyDetailTable.GetReportOrderDBName() + ", " + AAccountHierarchyDetailTable.GetReportingAccountCodeDBName();
            view.RowFilter =
                AAccountHierarchyDetailTable.GetAccountHierarchyCodeDBName() + " = '" + ADetailRow.AccountHierarchyCode + "' AND " +
                AAccountHierarchyDetailTable.GetAccountCodeToReportToDBName() + " = '" + ADetailRow.ReportingAccountCode + "'";

            if (view.Count > 0)
            {
                foreach (DataRowView rowView in view)
                {
                    AAccountHierarchyDetailRow accountDetail = (AAccountHierarchyDetailRow)rowView.Row;
                    result.Append(InsertNodeIntoHTMLTreeView(AMainDS, ALedgerNumber, accountDetail));
                }
            }

            if (AIsRootNode)
            {
                result.Append("</ul></li></ul>");
            }
            else if (!AccountRow.PostingStatus)
            {
                result.Append("</ul></li>");
            }

            return result.ToString();
        }

        /// <summary>
        /// returns the selected account hierarchy available for this ledger, formatted for the html client
        /// </summary>
        [RequireModulePermission("FINANCE-1")]
        public static string LoadAccountHierarchyHtmlCode(Int32 ALedgerNumber, string AAccountHierarchyCode)
        {
            GLSetupTDS MainDS = LoadAccountHierarchies(ALedgerNumber);

            AAccountHierarchyRow accountHierarchy = (AAccountHierarchyRow)MainDS.AAccountHierarchy.Rows.Find(new object[] { ALedgerNumber,
                                                                                                                            AAccountHierarchyCode });

            StringBuilder result = new StringBuilder();

            if (accountHierarchy != null)
            {
                // find the BALSHT account that is reporting to the root account
                MainDS.AAccountHierarchyDetail.DefaultView.RowFilter =
                    AAccountHierarchyDetailTable.GetAccountHierarchyCodeDBName() + " = '" + AAccountHierarchyCode + "' AND " +
                    AAccountHierarchyDetailTable.GetAccountCodeToReportToDBName() + " = '" + accountHierarchy.RootAccountCode + "'";


                foreach (DataRowView vrow in MainDS.AAccountHierarchyDetail.DefaultView)
                {
                    result.Append(InsertNodeIntoHTMLTreeView(
                        MainDS,
                        ALedgerNumber,
                        (AAccountHierarchyDetailRow)vrow.Row,
                        true));
                }
            }

            return result.ToString();
        }

        private static string InsertNodeIntoHTMLTreeView(GLSetupTDS AMainDS,
            Int32 ALedgerNumber,
            ACostCentreRow ARow,
            bool AIsRootNode = false)
        {
            StringBuilder result = new StringBuilder();

            string nodeLabel = ARow.CostCentreCode;

            if (!ARow.IsCostCentreNameNull())
            {
                nodeLabel += " (" + ARow.CostCentreName + ")";
            }

            if (AIsRootNode)
            {
                result.Append("<ul><li id='acct" + ARow.CostCentreCode + "'><span><i class=\"icon-folder-open\"></i>" + nodeLabel + "</span><ul>");
            }
            else if (!ARow.PostingCostCentreFlag)
            {
                result.Append("<li id='acct" + ARow.CostCentreCode + "'><span><i class=\"icon-minus-sign\"></i>" + nodeLabel + "</span><ul>");
            }
            else if (ARow.PostingCostCentreFlag)
            {
                result.Append("<li id='acct" + ARow.CostCentreCode + "'><span><i class=\"icon-leaf\"></i>" + nodeLabel + "</span></<li>");
            }

            // Now add the children of this node:
            DataView view = new DataView(AMainDS.ACostCentre);
            view.Sort = ACostCentreTable.GetCostCentreCodeDBName() + ", " + ACostCentreTable.GetCostCentreToReportToDBName();
            view.RowFilter =
                ACostCentreTable.GetCostCentreToReportToDBName() + " = '" + ARow.CostCentreCode + "'";

            if (view.Count > 0)
            {
                foreach (DataRowView rowView in view)
                {
                    ACostCentreRow childRow = (ACostCentreRow)rowView.Row;
                    result.Append(InsertNodeIntoHTMLTreeView(AMainDS, ALedgerNumber, childRow));
                }
            }

            if (AIsRootNode)
            {
                result.Append("</ul></li></ul>");
            }
            else if (!ARow.PostingCostCentreFlag)
            {
                result.Append("</ul></li>");
            }

            return result.ToString();
        }

        /// <summary>
        /// returns the cost centre hierarchy available for this ledger, formatted for the html client
        /// </summary>
        [RequireModulePermission("FINANCE-1")]
        public static string LoadCostCentreHierarchyHtmlCode(Int32 ALedgerNumber)
        {
            GLSetupTDS MainDS = LoadCostCentreHierarchy(ALedgerNumber);

            StringBuilder result = new StringBuilder();

            // find the root cost centre
            MainDS.ACostCentre.DefaultView.RowFilter =
                ACostCentreTable.GetCostCentreToReportToDBName() + " IS NULL";

            result.Append(InsertNodeIntoHTMLTreeView(
                    MainDS,
                    ALedgerNumber,
                    (ACostCentreRow)MainDS.ACostCentre.DefaultView[0].Row,
                    true));

            return result.ToString();
        }

        /// <summary>
        /// returns cost centre hierarchy and cost centre details for this ledger
        /// </summary>
        /// <param name="ALedgerNumber"></param>
        /// <returns></returns>
        [RequireModulePermission("FINANCE-1")]
        public static GLSetupTDS LoadCostCentreHierarchy(Int32 ALedgerNumber)
        {
            #region Validate Arguments

            if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }

            #endregion Validate Arguments

            GLSetupTDS MainDS = new GLSetupTDS();

            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("LoadCostCentreHierarchy");

            try
            {
                db.ReadTransaction(
                    ref Transaction,
                    delegate
                    {
                        ALedgerAccess.LoadByPrimaryKey(MainDS, ALedgerNumber, Transaction);
                        ACostCentreAccess.LoadViaALedger(MainDS, ALedgerNumber, Transaction);
                        AValidLedgerNumberAccess.LoadViaALedger(MainDS, ALedgerNumber, Transaction);
                    });

                // Accept row changes here so that the Client gets 'unmodified' rows
                MainDS.AcceptChanges();

                // Remove all Tables that were not filled with data before remoting them.
                MainDS.RemoveEmptyTables();
            }
            catch (Exception ex)
            {
                TLogging.LogException(ex, Utilities.GetMethodSignature());
                throw;
            }

            db.CloseDBConnection();

            return MainDS;
        }

        /// <summary></summary>
        /// <param name="ALedgerNumber"></param>
        /// <returns></returns>
        [RequireModulePermission("FINANCE-1")]
        public static DataTable LoadLocalCostCentres(Int32 ALedgerNumber)
        {
            #region Validate Arguments

            if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }

            #endregion Validate Arguments

            DataTable ParentCostCentreTbl = null;

            // For easier readability
            //                  "SELECT a_cost_centre_code_c AS CostCentreCode, " +
            //                  "a_cost_centre_name_c AS CostCentreName, " +
            //                  "a_posting_cost_centre_flag_l AS Posting, " +
            //                  "a_cost_centre_to_report_to_c AS ReportsTo" +
            //                  " FROM PUB_a_cost_centre" +
            //                  " WHERE a_ledger_number_i = " + ALedgerNumber +
            //                  " AND a_cost_centre_type_c = 'Local';";
            String SqlQuery = String.Format("SELECT {1} AS CostCentreCode, " +
                "{2} AS CostCentreName, {3} AS Posting, {4} AS ReportsTo" +
                " FROM PUB_{0}" +
                " WHERE {5} = {6}" +
                "  AND {7} = 'Local'",
                ACostCentreTable.GetTableDBName(),
                ACostCentreTable.GetCostCentreCodeDBName(),
                ACostCentreTable.GetCostCentreNameDBName(),
                ACostCentreTable.GetPostingCostCentreFlagDBName(),
                ACostCentreTable.GetCostCentreToReportToDBName(),
                ACostCentreTable.GetLedgerNumberDBName(),
                ALedgerNumber,
                ACostCentreTable.GetCostCentreTypeDBName());

            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("LoadLocalCostCentres");

            try
            {
                db.ReadTransaction(
                    ref Transaction,
                    delegate
                    {
                        ParentCostCentreTbl = db.SelectDT(SqlQuery, "ParentCostCentre", Transaction);
                    });
            }
            catch (Exception ex)
            {
                TLogging.LogException(ex, Utilities.GetMethodSignature());
                throw;
            }

            db.CloseDBConnection();

            return ParentCostCentreTbl;
        }

        /// <summary>
        /// LoadCostCentrePartnerLinks
        /// </summary>
        /// <param name="ALedgerNumber"></param>
        /// <param name="APartnerKey"></param>
        /// <param name="APartnerCostCentreTbl"></param>
        /// <param name="ADataBase"></param>
        /// <returns>False when Partner of Type CostCentre but no links exist</returns>
        [RequireModulePermission("FINANCE-1")]
        public static Boolean LoadCostCentrePartnerLinks(Int32 ALedgerNumber, Int64 APartnerKey, out DataTable APartnerCostCentreTbl,
            TDataBase ADataBase = null)
        {
            #region Validate Arguments

            if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }
            else if (APartnerKey < 0)
            {
                throw new ArgumentException(String.Format(Catalog.GetString("Function:{0} - The Partner Key is less than 0!"),
                        Utilities.GetMethodName(true)));
            }

            #endregion Validate Arguments

            APartnerCostCentreTbl = null;
            DataTable PartnerCostCentreTbl = null;
            AValidLedgerNumberTable LinksTbl = null;

            bool PartnerAndLinksCombinationIsValid = true;
            int NumPartnerWithTypeCostCentre = 0;
            int NumPartnerLinks = 0;

            // Load Partners where PartnerType includes "COSTCENTRE":
            String SqlQuery = BuildSQLForCostCentrePartnerLinks(APartnerKey);

            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("LoadCostCentrePartnerLinks", ADataBase);

            try
            {
                db.ReadTransaction(
                    ref Transaction,
                    delegate
                    {
                        PartnerCostCentreTbl = db.SelectDT(SqlQuery, "PartnerCostCentre", Transaction);

                        PartnerCostCentreTbl.DefaultView.Sort = ("PartnerKey");

                        if (APartnerKey > 0)
                        {
                            LinksTbl = AValidLedgerNumberAccess.LoadByPrimaryKey(ALedgerNumber, APartnerKey, Transaction);
                        }
                        else
                        {
                            LinksTbl = AValidLedgerNumberAccess.LoadViaALedger(ALedgerNumber, Transaction);
                        }

                        //See how many rows exist
                        NumPartnerWithTypeCostCentre = PartnerCostCentreTbl.Rows.Count;
                        NumPartnerLinks = LinksTbl.Count;

                        if (NumPartnerWithTypeCostCentre > 0)
                        {
                            if (NumPartnerLinks > 0)
                            {
                                foreach (AValidLedgerNumberRow validLedgNumRow in LinksTbl.Rows)
                                {
                                    Int32 RowIdx = PartnerCostCentreTbl.DefaultView.Find(validLedgNumRow.PartnerKey);

                                    if (RowIdx >= 0)
                                    {
                                        PartnerCostCentreTbl.DefaultView[RowIdx].Row["IsLinked"] = validLedgNumRow.CostCentreCode;
                                        ACostCentreTable cCTbl =
                                            ACostCentreAccess.LoadByPrimaryKey(ALedgerNumber, validLedgNumRow.CostCentreCode, Transaction);

                                        if (cCTbl.Rows.Count > 0)
                                        {
                                            PartnerCostCentreTbl.DefaultView[RowIdx].Row["ReportsTo"] = cCTbl[0].CostCentreToReportTo;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                PartnerAndLinksCombinationIsValid = false;
                                //Empty the partner table as well ready for return
                                PartnerCostCentreTbl.Rows.Clear();
                            }
                        }
                    });
            }
            catch (Exception ex)
            {
                TLogging.LogException(ex, Utilities.GetMethodSignature());
                throw;
            }

            if (ADataBase == null)
            {
                db.CloseDBConnection();
            }

            //Set the out value
            APartnerCostCentreTbl = PartnerCostCentreTbl;

            return PartnerAndLinksCombinationIsValid;
        }

        private static string BuildSQLForCostCentrePartnerLinks(Int64 APartnerKey)
        {
            string RetSQL = string.Empty;

            // For easier readability
            //                  "SELECT p_partner.p_partner_short_name_c as ShortName," +
            //                  "   p_partner.p_partner_key_n as PartnerKey," +
            //                  "  '0' as IsLinked," +
            //                  "  '0' as ReportsTo" +
            //                  " FROM public.p_partner, public.p_partner_type" +
            //                  " WHERE p_partner.p_partner_key_n = p_partner_type.p_partner_key_n";

            //if (APartnerKey > 0)
            //{
            //    SqlQuery += " AND p_partner.p_partner_key_n = " + APartnerKey.ToString();
            //}

            //SqlQuery += " AND p_type_code_c = '" + MPartnerConstants.PARTNERTYPE_COSTCENTRE + "';";

            // Load Partners where PartnerType includes "COSTCENTRE":
            RetSQL = String.Format("SELECT {0}.{2} as ShortName," +
                " {0}.{3} as PartnerKey, '0' as IsLinked, '0' as ReportsTo" +
                " FROM public.{0}, public.{1}" +
                " WHERE {0}.{3} = {1}.{4}",
                PPartnerTable.GetTableDBName(),
                PPartnerTypeTable.GetTableDBName(),
                PPartnerTable.GetPartnerShortNameDBName(),
                PPartnerTable.GetPartnerKeyDBName(),
                PPartnerTypeTable.GetPartnerKeyDBName());

            if (APartnerKey > 0)
            {
                RetSQL += String.Format(" AND {0}.{1} = {2}",
                    PPartnerTable.GetTableDBName(),
                    PPartnerTable.GetPartnerKeyDBName(),
                    APartnerKey);
            }

            RetSQL += String.Format(" AND {0}.{1} = '{2}'",
                PPartnerTypeTable.GetTableDBName(),
                PPartnerTypeTable.GetTypeCodeDBName(),
                MPartnerConstants.PARTNERTYPE_COSTCENTRE);

            return RetSQL;
        }

        /// <summary>
        /// LoadCostCentrePartnerLinks
        /// </summary>
        /// <param name="ALedgerNumber"></param>
        /// <param name="APartnerKey"></param>
        /// <returns>False when Partner of Type CostCentre but no links exist</returns>
        [RequireModulePermission("FINANCE-1")]
        public static Boolean CostCentrePartnerLinksExist(Int32 ALedgerNumber, Int64 APartnerKey)
        {
            #region Validate Arguments

            if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }
            else if (APartnerKey < 0)
            {
                throw new ArgumentException(String.Format(Catalog.GetString("Function:{0} - The Partner Key is less than 0!"),
                        Utilities.GetMethodName(true)));
            }

            #endregion Validate Arguments

            DataTable PartnerCostCentreTbl = null;
            AValidLedgerNumberTable LinksTbl = null;

            bool PartnerAndLinksCombinationIsValid = true;
            int NumPartnerWithTypeCostCentre = 0;
            int NumPartnerLinks = 0;

            // Load Partners where PartnerType includes "COSTCENTRE":
            String SqlQuery = BuildSQLForCostCentrePartnerLinks(APartnerKey);

            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("CostCentrePartnerLinksExist");

            try
            {
                db.ReadTransaction(
                    ref Transaction,
                    delegate
                    {
                        PartnerCostCentreTbl = db.SelectDT(SqlQuery, "PartnerCostCentre", Transaction);

                        if (APartnerKey > 0)
                        {
                            LinksTbl = AValidLedgerNumberAccess.LoadByPrimaryKey(ALedgerNumber, APartnerKey, Transaction);
                        }
                        else
                        {
                            LinksTbl = AValidLedgerNumberAccess.LoadViaALedger(ALedgerNumber, Transaction);
                        }

                        //See how many rows exist
                        NumPartnerWithTypeCostCentre = PartnerCostCentreTbl.Rows.Count;
                        NumPartnerLinks = LinksTbl.Count;

                        if ((NumPartnerWithTypeCostCentre > 0) && (NumPartnerLinks == 0))
                        {
                            PartnerAndLinksCombinationIsValid = false;
                        }
                    });
            }
            catch (Exception ex)
            {
                TLogging.LogException(ex, Utilities.GetMethodSignature());
                throw;
            }

            db.CloseDBConnection();

            return PartnerAndLinksCombinationIsValid;
        }

        /// <summary>
        /// Get Partners linked to Cost Centres so that I can email reports to them.
        /// Extended Dec 2014 to alternatively return a list of email addresses for HOSAs
        /// </summary>
        /// <param name="ALedgerNumber"></param>
        /// <param name="ACostCentreFilter"></param>
        /// <returns></returns>
        [RequireModulePermission("FINANCE-1")]
        public static DataTable GetLinkedPartners(Int32 ALedgerNumber, String ACostCentreFilter)
        {
            #region Validate Arguments

            if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }

            #endregion Validate Arguments

            String SqlQuery = string.Empty;

            DataTable ReturnTable = null;
            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("CostCentrePartnerLinksExist");

            try
            {
                db.ReadTransaction(
                    ref Transaction,
                    delegate
                    {
                        DataTable emailTbl = null;

                        if (ACostCentreFilter == "Foreign")
                        {
                            //        SELECT a_conditional_value_c||'00' AS CostCentreCode, p_email_address_c AS EmailAddress, p_email_address_c AS PartnerShortName"
                            //         FROM a_email_destination
                            //         WHERE a_file_code_c='HOSA';
                            SqlQuery = String.Format("SELECT {1}||'00' AS CostCentreCode," +
                                " {2} AS EmailAddress, {2} AS PartnerShortName" +
                                " FROM {0}" +
                                " WHERE {3} = 'HOSA'",
                                AEmailDestinationTable.GetTableDBName(),
                                AEmailDestinationTable.GetConditionalValueDBName(),
                                AEmailDestinationTable.GetEmailAddressDBName(),
                                AEmailDestinationTable.GetFileCodeDBName());

                            emailTbl = db.SelectDT(SqlQuery, "HosaAddresses", Transaction);

                            ReturnTable = emailTbl;
                        }
                        else
                        {
                            DataTable partnerCostCentreTbl = null;

                            string emailAddress;

                            //                  "SELECT p_partner.p_partner_key_n as PartnerKey, " +
                            //                  " a_cost_centre_code_c as CostCentreCode, " +
                            //                  " '' AS EmailAddress," +
                            //                  " p_partner_short_name_c As PartnerShortName" +
                            //                  " FROM a_valid_ledger_number, p_partner" +
                            //                  " WHERE a_ledger_number_i=" + ALedgerNumber + ACostCentreFilter +
                            //                  " AND p_partner.p_partner_key_n = a_valid_ledger_number.p_partner_key_n" +
                            //                  " ORDER BY a_cost_centre_code_c";
                            SqlQuery = String.Format("SELECT {0}.{2} as PartnerKey," +
                                " {1}.{3} as CostCentreCode, '' AS EmailAddress, {0}.{4} As PartnerShortName" +
                                " FROM {1}, {0}" +
                                " WHERE {1}.{5}={6}{7}" +
                                "  AND {0}.{2} = {1}.{8}" +
                                " ORDER BY {1}.{3}",
                                PPartnerTable.GetTableDBName(),
                                AValidLedgerNumberTable.GetTableDBName(),
                                PPartnerTable.GetPartnerKeyDBName(),
                                AValidLedgerNumberTable.GetCostCentreCodeDBName(),
                                PPartnerTable.GetPartnerShortNameDBName(),
                                AValidLedgerNumberTable.GetLedgerNumberDBName(),
                                ALedgerNumber,
                                ACostCentreFilter,
                                AValidLedgerNumberTable.GetPartnerKeyDBName());

                            partnerCostCentreTbl = db.SelectDT(SqlQuery, "PartnerCostCentre", Transaction);

                            foreach (DataRow Row in partnerCostCentreTbl.Rows)
                            {
                                if (TContactDetailsAggregate.GetPrimaryEmailAddress(Transaction, (Int64)Row["PartnerKey"], out emailAddress))
                                {
                                    // 'Primary Email Address' of Partner (String.Empty is supplied if the Partner hasn't got one)
                                    Row["EmailAddress"] = emailAddress;
                                }
                                else
                                {
                                    Row["EmailAddress"] = String.Empty;
                                }
                            }

                            ReturnTable = partnerCostCentreTbl;
                        }
                    });

                db.CloseDBConnection();

                return ReturnTable;
            }
            catch (Exception ex)
            {
                TLogging.LogException(ex, Utilities.GetMethodSignature());
                throw;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="ALedgerNumber"></param>
        /// <param name="APartnerCostCentreTbl"></param>
        [RequireModulePermission("FINANCE-3")]
        public static void SaveCostCentrePartnerLinks(
            Int32 ALedgerNumber, DataTable APartnerCostCentreTbl)
        {
            #region Validate Arguments

            if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }
            else if (APartnerCostCentreTbl == null)
            {
                throw new EFinanceSystemDataObjectNullOrEmptyException(String.Format(Catalog.GetString(
                            "Function:{0} - The Partner Cost Centre table cannot be accessed!"),
                        Utilities.GetMethodName(true)));
            }

            #endregion Validate Arguments

            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("SaveCostCentrePartnerLinks");
            bool SubmissionOK = false;

            try
            {
                db.WriteTransaction(ref Transaction, ref SubmissionOK,
                    delegate
                    {
                        AValidLedgerNumberTable LinksTbl = AValidLedgerNumberAccess.LoadViaALedger(ALedgerNumber, Transaction);
                        LinksTbl.DefaultView.Sort = "p_partner_key_n";

                        ACostCentreTable CostCentreTbl = ACostCentreAccess.LoadViaALedger(ALedgerNumber, Transaction);
                        CostCentreTbl.DefaultView.Sort = "a_cost_centre_code_c";

                        PPartnerTypeTable PartnerTypeCCToDeleteTbl = null;

                        //DataView partnersWithCCType = new DataView(APartnerCostCentreTbl);
                        //partnersWithCCType.

                        foreach (DataRow Row in APartnerCostCentreTbl.Rows)
                        {
                            string rowCCCode = string.Empty;
                            bool isDeletedRow = (Row.RowState == DataRowState.Deleted);

                            if (!isDeletedRow)
                            {
                                rowCCCode = Convert.ToString(Row["IsLinked"]);
                            }

                            if (!isDeletedRow && (rowCCCode != "0"))   // This should be in the LinksTbl - if it's not, I'll add it.
                            {                       // { AND I probably need to create a CostCentre Row too! }
                                Int32 CostCentreRowIdx = CostCentreTbl.DefaultView.Find(rowCCCode);

                                if (CostCentreRowIdx < 0)       // There's no such Cost Centre - I need to create it now.
                                {
                                    ACostCentreRow newCostCentreRow = CostCentreTbl.NewRowTyped();
                                    newCostCentreRow.LedgerNumber = ALedgerNumber;
                                    newCostCentreRow.CostCentreCode = rowCCCode;
                                    newCostCentreRow.CostCentreToReportTo = Convert.ToString(Row["ReportsTo"]);
                                    newCostCentreRow.CostCentreName = Convert.ToString(Row["ShortName"]);
                                    newCostCentreRow.PostingCostCentreFlag = true;
                                    newCostCentreRow.CostCentreActiveFlag = true;
                                    CostCentreTbl.Rows.Add(newCostCentreRow);
                                }
                                else    // The cost Centre was found, but the match above was case-insensitive.
                                {       // So I'm going to use the actual name from the table, otherwise it might break the DB Constraint.
                                    rowCCCode = CostCentreTbl.DefaultView[CostCentreRowIdx].Row["a_cost_centre_code_c"].ToString();
                                }

                                Int32 RowIdx = LinksTbl.DefaultView.Find(Row["PartnerKey"]);

                                if (RowIdx < 0)
                                {
                                    AValidLedgerNumberRow LinksRow = LinksTbl.NewRowTyped();
                                    LinksRow.LedgerNumber = ALedgerNumber;
                                    LinksRow.PartnerKey = Convert.ToInt64(Row["PartnerKey"]);
                                    LinksRow.IltProcessingCentre = 4000000; // This is the ICH ledger number, but apparently anyone cares about it!
                                    LinksRow.CostCentreCode = rowCCCode;
                                    LinksTbl.Rows.Add(LinksRow);
                                }
                                else    // If this partner is already linked to a cost centre, it's possible the user has changed the code!
                                {
                                    AValidLedgerNumberRow LinksRow = (AValidLedgerNumberRow)LinksTbl.DefaultView[RowIdx].Row;
                                    LinksRow.CostCentreCode = rowCCCode;
                                }
                            }
                            else                // This should not be in the LinksTbl - if it is, I'll delete it.
                            {
                                Int64 partnerKey = (Int64)Row["PartnerKey", isDeletedRow ? DataRowVersion.Original : DataRowVersion.Default];
                                Int32 RowIdx = LinksTbl.DefaultView.Find(partnerKey);

                                if (RowIdx >= 0)
                                {
                                    AValidLedgerNumberRow LinksRow = (AValidLedgerNumberRow)LinksTbl.DefaultView[RowIdx].Row;
                                    LinksRow.Delete();
                                }

                                if (isDeletedRow)
                                {
                                    if (PartnerTypeCCToDeleteTbl == null)
                                    {
                                        PartnerTypeCCToDeleteTbl = new PPartnerTypeTable();
                                    }

                                    //Add each row that needs to be deleted
                                    PartnerTypeCCToDeleteTbl.Merge(PPartnerTypeAccess.LoadByPrimaryKey(partnerKey,
                                            MPartnerConstants.PARTNERTYPE_COSTCENTRE, Transaction));
                                }
                            }
                        }

                        ACostCentreAccess.SubmitChanges(CostCentreTbl, Transaction);
                        AValidLedgerNumberAccess.SubmitChanges(LinksTbl, Transaction);

                        //Process any partner type CC records that need to be deleted.
                        if ((PartnerTypeCCToDeleteTbl != null) && (PartnerTypeCCToDeleteTbl.Count > 0))
                        {
                            //Make sure the data view does not change in size during iteration
                            PartnerTypeCCToDeleteTbl.DefaultView.RowStateFilter = DataViewRowState.OriginalRows;

                            foreach (DataRowView drv in PartnerTypeCCToDeleteTbl.DefaultView)
                            {
                                drv.Row.Delete();
                            }

                            PPartnerTypeAccess.SubmitChanges(PartnerTypeCCToDeleteTbl, Transaction);
                        }

                        SubmissionOK = true;

                        TCacheableTablesManager.GCacheableTablesManager.MarkCachedTableNeedsRefreshing(
                            TCacheableFinanceTablesEnum.CostCentresLinkedToPartnerList.ToString());
                    });
            }
            catch (Exception ex)
            {
                TLogging.LogException(ex, Utilities.GetMethodSignature());
                throw;
            }

            db.CloseDBConnection();
        }

        private static void DropAccountProperties(
            ref GLSetupTDS AInspectDS,
            Int32 ALedgerNumber,
            String AAccountCode)
        {
            #region Validate Arguments

            if (AInspectDS == null)
            {
                throw new EFinanceSystemDataObjectNullOrEmptyException(String.Format(Catalog.GetString("Function:{0} - The Inspect dataset is null!"),
                        Utilities.GetMethodName(true)));
            }
            else if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }
            else if (AAccountCode.Length == 0)
            {
                throw new ArgumentException(String.Format(Catalog.GetString("Function:{0} - The Account code is empty!"),
                        Utilities.GetMethodName(true)));
            }

            #endregion Validate Arguments

            if (AInspectDS.AAccountProperty != null)
            {
                AInspectDS.AAccountProperty.DefaultView.RowFilter = String.Format("{0}={1} and {2}='{3}'",
                    AAccountPropertyTable.GetLedgerNumberDBName(),
                    ALedgerNumber,
                    AAccountPropertyTable.GetAccountCodeDBName(),
                    AAccountCode);

                foreach (DataRowView rv in AInspectDS.AAccountProperty.DefaultView)
                {
                    AAccountPropertyRow accountPropertyRow = (AAccountPropertyRow)rv.Row;
                    accountPropertyRow.Delete();
                }

                AInspectDS.AAccountProperty.DefaultView.RowFilter = string.Empty;
            }
        }

        private static void DropSuspenseAccount(
            ref GLSetupTDS AInspectDS,
            Int32 ALedgerNumber,
            String AAccountCode)
        {
            #region Validate Arguments

            if (AInspectDS == null)
            {
                throw new EFinanceSystemDataObjectNullOrEmptyException(String.Format(Catalog.GetString("Function:{0} - The Inspect dataset is null!"),
                        Utilities.GetMethodName(true)));
            }
            else if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }
            else if (AAccountCode.Length == 0)
            {
                throw new ArgumentException(String.Format(Catalog.GetString("Function:{0} - The Account code is empty!"),
                        Utilities.GetMethodName(true)));
            }

            #endregion Validate Arguments

            if (AInspectDS.ASuspenseAccount != null)
            {
                AInspectDS.ASuspenseAccount.DefaultView.RowFilter = String.Format("{0}={1} and {2}='{3}'",
                    ASuspenseAccountTable.GetLedgerNumberDBName(),
                    ALedgerNumber,
                    ASuspenseAccountTable.GetSuspenseAccountCodeDBName(),
                    AAccountCode);

                foreach (DataRowView rv in AInspectDS.ASuspenseAccount.DefaultView)
                {
                    ASuspenseAccountRow suspenseAccountRow = (ASuspenseAccountRow)rv.Row;
                    suspenseAccountRow.Delete();
                }

                AInspectDS.ASuspenseAccount.DefaultView.RowFilter = "";
            }
        }

        /// <summary>
        /// save general ledger settings
        /// </summary>
        /// <param name="ALedgerNumber"></param>
        /// <param name="ACalendarStartDate"></param>
        /// <param name="AInspectDS"></param>
        /// <returns></returns>
        [RequireModulePermission("FINANCE-3")]
        public static TSubmitChangesResult SaveLedgerSettings(
            Int32 ALedgerNumber,
            DateTime ACalendarStartDate,
            ref GLSetupTDS AInspectDS)
        {
            #region Validate Arguments

            if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }
            else if (AInspectDS == null)
            {
                return TSubmitChangesResult.scrNothingToBeSaved;
            }

            #endregion Validate Arguments

            ALedgerTable LedgerTable;
            ALedgerRow LedgerRow;
            AAccountingPeriodTable AccountingPeriodTable;
            AAccountingPeriodRow AccountingPeriodRow;
            AAccountingPeriodTable NewAccountingPeriodTable;
            AAccountingPeriodRow NewAccountingPeriodRow;
            AGeneralLedgerMasterTable GLMTable;
            AGeneralLedgerMasterRow GLMRow;
            AGeneralLedgerMasterPeriodTable GLMPeriodTable;
            AGeneralLedgerMasterPeriodTable TempGLMPeriodTable;
            AGeneralLedgerMasterPeriodTable NewGLMPeriodTable;
            AGeneralLedgerMasterPeriodRow GLMPeriodRow;
            AGeneralLedgerMasterPeriodRow TempGLMPeriodRow;
            AGeneralLedgerMasterPeriodRow NewGLMPeriodRow;

            int CurrentNumberPeriods;
            int NewNumberPeriods;
            int CurrentNumberFwdPostingPeriods;
            int NewNumberFwdPostingPeriods;
            int CurrentLastFwdPeriod;
            int NewLastFwdPeriod;
            int Period;
            Boolean ExtendFwdPeriods = false;
            DateTime PeriodStartDate;
            DateTime CurrentCalendarStartDate;
            Boolean CreateCalendar = false;

            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("SaveLedgerSettings");
            bool SubmissionOK = false;

            GLSetupTDS InspectDS = AInspectDS;

            try
            {
                db.WriteTransaction(
                    ref Transaction,
                    ref SubmissionOK,
                    delegate
                    {
                        // load ledger row currently saved in database so it can be used for comparison with modified data
                        LedgerTable = ALedgerAccess.LoadByPrimaryKey(ALedgerNumber, Transaction);

                        #region Validate Data

                        if ((LedgerTable == null) || (LedgerTable.Count == 0))
                        {
                            throw new EFinanceSystemDataTableReturnedNoDataException(String.Format(Catalog.GetString(
                                        "Function:{0} - Ledger data for Ledger number {1} does not exist or could not be accessed!"),
                                    Utilities.GetMethodName(true),
                                    ALedgerNumber));
                        }

                        #endregion Validate Data

                        LedgerRow = (ALedgerRow)LedgerTable.Rows[0];

                        if (InspectDS.ALedger != null)
                        {
                            // initialize variables for accounting periods and forward periods
                            CurrentNumberPeriods = LedgerRow.NumberOfAccountingPeriods;
                            NewNumberPeriods = ((ALedgerRow)(InspectDS.ALedger.Rows[0])).NumberOfAccountingPeriods;

                            CurrentNumberFwdPostingPeriods = LedgerRow.NumberFwdPostingPeriods;
                            NewNumberFwdPostingPeriods = ((ALedgerRow)(InspectDS.ALedger.Rows[0])).NumberFwdPostingPeriods;

                            // retrieve currently saved calendar start date (start date of financial year)
                            AAccountingPeriodTable CalendarTable = AAccountingPeriodAccess.LoadByPrimaryKey(ALedgerNumber, 1, Transaction);
                            CurrentCalendarStartDate = DateTime.MinValue;

                            if (CalendarTable.Count > 0)
                            {
                                CurrentCalendarStartDate = ((AAccountingPeriodRow)CalendarTable.Rows[0]).PeriodStartDate;
                            }

                            // update accounting periods (calendar):
                            // this only needs to be done if the calendar mode is changed
                            // or if calendar mode is monthly and the start date has changed
                            // or if not monthly and number of periods has changed
                            if (((ALedgerRow)(InspectDS.ALedger.Rows[0])).CalendarMode != LedgerRow.CalendarMode)
                            {
                                CreateCalendar = true;
                            }
                            else if (((ALedgerRow)(InspectDS.ALedger.Rows[0])).CalendarMode
                                     && (ACalendarStartDate != CurrentCalendarStartDate))
                            {
                                CreateCalendar = true;
                            }
                            else if (!((ALedgerRow)(InspectDS.ALedger.Rows[0])).CalendarMode
                                     && (NewNumberPeriods != CurrentNumberPeriods))
                            {
                                CreateCalendar = true;
                            }

                            if (!CreateCalendar
                                && (NewNumberFwdPostingPeriods < CurrentNumberFwdPostingPeriods))
                            {
                                CreateCalendar = true;
                            }

                            if (!CreateCalendar
                                && (NewNumberFwdPostingPeriods > CurrentNumberFwdPostingPeriods))
                            {
                                // in this case only extend the periods (as there may already be existing transactions)
                                ExtendFwdPeriods = true;
                            }

                            // now perform the actual update of accounting periods (calendar)
                            if (CreateCalendar)
                            {
                                // first make sure all accounting period records are deleted
                                if (AAccountingPeriodAccess.CountViaALedger(ALedgerNumber, Transaction) > 0)
                                {
                                    AAccountingPeriodTable TemplateTable = new AAccountingPeriodTable();
                                    AAccountingPeriodRow TemplateRow = TemplateTable.NewRowTyped(false);
                                    TemplateRow.LedgerNumber = ALedgerNumber;
                                    AAccountingPeriodAccess.DeleteUsingTemplate(TemplateRow, null, Transaction);
                                }

                                // now create all accounting period records according to monthly calendar mode
                                // (at the same time create forwarding periods. If number of forwarding periods also
                                // changes with this saving method then this will be dealt with further down in the code)
                                NewAccountingPeriodTable = new AAccountingPeriodTable();

                                PeriodStartDate = ACalendarStartDate;

                                for (Period = 1; Period <= NewNumberPeriods; Period++)
                                {
                                    NewAccountingPeriodRow = NewAccountingPeriodTable.NewRowTyped();
                                    NewAccountingPeriodRow.LedgerNumber = ALedgerNumber;
                                    NewAccountingPeriodRow.AccountingPeriodNumber = Period;
                                    NewAccountingPeriodRow.PeriodStartDate = PeriodStartDate;

                                    //TODO: Calendar vs Financial Date Handling - Check for current ledger number of periods
                                    if ((((ALedgerRow)(InspectDS.ALedger.Rows[0])).NumberOfAccountingPeriods == 13)
                                        && (Period == 12))
                                    {
                                        // in case of 12 periods the second last period represents the last month except for the very last day
                                        NewAccountingPeriodRow.PeriodEndDate = PeriodStartDate.AddMonths(1).AddDays(-2);
                                    }
                                    else if ((((ALedgerRow)(InspectDS.ALedger.Rows[0])).NumberOfAccountingPeriods == 13)
                                             && (Period == 13))
                                    {
                                        // in case of 13 periods the last period just represents the very last day of the financial year
                                        NewAccountingPeriodRow.PeriodEndDate = PeriodStartDate;
                                    }
                                    else
                                    {
                                        NewAccountingPeriodRow.PeriodEndDate = PeriodStartDate.AddMonths(1).AddDays(-1);
                                    }

                                    NewAccountingPeriodRow.AccountingPeriodDesc = PeriodStartDate.ToString("MMMM");
                                    NewAccountingPeriodTable.Rows.Add(NewAccountingPeriodRow);
                                    PeriodStartDate = NewAccountingPeriodRow.PeriodEndDate.AddDays(1);
                                }

                                AAccountingPeriodAccess.SubmitChanges(NewAccountingPeriodTable, Transaction);

                                TCacheableTablesManager.GCacheableTablesManager.MarkCachedTableNeedsRefreshing(
                                    TCacheableFinanceTablesEnum.AccountingPeriodList.ToString());

                                CurrentNumberPeriods = NewNumberPeriods;
                            }

                            // check if any new forwarding periods need to be created
                            if (CreateCalendar || ExtendFwdPeriods)
                            {
                                // now create new forwarding posting periods (if at all needed)
                                NewAccountingPeriodTable = new AAccountingPeriodTable();

                                // if calendar was created then there are no forward periods yet
                                if (CreateCalendar)
                                {
                                    Period = CurrentNumberPeriods + 1;
                                }
                                else
                                {
                                    Period = CurrentNumberPeriods + CurrentNumberFwdPostingPeriods + 1;
                                }

                                while (Period <= NewNumberPeriods + NewNumberFwdPostingPeriods)
                                {
                                    AccountingPeriodTable = AAccountingPeriodAccess.LoadByPrimaryKey(ALedgerNumber,
                                        Period - CurrentNumberPeriods,
                                        Transaction);
                                    AccountingPeriodRow = (AAccountingPeriodRow)AccountingPeriodTable.Rows[0];

                                    NewAccountingPeriodRow = NewAccountingPeriodTable.NewRowTyped();
                                    NewAccountingPeriodRow.LedgerNumber = ALedgerNumber;
                                    NewAccountingPeriodRow.AccountingPeriodNumber = Period;
                                    NewAccountingPeriodRow.AccountingPeriodDesc = AccountingPeriodRow.AccountingPeriodDesc;
                                    NewAccountingPeriodRow.PeriodStartDate = AccountingPeriodRow.PeriodStartDate.AddYears(1);
                                    NewAccountingPeriodRow.PeriodEndDate = AccountingPeriodRow.PeriodEndDate.AddYears(1);

                                    NewAccountingPeriodTable.Rows.Add(NewAccountingPeriodRow);

                                    Period++;
                                }

                                AAccountingPeriodAccess.SubmitChanges(NewAccountingPeriodTable, Transaction);

                                TCacheableTablesManager.GCacheableTablesManager.MarkCachedTableNeedsRefreshing(
                                    TCacheableFinanceTablesEnum.AccountingPeriodList.ToString());

                                // also create new general ledger master periods with balances
                                CurrentLastFwdPeriod = LedgerRow.NumberOfAccountingPeriods + CurrentNumberFwdPostingPeriods;
                                NewLastFwdPeriod = LedgerRow.NumberOfAccountingPeriods + NewNumberFwdPostingPeriods;
                                // TODO: the following 2 lines would need to replace the 2 lines above if not all possible forward periods are created initially
                                //CurrentLastFwdPeriod = LedgerRow.CurrentPeriod + CurrentNumberFwdPostingPeriods;
                                //NewLastFwdPeriod = LedgerRow.CurrentPeriod + NewNumberFwdPostingPeriods;

                                GLMTable = new AGeneralLedgerMasterTable();
                                AGeneralLedgerMasterRow template = GLMTable.NewRowTyped(false);

                                template.LedgerNumber = ALedgerNumber;
                                template.Year = LedgerRow.CurrentFinancialYear;

                                // find all general ledger master records of the current financial year for given ledger
                                GLMTable = AGeneralLedgerMasterAccess.LoadUsingTemplate(template, Transaction);

                                NewGLMPeriodTable = new AGeneralLedgerMasterPeriodTable();

                                foreach (DataRow Row in GLMTable.Rows)
                                {
                                    // for each of the general ledger master records of the current financial year set the
                                    // new, extended forwarding glm period records (most likely they will not exist yet
                                    // but if they do then update values)
                                    GLMRow = (AGeneralLedgerMasterRow)Row;
                                    GLMPeriodTable =
                                        AGeneralLedgerMasterPeriodAccess.LoadByPrimaryKey(GLMRow.GlmSequence, CurrentLastFwdPeriod, Transaction);

                                    if (GLMPeriodTable.Count > 0)
                                    {
                                        GLMPeriodRow = (AGeneralLedgerMasterPeriodRow)GLMPeriodTable.Rows[0];

                                        for (Period = CurrentLastFwdPeriod + 1; Period <= NewLastFwdPeriod; Period++)
                                        {
                                            if (AGeneralLedgerMasterPeriodAccess.Exists(GLMPeriodRow.GlmSequence, Period, Transaction))
                                            {
                                                // if the record already exists then just change values
                                                TempGLMPeriodTable = AGeneralLedgerMasterPeriodAccess.LoadByPrimaryKey(GLMPeriodRow.GlmSequence,
                                                    Period,
                                                    Transaction);
                                                TempGLMPeriodRow = (AGeneralLedgerMasterPeriodRow)TempGLMPeriodTable.Rows[0];
                                                TempGLMPeriodRow.ActualBase = GLMPeriodRow.ActualBase;
                                                TempGLMPeriodRow.ActualIntl = GLMPeriodRow.ActualIntl;

                                                if (!GLMPeriodRow.IsActualForeignNull())
                                                {
                                                    TempGLMPeriodRow.ActualForeign = GLMPeriodRow.ActualForeign;
                                                }
                                                else
                                                {
                                                    TempGLMPeriodRow.SetActualForeignNull();
                                                }

                                                NewGLMPeriodTable.Merge(TempGLMPeriodTable, true);
                                            }
                                            else
                                            {
                                                // add new row since it does not exist yet
                                                NewGLMPeriodRow = NewGLMPeriodTable.NewRowTyped();
                                                NewGLMPeriodRow.GlmSequence = GLMPeriodRow.GlmSequence;
                                                NewGLMPeriodRow.PeriodNumber = Period;
                                                NewGLMPeriodRow.ActualBase = GLMPeriodRow.ActualBase;
                                                NewGLMPeriodRow.ActualIntl = GLMPeriodRow.ActualIntl;

                                                if (!GLMPeriodRow.IsActualForeignNull())
                                                {
                                                    NewGLMPeriodRow.ActualForeign = GLMPeriodRow.ActualForeign;
                                                }
                                                else
                                                {
                                                    NewGLMPeriodRow.SetActualForeignNull();
                                                }

                                                NewGLMPeriodTable.Rows.Add(NewGLMPeriodRow);
                                            }
                                        }

                                        // remove periods if the number of periods + forwarding periods has been reduced
                                        int NumberOfExistingPeriods = LedgerRow.NumberOfAccountingPeriods + LedgerRow.NumberFwdPostingPeriods;

                                        while ((NewNumberPeriods + NewNumberFwdPostingPeriods) < NumberOfExistingPeriods)
                                        {
                                            AGeneralLedgerMasterPeriodAccess.DeleteByPrimaryKey(GLMPeriodRow.GlmSequence,
                                                NumberOfExistingPeriods,
                                                Transaction);

                                            NumberOfExistingPeriods--;
                                        }
                                    }
                                }

                                // just one SubmitChanges for all records needed
                                AGeneralLedgerMasterPeriodAccess.SubmitChanges(NewGLMPeriodTable, Transaction);
                            }
                        }

                        // update a_ledger_init_flag records for:
                        // suspense account flag: "SUSP-ACCT"
                        // budget flag: "BUDGET"
                        // base currency: "CURRENCY"
                        // international currency: "INTL-CURRENCY" (this is a new flag for OpenPetra)
                        // current period (start of ledger date): CURRENT-PERIOD
                        // calendar settings: CAL
                        // (Apparently no-one currently looks for the presence of any of these flags?)
                        TLedgerInitFlag flag = new TLedgerInitFlag(ALedgerNumber, "", Transaction.DataBaseObj);
                        flag.SetOrRemoveFlag(MFinanceConstants.LEDGER_INIT_FLAG_SUSP_ACC, LedgerRow.SuspenseAccountFlag);
                        flag.SetOrRemoveFlag(MFinanceConstants.LEDGER_INIT_FLAG_BUDGET, LedgerRow.BudgetControlFlag);
                        flag.SetOrRemoveFlag(MFinanceConstants.LEDGER_INIT_FLAG_CURRENCY, !LedgerRow.IsBaseCurrencyNull());
                        flag.SetOrRemoveFlag(MFinanceConstants.LEDGER_INIT_FLAG_INTL_CURRENCY,
                            !LedgerRow.IsIntlCurrencyNull());
                        flag.SetOrRemoveFlag(MFinanceConstants.LEDGER_INIT_FLAG_CURRENT_PERIOD,
                            !LedgerRow.IsCurrentPeriodNull());
                        flag.SetOrRemoveFlag(MFinanceConstants.LEDGER_INIT_FLAG_CAL,
                            !LedgerRow.IsNumberOfAccountingPeriodsNull());

                        GLSetupTDSAccess.SubmitChanges(InspectDS, Transaction.DataBaseObj);

                        SubmissionOK = true;
                    });
            }
            catch (Exception ex)
            {
                TLogging.LogException(ex, Utilities.GetMethodSignature());
                throw;
            }

            db.CloseDBConnection();

            return TSubmitChangesResult.scrOK;
        }

        /// <summary>
        /// save modified account hierarchy etc; does not support moving accounts;
        /// also used for saving cost centre hierarchy and cost centre details
        /// </summary>
        /// <param name="ALedgerNumber"></param>
        /// <param name="AInspectDS"></param>
        /// <param name="AVerificationResult"></param>
        /// <returns></returns>
        [RequireModulePermission("FINANCE-3")]
        public static TSubmitChangesResult SaveGLSetupTDS(
            Int32 ALedgerNumber,
            ref GLSetupTDS AInspectDS,
            out TVerificationResultCollection AVerificationResult)
        {
            //Put first because is required within argument validation
            AVerificationResult = new TVerificationResultCollection();

            #region Validate Arguments

            if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }
            else if (AInspectDS == null)
            {
                return TSubmitChangesResult.scrNothingToBeSaved;
            }

            #endregion Validate Arguments

            TSubmitChangesResult ReturnValue = TSubmitChangesResult.scrOK;

            if ((AInspectDS.ACostCentre != null) && (AInspectDS.AValidLedgerNumber != null))
            {
                // check for removed cost centres, and also delete the AValidLedgerNumber row if there is one for the removed cost centre
                foreach (ACostCentreRow cc in AInspectDS.ACostCentre.Rows)
                {
                    if (cc.RowState == DataRowState.Deleted)
                    {
                        string CostCentreCodeToDelete = cc[ACostCentreTable.ColumnCostCentreCodeId, DataRowVersion.Original].ToString();

                        AInspectDS.AValidLedgerNumber.DefaultView.RowFilter =
                            String.Format("{0}='{1}'",
                                AValidLedgerNumberTable.GetCostCentreCodeDBName(),
                                CostCentreCodeToDelete);

                        foreach (DataRowView rv in AInspectDS.AValidLedgerNumber.DefaultView)
                        {
                            AValidLedgerNumberRow ValidLedgerNumberRow = (AValidLedgerNumberRow)rv.Row;

                            ValidLedgerNumberRow.Delete();
                        }
                    }
                }

                AInspectDS.AValidLedgerNumber.DefaultView.RowFilter = "";
            }

            if (AInspectDS.AAccount != null)
            {
                // check each AAccount row
                foreach (GLSetupTDSAAccountRow acc in AInspectDS.AAccount.Rows)
                {
                    // special treatment of deleted accounts
                    if (acc.RowState == DataRowState.Deleted)
                    {
                        // delete all account properties as well
                        string AccountCodeToDelete = acc[GLSetupTDSAAccountTable.ColumnAccountCodeId, DataRowVersion.Original].ToString();

                        DropAccountProperties(ref AInspectDS, ALedgerNumber, AccountCodeToDelete);
                        DropSuspenseAccount(ref AInspectDS, ALedgerNumber, AccountCodeToDelete);

                        continue;
                    }

                    /* BankAccountFlag */

                    // if the flag has been changed by the client, it will not be null
                    if (!acc.IsBankAccountFlagNull())
                    {
                        if (AInspectDS.AAccountProperty == null)
                        {
                            // because AccountProperty has not been changed on the client, GetChangesTyped will have removed the table
                            // so we need to reload the table from the database
                            AInspectDS.Merge(new AAccountPropertyTable());

                            GLSetupTDS inspectDS = AInspectDS;
                            TDBTransaction transaction = new TDBTransaction();
                            TDataBase db = DBAccess.Connect("SaveGLSetupTDS");

                            db.ReadTransaction(
                                ref transaction,
                                delegate
                                {
                                    AAccountPropertyAccess.LoadViaALedger(inspectDS, ALedgerNumber, transaction);
                                });

                            db.CloseDBConnection();
                        }

                        AInspectDS.AAccountProperty.DefaultView.RowFilter =
                            String.Format("{0}='{1}' and {2}='{3}'",
                                AAccountPropertyTable.GetAccountCodeDBName(),
                                acc.AccountCode,
                                AAccountPropertyTable.GetPropertyCodeDBName(),
                                MFinanceConstants.ACCOUNT_PROPERTY_BANK_ACCOUNT);

                        if ((AInspectDS.AAccountProperty.DefaultView.Count == 0) && acc.BankAccountFlag)
                        {
                            AAccountPropertyRow accProp = AInspectDS.AAccountProperty.NewRowTyped(true);
                            accProp.LedgerNumber = acc.LedgerNumber;
                            accProp.AccountCode = acc.AccountCode;
                            accProp.PropertyCode = MFinanceConstants.ACCOUNT_PROPERTY_BANK_ACCOUNT;
                            accProp.PropertyValue = "true";
                            AInspectDS.AAccountProperty.Rows.Add(accProp);
                        }
                        else if (AInspectDS.AAccountProperty.DefaultView.Count == 1)
                        {
                            AAccountPropertyRow accProp = (AAccountPropertyRow)AInspectDS.AAccountProperty.DefaultView[0].Row;

                            if (!acc.BankAccountFlag)
                            {
                                accProp.Delete();
                            }
                            else
                            {
                                accProp.PropertyValue = "true";
                            }
                        }

                        AInspectDS.AAccountProperty.DefaultView.RowFilter = "";
                    }

                    /* SuspenseAccountFlag */

                    // if the flag has been changed by the client, it will not be null
                    if (!acc.IsSuspenseAccountFlagNull())
                    {
                        if (AInspectDS.ASuspenseAccount == null)
                        {
                            // because ASuspenseAccount has not been changed on the client, GetChangesTyped will have removed the table
                            // so we need to reload the table from the database
                            AInspectDS.Merge(new ASuspenseAccountTable());

                            GLSetupTDS inspectDS = AInspectDS;
                            TDBTransaction transaction = new TDBTransaction();
                            TDataBase db = DBAccess.Connect("SaveGLSetupTDS");

                            db.ReadTransaction(
                                ref transaction,
                                delegate
                                {
                                    ASuspenseAccountAccess.LoadViaALedger(inspectDS, ALedgerNumber, transaction);
                                });

                            db.CloseDBConnection();
                        }

                        AInspectDS.ASuspenseAccount.DefaultView.RowFilter =
                            String.Format("{0}='{1}'",
                                ASuspenseAccountTable.GetSuspenseAccountCodeDBName(),
                                acc.AccountCode);

                        // create a new suspense account record
                        if ((AInspectDS.ASuspenseAccount.DefaultView.Count == 0) && acc.SuspenseAccountFlag)
                        {
                            ASuspenseAccountRow accProp = AInspectDS.ASuspenseAccount.NewRowTyped(true);
                            accProp.LedgerNumber = acc.LedgerNumber;
                            accProp.SuspenseAccountCode = acc.AccountCode;
                            AInspectDS.ASuspenseAccount.Rows.Add(accProp);
                        }
                        // delete a suspense account record
                        else if (AInspectDS.ASuspenseAccount.DefaultView.Count == 1)
                        {
                            ASuspenseAccountRow accProp = (ASuspenseAccountRow)AInspectDS.ASuspenseAccount.DefaultView[0].Row;

                            if (!acc.SuspenseAccountFlag)
                            {
                                accProp.Delete();
                            }
                        }

                        AInspectDS.ASuspenseAccount.DefaultView.RowFilter = "";
                    } // Suspense Account

                    //
                    // If the user has changed the AccountType, the DebitCreditIndicator must be correctly set,
                    // and if this is changed, any existing GLM and GLMP records must change sign!

                    if (acc.RowState == DataRowState.Modified)
                    {
                        String previousAccType = acc[AAccountTable.ColumnAccountTypeId, DataRowVersion.Original].ToString();

                        if (acc.AccountType != previousAccType)
                        {
                            Boolean prevDebitCredit = Convert.ToBoolean(acc[AAccountTable.ColumnDebitCreditIndicatorId, DataRowVersion.Original]);

                            switch (acc.AccountType)
                            {
                                case "Expense":
                                case "Asset":
                                    acc.DebitCreditIndicator = true;
                                    break;

                                case "Income":
                                case "Liability":
                                case "Equity":
                                    acc.DebitCreditIndicator = false;
                                    break;
                            }

                            if (acc.DebitCreditIndicator != prevDebitCredit)
                            {
                                TDBTransaction transaction = new TDBTransaction();
                                TDataBase db = DBAccess.Connect("db");
                                bool SubmitOK = true;
                                db.WriteTransaction(ref transaction, ref SubmitOK,
                                    delegate
                                    {
                                        String query =
                                            "UPDATE a_general_ledger_master set " +
                                            " a_start_balance_base_n = - a_start_balance_base_n, " +
                                            " a_start_balance_foreign_n = - a_start_balance_foreign_n, " +
                                            " a_start_balance_intl_n = - a_start_balance_intl_n, " +
                                            " a_ytd_actual_base_n = - a_ytd_actual_base_n, " +
                                            " a_ytd_actual_foreign_n = - a_ytd_actual_foreign_n, " +
                                            " a_ytd_actual_intl_n = - a_ytd_actual_intl_n, " +
                                            " a_closing_period_actual_base_n = - a_closing_period_actual_base_n, " +
                                            // " a_closing_period_actual_foreign_n = - a_closing_period_actual_foreign_n, " + !We don't have one of these!
                                            " a_closing_period_actual_intl_n = - a_closing_period_actual_intl_n " +
                                            " WHERE a_ledger_number_i = " + ALedgerNumber +
                                            " AND a_account_code_c = '" + acc.AccountCode + "'";
                                        db.ExecuteNonQuery(query, transaction);

                                        query =
                                            "UPDATE a_general_ledger_master_period a set " +
                                            " a_actual_base_n = - a_actual_base_n, " +
                                            " a_budget_base_n = - a_budget_base_n, " +
                                            " a_actual_intl_n = - a_actual_intl_n, " +
                                            " a_budget_intl_n = - a_budget_intl_n, " +
                                            " a_actual_foreign_n = - a_actual_foreign_n " +
                                            " where exists (select 1 from a_general_ledger_master b " +
                                            " where a.a_glm_sequence_i = b.a_glm_sequence_i " +
                                            " AND a_ledger_number_i = " + ALedgerNumber +
                                            " AND a_account_code_c = '" + acc.AccountCode + "')";
                                        db.ExecuteNonQuery(query, transaction);
                                    });

                                db.CloseDBConnection();
                            }
                        }
                    }
                }
            }

            if (AInspectDS.AAnalysisType != null)
            {
                if (AInspectDS.AAnalysisType.Rows.Count > 0)
                {
                    ValidateAAnalysisType(ref AVerificationResult, AInspectDS.AAnalysisType);
                    ValidateAAnalysisTypeManual(ref AVerificationResult, AInspectDS.AAnalysisType);

                    if (!TVerificationHelper.IsNullOrOnlyNonCritical(AVerificationResult))
                    {
                        ReturnValue = TSubmitChangesResult.scrError;
                    }
                }
            }

            if (ReturnValue != TSubmitChangesResult.scrError)
            {
                TDataBase db = DBAccess.Connect("db");
                TDBTransaction transaction = db.BeginTransaction(IsolationLevel.Serializable);

                GLSetupTDSAccess.SubmitChanges(AInspectDS, transaction.DataBaseObj);

                if (AInspectDS.AAnalysisAttribute != null)
                {
                    AInspectDS.AAnalysisAttribute.AcceptChanges(); // This may prevent a constraints exception when the dataset is returned and merged.
                }

                ReturnValue = TSubmitChangesResult.scrOK;

                transaction.Commit();

                db.CloseDBConnection();
            }

            TCacheableTablesManager.GCacheableTablesManager.MarkCachedTableNeedsRefreshing(
                TCacheableFinanceTablesEnum.AccountList.ToString());
            TCacheableTablesManager.GCacheableTablesManager.MarkCachedTableNeedsRefreshing(
                TCacheableFinanceTablesEnum.AnalysisTypeList.ToString());
            TCacheableTablesManager.GCacheableTablesManager.MarkCachedTableNeedsRefreshing(
                TCacheableFinanceTablesEnum.CostCentreList.ToString());
            TCacheableTablesManager.GCacheableTablesManager.MarkCachedTableNeedsRefreshing(
                TCacheableFinanceTablesEnum.SuspenseAccountList.ToString());

            if (AVerificationResult.Count > 0)
            {
                // Downgrade TScreenVerificationResults to TVerificationResults in order to allow
                // Serialisation (needed for .NET Remoting).
                TVerificationResultCollection.DowngradeScreenVerificationResults(AVerificationResult);
            }

            return ReturnValue;
        }

        private static bool AccountHasChildren(Int32 ALedgerNumber, string AAccountCode, TDBTransaction ATransaction)
        {
            #region Validate Arguments

            if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }
            else if (AAccountCode.Length == 0)
            {
                throw new ArgumentException(String.Format(Catalog.GetString("Function:{0} - The Account code is empty!"),
                        Utilities.GetMethodName(true)));
            }
            else if (ATransaction == null)
            {
                throw new EFinanceSystemDBTransactionNullException(String.Format(Catalog.GetString(
                            "Function:{0} - Database Transaction must not be NULL!"),
                        Utilities.GetMethodName(true)));
            }

            #endregion Validate Arguments

            String QuerySql =
                "SELECT COUNT (*) FROM PUB_a_account_hierarchy_detail WHERE " +
                "a_ledger_number_i=" + ALedgerNumber + " AND " +
                "a_account_code_to_report_to_c = '" + AAccountCode + "';";

            object SqlResult = ATransaction.DataBaseObj.ExecuteScalar(QuerySql, ATransaction);

            return Convert.ToInt32(SqlResult) > 0;
        }

        /// <summary>I can add child accounts to this account if it's a summary account,
        ///          or if there have never been transactions posted to it, and no current budget.
        ///
        ///          (If children are added to this account, it will be promoted to a summary account.)
        ///
        ///          I can delete this account if it has no transactions posted as above,
        ///          AND it has no children.
        /// </summary>
        /// <param name="ALedgerNumber"></param>
        /// <param name="AAccountCode"></param>
        /// <param name="ACanBeParent"></param>
        /// <param name="ACanDelete"></param>
        /// <param name="AMsg"></param>
        /// <returns></returns>
        [RequireModulePermission("FINANCE-1")]
        public static Boolean GetAccountCodeAttributes(Int32 ALedgerNumber,
            String AAccountCode,
            out bool ACanBeParent,
            out bool ACanDelete,
            out String AMsg)
        {
            #region Validate Arguments

            if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }
            else if (AAccountCode.Length == 0)
            {
                throw new ArgumentException(String.Format(Catalog.GetString("Function:{0} - The Account code is empty!"),
                        Utilities.GetMethodName(true)));
            }

            #endregion Validate Arguments

            bool CanBeParent = true;
            bool CanDelete = true;
            bool DbSuccess = true;
            string Msg = "";

            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("GetAccountCodeAttributes");

            try
            {
                db.ReadTransaction(
                    ref Transaction,
                    delegate
                    {
                        AAccountTable AccountTbl = AAccountAccess.LoadByPrimaryKey(ALedgerNumber, AAccountCode, Transaction);

                        if (AccountTbl.Rows.Count < 1)  // This shouldn't happen..
                        {
                            DbSuccess = false;
                        }
                        else
                        {
                            bool IsParent = AccountHasChildren(ALedgerNumber, AAccountCode, Transaction);
                            AAccountRow AccountRow = AccountTbl[0];
                            CanBeParent = IsParent; // If it's a summary account, it's OK (This shouldn't happen either, because the client shouldn't ask me!)
                            CanDelete = !IsParent;

                            if (!CanDelete)
                            {
                                Msg = Catalog.GetString("Account is a summary account with other accounts reporting into it.");
                            }

                            if (!CanBeParent || CanDelete)
                            {
                                List <TRowReferenceInfo>CascadingReferences;
                                Int32 Refs = AAccountCascading.CountByPrimaryKey(
                                    ALedgerNumber, AAccountCode,
                                    3, Transaction, true,
                                    out CascadingReferences);

                                bool IsInUse = (Refs > 1);

                                CanBeParent = !IsInUse;    // For posting accounts, I can still add children (and upgrade the account) if there's nothing posted to it yet.
                                CanDelete = !IsInUse;      // Once it has transactions posted, I can't delete it, ever.

                                if (!CanDelete)
                                {
                                    Msg = Catalog.GetString("Account has been used in Transactions.");
                                }
                            }
                        }
                    });

                ACanBeParent = CanBeParent;
                ACanDelete = CanDelete;
                AMsg = Msg;
            }
            catch (Exception ex)
            {
                TLogging.LogException(ex, Utilities.GetMethodSignature());
                throw;
            }

            db.CloseDBConnection();

            return DbSuccess;
        }

        /// <summary>
        /// helper function for ExportAccountHierarchy
        /// </summary>
        private static void InsertNodeIntoXmlDocument(GLSetupTDS AMainDS,
            XmlDocument ADoc,
            XmlNode AParentNode,
            AAccountHierarchyDetailRow ADetailRow)
        {
            #region Validate Arguments

            if (AMainDS == null)
            {
                throw new EFinanceSystemDataObjectNullOrEmptyException(String.Format(Catalog.GetString(
                            "Function:{0} - The GL Posting dataset is null!"),
                        Utilities.GetMethodName(true)));
            }
            else if (AMainDS.AAccount == null)
            {
                throw new EFinanceSystemDataObjectNullOrEmptyException(String.Format(Catalog.GetString(
                            "Function:{0} - The Account table does not exist in the dataset!"),
                        Utilities.GetMethodName(true)));
            }
            else if (AMainDS.AAccountProperty == null)
            {
                throw new EFinanceSystemDataObjectNullOrEmptyException(String.Format(Catalog.GetString(
                            "Function:{0} - The Account Property table does not exist in the dataset!"),
                        Utilities.GetMethodName(true)));
            }
            else if (AMainDS.AAccountHierarchyDetail == null)
            {
                throw new EFinanceSystemDataObjectNullOrEmptyException(String.Format(Catalog.GetString(
                            "Function:{0} - The Account Hierarchy Detail table does not exist in the dataset!"),
                        Utilities.GetMethodName(true)));
            }

            #endregion Validate Arguments

            AAccountRow account = (AAccountRow)AMainDS.AAccount.Rows.Find(new object[] { ADetailRow.LedgerNumber, ADetailRow.ReportingAccountCode });
            XmlElement accountNode = ADoc.CreateElement(TYml2Xml.XMLELEMENT);

            // AccountCodeToReportTo and ReportOrder are encoded implicitly
            accountNode.SetAttribute("name", ADetailRow.ReportingAccountCode);
            accountNode.SetAttribute("active", account.AccountActiveFlag ? "True" : "False");
            accountNode.SetAttribute("type", account.AccountType.ToString());
            accountNode.SetAttribute("debitcredit", account.DebitCreditIndicator ? "debit" : "credit");
            accountNode.SetAttribute("validcc", account.ValidCcCombo);
            accountNode.SetAttribute("shortdesc", account.EngAccountCodeShortDesc);

            if (account.EngAccountCodeLongDesc != account.EngAccountCodeShortDesc)
            {
                accountNode.SetAttribute("longdesc", account.EngAccountCodeLongDesc);
            }

            if (account.EngAccountCodeShortDesc != account.AccountCodeShortDesc)
            {
                accountNode.SetAttribute("localdesc", account.AccountCodeShortDesc);
            }

            if (account.EngAccountCodeLongDesc != account.AccountCodeLongDesc)
            {
                accountNode.SetAttribute("locallongdesc", account.AccountCodeLongDesc);
            }

            if (AMainDS.AAccountProperty.Rows.Find(new object[] { account.LedgerNumber, account.AccountCode,
                                                                  MFinanceConstants.ACCOUNT_PROPERTY_BANK_ACCOUNT, "true" }) != null)
            {
                accountNode.SetAttribute("bankaccount", "true");
            }

            if (account.ForeignCurrencyFlag)
            {
                accountNode.SetAttribute("currency", account.ForeignCurrencyCode);
            }

            AParentNode.AppendChild(accountNode);

            AMainDS.AAccountHierarchyDetail.DefaultView.Sort = AAccountHierarchyDetailTable.GetReportOrderDBName();
            AMainDS.AAccountHierarchyDetail.DefaultView.RowFilter =
                AAccountHierarchyDetailTable.GetAccountHierarchyCodeDBName() + " = '" + ADetailRow.AccountHierarchyCode + "' AND " +
                AAccountHierarchyDetailTable.GetAccountCodeToReportToDBName() + " = '" + ADetailRow.ReportingAccountCode + "'";

            foreach (DataRowView rowView in AMainDS.AAccountHierarchyDetail.DefaultView)
            {
                AAccountHierarchyDetailRow accountDetail = (AAccountHierarchyDetailRow)rowView.Row;
                InsertNodeIntoXmlDocument(AMainDS, ADoc, accountNode, accountDetail);
            }
        }

        /// <summary>
        /// return a simple XMLDocument (encoded into a string) with the account hierarchy and account details;
        /// root account can be calculated (find which account is reporting nowhere)
        /// </summary>
        /// <returns></returns>
        [RequireModulePermission("FINANCE-1")]
        public static bool ExportAccountHierarchy(Int32 ALedgerNumber, string AAccountHierarchyName, out string AHierarchyXml)
        {
            #region Validate Arguments

            if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }
            else if (AAccountHierarchyName.Length == 0)
            {
                throw new ArgumentException(String.Format(Catalog.GetString("Function:{0} - The Account Hierarchy name is empty!"),
                        Utilities.GetMethodName(true)));
            }

            #endregion Validate Arguments

            XmlDocument xmlDoc = TYml2Xml.CreateXmlDocument();

            GLSetupTDS MainDS = new GLSetupTDS();
            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("ExportAccountHierarchy");

            try
            {
                db.ReadTransaction(
                    ref Transaction,
                    delegate
                    {
                        AAccountHierarchyAccess.LoadViaALedger(MainDS, ALedgerNumber, Transaction);
                        AAccountHierarchyDetailAccess.LoadViaALedger(MainDS, ALedgerNumber, Transaction);
                        AAccountAccess.LoadViaALedger(MainDS, ALedgerNumber, Transaction);
                        AAccountPropertyAccess.LoadViaALedger(MainDS, ALedgerNumber, Transaction);
                    });

                AAccountHierarchyRow accountHierarchy = (AAccountHierarchyRow)MainDS.AAccountHierarchy.Rows.Find(new object[] { ALedgerNumber,
                                                                                                                                AAccountHierarchyName });

                if (accountHierarchy != null)
                {
                    // find the BALSHT account that is reporting to the root account
                    MainDS.AAccountHierarchyDetail.DefaultView.RowFilter =
                        AAccountHierarchyDetailTable.GetAccountHierarchyCodeDBName() + " = '" + AAccountHierarchyName + "' AND " +
                        AAccountHierarchyDetailTable.GetAccountCodeToReportToDBName() + " = '" + accountHierarchy.RootAccountCode + "'";


                    foreach (DataRowView vrow in MainDS.AAccountHierarchyDetail.DefaultView)
                    {
                        InsertNodeIntoXmlDocument(MainDS, xmlDoc, xmlDoc.DocumentElement,
                            (AAccountHierarchyDetailRow)vrow.Row);
                    }
                }
            }
            catch (Exception ex)
            {
                TLogging.LogException(ex, Utilities.GetMethodSignature());
                throw;
            }

            // XmlDocument is not serializable, therefore print it to string and return the string
            AHierarchyXml = TXMLParser.XmlToString(xmlDoc);

            return true;
        }

        /// export account hierarchy as yml string encoded in base64
        [RequireModulePermission("FINANCE-1")]
        public static bool ExportAccountHierarchyYml(Int32 ALedgerNumber, string AAccountHierarchyName, out string AHierarchyYml)
        {
            XmlDocument doc = new XmlDocument();

            string docstr;

            ExportAccountHierarchy(ALedgerNumber, AAccountHierarchyName, out docstr);

            doc.LoadXml(docstr);

            AHierarchyYml = THttpBinarySerializer.SerializeToBase64(TYml2Xml.Xml2Yml(doc));

            return true;
        }

        /// <summary>
        /// helper function for ExportCostCentreHierarchy
        /// </summary>
        private static void InsertNodeIntoXmlDocument(GLSetupTDS AMainDS,
            XmlDocument ADoc,
            XmlNode AParentNode,
            ACostCentreRow ADetailRow)
        {
            #region Validate Arguments

            if (AMainDS == null)
            {
                throw new EFinanceSystemDataObjectNullOrEmptyException(String.Format(Catalog.GetString("Function:{0} - The GL Setup dataset is null!"),
                        Utilities.GetMethodName(true)));
            }
            else if (AMainDS.ACostCentre == null)
            {
                throw new EFinanceSystemDataObjectNullOrEmptyException(String.Format(Catalog.GetString(
                            "Function:{0} - The Cost Centre table does not exist in the dataset!"),
                        Utilities.GetMethodName(true)));
            }
            else if (ADoc == null)
            {
                throw new ArgumentException(String.Format(Catalog.GetString("Function:{0} - The XML document is null!"),
                        Utilities.GetMethodName(true)));
            }
            else if (AParentNode == null)
            {
                throw new ArgumentException(String.Format(Catalog.GetString("Function:{0} - The XML parent node is null!"),
                        Utilities.GetMethodName(true)));
            }
            else if (ADetailRow == null)
            {
                throw new EFinanceSystemDataObjectNullOrEmptyException(String.Format(Catalog.GetString(
                            "Function:{0} - The Cost Centre detail row does not exist!"),
                        Utilities.GetMethodName(true)));
            }

            #endregion Validate Arguments

            XmlElement CostCentreNode = ADoc.CreateElement(TYml2Xml.XMLELEMENT);

            // CostCentreToReportTo is encoded implicitly
            CostCentreNode.SetAttribute("name", ADetailRow.CostCentreCode);
            CostCentreNode.SetAttribute("descr", ADetailRow.CostCentreName);
            CostCentreNode.SetAttribute("active", ADetailRow.CostCentreActiveFlag ? "True" : "False");
            CostCentreNode.SetAttribute("type", ADetailRow.CostCentreType.ToString());
            AParentNode.AppendChild(CostCentreNode);

            AMainDS.ACostCentre.DefaultView.Sort = ACostCentreTable.GetCostCentreCodeDBName();
            AMainDS.ACostCentre.DefaultView.RowFilter =
                ACostCentreTable.GetCostCentreToReportToDBName() + " = '" + ADetailRow.CostCentreCode + "'";

            foreach (DataRowView rowView in AMainDS.ACostCentre.DefaultView)
            {
                InsertNodeIntoXmlDocument(AMainDS, ADoc, CostCentreNode, (ACostCentreRow)rowView.Row);
            }
        }

        /// <summary>
        /// return a simple XMLDocument (encoded into a string) with the cost centre hierarchy and cost centre details;
        /// </summary>
        /// <param name="ALedgerNumber"></param>
        /// <returns></returns>
        [RequireModulePermission("FINANCE-1")]
        public static string ExportCostCentreHierarchy(Int32 ALedgerNumber)
        {
            #region Validate Arguments

            if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }

            #endregion Validate Arguments

            XmlDocument doc = TYml2Xml.CreateXmlDocument();

            GLSetupTDS MainDS = new GLSetupTDS();
            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("ExportCostCentreHierarchy");

            try
            {
                db.ReadTransaction(
                    ref Transaction,
                    delegate
                    {
                        ACostCentreAccess.LoadViaALedger(MainDS, ALedgerNumber, Transaction);
                    });

                MainDS.ACostCentre.DefaultView.RowFilter =
                    ACostCentreTable.GetCostCentreToReportToDBName() + " IS NULL";

                InsertNodeIntoXmlDocument(MainDS, doc, doc.DocumentElement,
                    (ACostCentreRow)MainDS.ACostCentre.DefaultView[0].Row);
            }
            catch (Exception ex)
            {
                TLogging.LogException(ex, Utilities.GetMethodSignature());
                throw;
            }

            db.CloseDBConnection();

            // XmlDocument is not serializable, therefore print it to string and return the string
            return TXMLParser.XmlToString(doc);
        }

        /// export cost centre hierarchy as yml string encoded in base64
        [RequireModulePermission("FINANCE-1")]
        public static bool ExportCostCentreHierarchyYml(Int32 ALedgerNumber, out string AHierarchyYml)
        {
            XmlDocument doc = new XmlDocument();

            string docstr = ExportCostCentreHierarchy(ALedgerNumber);

            doc.LoadXml(docstr);

            AHierarchyYml = THttpBinarySerializer.SerializeToBase64(TYml2Xml.Xml2Yml(doc));

            return true;
        }

        private static void CreateAccountHierarchyRecursively(ref GLSetupTDS AMainDS,
            Int32 ALedgerNumber,
            ref StringCollection AImportedAccountNames,
            XmlNode ACurrentNode,
            string AParentAccountCode,
            ref TVerificationResultCollection AVerificationResult)
        {
            #region Validate Arguments

            if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }
            else if (AMainDS == null)
            {
                throw new EFinanceSystemDataObjectNullOrEmptyException(String.Format(Catalog.GetString("Function:{0} - The GL Setup dataset is null!"),
                        Utilities.GetMethodName(true)));
            }
            else if (AMainDS.AAccount == null)
            {
                throw new EFinanceSystemDataObjectNullOrEmptyException(String.Format(Catalog.GetString(
                            "Function:{0} - The Account table does not exist in the dataset!"),
                        Utilities.GetMethodName(true)));
            }
            else if (ACurrentNode == null)
            {
                throw new ArgumentException(String.Format(Catalog.GetString("Function:{0} - The current XML node is null!"),
                        Utilities.GetMethodName(true)));
            }

            #endregion Validate Arguments

            AAccountRow NewAccount = null;

            string AccountCode = TYml2Xml.GetElementName(ACurrentNode).ToUpper();

            AImportedAccountNames.Add(AccountCode);

            // does this account already exist?
            bool NewRow = false;
            DataRow ExistingAccount = AMainDS.AAccount.Rows.Find(new object[] { ALedgerNumber, AccountCode });

            if (ExistingAccount != null)
            {
                NewAccount = (AAccountRow)ExistingAccount;
                DropAccountProperties(ref AMainDS, ALedgerNumber, AccountCode);
                ((GLSetupTDSAAccountRow)NewAccount).BankAccountFlag = false;
            }
            else
            {
                NewRow = true;
                NewAccount = AMainDS.AAccount.NewRowTyped();
            }

            NewAccount.LedgerNumber = AMainDS.AAccountHierarchy[0].LedgerNumber;
            NewAccount.AccountCode = AccountCode;
            NewAccount.AccountActiveFlag = TYml2Xml.GetAttributeRecursive(ACurrentNode, "active").ToLower() == "true";
            NewAccount.AccountType = TYml2Xml.GetAttributeRecursive(ACurrentNode, "type");
            NewAccount.DebitCreditIndicator = TYml2Xml.GetAttributeRecursive(ACurrentNode, "debitcredit") == "debit";
            NewAccount.ValidCcCombo = TYml2Xml.GetAttributeRecursive(ACurrentNode, "validcc");
            NewAccount.EngAccountCodeShortDesc = TYml2Xml.GetAttributeRecursive(ACurrentNode, "shortdesc");

            if (TXMLParser.HasAttribute(ACurrentNode, "shortdesc"))
            {
                NewAccount.EngAccountCodeLongDesc = TYml2Xml.GetAttribute(ACurrentNode, "longdesc");
                NewAccount.AccountCodeShortDesc = TYml2Xml.GetAttribute(ACurrentNode, "localdesc");
                NewAccount.AccountCodeLongDesc = TYml2Xml.GetAttribute(ACurrentNode, "locallongdesc");
            }
            else
            {
                NewAccount.EngAccountCodeLongDesc = TYml2Xml.GetAttributeRecursive(ACurrentNode, "longdesc");
                NewAccount.AccountCodeShortDesc = TYml2Xml.GetAttributeRecursive(ACurrentNode, "localdesc");
                NewAccount.AccountCodeLongDesc = TYml2Xml.GetAttributeRecursive(ACurrentNode, "locallongdesc");
            }

            if (NewAccount.EngAccountCodeLongDesc.Length == 0)
            {
                NewAccount.EngAccountCodeLongDesc = NewAccount.EngAccountCodeShortDesc;
            }

            if (NewAccount.AccountCodeShortDesc.Length == 0)
            {
                NewAccount.AccountCodeShortDesc = NewAccount.EngAccountCodeShortDesc;
            }

            if (NewAccount.AccountCodeLongDesc.Length == 0)
            {
                NewAccount.AccountCodeLongDesc = NewAccount.AccountCodeShortDesc;
            }

            if (NewRow)
            {
                AMainDS.AAccount.Rows.Add(NewAccount);
            }

            if (TYml2Xml.GetAttributeRecursive(ACurrentNode, "bankaccount") == "true")
            {
                AAccountPropertyRow accProp = AMainDS.AAccountProperty.NewRowTyped(true);
                accProp.LedgerNumber = NewAccount.LedgerNumber;
                accProp.AccountCode = NewAccount.AccountCode;
                accProp.PropertyCode = MFinanceConstants.ACCOUNT_PROPERTY_BANK_ACCOUNT;
                accProp.PropertyValue = "true";
                AMainDS.AAccountProperty.Rows.Add(accProp);
                ((GLSetupTDSAAccountRow)NewAccount).BankAccountFlag = true;
            }

            if (TYml2Xml.HasAttributeRecursive(ACurrentNode, "currency"))
            {
                string currency = TYml2Xml.GetAttributeRecursive(ACurrentNode, "currency");

                if (currency != AMainDS.ALedger[0].BaseCurrency)
                {
                    AMainDS.ACurrency.DefaultView.Sort = ACurrencyTable.GetCurrencyCodeDBName();
                    if (AMainDS.ACurrency.DefaultView.Find(currency) == -1)
                    {
                        AVerificationResult.Add(new TVerificationResult(
                            Catalog.GetString("Import hierarchy"),
                            "cannot find currency " + currency,
                            TResultSeverity.Resv_Critical));
                    }

                    NewAccount.ForeignCurrencyCode = currency;
                    NewAccount.ForeignCurrencyFlag = true;
                }
            }

            // account hierarchy has been deleted, so always add
            AAccountHierarchyDetailRow NewAccountHDetail = AMainDS.AAccountHierarchyDetail.NewRowTyped();
            NewAccountHDetail.LedgerNumber = AMainDS.AAccountHierarchy[0].LedgerNumber;
            NewAccountHDetail.AccountHierarchyCode = AMainDS.AAccountHierarchy[0].AccountHierarchyCode;
            NewAccountHDetail.AccountCodeToReportTo = AParentAccountCode;
            NewAccountHDetail.ReportingAccountCode = AccountCode;
            NewAccountHDetail.ReportOrder = AMainDS.AAccountHierarchyDetail.Rows.Count;

            AMainDS.AAccountHierarchyDetail.Rows.Add(NewAccountHDetail);

            NewAccount.PostingStatus = !ACurrentNode.HasChildNodes;

            foreach (XmlNode child in ACurrentNode.ChildNodes)
            {
                CreateAccountHierarchyRecursively(ref AMainDS, ALedgerNumber, ref AImportedAccountNames, child, NewAccount.AccountCode, ref AVerificationResult);
            }
        }

        /// <summary>
        /// only works if there are no balances/transactions yet for the accounts that are deleted
        /// </summary>
        [RequireModulePermission("FINANCE-3")]
        public static bool ImportAccountHierarchy(Int32 ALedgerNumber, string AHierarchyName, string AYmlAccountHierarchy, out TVerificationResultCollection AVerificationResult)
        {
            AVerificationResult = new TVerificationResultCollection();

            #region Validate Arguments

            if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }
            else if (AHierarchyName.Length == 0)
            {
                throw new ArgumentException(String.Format(Catalog.GetString("Function:{0} - The Hierarchy name is empty!"),
                        Utilities.GetMethodName(true)));
            }
            else if (AYmlAccountHierarchy.Length == 0)
            {
                throw new ArgumentException(String.Format(Catalog.GetString("Function:{0} - The Account Hierarchy YML is empty!"),
                        Utilities.GetMethodName(true)));
            }

            #endregion Validate Arguments

            XmlDocument XMLDoc = new XmlDocument();

            if (AYmlAccountHierarchy.StartsWith("<?xml version="))
            {
                XMLDoc.LoadXml(AYmlAccountHierarchy);
            }
            else
            {
                try
                {
                    TYml2Xml ymlParser = new TYml2Xml(AYmlAccountHierarchy.Split(new char[] { '\n' }));
                    XMLDoc = ymlParser.ParseYML2XML();
                }
                catch (Exception exp)
                {
                    TLogging.Log(exp.ToString());
                    AVerificationResult.Add(new TVerificationResult(
                        Catalog.GetString("Import hierarchy"),
                        "base64" + THttpBinarySerializer.SerializeToBase64(
                            Catalog.GetString("There was a problem with the syntax of the file: ") +
                            Environment.NewLine +
                            exp.Message),
                        TResultSeverity.Resv_Critical));
                    return false;
                }
            }

            GLSetupTDS MainDS = LoadAccountHierarchies(ALedgerNumber);
            XmlNode Root = XMLDoc.FirstChild.NextSibling.FirstChild;

            StringCollection ImportedAccountNames = new StringCollection();

            ImportedAccountNames.Add(ALedgerNumber.ToString());

            // delete all account hierarchy details of this hierarchy
            foreach (AAccountHierarchyDetailRow accounthdetail in MainDS.AAccountHierarchyDetail.Rows)
            {
                if (accounthdetail.AccountHierarchyCode == AHierarchyName)
                {
                    accounthdetail.Delete();
                }
            }

            while (Root != null)
            {
                CreateAccountHierarchyRecursively(ref MainDS, ALedgerNumber, ref ImportedAccountNames, Root, ALedgerNumber.ToString(), ref AVerificationResult);
                Root = Root.NextSibling;
            }

            foreach (AAccountRow accountRow in MainDS.AAccount.Rows)
            {
                if ((accountRow.RowState != DataRowState.Deleted) && !ImportedAccountNames.Contains(accountRow.AccountCode))
                {
                    // if there are any existing posted transactions that reference this account, it can't be deleted.
                    ATransactionTable transTbl = null;
                    AMotivationDetailTable motivationDetailTbl = null;

                    TDBTransaction transaction = new TDBTransaction();
                    TDataBase db = DBAccess.Connect("ImportAccountHierarchy");                    
                    db.ReadTransaction(
                        ref transaction,
                        delegate
                        {
                            // if there are any existing posted transactions that reference this account, it can't be deleted.
                            transTbl = ATransactionAccess.LoadViaAAccount(ALedgerNumber, accountRow.AccountCode, transaction);

                            // if this account is referenced by a Motivation Detail, then it can't be deleted
                            motivationDetailTbl = AMotivationDetailAccess.LoadViaAAccountAccountCode(ALedgerNumber, accountRow.AccountCode, transaction);
                        });

                    if ((motivationDetailTbl.Rows.Count == 0) && (transTbl.Rows.Count == 0)) // No-one's used this account, so I can delete it.
                    {
                        // remove transaction types if they reference the account
                        foreach (ATransactionTypeRow Row in MainDS.ATransactionType.Rows)
                        {
                            if ((Row.RowState != DataRowState.Deleted) && (Row.LedgerNumber == ALedgerNumber) && (Row.DebitAccountCode == accountRow.AccountCode || Row.CreditAccountCode == accountRow.AccountCode))
                            {
                                Row.Delete();
                            }
                        }

                        // If the deleted account included Analysis types I need to unlink them from the Account first.
                        foreach (AAnalysisAttributeRow Row in MainDS.AAnalysisAttribute.Rows)
                        {
                            if ((Row.LedgerNumber == ALedgerNumber) && (Row.AccountCode == accountRow.AccountCode))
                            {
                                Row.Delete();
                            }
                        }

                        // remove fees receivable if they reference the account
                        foreach (AFeesReceivableRow Row in MainDS.AFeesReceivable.Rows)
                        {
                            if ((Row.RowState != DataRowState.Deleted) && (Row.LedgerNumber == ALedgerNumber) && (Row.AccountCode == accountRow.AccountCode))
                            {
                                Row.Delete();
                            }
                        }

                        // remove fees payable if they reference the account
                        foreach (AFeesPayableRow Row in MainDS.AFeesPayable.Rows)
                        {
                            if ((Row.RowState != DataRowState.Deleted) && (Row.LedgerNumber == ALedgerNumber) && (Row.AccountCode == accountRow.AccountCode))
                            {
                                Row.Delete();
                            }
                        }

                        accountRow.Delete();
                    }
                    else
                    {
                        if (motivationDetailTbl.Rows.Count > 0)
                        {
                            foreach (AMotivationDetailRow row in motivationDetailTbl.Rows)
                            {
                                string ErrorMsg = String.Format(Catalog.GetString("The motivation detail {0}/{1} references account {2} which should be deleted"), row.MotivationGroupCode, row.MotivationDetailCode, accountRow.AccountCode);
                                AVerificationResult.Add(new TVerificationResult(Catalog.GetString("Import hierarchy"), ErrorMsg, TResultSeverity.Resv_Critical));
                            }
                        }

                        if (transTbl.Rows.Count > 0)
                        {
                            string ErrorMsg = String.Format(Catalog.GetString("There is a balance on account {0}"), accountRow.AccountCode);
                            AVerificationResult.Add(new TVerificationResult(Catalog.GetString("Import hierarchy"), ErrorMsg, TResultSeverity.Resv_Critical));
                        }
                    }

                    db.CloseDBConnection();
                }
            }

            if (AVerificationResult.HasCriticalErrors)
            {
                return false;
            }

            return SaveGLSetupTDS(ALedgerNumber, ref MainDS, out AVerificationResult) == TSubmitChangesResult.scrOK;
        }

        private static bool CreateCostCentresRecursively(ref GLSetupTDS AMainDS,
            Int32 ALedgerNumber,
            ref StringCollection AImportedCostCentreCodes,
            XmlNode ACurrentNode,
            string AParentCostCentreCode,
            out TVerificationResultCollection AVerificationResult)
        {
            ACostCentreRow newCostCentre = null;
            AVerificationResult = new TVerificationResultCollection();

            string CostCentreCode = TYml2Xml.GetElementName(ACurrentNode).ToUpper();

            AImportedCostCentreCodes.Add(CostCentreCode);

            // does this costcentre already exist?
            bool newRow = false;
            DataRow existingCostCentre = AMainDS.ACostCentre.Rows.Find(new object[] { ALedgerNumber, CostCentreCode });

            if (existingCostCentre != null)
            {
                newCostCentre = (ACostCentreRow)existingCostCentre;
            }
            else if ((AParentCostCentreCode == null) && (AMainDS.ACostCentre.Rows.Count > 0))
            {
                AVerificationResult.Add(new TVerificationResult("Import CostCentres",
                    "Root Cost Centre " + CostCentreCode + " does not match existing cost centre", TResultSeverity.Resv_Critical));
                return false;
            }
            else
            {
                newRow = true;
                newCostCentre = AMainDS.ACostCentre.NewRowTyped();
            }

            newCostCentre.LedgerNumber = ALedgerNumber;
            newCostCentre.CostCentreCode = CostCentreCode;
            newCostCentre.CostCentreName = TYml2Xml.GetAttribute(ACurrentNode, "descr");
            newCostCentre.CostCentreActiveFlag = TYml2Xml.GetAttributeRecursive(ACurrentNode, "active").ToLower() == "true";
            newCostCentre.SystemCostCentreFlag = true;
            newCostCentre.CostCentreType = TYml2Xml.GetAttributeRecursive(ACurrentNode, "type");
            newCostCentre.PostingCostCentreFlag = (ACurrentNode.ChildNodes.Count == 0);

            if ((AParentCostCentreCode != null) && (AParentCostCentreCode.Length != 0))
            {
                newCostCentre.CostCentreToReportTo = AParentCostCentreCode;
            }

            if (newRow)
            {
                AMainDS.ACostCentre.Rows.Add(newCostCentre);
            }

            foreach (XmlNode child in ACurrentNode.ChildNodes)
            {
                if (!CreateCostCentresRecursively(ref AMainDS, ALedgerNumber, ref AImportedCostCentreCodes, child, newCostCentre.CostCentreCode, out AVerificationResult))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// only works if there are no balances/transactions yet for the cost centres that are deleted.
        /// Returns false with helpful error message otherwise.
        /// </summary>
        [RequireModulePermission("FINANCE-3")]
        public static bool ImportCostCentreHierarchy(Int32 ALedgerNumber, string AYmlHierarchy, out TVerificationResultCollection VerificationResult)
        {
            VerificationResult = new TVerificationResultCollection();

            XmlDocument XMLDoc = new XmlDocument();

            if (AYmlHierarchy.StartsWith("<?xml version="))
            {
                XMLDoc.LoadXml(AYmlHierarchy);
            }
            else
            {
                try
                {
                    TYml2Xml ymlParser = new TYml2Xml(AYmlHierarchy.Split(new char[] { '\n' }));
                    XMLDoc = ymlParser.ParseYML2XML();
                }
                catch (XmlException exp)
                {
                    TLogging.Log(exp.ToString());
                    VerificationResult.Add(new TVerificationResult("Import CostCentres",
                        Catalog.GetString("There was a problem with the syntax of the file.") +
                        Environment.NewLine +
                        exp.Message, TResultSeverity.Resv_Critical));
                    return false;
                }
            }

            GLSetupTDS MainDS = LoadCostCentreHierarchy(ALedgerNumber);
            XmlNode root = XMLDoc.FirstChild.NextSibling.FirstChild;

            StringCollection ImportedCostCentreNames = new StringCollection();

            if (!CreateCostCentresRecursively(ref MainDS, ALedgerNumber, ref ImportedCostCentreNames, root, null, out VerificationResult))
            {
                return false;
            }

            foreach (ACostCentreRow costCentreRow in MainDS.ACostCentre.Rows)
            {
                if ((costCentreRow.RowState != DataRowState.Deleted) && !ImportedCostCentreNames.Contains(costCentreRow.CostCentreCode))
                {
                    // delete costcentres that don't exist anymore in the new hierarchy.
                    // (check if their balance is empty and no transactions exist, or catch database constraint violation)
                    bool CanBeParent;
                    bool CanDelete;
                    String ErrorMsg;
                    GetCostCentreAttributes(ALedgerNumber,
                        costCentreRow.CostCentreCode,
                        out CanBeParent,
                        out CanDelete,
                        out ErrorMsg);

                    if (!CanDelete)
                    {
                        TLogging.Log("cannot delete " + costCentreRow.CostCentreCode + " " + ErrorMsg);
                        costCentreRow.CostCentreActiveFlag = false;
                    }
                    else
                    {
                        costCentreRow.Delete();
                    }
                }
            }

            return SaveGLSetupTDS(ALedgerNumber, ref MainDS, out VerificationResult) == TSubmitChangesResult.scrOK;
        }

        /// import a new Account hierarchy into an empty new ledger
        private static void ImportDefaultAccountHierarchy(ref GLSetupTDS AMainDS, Int32 ALedgerNumber, ref TVerificationResultCollection AVerificationResult)
        {
            XmlDocument doc;
            TYml2Xml ymlFile;
            string Filename = TAppSettingsManager.GetValue("SqlFiles.Path", ".") +
                              Path.DirectorySeparatorChar +
                              "DefaultAccountHierarchy.yml";

            try
            {
                ymlFile = new TYml2Xml(Filename);
                doc = ymlFile.ParseYML2XML();
            }
            catch (XmlException exp)
            {
                throw new Exception(
                    Catalog.GetString("There was a problem with the syntax of the file.") +
                    Environment.NewLine +
                    exp.Message +
                    Environment.NewLine +
                    Filename);
            }

            // create the root account
            AAccountHierarchyRow accountHierarchyRow = AMainDS.AAccountHierarchy.NewRowTyped();
            accountHierarchyRow.LedgerNumber = ALedgerNumber;
            accountHierarchyRow.AccountHierarchyCode = "STANDARD";
            accountHierarchyRow.RootAccountCode = ALedgerNumber.ToString();
            AMainDS.AAccountHierarchy.Rows.Add(accountHierarchyRow);

            AAccountRow accountRow = AMainDS.AAccount.NewRowTyped();
            accountRow.LedgerNumber = ALedgerNumber;
            accountRow.AccountCode = ALedgerNumber.ToString();
            accountRow.PostingStatus = false;
            AMainDS.AAccount.Rows.Add(accountRow);

            XmlNode root = doc.FirstChild.NextSibling.FirstChild;

            StringCollection ImportedAccountNames = new StringCollection();

            CreateAccountHierarchyRecursively(ref AMainDS, ALedgerNumber, ref ImportedAccountNames, root, ALedgerNumber.ToString(), ref AVerificationResult);
        }

        private static void ImportDefaultCostCentreHierarchy(ref GLSetupTDS AMainDS, Int32 ALedgerNumber, string ALedgerName)
        {
            if (ALedgerName.Length == 0)
            {
                throw new Exception("We need a name for the ledger, otherwise the yml will be invalid");
            }

            // load XmlCostCentreHierarchy from a default file

            string Filename = TAppSettingsManager.GetValue("SqlFiles.Path", ".") +
                              Path.DirectorySeparatorChar +
                              "DefaultCostCentreHierarchy.yml";
            TextReader reader = new StreamReader(Filename, TTextFile.GetFileEncoding(Filename), false);
            string XmlCostCentreHierarchy = reader.ReadToEnd();

            reader.Close();

            XmlCostCentreHierarchy = XmlCostCentreHierarchy.Replace("{#LEDGERNUMBER}", ALedgerNumber.ToString());
            XmlCostCentreHierarchy = XmlCostCentreHierarchy.Replace("{#LEDGERNUMBERWITHLEADINGZEROS}", ALedgerNumber.ToString("00"));

            XmlCostCentreHierarchy = XmlCostCentreHierarchy.Replace("{#LEDGERNAME}", ALedgerName);

            string[] lines = XmlCostCentreHierarchy.Replace("\r", "").Split(new char[] { '\n' });
            TYml2Xml ymlFile = new TYml2Xml(lines);
            XmlDocument doc = ymlFile.ParseYML2XML();

            XmlNode root = doc.FirstChild.NextSibling.FirstChild;

            StringCollection ImportedCostCentreNames = new StringCollection();

            TVerificationResultCollection VerificationResult;
            CreateCostCentresRecursively(ref AMainDS, ALedgerNumber, ref ImportedCostCentreNames, root, null, out VerificationResult);
        }

        private static void SetupILTCostCentreHierarchy(ref GLSetupTDS AMainDS, Int32 ALedgerNumber, TDBTransaction ATransaction)
        {
            // For easier readability
            // SELECT p.p_partner_key_n, p.p_partner_short_name_c
            // FROM p_partner p JOIN p_partner_type t
            // ON p.p_partner_key_n=t.p_partner_key_n
            // WHERE t.p_type_code_c='LEDGER' AND p_partner_key_n<>the-ledger-number-multiplied-by-1000000
            // ORDER BY p.p_partner_key_n

            string SqlQuery = string.Format(
                "SELECT p.{0}, p.{1} FROM {2} p JOIN {3} t ON p.{0}=t.{4} WHERE t.{5}='LEDGER' AND p.{0}<>{6} ORDER BY p.{0}",
                PPartnerTable.GetPartnerKeyDBName(),
                PPartnerTable.GetPartnerShortNameDBName(),
                PPartnerTable.GetTableDBName(),
                PPartnerTypeTable.GetTableDBName(),
                PPartnerTypeTable.GetPartnerKeyDBName(),
                PPartnerTypeTable.GetTypeCodeDBName(),
                ALedgerNumber * 1000000);
            DataTable t = ATransaction.DataBaseObj.SelectDT(SqlQuery, "ILTCostCentres", ATransaction);

            foreach (DataRow row in t.Rows)
            {
                Int64 partnerKey = Convert.ToInt64(row[0]);
                string costCentreCode = string.Format("{0:0000}", partnerKey / 10000);

                ACostCentreRow ccRow = AMainDS.ACostCentre.NewRowTyped(true);
                ccRow.LedgerNumber = ALedgerNumber;
                ccRow.CostCentreCode = costCentreCode;
                ccRow.CostCentreName = Convert.ToString(row[1]);
                ccRow.CostCentreToReportTo = MFinanceConstants.INTER_LEDGER_HEADING;
                ccRow.CostCentreType = "Foreign";
                ccRow.SystemCostCentreFlag = true;
                AMainDS.ACostCentre.Rows.Add(ccRow);

                AValidLedgerNumberRow vlRow = AMainDS.AValidLedgerNumber.NewRowTyped();
                vlRow.LedgerNumber = ALedgerNumber;
                vlRow.PartnerKey = partnerKey;
                vlRow.IltProcessingCentre = 4000000;
                vlRow.CostCentreCode = costCentreCode;
                AMainDS.AValidLedgerNumber.Rows.Add(vlRow);
            }
        }

        private static void CreateMotivationDetailFee(ref GLSetupTDS AMainDS,
            Int32 ALedgerNumber,
            XmlNode ACurrentNode,
            string AMotivationGroupCode,
            string AMotivationDetailCode)
        {
            AMotivationDetailFeeRow newMotivationDetailFee = null;

            string MotivationDetailFeeCode = TYml2Xml.GetElementName(ACurrentNode).ToUpper();

            // does this motivation detail fee already exist?
            DataRow existingMotivationDetailFee =
                AMainDS.AMotivationDetailFee.Rows.Find(new object[] { ALedgerNumber, AMotivationGroupCode, AMotivationDetailCode,
                                                                      MotivationDetailFeeCode });

            if (existingMotivationDetailFee == null)
            {
                newMotivationDetailFee = AMainDS.AMotivationDetailFee.NewRowTyped();
                newMotivationDetailFee.LedgerNumber = ALedgerNumber;
                newMotivationDetailFee.MotivationGroupCode = AMotivationGroupCode;
                newMotivationDetailFee.MotivationDetailCode = AMotivationDetailCode;
                newMotivationDetailFee.FeeCode = MotivationDetailFeeCode;
                AMainDS.AMotivationDetailFee.Rows.Add(newMotivationDetailFee);
            }
        }

        private static void CreateMotivationDetail(ref GLSetupTDS AMainDS,
            Int32 ALedgerNumber,
            XmlNode ACurrentNode,
            string AMotivationGroupCode,
            TDataBase ADataBase)
        {
            AMotivationDetailRow newMotivationDetail = null;

            string MotivationDetailCode = TYml2Xml.GetElementName(ACurrentNode).ToUpper();

            // does this motivation already exist?
            bool newRow = false;
            DataRow existingMotivationDetail = AMainDS.AMotivationDetail.Rows.Find(new object[] { ALedgerNumber, AMotivationGroupCode,
                                                                                                  MotivationDetailCode });

            if (existingMotivationDetail != null)
            {
                newMotivationDetail = (AMotivationDetailRow)existingMotivationDetail;
            }
            else
            {
                newRow = true;
                newMotivationDetail = AMainDS.AMotivationDetail.NewRowTyped();
            }

            newMotivationDetail.LedgerNumber = ALedgerNumber;
            newMotivationDetail.MotivationGroupCode = AMotivationGroupCode;
            newMotivationDetail.MotivationDetailCode = MotivationDetailCode;

            if (TYml2Xml.HasAttribute(ACurrentNode, "accountcode"))
            {
                newMotivationDetail.AccountCode = TYml2Xml.GetAttribute(ACurrentNode, "accountcode");
            }

            newMotivationDetail.CostCentreCode = TLedgerInfo.GetStandardCostCentre(ALedgerNumber);

            if (TYml2Xml.HasAttribute(ACurrentNode, "description"))
            {
                newMotivationDetail.MotivationDetailDesc = TYml2Xml.GetAttribute(ACurrentNode, "description");
                newMotivationDetail.MotivationDetailDescLocal = newMotivationDetail.MotivationDetailDesc;
            }

            if (newRow)
            {
                AMainDS.AMotivationDetail.Rows.Add(newMotivationDetail);
            }

            foreach (XmlNode child in ACurrentNode.ChildNodes)
            {
                CreateMotivationDetailFee(ref AMainDS, ALedgerNumber, child, newMotivationDetail.MotivationGroupCode,
                    newMotivationDetail.MotivationDetailCode);
            }
        }

        private static void CreateMotivationGroup(ref GLSetupTDS AMainDS,
            Int32 ALedgerNumber,
            XmlNode ACurrentNode,
            TDataBase ADataBase)
        {
            AMotivationGroupRow newMotivationGroup = null;

            string MotivationGroupCode = TYml2Xml.GetElementName(ACurrentNode).ToUpper();

            // does this motivation already exist?
            bool newRow = false;
            DataRow existingMotivationGroup = AMainDS.AMotivationGroup.Rows.Find(new object[] { ALedgerNumber, MotivationGroupCode });

            if (existingMotivationGroup != null)
            {
                newMotivationGroup = (AMotivationGroupRow)existingMotivationGroup;
            }
            else
            {
                newRow = true;
                newMotivationGroup = AMainDS.AMotivationGroup.NewRowTyped();
            }

            newMotivationGroup.LedgerNumber = ALedgerNumber;
            newMotivationGroup.MotivationGroupCode = MotivationGroupCode;

            if (TYml2Xml.HasAttribute(ACurrentNode, "desclocal"))
            {
                newMotivationGroup.MotivationGroupDescLocal = TYml2Xml.GetAttribute(ACurrentNode, "desclocal");
            }

            if (TYml2Xml.HasAttribute(ACurrentNode, "description"))
            {
                newMotivationGroup.MotivationGroupDescription = TYml2Xml.GetAttribute(ACurrentNode, "description");
            }

            if (newMotivationGroup.MotivationGroupDescription.Length == 0)
            {
                newMotivationGroup.MotivationGroupDescription = newMotivationGroup.MotivationGroupDescLocal;
            }

            if (newRow)
            {
                AMainDS.AMotivationGroup.Rows.Add(newMotivationGroup);
            }

            foreach (XmlNode child in ACurrentNode.ChildNodes)
            {
                CreateMotivationDetail(ref AMainDS, ALedgerNumber, child, newMotivationGroup.MotivationGroupCode, ADataBase);
            }
        }

        /// import motivation groups, details into an empty new ledger
        private static void ImportDefaultMotivations(ref GLSetupTDS AMainDS, Int32 ALedgerNumber, TDataBase ADataBase)
        {
            XmlDocument doc;
            TYml2Xml ymlFile;
            string Filename = TAppSettingsManager.GetValue("SqlFiles.Path", ".") +
                              Path.DirectorySeparatorChar +
                              "DefaultMotivations.yml";

            try
            {
                ymlFile = new TYml2Xml(Filename);
                doc = ymlFile.ParseYML2XML();
            }
            catch (XmlException exp)
            {
                throw new Exception(
                    Catalog.GetString("There was a problem with the syntax of the file.") +
                    Environment.NewLine +
                    exp.Message +
                    Environment.NewLine +
                    Filename);
            }

            XmlNode root = doc.FirstChild.NextSibling;

            foreach (XmlNode child in root)
            {
                CreateMotivationGroup(ref AMainDS, ALedgerNumber, child, ADataBase);
            }
        }

        /// import records for fees payable or receivable into an empty new ledger
        private static void ImportDefaultAdminGrantsPayableReceivable(ref GLSetupTDS AMainDS, Int32 ALedgerNumber)
        {
            AFeesPayableRow newFeesPayableRow = null;
            AFeesReceivableRow newFeesReceivableRow = null;
            bool newRow;
            DataRow existingRow;

            bool IsFeesPayable;
            string FeeCode;

            XmlDocument doc;
            TYml2Xml ymlFile;
            string Filename = TAppSettingsManager.GetValue("SqlFiles.Path", ".") +
                              Path.DirectorySeparatorChar +
                              "DefaultAdminGrantsPayableReceivable.yml";

            try
            {
                ymlFile = new TYml2Xml(Filename);
                doc = ymlFile.ParseYML2XML();
            }
            catch (XmlException exp)
            {
                throw new Exception(
                    Catalog.GetString("There was a problem with the syntax of the file.") +
                    Environment.NewLine +
                    exp.Message +
                    Environment.NewLine +
                    Filename);
            }

            XmlNode root = doc.FirstChild.NextSibling;

            foreach (XmlNode child in root)
            {
                FeeCode = TYml2Xml.GetElementName(child).ToUpper();

                IsFeesPayable = (TYml2Xml.GetAttribute(child, "feespayable") == "yes"
                                 || TYml2Xml.GetAttribute(child, "feespayable") == "true");

                if (IsFeesPayable)
                {
                    // does this fee already exist?
                    newRow = false;
                    existingRow = AMainDS.AFeesPayable.Rows.Find(new object[] { ALedgerNumber, FeeCode });

                    if (existingRow != null)
                    {
                        newFeesPayableRow = (AFeesPayableRow)existingRow;
                    }
                    else
                    {
                        newRow = true;
                        newFeesPayableRow = AMainDS.AFeesPayable.NewRowTyped();
                    }

                    newFeesPayableRow.LedgerNumber = ALedgerNumber;
                    newFeesPayableRow.FeeCode = FeeCode;
                    newFeesPayableRow.ChargeOption = TYml2Xml.GetAttribute(child, "chargeoption");

                    if (TYml2Xml.HasAttribute(child, "percentage"))
                    {
                        newFeesPayableRow.ChargePercentage = Convert.ToInt32(TYml2Xml.GetAttribute(child, "percentage"));
                    }

                    newFeesPayableRow.CostCentreCode = TYml2Xml.GetAttribute(child, "costcentrecode");
                    newFeesPayableRow.AccountCode = TYml2Xml.GetAttribute(child, "accountcode");
                    newFeesPayableRow.DrAccountCode = TYml2Xml.GetAttribute(child, "draccountcode");

                    if (TYml2Xml.HasAttribute(child, "description"))
                    {
                        newFeesPayableRow.FeeDescription = TYml2Xml.GetAttribute(child, "description");
                    }

                    if (newRow)
                    {
                        AMainDS.AFeesPayable.Rows.Add(newFeesPayableRow);
                    }
                }
                else
                {
                    // does this fee already exist?
                    newRow = false;
                    existingRow = AMainDS.AFeesReceivable.Rows.Find(new object[] { ALedgerNumber, FeeCode });

                    if (existingRow != null)
                    {
                        newFeesReceivableRow = (AFeesReceivableRow)existingRow;
                    }
                    else
                    {
                        newRow = true;
                        newFeesReceivableRow = AMainDS.AFeesReceivable.NewRowTyped();
                    }

                    newFeesReceivableRow.LedgerNumber = ALedgerNumber;
                    newFeesReceivableRow.FeeCode = FeeCode;
                    newFeesReceivableRow.ChargeOption = TYml2Xml.GetAttribute(child, "chargeoption");

                    if (TYml2Xml.HasAttribute(child, "percentage"))
                    {
                        newFeesReceivableRow.ChargePercentage = Convert.ToInt32(TYml2Xml.GetAttribute(child, "percentage"));
                    }

                    newFeesReceivableRow.CostCentreCode = TYml2Xml.GetAttribute(child, "costcentrecode");
                    newFeesReceivableRow.AccountCode = TYml2Xml.GetAttribute(child, "accountcode");
                    newFeesReceivableRow.DrAccountCode = TYml2Xml.GetAttribute(child, "draccountcode");

                    if (TYml2Xml.HasAttribute(child, "description"))
                    {
                        newFeesReceivableRow.FeeDescription = TYml2Xml.GetAttribute(child, "description");
                    }

                    if (newRow)
                    {
                        AMainDS.AFeesReceivable.Rows.Add(newFeesReceivableRow);
                    }
                }
            }
        }

        /// <summary>
        /// On creation of a new Ledger, if the user requested ICH Account is Asset,
        /// this does the rewire.
        /// But if there was no ICH in the newly created Hierarchy, it doesn't panic.
        /// </summary>
        private static void RewireIchIsAsset(Int32 ANewLedgerNumber)
        {
            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("RewireIchIsAsset");
            bool SubmissionOK = false;

            db.WriteTransaction(ref Transaction, ref SubmissionOK,
                delegate
                {
                    AAccountTable AccountTbl = AAccountAccess.LoadByPrimaryKey(ANewLedgerNumber, "8500", Transaction);
                    AAccountRow IchAccountRow = null;

                    if (AccountTbl.Rows.Count > 0)
                    {
                        IchAccountRow = AccountTbl[0];
                        IchAccountRow.AccountType = "Asset";
                        IchAccountRow.DebitCreditIndicator = true;
                        AAccountAccess.SubmitChanges(AccountTbl, Transaction);
                    }

                    //
                    // If there's an 8500X account, that also needs to be re-tweaked:
                    AccountTbl = AAccountAccess.LoadByPrimaryKey(ANewLedgerNumber, "8500X", Transaction);

                    if (AccountTbl.Rows.Count > 0)
                    {
                        IchAccountRow = AccountTbl[0];
                        IchAccountRow.AccountType = "Asset";
                        IchAccountRow.DebitCreditIndicator = true;
                        AAccountAccess.SubmitChanges(AccountTbl, Transaction);
                    }

                    //
                    // The Summary account also needs to be re-tweaked:
                    AccountTbl = AAccountAccess.LoadByPrimaryKey(ANewLedgerNumber, "8500S", Transaction);

                    if (AccountTbl.Rows.Count > 0)
                    {
                        IchAccountRow = AccountTbl[0];
                        IchAccountRow.AccountType = "Asset";
                        IchAccountRow.DebitCreditIndicator = true;
                        AAccountAccess.SubmitChanges(AccountTbl, Transaction);
                    }

                    //
                    // ICH ("8500S") normally reports to "CRS". I need it to report to "DRS" instead:
                    AAccountHierarchyDetailTable HierarchyTbl = AAccountHierarchyDetailAccess.LoadByPrimaryKey(
                        ANewLedgerNumber, "STANDARD", "8500S", Transaction);

                    if (HierarchyTbl.Rows.Count > 0)
                    {
                        AAccountHierarchyDetailRow HierarchyRow = HierarchyTbl[0];
                        HierarchyRow.AccountCodeToReportTo = "DRS";
                        AAccountHierarchyDetailAccess.SubmitChanges(HierarchyTbl, Transaction);
                    }

                    SubmissionOK = true;
                });

            db.CloseDBConnection();
        }

        /// Is this user a finance user, but there is no ledger yet?
        [NoRemoting]
        public static string GetLedgerSetupAssistant()
        {
            TDBTransaction t = new TDBTransaction();
            TDataBase db = DBAccess.Connect("GetLedgerSetupAssistant");

            string result = String.Empty;
            string sql = "SELECT COUNT(*) FROM PUB_s_user_module_access_permission p1 " +
                "WHERE p1.s_module_id_c = 'FINANCE-3' AND p1.s_can_access_l = true " +
                "AND p1.s_user_id_c = '" + UserInfo.GetUserInfo().UserID + "' " +
                "AND NOT EXISTS (SELECT * FROM PUB_a_ledger)";

            db.ReadTransaction(ref t,
                delegate
                {
                    if (Convert.ToInt32(db.ExecuteScalar(sql, t)) > 0)
                    {
                        result = "CrossLedgerSetup/LedgerSetup";
                    }
                });

            db.CloseDBConnection();

            return result;
        }


        /// create a site so that self sign up works even without a ledger
        [NoRemoting]
        public static bool CreateSite(ref GLSetupTDS AMainDS, string ALedgerName, Int64 ASiteKey, TDBTransaction ATransaction)
        {
            PPartnerRow partnerRow;

            if (!PPartnerAccess.Exists(ASiteKey, ATransaction))
            {
                partnerRow = AMainDS.PPartner.NewRowTyped();
                partnerRow.PartnerKey = ASiteKey;
                partnerRow.PartnerShortName = ALedgerName;
                partnerRow.StatusCode = MPartnerConstants.PARTNERSTATUS_ACTIVE;
                partnerRow.PartnerClass = MPartnerConstants.PARTNERCLASS_UNIT;
                AMainDS.PPartner.Rows.Add(partnerRow);

                // create or use addresses (only if partner record is created here as
                // otherwise we assume that Partner has address already)
                PLocationRow locationRow;
                PLocationTable LocTemplateTable;
                PLocationTable LocResultTable;
                PLocationRow LocTemplateRow;
                StringCollection LocTemplateOperators;

                // find address with country set
                LocTemplateTable = new PLocationTable();
                LocTemplateRow = LocTemplateTable.NewRowTyped(false);
                LocTemplateRow.SiteKey = 0;
                LocTemplateRow.StreetName = Catalog.GetString("No valid address on file");
                LocTemplateRow.CountryCode = "99";
                LocTemplateOperators = new StringCollection();

                LocResultTable = PLocationAccess.LoadUsingTemplate(LocTemplateRow, LocTemplateOperators, ATransaction);

                if (LocResultTable.Count > 0)
                {
                    locationRow = (PLocationRow)LocResultTable.Rows[0];
                }
                else
                {
                    // no location record exists yet: create new one
                    locationRow = AMainDS.PLocation.NewRowTyped();
                    locationRow.SiteKey = 0;
                    locationRow.LocationKey = (int)ATransaction.DataBaseObj.GetNextSequenceValue(
                        TSequenceNames.seq_location_number.ToString(), ATransaction);
                    locationRow.StreetName = Catalog.GetString("No valid address on file");
                    locationRow.CountryCode = "99";
                    AMainDS.PLocation.Rows.Add(locationRow);
                }

                // now create partner location record
                PPartnerLocationRow partnerLocationRow = AMainDS.PPartnerLocation.NewRowTyped();
                partnerLocationRow.SiteKey = locationRow.SiteKey;
                partnerLocationRow.PartnerKey = ASiteKey;
                partnerLocationRow.LocationKey = locationRow.LocationKey;
                partnerLocationRow.DateEffective = DateTime.Today;
                AMainDS.PPartnerLocation.Rows.Add(partnerLocationRow);
            }
            else
            {
                // partner record already exists in database -> update ledger name
                PPartnerAccess.LoadByPrimaryKey(AMainDS, ASiteKey, ATransaction);
                partnerRow = (PPartnerRow)AMainDS.PPartner.Rows[0];
                partnerRow.PartnerShortName = ALedgerName;
            }

            PPartnerLedgerRow partnerledger;

            if (!PPartnerLedgerAccess.Exists(ASiteKey, ATransaction))
            {
                partnerledger = AMainDS.PPartnerLedger.NewRowTyped();
                partnerledger.PartnerKey = ASiteKey;
                partnerledger.LastPartnerId = 5000;
                AMainDS.PPartnerLedger.Rows.Add(partnerledger);
            }

            if (!PUnitAccess.Exists(ASiteKey, ATransaction))
            {
                PUnitRow unitRow = AMainDS.PUnit.NewRowTyped();
                unitRow.PartnerKey = ASiteKey;
                unitRow.UnitName = ALedgerName;
                AMainDS.PUnit.Rows.Add(unitRow);
            }

            // if this is the first ledger, make it the default site
            SSystemDefaultsTable systemDefaults = SSystemDefaultsAccess.LoadByPrimaryKey("SiteKey", ATransaction);

            if (systemDefaults.Rows.Count == 0)
            {
                SSystemDefaultsRow systemDefaultsRow = AMainDS.SSystemDefaults.NewRowTyped();
                systemDefaultsRow.DefaultCode = SharedConstants.SYSDEFAULT_SITEKEY;
                systemDefaultsRow.DefaultDescription = "there has to be one site key for the database";
                systemDefaultsRow.DefaultValue = ASiteKey.ToString("0000000000");
                AMainDS.SSystemDefaults.Rows.Add(systemDefaultsRow);
            }

            return true;
        }

        /// create a unit and partner for the inter ledger transfer ledger, for a_valid_ledger_number
        [NoRemoting]
        private static bool CreateILTPartner(ref GLSetupTDS AMainDS, Int64 APartnerKey, TDBTransaction ATransaction)
        {
            PPartnerRow partnerRow;

            if (!PPartnerAccess.Exists(APartnerKey, ATransaction))
            {
                partnerRow = AMainDS.PPartner.NewRowTyped();
                partnerRow.PartnerKey = APartnerKey;
                partnerRow.PartnerShortName = "ILT";
                partnerRow.StatusCode = MPartnerConstants.PARTNERSTATUS_ACTIVE;
                partnerRow.PartnerClass = MPartnerConstants.PARTNERCLASS_UNIT;
                AMainDS.PPartner.Rows.Add(partnerRow);

                // create or use addresses (only if partner record is created here as
                // otherwise we assume that Partner has address already)
                PLocationRow locationRow;
                PLocationTable LocTemplateTable;
                PLocationTable LocResultTable;
                PLocationRow LocTemplateRow;
                StringCollection LocTemplateOperators;

                // find address with country set
                LocTemplateTable = new PLocationTable();
                LocTemplateRow = LocTemplateTable.NewRowTyped(false);
                LocTemplateRow.SiteKey = 0;
                LocTemplateRow.StreetName = Catalog.GetString("No valid address on file");
                LocTemplateRow.CountryCode = "99";
                LocTemplateOperators = new StringCollection();

                LocResultTable = PLocationAccess.LoadUsingTemplate(LocTemplateRow, LocTemplateOperators, ATransaction);

                if (LocResultTable.Count > 0)
                {
                    locationRow = (PLocationRow)LocResultTable.Rows[0];
                }
                else
                {
                    // no location record exists yet: create new one
                    locationRow = AMainDS.PLocation.NewRowTyped();
                    locationRow.SiteKey = 0;
                    locationRow.LocationKey = (int)ATransaction.DataBaseObj.GetNextSequenceValue(
                        TSequenceNames.seq_location_number.ToString(), ATransaction);
                    locationRow.StreetName = Catalog.GetString("No valid address on file");
                    locationRow.CountryCode = "99";
                    AMainDS.PLocation.Rows.Add(locationRow);
                }

                // now create partner location record
                PPartnerLocationRow partnerLocationRow = AMainDS.PPartnerLocation.NewRowTyped();
                partnerLocationRow.SiteKey = locationRow.SiteKey;
                partnerLocationRow.PartnerKey = APartnerKey;
                partnerLocationRow.LocationKey = locationRow.LocationKey;
                partnerLocationRow.DateEffective = DateTime.Today;
                AMainDS.PPartnerLocation.Rows.Add(partnerLocationRow);
            }

            if (!PUnitAccess.Exists(APartnerKey, ATransaction))
            {
                PUnitRow unitRow = AMainDS.PUnit.NewRowTyped();
                unitRow.PartnerKey = APartnerKey;
                unitRow.UnitName = "ILT";
                AMainDS.PUnit.Rows.Add(unitRow);
            }

            return true;
        }

        /// <summary>
        /// get the country codes
        /// </summary>
        [RequireModulePermission("FINANCE-1")]
        public static bool GetCountryCodes(out PCountryTable AResultTable)
        {
            TDBTransaction Transaction = new TDBTransaction();

            PCountryTable result = new PCountryTable();

            DBAccess.ReadTransaction(ref Transaction,
                delegate
                {
                    result = PCountryAccess.LoadAll(Transaction);
                });

            AResultTable = result;

            return AResultTable.Rows.Count > 0;
        }

        /// <summary>
        /// get the currency codes
        /// </summary>
        [RequireModulePermission("FINANCE-1")]
        public static bool GetCurrencyCodes(out ACurrencyTable AResultTable)
        {
            TDBTransaction Transaction = new TDBTransaction();

            ACurrencyTable result = new ACurrencyTable();

            DBAccess.ReadTransaction(ref Transaction,
                delegate
                {
                    result = ACurrencyAccess.LoadAll(Transaction);
                });

            AResultTable = result;

            return AResultTable.Rows.Count > 0;
        }

        /// <summary>
        /// create a new ledger and do the initial setup
        /// </summary>
        [RequireModulePermission("FINANCE-3")]
        public static bool CreateNewLedger(
            Int32 ANewLedgerNumber,
            String ALedgerName,
            String ACountryCode,
            String ABaseCurrency,
            String AIntlCurrency,
            DateTime ACalendarStartDate,
            Int32 ANumberOfAccountingPeriods,
            Int32 ACurrentPeriod,
            Int32 ANumberFwdPostingPeriods,
            bool AWithILT,
            out TVerificationResultCollection AVerificationResult)
        {
            bool AActivateAccountsPayable = false;
            Int32 AStartingReceiptNumber = 1;
            bool AActivateGiftProcessing = true;
            bool IchIsAsset = false;
            AVerificationResult = null;
            bool AllOK = false;

            TDataBase db = DBAccess.Connect("CreateNewLedger");

            TDBTransaction Transaction = db.BeginTransaction(IsolationLevel.Serializable);

            try
            {
                // check if such a ledger already exists
                ALedgerTable tempLedger = ALedgerAccess.LoadByPrimaryKey(ANewLedgerNumber, Transaction);

                if (tempLedger.Count > 0)
                {
                    AVerificationResult = new TVerificationResultCollection();
                    string msg = String.Format(Catalog.GetString(
                            "There is already a ledger with number {0}. Please choose another number."), ANewLedgerNumber);
                    AVerificationResult.Add(new TVerificationResult(Catalog.GetString("Creating Ledger"), msg, TResultSeverity.Resv_Critical));

                    return false;
                }

                if ((ANewLedgerNumber <= 1) || (ANewLedgerNumber > 9999))
                {
                    // ledger number 1 does not work, because the root unit has partner key 1000000.
                    AVerificationResult = new TVerificationResultCollection();
                    string msg = String.Format(Catalog.GetString(
                            "Invalid number {0} for a ledger. Please choose a number between 2 and 9999."), ANewLedgerNumber);
                    AVerificationResult.Add(new TVerificationResult(Catalog.GetString("Creating Ledger"), msg, TResultSeverity.Resv_Critical));

                    return false;
                }

                Int64 PartnerKey = Convert.ToInt64(ANewLedgerNumber) * 1000000L;
                GLSetupTDS MainDS = new GLSetupTDS();

                // we currently don't support an international currency
                AIntlCurrency = ABaseCurrency;

                ALedgerRow ledgerRow = MainDS.ALedger.NewRowTyped();
                ledgerRow.LedgerNumber = ANewLedgerNumber;
                ledgerRow.LedgerName = ALedgerName;
                ledgerRow.CurrentPeriod = ACurrentPeriod;
                ledgerRow.NumberOfAccountingPeriods = ANumberOfAccountingPeriods;
                ledgerRow.NumberFwdPostingPeriods = ANumberFwdPostingPeriods;
                ledgerRow.BaseCurrency = ABaseCurrency;
                ledgerRow.IntlCurrency = AIntlCurrency;
                ledgerRow.ActualsDataRetention = 11;
                ledgerRow.GiftDataRetention = 11;
                ledgerRow.BudgetDataRetention = 2;
                ledgerRow.CountryCode = ACountryCode;
                ledgerRow.ForexGainsLossesAccount = "5003";
                ledgerRow.PartnerKey = PartnerKey;

                if (ANumberOfAccountingPeriods == 12)
                {
                    ledgerRow.CalendarMode = true;
                }
                else
                {
                    ledgerRow.CalendarMode = false;
                }

                MainDS.ALedger.Rows.Add(ledgerRow);

                CreateSite(ref MainDS, ALedgerName, PartnerKey, Transaction);
                CreateILTPartner(ref MainDS, 4000000, Transaction);

                PPartnerTypeAccess.LoadViaPPartner(MainDS, PartnerKey, Transaction);
                PPartnerTypeRow partnerTypeRow;

                // only create special type "LEDGER" if it does not exist yet
                if (MainDS.PPartnerType.Rows.Find(new object[] { PartnerKey, MPartnerConstants.PARTNERTYPE_LEDGER }) == null)
                {
                    partnerTypeRow = MainDS.PPartnerType.NewRowTyped();
                    partnerTypeRow.PartnerKey = PartnerKey;
                    partnerTypeRow.TypeCode = MPartnerConstants.PARTNERTYPE_LEDGER;
                    MainDS.PPartnerType.Rows.Add(partnerTypeRow);
                }

                String ModuleId = "LEDGER" + ANewLedgerNumber.ToString("0000");

                if (!SModuleAccess.Exists(ModuleId, Transaction))
                {
                    SModuleRow moduleRow = MainDS.SModule.NewRowTyped();
                    moduleRow.ModuleId = ModuleId;
                    moduleRow.ModuleName = moduleRow.ModuleId;
                    MainDS.SModule.Rows.Add(moduleRow);
                }

                //TODO: Calendar vs Financial Date Handling - Need to review this
                // create calendar
                // at the moment we only support financial years that start on the first day of a month
                // and currently only 12 or 13 periods are allowed and a maximum of 8 forward periods
                DateTime periodStartDate = ACalendarStartDate;

                for (Int32 periodNumber = 1; periodNumber <= ANumberOfAccountingPeriods + ANumberFwdPostingPeriods; periodNumber++)
                {
                    AAccountingPeriodRow accountingPeriodRow = MainDS.AAccountingPeriod.NewRowTyped();
                    accountingPeriodRow.LedgerNumber = ANewLedgerNumber;
                    accountingPeriodRow.AccountingPeriodNumber = periodNumber;
                    accountingPeriodRow.PeriodStartDate = periodStartDate;

                    if ((ANumberOfAccountingPeriods == 13)
                        && (periodNumber == 12))
                    {
                        // in case of 12 periods the second last period represents the last month except for the very last day
                        accountingPeriodRow.PeriodEndDate = periodStartDate.AddMonths(1).AddDays(-2);
                    }
                    else if ((ANumberOfAccountingPeriods == 13)
                             && (periodNumber == 13))
                    {
                        // in case of 13 periods the last period just represents the very last day of the financial year
                        accountingPeriodRow.PeriodEndDate = periodStartDate;
                    }
                    else
                    {
                        accountingPeriodRow.PeriodEndDate = periodStartDate.AddMonths(1).AddDays(-1);
                    }

                    // The month 'description' is always in English but can be edited in the Calendar GUI
                    accountingPeriodRow.AccountingPeriodDesc = periodStartDate.ToString("MMMM", CultureInfo.InvariantCulture);
                    MainDS.AAccountingPeriod.Rows.Add(accountingPeriodRow);
                    periodStartDate = accountingPeriodRow.PeriodEndDate.AddDays(1);
                }

                // mark cached table for accounting periods to be refreshed
                TCacheableTablesManager.GCacheableTablesManager.MarkCachedTableNeedsRefreshing(
                    TCacheableFinanceTablesEnum.AccountingPeriodList.ToString());

                AAccountingSystemParameterRow accountingSystemParameterRow = MainDS.AAccountingSystemParameter.NewRowTyped();
                accountingSystemParameterRow.LedgerNumber = ANewLedgerNumber;
                accountingSystemParameterRow.ActualsDataRetention = ledgerRow.ActualsDataRetention;
                accountingSystemParameterRow.GiftDataRetention = ledgerRow.GiftDataRetention;
                accountingSystemParameterRow.NumberFwdPostingPeriods = ledgerRow.NumberFwdPostingPeriods;
                accountingSystemParameterRow.NumberOfAccountingPeriods = ledgerRow.NumberOfAccountingPeriods;
                accountingSystemParameterRow.BudgetDataRetention = ledgerRow.BudgetDataRetention;
                MainDS.AAccountingSystemParameter.Rows.Add(accountingSystemParameterRow);

                // activate GL subsystem (this is always active)
                ASystemInterfaceRow systemInterfaceRow = MainDS.ASystemInterface.NewRowTyped();
                systemInterfaceRow.LedgerNumber = ANewLedgerNumber;

                systemInterfaceRow.SubSystemCode = CommonAccountingSubSystemsEnum.GL.ToString();
                systemInterfaceRow.SetUpComplete = true;
                MainDS.ASystemInterface.Rows.Add(systemInterfaceRow);


                ATransactionTypeRow transactionTypeRow;

                // TODO: this might be different for other account or costcentre names
                transactionTypeRow = MainDS.ATransactionType.NewRowTyped();
                transactionTypeRow.LedgerNumber = ANewLedgerNumber;
                transactionTypeRow.SubSystemCode = CommonAccountingSubSystemsEnum.GL.ToString();
                transactionTypeRow.TransactionTypeCode = CommonAccountingTransactionTypesEnum.ALLOC.ToString();
                transactionTypeRow.DebitAccountCode = "BAL SHT";
                transactionTypeRow.CreditAccountCode = "BAL SHT";
                transactionTypeRow.TransactionTypeDescription = "Allocation Journal";
                transactionTypeRow.SpecialTransactionType = true;
                MainDS.ATransactionType.Rows.Add(transactionTypeRow);

                transactionTypeRow = MainDS.ATransactionType.NewRowTyped();
                transactionTypeRow.LedgerNumber = ANewLedgerNumber;
                transactionTypeRow.SubSystemCode = CommonAccountingSubSystemsEnum.GL.ToString();
                transactionTypeRow.TransactionTypeCode = CommonAccountingTransactionTypesEnum.REALLOC.ToString();
                transactionTypeRow.DebitAccountCode = "BAL SHT";
                transactionTypeRow.CreditAccountCode = "BAL SHT";
                transactionTypeRow.TransactionTypeDescription = "Reallocation Journal";
                transactionTypeRow.SpecialTransactionType = true;
                MainDS.ATransactionType.Rows.Add(transactionTypeRow);

                transactionTypeRow = MainDS.ATransactionType.NewRowTyped();
                transactionTypeRow.LedgerNumber = ANewLedgerNumber;
                transactionTypeRow.SubSystemCode = CommonAccountingSubSystemsEnum.GL.ToString();
                transactionTypeRow.TransactionTypeCode = CommonAccountingTransactionTypesEnum.REVAL.ToString();
                transactionTypeRow.DebitAccountCode = "5003";
                transactionTypeRow.CreditAccountCode = "5003";
                transactionTypeRow.TransactionTypeDescription = "Foreign Exchange Revaluation";
                transactionTypeRow.SpecialTransactionType = true;
                MainDS.ATransactionType.Rows.Add(transactionTypeRow);

                transactionTypeRow = MainDS.ATransactionType.NewRowTyped();
                transactionTypeRow.LedgerNumber = ANewLedgerNumber;
                transactionTypeRow.SubSystemCode = CommonAccountingSubSystemsEnum.GL.ToString();
                transactionTypeRow.TransactionTypeCode = CommonAccountingTransactionTypesEnum.STD.ToString();
                transactionTypeRow.DebitAccountCode = MFinanceConstants.ACCOUNT_BAL_SHT;
                transactionTypeRow.CreditAccountCode = MFinanceConstants.ACCOUNT_BAL_SHT;
                transactionTypeRow.TransactionTypeDescription = "Standard Journal";
                transactionTypeRow.SpecialTransactionType = false;
                MainDS.ATransactionType.Rows.Add(transactionTypeRow);


                AValidLedgerNumberTable validLedgerNumberTable = AValidLedgerNumberAccess.LoadByPrimaryKey(ANewLedgerNumber, PartnerKey, Transaction);

                if (validLedgerNumberTable.Rows.Count == 0)
                {
                    AValidLedgerNumberRow validLedgerNumberRow = MainDS.AValidLedgerNumber.NewRowTyped();
                    validLedgerNumberRow.PartnerKey = PartnerKey;
                    validLedgerNumberRow.LedgerNumber = ANewLedgerNumber;
                    validLedgerNumberRow.IltProcessingCentre = 4000000;
                    validLedgerNumberRow.CostCentreCode = (ANewLedgerNumber * 100).ToString("0000");
                    MainDS.AValidLedgerNumber.Rows.Add(validLedgerNumberRow);
                }

                ACostCentreTypesRow costCentreTypesRow = MainDS.ACostCentreTypes.NewRowTyped();
                costCentreTypesRow.LedgerNumber = ANewLedgerNumber;
                costCentreTypesRow.CostCentreType = "Local";
                costCentreTypesRow.Deletable = false;
                MainDS.ACostCentreTypes.Rows.Add(costCentreTypesRow);
                costCentreTypesRow = MainDS.ACostCentreTypes.NewRowTyped();
                costCentreTypesRow.LedgerNumber = ANewLedgerNumber;
                costCentreTypesRow.CostCentreType = "Foreign";
                costCentreTypesRow.Deletable = false;
                MainDS.ACostCentreTypes.Rows.Add(costCentreTypesRow);

                TSystemDefaults SystemDefaults = new TSystemDefaults(db);

                if (AWithILT)
                {
                    SystemDefaults.SetSystemDefault(SharedConstants.SYSDEFAULT_ILTPROCESSINGENABLED, Boolean.TrueString);
                }

                ImportDefaultAccountHierarchy(ref MainDS, ANewLedgerNumber, ref AVerificationResult);
                ImportDefaultCostCentreHierarchy(ref MainDS, ANewLedgerNumber, ALedgerName);

                if (AWithILT || SystemDefaults.GetBooleanDefault(
                    SharedConstants.SYSDEFAULT_ILTPROCESSINGENABLED, false) == true)
                {
                    ACostCentreRow newCostCentreRow = MainDS.ACostCentre.NewRowTyped();
                    newCostCentreRow.LedgerNumber = ANewLedgerNumber;
                    newCostCentreRow.CostCentreCode = MFinanceConstants.INTER_LEDGER_HEADING;
                    newCostCentreRow.CostCentreToReportTo = "[" + ANewLedgerNumber.ToString() + "]";
                    newCostCentreRow.CostCentreName = "Inter Ledger Transfer Total";
                    newCostCentreRow.PostingCostCentreFlag = false;
                    newCostCentreRow.CostCentreActiveFlag = true;
                    MainDS.ACostCentre.Rows.Add(newCostCentreRow);
                    
                    SetupILTCostCentreHierarchy(ref MainDS, ANewLedgerNumber, Transaction);
                }

                ImportDefaultMotivations(ref MainDS, ANewLedgerNumber, db);
                ImportDefaultAdminGrantsPayableReceivable(ref MainDS, ANewLedgerNumber);

                GLSetupTDSAccess.SubmitChanges(MainDS, db);

                // activate gift processing subsystem
                if (AActivateGiftProcessing)
                {
                    ActivateGiftProcessingSubsystem(ANewLedgerNumber, AStartingReceiptNumber, db);
                }

                // activate accounts payable subsystem
                if (AActivateAccountsPayable)
                {
                    ActivateAccountsPayableSubsystem(ANewLedgerNumber);
                }

                // give the current user access permissions to this new ledger
                SUserModuleAccessPermissionTable moduleAccessPermissionTable = new SUserModuleAccessPermissionTable();

                SUserModuleAccessPermissionRow moduleAccessPermissionRow = moduleAccessPermissionTable.NewRowTyped();
                moduleAccessPermissionRow.UserId = UserInfo.GetUserInfo().UserID;
                moduleAccessPermissionRow.ModuleId = "LEDGER" + ANewLedgerNumber.ToString("0000");
                moduleAccessPermissionRow.CanAccess = true;
                moduleAccessPermissionTable.Rows.Add(moduleAccessPermissionRow);

                SUserModuleAccessPermissionAccess.SubmitChanges(moduleAccessPermissionTable, Transaction);

                // create system analysis types for the new ledger
                AAnalysisTypeAccess.LoadAll(MainDS, Transaction);
                AAnalysisTypeTable NewAnalysisTypes = new AAnalysisTypeTable();

                foreach (AAnalysisTypeRow AnalysisTypeRow in MainDS.AAnalysisType.Rows)
                {
                    if (AnalysisTypeRow.SystemAnalysisType
                        && !NewAnalysisTypes.Rows.Contains(new Object[] { ANewLedgerNumber, AnalysisTypeRow.AnalysisTypeCode }))
                    {
                        AAnalysisTypeRow NewAnalysisType = NewAnalysisTypes.NewRowTyped();
                        NewAnalysisType.ItemArray = (object[])AnalysisTypeRow.ItemArray.Clone();
                        NewAnalysisType.LedgerNumber = ANewLedgerNumber;
                        NewAnalysisTypes.Rows.Add(NewAnalysisType);
                    }
                }

                AAnalysisTypeAccess.SubmitChanges(NewAnalysisTypes, Transaction);


                AllOK = true;
            }
            catch (Exception Exc)
            {
                TLogging.Log("An Exception occured during the creation of a new Ledger:" + Environment.NewLine + Exc.ToString());

                Transaction.Rollback();

                throw;
            }
            finally
            {
                if (AllOK)
                {
                    Transaction.Commit();

                    //
                    // If the user has specified that ICH is an asset,
                    // I need to re-write it into the hierarchy:
                    if (IchIsAsset)
                    {
                        RewireIchIsAsset(ANewLedgerNumber);
                    }
                }
                else
                {
                    Transaction.Rollback();
                }
            }

            db.CloseDBConnection();

            return AllOK;
        }

        /// <summary>
        /// return true if the ledger contains any transactions
        /// </summary>
        [RequireModulePermission("FINANCE-1")]
        public static bool ContainsTransactions(Int32 ALedgerNumber)
        {
            bool Result = true;

            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("ContainsTransactions");

            db.ReadTransaction(
                ref Transaction,
                delegate
                {
                    Result = (ATransactionAccess.CountViaALedger(ALedgerNumber, Transaction) > 0);
                });

            db.CloseDBConnection();

            return Result;
        }

        /// <summary>
        /// deletes the complete ledger, with all finance data. useful for testing purposes
        /// </summary>
        [RequireModulePermission("FINANCE-3")]
        public static bool DeleteLedger(Int32 ALedgerNumber, out TVerificationResultCollection AVerificationResult)
        {
            AVerificationResult = null;
            TVerificationResultCollection VerificationResult = new TVerificationResultCollection();

            TProgressTracker.InitProgressTracker(DomainManager.GClientID.ToString(),
                Catalog.GetString("Deleting ledger"),
                100);

            TProgressTracker.SetCurrentState(DomainManager.GClientID.ToString(),
                Catalog.GetString("Deleting ledger"),
                20);

            TDBTransaction Transaction = new TDBTransaction();
            bool SubmitOK = false;
            TDataBase db = DBAccess.Connect("DeleteLedger");
            bool Result = true;
            db.WriteTransaction(ref Transaction,
                ref SubmitOK,
                delegate
                {
                    try
                    {
                        OdbcParameter[] ledgerparameter = new OdbcParameter[] {
                            new OdbcParameter("ledgernumber", OdbcType.Int)
                        };
                        ledgerparameter[0].Value = ALedgerNumber;

                        db.ExecuteNonQuery(
                            String.Format("DELETE FROM PUB_{0} WHERE {1} = 'LEDGER{2:0000}'",
                                SUserModuleAccessPermissionTable.GetTableDBName(),
                                SUserModuleAccessPermissionTable.GetModuleIdDBName(),
                                ALedgerNumber),
                            Transaction);

                        db.ExecuteNonQuery(
                            String.Format("DELETE FROM PUB_{0} WHERE {1} = 'LEDGER{2:0000}'",
                                SModuleTable.GetTableDBName(),
                                SModuleTable.GetModuleIdDBName(),
                                ALedgerNumber),
                            Transaction);

                        db.ExecuteNonQuery(
                            String.Format(
                                "DELETE FROM PUB_{0} WHERE EXISTS (SELECT * FROM PUB_{1} WHERE {2}.{3} = {4}.{5} AND {6}.{7} = ?)",
                                AGeneralLedgerMasterPeriodTable.GetTableDBName(),
                                AGeneralLedgerMasterTable.GetTableDBName(),
                                AGeneralLedgerMasterTable.GetTableDBName(),
                                AGeneralLedgerMasterTable.GetGlmSequenceDBName(),
                                AGeneralLedgerMasterPeriodTable.GetTableDBName(),
                                AGeneralLedgerMasterPeriodTable.GetGlmSequenceDBName(),
                                AGeneralLedgerMasterTable.GetTableDBName(),
                                AGeneralLedgerMasterTable.GetLedgerNumberDBName()),
                            Transaction, ledgerparameter);

                        db.ExecuteNonQuery(
                            String.Format(
                                "DELETE FROM PUB_{0} WHERE EXISTS (SELECT * FROM PUB_{1} WHERE {2}.{3} = {4}.{5} AND {6}.{7} = ?)",
                                ABudgetPeriodTable.GetTableDBName(),
                                ABudgetTable.GetTableDBName(),
                                ABudgetTable.GetTableDBName(),
                                ABudgetTable.GetBudgetSequenceDBName(),
                                ABudgetPeriodTable.GetTableDBName(),
                                ABudgetPeriodTable.GetBudgetSequenceDBName(),
                                ABudgetTable.GetTableDBName(),
                                ABudgetTable.GetLedgerNumberDBName()),
                            Transaction, ledgerparameter);

                        db.ExecuteNonQuery(
                            String.Format(
                                "DELETE FROM PUB_{0} WHERE EXISTS (SELECT * FROM PUB_{1} WHERE {2}.{3} = {4}.{5} AND {6}.{7} = ?)",
                                AEpTransactionTable.GetTableDBName(),
                                AEpStatementTable.GetTableDBName(),
                                AEpTransactionTable.GetTableDBName(),
                                AEpTransactionTable.GetStatementKeyDBName(),
                                AEpStatementTable.GetTableDBName(),
                                AEpStatementTable.GetStatementKeyDBName(),
                                AEpStatementTable.GetTableDBName(),
                                AEpStatementTable.GetLedgerNumberDBName()),
                            Transaction, ledgerparameter);

                        // the following tables are not deleted at the moment as they are not in use
                        //      PFoundationProposalDetailTable.GetTableDBName(),
                        // also: tables referring to ATaxTableTable are not deleted now as they are not yet in use
                        //      (those are tables needed in the accounts receivable module that does not exist yet)


                        string[] tablenames = new string[] {
                            AValidLedgerNumberTable.GetTableDBName(),
                                 AProcessedFeeTable.GetTableDBName(),
                                 AGeneralLedgerMasterTable.GetTableDBName(),
                                 AMotivationDetailFeeTable.GetTableDBName(),

                                 AEpMatchTable.GetTableDBName(),
                                 AEpStatementTable.GetTableDBName(),
                                 AEpAccountTable.GetTableDBName(),

                                 ABudgetTable.GetTableDBName(),
                                 ABudgetRevisionTable.GetTableDBName(),

                                 ARecurringGiftDetailTable.GetTableDBName(),
                                 ARecurringGiftTable.GetTableDBName(),
                                 ARecurringGiftBatchTable.GetTableDBName(),

                                 AGiftDetailTable.GetTableDBName(),
                                 AGiftTable.GetTableDBName(),
                                 AGiftBatchTable.GetTableDBName(),

                                 ATransAnalAttribTable.GetTableDBName(),
                                 ATransactionTable.GetTableDBName(),
                                 AJournalTable.GetTableDBName(),
                                 ABatchTable.GetTableDBName(),

                                 ARecurringTransAnalAttribTable.GetTableDBName(),
                                 ARecurringTransactionTable.GetTableDBName(),
                                 ARecurringJournalTable.GetTableDBName(),
                                 ARecurringBatchTable.GetTableDBName(),

                                 AEpDocumentPaymentTable.GetTableDBName(),
                                 AEpPaymentTable.GetTableDBName(),

                                 AApAnalAttribTable.GetTableDBName(),
                                 AApDocumentPaymentTable.GetTableDBName(),
                                 AApPaymentTable.GetTableDBName(),
                                 ACrdtNoteInvoiceLinkTable.GetTableDBName(),
                                 AApDocumentDetailTable.GetTableDBName(),
                                 AApDocumentTable.GetTableDBName(),

                                 AFreeformAnalysisTable.GetTableDBName(),

                                 AEpAccountTable.GetTableDBName(),
                                 ASuspenseAccountTable.GetTableDBName(),
                                 SGroupMotivationTable.GetTableDBName(),
                                 AIchStewardshipTable.GetTableDBName(),
                                 SGroupCostCentreTable.GetTableDBName(),
                                 AAnalysisAttributeTable.GetTableDBName(),

                                 AMotivationDetailTable.GetTableDBName(),
                                 AMotivationGroupTable.GetTableDBName(),
                                 AFeesReceivableTable.GetTableDBName(),
                                 AFeesPayableTable.GetTableDBName(),
                                 ACostCentreTable.GetTableDBName(),
                                 ATransactionTypeTable.GetTableDBName(),
                                 AAccountPropertyTable.GetTableDBName(),
                                 AAccountHierarchyDetailTable.GetTableDBName(),
                                 AAccountHierarchyTable.GetTableDBName(),
                                 AAccountTable.GetTableDBName(),
                                 ASystemInterfaceTable.GetTableDBName(),
                                 AAccountingSystemParameterTable.GetTableDBName(),
                                 ACostCentreTypesTable.GetTableDBName(),
                                 AAnalysisTypeTable.GetTableDBName(),

                                 ALedgerInitFlagTable.GetTableDBName(),
                                 ATaxTableTable.GetTableDBName(),

                                 AAccountingPeriodTable.GetTableDBName(),

                                 SGroupLedgerTable.GetTableDBName()
                        };

                        foreach (string table in tablenames)
                        {
                            db.ExecuteNonQuery(
                                String.Format("DELETE FROM PUB_{0} WHERE a_ledger_number_i = ?", table),
                                Transaction, ledgerparameter);
                        }

                        ALedgerAccess.DeleteByPrimaryKey(ALedgerNumber, Transaction);

                        if (TProgressTracker.GetCurrentState(DomainManager.GClientID.ToString()).CancelJob == true)
                        {
                            TProgressTracker.FinishJob(DomainManager.GClientID.ToString());
                            throw new Exception("Deletion of Ledger was cancelled by the user");
                        }

                        SubmitOK = true;
                    }
                    catch (Exception e)
                    {
                        TLogging.Log(e.ToString());

                        if (TDBExceptionHelper.IsTransactionSerialisationException(e))
                        {
                            VerificationResult.Add(new TVerificationResult("DeleteLedger",
                                    ErrorCodeInventory.RetrieveErrCodeInfo(PetraErrorCodes.ERR_DB_SERIALIZATION_EXCEPTION)));
                        }
                        else
                        {
                            VerificationResult.Add(new TVerificationResult(
                                    "Problems deleting ledger " + ALedgerNumber.ToString(),
                                    e.Message,
                                    "Cannot delete ledger",
                                    string.Empty,
                                    TResultSeverity.Resv_Critical,
                                    Guid.Empty));
                        }

                        Result = false;
                    }
                    finally
                    {
                        TProgressTracker.FinishJob(DomainManager.GClientID.ToString());
                    }
                });

            db.CloseDBConnection();

            AVerificationResult = VerificationResult;
            return Result;
        }

        /// <summary>
        /// maintain ledger settings
        /// </summary>
        [RequireModulePermission("FINANCE-3")]
        public static bool MaintainLedger(string action, Int32 ALedgerNumber, String ALedgerName,
            out TVerificationResultCollection AVerificationResult)
        {
            AVerificationResult = new TVerificationResultCollection();

            if (action == "update")
            {
                TDBTransaction Transaction = new TDBTransaction();
                TDataBase db = DBAccess.Connect("MaintainLedger");
                bool SubmissionOK = false;

                try
                {
                    db.WriteTransaction(
                        ref Transaction, ref SubmissionOK,
                        delegate
                        {
                            ALedgerTable LedgerTable = ALedgerAccess.LoadByPrimaryKey(ALedgerNumber, Transaction);

                            ALedgerRow LedgerRow = (ALedgerRow)LedgerTable.Rows[0];
                            LedgerRow.LedgerName = ALedgerName;

                            ALedgerAccess.SubmitChanges(LedgerTable, Transaction);

                            SubmissionOK = true;
                        });
                }
                catch (Exception ex)
                {
                    TLogging.LogException(ex, Utilities.GetMethodSignature());
                    return false;
                }

                db.CloseDBConnection();

                return true;
            }
            else if (action == "delete")
            {
                return DeleteLedger(ALedgerNumber, out AVerificationResult);
            }

            return false;
        }

        /// <summary>
        /// get the ledger numbers that are available for the current user
        /// </summary>
        [RequireModulePermission("FINANCE-1")]
        public static ALedgerTable GetAvailableLedgers()
        {
            // TODO check for permissions of the current user
            ALedgerTable LedgerTable = null;

            StringCollection Fields = new StringCollection();

            Fields.Add(ALedgerTable.GetLedgerNameDBName());
            Fields.Add(ALedgerTable.GetLedgerNumberDBName());
            Fields.Add(ALedgerTable.GetBaseCurrencyDBName());
            Fields.Add(ALedgerTable.GetIntlCurrencyDBName());
            Fields.Add(ALedgerTable.GetLedgerStatusDBName());
            Fields.Add(ALedgerTable.GetCurrentPeriodDBName());
            Fields.Add(ALedgerTable.GetNumberOfAccountingPeriodsDBName());
            Fields.Add(ALedgerTable.GetNumberFwdPostingPeriodsDBName());
            Fields.Add(ALedgerTable.GetCountryCodeDBName());

            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("GetAvailableLedgers");
            db.ReadTransaction(
                ref Transaction,
                delegate
                {
                    LedgerTable = ALedgerAccess.LoadAll(Fields, Transaction, null, 0, 0);
                });

            db.CloseDBConnection();

            return LedgerTable;
        }

        /// <summary>
        /// Load  the table AFREEFORMANALSYSIS
        /// </summary>
        [RequireModulePermission("FINANCE-1")]
        public static AFreeformAnalysisTable LoadAFreeformAnalysis(Int32 ALedgerNumber)
        {
            GLSetupTDS MainDS = new GLSetupTDS();

            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("LoadAFreeformAnalysis");

            db.ReadTransaction(
                ref Transaction,
                delegate
                {
                    AFreeformAnalysisAccess.LoadViaALedger(MainDS, ALedgerNumber, Transaction);
                });

            // Accept row changes here so that the Client gets 'unmodified' rows
            MainDS.AcceptChanges();

            // Remove all Tables that were not filled with data before remoting them.
            MainDS.RemoveEmptyTables();
            AFreeformAnalysisTable myAT = MainDS.AFreeformAnalysis;

            db.CloseDBConnection();

            return myAT;
        }

        /// <summary>
        /// Check if a AnalysisAttribute Row can be removed from an Account (not if it's in use!)
        /// </summary>
        [RequireModulePermission("FINANCE-1")]
        public static Boolean CanDetachTypeCodeFromAccount(Int32 ALedgerNumber, String AAccountCode, String ATypeCode, out String AMessage)
        {
            TDBTransaction ReadTrans = new TDBTransaction();
            TDataBase db = DBAccess.Connect("CanDetachTypeCodeFromAccount");
            bool Result = true;
            string Message = String.Empty;
            db.ReadTransaction(
                ref ReadTrans,
                delegate
                {
                    if (Result)
                    {
                        AApAnalAttribTable tbl = new AApAnalAttribTable();
                        AApAnalAttribRow Template = tbl.NewRowTyped(false);
                        Template.LedgerNumber = ALedgerNumber;
                        Template.AccountCode = AAccountCode;
                        Template.AnalysisTypeCode = ATypeCode;
                        tbl = AApAnalAttribAccess.LoadUsingTemplate(Template, ReadTrans);

                        if (tbl.Rows.Count > 0)
                        {
                            Message = String.Format(Catalog.GetString("Cannot remove {0} from {1}: "), ATypeCode, AAccountCode) +
                                      String.Format(Catalog.GetString("Analysis Type is used in AP documents ({0} entries)."), tbl.Rows.Count);
                            Result = false;
                        }
                    }

                    if (Result)
                    {
                        ATransAnalAttribTable tbl = new ATransAnalAttribTable();
                        ATransAnalAttribRow Template = tbl.NewRowTyped(false);
                        Template.LedgerNumber = ALedgerNumber;
                        Template.AccountCode = AAccountCode;
                        Template.AnalysisTypeCode = ATypeCode;
                        tbl = ATransAnalAttribAccess.LoadUsingTemplate(Template, ReadTrans);

                        if (tbl.Rows.Count > 0)
                        {
                            Message = String.Format(Catalog.GetString("Cannot remove {0} from {1}: "), ATypeCode, AAccountCode) +
                                      String.Format(Catalog.GetString("Analysis Type is used in Transactions ({0} entries)."), tbl.Rows.Count);
                            Result = false;
                        }
                    }

                    if (Result)
                    {
                        ARecurringTransAnalAttribTable tbl = new ARecurringTransAnalAttribTable();
                        ARecurringTransAnalAttribRow Template = tbl.NewRowTyped(false);
                        Template.LedgerNumber = ALedgerNumber;
                        Template.AccountCode = AAccountCode;
                        Template.AnalysisTypeCode = ATypeCode;
                        tbl = ARecurringTransAnalAttribAccess.LoadUsingTemplate(Template, ReadTrans);

                        if (tbl.Rows.Count > 0)
                        {
                            Message = String.Format(Catalog.GetString("Cannot remove {0} from {1}: "), ATypeCode, AAccountCode) +
                                      String.Format(Catalog.GetString("Analysis Type is used in recurring Transactions ({0} entries)."), tbl.Rows.Count);
                            Result = false;
                        }
                    }
                });

            db.CloseDBConnection();

            AMessage = Message;
            return Result;
        }

        /// <summary>
        /// Check if a value in  AFREEFORMANALSYSIS can be deleted (count the references in ATRansANALATTRIB)
        /// </summary>
        [RequireModulePermission("FINANCE-1")]
        public static int CheckDeleteAFreeformAnalysis(Int32 ALedgerNumber, String ATypeCode, String AAnalysisValue)
        {
            int RetVal = 0;

            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("CheckDeleteAFreeformAnalysis");

            db.ReadTransaction(
                ref Transaction,
                delegate
                {
                    RetVal = ATransAnalAttribAccess.CountViaAFreeformAnalysis(ALedgerNumber, ATypeCode, AAnalysisValue, Transaction);
                });

            db.CloseDBConnection();

            return RetVal;
        }

        /// <summary>
        /// Check if a TypeCode in  AnalysisType can be deleted (count the references in ATRansAnalysisAtrributes)
        /// </summary>
        [RequireModulePermission("FINANCE-1")]
        public static int CheckDeleteAAnalysisType(Int32 ALedgerNumber, String ATypeCode)
        {
            int RetVal = 0;

            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("CheckDeleteAAnalysisType");

            db.ReadTransaction(
                ref Transaction,
                delegate
                {
                    RetVal = AAnalysisAttributeAccess.CountViaAAnalysisType(ALedgerNumber, ATypeCode, Transaction);
                });

            db.CloseDBConnection();

            return RetVal;
        }

        /// <summary>
        /// Get a list of Analysis Attributes that must be used with this account.
        /// </summary>
        [RequireModulePermission("FINANCE-1")]
        public static StringCollection RequiredAnalysisAttributesForAccount(Int32 ALedgerNumber, String AAccountCode, Boolean AActiveOnly = false)
        {
            #region Validate Arguments

            if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }
            else if (AAccountCode.Length == 0)
            {
                throw new ArgumentException(String.Format(Catalog.GetString("Function:{0} - The Account code is empty!"),
                        Utilities.GetMethodName(true)));
            }

            #endregion Validate Arguments

            StringCollection RetVal = new StringCollection();
            AAnalysisAttributeTable AnalAttribTable = null;

            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("RequiredAnalysisAttributesForAccount");

            db.ReadTransaction(
                ref Transaction,
                delegate
                {
                    AnalAttribTable = AAnalysisAttributeAccess.LoadViaAAccount(ALedgerNumber, AAccountCode, Transaction);
                });

            foreach (AAnalysisAttributeRow Row in AnalAttribTable.Rows)
            {
                if (!AActiveOnly || Row.Active)
                {
                    RetVal.Add(Row.AnalysisTypeCode);
                }
            }

            db.CloseDBConnection();

            return RetVal;
        }

        /// <summary>
        /// Check if this account code for Ledger ALedgerNumber requires one or more analysis attributes
        /// </summary>
        [RequireModulePermission("FINANCE-1")]
        public static bool AccountHasAnalysisAttributes(Int32 ALedgerNumber,
            String AAccountCode,
            out Int32 ANumberOfAttributes,
            bool AActiveOnly = false)
        {
            #region Validate Arguments

            if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }
            else if (AAccountCode.Length == 0)
            {
                throw new ArgumentException(String.Format(Catalog.GetString("Function:{0} - The Account code is empty!"),
                        Utilities.GetMethodName(true)));
            }

            #endregion Validate Arguments

            ANumberOfAttributes = 0;
            Int32 NumberOfAttributes = ANumberOfAttributes;

            AAnalysisAttributeTable AnalysisAttributeTable = null;
            bool AccountAnalysisAttributeExists = false;

            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("AccountHasAnalysisAttributes");

            try
            {
                db.ReadTransaction(
                    ref Transaction,
                    delegate
                    {
                        if (!AActiveOnly)
                        {
                            AnalysisAttributeTable = AAnalysisAttributeAccess.LoadViaAAccount(ALedgerNumber, AAccountCode, Transaction);
                        }
                        else
                        {
                            AAnalysisAttributeTable AATable = new AAnalysisAttributeTable();
                            AAnalysisAttributeRow TemplateAARow = AATable.NewRowTyped(false);
                            TemplateAARow.LedgerNumber = ALedgerNumber;
                            TemplateAARow.AccountCode = AAccountCode;
                            TemplateAARow.Active = true;

                            AnalysisAttributeTable = AAnalysisAttributeAccess.LoadUsingTemplate(TemplateAARow, Transaction);
                        }

                        NumberOfAttributes = AnalysisAttributeTable.Count;

                        AccountAnalysisAttributeExists = (NumberOfAttributes > 0);
                    });

                ANumberOfAttributes = NumberOfAttributes;
            }
            catch (Exception ex)
            {
                TLogging.LogException(ex, Utilities.GetMethodSignature());
                throw;
            }

            db.CloseDBConnection();

            return AccountAnalysisAttributeExists;
        }

        /// <summary>
        /// Check if this account code for Ledger ALedgerNumber requires one or more analysis attributes
        /// </summary>
        [RequireModulePermission("FINANCE-1")]
        public static bool AccountHasAnalysisAttributes(Int32 ALedgerNumber, String AAccountCode, bool AActiveOnly = false)
        {
            #region Validate Arguments

            if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }

            #endregion Validate Arguments

            AAnalysisAttributeTable AnalysisAttributeTable = null;
            bool AccountAnalysisAttributeExists = false;

            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("AccountHasAnalysisAttributes");

            try
            {
                db.ReadTransaction(
                    ref Transaction,
                    delegate
                    {
                        if (!AActiveOnly)
                        {
                            AnalysisAttributeTable = AAnalysisAttributeAccess.LoadViaAAccount(ALedgerNumber, AAccountCode, Transaction);
                        }
                        else
                        {
                            AAnalysisAttributeTable AATable = new AAnalysisAttributeTable();
                            AAnalysisAttributeRow TemplateAARow = AATable.NewRowTyped(false);
                            TemplateAARow.LedgerNumber = ALedgerNumber;
                            TemplateAARow.AccountCode = AAccountCode;
                            TemplateAARow.Active = true;

                            AnalysisAttributeTable = AAnalysisAttributeAccess.LoadUsingTemplate(TemplateAARow, Transaction);
                        }
                    });

                AccountAnalysisAttributeExists = (AnalysisAttributeTable.Count > 0);
            }
            catch (Exception ex)
            {
                TLogging.LogException(ex, Utilities.GetMethodSignature());
                throw;
            }

            db.CloseDBConnection();

            return AccountAnalysisAttributeExists;
        }

        /// <summary>
        /// Check if this account type code for Ledger ALedgerNumber requires one or more analysis attribute values
        /// </summary>
        [RequireModulePermission("FINANCE-1")]
        public static bool AccountAnalysisAttributeRequiresValues(Int32 ALedgerNumber, string AAnalysisTypeCode, bool AActiveOnly)
        {
            #region Validate Arguments

            if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }
            else if (AAnalysisTypeCode.Length == 0)
            {
                throw new ArgumentException(String.Format(Catalog.GetString("Function:{0} - The Analysis Type code is empty!"),
                        Utilities.GetMethodName(true)));
            }

            #endregion Validate Arguments

            bool AccountAnalysisAttributeValueRequired = false;

            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("AccountAnalysisAttributeRequiresValues");

            try
            {
                db.ReadTransaction(
                    ref Transaction,
                    delegate
                    {
                        AFreeformAnalysisTable FFATable = new AFreeformAnalysisTable();
                        AFreeformAnalysisRow TemplateFFARow = FFATable.NewRowTyped(false);
                        TemplateFFARow.LedgerNumber = ALedgerNumber;
                        TemplateFFARow.AnalysisTypeCode = AAnalysisTypeCode;

                        if (AActiveOnly)
                        {
                            TemplateFFARow.Active = AActiveOnly;
                        }

                        AFreeformAnalysisTable FreeformAnalysisTable = AFreeformAnalysisAccess.LoadUsingTemplate(TemplateFFARow, Transaction);

                        AccountAnalysisAttributeValueRequired = (FreeformAnalysisTable.Count > 0);
                    });
            }
            catch (Exception ex)
            {
                TLogging.LogException(ex, Utilities.GetMethodSignature());
                throw;
            }

            db.CloseDBConnection();

            return AccountAnalysisAttributeValueRequired;
        }

        //
        //    Rename Account: to rename an AccountCode or a CostCentreCode, we need to update lots of values all over the database:
        private static void UpdateAccountField(String ATblName,
            String AFldName,
            String AOldName,
            String ANewName,
            Int32 ALedgerNumber,
            TDBTransaction ATransaction,
            ref String AttemptedOperation)
        {
            #region Validate Arguments

            //Ledger cam be -1 in this context
            if (ALedgerNumber < -1)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than or equal to -1!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }
            else if (ATransaction == null)
            {
                throw new EFinanceSystemDBTransactionNullException(String.Format(Catalog.GetString(
                            "Function:{0} - Database Transaction must not be NULL!"),
                        Utilities.GetMethodName(true)));
            }
            else if (ATblName.Length == 0)
            {
                throw new ArgumentException(String.Format(Catalog.GetString("Function:{0} - The table name to alter is empty!"),
                        Utilities.GetMethodName(true)));
            }
            else if (AFldName.Length == 0)
            {
                throw new ArgumentException(String.Format(Catalog.GetString("Function:{0} - The field name to change is empty!"),
                        Utilities.GetMethodName(true)));
            }
            else if (AOldName.Length == 0)
            {
                throw new ArgumentException(String.Format(Catalog.GetString("Function:{0} - The old name is empty!"),
                        Utilities.GetMethodName(true)));
            }
            else if (ANewName.Length == 0)
            {
                throw new ArgumentException(String.Format(Catalog.GetString("Function:{0} - The new name is empty!"),
                        Utilities.GetMethodName(true)));
            }

            #endregion Validate Arguments

            AttemptedOperation = String.Format("Rename {0} in {1}", AFldName, ATblName);

            String QuerySql = String.Format("UPDATE PUB_{0} SET {1}='{2}' WHERE {1}='{3}'",
                ATblName,
                AFldName,
                ANewName,
                AOldName);

            if (ALedgerNumber >= 0)
            {
                QuerySql += String.Format(" AND {0}={1}", ALedgerTable.GetLedgerNumberDBName(), ALedgerNumber);
            }

            ATransaction.DataBaseObj.ExecuteNonQuery(QuerySql, ATransaction);
        }

        /// <summary>
        /// Use this new account code instead of that old one.
        /// THIS RENAMES THE FIELD IN LOTS OF PLACES!
        /// </summary>
        /// <param name="AOldCode"></param>
        /// <param name="ANewCode"></param>
        /// <param name="ALedgerNumber"></param>
        /// <param name="AVerificationResults"></param>
        /// <returns></returns>
        [RequireModulePermission("FINANCE-3")]
        public static bool RenameAccountCode(String AOldCode,
            String ANewCode,
            Int32 ALedgerNumber,
            out TVerificationResultCollection AVerificationResults)
        {
            #region Validate Arguments

            if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }
            else if (AOldCode.Length == 0)
            {
                throw new ArgumentException(String.Format(Catalog.GetString("Function:{0} - The Old Account code is empty!"),
                        Utilities.GetMethodName(true)));
            }
            else if (ANewCode.Length == 0)
            {
                throw new ArgumentException(String.Format(Catalog.GetString("Function:{0} - The New Account code is empty!"),
                        Utilities.GetMethodName(true)));
            }

            #endregion Validate Arguments

            String VerificationContext = "Rename Account Code";
            String AttemptedOperation = string.Empty;

            TVerificationResultCollection VerificationResults = null;

            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("RenameAccountCode");
            bool SubmissionOK = false;

            try
            {
                db.WriteTransaction(
                    ref Transaction,
                    ref SubmissionOK,
                    delegate
                    {
                        // First check whether this new code is available for use!
                        //
                        AAccountTable TempAccountTbl = AAccountAccess.LoadByPrimaryKey(ALedgerNumber, ANewCode, Transaction);

                        if (TempAccountTbl.Rows.Count > 0)
                        {
                            VerificationResults.Add(new TVerificationResult(VerificationContext, "Target name is already present",
                                    TResultSeverity.Resv_Critical));
                            return;
                        }

                        TempAccountTbl = AAccountAccess.LoadByPrimaryKey(ALedgerNumber, AOldCode, Transaction);

                        if (TempAccountTbl.Rows.Count != 1)
                        {
                            VerificationResults.Add(new TVerificationResult(VerificationContext, "Existing name not accessible",
                                    TResultSeverity.Resv_Critical));
                            return;
                        }

                        AAccountRow PrevAccountRow = TempAccountTbl[0];
                        AAccountRow NewAccountRow = TempAccountTbl.NewRowTyped();
                        DataUtilities.CopyAllColumnValues(PrevAccountRow, NewAccountRow);
                        NewAccountRow.AccountCode = ANewCode;
                        TempAccountTbl.Rows.Add(NewAccountRow);

                        AAccountAccess.SubmitChanges(TempAccountTbl, Transaction);

                        TempAccountTbl.AcceptChanges();

                        UpdateAccountField(ALedgerTable.GetTableDBName(), ALedgerTable.GetCreditorGlAccountCodeDBName(),
                            AOldCode, ANewCode, ALedgerNumber, Transaction, ref AttemptedOperation);
                        UpdateAccountField(ALedgerTable.GetTableDBName(), ALedgerTable.GetDebtorGlAccountCodeDBName(),
                            AOldCode, ANewCode, ALedgerNumber, Transaction, ref AttemptedOperation);
                        UpdateAccountField(ALedgerTable.GetTableDBName(), ALedgerTable.GetFaGlAccountCodeDBName(),
                            AOldCode, ANewCode, ALedgerNumber, Transaction, ref AttemptedOperation);
                        UpdateAccountField(ALedgerTable.GetTableDBName(), ALedgerTable.GetIltGlAccountCodeDBName(),
                            AOldCode, ANewCode, ALedgerNumber, Transaction, ref AttemptedOperation);
                        UpdateAccountField(ALedgerTable.GetTableDBName(), ALedgerTable.GetPoAccrualGlAccountCodeDBName(),
                            AOldCode, ANewCode, ALedgerNumber, Transaction, ref AttemptedOperation);
                        UpdateAccountField(ALedgerTable.GetTableDBName(), ALedgerTable.GetProfitLossGlAccountCodeDBName(),
                            AOldCode, ANewCode, ALedgerNumber, Transaction, ref AttemptedOperation);
                        UpdateAccountField(ALedgerTable.GetTableDBName(), ALedgerTable.GetPurchaseGlAccountCodeDBName(),
                            AOldCode, ANewCode, ALedgerNumber, Transaction, ref AttemptedOperation);
                        UpdateAccountField(ALedgerTable.GetTableDBName(), ALedgerTable.GetSalesGlAccountCodeDBName(),
                            AOldCode, ANewCode, ALedgerNumber, Transaction, ref AttemptedOperation);
                        UpdateAccountField(ALedgerTable.GetTableDBName(), ALedgerTable.GetSoAccrualGlAccountCodeDBName(),
                            AOldCode, ANewCode, ALedgerNumber, Transaction, ref AttemptedOperation);
                        UpdateAccountField(ALedgerTable.GetTableDBName(), ALedgerTable.GetStockAdjGlAccountCodeDBName(),
                            AOldCode, ANewCode, ALedgerNumber, Transaction, ref AttemptedOperation);
                        UpdateAccountField(ALedgerTable.GetTableDBName(), ALedgerTable.GetStockGlAccountCodeDBName(),
                            AOldCode, ANewCode, ALedgerNumber, Transaction, ref AttemptedOperation);
                        UpdateAccountField(ALedgerTable.GetTableDBName(), ALedgerTable.GetTaxInputGlAccountCodeDBName(),
                            AOldCode, ANewCode, ALedgerNumber, Transaction, ref AttemptedOperation);
                        UpdateAccountField(ALedgerTable.GetTableDBName(), ALedgerTable.GetTaxOutputGlAccountCodeDBName(),
                            AOldCode, ANewCode, ALedgerNumber, Transaction, ref AttemptedOperation);
                        UpdateAccountField(ALedgerTable.GetTableDBName(), ALedgerTable.GetCostOfSalesGlAccountDBName(),
                            AOldCode, ANewCode, ALedgerNumber, Transaction, ref AttemptedOperation);
                        UpdateAccountField(ALedgerTable.GetTableDBName(), ALedgerTable.GetForexGainsLossesAccountDBName(),
                            AOldCode, ANewCode, ALedgerNumber, Transaction, ref AttemptedOperation);
                        UpdateAccountField(ALedgerTable.GetTableDBName(), ALedgerTable.GetRetEarningsGlAccountDBName(),
                            AOldCode, ANewCode, ALedgerNumber, Transaction, ref AttemptedOperation);
                        UpdateAccountField(ALedgerTable.GetTableDBName(), ALedgerTable.GetStockAccrualGlAccountDBName(),
                            AOldCode, ANewCode, ALedgerNumber, Transaction, ref AttemptedOperation);
                        UpdateAccountField(ATransactionTable.GetTableDBName(), ATransactionTable.GetAccountCodeDBName(),
                            AOldCode, ANewCode, ALedgerNumber, Transaction, ref AttemptedOperation);
                        UpdateAccountField(ATransactionTable.GetTableDBName(), ATransactionTable.GetPrimaryAccountCodeDBName(),
                            AOldCode, ANewCode, ALedgerNumber, Transaction, ref AttemptedOperation);

                        /*
                         *              UpdateAccountField ("a_this_year_old_transaction","a_account_code_c", AOldCode, ANewCode, ALedgerNumber, Transaction, ref AttemptedOperation);
                         *              UpdateAccountField ("a_this_year_old_transaction","a_primary_account_code_c", AOldCode, ANewCode, ALedgerNumber, Transaction, ref AttemptedOperation);
                         *              UpdateAccountField ("a_previous_year_transaction","a_account_code_c", AOldCode, ANewCode, ALedgerNumber, Transaction, ref AttemptedOperation);
                         *              UpdateAccountField ("a_previous_year_transaction","a_primary_account_code_c", AOldCode, ANewCode, ALedgerNumber, Transaction, ref AttemptedOperation);
                         */
                        UpdateAccountField(AFeesReceivableTable.GetTableDBName(), AFeesReceivableTable.GetAccountCodeDBName(),
                            AOldCode, ANewCode, ALedgerNumber, Transaction, ref AttemptedOperation);
                        UpdateAccountField(AFeesReceivableTable.GetTableDBName(), AFeesReceivableTable.GetDrAccountCodeDBName(),
                            AOldCode, ANewCode, ALedgerNumber, Transaction, ref AttemptedOperation);
                        UpdateAccountField(AFeesPayableTable.GetTableDBName(), AFeesPayableTable.GetAccountCodeDBName(),
                            AOldCode, ANewCode, ALedgerNumber, Transaction, ref AttemptedOperation);
                        UpdateAccountField(AFeesPayableTable.GetTableDBName(), AFeesPayableTable.GetDrAccountCodeDBName(),
                            AOldCode, ANewCode, ALedgerNumber, Transaction, ref AttemptedOperation);
                        UpdateAccountField(ATransactionTypeTable.GetTableDBName(), ATransactionTypeTable.GetBalancingAccountCodeDBName(),
                            AOldCode, ANewCode, ALedgerNumber, Transaction, ref AttemptedOperation);
                        UpdateAccountField(ATransactionTypeTable.GetTableDBName(), ATransactionTypeTable.GetCreditAccountCodeDBName(),
                            AOldCode, ANewCode, ALedgerNumber, Transaction, ref AttemptedOperation);
                        UpdateAccountField(ATransactionTypeTable.GetTableDBName(), ATransactionTypeTable.GetDebitAccountCodeDBName(),
                            AOldCode, ANewCode, ALedgerNumber, Transaction, ref AttemptedOperation);

                        AAnalysisAttributeTable TempAnalAttrTbl = AAnalysisAttributeAccess.LoadViaAAccount(ALedgerNumber, AOldCode, Transaction);
                        Int32 OriginalAttribCount = TempAnalAttrTbl.Rows.Count;

                        for (Int32 Idx = OriginalAttribCount - 1; Idx >= 0; Idx--)
                        {
                            AAnalysisAttributeRow OldAnalAttribRow = (AAnalysisAttributeRow)TempAnalAttrTbl.Rows[Idx];
                            // "a_analysis_attribute"  is the referrent in foreign keys, so I can't just go changing it - I need to make a copy?
                            AAnalysisAttributeRow NewAnalAttribRow = TempAnalAttrTbl.NewRowTyped();
                            DataUtilities.CopyAllColumnValues(OldAnalAttribRow, NewAnalAttribRow);
                            NewAnalAttribRow.AccountCode = ANewCode;
                            TempAnalAttrTbl.Rows.Add(NewAnalAttribRow);
                        }

                        AAnalysisAttributeAccess.SubmitChanges(TempAnalAttrTbl, Transaction);
                        TempAnalAttrTbl.AcceptChanges();

                        UpdateAccountField(ATransAnalAttribTable.GetTableDBName(), ATransAnalAttribTable.GetAccountCodeDBName(),
                            AOldCode, ANewCode, ALedgerNumber, Transaction, ref AttemptedOperation);
                        UpdateAccountField(ARecurringTransAnalAttribTable.GetTableDBName(), ARecurringTransAnalAttribTable.GetAccountCodeDBName(),
                            AOldCode, ANewCode, ALedgerNumber, Transaction, ref AttemptedOperation);
                        UpdateAccountField(AApAnalAttribTable.GetTableDBName(), AApAnalAttribTable.GetAccountCodeDBName(),
                            AOldCode, ANewCode, ALedgerNumber, Transaction, ref AttemptedOperation);

                        for (Int32 Idx = OriginalAttribCount - 1; Idx >= 0; Idx--)
                        {
                            AAnalysisAttributeRow OldAnalAttribRow = (AAnalysisAttributeRow)TempAnalAttrTbl.Rows[Idx];
                            OldAnalAttribRow.Delete();
                        }

                        AAnalysisAttributeAccess.SubmitChanges(TempAnalAttrTbl, Transaction);
                        TempAnalAttrTbl.AcceptChanges();

                        UpdateAccountField(ASuspenseAccountTable.GetTableDBName(), ASuspenseAccountTable.GetSuspenseAccountCodeDBName(),
                            AOldCode, ANewCode, ALedgerNumber, Transaction, ref AttemptedOperation);
                        UpdateAccountField(AMotivationDetailTable.GetTableDBName(), AMotivationDetailTable.GetAccountCodeDBName(),
                            AOldCode, ANewCode, ALedgerNumber, Transaction, ref AttemptedOperation);
                        UpdateAccountField(ARecurringTransactionTable.GetTableDBName(), ARecurringTransactionTable.GetAccountCodeDBName(),
                            AOldCode, ANewCode, ALedgerNumber, Transaction, ref AttemptedOperation);
                        UpdateAccountField(AGiftBatchTable.GetTableDBName(), AGiftBatchTable.GetBankAccountCodeDBName(),
                            AOldCode, ANewCode, ALedgerNumber, Transaction, ref AttemptedOperation);
                        UpdateAccountField(ARecurringGiftBatchTable.GetTableDBName(), ARecurringGiftBatchTable.GetBankAccountCodeDBName(),
                            AOldCode, ANewCode, ALedgerNumber, Transaction, ref AttemptedOperation);
                        UpdateAccountField(AApDocumentDetailTable.GetTableDBName(), AApDocumentDetailTable.GetAccountCodeDBName(),
                            AOldCode, ANewCode, ALedgerNumber, Transaction, ref AttemptedOperation);
                        UpdateAccountField(AApDocumentTable.GetTableDBName(), AApDocumentTable.GetApAccountDBName(),
                            AOldCode, ANewCode, ALedgerNumber, Transaction, ref AttemptedOperation);
                        UpdateAccountField(AApPaymentTable.GetTableDBName(), AApPaymentTable.GetBankAccountDBName(),
                            AOldCode, ANewCode, ALedgerNumber, Transaction, ref AttemptedOperation);
                        UpdateAccountField(AEpPaymentTable.GetTableDBName(), AEpPaymentTable.GetBankAccountDBName(),
                            AOldCode, ANewCode, ALedgerNumber, Transaction, ref AttemptedOperation);

                        UpdateAccountField(AApSupplierTable.GetTableDBName(), AApSupplierTable.GetDefaultApAccountDBName(),
                            AOldCode, ANewCode, -1, Transaction, ref AttemptedOperation); // There's no Ledger associated with this field.

                        UpdateAccountField(ABudgetTable.GetTableDBName(), ABudgetTable.GetAccountCodeDBName(),
                            AOldCode, ANewCode, ALedgerNumber, Transaction, ref AttemptedOperation);
                        UpdateAccountField(AGeneralLedgerMasterTable.GetTableDBName(), AGeneralLedgerMasterTable.GetAccountCodeDBName(),
                            AOldCode, ANewCode, ALedgerNumber, Transaction, ref AttemptedOperation);
                        UpdateAccountField(AAccountHierarchyDetailTable.GetTableDBName(), AAccountHierarchyDetailTable.GetReportingAccountCodeDBName(),
                            AOldCode, ANewCode, ALedgerNumber, Transaction, ref AttemptedOperation);
                        UpdateAccountField(AAccountHierarchyDetailTable.GetTableDBName(), AAccountHierarchyDetailTable.GetAccountCodeToReportToDBName(),
                            AOldCode, ANewCode, ALedgerNumber, Transaction, ref AttemptedOperation);
                        UpdateAccountField(AAccountHierarchyTable.GetTableDBName(), AAccountHierarchyTable.GetRootAccountCodeDBName(),
                            AOldCode, ANewCode, ALedgerNumber, Transaction, ref AttemptedOperation);
                        UpdateAccountField(AAccountPropertyTable.GetTableDBName(), AAccountPropertyTable.GetAccountCodeDBName(),
                            AOldCode, ANewCode, ALedgerNumber, Transaction, ref AttemptedOperation);
                        //              UpdateAccountField("a_fin_statement_group","a_account_code_c", AOldCode, ANewCode, ALedgerNumber, Transaction, ref AttemptedOperation);

                        PrevAccountRow.Delete();

                        AAccountAccess.SubmitChanges(TempAccountTbl, Transaction);

                        SubmissionOK = true;
                    });
            }
            catch (Exception ex)
            {
                TLogging.LogException(ex, Utilities.GetMethodSignature());
                throw;
            }

            db.CloseDBConnection();

            AVerificationResults = VerificationResults;

            return SubmissionOK;
        }

        private static bool CostCentreHasChildren(Int32 ALedgerNumber, string ACostCentreCode, TDBTransaction ATransaction)
        {
            #region Validate Arguments

            if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }
            else if (ATransaction == null)
            {
                throw new EFinanceSystemDBTransactionNullException(String.Format(Catalog.GetString(
                            "Function:{0} - Database Transaction must not be NULL!"),
                        Utilities.GetMethodName(true)));
            }
            else if (ACostCentreCode.Length == 0)
            {
                throw new ArgumentException(String.Format(Catalog.GetString("Function:{0} - The Cost Centre code is empty!"),
                        Utilities.GetMethodName(true)));
            }

            #endregion Validate Arguments

            String QuerySql = String.Format("SELECT COUNT (*) FROM PUB_{0} WHERE {1}={2} AND {3}='{4}'",
                ACostCentreTable.GetTableDBName(),
                ACostCentreTable.GetLedgerNumberDBName(),
                ALedgerNumber,
                ACostCentreTable.GetCostCentreToReportToDBName(),
                ACostCentreCode);

            object SqlResult = ATransaction.DataBaseObj.ExecuteScalar(QuerySql, ATransaction);

            return Convert.ToInt32(SqlResult) > 0;
        }

        /// <summary>I can add child accounts to this account if it's a summary Cost Centre,
        ///          or if there have never been transactions posted to it,
        ///          or if it's linked to a partner.
        ///
        ///          (If children are added to this Cost Centre, it will be promoted to a summary Cost Centre.)
        ///
        ///          But I can't add to 'ILT', or the children of 'ILT'.
        ///
        ///          I can delete this Cost Centre if it has no transactions posted as above,
        ///          AND it has no children.
        ///          But I can't delete System Cost Centres.
        /// </summary>
        /// <returns>true if the attributes were found.</returns>
        [RequireModulePermission("FINANCE-1")]
        public static Boolean GetCostCentreAttributes(Int32 ALedgerNumber,
            String ACostCentreCode,
            out bool ACanBeParent,
            out bool ACanDelete,
            out String AMsg)
        {
            #region Validate Arguments

            if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }
            else if (ACostCentreCode.Length == 0)
            {
                throw new ArgumentException(String.Format(Catalog.GetString("Function:{0} - The Cost Centre code is empty!"),
                        Utilities.GetMethodName(true)));
            }

            #endregion Validate Arguments

            Boolean CanBeParent = false;
            Boolean CanDelete = false;
            String Msg = string.Empty;
            bool DBSuccess = true;

            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("GetCostCentreAttributes");

            try
            {
                db.ReadTransaction(ref Transaction,
                    delegate
                    {
                        ACostCentreTable tempTbl = ACostCentreAccess.LoadByPrimaryKey(ALedgerNumber, ACostCentreCode, Transaction);

                        #region Validate Data

                        if ((tempTbl == null) || (tempTbl.Count == 0))
                        {
                            DBSuccess = false;
                            CanBeParent = false;
                            CanDelete = false;
                            Msg = String.Format(Catalog.GetString("Cost Centre Code {0} is not in the database."), ACostCentreCode);
                            return;
                        }

                        #endregion Validate Data

                        ACostCentreRow CostCentreRow = tempTbl[0];

                        TSystemDefaults SystemDefaults = new TSystemDefaults(db);
                        if (SystemDefaults.GetBooleanDefault(
                            SharedConstants.SYSDEFAULT_ILTPROCESSINGENABLED, false) == true)
                        {
                            // Don't allow any cost centres to be added to ILT or its children.
                            if ((CostCentreRow.CostCentreCode == MFinanceConstants.INTER_LEDGER_HEADING)
                                || (CostCentreRow.CostCentreToReportTo == MFinanceConstants.INTER_LEDGER_HEADING))
                            {
                                CanBeParent = false;
                                CanDelete = false;
                                Msg = Catalog.GetString("Cost Centres in ILT cannot have children.");
                                return;
                            }
                        }

                        bool isParent = CostCentreHasChildren(ALedgerNumber, ACostCentreCode, Transaction);
                        CanBeParent = !CostCentreRow.PostingCostCentreFlag; // If it's a summary Cost Centre, it's OK (This shouldn't happen either, because the client shouldn't ask me!)

                        if (isParent)
                        {
                            CanDelete = false;
                            Msg = Catalog.GetString("Cost Centre has children.");
                        }
                        else
                        {
                            CanDelete = true;
                        }

                        if (!CanBeParent || CanDelete)
                        {
                            string sql = "SELECT COUNT(*) FROM PUB_a_transaction " +
                                "WHERE a_ledger_number_i = " + ALedgerNumber.ToString() + " " +
                                "AND a_cost_centre_code_c = '" + ACostCentreCode + "'";
                            Int32 refs = Convert.ToInt32(db.ExecuteScalar(sql, Transaction));

                            bool isInUse = (refs > 0);

                            if (isInUse)
                            {
                                CanBeParent = false;
                                CanDelete = false;      // Once it has transactions posted, I can't delete it, ever.
                                Msg = Catalog.GetString("Cost Centre is referenced in transactions.");
                            }
                            else
                            {
                                CanBeParent = true;    // For posting Cost Centres, I can still add children (and change the Cost Centre to summary) if there's nothing posted to it yet.
                            }
                        }
                    }); // End of ReadTransaction with anonymous function
            }
            catch (Exception ex)
            {
                TLogging.LogException(ex, Utilities.GetMethodSignature());
                throw;
            }

            db.CloseDBConnection();

            ACanBeParent = CanBeParent;
            ACanDelete = CanDelete;
            AMsg = Msg;

            return DBSuccess;
        }

        /// <summary>
        /// Use this new Cost Centre code instead of that old one.
        /// THIS RENAMES THE FIELD IN LOTS OF PLACES!
        /// </summary>
        /// <param name="AOldCode"></param>
        /// <param name="ANewCode"></param>
        /// <param name="ALedgerNumber"></param>
        /// <param name="AVerificationResults"></param>
        /// <returns></returns>
        [RequireModulePermission("FINANCE-3")]
        public static bool RenameCostCentreCode(String AOldCode,
            String ANewCode,
            Int32 ALedgerNumber,
            out TVerificationResultCollection AVerificationResults)
        {
            #region Validate Arguments

            if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }
            else if (AOldCode.Length == 0)
            {
                throw new ArgumentException(String.Format(Catalog.GetString("Function:{0} - The Old Cost Centre code is empty!"),
                        Utilities.GetMethodName(true)));
            }
            else if (ANewCode.Length == 0)
            {
                throw new ArgumentException(String.Format(Catalog.GetString("Function:{0} - The New Cost Centre code is empty!"),
                        Utilities.GetMethodName(true)));
            }

            #endregion Validate Arguments

            String VerificationContext = "Rename Cost Centre Code";
            String AttemptedOperation = string.Empty;

            TVerificationResultCollection VerificationResults = new TVerificationResultCollection();

            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("RenameCostCentreCode");
            bool SubmissionOK = false;

            try
            {
                db.WriteTransaction(
                    ref Transaction,
                    ref SubmissionOK,
                    delegate
                    {
                        //
                        // First check whether this new code is available for use!
                        // (Check that the old name exists, and the new name doesn't!)
                        //
                        ACostCentreTable tempTbl = ACostCentreAccess.LoadByPrimaryKey(ALedgerNumber, ANewCode, Transaction);

                        if (tempTbl.Rows.Count > 0)
                        {
                            VerificationResults.Add(new TVerificationResult(VerificationContext, "Target name is already present",
                                    TResultSeverity.Resv_Critical));
                            return;
                        }

                        tempTbl = ACostCentreAccess.LoadByPrimaryKey(ALedgerNumber, AOldCode, Transaction);

                        if (tempTbl.Rows.Count != 1)
                        {
                            VerificationResults.Add(new TVerificationResult(VerificationContext, "Existing name not accessible",
                                    TResultSeverity.Resv_Critical));
                            return;
                        }

                        ACostCentreRow prevRow = tempTbl[0];

                        if (prevRow.SystemCostCentreFlag)
                        {
                            VerificationResults.Add(new TVerificationResult(VerificationContext,
                                    String.Format("Cannot rename System Cost Centre {0}.", AOldCode),
                                    TResultSeverity.Resv_Critical));
                            return;
                        }

                        // I can't just rename this,
                        // because lots of tables rely on this entry and I'll break their foreign constraints.
                        // I need to create a new row, point everyone to that, then delete the current row.
                        //
                        ACostCentreRow newRow = tempTbl.NewRowTyped();
                        DataUtilities.CopyAllColumnValues(prevRow, newRow);
                        newRow.CostCentreCode = ANewCode;
                        tempTbl.Rows.Add(newRow);

                        ACostCentreAccess.SubmitChanges(tempTbl, Transaction);

                        tempTbl.AcceptChanges();

                        UpdateAccountField(ACostCentreTable.GetTableDBName(), ACostCentreTable.GetCostCentreToReportToDBName(),
                            AOldCode, ANewCode, ALedgerNumber, Transaction, ref AttemptedOperation);
                        UpdateAccountField(ATransactionTable.GetTableDBName(), ATransactionTable.GetCostCentreCodeDBName(),
                            AOldCode, ANewCode, ALedgerNumber, Transaction, ref AttemptedOperation);
                        UpdateAccountField(ARecurringTransactionTable.GetTableDBName(), ARecurringTransactionTable.GetCostCentreCodeDBName(),
                            AOldCode, ANewCode, ALedgerNumber, Transaction, ref AttemptedOperation);
                        UpdateAccountField(AValidLedgerNumberTable.GetTableDBName(), AValidLedgerNumberTable.GetCostCentreCodeDBName(),
                            AOldCode, ANewCode, ALedgerNumber, Transaction, ref AttemptedOperation);
                        UpdateAccountField(AMotivationDetailTable.GetTableDBName(), AMotivationDetailTable.GetCostCentreCodeDBName(),
                            AOldCode, ANewCode, ALedgerNumber, Transaction, ref AttemptedOperation);
                        UpdateAccountField(AFeesReceivableTable.GetTableDBName(), AFeesReceivableTable.GetCostCentreCodeDBName(),
                            AOldCode, ANewCode, ALedgerNumber, Transaction, ref AttemptedOperation);
                        UpdateAccountField(AFeesPayableTable.GetTableDBName(), AFeesPayableTable.GetCostCentreCodeDBName(),
                            AOldCode, ANewCode, ALedgerNumber, Transaction, ref AttemptedOperation);
                        UpdateAccountField(AGiftBatchTable.GetTableDBName(), AGiftBatchTable.GetBankCostCentreDBName(),
                            AOldCode, ANewCode, ALedgerNumber, Transaction, ref AttemptedOperation);
                        UpdateAccountField(AGiftDetailTable.GetTableDBName(), AGiftDetailTable.GetCostCentreCodeDBName(),
                            AOldCode, ANewCode, ALedgerNumber, Transaction, ref AttemptedOperation);
                        UpdateAccountField(ARecurringGiftBatchTable.GetTableDBName(), ARecurringGiftBatchTable.GetBankCostCentreDBName(),
                            AOldCode, ANewCode, ALedgerNumber, Transaction, ref AttemptedOperation);
                        UpdateAccountField(AApDocumentDetailTable.GetTableDBName(), AApDocumentDetailTable.GetCostCentreCodeDBName(),
                            AOldCode, ANewCode, ALedgerNumber, Transaction, ref AttemptedOperation);
                        UpdateAccountField(AProcessedFeeTable.GetTableDBName(), AProcessedFeeTable.GetCostCentreCodeDBName(),
                            AOldCode, ANewCode, ALedgerNumber, Transaction, ref AttemptedOperation);
                        UpdateAccountField(ABudgetTable.GetTableDBName(), ABudgetTable.GetCostCentreCodeDBName(),
                            AOldCode, ANewCode, ALedgerNumber, Transaction, ref AttemptedOperation);
                        UpdateAccountField(AGeneralLedgerMasterTable.GetTableDBName(), AGeneralLedgerMasterTable.GetCostCentreCodeDBName(),
                            AOldCode, ANewCode, ALedgerNumber, Transaction, ref AttemptedOperation);

                        prevRow.Delete();

                        ACostCentreAccess.SubmitChanges(tempTbl, Transaction);

                        SubmissionOK = true;
                    });
            }
            catch (Exception ex)
            {
                TLogging.LogException(ex, Utilities.GetMethodSignature());
                throw;
            }

            db.CloseDBConnection();

            AVerificationResults = VerificationResults;

            return SubmissionOK;
        } // RenameCostCentreCode

        /// <summary>
        /// Checks an account can be made a foreign currency account.
        /// </summary>
        /// <param name="ALedgerNumber"></param>
        /// <param name="AAccountCode"></param>
        /// <returns></returns>
        [RequireModulePermission("FINANCE-1")]
        public static bool CheckAccountCanBeMadeForeign(Int32 ALedgerNumber, string AAccountCode)
        {
            #region Validate Arguments

            if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }
            else if (AAccountCode.Length == 0)
            {
                throw new ArgumentException(String.Format(Catalog.GetString("Function:{0} - The Account code is empty!"),
                        Utilities.GetMethodName(true)));
            }

            #endregion Validate Arguments

            bool ReturnValue = true;

            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("CheckAccountCanBeMadeForeign");

            try
            {
                db.ReadTransaction(
                    ref Transaction,
                    delegate
                    {
                        //For readability
                        //               "SELECT * FROM a_general_ledger_master " +
                        //               "WHERE a_general_ledger_master.a_ledger_number_i = " + ALedgerNumber +
                        //               " AND a_general_ledger_master.a_account_code_c = '" + AAccountCode + "'" +
                        //               " AND a_general_ledger_master.a_cost_centre_code_c = '[" + ALedgerNumber + "]'" +
                        //               " AND EXISTS (SELECT * FROM a_general_ledger_master_period " +
                        //               "WHERE a_general_ledger_master_period.a_glm_sequence_i = a_general_ledger_master.a_glm_sequence_i" +
                        //               " AND a_general_ledger_master_period.a_actual_base_n <> 0)";
                        string Query = String.Format("SELECT * FROM {0} " +
                            "WHERE {0}.{1} = {2}" +
                            " AND {0}.{3} = '{4}'" +
                            " AND {0}.{5} = '[{2}]'" +
                            " AND EXISTS (SELECT * FROM {6} " +
                            "  WHERE {6}.{7} = {0}.{8}" +
                            "   AND {6}.{9} <> 0)",
                            AGeneralLedgerMasterTable.GetTableDBName(),
                            AGeneralLedgerMasterTable.GetLedgerNumberDBName(),
                            ALedgerNumber,
                            AGeneralLedgerMasterTable.GetAccountCodeDBName(),
                            AAccountCode,
                            AGeneralLedgerMasterTable.GetCostCentreCodeDBName(),
                            AGeneralLedgerMasterPeriodTable.GetTableDBName(),
                            AGeneralLedgerMasterPeriodTable.GetGlmSequenceDBName(),
                            AGeneralLedgerMasterTable.GetGlmSequenceDBName(),
                            AGeneralLedgerMasterPeriodTable.GetActualBaseDBName());

                        DataTable dT = Transaction.DataBaseObj.SelectDT(Query, "DataTable", Transaction);

                        if ((dT != null) && (dT.Rows.Count > 0))
                        {
                            ReturnValue = false;
                        }
                    });
            }
            catch (Exception ex)
            {
                TLogging.LogException(ex, Utilities.GetMethodSignature());
                throw;
            }

            db.CloseDBConnection();

            return ReturnValue;
        }

        /// <summary>
        /// Checks if a foreign currency account has no balances.
        /// </summary>
        /// <param name="ALedgerNumber"></param>
        /// <param name="AYear"></param>
        /// <param name="AAccountCode"></param>
        /// <returns>True if balances exist</returns>
        [RequireModulePermission("FINANCE-1")]
        public static bool CheckForeignAccountHasBalances(Int32 ALedgerNumber, Int32 AYear, string AAccountCode)
        {
            #region Validate Arguments

            if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }
            else if (AAccountCode.Length == 0)
            {
                throw new ArgumentException(String.Format(Catalog.GetString("Function:{0} - The Account code is empty!"),
                        Utilities.GetMethodName(true)));
            }

            #endregion Validate Arguments

            bool ReturnValue = false;

            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("CheckForeignAccountHasBalances");

            try
            {
                db.ReadTransaction(
                    ref Transaction,
                    delegate
                    {
                        AGeneralLedgerMasterTable GeneralLedgerMasterTable = AGeneralLedgerMasterAccess.LoadByUniqueKey(
                            ALedgerNumber, AYear, AAccountCode, "[" + ALedgerNumber + "]", Transaction);

                        {
                            if ((GeneralLedgerMasterTable != null) && (GeneralLedgerMasterTable.Rows.Count > 0))
                            {
                                if (!GeneralLedgerMasterTable[0].IsYtdActualForeignNull() && (GeneralLedgerMasterTable[0].YtdActualForeign != 0))
                                {
                                    ReturnValue = true;
                                }
                            }
                        }
                    });
            }
            catch (Exception ex)
            {
                TLogging.LogException(ex, Utilities.GetMethodSignature());
                throw;
            }

            db.CloseDBConnection();

            return ReturnValue;
        }

        /// <summary>
        /// Makes all foreign currency balances zero for the given account on the given year for all posting cost centres
        /// </summary>
        /// <param name="ALedgerNumber"></param>
        /// <param name="AYear"></param>
        /// <param name="AAccountCode"></param>
        /// <returns>True if successful</returns>
        [RequireModulePermission("FINANCE-1")]
        public static bool ZeroForeignCurrencyBalances(Int32 ALedgerNumber, Int32 AYear, string[] AAccountCode)
        {
            #region Validate Arguments

            if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }
            else if (AAccountCode.Length == 0)
            {
                return true;
            }

            #endregion Validate Arguments

            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("ZeroForeignCurrencyBalances");
            bool SubmissionOK = false;

            db.WriteTransaction(ref Transaction, ref SubmissionOK,
                delegate
                {
                    foreach (string AccountCode in AAccountCode)
                    {
                        //               "SELECT a_general_ledger_master.* " +
                        //               "FROM a_general_ledger_master, a_cost_centre " +
                        //               "WHERE a_general_ledger_master.a_ledger_number_i = " + ALedgerNumber +
                        //               " AND a_general_ledger_master.a_account_code_c = '" + AccountCode + "'" +
                        //               " AND a_general_ledger_master.a_year_i = " + AYear +
                        //               " AND a_cost_centre.a_cost_centre_code_c = a_general_ledger_master.a_cost_centre_code_c" +
                        //               " AND a_cost_centre.a_ledger_number_i = " + ALedgerNumber +
                        //               " AND a_cost_centre.a_posting_cost_centre_flag_l = true";
                        string Query = String.Format("SELECT {0}.* " +
                            "FROM {0}, {1} " +
                            "WHERE {0}.{2} = {3}" +
                            " AND {0}.{4} = '{5}'" +
                            " AND {0}.{6} = {7}" +
                            " AND {1}.{8} = {0}.{9}" +
                            " AND {1}.{10} = {3}" +
                            " AND {1}.{11} = true",
                            AGeneralLedgerMasterTable.GetTableDBName(),
                            ACostCentreTable.GetTableDBName(),
                            AGeneralLedgerMasterTable.GetLedgerNumberDBName(),
                            ALedgerNumber,
                            AGeneralLedgerMasterTable.GetAccountCodeDBName(),
                            AccountCode,
                            AGeneralLedgerMasterTable.GetYearDBName(),
                            AYear,
                            ACostCentreTable.GetCostCentreCodeDBName(),
                            AGeneralLedgerMasterTable.GetCostCentreCodeDBName(),
                            ACostCentreTable.GetLedgerNumberDBName(),
                            ACostCentreTable.GetPostingCostCentreFlagDBName());

                        AGeneralLedgerMasterTable GeneralLedgerMasterTable = new AGeneralLedgerMasterTable();
                        Transaction.DataBaseObj.SelectDT(GeneralLedgerMasterTable, Query, Transaction);

                        foreach (DataRow Row in GeneralLedgerMasterTable.Rows)
                        {
                            Row[AGeneralLedgerMasterTable.GetYtdActualForeignDBName()] = 0;
                            Row[AGeneralLedgerMasterTable.GetStartBalanceForeignDBName()] = 0;

                            AGeneralLedgerMasterPeriodTable GeneralLedgerMasterPeriodTable =
                                AGeneralLedgerMasterPeriodAccess.LoadViaAGeneralLedgerMaster((int)Row[AGeneralLedgerMasterTable.GetGlmSequenceDBName()
                                    ], Transaction);

                            foreach (AGeneralLedgerMasterPeriodRow GeneralLedgerMasterPeriodRow in GeneralLedgerMasterPeriodTable.Rows)
                            {
                                GeneralLedgerMasterPeriodRow.ActualForeign = 0;
                            }

                            AGeneralLedgerMasterPeriodAccess.SubmitChanges(GeneralLedgerMasterPeriodTable, Transaction);
                        }

                        AGeneralLedgerMasterAccess.SubmitChanges(GeneralLedgerMasterTable, Transaction);
                    }

                    SubmissionOK = true;
                });

            db.CloseDBConnection();

            return SubmissionOK;
        }

        #region Data Validation

        static partial void ValidateAAnalysisType(ref TVerificationResultCollection AVerificationResult, TTypedDataTable ASubmitTable);
        static partial void ValidateAAnalysisTypeManual(ref TVerificationResultCollection AVerificationResult, TTypedDataTable ASubmitTable);

        #endregion Data Validation
    } // TGLSetupWebConnector
} // namespace
