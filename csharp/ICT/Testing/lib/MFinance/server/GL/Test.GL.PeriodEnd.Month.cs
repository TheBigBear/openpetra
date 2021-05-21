﻿//
// DO NOT REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
//
// @Authors:
//       wolfgangu, timop
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

using System;
using System.IO;
using System.Collections;
using System.Data.Odbc;
using NUnit.Framework;
using Ict.Testing.NUnitTools;
using Ict.Testing.NUnitPetraServer;
using Ict.Petra.Server.MFinance.GL;
using Ict.Petra.Server.MFinance.Common;
using Ict.Common.Verification;

using Ict.Petra.Server.MFinance.Account.Data.Access;
using Ict.Petra.Shared.MFinance.Account.Data;
using Ict.Petra.Server.MFinance.GL.WebConnectors;
using Ict.Petra.Shared.MCommon.Data;
using Ict.Petra.Server.MCommon.Data.Access;


using Ict.Common;
using Ict.Common.DB;
using Ict.Petra.Server.MFinance.Gift.Data.Access;
using Ict.Petra.Server.MPartner.Partner.Data.Access;
using Ict.Petra.Shared;
using Ict.Petra.Shared.MFinance;
using Ict.Petra.Shared.MFinance.Gift.Data;
using Ict.Petra.Shared.MFinance.GL.Data;
using Ict.Petra.Shared.MPartner.Partner.Data;
using Ict.Petra.Server.MFinance.Gift.WebConnectors;
using Ict.Petra.Server.MFinance.Gift;
using System.Collections.Generic;

namespace Ict.Testing.Petra.Server.MFinance.GL
{
    /// <summary>
    /// Test of the GL.PeriodEnd.Month routines ...
    /// </summary>
    [TestFixture]
    public class TestGLPeriodicEndMonth
    {
        private int FLedgerNumber;
        private TLedgerInfo FledgerInfo;

        /// <summary>
        /// Tests if unposted batches are detected correctly
        /// </summary>
        [Test]
        public void Test_PEMM_02_UnpostedBatches()
        {
            // System.Diagnostics.Debug.WriteLine(
            UnloadTestData_GetBatchInfo();
            Assert.AreEqual(0, new GetBatchInfo(
                    FLedgerNumber, FledgerInfo.CurrentFinancialYear, FledgerInfo.CurrentPeriod).NumberOfBatches, "No unposted batch shall be found");

            LoadTestData_GetBatchInfo();

            Assert.AreEqual(2, new GetBatchInfo(
                    FLedgerNumber,
                    FledgerInfo.CurrentFinancialYear,
                    FledgerInfo.CurrentPeriod).NumberOfBatches, "Two of the four batches shall be found");
            //UnloadTestData_GetBatchInfo();

            TVerificationResultCollection verificationResult;
            List <Int32>glBatchNumbers;
            Boolean stewardshipBatch;

            bool blnHasErrors = !TPeriodIntervalConnector.PeriodMonthEnd(
                FLedgerNumber, true,
                out glBatchNumbers,
                out stewardshipBatch,
                out verificationResult);
            bool blnStatusArrived = false;

            for (int i = 0; i < verificationResult.Count; ++i)
            {
                if (verificationResult[i].ResultCode.Equals(
                        TPeriodEndErrorAndStatusCodes.PEEC_06.ToString()))
                {
                    blnStatusArrived = true;
                    Assert.IsTrue(verificationResult[i].ResultSeverity == TResultSeverity.Resv_Critical,
                        "Value shall be of type critical ...");
                }
            }

            Assert.IsTrue(blnStatusArrived, "Status message hase been shown");
            Assert.IsTrue(blnHasErrors, "This is a Critical Message");
            UnloadTestData_GetBatchInfo();
        }

        /// <summary>
        /// Tests if suspended accounts are detected correctly
        /// </summary>
        [Test]
        public void Test_PEMM_03_SuspensedAccounts()
        {
            TCommonAccountingTool commonAccountingTool =
                new TCommonAccountingTool(FLedgerNumber, "NUNIT");

            commonAccountingTool.AddBaseCurrencyJournal();
            commonAccountingTool.JournalDescription = "Test Data accounts";
            string strAccountBank = "6000";
            // Accounting of some gifts ...
            commonAccountingTool.AddBaseCurrencyTransaction(
                strAccountBank, "7300", "Gift Example", "Debit", MFinanceConstants.IS_DEBIT, 100);
            commonAccountingTool.AddBaseCurrencyTransaction(
                "0100", "7300", "Gift Example", "Credit", MFinanceConstants.IS_CREDIT, 100);
            commonAccountingTool.CloseSaveAndPost(); // returns true if posting seemed to work

            new ChangeSuspenseAccount(FLedgerNumber, strAccountBank).Suspense();

            TVerificationResultCollection verificationResult;
            List <Int32>glBatchNumbers;
            Boolean stewardshipBatch;

            bool blnHasErrors = !TPeriodIntervalConnector.PeriodMonthEnd(    // Changed to InfoMode because
                FLedgerNumber, true,                                        // the suspense accounts warning is now shown only in InfoMode.
                out glBatchNumbers,
                out stewardshipBatch,
                out verificationResult);
            bool blnStatusArrived = false;

            for (int i = 0; i < verificationResult.Count; ++i)
            {
                if (verificationResult[i].ResultCode.Equals(
                        TPeriodEndErrorAndStatusCodes.PEEC_07.ToString()))
                {
                    blnStatusArrived = true;
                    Assert.AreEqual(TResultSeverity.Resv_Status, verificationResult[i].ResultSeverity,
                        "MonthEnd verificationResult should be status only ...");
                }
            }

            Assert.IsTrue(blnStatusArrived, "MonthEnd status message PEEC_07 has been shown");
            Assert.IsFalse(blnHasErrors, "there should not be an error for closing the first period");

            int periodCounter = 1;

            //TODO: Calendar vs Financial Date Handling - Check if this should not assume 12 but rather use number of financial periods in ledger
            while (!blnHasErrors && periodCounter < 12)
            {
                blnHasErrors = !TPeriodIntervalConnector.PeriodMonthEnd(
                    FLedgerNumber, false,
                    out glBatchNumbers,
                    out stewardshipBatch,
                    out verificationResult);

                Assert.IsFalse(blnHasErrors, "there was an error closing period " + periodCounter.ToString());
                periodCounter++;
                Assert.AreEqual(periodCounter, new TLedgerInfo(FLedgerNumber).CurrentPeriod, "should be in new period");
            }

            blnHasErrors = !TPeriodIntervalConnector.PeriodMonthEnd(
                FLedgerNumber, false,
                out glBatchNumbers,
                out stewardshipBatch,
                out verificationResult);

            blnStatusArrived = false;

            for (int i = 0; i < verificationResult.Count; ++i)
            {
                if (verificationResult[i].ResultCode.Equals(
                        TPeriodEndErrorAndStatusCodes.PEEC_07.ToString())
                    && (TResultSeverity.Resv_Critical == verificationResult[i].ResultSeverity))
                {
                    blnStatusArrived = true;
                }
            }

            Assert.IsTrue(blnStatusArrived, "there should be  a critical error PEEC_07 for the suspense account");
            Assert.IsTrue(blnHasErrors, "there should be an error because we cannot close the last period due to suspense account with a balance");

            new ChangeSuspenseAccount(FLedgerNumber, strAccountBank).Unsuspense();
        }

        private void ImportGiftBatch(DateTime AEffectiveDate)
        {
            TGiftImporting importer = new TGiftImporting();

            string testFile = TAppSettingsManager.GetValue("GiftBatch.file", "../../csharp/ICT/Testing/lib/MFinance/SampleData/sampleGiftBatch.csv");
            StreamReader sr = new StreamReader(testFile);
            string FileContent = sr.ReadToEnd();

            FileContent = FileContent.Replace("{ledgernumber}", FLedgerNumber.ToString());
            FileContent = FileContent.Replace("{thisyear}-01-01", AEffectiveDate.ToString("yyyy-MM-dd"));

            sr.Close();

            Hashtable parameters = new Hashtable();
            parameters.Add("Delimiter", ",");
            parameters.Add("ALedgerNumber", FLedgerNumber);
            parameters.Add("DateFormatString", "yyyy-MM-dd");
            parameters.Add("DatesMayBeIntegers", false);
            parameters.Add("NumberFormat", "American");
            parameters.Add("NewLine", Environment.NewLine);

            TVerificationResultCollection VerificationResult;
            GiftBatchTDSAGiftDetailTable NeedRecipientLedgerNumber;
            bool refreshRequired;

            importer.ImportGiftBatches(parameters, FileContent, out NeedRecipientLedgerNumber, out refreshRequired, out VerificationResult);
            Assert.True(TVerificationHelper.IsNullOrOnlyNonCritical(VerificationResult),
                "Failed to import the test gift batch.  The file contains critical error(s): " + VerificationResult.BuildVerificationResultString());
        }

        /// <summary>
        /// Test for unposted gift batches ...
        /// </summary>
        [Test]
        public void Test_PEMM_04_UnpostedGifts()
        {
            TAccountPeriodInfo getAccountingPeriodInfo =
                new TAccountPeriodInfo(FLedgerNumber, new TLedgerInfo(
                        FLedgerNumber).CurrentPeriod);

            ImportGiftBatch(getAccountingPeriodInfo.PeriodStartDate);

            List <Int32>glBatchNumbers;
            Boolean stewardshipBatch;
            TVerificationResultCollection verificationResult;

            bool blnHasErrors = !TPeriodIntervalConnector.PeriodMonthEnd(
                FLedgerNumber, true,
                out glBatchNumbers,
                out stewardshipBatch,
                out verificationResult);
            bool blnStatusArrived = false;

            for (int i = 0; i < verificationResult.Count; ++i)
            {
                if (verificationResult[i].ResultCode.Equals(
                        TPeriodEndErrorAndStatusCodes.PEEC_08.ToString()))
                {
                    blnStatusArrived = true;
                    Assert.IsTrue(verificationResult[i].ResultSeverity == TResultSeverity.Resv_Critical,
                        "Value shall be of type critical ...");
                }
            }

            Assert.IsTrue(blnStatusArrived, "Message has not been shown");
            Assert.IsTrue(blnHasErrors, "This is a Critical Message");
        }

        /// <summary>
        /// Check for the revaluation status ...
        /// </summary>
        [Test]
        public void Test_PEMM_05_Revaluation()
        {
            FLedgerNumber = CommonNUnitFunctions.CreateNewLedger();
            // load foreign currency account 6001
            CommonNUnitFunctions.LoadTestDataBase("csharp\\ICT\\Testing\\lib\\MFinance\\server\\GL\\" +
                "test-sql\\gl-test-account-data.sql", FLedgerNumber);

            // post a batch for foreign currency account 6001
            TCommonAccountingTool commonAccountingTool =
                new TCommonAccountingTool(FLedgerNumber, "NUNIT");
            commonAccountingTool.AddForeignCurrencyJournal("GBP", 1.1m);
            commonAccountingTool.JournalDescription = "Test foreign currency account";
            string strAccountGift = "0200";
            string strAccountBank = "6001";

            // Accounting of some gifts ...
            commonAccountingTool.AddBaseCurrencyTransaction(
                strAccountBank, (FLedgerNumber * 100).ToString(), "Gift Example", "Debit", MFinanceConstants.IS_DEBIT, 100);

            commonAccountingTool.AddBaseCurrencyTransaction(
                strAccountGift, (FLedgerNumber * 100).ToString(), "Gift Example", "Credit", MFinanceConstants.IS_CREDIT, 100);

            Boolean PostedOk = commonAccountingTool.CloseSaveAndPost(); // returns true if posting seemed to work
            Assert.IsTrue(PostedOk, "Post foreign gift batch");


            TVerificationResultCollection verificationResult;

            /*
             * This error is no longer critical - it's OK to run month end even if a reval is required. (Mantis# 03905)
             *
             *          bool blnHasErrors = !TPeriodIntervalConnector.TPeriodMonthEnd(
             *              FLedgerNumber, true, out verificationResult);
             *
             *          for (int i = 0; i < verificationResult.Count; ++i)
             *          {
             *              if (verificationResult[i].ResultCode.Equals(
             *                      TPeriodEndErrorAndStatusCodes.PEEC_05.ToString()))
             *              {
             *                  blnStatusArrived = true;
             *                  Assert.IsTrue(verificationResult[i].ResultSeverity == TResultSeverity.Resv_Critical,
             *                      "A critical error is required: need to run revaluation first ...");
             *              }
             *          }
             */

            // run revaluation
            Int32 forexBatchNumber;
            List <Int32>glBatchNumbers;
            Boolean stewardshipBatch;
            TLedgerInfo ledgerinfo = new TLedgerInfo(FLedgerNumber);

            Boolean revalueOk = TRevaluationWebConnector.Revaluate(FLedgerNumber,
                new string[] { strAccountGift },
                new string[] { "GBP" },
                new decimal[] { 1.2m },
                ledgerinfo.GetStandardCostCentre(),
                out forexBatchNumber,
                out verificationResult);

            if (!revalueOk)
            {
                TLogging.Log("\n\n\nTRevaluationWebConnector.Revaluate had problems. VerificationResult follows:");
                TLogging.Log(verificationResult.BuildVerificationResultString());
            }

            Assert.IsTrue(revalueOk, "Problem running the revaluation");

            Boolean Err = !TPeriodIntervalConnector.PeriodMonthEnd(
                FLedgerNumber, true,
                out glBatchNumbers,
                out stewardshipBatch,
                out verificationResult);

            if (Err)
            {
                TLogging.Log("\n\n\nTPeriodMonthEnd returned true, VerificationResult follows:");
                TLogging.Log(verificationResult.BuildVerificationResultString());
            }

            Assert.IsFalse(Err, "Should be able to close the month after revaluation has been run.");
        }

        /// <summary>
        /// Move to the next month
        /// </summary>
        [Test]
        public void Test_SwitchToNextMonth()
        {
            FLedgerNumber = CommonNUnitFunctions.CreateNewLedger();
            TLedgerInfo LedgerInfo = new TLedgerInfo(FLedgerNumber);
            int Counter = 0;

            do
            {
                Int32 CurrentPeriod = LedgerInfo.CurrentPeriod;
                ++Counter;
                Assert.Greater(20, Counter, "Too many loops");

                // Set revaluation flag ...
                new TLedgerInitFlag(FLedgerNumber,
                    "Reval").IsSet = true;

                // Run MonthEnd ...
                TVerificationResultCollection verificationResult;
                List <Int32>glBatchNumbers;
                Boolean stewardshipBatch;

                bool blnHasErrors = !TPeriodIntervalConnector.PeriodMonthEnd(
                    FLedgerNumber, false,
                    out glBatchNumbers,
                    out stewardshipBatch,
                    out verificationResult);

                if (!LedgerInfo.ProvisionalYearEndFlag)
                {
                    Assert.AreEqual(CurrentPeriod + 1,
                        LedgerInfo.CurrentPeriod, "Period increment");
                }

                Assert.IsFalse(blnHasErrors, "Month end without any error");
                System.Diagnostics.Debug.WriteLine("Counter: " + Counter.ToString());
            } while (!LedgerInfo.ProvisionalYearEndFlag);
        }

        /// <summary>
        /// TestFixtureSetUp
        /// </summary>
        [OneTimeSetUp]
        public void Init()
        {
            TPetraServerConnector.Connect();
            bool WITH_ILT = true;
            FLedgerNumber = CommonNUnitFunctions.CreateNewLedger(null, WITH_ILT);
            FledgerInfo = new TLedgerInfo(FLedgerNumber);

            System.Diagnostics.Debug.WriteLine("Init: " + this.ToString());
        }

        /// <summary>
        /// TearDown the test
        /// </summary>
        [OneTimeTearDown]
        public void TearDownTest()
        {
            TPetraServerConnector.Disconnect();
            System.Diagnostics.Debug.WriteLine("TearDown: " + this.ToString());
        }

        private const string strTestDataBatchDescription = "TestGLPeriodicEndMonth-TESTDATA";

        private void LoadTestData_GetBatchInfo()
        {
            ABatchRow template = new ABatchTable().NewRowTyped(false);

            template.LedgerNumber = FLedgerNumber;
            template.BatchDescription = strTestDataBatchDescription;

            TDBTransaction transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("LoadTestData_GetBatchInfo");
            ABatchTable batches = null;
            db.ReadTransaction(ref transaction,
                delegate
                {
                    batches = ABatchAccess.LoadUsingTemplate(template, transaction);
                });

            if (batches.Rows.Count == 0)
            {
                CommonNUnitFunctions.LoadTestDataBase("csharp\\ICT\\Testing\\lib\\MFinance\\server\\GL\\" +
                    "test-sql\\gl-test-batch-data.sql", FLedgerNumber);
            }
        }

        private void UnloadTestData_GetBatchInfo()
        {
            OdbcParameter[] ParametersArray;
            ParametersArray = new OdbcParameter[2];
            ParametersArray[0] = new OdbcParameter("", OdbcType.Int);
            ParametersArray[0].Value = FLedgerNumber;
            ParametersArray[1] = new OdbcParameter("", OdbcType.VarChar);
            ParametersArray[1].Value = strTestDataBatchDescription;

            TDBTransaction transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("UnloadTestData_GetBatchInfo");
            bool SubmissionOK = true;
            db.WriteTransaction(ref transaction, ref SubmissionOK,
                delegate
                {
                    string strSQL = "DELETE FROM PUB_" + ABatchTable.GetTableDBName() + " ";
                    strSQL += "WHERE " + ABatchTable.GetLedgerNumberDBName() + " = ? " +
                              "AND " + ABatchTable.GetBatchDescriptionDBName() + " = ? ";
                    db.ExecuteNonQuery(
                        strSQL, transaction, ParametersArray);
                });
            db.CloseDBConnection();
        }
    }

    class ChangeSuspenseAccount
    {
        int ledgerNumber;
        string strAcount;
        public ChangeSuspenseAccount(int ALedgerNumber, string AAccount)
        {
            ledgerNumber = ALedgerNumber;
            strAcount = AAccount;
        }

        public void Suspense()
        {
            try
            {
                OdbcParameter[] ParametersArray;
                ParametersArray = new OdbcParameter[2];
                ParametersArray[0] = new OdbcParameter("", OdbcType.Int);
                ParametersArray[0].Value = ledgerNumber;
                ParametersArray[1] = new OdbcParameter("", OdbcType.VarChar);
                ParametersArray[1].Value = strAcount;

                TDBTransaction transaction = new TDBTransaction();
                TDataBase db = DBAccess.Connect("Suspense");
                bool SubmissionOK = true;
                db.WriteTransaction(ref transaction, ref SubmissionOK,
                    delegate
                    {
                        string strSQL = "INSERT INTO PUB_" + ASuspenseAccountTable.GetTableDBName() + " ";
                        strSQL += "(" + ASuspenseAccountTable.GetLedgerNumberDBName();
                        strSQL += "," + ASuspenseAccountTable.GetSuspenseAccountCodeDBName() + ") ";
                        strSQL += "VALUES ( ? , ? )";

                        db.ExecuteNonQuery(strSQL, transaction, ParametersArray);
                    });
            }
            catch (Exception)
            {
                Assert.Fail("No database access to run the test");
            }
        }

        public void Unsuspense()
        {
            // The equivalent try/catch block that is used for Suspense() was removed 27 Jan 2015 in order to fix
            //  the issue in Mantis #3730
            OdbcParameter[] ParametersArray;
            ParametersArray = new OdbcParameter[2];
            ParametersArray[0] = new OdbcParameter("", OdbcType.Int);
            ParametersArray[0].Value = ledgerNumber;
            ParametersArray[1] = new OdbcParameter("", OdbcType.VarChar);
            ParametersArray[1].Value = strAcount;

            TDBTransaction transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("Unsuspense");
            bool SubmissionOK = true;
            db.WriteTransaction(ref transaction, ref SubmissionOK,
                delegate
                {
                    string strSQL = "DELETE FROM PUB_" + ASuspenseAccountTable.GetTableDBName() + " ";
                    strSQL += "WHERE " + ASuspenseAccountTable.GetLedgerNumberDBName() + " = ? ";
                    strSQL += "AND " + ASuspenseAccountTable.GetSuspenseAccountCodeDBName() + " = ? ";

                    db.ExecuteNonQuery(strSQL, transaction, ParametersArray);
                });
        }
    }
}
