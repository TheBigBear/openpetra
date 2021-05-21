﻿//
// DO NOT REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
//
// @Authors:
//       timop
//
// Copyright 2004-2020 by OM International
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
using System.IO;
using System.Xml;
using System.Text;
using System.Collections.Generic;
using System.Data;
using Ict.Common;
using Ict.Common.IO;
using Ict.Common.DB;
using Ict.Common.Verification;
using Ict.Petra.Shared.MFinance.Account.Data;
using Ict.Petra.Shared.MPartner.Partner.Data;
using Ict.Petra.Server.MPartner.Partner.Data.Access;
using Ict.Petra.Shared.MPersonnel.Personnel.Data;
using Ict.Petra.Server.MPersonnel.Personnel.Data.Access;
using Ict.Petra.Shared.MFinance.GL.Data;
using Ict.Petra.Shared.MPartner;
using Ict.Petra.Server.MPartner.Common;
using Ict.Petra.Server.App.Core;
using Ict.Petra.Server.MFinance.Account.Data.Access;
using Ict.Petra.Server.MFinance.Common;
using Ict.Petra.Server.MFinance.GL;
using Ict.Petra.Server.MFinance.Setup.WebConnectors;

namespace Ict.Petra.Tools.SampleDataConstructor
{
    /// <summary>
    /// tools for populating a ledger with data
    /// </summary>
    public class SampleDataLedger
    {
        /// LedgerNumber to be set from outside
        public static int FLedgerNumber = 43;

        /// will start the calendar early enough so that the current period is open
        public static int FNumberOfClosedPeriods = 5;

        /// we used to have 8 open periods after the current period, but 12 is easier for the test databases
        public static int FNumberOfFwdPostingPeriods = 12;

        /// update the current ledger with new number of forward posting periods, and different start date for the calendar
        public static void UpdateLedger(DateTime ACalendarStartDate)
        {
            GLSetupTDS InspectDS = new GLSetupTDS();

            TDataBase db = DBAccess.Connect("UpdateLedger");

            TDBTransaction Transaction = new TDBTransaction();
            db.ReadTransaction(ref Transaction,
                delegate
                {
                    ALedgerAccess.LoadByPrimaryKey(InspectDS, FLedgerNumber, Transaction);
                });

            db.CloseDBConnection();

            InspectDS.ALedger[0].NumberFwdPostingPeriods = FNumberOfFwdPostingPeriods;

            TGLSetupWebConnector.SaveLedgerSettings(FLedgerNumber, ACalendarStartDate, ref InspectDS);
        }

        /// create new ledger
        public static void CreateNewLedger()
        {
            TVerificationResultCollection VerificationResult;

            TDataBase db = DBAccess.Connect("CreateNewLedger");

            TDBTransaction Transaction = new TDBTransaction();
            db.ReadTransaction(ref Transaction,
                delegate
                {
                    if (ALedgerAccess.Exists(FLedgerNumber, Transaction) && !TGLSetupWebConnector.DeleteLedger(FLedgerNumber, out VerificationResult))
                    {
                        throw new Exception("could not delete ledger");
                    }
                });

            db.CloseDBConnection();

            TGLSetupWebConnector.CreateNewLedger(FLedgerNumber, "SecondLedger", "GB", "EUR", "EUR",
                    new DateTime(DateTime.Now.Year - 1, 4, 1),
                    12, 1, FNumberOfFwdPostingPeriods, false, out VerificationResult);
        }

        /// <summary>
        /// init the exchange rate, to avoid messages "Cannot find exchange rate for EUR USD"
        /// </summary>
        public static void InitExchangeRate(TDataBase ADataBase = null)
        {
            TDataBase db = DBAccess.Connect("InitExchangeRate", ADataBase);
            TDBTransaction Transaction = new TDBTransaction();
            bool SubmitOK = false;
            db.WriteTransaction(ref Transaction,
                ref SubmitOK,
            delegate
            {
                TAccountPeriodInfo AccountingPeriodInfo =
                    new TAccountPeriodInfo(FLedgerNumber, 1);
                ADailyExchangeRateTable dailyrates = new ADailyExchangeRateTable();
                ADailyExchangeRateRow row = dailyrates.NewRowTyped(true);

                row.DateEffectiveFrom = AccountingPeriodInfo.PeriodStartDate;
                row.TimeEffectiveFrom = 100;
                row.FromCurrencyCode = "USD";
                row.ToCurrencyCode = "EUR";
                row.RateOfExchange = 1.34m;
                dailyrates.Rows.Add(row);
                row = dailyrates.NewRowTyped(true);
                row.DateEffectiveFrom = AccountingPeriodInfo.PeriodStartDate;
                row.TimeEffectiveFrom = 100;
                row.FromCurrencyCode = "USD";
                row.ToCurrencyCode = "GBP";
                row.RateOfExchange = 1.57m;
                dailyrates.Rows.Add(row);

                if (!ADailyExchangeRateAccess.Exists(row.FromCurrencyCode, row.ToCurrencyCode, row.DateEffectiveFrom, row.TimeEffectiveFrom, Transaction))
                {
                    ADailyExchangeRateAccess.SubmitChanges(dailyrates, Transaction);
                }

                ALedgerTable Ledger = ALedgerAccess.LoadByPrimaryKey(FLedgerNumber, Transaction);

                for (int periodCounter = 1; periodCounter <= Ledger[0].NumberOfAccountingPeriods + Ledger[0].NumberFwdPostingPeriods; periodCounter++)
                {
                    AccountingPeriodInfo = new TAccountPeriodInfo(FLedgerNumber, periodCounter);

                    ACorporateExchangeRateTable corprates = new ACorporateExchangeRateTable();
                    ACorporateExchangeRateRow corprow = corprates.NewRowTyped(true);
                    corprow.DateEffectiveFrom = AccountingPeriodInfo.PeriodStartDate;
                    corprow.TimeEffectiveFrom = 100;
                    corprow.FromCurrencyCode = "USD";
                    corprow.ToCurrencyCode = "EUR";
                    corprow.RateOfExchange = 1.34m;
                    corprates.Rows.Add(corprow);
                    corprow = corprates.NewRowTyped(true);
                    corprow.DateEffectiveFrom = AccountingPeriodInfo.PeriodStartDate;
                    corprow.TimeEffectiveFrom = 100;
                    corprow.FromCurrencyCode = "USD";
                    corprow.ToCurrencyCode = "GBP";
                    corprow.RateOfExchange = 1.57m;
                    corprates.Rows.Add(corprow);

                    if (!ACorporateExchangeRateAccess.Exists(corprow.FromCurrencyCode, corprow.ToCurrencyCode, corprow.DateEffectiveFrom, Transaction))
                    {
                        ACorporateExchangeRateAccess.SubmitChanges(corprates, Transaction);
                    }
                }
                SubmitOK = true;
            });

            if (ADataBase == null)
            {
                db.CloseDBConnection();
            }
        }

        /// <summary>
        /// Populate ledger with gifts and invoices, post batches, close periods and years, according to FNumberOfClosedPeriods
        /// </summary>
        public static void PopulateData(string datadirectory, bool smallNumber = false)
        {
            int periodOverall = 0;
            int yearCounter = 0;
            int period = 1;
            int YearAD = DateTime.Today.Year - (FNumberOfClosedPeriods / 12);

            SampleDataGiftBatches.FLedgerNumber = FLedgerNumber;
            SampleDataAccountsPayable.FLedgerNumber = FLedgerNumber;
            SampleDataGiftBatches.LoadBatches(Path.Combine(datadirectory, "donations.csv"), smallNumber);
            SampleDataAccountsPayable.GenerateInvoices(Path.Combine(datadirectory, "invoices.csv"), YearAD, smallNumber);

            TDataBase db = DBAccess.Connect("GenerateLedger");

            while (periodOverall <= FNumberOfClosedPeriods)
            {
                TLogging.LogAtLevel(1, "working on year " + yearCounter.ToString() + " / period " + period.ToString());

                SampleDataGiftBatches.CreateGiftBatches(period);

                SampleDataLedger.InitExchangeRate(db);

                if (!SampleDataGiftBatches.PostBatches(yearCounter, period, (periodOverall == FNumberOfClosedPeriods) ? 1 : 0, db))
                {
                    throw new Exception("could not post gift batches");
                }

                if (!SampleDataAccountsPayable.PostAndPayInvoices(yearCounter, period, (periodOverall == FNumberOfClosedPeriods) ? 1 : 0))
                {
                    throw new Exception("could not post invoices");
                }

                TLedgerInfo LedgerInfo = new TLedgerInfo(FLedgerNumber, db);

                if (periodOverall < FNumberOfClosedPeriods)
                {
                    TAccountPeriodInfo AccountingPeriodInfo =
                        new TAccountPeriodInfo(FLedgerNumber, period);
                    TLogging.Log("closing period at " + AccountingPeriodInfo.PeriodEndDate.ToShortDateString());

                    // run month end
                    TMonthEnd MonthEndOperator = new TMonthEnd(db, LedgerInfo);
                    MonthEndOperator.SetNextPeriod(null);


                    if (period == 12)
                    {
                        TDBTransaction transaction = new TDBTransaction();
                        bool SubmissionOK = false;

                        db.WriteTransaction(
                            ref transaction,
                            ref SubmissionOK,
                            delegate
                            {
                                TYearEnd YearEndOperator = new TYearEnd(LedgerInfo);
                                // run year end
                                TVerificationResultCollection verificationResult = new TVerificationResultCollection();
                                List <Int32>glBatches = new List <int>();
                                TReallocation reallocation = new TReallocation(LedgerInfo, glBatches, transaction);
                                reallocation.VerificationResultCollection = verificationResult;
                                reallocation.IsInInfoMode = false;
                                reallocation.RunOperation();

                                TGlmNewYearInit glmNewYearInit = new TGlmNewYearInit(LedgerInfo, yearCounter, YearEndOperator, transaction);
                                glmNewYearInit.VerificationResultCollection = verificationResult;
                                glmNewYearInit.IsInInfoMode = false;
                                glmNewYearInit.RunOperation();
                                YearEndOperator.SetNextPeriod(transaction);

                                SampleDataLedger.InitExchangeRate(db);

                                YearAD++;
                                yearCounter++;
                                SampleDataAccountsPayable.GenerateInvoices(Path.Combine(datadirectory, "invoices.csv"), YearAD, smallNumber, db);
                                period = 0;
                                SubmissionOK = true;
                            });
                    }
                }

                period++;
                periodOverall++;
            }
        }
    }
}
