//
// DO NOT REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
//
// @Authors:
//       timop, christophert
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
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Data.Odbc;
using System.Globalization;
using System.IO;

using Ict.Common;
using Ict.Common.Data;
using Ict.Common.DB;
using Ict.Common.DB.Exceptions;
using Ict.Common.Exceptions;
using Ict.Common.Verification;
using Ict.Common.Verification.Exceptions;
using Ict.Common.Remoting.Server;

using Ict.Petra.Server.App.Core;
using Ict.Petra.Server.App.Core.Security;
using Ict.Petra.Server.MCommon.Data.Access;
using Ict.Petra.Server.MFinance.Account.Data.Access;
using Ict.Petra.Server.MFinance.Cacheable;
using Ict.Petra.Server.MFinance.Common;
using Ict.Petra.Server.MFinance.Common.ServerLookups.WebConnectors;
using Ict.Petra.Server.MFinance.Gift.Data.Access;
using Ict.Petra.Server.MFinance.GL.WebConnectors;
using Ict.Petra.Server.MFinance.Setup.WebConnectors;
using Ict.Petra.Server.MPartner.Partner.Data.Access;
using Ict.Petra.Server.MPartner.Partner.ServerLookups.WebConnectors;
using Ict.Petra.Server.MSysMan.Common.WebConnectors;

using Ict.Petra.Shared;
using Ict.Petra.Shared.MCommon.Data;
using Ict.Petra.Shared.MFinance;
using Ict.Petra.Shared.MFinance.Account.Data;
using Ict.Petra.Shared.MFinance.Gift.Data;
using Ict.Petra.Shared.MFinance.GL.Data;
using Ict.Petra.Shared.MPartner;
using Ict.Petra.Shared.MPartner.Partner.Data;

namespace Ict.Petra.Server.MFinance.Gift.WebConnectors
{
    ///<summary>
    /// This connector provides data for the finance Gift screens
    ///</summary>
    public partial class TGiftTransactionWebConnector
    {
        /// <summary>
        /// create a new batch with a consecutive batch number in the ledger,
        /// and immediately store the batch and the new number in the database
        /// </summary>
        /// <param name="ALedgerNumber"></param>
        /// <returns></returns>
        [RequireModulePermission("FINANCE-1")]
        public static GiftBatchTDS CreateAGiftBatch(Int32 ALedgerNumber)
        {
            return CreateAGiftBatch(ALedgerNumber, DateTime.Today, Catalog.GetString("PLEASE ENTER DESCRIPTION"));
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="ALedgerNumber"></param>
        /// <param name="ADateEffective"></param>
        /// <param name="ABatchDescription"></param>
        /// <param name="ADataBase"></param>
        /// <returns></returns>
        [RequireModulePermission("FINANCE-1")]
        public static GiftBatchTDS CreateAGiftBatch(Int32 ALedgerNumber, DateTime ADateEffective, string ABatchDescription, TDataBase ADataBase = null)
        {
            #region Validate Arguments

            if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }

            #endregion Validate Arguments

            GiftBatchTDS MainDS = new GiftBatchTDS();

            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("CreateAGiftBatch", ADataBase);
            bool SubmissionOK = false;

            try
            {
                db.WriteTransaction(
                    ref Transaction,
                    ref SubmissionOK,
                    delegate
                    {
                        ALedgerTable ledgerTable = ALedgerAccess.LoadByPrimaryKey(ALedgerNumber, Transaction);

                        #region Validate Data

                        if ((ledgerTable == null) || (ledgerTable.Count == 0))
                        {
                            throw new EFinanceSystemDataTableReturnedNoDataException(String.Format(Catalog.GetString(
                                        "Function:{0} - Ledger data for Ledger number {1} does not exist or could not be accessed!"),
                                    Utilities.GetMethodName(true),
                                    ALedgerNumber));
                        }

                        #endregion Validate Data

                        TGiftBatchFunctions.CreateANewGiftBatchRow(ref MainDS, ref Transaction, ref ledgerTable, ALedgerNumber, ADateEffective);

                        if (ABatchDescription.Length > 0)
                        {
                            MainDS.AGiftBatch[0].BatchDescription = ABatchDescription;
                        }

                        ALedgerAccess.SubmitChanges(ledgerTable, Transaction);
                        AGiftBatchAccess.SubmitChanges(MainDS.AGiftBatch, Transaction);
                        MainDS.AGiftBatch.AcceptChanges();

                        SubmissionOK = true;
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

            return MainDS;
        }

        /// <summary>
        /// create a new recurring batch with a consecutive batch number in the ledger,
        /// and immediately store the batch and the new number in the database
        /// </summary>
        /// <param name="ALedgerNumber"></param>
        /// <param name="ADataBase"></param>
        /// <returns></returns>
        [RequireModulePermission("FINANCE-1")]
        public static GiftBatchTDS CreateARecurringGiftBatch(Int32 ALedgerNumber, TDataBase ADataBase = null)
        {
            #region Validate Arguments

            if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }

            #endregion Validate Arguments

            GiftBatchTDS MainDS = new GiftBatchTDS();

            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("CreateARecurringGiftBatch", ADataBase);
            bool SubmissionOK = false;

            try
            {
                db.WriteTransaction(
                    ref Transaction,
                    ref SubmissionOK,
                    delegate
                    {
                        TGiftBatchFunctions.CreateANewRecurringGiftBatchRow(ref MainDS, ref Transaction, ALedgerNumber);

                        GiftBatchTDSAccess.SubmitChanges(MainDS, Transaction.DataBaseObj);
                        MainDS.ARecurringGiftBatch.AcceptChanges();

                        SubmissionOK = true;
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

            return MainDS;
        }

        /// <summary>
        /// create a gift batch from a recurring gift batch
        /// including gift and gift detail
        /// </summary>
        /// <param name="ARequestParams">HashTable with many parameters</param>
        /// <param name="ANewGiftBatchNo">The new gift batch number</param>
        [RequireModulePermission("FINANCE-1")]
        public static bool SubmitRecurringGiftBatch(Hashtable ARequestParams, out int ANewGiftBatchNo)
        {
            ANewGiftBatchNo = 0;

            Int32 ALedgerNumber;
            Int32 ABatchNumber;
            DateTime AEffectiveDate;
            String AReference;
            Decimal AExchangeRateToBase;
            Decimal AExchangeRateIntlToBase;

            #region Validate Parameter Arguments

            try
            {
                ALedgerNumber = (Int32)ARequestParams["ALedgerNumber"];
                ABatchNumber = (Int32)ARequestParams["ABatchNumber"];
                AEffectiveDate = (DateTime)ARequestParams["AEffectiveDate"];
                AReference = (String)ARequestParams["AReference"];
                AExchangeRateToBase = (Decimal)ARequestParams["AExchangeRateToBase"];
                AExchangeRateIntlToBase = (Decimal)ARequestParams["AExchangeRateIntlToBase"];
            }
            catch (Exception ex)
            {
                TLogging.LogException(ex, Utilities.GetMethodSignature());
                throw;
            }

            if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }
            else if (ABatchNumber <= 0)
            {
                throw new EFinanceSystemInvalidBatchNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Batch number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber, ABatchNumber);
            }
            else if (AExchangeRateToBase <= 0)
            {
                throw new ArgumentException(String.Format(Catalog.GetString(
                            "Function:{0} - The exchange rate to base parameter must be greater than 0!"),
                        Utilities.GetMethodName(true)));
            }
            else if (AExchangeRateIntlToBase <= 0)
            {
                throw new ArgumentException(String.Format(Catalog.GetString(
                            "Function:{0} - The international exchange rate parameter must be greater than 0!"),
                        Utilities.GetMethodName(true)));
            }

            #endregion Validate Parameter Arguments

            bool TaxDeductiblePercentageEnabled =
                new TSystemDefaults().GetBooleanDefault(SharedConstants.SYSDEFAULT_TAXDEDUCTIBLEPERCENTAGE, false);
            bool TransactionInIntlCurrency = false;

            int NewGiftBatchNumber = -1;

            GiftBatchTDS MainDS = new GiftBatchTDS();
            GiftBatchTDS MainRecurringDS = LoadRecurringGiftTransactionsForBatch(ALedgerNumber, ABatchNumber);

            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("SubmitRecurringGiftBatch");
            bool SubmissionOK = false;

            try
            {
                db.WriteTransaction(
                    ref Transaction,
                    ref SubmissionOK,
                    delegate
                    {
                        ALedgerTable ledgerTable = ALedgerAccess.LoadByPrimaryKey(ALedgerNumber, Transaction);
                        ARecurringGiftBatchAccess.LoadByPrimaryKey(MainRecurringDS, ALedgerNumber, ABatchNumber, Transaction);

                        #region Validate Data

                        if ((ledgerTable == null) || (ledgerTable.Count == 0))
                        {
                            throw new EFinanceSystemDataTableReturnedNoDataException(String.Format(Catalog.GetString(
                                        "Function:{0} - Details for Ledger {1} could not be accessed!"), Utilities.GetMethodSignature(),
                                    ALedgerNumber));
                        }
                        else if ((MainRecurringDS.ARecurringGiftBatch == null) || (MainRecurringDS.ARecurringGiftBatch.Count == 0))
                        {
                            throw new EFinanceSystemDataTableReturnedNoDataException(String.Format(Catalog.GetString(
                                        "Function:{0} - Details for Recurring Gift Batch {1} could not be accessed!"), Utilities.GetMethodSignature(),
                                    ABatchNumber));
                        }

                        #endregion Validate Data

                        // Assuming all relevant data is loaded in MainRecurringDS
                        ARecurringGiftBatchRow recBatch = (ARecurringGiftBatchRow)MainRecurringDS.ARecurringGiftBatch[0];

                        if ((recBatch.BatchNumber == ABatchNumber) && (recBatch.LedgerNumber == ALedgerNumber))
                        {
                            Decimal batchTotal = 0;

                            AGiftBatchRow batch = TGiftBatchFunctions.CreateANewGiftBatchRow(ref MainDS,
                                ref Transaction,
                                ref ledgerTable,
                                ALedgerNumber,
                                AEffectiveDate);

                            NewGiftBatchNumber = batch.BatchNumber;

                            batch.BatchDescription = recBatch.BatchDescription;
                            batch.BankCostCentre = recBatch.BankCostCentre;
                            batch.BankAccountCode = recBatch.BankAccountCode;
                            batch.ExchangeRateToBase = AExchangeRateToBase;
                            batch.MethodOfPaymentCode = recBatch.MethodOfPaymentCode;
                            batch.GiftType = recBatch.GiftType;
                            batch.HashTotal = recBatch.HashTotal;
                            batch.CurrencyCode = recBatch.CurrencyCode;

                            TransactionInIntlCurrency = (batch.CurrencyCode == ledgerTable[0].IntlCurrency);

                            DataView giftDV = new DataView(MainRecurringDS.ARecurringGift);
                            giftDV.Sort = string.Format("{0} ASC", ARecurringGiftTable.GetGiftTransactionNumberDBName());

                            foreach (DataRowView giftRV in giftDV)
                            {
                                ARecurringGiftRow recGift = (ARecurringGiftRow)giftRV.Row;

                                if ((recGift.BatchNumber == ABatchNumber) && (recGift.LedgerNumber == ALedgerNumber) && recGift.Active)
                                {
                                    //Look if there is a detail which is in the donation period (else continue)
                                    bool foundDetail = false;

                                    foreach (ARecurringGiftDetailRow recGiftDetail in MainRecurringDS.ARecurringGiftDetail.Rows)
                                    {
                                        if ((recGiftDetail.GiftTransactionNumber == recGift.GiftTransactionNumber)
                                            && (recGiftDetail.BatchNumber == ABatchNumber) && (recGiftDetail.LedgerNumber == ALedgerNumber)
                                            && ((recGiftDetail.StartDonations == null) || (AEffectiveDate >= recGiftDetail.StartDonations))
                                            && ((recGiftDetail.EndDonations == null) || (AEffectiveDate <= recGiftDetail.EndDonations))
                                            )
                                        {
                                            foundDetail = true;
                                            break;
                                        }
                                    }

                                    if (!foundDetail)
                                    {
                                        continue;
                                    }

                                    // make the gift from recGift
                                    AGiftRow gift = MainDS.AGift.NewRowTyped();
                                    gift.LedgerNumber = batch.LedgerNumber;
                                    gift.BatchNumber = batch.BatchNumber;
                                    gift.GiftTransactionNumber = ++batch.LastGiftNumber;
                                    gift.DonorKey = recGift.DonorKey;
                                    gift.DateEntered = AEffectiveDate;
                                    gift.MethodOfGivingCode = recGift.MethodOfGivingCode;

                                    if (gift.MethodOfGivingCode.Length == 0)
                                    {
                                        gift.SetMethodOfGivingCodeNull();
                                    }

                                    gift.MethodOfPaymentCode = recGift.MethodOfPaymentCode;

                                    if (gift.MethodOfPaymentCode.Length == 0)
                                    {
                                        gift.SetMethodOfPaymentCodeNull();
                                    }

                                    if (AReference != "")
                                    {
                                        gift.Reference = AReference;
                                    }
                                    else
                                    {
                                        gift.Reference = recGift.Reference;
                                    }

                                    gift.ReceiptLetterCode = recGift.ReceiptLetterCode;

                                    MainDS.AGift.Rows.Add(gift);

                                    DataView giftDetailDV = new DataView(MainRecurringDS.ARecurringGiftDetail);
                                    giftDetailDV.Sort = string.Format("{0} ASC, {1} ASC",
                                        ARecurringGiftDetailTable.GetGiftTransactionNumberDBName(),
                                        ARecurringGiftDetailTable.GetDetailNumberDBName());

                                    foreach (DataRowView detailRV in giftDetailDV)
                                    {
                                        ARecurringGiftDetailRow recGiftDetail = (ARecurringGiftDetailRow)detailRV.Row;

                                        //decimal amtIntl = 0M;
                                        decimal amtBase = 0M;
                                        decimal amtTrans = 0M;

                                        if ((recGiftDetail.GiftTransactionNumber == recGift.GiftTransactionNumber)
                                            && (recGiftDetail.BatchNumber == ABatchNumber) && (recGiftDetail.LedgerNumber == ALedgerNumber)
                                            && ((recGiftDetail.StartDonations == null) || (recGiftDetail.StartDonations <= AEffectiveDate))
                                            && ((recGiftDetail.EndDonations == null) || (recGiftDetail.EndDonations >= AEffectiveDate))
                                            )
                                        {
                                            AGiftDetailRow detail = MainDS.AGiftDetail.NewRowTyped();
                                            detail.LedgerNumber = gift.LedgerNumber;
                                            detail.BatchNumber = gift.BatchNumber;
                                            detail.GiftTransactionNumber = gift.GiftTransactionNumber;
                                            detail.DetailNumber = ++gift.LastDetailNumber;

                                            amtTrans = recGiftDetail.GiftAmount;
                                            amtBase = GLRoutines.Divide((decimal)amtTrans, AExchangeRateToBase);
                                            detail.GiftTransactionAmount = amtTrans;
                                            detail.GiftAmount = amtBase;
                                            detail.GiftAmountIntl =
                                                TransactionInIntlCurrency ? amtTrans : GLRoutines.Divide((decimal)amtBase, AExchangeRateIntlToBase);
                                            batchTotal += amtTrans;

                                            detail.RecipientKey = recGiftDetail.RecipientKey;
                                            detail.RecipientLedgerNumber = recGiftDetail.RecipientLedgerNumber;
                                            detail.ChargeFlag = recGiftDetail.ChargeFlag;
                                            detail.ConfidentialGiftFlag = recGiftDetail.ConfidentialGiftFlag;
                                            detail.TaxDeductible = recGiftDetail.TaxDeductible;
                                            detail.MailingCode = recGiftDetail.MailingCode;

                                            if (detail.MailingCode.Length == 0)
                                            {
                                                detail.SetMailingCodeNull();
                                            }

                                            detail.MotivationGroupCode = recGiftDetail.MotivationGroupCode;
                                            detail.MotivationDetailCode = recGiftDetail.MotivationDetailCode;

                                            detail.GiftCommentOne = recGiftDetail.GiftCommentOne;
                                            detail.CommentOneType = recGiftDetail.CommentOneType;
                                            detail.GiftCommentTwo = recGiftDetail.GiftCommentTwo;
                                            detail.CommentTwoType = recGiftDetail.CommentTwoType;
                                            detail.GiftCommentThree = recGiftDetail.GiftCommentThree;
                                            detail.CommentThreeType = recGiftDetail.CommentThreeType;

                                            if (TaxDeductiblePercentageEnabled)
                                            {
                                                // Sets TaxDeductiblePct and uses it to calculate the tax deductibility amounts for a Gift Detail
                                                TGift.SetDefaultTaxDeductibilityData(ref detail, gift.DateEntered, Transaction);
                                            }

                                            MainDS.AGiftDetail.Rows.Add(detail);
                                        }
                                    }

                                    batch.BatchTotal = batchTotal;
                                }
                            }
                        }

                        ALedgerAccess.SubmitChanges(ledgerTable, Transaction);
                        AGiftBatchAccess.SubmitChanges(MainDS.AGiftBatch, Transaction);
                        AGiftAccess.SubmitChanges(MainDS.AGift, Transaction);
                        AGiftDetailAccess.SubmitChanges(MainDS.AGiftDetail, Transaction);

                        MainDS.AcceptChanges();

                        SubmissionOK = true;
                    });

                ANewGiftBatchNo = NewGiftBatchNumber;
                return SubmissionOK;
            }
            catch (Exception ex)
            {
                TLogging.LogException(ex, Utilities.GetMethodSignature());
                throw;
            }
            finally
            {
                db.CloseDBConnection();
            }
        }

        /// <summary>
        /// Loads all available years with gift data into a table
        /// To be used by a combobox to select the financial year
        ///
        /// </summary>
        /// <returns>DataTable</returns>
        [RequireModulePermission("FINANCE-1")]
        public static DataTable GetAvailableGiftYears(Int32 ALedgerNumber, out String ADisplayMember, out String AValueMember)
        {
            #region Validate Arguments

            if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }

            #endregion Validate Arguments

            ADisplayMember = "YearDate";
            AValueMember = "YearNumber";

            DataTable ReturnTable = new DataTable();
            ReturnTable.Columns.Add(AValueMember, typeof(System.Int32));
            ReturnTable.Columns.Add(ADisplayMember, typeof(String));
            ReturnTable.PrimaryKey = new DataColumn[] {
                ReturnTable.Columns[0]
            };

            System.Type TypeofTable = null;
            TCacheable CachePopulator = new TCacheable();

            ALedgerTable LedgerTable = (ALedgerTable)CachePopulator.GetCacheableTable(TCacheableFinanceTablesEnum.LedgerDetails,
                "",
                false,
                ALedgerNumber,
                out TypeofTable);

            AAccountingPeriodTable AccountingPeriods = (AAccountingPeriodTable)CachePopulator.GetCacheableTable(
                TCacheableFinanceTablesEnum.AccountingPeriodList,
                "",
                false,
                ALedgerNumber,
                out TypeofTable);

            #region Validate Data

            if ((LedgerTable == null) || (LedgerTable.Count == 0))
            {
                throw new EFinanceSystemCacheableTableReturnedNoDataException(String.Format(Catalog.GetString(
                            "Function:{0} - Ledger data for Ledger number {1} does not exist or could not be accessed!"),
                        Utilities.GetMethodName(true),
                        ALedgerNumber));
            }
            else if ((AccountingPeriods == null) || (AccountingPeriods.Count == 0))
            {
                throw new EFinanceSystemCacheableTableReturnedNoDataException(String.Format(Catalog.GetString(
                            "Function:{0} - Accounting Periods data for Ledger number {1} does not exist or could not be accessed!"),
                        Utilities.GetMethodName(true),
                        ALedgerNumber));
            }

            #endregion Validate Data

            AAccountingPeriodRow CurrentYearEndPeriod =
                (AAccountingPeriodRow)AccountingPeriods.Rows.Find(new object[] { ALedgerNumber, LedgerTable[0].NumberOfAccountingPeriods });
            DateTime CurrentYearEnd = CurrentYearEndPeriod.PeriodEndDate;

            TDBTransaction ReadTransaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("GetAvailableGiftYears");

            try
            {
                db.ReadTransaction(
                    ref ReadTransaction,
                    delegate
                    {
                        // add the years, which are retrieved by reading from the gift batch tables
                        string Sql =
                            String.Format("SELECT DISTINCT {0} AS availYear FROM PUB_{1} WHERE {2}={3} ORDER BY 1 DESC",
                                AGiftBatchTable.GetBatchYearDBName(),
                                AGiftBatchTable.GetTableDBName(),
                                AGiftBatchTable.GetLedgerNumberDBName(),
                                ALedgerNumber);

                        DataTable BatchYearTable = db.SelectDT(Sql, "BatchYearTable", ReadTransaction);

                        foreach (DataRow row in BatchYearTable.Rows)
                        {
                            DataRow resultRow = ReturnTable.NewRow();
                            resultRow[0] = row[0];
                            resultRow[1] = CurrentYearEnd.AddYears(-1 * (LedgerTable[0].CurrentFinancialYear - Convert.ToInt32(
                                                                             row[0]))).ToString("yyyy");
                            ReturnTable.Rows.Add(resultRow);
                        }
                    });

                // we should also check if the current year has been added, in case there are no gift batches yet
                if (ReturnTable.Rows.Find(LedgerTable[0].CurrentFinancialYear) == null)
                {
                    DataRow resultRow = ReturnTable.NewRow();
                    resultRow[0] = LedgerTable[0].CurrentFinancialYear;
                    resultRow[1] = CurrentYearEnd.ToString("yyyy");
                    ReturnTable.Rows.InsertAt(resultRow, 0);
                }
            }
            catch (Exception ex)
            {
                TLogging.LogException(ex, Utilities.GetMethodSignature());
                throw;
            }

            db.CloseDBConnection();

            return ReturnTable;
        }

        /// <summary>
        /// returns ledger table for specified ledger
        /// </summary>
        /// <param name="ALedgerNumber"></param>
        /// <returns></returns>
        [RequireModulePermission("FINANCE-1")]
        public static GiftBatchTDS LoadALedgerTable(Int32 ALedgerNumber)
        {
            #region Validate Arguments

            if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }

            #endregion Validate Arguments

            GiftBatchTDS MainDS = new GiftBatchTDS();

            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("LoadALedgerTable");

            try
            {
                db.ReadTransaction(
                    ref Transaction,
                    delegate
                    {
                        ALedgerAccess.LoadByPrimaryKey(MainDS, ALedgerNumber, Transaction);

                        #region Validate Data

                        if ((MainDS.ALedger == null) || (MainDS.ALedger.Count == 0))
                        {
                            throw new EFinanceSystemDataTableReturnedNoDataException(String.Format(Catalog.GetString(
                                        "Function:{0} - Details for Ledger {1} could not be accessed!"), Utilities.GetMethodSignature(),
                                    ALedgerNumber));
                        }

                        #endregion Validate Data
                    });

                // Remove all Tables that were not filled with data before remoting them.
                MainDS.RemoveEmptyTables();

                // Accept row changes here so that the Client gets 'unmodified' rows
                MainDS.AcceptChanges();
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
        /// loads a list of batches for the given ledger
        /// also get the ledger for the base currency etc
        /// </summary>
        /// <param name="ALedgerNumber"></param>
        /// <param name="AYear">if -1, the year will be ignored</param>
        /// <param name="APeriod">if AYear is -1 or period is -1, the period will be ignored.
        /// if APeriod is 0 and the current year is selected, then the current and the forwarding periods are used.
        /// Period = -2 means all periods in current year</param>
        /// <param name="ABatchStatus"></param>
        /// <param name="ACurrencyCode"></param>
        /// <returns></returns>
        [RequireModulePermission("FINANCE-1")]
        public static GiftBatchTDS LoadAGiftBatchForYearPeriod(
            Int32 ALedgerNumber, Int32 AYear, Int32 APeriod,
            String ABatchStatus,
            out String ACurrencyCode
            )
        {
            #region Validate Arguments

            if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }

            #endregion Validate Arguments

            string FilterByPeriod = string.Empty;
            string CurrencyCode = string.Empty;

            GiftBatchTDS MainDS = new GiftBatchTDS();

            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("LoadAGiftBatchForYearPeriod");

            try
            {
                db.ReadTransaction(
                    ref Transaction,
                    delegate
                    {
                        //Load Ledger table
                        ALedgerAccess.LoadByPrimaryKey(MainDS, ALedgerNumber, Transaction);

                        #region Validate Data

                        if ((MainDS.ALedger == null) || (MainDS.ALedger.Count == 0))
                        {
                            throw new EFinanceSystemDataTableReturnedNoDataException(String.Format(Catalog.GetString(
                                        "Function:{0} - Details for Ledger {1} could not be accessed!"), Utilities.GetMethodSignature(),
                                    ALedgerNumber));
                        }

                        #endregion Validate Data

                        if (AYear > -1)
                        {
                            FilterByPeriod = String.Format(" AND gb.{0} = {1}",
                                AGiftBatchTable.GetBatchYearDBName(),
                                AYear);

                            if ((APeriod == 0) && (AYear == MainDS.ALedger[0].CurrentFinancialYear))
                            {
                                //Return current and forwarding periods
                                FilterByPeriod += String.Format(" AND gb.{0} >= {1}",
                                    AGiftBatchTable.GetBatchPeriodDBName(),
                                    MainDS.ALedger[0].CurrentPeriod);
                            }
                            else if (APeriod > 0)
                            {
                                //Return only specified period
                                FilterByPeriod += String.Format(" AND gb.{0} = {1}",
                                    AGiftBatchTable.GetBatchPeriodDBName(),
                                    APeriod);
                            }
                            else
                            {
                                //Nothing to add, returns all periods
                            }
                        }

                        string FilterByBatchStatus = string.Empty;

                        if (ABatchStatus == MFinanceConstants.BATCH_CANCELLED ||
                            ABatchStatus == MFinanceConstants.BATCH_POSTED ||
                            ABatchStatus == MFinanceConstants.BATCH_UNPOSTED)
                        {
                            FilterByBatchStatus += String.Format(" AND gb.{0} = '{1}'",
                                AGiftBatchTable.GetBatchStatusDBName(),
                                ABatchStatus);
                        }

                        string SelectClause =
                            String.Format("SELECT * FROM PUB_{0} gb WHERE gb.{1} = {2}",
                                AGiftBatchTable.GetTableDBName(),
                                AGiftBatchTable.GetLedgerNumberDBName(),
                                ALedgerNumber);

                        db.Select(MainDS, SelectClause + FilterByPeriod + FilterByBatchStatus,
                            MainDS.AGiftBatch.TableName, Transaction);

                        // now get the gift detail transaction amounts for the gift batch total
                        SelectClause =
                            String.Format("SELECT * FROM PUB_{0} gb, PUB_{1} gd WHERE gb.{2} = {3} AND gb.{4} = gd.{5} AND gb.{6} = gd.{7}",
                                AGiftBatchTable.GetTableDBName(),
                                AGiftDetailTable.GetTableDBName(),
                                AGiftBatchTable.GetLedgerNumberDBName(),
                                ALedgerNumber,
                                AGiftBatchTable.GetLedgerNumberDBName(),
                                AGiftDetailTable.GetLedgerNumberDBName(),
                                AGiftBatchTable.GetBatchNumberDBName(),
                                AGiftDetailTable.GetBatchNumberDBName());
                        db.Select(MainDS, SelectClause + FilterByPeriod + FilterByBatchStatus,
                            MainDS.AGiftDetail.TableName, Transaction);

                        foreach (GiftBatchTDSAGiftBatchRow batchRow in MainDS.AGiftBatch.Rows)
                        {
                            batchRow.GiftBatchTotal = 0;

                            foreach (GiftBatchTDSAGiftDetailRow giftDetail in MainDS.AGiftDetail.Rows)
                            {
                                if (giftDetail.BatchNumber == batchRow.BatchNumber)
                                {
                                    batchRow.GiftBatchTotal += giftDetail.GiftTransactionAmount;
                                }
                            }
                        }

                        CurrencyCode = TFinanceServerLookupWebConnector.GetLedgerBaseCurrency(ALedgerNumber, db);

                        MainDS.AGiftDetail.Clear();
                    });


                MainDS.AcceptChanges();
            }
            catch (Exception ex)
            {
                TLogging.LogException(ex, Utilities.GetMethodSignature());
                throw;
            }

            ACurrencyCode = CurrencyCode;

            db.CloseDBConnection();

            return MainDS;
        }

        /// <summary>
        /// loads a list of batches for the given ledger's current year
        /// </summary>
        /// <param name="ALedgerNumber"></param>
        /// <returns></returns>
        [RequireModulePermission("FINANCE-1")]
        public static GiftBatchTDS LoadAGiftBatchesForCurrentYear(Int32 ALedgerNumber)
        {
            #region Validate Arguments

            if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }

            #endregion Validate Arguments

            GiftBatchTDS MainDS = new GiftBatchTDS();

            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("LoadAGiftBatchesForCurrentYear");

            try
            {
                db.ReadTransaction(
                    ref Transaction,
                    delegate
                    {
                        ALedgerAccess.LoadByPrimaryKey(MainDS, ALedgerNumber, Transaction);

                        #region Validate Data

                        if ((MainDS.ALedger == null) || (MainDS.ALedger.Count == 0))
                        {
                            throw new EFinanceSystemDataTableReturnedNoDataException(String.Format(Catalog.GetString(
                                        "Function:{0} - Ledger data for Ledger number {1} does not exist or could not be accessed!"),
                                    Utilities.GetMethodName(true),
                                    ALedgerNumber));
                        }

                        #endregion Validate Data

                        string SelectClause = String.Format("SELECT * FROM PUB_{0} WHERE {1} = {2} AND PUB_{0}.{3} = {4}",
                            AGiftBatchTable.GetTableDBName(),
                            AGiftBatchTable.GetLedgerNumberDBName(),
                            ALedgerNumber,
                            AGiftBatchTable.GetBatchYearDBName(),
                            MainDS.ALedger[0].CurrentFinancialYear);

                        db.Select(MainDS, SelectClause, MainDS.AGiftBatch.TableName, Transaction);
                    });

                MainDS.AcceptChanges();
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
        /// loads a list of batches for the given ledger's current year and current plus forwarding periods
        /// </summary>
        /// <param name="ALedgerNumber"></param>
        /// <returns></returns>
        [RequireModulePermission("FINANCE-1")]
        public static GiftBatchTDS LoadAGiftBatchesForCurrentYearPeriod(Int32 ALedgerNumber)
        {
            #region Validate Arguments

            if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }

            #endregion Validate Arguments

            GiftBatchTDS MainDS = new GiftBatchTDS();

            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("LoadAGiftBatchesForCurrentYearPeriod");

            try
            {
                db.ReadTransaction(
                    ref Transaction,
                    delegate
                    {
                        ALedgerAccess.LoadByPrimaryKey(MainDS, ALedgerNumber, Transaction);

                        #region Validate Data

                        if ((MainDS.ALedger == null) || (MainDS.ALedger.Count == 0))
                        {
                            throw new EFinanceSystemDataTableReturnedNoDataException(String.Format(Catalog.GetString(
                                        "Function:{0} - Ledger data for Ledger number {1} does not exist or could not be accessed!"),
                                    Utilities.GetMethodName(true),
                                    ALedgerNumber));
                        }

                        #endregion Validate Data

                        string SelectClause = String.Format("SELECT * FROM PUB_{0} WHERE {1} = {2} AND PUB_{0}.{3} = {4} AND PUB_{0}.{5} >= {6}",
                            AGiftBatchTable.GetTableDBName(),
                            AGiftBatchTable.GetLedgerNumberDBName(),
                            ALedgerNumber,
                            AGiftBatchTable.GetBatchYearDBName(),
                            MainDS.ALedger[0].CurrentFinancialYear,
                            AGiftBatchTable.GetBatchPeriodDBName(),
                            MainDS.ALedger[0].CurrentPeriod);

                        db.Select(MainDS, SelectClause, MainDS.AGiftBatch.TableName, Transaction);
                    });

                MainDS.AcceptChanges();
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
        /// loads a GiftBatchTDS for a whole transaction (i.e. all details in the specified gift)
        /// </summary>
        /// <param name="ALedgerNumber"></param>
        /// <param name="ABatchNumber"></param>
        /// <param name="AGiftTransactionNumber"></param>
        /// <returns>DataSet containing the transation's data</returns>
        [RequireModulePermission("FINANCE-1")]
        public static GiftBatchTDS LoadAGiftSingle(Int32 ALedgerNumber, Int32 ABatchNumber, Int32 AGiftTransactionNumber)
        {
            #region Validate Arguments

            if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }
            else if (ABatchNumber <= 0)
            {
                throw new EFinanceSystemInvalidBatchNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Batch number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber, ABatchNumber);
            }
            else if (AGiftTransactionNumber <= 0)
            {
                throw new ArgumentException(String.Format(Catalog.GetString(
                            "Function:{0} - The Gift Transaction number in Ledger {1}, Batch {2} must be greater than 0!"),
                        Utilities.GetMethodName(true), ALedgerNumber, ABatchNumber));
            }

            #endregion Validate Arguments

            GiftBatchTDS MainDS = new GiftBatchTDS();

            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("LoadAGiftSingle");

            try
            {
                db.ReadTransaction(
                    ref Transaction,
                    delegate
                    {
                        ALedgerAccess.LoadByPrimaryKey(MainDS, ALedgerNumber, Transaction);
                        AGiftBatchAccess.LoadByPrimaryKey(MainDS, ALedgerNumber, ABatchNumber, Transaction);
                        AGiftAccess.LoadByPrimaryKey(MainDS, ALedgerNumber, ABatchNumber, AGiftTransactionNumber, Transaction);
                        AGiftDetailAccess.LoadViaAGift(MainDS, ALedgerNumber, ABatchNumber, AGiftTransactionNumber, Transaction);

                        #region Validate Data

                        if ((MainDS.ALedger == null) || (MainDS.ALedger.Count == 0))
                        {
                            throw new EFinanceSystemDataTableReturnedNoDataException(String.Format(Catalog.GetString(
                                        "Function:{0} - Ledger data for Ledger number {1} does not exist or could not be accessed!"),
                                    Utilities.GetMethodName(true),
                                    ALedgerNumber));
                        }
                        else if ((MainDS.AGiftBatch == null) || (MainDS.AGiftBatch.Count == 0))
                        {
                            throw new EFinanceSystemDataTableReturnedNoDataException(String.Format(Catalog.GetString(
                                        "Function:{0} - Batch data for Gift Batch number {1} in Ledger number {2} does not exist or could not be accessed!"),
                                    Utilities.GetMethodName(true),
                                    ABatchNumber,
                                    ALedgerNumber));
                        }
                        else if ((MainDS.AGift == null) || (MainDS.AGift.Count == 0))
                        {
                            throw new EFinanceSystemDataTableReturnedNoDataException(String.Format(Catalog.GetString(
                                        "Function:{0} - Gift data for Gift {1} in Batch number {2} in Ledger number {3} does not exist or could not be accessed!"),
                                    Utilities.GetMethodName(true),
                                    AGiftTransactionNumber,
                                    ABatchNumber,
                                    ALedgerNumber));
                        }
                        else if ((MainDS.AGiftDetail == null) || (MainDS.AGiftDetail.Count == 0))
                        {
                            throw new EFinanceSystemDataTableReturnedNoDataException(String.Format(Catalog.GetString(
                                        "Function:{0} - Gift Details for Gift {1} in Batch number {2} in Ledger number {3} do not exist or could not be accessed!"),
                                    Utilities.GetMethodName(true),
                                    AGiftTransactionNumber,
                                    ABatchNumber,
                                    ALedgerNumber));
                        }

                        #endregion Validate Data
                    });

                MainDS.AcceptChanges();
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
        /// Loads a donor's last gift (if it exists) and returns the associated gift details.
        /// </summary>
        /// <param name="ADonorPartnerKey"></param>
        /// <param name="ALedgerNumber"></param>
        /// <param name="ALatestUnpostedGiftDateEntered"></param>
        /// <returns></returns>
        [RequireModulePermission("FINANCE-1")]
        public static GiftBatchTDSAGiftDetailTable LoadDonorLastPostedGift(Int64 ADonorPartnerKey,
            Int32 ALedgerNumber,
            DateTime ALatestUnpostedGiftDateEntered)
        {
            #region Validate Arguments

            if (ADonorPartnerKey < 0)
            {
                throw new ArgumentException(String.Format(Catalog.GetString(
                            "Function:{0} - The Donor Partnerkey cannot be a negative number!"),
                        Utilities.GetMethodName(true)));
            }

            #endregion Validate Arguments

            GiftBatchTDSAGiftDetailTable LastGiftData = new GiftBatchTDSAGiftDetailTable();

            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("LoadDonorLastPostedGift");

            db.ReadTransaction(
                ref Transaction,
                delegate
                {
                    // load latest gift from donor
                    string Query = "SELECT Gift.*" +
                                   " FROM a_gift AS Gift" +
                                   " WHERE Gift.a_ledger_number_i = " + ALedgerNumber +
                                   "  AND Gift.p_donor_key_n = " + ADonorPartnerKey +
                                   "  AND Gift.a_date_entered_d > '" + ALatestUnpostedGiftDateEntered.ToString("yyyy/MM/dd") + "'" +
                                   " ORDER BY Gift.a_date_entered_d DESC, Gift.a_gift_transaction_number_i DESC" +
                                   " LIMIT 1";

                    DataTable GiftTable = db.SelectDT(Query, AGiftTable.GetTableDBName(), Transaction);

                    if ((GiftTable == null) || (GiftTable.Rows.Count == 0))
                    {
                        return;
                    }

                    DataRow GiftRow = GiftTable.Rows[0];

                    // load gift details for the latest gift
                    Query = "SELECT a_gift_detail.*, p_partner.p_partner_short_name_c AS RecipientDescription" +
                            " FROM a_gift_detail, p_partner" +
                            " WHERE a_gift_detail.a_ledger_number_i = " + ALedgerNumber +
                            " AND a_gift_detail.a_batch_number_i = " + GiftRow[AGiftTable.GetBatchNumberDBName()] +
                            " AND a_gift_detail.a_gift_transaction_number_i = " + GiftRow[AGiftTable.GetGiftTransactionNumberDBName()] +
                            " AND a_gift_detail.p_recipient_key_n = p_partner.p_partner_key_n";

                    db.SelectDT(LastGiftData, Query, Transaction);
                });

            LastGiftData.AcceptChanges();

            db.CloseDBConnection();

            return LastGiftData;
        }

        /// <summary>
        /// Returns True if this person is a Gift Donor.
        /// </summary>
        /// <param name="ALedgerNumber"></param>
        /// <param name="ADonorPartnerKey"></param>
        /// <returns></returns>
        [RequireModulePermission("FINANCE-1")]
        public static Boolean DonorHasGiven(Int32 ALedgerNumber,
            Int64 ADonorPartnerKey)
        {
            #region Validate Arguments

            if (ADonorPartnerKey < 0)
            {
                throw new ArgumentException(String.Format(Catalog.GetString(
                            "Function:{0} - The Donor Partnerkey cannot be a negative number!"),
                        Utilities.GetMethodName(true)));
            }

            #endregion Validate Arguments

            DataTable GiftTable = null;
            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("DonorHasGiven");

            db.ReadTransaction(
                ref Transaction,
                delegate
                {
                    // load latest gift from donor
                    string Query = "SELECT a_ledger_number_i" +
                                   " FROM a_gift" +
                                   " WHERE a_ledger_number_i = " + ALedgerNumber +
                                   "  AND p_donor_key_n = " + ADonorPartnerKey +
                                   " LIMIT 1;";

                    GiftTable = db.SelectDT(Query, AGiftTable.GetTableDBName(), Transaction);
                });

            db.CloseDBConnection();

            return (GiftTable != null) && (GiftTable.Rows.Count > 0);
        }

        /// <summary>
        /// loads a specific gift batch, without gift transactions nor details
        /// </summary>
        [RequireModulePermission("FINANCE-1")]
        public static GiftBatchTDS LoadAGiftBatchSingle(Int32 ALedgerNumber, Int32 ABatchNumber, out Boolean ABatchIsUnposted, out string ACurrencyCode)
        {
            ACurrencyCode = String.Empty;

            #region Validate Arguments

            if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }
            else if (ABatchNumber <= 0)
            {
                throw new EFinanceSystemInvalidBatchNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Batch number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber, ABatchNumber);
            }

            #endregion Validate Arguments

            GiftBatchTDS MainDS = new GiftBatchTDS();

            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("LoadAGiftBatchSingle");
            string CurrencyCode = String.Empty;

            db.ReadTransaction(
                ref Transaction,
                delegate
                {
                    MainDS = LoadAGiftBatchSingle(ALedgerNumber, ABatchNumber, ref Transaction);

                    // now get the gift detail transaction amounts for the gift batch total
                    AGiftDetailTable gd = AGiftDetailAccess.LoadViaAGiftBatch(ALedgerNumber, ABatchNumber, Transaction);

                    MainDS.AGiftBatch[0].GiftBatchTotal = 0;

                    foreach (AGiftDetailRow gdRow in gd.Rows)
                    {
                        MainDS.AGiftBatch[0].GiftBatchTotal += gdRow.GiftTransactionAmount;
                    }

                    CurrencyCode = TFinanceServerLookupWebConnector.GetLedgerBaseCurrency(ALedgerNumber, db);                   
                });

            MainDS.AcceptChanges();

            ABatchIsUnposted = MainDS.AGiftBatch[0].BatchStatus == MFinanceConstants.BATCH_UNPOSTED;
            ACurrencyCode = CurrencyCode;

            db.CloseDBConnection();

            return MainDS;
        }

        /// <summary>
        /// loads a specific gift batch, without gift transactions nor details
        /// </summary>
        /// <param name="ALedgerNumber"></param>
        /// <param name="ABatchNumber"></param>
        /// <param name="ATransaction"></param>
        /// <returns></returns>
        [RequireModulePermission("FINANCE-1")]
        private static GiftBatchTDS LoadAGiftBatchSingle(Int32 ALedgerNumber, Int32 ABatchNumber, ref TDBTransaction ATransaction)
        {
            #region Validate Arguments

            if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }
            else if (ABatchNumber <= 0)
            {
                throw new EFinanceSystemInvalidBatchNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Batch number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber, ABatchNumber);
            }
            else if (ATransaction == null)
            {
                throw new EFinanceSystemDBTransactionNullException(String.Format(Catalog.GetString(
                            "Function:{0} - Database Transaction must not be NULL!"),
                        Utilities.GetMethodName(true)));
            }

            #endregion Validate Arguments

            GiftBatchTDS MainDS = new GiftBatchTDS();

            try
            {
                ALedgerAccess.LoadByPrimaryKey(MainDS, ALedgerNumber, ATransaction);
                AGiftBatchAccess.LoadByPrimaryKey(MainDS, ALedgerNumber, ABatchNumber, ATransaction);

                #region Validate Data

                if ((MainDS.ALedger == null) || (MainDS.ALedger.Count == 0))
                {
                    throw new EFinanceSystemDataTableReturnedNoDataException(String.Format(Catalog.GetString(
                                "Function:{0} - Ledger data for Ledger number {1} does not exist or could not be accessed!"),
                            Utilities.GetMethodName(true),
                            ALedgerNumber));
                }
                else if ((MainDS.AGiftBatch == null) || (MainDS.AGiftBatch.Count == 0))
                {
                    throw new EFinanceSystemDataTableReturnedNoDataException(String.Format(Catalog.GetString(
                                "Function:{0} - Gift Batch data for Batch {1} in Ledger number {2} does not exist or could not be accessed!"),
                            Utilities.GetMethodName(true),
                            ABatchNumber,
                            ALedgerNumber));
                }

                #endregion Validate Data

                MainDS.AcceptChanges();
            }
            catch (Exception ex)
            {
                TLogging.LogException(ex, Utilities.GetMethodSignature());
                throw;
            }

            return MainDS;
        }

        /// <summary>
        /// loads a list of recurring batches for the given ledger
        /// also get the ledger for the base currency etc
        /// TODO: limit to period, limit to batch status, etc
        /// </summary>
        /// <param name="ALedgerNumber"></param>
        /// <returns></returns>
        [RequireModulePermission("FINANCE-1")]
        public static GiftBatchTDS LoadARecurringGiftBatch(Int32 ALedgerNumber)
        {
            #region Validate Arguments

            if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }

            #endregion Validate Arguments

            GiftBatchTDS MainDS = new GiftBatchTDS();

            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("LoadARecurringGiftBatch");

            try
            {
                db.ReadTransaction(
                    ref Transaction,
                    delegate
                    {
                        ALedgerAccess.LoadByPrimaryKey(MainDS, ALedgerNumber, Transaction);
                        ARecurringGiftBatchAccess.LoadViaALedger(MainDS, ALedgerNumber, Transaction);

                        #region Validate Data

                        if ((MainDS.ALedger == null) || (MainDS.ALedger.Count == 0))
                        {
                            throw new EFinanceSystemDataTableReturnedNoDataException(String.Format(Catalog.GetString(
                                        "Function:{0} - Ledger data for Ledger number {1} does not exist or could not be accessed!"),
                                    Utilities.GetMethodName(true),
                                    ALedgerNumber));
                        }

                        #endregion Validate Data
                    });

                MainDS.AcceptChanges();
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
        /// loads a list of recurring batches for the given ledger
        /// also get the ledger for the base currency etc
        /// TODO: limit to period, limit to batch status, etc
        /// </summary>
        /// <param name="ALedgerNumber"></param>
        /// <param name="ABatchNumber"></param>
        /// <returns></returns>
        [RequireModulePermission("FINANCE-1")]
        public static GiftBatchTDS LoadARecurringGiftBatchSingle(Int32 ALedgerNumber, Int32 ABatchNumber)
        {
            #region Validate Arguments

            if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }
            else if (ABatchNumber <= 0)
            {
                throw new EFinanceSystemInvalidBatchNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Batch number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber, ABatchNumber);
            }

            #endregion Validate Arguments

            GiftBatchTDS MainDS = new GiftBatchTDS();

            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("LoadARecurringGiftBatchSingle");

            try
            {
                db.ReadTransaction(
                    ref Transaction,
                    delegate
                    {
                        ALedgerAccess.LoadByPrimaryKey(MainDS, ALedgerNumber, Transaction);
                        ARecurringGiftBatchAccess.LoadByPrimaryKey(MainDS, ALedgerNumber, ABatchNumber, Transaction);

                        #region Validate Data

                        if ((MainDS.ALedger == null) || (MainDS.ALedger.Count == 0))
                        {
                            throw new EFinanceSystemDataTableReturnedNoDataException(String.Format(Catalog.GetString(
                                        "Function:{0} - Ledger data for Ledger number {1} does not exist or could not be accessed!"),
                                    Utilities.GetMethodName(true),
                                    ALedgerNumber));
                        }
                        else if ((MainDS.ARecurringGiftBatch == null) || (MainDS.ARecurringGiftBatch.Count == 0))
                        {
                            throw new EFinanceSystemDataTableReturnedNoDataException(String.Format(Catalog.GetString(
                                        "Function:{0} - Batch data for Recurring Gift Batch {1} in Ledger number {2} does not exist or could not be accessed!"),
                                    Utilities.GetMethodName(true),
                                    ABatchNumber,
                                    ALedgerNumber));
                        }

                        #endregion Validate Data
                    });

                MainDS.AcceptChanges();
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
        /// Retrieve the cost centre code for the recipient
        /// </summary>
        /// <param name="ALedgerNumber"></param>
        /// <param name="ARecipientPartnerKey"></param>
        /// <param name="ARecipientLedgerNumber"></param>
        /// <param name="ADateGiftEntered"></param>
        /// <param name="AMotivationGroupCode"></param>
        /// <param name="AMotivationDetailCode"></param>
        /// <param name="APartnerIsMissingLink"></param>
        /// <returns></returns>
        [RequireModulePermission("FINANCE-1")]
        public static string RetrieveCostCentreCodeForRecipient(Int32 ALedgerNumber,
            Int64 ARecipientPartnerKey,
            Int64 ARecipientLedgerNumber,
            DateTime ADateGiftEntered,
            String AMotivationGroupCode,
            String AMotivationDetailCode,
            out bool APartnerIsMissingLink)
        {
            #region Validate Arguments

            if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }
            else if (ARecipientPartnerKey < 0)
            {
                throw new ArgumentException(String.Format(Catalog.GetString(
                            "Retrieve Cost Centre Code For Recipient ({0}) - Recipient Key is less than 0!"),
                        ARecipientPartnerKey));
            }
            else if (ARecipientLedgerNumber < 0)
            {
                throw new ArgumentException(String.Format(Catalog.GetString(
                            "Retrieve Cost Centre Code For Recipient ({0}) - Ledger Number is less than 0!"),
                        ARecipientPartnerKey));
            }
            else if (AMotivationGroupCode.Length == 0)
            {
                throw new ArgumentException(String.Format(Catalog.GetString(
                            "Retrieve Cost Centre Code For Recipient ({0}) - Motivation Group Code is empty!"),
                        ARecipientPartnerKey));
            }
            else if (AMotivationDetailCode.Length == 0)
            {
                throw new ArgumentException(String.Format(Catalog.GetString(
                            "Retrieve Cost Centre Code For Recipient ({0}) - Motivation Detail Code is empty!"),
                        ARecipientPartnerKey));
            }

            #endregion Validate Arguments

            APartnerIsMissingLink = false;
            bool PartnerIsMissingLink = APartnerIsMissingLink;
            string CostCentreCode = string.Empty;

            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("RetrieveCostCentreCodeForRecipient");

            try
            {
                db.ReadTransaction(
                    ref Transaction,
                    delegate
                    {
                        CostCentreCode = RetrieveCostCentreCodeForRecipient(ALedgerNumber,
                            ARecipientPartnerKey,
                            ARecipientLedgerNumber,
                            ADateGiftEntered,
                            AMotivationGroupCode,
                            AMotivationDetailCode,
                            out PartnerIsMissingLink,
                            Transaction);
                    });
            }
            catch (Exception ex)
            {
                TLogging.LogException(ex, Utilities.GetMethodSignature());
                throw;
            }

            APartnerIsMissingLink = PartnerIsMissingLink;

            db.CloseDBConnection();

            return CostCentreCode;
        }

        /// <summary>
        /// Retrieve the cost centre code for the recipient
        /// </summary>
        [RequireModulePermission("FINANCE-1")]
        private static string RetrieveCostCentreCodeForRecipient(Int32 ALedgerNumber,
            Int64 ARecipientPartnerKey,
            Int64 ARecipientLedgerNumber,
            DateTime ADateGiftEntered,
            String AMotivationGroupCode,
            String AMotivationDetailCode,
            out bool APartnerIsMissingLink,
            TDBTransaction ATransaction)
        {
            #region Validate Arguments

            if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }
            else if (ARecipientPartnerKey < 0)
            {
                throw new ArgumentException(String.Format(Catalog.GetString("Function:{0} - The Recipient Partner Key is less than 0!"),
                        Utilities.GetMethodName(true)));
            }
            else if (ARecipientLedgerNumber < 0)
            {
                throw new ArgumentException(String.Format(Catalog.GetString("Function:{0} - The Recipient Ledger Number is less than 0!"),
                        Utilities.GetMethodName(true)));
            }
            else if (AMotivationGroupCode.Length == 0)
            {
                throw new ArgumentException(String.Format(Catalog.GetString("Function:{0} - The Motivation Group Code is empty!"),
                        Utilities.GetMethodName(true)));
            }
            else if (AMotivationDetailCode.Length == 0)
            {
                throw new ArgumentException(String.Format(Catalog.GetString("Function:{0} - The Motivation Detail Code is empty!"),
                        Utilities.GetMethodName(true)));
            }
            else if (ATransaction == null)
            {
                throw new EFinanceSystemDBTransactionNullException(String.Format(Catalog.GetString(
                            "Function:{0} - Database Transaction must not be NULL!"),
                        Utilities.GetMethodName(true)));
            }

            #endregion Validate Arguments

            APartnerIsMissingLink = false;
            string CostCentreCode = string.Empty;

            //bool KeyMinIsActive = false;
            //bool KeyMinExists = KeyMinistryExists(ARecipientPartnerKey, out KeyMinIsActive);

            if (ARecipientPartnerKey > 0)
            {
                DataTable PartnerCostCentreTbl = null;

                APartnerIsMissingLink = !TGLSetupWebConnector.LoadCostCentrePartnerLinks(ALedgerNumber,
                    ARecipientPartnerKey,
                    out PartnerCostCentreTbl,
                    ATransaction.DataBaseObj);

                if (!APartnerIsMissingLink && (PartnerCostCentreTbl != null) && (PartnerCostCentreTbl.Rows.Count > 0))
                {
                    CostCentreCode = (string)PartnerCostCentreTbl.DefaultView[0].Row["IsLinked"];
                }
                else if (ARecipientLedgerNumber > 0)
                {
                    //Valid ledger number table
                    CheckCostCentreDestinationForRecipient(ALedgerNumber, ARecipientLedgerNumber, out CostCentreCode,
                    ATransaction.DataBaseObj);
                }
            }

            if (CostCentreCode.Length == 0)
            {
                AMotivationDetailTable motivationDetailTable =
                    AMotivationDetailAccess.LoadByPrimaryKey(ALedgerNumber, AMotivationGroupCode, AMotivationDetailCode, ATransaction);

                #region Validate Data 2

                if ((motivationDetailTable == null) || (motivationDetailTable.Count == 0))
                {
                    throw new EFinanceSystemDataTableReturnedNoDataException(String.Format(Catalog.GetString(
                                "Function:{0} - Motivation Detail data for Ledger number {1}, Motivation Group {2} Detail {3} does not exist or could not be accessed!"),
                            Utilities.GetMethodName(true),
                            ALedgerNumber,
                            AMotivationGroupCode,
                            AMotivationDetailCode));
                }

                #endregion Validate Data 2

                CostCentreCode = motivationDetailTable[0].CostCentreCode;
            }

            return CostCentreCode;
        }

        /// <summary>
        /// Check that the chosen recipient ledger is set up for ILT - or that the recipient has a separate cost centre
        /// </summary>
        /// <param name="ALedgerNumber"></param>
        /// <param name="ARecipientPartnerKey"></param>
        /// <param name="ARecipientLedgerNumber"></param>
        /// <param name="AVerificationResults"></param>
        /// <returns></returns>
        [RequireModulePermission("FINANCE-1")]
        public static bool IsRecipientLedgerNumberSetupForILT(Int32 ALedgerNumber,
            Int64 ARecipientPartnerKey,
            Int64 ARecipientLedgerNumber,
            out TVerificationResultCollection AVerificationResults)
        {
            #region Validate Arguments

            if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }
            else if (ARecipientPartnerKey < 0)
            {
                throw new ArgumentException(String.Format(Catalog.GetString("Function:{0} - The Recipient Partner Key is less than 0!"),
                        Utilities.GetMethodName(true)));
            }
            else if (ARecipientLedgerNumber < 0)
            {
                throw new ArgumentException(String.Format(Catalog.GetString("Function:{0} - The Recipient Ledger Number is less than 0!"),
                        Utilities.GetMethodName(true)));
            }

            #endregion Validate Arguments

            AVerificationResults = new TVerificationResultCollection();

            Int32 RecipientLedger = (int)ARecipientLedgerNumber / 1000000;

            try
            {
                if ((RecipientLedger != ALedgerNumber) && (ARecipientPartnerKey > 0))
                {
                    string CostCentreCode = string.Empty;

                    //Valid ledger number table
                    if (!CheckCostCentreDestinationForRecipient(ALedgerNumber, ARecipientLedgerNumber, out CostCentreCode)
                        && !CheckCostCentreDestinationForRecipient(ALedgerNumber, ARecipientPartnerKey, out CostCentreCode))
                    {
                        TPartnerClass Class;
                        string ARecipientLedgerNumberName = string.Empty;
                        TPartnerServerLookups.GetPartnerShortName(ARecipientLedgerNumber, out ARecipientLedgerNumberName, out Class);

                        AVerificationResults.Add(
                            new TVerificationResult(
                                null,
                                string.Format(ErrorCodes.GetErrorInfo(PetraErrorCodes.ERR_RECIPIENTFIELD_NOT_ILT).ErrorMessageText,
                                    ARecipientLedgerNumberName, ARecipientLedgerNumber.ToString("0000000000")),
                                PetraErrorCodes.ERR_RECIPIENTFIELD_NOT_ILT,
                                TResultSeverity.Resv_Critical));

                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                TLogging.LogException(ex, Utilities.GetMethodSignature());
                throw;
            }

            return true;
        }

        /// <summary>
        /// loads a list of gift transactions and details for the given ledger and batch
        /// </summary>
        [RequireModulePermission("FINANCE-1")]
        public static GiftBatchTDS LoadGiftTransactionsForBatch(Int32 ALedgerNumber, Int32 ABatchNumber, out Boolean ABatchIsUnposted, out String ACurrencyCode)
        {
            #region Validate Arguments

            if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }
            else if (ABatchNumber <= 0)
            {
                throw new EFinanceSystemInvalidBatchNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Batch number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber, ABatchNumber);
            }

            #endregion Validate Arguments

            GiftBatchTDS MainDS = new GiftBatchTDS();
            ABatchIsUnposted = false;

            try
            {
                MainDS = LoadAGiftBatchAndRelatedData(ALedgerNumber, ABatchNumber, false);

                #region Validate Data

                if ((MainDS == null) || (MainDS.Tables.Count == 0))
                {
                    throw new EFinanceSystemDataObjectNullOrEmptyException(String.Format(Catalog.GetString(
                                "Function:{0} - Dataset MainDS is NULL or has no tables!"),
                            Utilities.GetMethodName(true),
                            ALedgerNumber));
                }

                #endregion Validate Data

                ABatchIsUnposted = (MainDS.AGiftBatch[0].BatchStatus == MFinanceConstants.BATCH_UNPOSTED);

                // drop all tables apart from AGift and AGiftDetail
                foreach (DataTable table in MainDS.Tables)
                {
                    if ((table.TableName != MainDS.AGift.TableName) && (table.TableName != MainDS.AGiftDetail.TableName))
                    {
                        table.Clear();
                    }
                }

                MainDS.AcceptChanges();
            }
            catch (Exception ex)
            {
                TLogging.LogException(ex, Utilities.GetMethodSignature());
                throw;
            }

            ACurrencyCode = TFinanceServerLookupWebConnector.GetLedgerBaseCurrency(ALedgerNumber);

            return MainDS;
        }

        /// <summary>
        /// loads a list of recurring gift transactions and details for the given ledger and recurring batch
        /// </summary>
        /// <param name="ALedgerNumber"></param>
        /// <param name="ABatchNumber"></param>
        /// <returns></returns>
        [RequireModulePermission("FINANCE-1")]
        public static GiftBatchTDS LoadRecurringGiftTransactionsForBatch(Int32 ALedgerNumber, Int32 ABatchNumber)
        {
            #region Validate Arguments

            if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }
            else if (ABatchNumber <= 0)
            {
                throw new EFinanceSystemInvalidBatchNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Batch number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber, ABatchNumber);
            }

            #endregion Validate Arguments

            GiftBatchTDS MainDS = new GiftBatchTDS();

            try
            {
                MainDS = LoadARecurringGiftBatchAndRelatedData(ALedgerNumber, ABatchNumber);

                #region Validate Data

                if ((MainDS == null) || (MainDS.Tables.Count == 0))
                {
                    throw new EFinanceSystemDataObjectNullOrEmptyException(String.Format(Catalog.GetString(
                                "Function:{0} - Dataset MainDS is NULL or has no tables!"),
                            Utilities.GetMethodName(true),
                            ALedgerNumber));
                }

                #endregion Validate Data

                // drop all tables apart from ARecurringGift and ARecurringGiftDetail
                foreach (DataTable table in MainDS.Tables)
                {
                    if ((table.TableName != MainDS.ARecurringGift.TableName) && (table.TableName != MainDS.ARecurringGiftDetail.TableName))
                    {
                        table.Clear();
                    }
                }

                MainDS.AcceptChanges();
            }
            catch (Exception ex)
            {
                TLogging.LogException(ex, Utilities.GetMethodSignature());
                throw;
            }

            return MainDS;
        }

        /// <summary>
        /// loads a list of gift transactions and details for the given ledger and batch
        /// </summary>
        /// <param name="requestParams"></param>
        /// <param name="AMessages"></param>
        /// <returns></returns>
        [RequireModulePermission("FINANCE-1")]
        public static GiftBatchTDS LoadDonorRecipientHistory(Hashtable requestParams,
            out TVerificationResultCollection AMessages)
        {
            GiftBatchTDS MainDS = new GiftBatchTDS();

            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("LoadDonorRecipientHistory");

            AMessages = new TVerificationResultCollection();

            string tempTableName = (string)requestParams["TempTable"];
            Int32 ledgerNumber = (Int32)requestParams["Ledger"];
            long recipientKey = (Int64)requestParams["Recipient"];
            long donorKey = (Int64)requestParams["Donor"];

            string dateFrom = (string)requestParams["DateFrom"];
            string dateTo = (string)requestParams["DateTo"];
            DateTime startDate;
            DateTime endDate;

            bool noDates = (dateFrom.Length == 0 && dateTo.Length == 0);

            string sqlStmt = string.Empty;

            try
            {
                sqlStmt = TDataBase.ReadSqlFile("Gift.GetDonationsOfDonorAndOrRecipientTemplate.sql");

                OdbcParameter param;

                List <OdbcParameter>parameters = new List <OdbcParameter>();

                param = new OdbcParameter("LedgerNumber", OdbcType.Int);
                param.Value = ledgerNumber;
                parameters.Add(param);
                param = new OdbcParameter("DonorAny", OdbcType.Bit);
                param.Value = (donorKey == 0);
                parameters.Add(param);
                param = new OdbcParameter("DonorKey", OdbcType.BigInt);
                param.Value = donorKey;
                parameters.Add(param);
                param = new OdbcParameter("RecipientAny", OdbcType.Bit);
                param.Value = (recipientKey == 0);
                parameters.Add(param);
                param = new OdbcParameter("RecipientKey", OdbcType.BigInt);
                param.Value = recipientKey;
                parameters.Add(param);

                noDates = (dateFrom.Length == 0 && dateTo.Length == 0);
                param = new OdbcParameter("DateAny", OdbcType.Bit);
                param.Value = noDates;
                parameters.Add(param);

                if (noDates)
                {
                    //These values don't matter because of the value of noDate
                    startDate = new DateTime(2000, 1, 1);
                    endDate = new DateTime(2000, 1, 1);
                }
                else if ((dateFrom.Length > 0) && (dateTo.Length > 0))
                {
                    startDate = Convert.ToDateTime(dateFrom);     //, new CultureInfo("en-US"));
                    endDate = Convert.ToDateTime(dateTo);     //, new CultureInfo("en-US"));
                }
                else if (dateFrom.Length > 0)
                {
                    startDate = Convert.ToDateTime(dateFrom);
                    endDate = new DateTime(2050, 1, 1);
                }
                else
                {
                    startDate = new DateTime(1965, 1, 1);
                    endDate = Convert.ToDateTime(dateTo);
                }

                param = new OdbcParameter("DateFrom", OdbcType.Date);
                param.Value = startDate;
                parameters.Add(param);
                param = new OdbcParameter("DateTo", OdbcType.Date);
                param.Value = endDate;
                parameters.Add(param);

                db.ReadTransaction(
                    ref Transaction,
                    delegate
                    {
                        //Load Ledger Table
                        ALedgerAccess.LoadByPrimaryKey(MainDS, ledgerNumber, Transaction);

                        //Can do this if needed: MainDS.DisableConstraints();
                        db.SelectToTempTable(MainDS, sqlStmt, tempTableName, Transaction, parameters.ToArray(), 0, 0);

                        MainDS.Tables[tempTableName].Columns.Add("DonorDescription");

                        PPartnerTable Tbl = null;

                        // Two scenarios. 1. The donor key is not set which means the Donor Description could be different for every record.
                        if (donorKey == 0)
                        {
                            Tbl = PPartnerAccess.LoadAll(Transaction);

                            foreach (DataRow Row in MainDS.Tables[tempTableName].Rows)
                            {
                                Row["DonorDescription"] = ((PPartnerRow)Tbl.Rows.Find(new object[] { Convert.ToInt64(
                                                                                                         Row["DonorKey"]) })).PartnerShortName;
                            }
                        }
                        // 2. The donor key is set which means the Donor Description will be the same for every record. (Less calculations this way.)
                        else
                        {
                            Tbl = PPartnerAccess.LoadByPrimaryKey(donorKey, Transaction);

                            foreach (DataRow Row in MainDS.Tables[tempTableName].Rows)
                            {
                                Row["DonorDescription"] = Tbl[0].PartnerShortName;
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

            MainDS.AcceptChanges();
            return MainDS;
        }

        /// <summary>
        /// this will store all new and modified batches, gift transactions and details
        /// </summary>
        /// <param name="AInspectDS"></param>
        /// <param name="AVerificationResult"></param>
        /// <param name="ADataBase"></param>
        /// <returns></returns>
        [RequireModulePermission("FINANCE-1")]
        public static TSubmitChangesResult SaveGiftBatchTDS(ref GiftBatchTDS AInspectDS,
            out TVerificationResultCollection AVerificationResult, TDataBase ADataBase = null)
        {
            AVerificationResult = new TVerificationResultCollection();
            TSubmitChangesResult SubmissionResult = TSubmitChangesResult.scrError;

            // make sure that empty tables are removed !! This can return NULL!
            AInspectDS = AInspectDS.GetChangesTyped(true);

            if (AInspectDS == null)
            {
                AVerificationResult.Add(new TVerificationResult(
                        Catalog.GetString("Save Gift Batch"),
                        Catalog.GetString("No changes - nothing to do"),
                        TResultSeverity.Resv_Info));
                return TSubmitChangesResult.scrNothingToBeSaved;
            }

            bool AllValidationsOK = true;

            bool GiftBatchTableInDataSet = (AInspectDS.AGiftBatch != null && AInspectDS.AGiftBatch.Count > 0);
            bool GiftTableInDataSet = (AInspectDS.AGift != null && AInspectDS.AGift.Count > 0);
            bool GiftDetailTableInDataSet = (AInspectDS.AGiftDetail != null && AInspectDS.AGiftDetail.Count > 0);

            //Not needed at present
            //int GiftBatchCount = GiftBatchTableInDataSet ? AInspectDS.AGiftBatch.Count : 0;
            int GiftCount = GiftTableInDataSet ? AInspectDS.AGift.Count : 0;
            int GiftDetailCount = GiftDetailTableInDataSet ? AInspectDS.AGiftDetail.Count : 0;

            bool RecurrGiftBatchTableInDataSet = (AInspectDS.ARecurringGiftBatch != null && AInspectDS.ARecurringGiftBatch.Count > 0);
            bool RecurrGiftTableInDataSet = (AInspectDS.ARecurringGift != null && AInspectDS.ARecurringGift.Count > 0);
            bool RecurrGiftDetailTableInDataSet = (AInspectDS.ARecurringGiftDetail != null && AInspectDS.ARecurringGiftDetail.Count > 0);

            if (RecurrGiftBatchTableInDataSet || RecurrGiftTableInDataSet || RecurrGiftDetailTableInDataSet)
            {
                if (GiftBatchTableInDataSet || GiftTableInDataSet || GiftDetailTableInDataSet)
                {
                    throw new Exception(String.Format("Function:{0} - Recurring and normal gift data found in same changes batch!",
                            Utilities.GetMethodName(true)));
                }

                return SaveRecurringGiftBatchTDS(ref AInspectDS,
                    ref AVerificationResult,
                    RecurrGiftBatchTableInDataSet,
                    RecurrGiftTableInDataSet,
                    RecurrGiftDetailTableInDataSet,
                    ADataBase);
            }
            else
            {
                if (!(GiftBatchTableInDataSet || GiftTableInDataSet || GiftDetailTableInDataSet))
                {
                    throw new Exception(String.Format("Function:{0} - No gift data changes to save!", Utilities.GetMethodName(true)));
                }
            }

            //Get a list of all batches involved
            List <Int32>ListAllGiftBatchesToProcess = new List <int>();

            //Get batch numbers involved
            if (GiftDetailTableInDataSet)
            {
                DataView AllBatchesToProcess = new DataView(AInspectDS.AGiftDetail);
                AllBatchesToProcess.RowStateFilter = DataViewRowState.OriginalRows | DataViewRowState.Added;

                foreach (DataRowView drv in AllBatchesToProcess)
                {
                    AGiftDetailRow gdr = (AGiftDetailRow)drv.Row;
                    int batchNumber;

                    if (gdr.RowState != DataRowState.Deleted)
                    {
                        batchNumber = gdr.BatchNumber;
                    }
                    else
                    {
                        batchNumber = (Int32)gdr[AGiftDetailTable.ColumnBatchNumberId, DataRowVersion.Original];
                    }

                    if (!ListAllGiftBatchesToProcess.Contains(batchNumber))
                    {
                        ListAllGiftBatchesToProcess.Add(batchNumber);
                    }
                }

                ValidateGiftDetail(ref AVerificationResult, AInspectDS.AGiftDetail);
                ValidateGiftDetailManual(ref AVerificationResult, AInspectDS.AGiftDetail);

                if (!TVerificationHelper.IsNullOrOnlyNonCritical(AVerificationResult))
                {
                    AllValidationsOK = false;
                }
            }

            //Get batch numbers involved
            if (GiftTableInDataSet)
            {
                DataView AllBatchesToProcess = new DataView(AInspectDS.AGift);
                AllBatchesToProcess.RowStateFilter = DataViewRowState.OriginalRows | DataViewRowState.Added;

                foreach (DataRowView drv in AllBatchesToProcess)
                {
                    AGiftRow gdr = (AGiftRow)drv.Row;
                    int batchNumber;

                    if (gdr.RowState != DataRowState.Deleted)
                    {
                        batchNumber = gdr.BatchNumber;
                    }
                    else
                    {
                        // for deleted batches
                        batchNumber = (Int32)gdr[AGiftTable.ColumnBatchNumberId, DataRowVersion.Original];
                    }

                    if (!ListAllGiftBatchesToProcess.Contains(batchNumber))
                    {
                        ListAllGiftBatchesToProcess.Add(batchNumber);
                    }
                }
            }

            //Get batch numbers involved
            if (GiftBatchTableInDataSet)
            {
                DataView AllBatchesToProcess = new DataView(AInspectDS.AGiftBatch);
                AllBatchesToProcess.RowStateFilter = DataViewRowState.OriginalRows | DataViewRowState.Added;

                foreach (DataRowView drv in AllBatchesToProcess)
                {
                    AGiftBatchRow gdr = (AGiftBatchRow)drv.Row;
                    int batchNumber;

                    if (gdr.RowState != DataRowState.Deleted)
                    {
                        batchNumber = gdr.BatchNumber;
                    }
                    else
                    {
                        // for deleted batches
                        batchNumber = (Int32)gdr[AGiftBatchTable.ColumnBatchNumberId, DataRowVersion.Original];
                    }

                    if (!ListAllGiftBatchesToProcess.Contains(batchNumber))
                    {
                        ListAllGiftBatchesToProcess.Add(batchNumber);
                    }
                }

                ValidateGiftBatch(ref AVerificationResult, AInspectDS.AGiftBatch);
                ValidateGiftBatchManual(ref AVerificationResult, AInspectDS.AGiftBatch);

                if (!TVerificationHelper.IsNullOrOnlyNonCritical(AVerificationResult))
                {
                    AllValidationsOK = false;
                }
            }

            if (AVerificationResult.Count > 0)
            {
                // Downgrade TScreenVerificationResults to TVerificationResults in order to allow
                // Serialisation (needed for .NET Remoting).
                TVerificationResultCollection.DowngradeScreenVerificationResults(AVerificationResult);
            }

            if (AllValidationsOK)
            {
                if ((GiftCount > 0) && (GiftDetailCount > 1))
                {
                    //The Gift Detail table must be in ascending order
                    AGiftDetailTable cloneDetail = (AGiftDetailTable)AInspectDS.AGiftDetail.Clone();

                    foreach (int batchNumber in ListAllGiftBatchesToProcess)
                    {
                        //Copy across any rows marked as deleted first.
                        DataView giftDetails1 = new DataView(AInspectDS.AGiftDetail);
                        giftDetails1.RowFilter = string.Format("{0}={1}",
                            AGiftDetailTable.GetBatchNumberDBName(),
                            batchNumber);
                        giftDetails1.RowStateFilter = DataViewRowState.Deleted;

                        foreach (DataRowView drv in giftDetails1)
                        {
                            AGiftDetailRow gDetailRow = (AGiftDetailRow)drv.Row;
                            cloneDetail.ImportRow(gDetailRow);
                        }

                        //Import the other rows in ascending order
                        DataView giftDetails2 = new DataView(AInspectDS.AGiftDetail);
                        giftDetails2.RowFilter = string.Format("{0}={1}",
                            AGiftDetailTable.GetBatchNumberDBName(),
                            batchNumber);

                        giftDetails2.Sort = String.Format("{0} ASC, {1} ASC, {2} ASC",
                            AGiftDetailTable.GetBatchNumberDBName(),
                            AGiftDetailTable.GetGiftTransactionNumberDBName(),
                            AGiftDetailTable.GetDetailNumberDBName());

                        foreach (DataRowView giftDetailRows in giftDetails2)
                        {
                            AGiftDetailRow gDR = (AGiftDetailRow)giftDetailRows.Row;
                            cloneDetail.ImportRow(gDR);
                        }
                    }

                    //Clear the table and import the rows from the clone
                    AInspectDS.AGiftDetail.Clear();

                    for (int i = 0; i < GiftDetailCount; i++)
                    {
                        AGiftDetailRow gDR2 = (AGiftDetailRow)cloneDetail[i];
                        AInspectDS.AGiftDetail.ImportRow(gDR2);
                    }
                }

                GiftBatchTDSAccess.SubmitChanges(AInspectDS, ADataBase);

                SubmissionResult = TSubmitChangesResult.scrOK;

                if (GiftTableInDataSet)
                {
                    if (GiftDetailTableInDataSet)
                    {
                        AInspectDS.AGiftDetail.AcceptChanges();
                    }

                    AInspectDS.AGift.AcceptChanges();

                    if (AInspectDS.AGift.Count > 0)
                    {
                        AGiftRow tranR = (AGiftRow)AInspectDS.AGift.Rows[0];

                        Int32 currentLedger = tranR.LedgerNumber;
                        Int32 currentBatch = tranR.BatchNumber;
                        Int32 giftToDelete = 0;

                        try
                        {
                            DataRow[] foundGiftsForDeletion = AInspectDS.AGift.Select(String.Format("{0} = '{1}'",
                                    AGiftTable.GetGiftStatusDBName(),
                                    MFinanceConstants.MARKED_FOR_DELETION));

                            if (foundGiftsForDeletion.Length > 0)
                            {
                                AGiftRow giftRowClient = null;

                                for (int i = 0; i < foundGiftsForDeletion.Length; i++)
                                {
                                    //A gift has been deleted
                                    giftRowClient = (AGiftRow)foundGiftsForDeletion[i];

                                    giftToDelete = giftRowClient.GiftTransactionNumber;
                                    TLogging.Log(String.Format("Gift to Delete: {0} from Batch: {1}",
                                            giftToDelete,
                                            currentBatch));

                                    giftRowClient.Delete();
                                }
                            }

                            GiftBatchTDSAccess.SubmitChanges(AInspectDS, ADataBase);

                            SubmissionResult = TSubmitChangesResult.scrOK;
                        }
                        catch (Exception ex)
                        {
                            TLogging.LogException(ex, Utilities.GetMethodSignature());
                            throw;
                        }
                    }
                }
            }

            return SubmissionResult;
        }

        /// cancel a Gift Batch
        [RequireModulePermission("FINANCE-2")]
        public static bool CancelBatch(
            Int32 ALedgerNumber,
            Int32 ABatchNumber,
            out TVerificationResultCollection AVerificationResult)
        {
            bool BatchIsUnposted;
            string CurrencyCode;
            GiftBatchTDS MainDS = LoadAGiftBatchSingle(ALedgerNumber, ABatchNumber, out BatchIsUnposted, out CurrencyCode);
            AVerificationResult = new TVerificationResultCollection();

            if (!BatchIsUnposted)
            {
                AVerificationResult.Add(new TVerificationResult(
                    Catalog.GetString("Cancel Gift Batch"),
                    "Cannot cancel because status of batch is " + MainDS.AGiftBatch[0].BatchStatus,
                    "Cannot cancel",
                    "ERROR_MODIFY_CLOSED_GIFTBATCH",
                    TResultSeverity.Resv_Critical));
                    return false;
            }

            if (MainDS.AGiftBatch.Rows.Count != 1)
            {
                return false;
            }

            AGiftBatchRow row = MainDS.AGiftBatch[0];
            row.BatchStatus = MFinanceConstants.BATCH_CANCELLED;

            try
            {
                SaveGiftBatchTDS(ref MainDS, out AVerificationResult);
            }
            catch (Exception)
            {
                return false;
            }
            
            return true;
        }

        /// <summary>
        /// return a string that shows the totals for each Motivation Detail
        /// </summary>
        [RequireModulePermission("FINANCE-1")]
        public static bool PreviewGiftBatch(Int32 ALedgerNumber, Int32 ABatchNumber, out string ResultingTotals)
        {
            ResultingTotals = String.Empty;

            #region Validate Arguments

            if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }
            else if (ABatchNumber <= 0)
            {
                throw new EFinanceSystemInvalidBatchNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Batch number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber, ABatchNumber);
            }

            #endregion Validate Arguments

            GiftBatchTDS MainDS = LoadAGiftBatchAndRelatedData(ALedgerNumber, ABatchNumber);

            if (MainDS.AGiftBatch.Rows.Count != 1)
            {
                return false;
            }

            Dictionary<string, decimal> AmountsPerMotivationDetail = new Dictionary<string, decimal>();

            foreach (AGiftDetailRow row in MainDS.AGiftDetail.Rows)
            {
                string motivationID = row.MotivationGroupCode + " - " + row.MotivationDetailCode;
                if (!AmountsPerMotivationDetail.ContainsKey(motivationID))
                {
                    AmountsPerMotivationDetail.Add(motivationID, row.GiftTransactionAmount);
                }
                else
                {
                    AmountsPerMotivationDetail[motivationID] += row.GiftTransactionAmount;
                }
            }

            foreach (string motivationID in AmountsPerMotivationDetail.Keys)
            {
                // return formatted string
                ResultingTotals += motivationID + ": " +
                    AmountsPerMotivationDetail[motivationID].ToString("#.##") +
                    "<br/>";
            }

            return true;
        }

        /// <summary>
        /// this will save and delete a batch
        /// </summary>
        [RequireModulePermission("FINANCE-1")]
        public static bool MaintainBatches(
            string action,
            Int32 ALedgerNumber,
            Int32 ABatchNumber,
            string ABatchDescription,
            DateTime AGlEffectiveDate,
            string ABankAccountCode,
            string ABankCostCentre,
            out TVerificationResultCollection AVerificationResult)
        {
            bool BatchIsUnposted;
            string CurrencyCode;
            GiftBatchTDS MainDS = LoadAGiftBatchSingle(ALedgerNumber, ABatchNumber, out BatchIsUnposted, out CurrencyCode);
            AVerificationResult = new TVerificationResultCollection();

            if (action != "create")
            {
                if (!BatchIsUnposted)
                {
                    AVerificationResult.Add(new TVerificationResult(
                        Catalog.GetString("Save Gift Batch"),
                        "Cannot save because status of batch is " + MainDS.AGiftBatch[0].BatchStatus,
                        "Cannot save",
                        "ERROR_MODIFY_CLOSED_GIFTBATCH",
                        TResultSeverity.Resv_Critical));
                        return false;
                }
            }

            if ((action == "create") || (action == "edit"))
            {

                if (MainDS.AGiftBatch.Rows.Count != 1)
                {
                    return false;
                }

                AGiftBatchRow row = MainDS.AGiftBatch[0];

                row.BatchDescription = ABatchDescription;
                row.GlEffectiveDate = AGlEffectiveDate;
                row.BankAccountCode = ABankAccountCode;
                row.BankCostCentre = ABankCostCentre;

                TDBTransaction ReadTransaction = new TDBTransaction();
                TDataBase db = DBAccess.Connect("MaintainBatches");

                try
                {
                    db.ReadTransaction(
                        ref ReadTransaction,
                        delegate
                        {
                            int DateEffectivePeriod, DateEffectiveYear;

                            TFinancialYear.IsValidPostingPeriod(row.LedgerNumber,
                                row.GlEffectiveDate,
                                out DateEffectivePeriod,
                                out DateEffectiveYear,
                                ReadTransaction);
                            row.BatchPeriod = DateEffectivePeriod;
                            row.BatchYear = DateEffectiveYear;
                        });

                }
                catch (Exception ex)
                {
                    TLogging.LogException(ex, Utilities.GetMethodSignature());
                    throw;
                }

                try
                {
                    SaveGiftBatchTDS(ref MainDS, out AVerificationResult);
                }
                catch (Exception)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            if (!TVerificationHelper.IsNullOrOnlyNonCritical(AVerificationResult))
            {
                TLogging.Log(AVerificationResult.BuildVerificationResultString());
                return false;
            }

            return true;
        }

        /// <summary>
        /// this will save and delete a recurring gift batch
        /// </summary>
        [RequireModulePermission("FINANCE-1")]
        public static bool MaintainRecurringBatches(
            string action,
            Int32 ALedgerNumber,
            Int32 ABatchNumber,
            string ABatchDescription,
            string ABankAccountCode,
            string ABankCostCentre,
            out TVerificationResultCollection AVerificationResult)
        {
            GiftBatchTDS MainDS = LoadARecurringGiftBatchSingle(ALedgerNumber, ABatchNumber);
            AVerificationResult = new TVerificationResultCollection();

            if ((action == "create") || (action == "edit"))
            {

                if (MainDS.ARecurringGiftBatch.Rows.Count != 1)
                {
                    return false;
                }

                ARecurringGiftBatchRow row = MainDS.ARecurringGiftBatch[0];

                row.BatchDescription = ABatchDescription;
                row.BankAccountCode = ABankAccountCode;
                row.BankCostCentre = ABankCostCentre;

                try
                {
                    SaveGiftBatchTDS(ref MainDS, out AVerificationResult);
                }
                catch (Exception)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            if (!TVerificationHelper.IsNullOrOnlyNonCritical(AVerificationResult))
            {
                TLogging.Log(AVerificationResult.BuildVerificationResultString());
                return false;
            }

            return true;
        }

        /// <summary>
        /// this will create, save and delete a gift transaction
        /// </summary>
        [RequireModulePermission("FINANCE-1")]
        public static bool MaintainGifts(
            string action,
            Int32 ALedgerNumber,
            Int32 ABatchNumber,
            Int32 AGiftTransactionNumber,
            DateTime ADateEntered,
            Int64 ADonorKey,
            string AReference,
            out TVerificationResultCollection AVerificationResult)
        {
            bool BatchIsUnposted;
            string CurrencyCode;
            GiftBatchTDS MainDS = LoadGiftTransactionsForBatch(ALedgerNumber, ABatchNumber, out BatchIsUnposted, out CurrencyCode);
            AVerificationResult = new TVerificationResultCollection();

            if (action != "create")
            {
                if (!BatchIsUnposted)
                {
                    AVerificationResult.Add(new TVerificationResult(
                        "Save Gift Batch",
                        "Cannot save because status of batch is " + MainDS.AGiftBatch[0].BatchStatus,
                        "Cannot save",
                        "ERROR_MODIFY_CLOSED_GIFTBATCH",
                        TResultSeverity.Resv_Critical));
                        return false;
                }
            }

            if (action == "create")
            {
                AGiftRow row = MainDS.AGift.NewRowTyped();
                row.LedgerNumber = ALedgerNumber;
                row.BatchNumber = ABatchNumber;
                row.GiftTransactionNumber = AGiftTransactionNumber;
                row.DateEntered = ADateEntered;
                row.DonorKey = ADonorKey;
                row.Reference = AReference;
                MainDS.AGift.Rows.Add(row);

                // TODO update gift batch last transaction number???

                try
                {
                    SaveGiftBatchTDS(ref MainDS, out AVerificationResult);
                }
                catch (Exception)
                {
                    return false;
                }
            }
            else if (action == "edit")
            {
                foreach (AGiftRow row in MainDS.AGift.Rows)
                {
                    if (row.GiftTransactionNumber == AGiftTransactionNumber)
                    {
                        row.DateEntered = ADateEntered;
                        row.DonorKey = ADonorKey;
                        row.Reference = AReference;
                    }
                }

                try
                {
                    SaveGiftBatchTDS(ref MainDS, out AVerificationResult);
                }
                catch (Exception)
                {
                    return false;
                }
            }
            else if (action == "delete")
            {
                foreach (AGiftDetailRow row in MainDS.AGiftDetail.Rows)
                {
                    if (row.GiftTransactionNumber == AGiftTransactionNumber)
                    {
                        row.Delete();
                    }
                    else if (row.GiftTransactionNumber > AGiftTransactionNumber)
                    {
                        row.GiftTransactionNumber--;
                    }
                }

                foreach (AGiftRow row in MainDS.AGift.Rows)
                {
                    if (row.GiftTransactionNumber == AGiftTransactionNumber)
                    {
                        row.Delete();
                    }
                    else if (row.GiftTransactionNumber > AGiftTransactionNumber)
                    {
                        row.GiftTransactionNumber--;
                    }
                }

                try
                {
                    SaveGiftBatchTDS(ref MainDS, out AVerificationResult);
                }
                catch (Exception)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            if (!TVerificationHelper.IsNullOrOnlyNonCritical(AVerificationResult))
            {
                TLogging.Log(AVerificationResult.BuildVerificationResultString());
                return false;
            }

            return true;
        }

        /// <summary>
        /// this will create, save and delete a recurring gift transaction
        /// </summary>
        [RequireModulePermission("FINANCE-1")]
        public static bool MaintainRecurringGifts(
            string action,
            Int32 ALedgerNumber,
            Int32 ABatchNumber,
            Int32 AGiftTransactionNumber,
            Int64 ADonorKey,
            string AReference,
            string AIBAN,
            string ASEPAMandate,
            out TVerificationResultCollection AVerificationResult)
        {
            GiftBatchTDS MainDS = LoadRecurringGiftTransactionsForBatch(ALedgerNumber, ABatchNumber);
            AVerificationResult = new TVerificationResultCollection();

            if (action == "create")
            {
                ARecurringGiftRow row = MainDS.ARecurringGift.NewRowTyped();
                row.LedgerNumber = ALedgerNumber;
                row.BatchNumber = ABatchNumber;
                row.GiftTransactionNumber = AGiftTransactionNumber;
                row.DonorKey = ADonorKey;
                row.Reference = AReference;
                // TODO: set IBAN of main bank account of partner
                // TODO: set SEPAMandate in p_data_label_value_partner
                // UpdateSEPAMandate(ALedgerNumber, ADonorKey, AIBAN, ASEPAMandate);
                MainDS.ARecurringGift.Rows.Add(row);

                // TODO update recurring gift batch last transaction number???

                try
                {
                    SaveGiftBatchTDS(ref MainDS, out AVerificationResult);
                }
                catch (Exception)
                {
                    return false;
                }
            }
            else if (action == "edit")
            {
                foreach (ARecurringGiftRow row in MainDS.ARecurringGift.Rows)
                {
                    if (row.GiftTransactionNumber == AGiftTransactionNumber)
                    {
                        row.DonorKey = ADonorKey;
                        row.Reference = AReference;
                    }
                }

                try
                {
                    SaveGiftBatchTDS(ref MainDS, out AVerificationResult);
                }
                catch (Exception)
                {
                    return false;
                }
            }
            else if (action == "delete")
            {
                foreach (ARecurringGiftDetailRow row in MainDS.ARecurringGiftDetail.Rows)
                {
                    if (row.GiftTransactionNumber == AGiftTransactionNumber)
                    {
                        row.Delete();
                    }
                    else if (row.GiftTransactionNumber > AGiftTransactionNumber)
                    {
                        row.GiftTransactionNumber--;
                    }
                }

                foreach (ARecurringGiftRow row in MainDS.ARecurringGift.Rows)
                {
                    if (row.GiftTransactionNumber == AGiftTransactionNumber)
                    {
                        row.Delete();
                    }
                    else if (row.GiftTransactionNumber > AGiftTransactionNumber)
                    {
                        row.GiftTransactionNumber--;
                    }
                }

                try
                {
                    SaveGiftBatchTDS(ref MainDS, out AVerificationResult);
                }
                catch (Exception)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            if (!TVerificationHelper.IsNullOrOnlyNonCritical(AVerificationResult))
            {
                TLogging.Log(AVerificationResult.BuildVerificationResultString());
                return false;
            }

            return true;
        }

        /// <summary>
        /// this will create, save and delete a gift detail
        /// </summary>
        [RequireModulePermission("FINANCE-1")]
        public static bool MaintainGiftDetails(
            string action,
            Int32 ALedgerNumber,
            Int32 ABatchNumber,
            Int32 AGiftTransactionNumber,
            Int32 ADetailNumber,
            Decimal AGiftTransactionAmount,
            string AGiftCommentOne,
            string AMotivationGroupCode,
            string AMotivationDetailCode,
            Int64 ARecipientKey,
            out TVerificationResultCollection AVerificationResult)
        {
            bool BatchIsUnposted;
            string CurrencyCode;
            GiftBatchTDS MainDS = LoadGiftTransactionsForBatch(ALedgerNumber, ABatchNumber, out BatchIsUnposted, out CurrencyCode);
            AVerificationResult = new TVerificationResultCollection();

            if (action != "create")
            {
                if (!BatchIsUnposted)
                {
                    AVerificationResult.Add(new TVerificationResult(
                        "Save Gift Batch",
                        "Cannot save because status of batch is " + MainDS.AGiftBatch[0].BatchStatus,
                        "Cannot save",
                        "ERROR_MODIFY_CLOSED_GIFTBATCH",
                        TResultSeverity.Resv_Critical));
                        return false;
                }
            }

            if (action == "create")
            {
                AGiftDetailRow row = MainDS.AGiftDetail.NewRowTyped();
                row.LedgerNumber = ALedgerNumber;
                row.BatchNumber = ABatchNumber;
                row.GiftTransactionNumber = AGiftTransactionNumber;
                row.DetailNumber = ADetailNumber;
                row.GiftTransactionAmount = AGiftTransactionAmount;
                row.GiftCommentOne = AGiftCommentOne;
                row.CommentOneType = MFinanceConstants.GIFT_COMMENT_TYPE_OFFICE;
                row.MotivationGroupCode = AMotivationGroupCode;
                row.MotivationDetailCode = AMotivationDetailCode;
                row.RecipientKey = ARecipientKey;
                MainDS.AGiftDetail.Rows.Add(row);

                // TODO update gift last detail number???

                try
                {
                    SaveGiftBatchTDS(ref MainDS, out AVerificationResult);
                }
                catch (Exception)
                {
                    return false;
                }
            }
            else if (action == "edit")
            {
                foreach (AGiftDetailRow row in MainDS.AGiftDetail.Rows)
                {
                    if ((row.GiftTransactionNumber == AGiftTransactionNumber) && (row.DetailNumber == ADetailNumber))
                    {
                        row.GiftTransactionAmount = AGiftTransactionAmount;
                        row.GiftCommentOne = AGiftCommentOne;
                        row.CommentOneType = MFinanceConstants.GIFT_COMMENT_TYPE_OFFICE;
                        row.MotivationGroupCode = AMotivationGroupCode;
                        row.MotivationDetailCode = AMotivationDetailCode;
                        row.RecipientKey = ARecipientKey;
                    }
                }

                try
                {
                    SaveGiftBatchTDS(ref MainDS, out AVerificationResult);
                }
                catch (Exception)
                {
                    return false;
                }
            }
            else if (action == "delete")
            {
                foreach (AGiftDetailRow row in MainDS.AGiftDetail.Rows)
                {
                    if ((row.GiftTransactionNumber == AGiftTransactionNumber) && (row.DetailNumber == ADetailNumber))
                    {
                        row.Delete();
                    }
                    else if ((row.GiftTransactionNumber == AGiftTransactionNumber) && row.DetailNumber > ADetailNumber)
                    {
                        row.DetailNumber--;
                    }
                }

                try
                {
                    SaveGiftBatchTDS(ref MainDS, out AVerificationResult);
                }
                catch (Exception)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            if (!TVerificationHelper.IsNullOrOnlyNonCritical(AVerificationResult))
            {
                TLogging.Log(AVerificationResult.BuildVerificationResultString());
                return false;
            }

            return true;
        }

        /// <summary>
        /// this will create, save and delete a recurring gift detail
        /// </summary>
        [RequireModulePermission("FINANCE-1")]
        public static bool MaintainRecurringGiftDetails(
            string action,
            Int32 ALedgerNumber,
            Int32 ABatchNumber,
            Int32 AGiftTransactionNumber,
            Int32 ADetailNumber,
            Decimal AGiftAmount,
            string AGiftCommentOne,
            string AMotivationGroupCode,
            string AMotivationDetailCode,
            Int64 ARecipientKey,
            DateTime AStartDonations,
            DateTime? AEndDonations,
            out TVerificationResultCollection AVerificationResult)
        {
            GiftBatchTDS MainDS = LoadRecurringGiftTransactionsForBatch(ALedgerNumber, ABatchNumber);
            AVerificationResult = new TVerificationResultCollection();

            if (action == "create")
            {
                ARecurringGiftDetailRow row = MainDS.ARecurringGiftDetail.NewRowTyped();
                row.LedgerNumber = ALedgerNumber;
                row.BatchNumber = ABatchNumber;
                row.GiftTransactionNumber = AGiftTransactionNumber;
                row.DetailNumber = ADetailNumber;
                row.GiftAmount = AGiftAmount;
                row.GiftCommentOne = AGiftCommentOne;
                row.CommentOneType = MFinanceConstants.GIFT_COMMENT_TYPE_OFFICE;
                row.MotivationGroupCode = AMotivationGroupCode;
                row.MotivationDetailCode = AMotivationDetailCode;
                row.RecipientKey = ARecipientKey;
                row.StartDonations = AStartDonations;
                if (AEndDonations.HasValue)
                {
                    row.EndDonations = AEndDonations.Value;
                }
                MainDS.ARecurringGiftDetail.Rows.Add(row);

                // TODO update recurring gift last detail number???

                try
                {
                    SaveGiftBatchTDS(ref MainDS, out AVerificationResult);
                }
                catch (Exception)
                {
                    return false;
                }
            }
            else if (action == "edit")
            {
                foreach (ARecurringGiftDetailRow row in MainDS.ARecurringGiftDetail.Rows)
                {
                    if ((row.GiftTransactionNumber == AGiftTransactionNumber) && (row.DetailNumber == ADetailNumber))
                    {
                        row.GiftAmount = AGiftAmount;
                        row.GiftCommentOne = AGiftCommentOne;
                        row.CommentOneType = MFinanceConstants.GIFT_COMMENT_TYPE_OFFICE;
                        row.MotivationGroupCode = AMotivationGroupCode;
                        row.MotivationDetailCode = AMotivationDetailCode;
                        row.RecipientKey = ARecipientKey;
                        row.StartDonations = AStartDonations;
                        if (AEndDonations.HasValue)
                        {
                            row.EndDonations = AEndDonations.Value;
                        }
                        else
                        {
                            row.SetEndDonationsNull();
                        }
                    }
                }

                try
                {
                    SaveGiftBatchTDS(ref MainDS, out AVerificationResult);
                }
                catch (Exception)
                {
                    return false;
                }
            }
            else if (action == "delete")
            {
                foreach (ARecurringGiftDetailRow row in MainDS.ARecurringGiftDetail.Rows)
                {
                    if ((row.GiftTransactionNumber == AGiftTransactionNumber) && (row.DetailNumber == ADetailNumber))
                    {
                        row.Delete();
                    }
                    else if ((row.GiftTransactionNumber == AGiftTransactionNumber) && row.DetailNumber > ADetailNumber)
                    {
                        row.DetailNumber--;
                    }
                }

                try
                {
                    SaveGiftBatchTDS(ref MainDS, out AVerificationResult);
                }
                catch (Exception)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            if (!TVerificationHelper.IsNullOrOnlyNonCritical(AVerificationResult))
            {
                TLogging.Log(AVerificationResult.BuildVerificationResultString());
                return false;
            }

            return true;
        }

        /// <summary>
        /// This will store all new and modified recurring batches, recurring gift transactions and recurring details
        /// </summary>
        /// <param name="AInspectDS"></param>
        /// <param name="AVerificationResult"></param>
        /// <param name="ARecurringGiftBatchTableInDataSet"></param>
        /// <param name="ARecurringGiftTableInDataSet"></param>
        /// <param name="ARecurringGiftDetailTableInDataSet"></param>
        /// <param name="ADataBase"></param>
        /// <returns></returns>
        [RequireModulePermission("FINANCE-1")]
        private static TSubmitChangesResult SaveRecurringGiftBatchTDS(ref GiftBatchTDS AInspectDS,
            ref TVerificationResultCollection AVerificationResult, bool ARecurringGiftBatchTableInDataSet,
            bool ARecurringGiftTableInDataSet, bool ARecurringGiftDetailTableInDataSet,
            TDataBase ADataBase)
        {
            TSubmitChangesResult SubmissionResult = TSubmitChangesResult.scrError;

            bool AllValidationsOK = true;

            //Not needed as yet
            //int RecurrGiftBatchCount = ARecurringGiftBatchTableInDataSet ? AInspectDS.ARecurringGiftBatch.Count : 0;
            int RecurrGiftCount = ARecurringGiftTableInDataSet ? AInspectDS.ARecurringGift.Count : 0;
            int RecurrGiftDetailCount = ARecurringGiftDetailTableInDataSet ? AInspectDS.ARecurringGiftDetail.Count : 0;

            //Get a list of all batches involved
            List <Int32>ListAllGiftBatchesToProcess = new List <int>();

            if (ARecurringGiftDetailTableInDataSet)
            {
                DataView AllBatchesToProcess = new DataView(AInspectDS.ARecurringGiftDetail);
                AllBatchesToProcess.RowStateFilter = DataViewRowState.OriginalRows | DataViewRowState.Added;

                foreach (DataRowView drv in AllBatchesToProcess)
                {
                    ARecurringGiftDetailRow gdr = (ARecurringGiftDetailRow)drv.Row;
                    int batchNumber;

                    if (gdr.RowState != DataRowState.Deleted)
                    {
                        batchNumber = gdr.BatchNumber;
                    }
                    else
                    {
                        // for deleted batches
                        batchNumber = (Int32)gdr[ARecurringGiftDetailTable.ColumnBatchNumberId, DataRowVersion.Original];
                    }

                    if (!ListAllGiftBatchesToProcess.Contains(batchNumber))
                    {
                        ListAllGiftBatchesToProcess.Add(batchNumber);
                    }
                }

                ValidateRecurringGiftDetail(ref AVerificationResult, AInspectDS.ARecurringGiftDetail);
                ValidateRecurringGiftDetailManual(ref AVerificationResult, AInspectDS.ARecurringGiftDetail);

                if (!TVerificationHelper.IsNullOrOnlyNonCritical(AVerificationResult))
                {
                    AllValidationsOK = false;
                }
            }

            if (ARecurringGiftTableInDataSet)
            {
                DataView AllBatchesToProcess = new DataView(AInspectDS.ARecurringGift);
                AllBatchesToProcess.RowStateFilter = DataViewRowState.OriginalRows | DataViewRowState.Added;

                foreach (DataRowView drv in AllBatchesToProcess)
                {
                    ARecurringGiftRow gdr = (ARecurringGiftRow)drv.Row;
                    int batchNumber;

                    if (gdr.RowState != DataRowState.Deleted)
                    {
                        batchNumber = gdr.BatchNumber;
                    }
                    else
                    {
                        // for deleted batches
                        batchNumber = (Int32)gdr[ARecurringGiftTable.ColumnBatchNumberId, DataRowVersion.Original];
                    }

                    if (!ListAllGiftBatchesToProcess.Contains(batchNumber))
                    {
                        ListAllGiftBatchesToProcess.Add(batchNumber);
                    }
                }
            }

            if (ARecurringGiftBatchTableInDataSet)
            {
                DataView AllBatchesToProcess = new DataView(AInspectDS.ARecurringGiftBatch);
                AllBatchesToProcess.RowStateFilter = DataViewRowState.OriginalRows | DataViewRowState.Added;

                foreach (DataRowView drv in AllBatchesToProcess)
                {
                    ARecurringGiftBatchRow gdr = (ARecurringGiftBatchRow)drv.Row;
                    int batchNumber;

                    if (gdr.RowState != DataRowState.Deleted)
                    {
                        batchNumber = gdr.BatchNumber;
                    }
                    else
                    {
                        // for deleted batches
                        batchNumber = (Int32)gdr[ARecurringGiftBatchTable.ColumnBatchNumberId, DataRowVersion.Original];
                    }

                    if (!ListAllGiftBatchesToProcess.Contains(batchNumber))
                    {
                        ListAllGiftBatchesToProcess.Add(batchNumber);
                    }
                }

                ValidateRecurringGiftBatch(ref AVerificationResult, AInspectDS.ARecurringGiftBatch);
                ValidateRecurringGiftBatchManual(ref AVerificationResult, AInspectDS.ARecurringGiftBatch);

                if (!TVerificationHelper.IsNullOrOnlyNonCritical(AVerificationResult))
                {
                    AllValidationsOK = false;
                }
            }

            if (AVerificationResult.Count > 0)
            {
                // Downgrade TScreenVerificationResults to TVerificationResults in order to allow
                // Serialisation (needed for .NET Remoting).
                TVerificationResultCollection.DowngradeScreenVerificationResults(AVerificationResult);
            }

            //TODO: multi-delete
            //Get a list of all batches to delete - for multi-delete
            //List <Int32>ListAllGiftBatchesToDelete = new List <int>();

            //DataView AllBatchesToDelete = new DataView(AInspectDS.ARecurringGiftBatch);
            //AllBatchesToDelete.RowStateFilter = DataViewRowState.Deleted;

            //foreach (DataRowView drv in AllBatchesToDelete)
            //{
            //    ListAllGiftBatchesToDelete.Add((int)(drv[ARecurringGiftBatchTable.ColumnBatchNumberId]));
            //}

            if (AllValidationsOK)
            {
                if ((RecurrGiftCount > 0) && (RecurrGiftDetailCount > 1))
                {
                    //The Gift Detail table must be in ascending order
                    ARecurringGiftDetailTable cloneDetail = (ARecurringGiftDetailTable)AInspectDS.ARecurringGiftDetail.Clone();

                    foreach (int batchNumber in ListAllGiftBatchesToProcess)
                    {
                        //Copy across any rows marked as deleted first.
                        DataView giftDetails1 = new DataView(AInspectDS.ARecurringGiftDetail);
                        giftDetails1.RowFilter = string.Format("{0}={1}",
                            ARecurringGiftDetailTable.GetBatchNumberDBName(),
                            batchNumber);
                        giftDetails1.RowStateFilter = DataViewRowState.Deleted;

                        foreach (DataRowView drv in giftDetails1)
                        {
                            ARecurringGiftDetailRow gDeletedDetailRow = (ARecurringGiftDetailRow)drv.Row;
                            cloneDetail.ImportRow(gDeletedDetailRow);
                        }

                        //Import the other rows in ascending order
                        DataView giftDetails2 = new DataView(AInspectDS.ARecurringGiftDetail);
                        giftDetails2.RowFilter = string.Format("{0}={1}",
                            ARecurringGiftDetailTable.GetBatchNumberDBName(),
                            batchNumber);

                        giftDetails2.Sort = String.Format("{0} ASC, {1} ASC, {2} ASC",
                            ARecurringGiftDetailTable.GetBatchNumberDBName(),
                            ARecurringGiftDetailTable.GetGiftTransactionNumberDBName(),
                            ARecurringGiftDetailTable.GetDetailNumberDBName());

                        foreach (DataRowView giftDetailRows in giftDetails2)
                        {
                            ARecurringGiftDetailRow gDR = (ARecurringGiftDetailRow)giftDetailRows.Row;
                            cloneDetail.ImportRow(gDR);
                        }
                    }

                    //Clear the table and import the rows from the clone
                    AInspectDS.ARecurringGiftDetail.Clear();

                    for (int i = 0; i < RecurrGiftDetailCount; i++)
                    {
                        ARecurringGiftDetailRow gDR2 = (ARecurringGiftDetailRow)cloneDetail[i];
                        AInspectDS.ARecurringGiftDetail.ImportRow(gDR2);
                    }
                }

                GiftBatchTDSAccess.SubmitChanges(AInspectDS, ADataBase);

                SubmissionResult = TSubmitChangesResult.scrOK;

                if (ARecurringGiftTableInDataSet)
                {
                    if (ARecurringGiftDetailTableInDataSet)
                    {
                        AInspectDS.ARecurringGiftDetail.AcceptChanges();
                    }

                    AInspectDS.ARecurringGift.AcceptChanges();

                    if (AInspectDS.ARecurringGift.Count > 0)
                    {
                        ARecurringGiftRow tranR = (ARecurringGiftRow)AInspectDS.ARecurringGift.Rows[0];

                        Int32 currentLedger = tranR.LedgerNumber;
                        Int32 currentBatch = tranR.BatchNumber;
                        Int32 giftToDelete = 0;

                        try
                        {
                            DataRow[] foundGiftsForDeletion = AInspectDS.ARecurringGift.Select(String.Format("{0} = '{1}'",
                                    ARecurringGiftTable.GetChargeStatusDBName(),
                                    MFinanceConstants.MARKED_FOR_DELETION));

                            if (foundGiftsForDeletion.Length > 0)
                            {
                                ARecurringGiftRow giftRowClient = null;

                                for (int i = 0; i < foundGiftsForDeletion.Length; i++)
                                {
                                    //A gift has been deleted
                                    giftRowClient = (ARecurringGiftRow)foundGiftsForDeletion[i];

                                    giftToDelete = giftRowClient.GiftTransactionNumber;
                                    TLogging.Log(String.Format("Gift to Delete: {0} from Recurring Batch: {1}",
                                            giftToDelete,
                                            currentBatch));

                                    giftRowClient.Delete();
                                }
                            }

                            GiftBatchTDSAccess.SubmitChanges(AInspectDS, ADataBase);

                            SubmissionResult = TSubmitChangesResult.scrOK;
                        }
                        catch (Exception ex)
                        {
                            TLogging.LogException(ex, Utilities.GetMethodSignature());
                            throw;
                        }
                    }
                }
            }

            return SubmissionResult;
        }

        private bool CheckGiftNumbersAreConsecutive()
        {
            //TODO
            return true;
        }

        /// <summary>
        /// Returns a table of gifts with Ex-Worker recipients
        /// </summary>
        /// <param name="AGiftDetailsToCheck">GiftDetails to check for ExWorker recipients</param>
        /// <param name="ANotInBatchNumber">Used to exclude gift from a particular batch</param>
        /// <returns></returns>
        [RequireModulePermission("FINANCE-1")]
        public static DataTable FindGiftRecipientExWorker(DataTable AGiftDetailsToCheck, int ANotInBatchNumber = -1)
        {
            DataTable ReturnValue = AGiftDetailsToCheck.Clone();

            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("FindGiftRecipientExWorker");

            db.ReadTransaction(
                ref Transaction,
                delegate
                {
                    foreach (DataRow Row in AGiftDetailsToCheck.Rows)
                    {
                        // check changed data is either added or modified and that it is by a new donor
                        if ((Row.RowState != DataRowState.Deleted)
                            && (((Int32)Row[GiftBatchTDSAGiftDetailTable.GetBatchNumberDBName()]) != ANotInBatchNumber))
                        {
                            PPartnerTypeTable PartnerTypeTable =
                                PPartnerTypeAccess.LoadViaPPartner((Int64)Row[GiftBatchTDSAGiftDetailTable.GetRecipientKeyDBName()], Transaction);

                            foreach (PPartnerTypeRow TypeRow in PartnerTypeTable.Rows)
                            {
                                if (TypeRow.TypeCode.StartsWith(new TSystemDefaults(db).GetStringDefault(SharedConstants.SYSDEFAULT_EXWORKERSPECIALTYPE,
                                            "EX-WORKER")))
                                {
                                    ReturnValue.Rows.Add((object[])Row.ItemArray.Clone());
                                    break;
                                }
                            }
                        }
                    }
                });

            ReturnValue.AcceptChanges();
            return ReturnValue;
        }

        /// <summary>
        /// creates the GL batch needed for posting the gift batch
        /// </summary>
        private static GLBatchTDS CreateGLBatchAndTransactionsForPostingGifts(Int32 ALedgerNumber, ref GiftBatchTDS AGiftDataset, TDataBase ADataBase = null)
        {
            // create one GL batch without a journal
            GLBatchTDS GLDataset = TGLPosting.CreateABatch(ALedgerNumber, ADataBase, false);

            ABatchRow batch = GLDataset.ABatch[0];
            AGiftBatchRow giftBatch = AGiftDataset.AGiftBatch[0];

            bool TaxDeductiblePercentageEnabled =
                new TSystemDefaults(ADataBase).GetBooleanDefault(SharedConstants.SYSDEFAULT_TAXDEDUCTIBLEPERCENTAGE, false);

            batch.BatchDescription = Catalog.GetString("Gift Batch " + giftBatch.BatchNumber.ToString());
            batch.DateEffective = giftBatch.GlEffectiveDate;
            batch.BatchPeriod = giftBatch.BatchPeriod;
            batch.GiftBatchNumber = giftBatch.BatchNumber;
            batch.BatchStatus = MFinanceConstants.BATCH_UNPOSTED;

            // one gift batch only has one currency, create only one journal
            AJournalRow journal = GLDataset.AJournal.NewRowTyped();
            journal.LedgerNumber = batch.LedgerNumber;
            journal.BatchNumber = batch.BatchNumber;
            journal.JournalNumber = 1;
            journal.DateEffective = batch.DateEffective;
            journal.JournalPeriod = giftBatch.BatchPeriod;
            journal.TransactionCurrency = giftBatch.CurrencyCode;
            journal.ExchangeRateToBase = giftBatch.ExchangeRateToBase;
            journal.ExchangeRateTime = 7200; //represents 2 hours into the date, i.e. 2am
            journal.JournalDescription = batch.BatchDescription;
            journal.TransactionTypeCode = CommonAccountingTransactionTypesEnum.GR.ToString();
            journal.SubSystemCode = CommonAccountingSubSystemsEnum.GR.ToString();
            journal.LastTransactionNumber = 0;
            journal.DateOfEntry = DateTime.Now;

            GLDataset.AJournal.Rows.Add(journal);

            foreach (GiftBatchTDSAGiftDetailRow giftdetail in AGiftDataset.AGiftDetail.Rows)
            {
                if (!TaxDeductiblePercentageEnabled)
                {
                    AddGiftDetailToGLBatch(ref GLDataset, giftdetail.CostCentreCode, giftdetail.AccountCode,
                        giftdetail.GiftTransactionAmount, giftdetail.GiftAmount, giftdetail.GiftAmountIntl, journal, giftBatch);
                }
                else if (!giftdetail.IsTaxDeductiblePctNull())
                {
                    // if tax deductible pct is enabled then the gift detail needs split in two: tax-deductible and non-deductible
                    if (giftdetail.TaxDeductiblePct > 0)
                    {
                        // tax deductible
                        AddGiftDetailToGLBatch(ref GLDataset,
                            giftdetail.CostCentreCode,
                            giftdetail.TaxDeductibleAccountCode,
                            giftdetail.TaxDeductibleAmount,
                            giftdetail.TaxDeductibleAmountBase,
                            giftdetail.TaxDeductibleAmountIntl,
                            journal,
                            giftBatch);
                    }

                    if (giftdetail.TaxDeductiblePct < 100)
                    {
                        // non deductible
                        AddGiftDetailToGLBatch(ref GLDataset,
                            giftdetail.CostCentreCode,
                            giftdetail.AccountCode,
                            giftdetail.NonDeductibleAmount,
                            giftdetail.NonDeductibleAmountBase,
                            giftdetail.NonDeductibleAmountIntl,
                            journal,
                            giftBatch);
                    }
                }

                // TODO: for other currencies a post to a_ledger.a_forex_gains_losses_account_c ???

                // TODO: do the fee calculation, a_fees_payable, a_fees_receivable
            }

            ATransactionRow transactionForTotals = GLDataset.ATransaction.NewRowTyped();
            transactionForTotals.LedgerNumber = journal.LedgerNumber;
            transactionForTotals.BatchNumber = journal.BatchNumber;
            transactionForTotals.JournalNumber = journal.JournalNumber;
            transactionForTotals.TransactionNumber = ++journal.LastTransactionNumber;
            transactionForTotals.TransactionAmount = 0;
            transactionForTotals.AmountInBaseCurrency = 0;
            transactionForTotals.AmountInIntlCurrency = 0;
            transactionForTotals.TransactionDate = giftBatch.GlEffectiveDate;
            transactionForTotals.SystemGenerated = true;

            foreach (ATransactionRow transactionRow in GLDataset.ATransaction.Rows)
            {
                if (transactionRow.DebitCreditIndicator)
                {
                    transactionForTotals.TransactionAmount -= transactionRow.TransactionAmount;
                    transactionForTotals.AmountInBaseCurrency -= transactionRow.AmountInBaseCurrency;
                    transactionForTotals.AmountInIntlCurrency -= transactionRow.AmountInIntlCurrency;
                }
                else
                {
                    transactionForTotals.TransactionAmount += transactionRow.TransactionAmount;
                    transactionForTotals.AmountInBaseCurrency += transactionRow.AmountInBaseCurrency;
                    transactionForTotals.AmountInIntlCurrency += transactionRow.AmountInIntlCurrency;
                }
            }

            // determine whether transaction is debit or credit
            transactionForTotals.DebitCreditIndicator = transactionForTotals.TransactionAmount > 0;

            transactionForTotals.TransactionAmount = Math.Abs(transactionForTotals.TransactionAmount);
            transactionForTotals.AmountInBaseCurrency = Math.Abs(transactionForTotals.AmountInBaseCurrency);
            transactionForTotals.AmountInIntlCurrency = Math.Abs(transactionForTotals.AmountInIntlCurrency);

            // TODO: account and costcentre based on linked costcentre, current commitment, and Motivation detail
            // if motivation cost centre is a summary cost centre, make sure the transaction costcentre is reporting to that summary cost centre
            // Careful: modify gift cost centre and account and recipient field only when the amount is positive.
            // adjustments and reversals must remain on the original value

//
// Modified July 2016 Tim Ingham: whereas it is possible for the user to specify the "Bank Cost Centre",
// This was ignored by the use of the standard Cost Centre in the line below.
// (The change disregards the TODO: comment above, which may be incorrect - this is the "totals" or "Debit" leg of the batch,
// and the BankCostCentre is not affected by the GiftDetail motivation details.)
//
//          transactionForTotals.CostCentreCode = TLedgerInfo.GetStandardCostCentre(ALedgerNumber);

            transactionForTotals.AccountCode = giftBatch.BankAccountCode;
            transactionForTotals.CostCentreCode = giftBatch.BankCostCentre;
            transactionForTotals.Narrative = "Deposit from receipts - Gift Batch " + giftBatch.BatchNumber.ToString();
            transactionForTotals.Reference = "GB" + giftBatch.BatchNumber.ToString();

            // it is possible that the total transaction amount is 0 in which case we do not need this transaction
            if (transactionForTotals.TransactionAmount != 0)
            {
                GLDataset.ATransaction.Rows.Add(transactionForTotals);
            }

            GLDataset.ATransaction.DefaultView.RowFilter = string.Empty;

            foreach (ATransactionRow transaction in GLDataset.ATransaction.Rows)
            {
                if (transaction.DebitCreditIndicator)
                {
                    journal.JournalDebitTotal += transaction.TransactionAmount;
                    batch.BatchDebitTotal += transaction.TransactionAmount;
                }
                else
                {
                    journal.JournalCreditTotal += transaction.TransactionAmount;
                    batch.BatchCreditTotal += transaction.TransactionAmount;
                }
            }

            batch.LastJournal = 1;

            return GLDataset;
        }

        private static void AddGiftDetailToGLBatch(ref GLBatchTDS AGLDataset,
            string ACostCentre, string AAccountCode, decimal ATransactionAmount, decimal AAmountInBaseCurrency, decimal AAmountInIntlCurrency,
            AJournalRow AJournal, AGiftBatchRow AGiftBatch)
        {
            ATransactionRow transaction = null;

            // do we have already a transaction for this costcentre&account?
            AGLDataset.ATransaction.DefaultView.RowFilter = String.Format("{0}='{1}' and {2}='{3}'",
                ATransactionTable.GetAccountCodeDBName(),
                AAccountCode,
                ATransactionTable.GetCostCentreCodeDBName(),
                ACostCentre);

            if (AGLDataset.ATransaction.DefaultView.Count == 0)
            {
                transaction = AGLDataset.ATransaction.NewRowTyped();
                transaction.LedgerNumber = AJournal.LedgerNumber;
                transaction.BatchNumber = AJournal.BatchNumber;
                transaction.JournalNumber = AJournal.JournalNumber;
                transaction.TransactionNumber = ++AJournal.LastTransactionNumber;
                transaction.AccountCode = AAccountCode;
                transaction.CostCentreCode = ACostCentre;
                transaction.Narrative = "GB - Gift Batch " + AGiftBatch.BatchNumber.ToString();
                transaction.Reference = "GB" + AGiftBatch.BatchNumber.ToString();
                transaction.DebitCreditIndicator = ATransactionAmount < 0;
                transaction.TransactionAmount = 0;
                transaction.AmountInBaseCurrency = 0;
                transaction.AmountInIntlCurrency = 0;
                transaction.SystemGenerated = true;
                transaction.TransactionDate = AGiftBatch.GlEffectiveDate;

                AGLDataset.ATransaction.Rows.Add(transaction);
            }
            else
            {
                transaction = (ATransactionRow)AGLDataset.ATransaction.DefaultView[0].Row;
            }

            // if gift has same debit/credit indicator as transaction
            if (transaction.DebitCreditIndicator == ATransactionAmount < 0)
            {
                transaction.TransactionAmount += Math.Abs(ATransactionAmount);
                transaction.AmountInBaseCurrency += Math.Abs(AAmountInBaseCurrency);
                transaction.AmountInIntlCurrency += Math.Abs(AAmountInIntlCurrency);
            }
            // if gift has a different debit/credit indicator as transaction
            else
            {
                transaction.TransactionAmount -= Math.Abs(ATransactionAmount);
                transaction.AmountInBaseCurrency -= Math.Abs(AAmountInBaseCurrency);
                transaction.AmountInIntlCurrency -= Math.Abs(AAmountInIntlCurrency);

                // if transaction amount has went negative then the debit/credit indicator must change
                if (transaction.TransactionAmount < 0)
                {
                    transaction.TransactionAmount = Math.Abs(transaction.TransactionAmount);
                    transaction.AmountInBaseCurrency = Math.Abs(transaction.AmountInBaseCurrency);
                    transaction.AmountInIntlCurrency = Math.Abs(transaction.AmountInIntlCurrency);

                    transaction.DebitCreditIndicator = !transaction.DebitCreditIndicator;
                }
            }

            if (transaction.TransactionAmount == 0)
            {
                int transNumToDelete = transaction.TransactionNumber;

                transaction.Delete();

                //Renumber transactions above
                AGLDataset.ATransaction.DefaultView.RowFilter = String.Format("{0}>{1}",
                    ATransactionTable.GetTransactionNumberDBName(),
                    transNumToDelete);
                AGLDataset.ATransaction.DefaultView.Sort = ATransactionTable.GetTransactionNumberDBName() + " ASC";

                foreach (DataRowView drv in AGLDataset.ATransaction.DefaultView)
                {
                    ATransactionRow tR = (ATransactionRow)drv.Row;
                    tR.TransactionNumber--;
                }

                AJournal.LastTransactionNumber--;
            }
        }

        /// load the donor names and tax settings for a given batch
        [NoRemoting]
        public static void LoadGiftDonorRelatedData(GiftBatchTDS AGiftDS,
            bool ARecurring,
            Int32 ALedgerNumber,
            Int32 ABatchNumber,
            TDBTransaction ATransaction)
        {
            TSystemDefaults SystemDefaults = new TSystemDefaults(ATransaction.DataBaseObj);

            #region Validate Arguments

            if (AGiftDS == null)
            {
                throw new EFinanceSystemDataObjectNullOrEmptyException(String.Format(Catalog.GetString("Function:{0} - The Gift dataset is null!"),
                        Utilities.GetMethodName(true)));
            }
            else if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }
            else if (ABatchNumber <= 0)
            {
                throw new EFinanceSystemInvalidBatchNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Batch number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber, ABatchNumber);
            }
            else if (ATransaction == null)
            {
                throw new EFinanceSystemDBTransactionNullException(String.Format(Catalog.GetString(
                            "Function:{0} - Database Transaction must not be NULL!"),
                        Utilities.GetMethodName(true)));
            }

            #endregion Validate Arguments

            try
            {
                bool TaxDeductiblePercentageEnabled = false;

                if (!ARecurring)
                {
                    TaxDeductiblePercentageEnabled =
                        SystemDefaults.GetBooleanDefault(SharedConstants.SYSDEFAULT_TAXDEDUCTIBLEPERCENTAGE, false);
                }

                List <OdbcParameter>parameters = new List <OdbcParameter>();
                OdbcParameter param = new OdbcParameter("ledger", OdbcType.Int);
                param.Value = ALedgerNumber;
                parameters.Add(param);
                param = new OdbcParameter("batch", OdbcType.Int);
                param.Value = ABatchNumber;
                parameters.Add(param);

                // load all donor shortnames in one go
                string getDonorSQL =
                    "SELECT DISTINCT SUBSTRING(dp.p_partner_class_c, 1, 1) AS p_partner_class_c, dp.p_partner_key_n, dp.p_partner_short_name_c, dp.p_status_code_c,"
                    +
                    " dp.p_receipt_letter_frequency_c, dp.p_receipt_each_gift_l, dp.p_anonymous_donor_l" +
                    " FROM PUB_p_partner dp, PUB_a_gift g "
                    +                                                                                                                                                                                      //, dp.p_receipt_each_gift_l
                    "WHERE g.a_ledger_number_i = ? AND g.a_batch_number_i = ? AND g.p_donor_key_n = dp.p_partner_key_n";

                if (ARecurring)
                {
                    getDonorSQL = getDonorSQL.Replace("PUB_a_gift", "PUB_a_recurring_gift");
                }

                ATransaction.DataBaseObj.Select(AGiftDS, getDonorSQL, AGiftDS.DonorPartners.TableName,
                    ATransaction,
                    parameters.ToArray(), 0, 0);

                // load all recipient partners and fields related to this gift batch in one go
                string getRecipientSQL =
                    "SELECT DISTINCT rp.*";

                if (TaxDeductiblePercentageEnabled && !ARecurring)
                {
                    getRecipientSQL += ", p_partner_tax_deductible_pct.*";
                }

                getRecipientSQL += " FROM PUB_a_gift_detail gd, PUB_p_partner rp";

                if (TaxDeductiblePercentageEnabled && !ARecurring)
                {
                    getRecipientSQL += " LEFT JOIN p_partner_tax_deductible_pct" +
                                       " ON p_partner_tax_deductible_pct.p_partner_key_n = rp.p_partner_key_n";
                }

                getRecipientSQL += " WHERE gd.a_ledger_number_i = ? AND gd.a_batch_number_i = ? AND gd.p_recipient_key_n = rp.p_partner_key_n";

                if (ARecurring)
                {
                    getRecipientSQL = getRecipientSQL.Replace("PUB_a_gift", "PUB_a_recurring_gift");
                }

                ATransaction.DataBaseObj.Select(AGiftDS, getRecipientSQL, AGiftDS.RecipientPartners.TableName,
                    ATransaction,
                    parameters.ToArray(), 0, 0);

                string getRecipientFamilySQL =
                    "SELECT DISTINCT pf.* FROM PUB_p_family pf, PUB_a_gift_detail gd " +
                    "WHERE gd.a_ledger_number_i = ? AND gd.a_batch_number_i = ? AND gd.p_recipient_key_n = pf.p_partner_key_n";

                if (ARecurring)
                {
                    getRecipientFamilySQL = getRecipientFamilySQL.Replace("PUB_a_gift", "PUB_a_recurring_gift");
                }

                ATransaction.DataBaseObj.Select(AGiftDS, getRecipientFamilySQL, AGiftDS.RecipientFamily.TableName,
                    ATransaction,
                    parameters.ToArray(), 0, 0);

                string getRecipientPersonSQL =
                    "SELECT DISTINCT pf.* FROM PUB_p_person pf, PUB_a_gift_detail gd " +
                    "WHERE gd.a_ledger_number_i = ? AND gd.a_batch_number_i = ? AND gd.p_recipient_key_n = pf.p_partner_key_n";

                if (ARecurring)
                {
                    getRecipientPersonSQL = getRecipientPersonSQL.Replace("PUB_a_gift", "PUB_a_recurring_gift");
                }

                ATransaction.DataBaseObj.Select(AGiftDS, getRecipientPersonSQL, AGiftDS.RecipientPerson.TableName,
                    ATransaction,
                    parameters.ToArray(), 0, 0);

                string getRecipientUnitSQL =
                    "SELECT DISTINCT pf.* FROM PUB_p_unit pf, PUB_a_gift_detail gd " +
                    "WHERE gd.a_ledger_number_i = ? AND gd.a_batch_number_i = ? AND gd.p_recipient_key_n = pf.p_partner_key_n";

                if (ARecurring)
                {
                    getRecipientUnitSQL = getRecipientUnitSQL.Replace("PUB_a_gift", "PUB_a_recurring_gift");
                }

                ATransaction.DataBaseObj.Select(AGiftDS, getRecipientUnitSQL, AGiftDS.RecipientUnit.TableName,
                    ATransaction,
                    parameters.ToArray(), 0, 0);

                // In Austria, the donors may have Govt. Tax Ids:
                if (SystemDefaults.GetBooleanDefault(SharedConstants.SYSDEFAULT_GOVID_DB_KEY_NAME, false))
                {
                    String taxTypeFieldValue = new TSystemDefaults(ATransaction.DataBaseObj).GetStringDefault("GovIdDbKeyName", "bPK");

                    String query = "SELECT * FROM p_tax WHERE p_tax_type_c='" + taxTypeFieldValue + "' AND p_partner_key_n IN" +
                                   " (SELECT DISTINCT p_donor_key_n FROM a_gift WHERE" +
                                   " a_gift.a_ledger_number_i = " + ALedgerNumber +
                                   " AND a_gift.a_batch_number_i = " + ABatchNumber + ")";
                    ATransaction.DataBaseObj.Select(AGiftDS, query,
                        AGiftDS.PTax.TableName, ATransaction);
                }
            }
            catch (Exception ex)
            {
                TLogging.LogException(ex, Utilities.GetMethodSignature());
                throw;
            }
        }

        /// <summary>
        /// Check if an entry exists in ValidLedgerNumber for the specified ledger number and partner key
        /// </summary>
        /// <param name="ALedgerNumber"></param>
        /// <param name="APartnerKey"></param>
        /// <param name="ACostCentreCode"></param>
        /// <returns></returns>
        [RequireModulePermission("FINANCE-1")]
        public static bool CheckCostCentreLinkForRecipient(Int32 ALedgerNumber, Int64 APartnerKey, out string ACostCentreCode)
        {
            bool CostCentreExists = false;

            ACostCentreCode = string.Empty;
            string CostCentreCode = ACostCentreCode;

            string ValidLedgerNumberTable = "ValidLedgerNumber";

            string GetPartnerValidLedgerNumberSQL = String.Format("SELECT DISTINCT vln.a_cost_centre_code_c FROM PUB_a_valid_ledger_number vln " +
                "WHERE vln.a_ledger_number_i = {0} AND vln.p_partner_key_n = {1}",
                ALedgerNumber,
                APartnerKey);

            DataSet tempDataSet = new DataSet();

            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("CheckCostCentreLinkForRecipient");

            db.ReadTransaction(
                ref Transaction,
                delegate
                {
                    try
                    {
                        db.Select(tempDataSet, GetPartnerValidLedgerNumberSQL, ValidLedgerNumberTable,
                            Transaction,
                            0, 0);

                        if (tempDataSet.Tables[ValidLedgerNumberTable] != null)
                        {
                            if (tempDataSet.Tables[ValidLedgerNumberTable].Rows.Count > 0)
                            {
                                DataRow row = tempDataSet.Tables[ValidLedgerNumberTable].Rows[0];
                                CostCentreCode = row[0].ToString();
                                CostCentreExists = true;
                            }

                            tempDataSet.Clear();
                        }
                    }
                    catch (Exception ex)
                    {
                        TLogging.LogException(ex, Utilities.GetMethodSignature());
                        throw;
                    }
                });

            ACostCentreCode = CostCentreCode;

            db.CloseDBConnection();

            return CostCentreExists;
        }

        /// <summary>
        /// Get gift destination for recipient
        /// </summary>
        /// <param name="APartnerKey"></param>
        /// <param name="AGiftDate"></param>
        /// <returns></returns>
        [RequireModulePermission("FINANCE-1")]
        public static Int64 GetRecipientGiftDestination(Int64 APartnerKey, DateTime ? AGiftDate)
        {
            return TPartnerServerLookups.GetPartnerGiftDestination(APartnerKey, AGiftDate);
        }

        /// <summary>
        /// Check if an entry exists in ValidLedgerNumber for the specified ledger number and partner key
        /// </summary>
        /// <param name="ALedgerNumber"></param>
        /// <param name="APartnerKey"></param>
        /// <param name="ACostCentreCode"></param>
        /// <param name="ADataBase"></param>
        /// <returns></returns>
        [RequireModulePermission("FINANCE-1")]
        public static bool CheckCostCentreDestinationForRecipient(Int32 ALedgerNumber,
            Int64 APartnerKey,
            out string ACostCentreCode,
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

            bool CostCentreExists = false;

            ACostCentreCode = string.Empty;
            string CostCentreCode = ACostCentreCode;

            try
            {
                TDBTransaction Transaction = new TDBTransaction();
                TDataBase db = DBAccess.Connect("CheckCostCentreDestinationForRecipient", ADataBase);
                db.ReadTransaction(
                    ref Transaction,
                    delegate
                    {
                        DataTable costCentreCodesTbl = null;

                        string costCentreCodeTableName = "CostCentreCodes";
                        string getCostCentreCodeSQL = String.Format(
                            "SELECT a_cost_centre_code_c FROM public.a_valid_ledger_number WHERE a_ledger_number_i = {0} AND p_partner_key_n = {1};",
                            ALedgerNumber,
                            APartnerKey
                            );

                        costCentreCodesTbl = db.SelectDT(getCostCentreCodeSQL, costCentreCodeTableName, Transaction);

                        if ((costCentreCodesTbl != null) && (costCentreCodesTbl.Rows.Count > 0))
                        {
                            CostCentreCode = (string)costCentreCodesTbl.DefaultView[0].Row[AValidLedgerNumberTable.GetCostCentreCodeDBName()];
                            CostCentreExists = true;
                        }
                    });

                if (ADataBase == null)
                {
                    db.CloseDBConnection();
                }
            }
            catch (Exception ex)
            {
                TLogging.LogException(ex, Utilities.GetMethodSignature());
                throw;
            }

            ACostCentreCode = CostCentreCode;

            return CostCentreExists;
        }

        /// <summary>
        /// Create GiftBatchTDS with the gift batch to post, and all gift transactions and details, and motivation details
        /// </summary>
        /// <param name="ALedgerNumber"></param>
        /// <param name="ABatchNumber"></param>
        /// <param name="AExcludeBatchRow"></param>
        /// <returns></returns>
        [RequireModulePermission("FINANCE-1")]
        public static GiftBatchTDS LoadAGiftBatchAndRelatedData(Int32 ALedgerNumber,
            Int32 ABatchNumber,
            bool AExcludeBatchRow = false)
        {
            #region Validate Arguments

            if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }
            else if (ABatchNumber <= 0)
            {
                throw new EFinanceSystemInvalidBatchNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Batch number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber, ABatchNumber);
            }

            #endregion Validate Arguments

            bool ChangesToCommit = false;

            GiftBatchTDS MainDS = new GiftBatchTDS();
            TDBTransaction Transaction = new TDBTransaction();
            TDataBase DBConnection = DBAccess.Connect("ReadGiftTds");
            bool ASubmissionOK = false;

            try
            {
                DBConnection.WriteTransaction(
                    ref Transaction,
                    ref ASubmissionOK,
                    delegate
                    {
                        MainDS = LoadAGiftBatchAndRelatedData(ALedgerNumber, ABatchNumber, Transaction, out ChangesToCommit, AExcludeBatchRow);

                        if (ChangesToCommit)
                        {
                            GiftBatchTDSAccess.SubmitChanges(MainDS, DBConnection);
                            ASubmissionOK = true;
                        }

                    });

                MainDS.AcceptChanges();
            }
            catch (Exception ex)
            {
                TLogging.LogException(ex, Utilities.GetMethodSignature());
                throw;
            }
            finally
            {
                DBConnection.CloseDBConnection();
            }

            return MainDS;
        }

        /// <summary>
        /// Create GiftBatchTDS with the gift batch to post, and all gift transactions and details, and motivation details.
        /// Public for tests
        /// </summary>
        //[RequireModulePermission("FINANCE-1")]
        [NoRemoting]
        public static GiftBatchTDS LoadAGiftBatchAndRelatedData(Int32 ALedgerNumber,
            Int32 ABatchNumber,
            TDBTransaction ATransaction,
            out bool AChangesToCommit,
            bool AExcludeBatchRow = false)
        {
            #region Validate Arguments

            if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }
            else if (ABatchNumber <= 0)
            {
                throw new EFinanceSystemInvalidBatchNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Batch number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber, ABatchNumber);
            }
            else if (ATransaction == null)
            {
                throw new EFinanceSystemDBTransactionNullException(String.Format(Catalog.GetString(
                            "Function:{0} - Database Transaction must not be NULL!"),
                        Utilities.GetMethodName(true)));
            }

            #endregion Validate Arguments

            AChangesToCommit = false;

            GiftBatchTDS MainDS = new GiftBatchTDS();

            ALedgerAccess.LoadByPrimaryKey(MainDS, ALedgerNumber, ATransaction);
            AMotivationDetailAccess.LoadViaALedger(MainDS, ALedgerNumber, ATransaction);
            AGiftBatchAccess.LoadByPrimaryKey(MainDS, ALedgerNumber, ABatchNumber, ATransaction);
            AGiftAccess.LoadViaAGiftBatch(MainDS, ALedgerNumber, ABatchNumber, ATransaction);
            AGiftDetailAccess.LoadViaAGiftBatch(MainDS, ALedgerNumber, ABatchNumber, ATransaction);

            //Load Ledger Partner types
            MainDS.LedgerPartnerTypes.Merge(PPartnerTypeAccess.LoadViaPType(MPartnerConstants.PARTNERTYPE_LEDGER, ATransaction));

            #region Validate Data 1

            //Only the following tables should not be empty when posting.
            if ((MainDS.ALedger == null) || (MainDS.ALedger.Count == 0))
            {
                throw new EFinanceSystemDataTableReturnedNoDataException(String.Format(Catalog.GetString(
                            "Function:{0} - Ledger data for Ledger number {1} does not exist or could not be accessed!"),
                        Utilities.GetMethodName(true),
                        ALedgerNumber));
            }
            else if ((MainDS.AMotivationDetail == null) || (MainDS.AMotivationDetail.Count == 0))
            {
                throw new EFinanceSystemDataTableReturnedNoDataException(String.Format(Catalog.GetString(
                            "Function:{0} - Motivation Detail data for Ledger number {1} does not exist or could not be accessed!"),
                        Utilities.GetMethodName(true),
                        ALedgerNumber));
            }
            else if ((MainDS.LedgerPartnerTypes == null) || (MainDS.LedgerPartnerTypes.Count == 0))
            {
                throw new EFinanceSystemDataTableReturnedNoDataException(String.Format(Catalog.GetString(
                            "Function:{0} - Ledger Partner Type data does not exist or could not be accessed!"),
                        Utilities.GetMethodName(true)));
            }

            //Below is not needed as a new batch is on the client but not on the server!
            //else if ((MainDS.AGiftBatch == null) || (MainDS.ARecurringGiftBatch.Count == 0))
            //{
            //    throw new EFinanceSystemDataTableReturnedNoDataException(String.Format(Catalog.GetString(
            //                "Function:{0} - Gift Batch data for Ledger number {1} Batch {2} does not exist or could not be accessed!"),
            //            Utilities.GetMethodName(true),
            //            ALedgerNumber,
            //            ABatchNumber));
            //}

            #endregion Validate Data 1

            //Load related donor data
            LoadGiftDonorRelatedData(MainDS, false, ALedgerNumber, ABatchNumber, ATransaction);

            DataView giftView = new DataView(MainDS.AGift);
            giftView.Sort = AGiftTable.GetGiftTransactionNumberDBName();

            bool IsUnposted = (MainDS.AGiftBatch[0].BatchStatus == MFinanceConstants.BATCH_UNPOSTED);

            // get the donor name and the gift total
            foreach (GiftBatchTDSAGiftRow giftRow in MainDS.AGift.Rows)
            {
                PPartnerRow donorRow = (PPartnerRow)MainDS.DonorPartners.Rows.Find(giftRow.DonorKey);
                giftRow.DonorName = donorRow.PartnerShortName;
                giftRow.GiftTotal = 0;

                foreach (GiftBatchTDSAGiftDetailRow giftDetail in MainDS.AGiftDetail.Rows)
                {
                    if (giftDetail.GiftTransactionNumber == giftRow.GiftTransactionNumber)
                    {
                        giftRow.GiftTotal += giftDetail.GiftTransactionAmount;
                    }
                }
            }

            // fill the columns in the modified GiftDetail Table to show donorkey, dateentered etc in the grid
            foreach (GiftBatchTDSAGiftDetailRow giftDetail in MainDS.AGiftDetail.Rows)
            {
                // get the gift
                GiftBatchTDSAGiftRow giftRow = (GiftBatchTDSAGiftRow)giftView.FindRows(giftDetail.GiftTransactionNumber)[0].Row;

                #region Validate Data 2

                if (giftRow == null)
                {
                    throw new EFinanceSystemDataTableReturnedNoDataException(String.Format(Catalog.GetString(
                                "Function:{0} - Gift Row {1} for Ledger number {1}, Batch {2} does not exist or could not be accessed!"),
                            Utilities.GetMethodName(true),
                            giftDetail.GiftTransactionNumber,
                            ALedgerNumber,
                            ABatchNumber));
                }

                #endregion Validate Data 2

                PPartnerRow donorRow = (PPartnerRow)MainDS.DonorPartners.Rows.Find(giftRow.DonorKey);

                #region Validate Data 3

                if (donorRow == null)
                {
                    throw new EFinanceSystemDataTableReturnedNoDataException(String.Format(Catalog.GetString(
                                "Function:{0} - Partner data for Donor {1} does not exist or could not be accessed!"),
                            Utilities.GetMethodName(true),
                            giftRow.DonorKey));
                }

                #endregion Validate Data 3

                giftDetail.DonorKey = giftRow.DonorKey;
                giftDetail.DonorName = donorRow.PartnerShortName;
                giftDetail.DonorClass = donorRow.PartnerClass;
                giftDetail.MethodOfGivingCode = giftRow.MethodOfGivingCode;
                giftDetail.MethodOfPaymentCode = giftRow.MethodOfPaymentCode;
                giftDetail.ReceiptNumber = giftRow.ReceiptNumber;
                giftDetail.ReceiptPrinted = giftRow.ReceiptPrinted;
                giftDetail.DateEntered = giftRow.DateEntered;
                giftDetail.Reference = giftRow.Reference;

                AMotivationDetailRow motivationDetailRow = (AMotivationDetailRow)MainDS.AMotivationDetail.Rows.Find(
                    new object[] { ALedgerNumber, giftDetail.MotivationGroupCode, giftDetail.MotivationDetailCode });

                //do the same for the Recipient
                if (giftDetail.RecipientKey > 0)
                {
                    // if true then this gift is protected and data cannot be changed
                    // (note: here this includes all negative gifts and not just reversals)
                    if (IsUnposted && (giftDetail.GiftTransactionAmount > 0))
                    {
                        // get the current Recipient Fund Number
                        giftDetail.RecipientField = GetRecipientFundNumberInner(MainDS, giftDetail.RecipientKey, giftDetail.DateEntered, ATransaction.DataBaseObj);

                        // these will be different if the recipient fund number has changed (i.e. a changed Gift Destination)
                        if (!giftDetail.FixedGiftDestination && (giftDetail.RecipientLedgerNumber != giftDetail.RecipientField))
                        {
                            giftDetail.RecipientLedgerNumber = giftDetail.RecipientField;
                            AChangesToCommit = true;
                        }

                        #region Validate Data 4

                        if (motivationDetailRow == null)
                        {
                            throw new EFinanceSystemDataTableReturnedNoDataException(String.Format(Catalog.GetString(
                                        "Function:{0} - Motivation Detail data for Ledger number {1}, Motivation Group {2} and Detail {3} does not exist or could not be accessed!"),
                                    Utilities.GetMethodName(true),
                                    ALedgerNumber,
                                    giftDetail.MotivationGroupCode,
                                    giftDetail.MotivationDetailCode));
                        }

                        #endregion Validate Data 4

                        bool partnerIsMissingLink = false;

                        string newCostCentreCode =
                            RetrieveCostCentreCodeForRecipient(ALedgerNumber,
                                giftDetail.RecipientKey,
                                giftDetail.RecipientLedgerNumber,
                                giftDetail.DateEntered,
                                motivationDetailRow.MotivationGroupCode,
                                motivationDetailRow.MotivationDetailCode,
                                out partnerIsMissingLink,
                                ATransaction);

                        if (giftDetail.CostCentreCode != newCostCentreCode)
                        {
                            giftDetail.CostCentreCode = newCostCentreCode;
                            AChangesToCommit = true;
                        }
                    }
                    else
                    {
                        giftDetail.RecipientField = giftDetail.RecipientLedgerNumber;
                    }

                    PPartnerRow RecipientRow = (PPartnerRow)MainDS.RecipientPartners.Rows.Find(giftDetail.RecipientKey);

                    if (RecipientRow != null)
                    {
                        giftDetail.RecipientDescription = RecipientRow.PartnerShortName;
                        giftDetail.RecipientClass = RecipientRow.PartnerClass;
                    }
                    else
                    {
                        giftDetail.RecipientDescription = "INVALID";
                        giftDetail.RecipientClass = string.Empty;
                    }

                    PUnitRow RecipientUnitRow = (PUnitRow)MainDS.RecipientUnit.Rows.Find(giftDetail.RecipientKey);

                    if ((RecipientUnitRow != null) && (RecipientUnitRow.UnitTypeCode == MPartnerConstants.UNIT_TYPE_KEYMIN))
                    {
                        giftDetail.RecipientKeyMinistry = RecipientUnitRow.UnitName;
                    }
                    else
                    {
                        giftDetail.SetRecipientKeyMinistryNull();
                    }
                }
                else
                {
                    if (motivationDetailRow.CostCentreCode != giftDetail.CostCentreCode)
                    {
                        giftDetail.CostCentreCode = motivationDetailRow.CostCentreCode;
                        AChangesToCommit = true;
                    }

                    giftDetail.RecipientDescription = "INVALID";
                    giftDetail.SetRecipientFieldNull();
                    giftDetail.SetRecipientKeyMinistryNull();
                }

                string newAccountCode = null;
                string newTaxDeductibleAccountCode = null;

                // get up-to-date account code
                if (motivationDetailRow != null)
                {
                    newAccountCode = motivationDetailRow.AccountCode;
                    newTaxDeductibleAccountCode = motivationDetailRow.TaxDeductibleAccountCode;
                }

                // update account codes if they need updated
                if (giftDetail.AccountCode != newAccountCode)
                {
                    giftDetail.AccountCode = newAccountCode;
                    AChangesToCommit = true;
                }

                if (giftDetail.TaxDeductibleAccountCode != newTaxDeductibleAccountCode)
                {
                    giftDetail.TaxDeductibleAccountCode = newTaxDeductibleAccountCode;
                    AChangesToCommit = true;
                }
            }

            if (AExcludeBatchRow)
            {
                MainDS.AGiftBatch.Clear();
            }

            //Do not acceptchanges(), as the modified rowstate is needed for submit in the calling method.
            return MainDS;
        }

        /// <summary>
        /// Create GiftBatchTDS with the gift batch to post, and all gift transactions and details, and motivation details
        /// </summary>
        /// <param name="ALedgerNumber"></param>
        /// <param name="ABatchNumber"></param>
        /// <param name="AExcludeBatchRow"></param>
        /// <returns></returns>
        [RequireModulePermission("FINANCE-1")]
        public static GiftBatchTDS LoadARecurringGiftBatchAndRelatedData(Int32 ALedgerNumber, Int32 ABatchNumber, bool AExcludeBatchRow = false)
        {
            #region Validate Arguments

            if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }
            else if (ABatchNumber <= 0)
            {
                throw new EFinanceSystemInvalidBatchNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Batch number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber, ABatchNumber);
            }

            #endregion Validate Arguments

            bool ChangesToCommit = false;
            GiftBatchTDS MainDS = new GiftBatchTDS();

            TDBTransaction Transaction = new TDBTransaction();
            TDataBase DBConnection = DBAccess.Connect("ReadRecurringGifts");

            try
            {
                bool SubmitOK = false;
                DBConnection.WriteTransaction(
                    ref Transaction,
                    ref SubmitOK,
                    delegate
                    {
                        MainDS =
                            LoadARecurringGiftBatchAndRelatedData(ALedgerNumber, ABatchNumber, Transaction, out ChangesToCommit, AExcludeBatchRow);

                        if (ChangesToCommit)
                        {
                            // if RecipientLedgerNumber has been updated then this should immediately be saved to the database
                            GiftBatchTDSAccess.SubmitChanges(MainDS, DBConnection);
                            SubmitOK = true;
                        }
                    });

                MainDS.AcceptChanges();
            }
            catch (Exception ex)
            {
                TLogging.LogException(ex, Utilities.GetMethodSignature());
                throw;
            }
            finally
            {
                DBConnection.CloseDBConnection();
            }
            return MainDS;
        }

        /// <summary>
        /// create GiftBatchTDS with the recurring gift batch, and all gift transactions and details, and motivation details
        /// </summary>
        [RequireModulePermission("FINANCE-1")]
        private static GiftBatchTDS LoadARecurringGiftBatchAndRelatedData(Int32 ALedgerNumber,
            Int32 ABatchNumber,
            TDBTransaction ATransaction,
            out bool AChangesToCommit,
            bool AExcludeBatchRow = false)
        {
            #region Validate Arguments

            if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }
            else if (ABatchNumber <= 0)
            {
                throw new EFinanceSystemInvalidBatchNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Batch number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber, ABatchNumber);
            }
            else if (ATransaction == null)
            {
                throw new EFinanceSystemDBTransactionNullException(String.Format(Catalog.GetString(
                            "Function:{0} - Database Transaction must not be NULL!"),
                        Utilities.GetMethodName(true)));
            }

            #endregion Validate Arguments

            AChangesToCommit = false;

            GiftBatchTDS MainDS = new GiftBatchTDS();

            ALedgerAccess.LoadByPrimaryKey(MainDS, ALedgerNumber, ATransaction);
            ARecurringGiftBatchAccess.LoadByPrimaryKey(MainDS, ALedgerNumber, ABatchNumber, ATransaction);
            ARecurringGiftAccess.LoadViaARecurringGiftBatch(MainDS, ALedgerNumber, ABatchNumber, ATransaction);
            ARecurringGiftDetailAccess.LoadViaARecurringGiftBatch(MainDS, ALedgerNumber, ABatchNumber, ATransaction);
            AMotivationDetailAccess.LoadViaALedger(MainDS, ALedgerNumber, ATransaction);

            //Load Ledger Partner types
            MainDS.LedgerPartnerTypes.Merge(PPartnerTypeAccess.LoadViaPType(MPartnerConstants.PARTNERTYPE_LEDGER, ATransaction));

            #region Validate Data 1

            //Only the following tables should not be empty when posting.
            if ((MainDS.ALedger == null) || (MainDS.ALedger.Count == 0))
            {
                throw new EFinanceSystemDataTableReturnedNoDataException(String.Format(Catalog.GetString(
                            "Function:{0} - Ledger data for Ledger number {1} does not exist or could not be accessed!"),
                        Utilities.GetMethodSignature(),
                        ALedgerNumber));
            }
            else if ((MainDS.AMotivationDetail == null) || (MainDS.AMotivationDetail.Count == 0))
            {
                throw new EFinanceSystemDataTableReturnedNoDataException(String.Format(Catalog.GetString(
                            "Function:{0} - Motivation Detail data for Ledger number {1} does not exist or could not be accessed!"),
                        Utilities.GetMethodSignature(),
                        ALedgerNumber));
            }
            else if ((MainDS.LedgerPartnerTypes == null) || (MainDS.LedgerPartnerTypes.Count == 0))
            {
                throw new EFinanceSystemDataTableReturnedNoDataException(String.Format(Catalog.GetString(
                            "Function:{0} - Ledger Partner Type data does not exist or could not be accessed!"),
                        Utilities.GetMethodName(true)));
            }

            //Not needed as recurring batch number passed in may only be on the client side
            //else if ((MainDS.ARecurringGiftBatch == null) || (MainDS.ARecurringGiftBatch.Count == 0))
            //{
            //    throw new EFinanceSystemDataTableReturnedNoDataException(String.Format(Catalog.GetString(
            //                "Function:{0} - Recurring Gift Batch data for Ledger number {1} Batch {2} does not exist or could not be accessed!"),
            //            Utilities.GetMethodSignature(),
            //            ALedgerNumber,
            //            ABatchNumber));
            //}

            #endregion Validate Data 1

            //Load related donor data
            LoadGiftDonorRelatedData(MainDS, true, ALedgerNumber, ABatchNumber, ATransaction);

            // get the donor name and the gift total
            foreach (GiftBatchTDSARecurringGiftRow giftRow in MainDS.ARecurringGift.Rows)
            {
                PPartnerRow donorRow = (PPartnerRow)MainDS.DonorPartners.Rows.Find(giftRow.DonorKey);
                giftRow.DonorName = donorRow.PartnerShortName;
                giftRow.GiftTotal = 0;

                foreach (GiftBatchTDSARecurringGiftDetailRow giftDetail in MainDS.ARecurringGiftDetail.Rows)
                {
                    if (giftDetail.GiftTransactionNumber == giftRow.GiftTransactionNumber)
                    {
                        giftRow.GiftTotal += giftDetail.GiftAmount;
                    }
                }
            }

            DataView giftView = new DataView(MainDS.ARecurringGift);
            giftView.Sort = ARecurringGiftTable.GetGiftTransactionNumberDBName();

            // fill the columns in the modified GiftDetail Table to show donorkey, dateentered etc in the grid
            foreach (GiftBatchTDSARecurringGiftDetailRow giftDetail in MainDS.ARecurringGiftDetail.Rows)
            {
                // get the gift
                ARecurringGiftRow giftRow = (ARecurringGiftRow)giftView.FindRows(giftDetail.GiftTransactionNumber)[0].Row;

                #region Validate Data 2

                if (giftRow == null)
                {
                    throw new EFinanceSystemDataTableReturnedNoDataException(String.Format(Catalog.GetString(
                                "Function:{0} - Gift Row {1} for Ledger number {1}, Recurring Batch {2} does not exist or could not be accessed!"),
                            Utilities.GetMethodName(true),
                            giftDetail.GiftTransactionNumber,
                            ALedgerNumber,
                            ABatchNumber));
                }

                #endregion Validate Data 2

                PPartnerRow donorRow = (PPartnerRow)MainDS.DonorPartners.Rows.Find(giftRow.DonorKey);

                #region Validate Data 3

                if (donorRow == null)
                {
                    throw new EFinanceSystemDataTableReturnedNoDataException(String.Format(Catalog.GetString(
                                "Function:{0} - Partner data for Donor {1} does not exist or could not be accessed!"),
                            Utilities.GetMethodName(true),
                            giftRow.DonorKey));
                }

                #endregion Validate Data 3

                giftDetail.DonorKey = giftRow.DonorKey;
                giftDetail.DonorName = donorRow.PartnerShortName;
                giftDetail.DonorClass = donorRow.PartnerClass;
                giftDetail.MethodOfGivingCode = giftRow.MethodOfGivingCode;
                giftDetail.MethodOfPaymentCode = giftRow.MethodOfPaymentCode;
                giftDetail.Active = giftRow.Active;
                giftDetail.DateEntered = DateTime.Now;

                AMotivationDetailRow motivationDetailRow = (AMotivationDetailRow)MainDS.AMotivationDetail.Rows.Find(
                    new object[] { ALedgerNumber, giftDetail.MotivationGroupCode, giftDetail.MotivationDetailCode });

                //do the same for the Recipient
                if (giftDetail.RecipientKey > 0)
                {
                    // GiftAmount should never be negative. Negative Recurring gifts are not allowed!
                    if (giftDetail.GiftAmount > 0)
                    {
                        // get the current Recipient Fund Number
                        giftDetail.RecipientField = GetRecipientFundNumberInner(MainDS, giftDetail.RecipientKey, DateTime.Today, ATransaction.DataBaseObj);

                        // these will be different if the recipient fund number has changed (i.e. a changed Gift Destination)
                        if (giftDetail.RecipientLedgerNumber != giftDetail.RecipientField)
                        {
                            giftDetail.RecipientLedgerNumber = giftDetail.RecipientField;
                            AChangesToCommit = true;
                        }

                        #region Validate Data 4

                        if (motivationDetailRow == null)
                        {
                            throw new EFinanceSystemDataTableReturnedNoDataException(String.Format(Catalog.GetString(
                                        "Function:{0} - Motivation Detail data for Ledger number {1}, Motivation Group {2} and Detail {3} does not exist or could not be accessed!"),
                                    Utilities.GetMethodSignature(),
                                    ALedgerNumber,
                                    giftDetail.MotivationGroupCode,
                                    giftDetail.MotivationDetailCode));
                        }

                        #endregion Validate Data 4

                        bool partnerIsMissingLink = false;

                        string newCostCentreCode =
                            RetrieveCostCentreCodeForRecipient(ALedgerNumber,
                                giftDetail.RecipientKey,
                                giftDetail.RecipientLedgerNumber,
                                DateTime.Now,
                                motivationDetailRow.MotivationGroupCode,
                                motivationDetailRow.MotivationDetailCode,
                                out partnerIsMissingLink,
                                ATransaction);

                        if (giftDetail.CostCentreCode != newCostCentreCode)
                        {
                            giftDetail.CostCentreCode = newCostCentreCode;
                            AChangesToCommit = true;
                        }
                    }
                    else
                    {
                        giftDetail.RecipientField = giftDetail.RecipientLedgerNumber;
                    }

                    PPartnerRow RecipientRow = (PPartnerRow)MainDS.RecipientPartners.Rows.Find(giftDetail.RecipientKey);

                    if (RecipientRow != null)
                    {
                        giftDetail.RecipientDescription = RecipientRow.PartnerShortName;
                        giftDetail.RecipientClass = RecipientRow.PartnerClass;
                    }
                    else
                    {
                        giftDetail.RecipientDescription = "INVALID";
                        giftDetail.RecipientClass = string.Empty;
                    }

                    PUnitRow RecipientUnitRow = (PUnitRow)MainDS.RecipientUnit.Rows.Find(giftDetail.RecipientKey);

                    if ((RecipientUnitRow != null) && (RecipientUnitRow.UnitTypeCode == MPartnerConstants.UNIT_TYPE_KEYMIN))
                    {
                        giftDetail.RecipientKeyMinistry = RecipientUnitRow.UnitName;
                    }
                    else
                    {
                        giftDetail.SetRecipientKeyMinistryNull();
                    }
                }
                else
                {
                    if (motivationDetailRow.CostCentreCode != giftDetail.CostCentreCode)
                    {
                        giftDetail.CostCentreCode = motivationDetailRow.CostCentreCode;
                        AChangesToCommit = true;
                    }

                    giftDetail.RecipientDescription = "INVALID";
                    giftDetail.SetRecipientFieldNull();
                    giftDetail.SetRecipientKeyMinistryNull();
                }

                string newAccountCode = null;

                // get up-to-date account code
                if (motivationDetailRow != null)
                {
                    newAccountCode = motivationDetailRow.AccountCode;
                }

                // update account codes if they need updated
                if (giftDetail.AccountCode != newAccountCode)
                {
                    giftDetail.AccountCode = newAccountCode;
                    AChangesToCommit = true;
                }
            }

            if (AExcludeBatchRow)
            {
                MainDS.ARecurringGiftBatch.Clear();
            }

            //Don't call AcceptChanges() as modified row status is needed in calling method.
            return MainDS;
        }

        /// <summary>
        /// calculate the admin fee for a given amount.
        /// public so that it can be tested by NUnit tests.
        /// </summary>
        /// <param name="AMainDS"></param>
        /// <param name="ALedgerNumber"></param>
        /// <param name="AFeeCode"></param>
        /// <param name="AGiftAmount"></param>
        /// <param name="AVerificationResult"></param>
        /// <returns></returns>
        [RequireModulePermission("FINANCE-2")]
        public static decimal CalculateAdminFee(GiftBatchTDS AMainDS,
            Int32 ALedgerNumber,
            string AFeeCode,
            decimal AGiftAmount,
            out TVerificationResultCollection AVerificationResult
            )
        {
            #region Validate Arguments

            if (AMainDS == null)
            {
                throw new EFinanceSystemDataObjectNullOrEmptyException(String.Format(Catalog.GetString(
                            "Function:{0} - The Gift Batch dataset is null!"),
                        Utilities.GetMethodName(true)));
            }
            else if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }
            else if (AFeeCode.Length == 0)
            {
                throw new ArgumentException(String.Format(Catalog.GetString("Function:{0} - The Fee code is empty!"),
                        Utilities.GetMethodName(true)));
            }

            #endregion Validate Arguments

            //Amount to return
            decimal FeeAmount = 0;

            decimal GiftPercentageAmount;
            decimal ChargeAmount;
            string ChargeOption;

            AVerificationResult = new TVerificationResultCollection();

            try
            {
                AFeesPayableRow feePayableRow = (AFeesPayableRow)AMainDS.AFeesPayable.Rows.Find(new object[] { ALedgerNumber, AFeeCode });

                if (feePayableRow == null)
                {
                    // this row will only exist if the gift's cc is foreign
                    AFeesReceivableRow feeReceivableRow = (AFeesReceivableRow)AMainDS.AFeesReceivable.Rows.Find(new object[] { ALedgerNumber,
                                                                                                                               AFeeCode });

                    #region Validate Data 1

                    if (feeReceivableRow == null)
                    {
                        // i.e. this fee code is for a receivable fee but the gift's cc is local
                        return FeeAmount;
                    }

                    #endregion Validate Data 1

                    GiftPercentageAmount = GLRoutines.Divide(feeReceivableRow.ChargePercentage * AGiftAmount, 100);
                    ChargeOption = feeReceivableRow.ChargeOption.ToUpper();
                    ChargeAmount = feeReceivableRow.ChargeAmount;
                }
                else
                {
                    GiftPercentageAmount = GLRoutines.Divide(feePayableRow.ChargePercentage * AGiftAmount, 100);
                    ChargeOption = feePayableRow.ChargeOption.ToUpper();
                    ChargeAmount = feePayableRow.ChargeAmount;
                }

                switch (ChargeOption)
                {
                    case MFinanceConstants.ADMIN_CHARGE_OPTION_FIXED :

                        if (AGiftAmount >= 0)
                        {
                            FeeAmount = ChargeAmount;
                        }
                        else
                        {
                            FeeAmount = -ChargeAmount;
                        }

                        break;

                    case MFinanceConstants.ADMIN_CHARGE_OPTION_MIN:

                        if (AGiftAmount >= 0)
                        {
                            if (ChargeAmount >= GiftPercentageAmount)
                            {
                                FeeAmount = ChargeAmount;
                            }
                            else
                            {
                                FeeAmount = GiftPercentageAmount;
                            }
                        }
                        else
                        {
                            if (-ChargeAmount <= GiftPercentageAmount)
                            {
                                FeeAmount = -ChargeAmount;
                            }
                            else
                            {
                                FeeAmount = GiftPercentageAmount;
                            }
                        }

                        break;

                    case MFinanceConstants.ADMIN_CHARGE_OPTION_MAX:

                        if (AGiftAmount >= 0)
                        {
                            if (ChargeAmount <= GiftPercentageAmount)
                            {
                                FeeAmount = ChargeAmount;
                            }
                            else
                            {
                                FeeAmount = GiftPercentageAmount;
                            }
                        }
                        else
                        {
                            if (-ChargeAmount >= GiftPercentageAmount)
                            {
                                FeeAmount = -ChargeAmount;
                            }
                            else
                            {
                                FeeAmount = GiftPercentageAmount;
                            }
                        }

                        break;

                    case MFinanceConstants.ADMIN_CHARGE_OPTION_PERCENT:
                        FeeAmount = GiftPercentageAmount;
                        break;

                    default:
                        throw new Exception(String.Format(Catalog.GetString(
                                "Function:{0} - Unexpected Fee Payable/Receivable Charge Option: '{1}' in Ledger: {2} and Fee Code: '{3}'!"),
                            Utilities.GetMethodName(true),
                            ChargeOption,
                            ALedgerNumber,
                            AFeeCode));
                }
            }
            catch (Exception ex)
            {
                TLogging.LogException(ex, Utilities.GetMethodSignature());
                throw;
            }

            // calculate the admin fee for the specific amount and admin fee. see gl4391.p

            return FeeAmount;
        }

        /// <summary>
        /// Public for tests
        /// </summary>
        [NoRemoting]
        public static void AddToFeeTotals(Int32 ALedgerNumber,
            GiftBatchTDS AMainDS,
            AGiftDetailRow AGiftDetailRow,
            string AFeeCode,
            decimal AFeeAmount,
            int APostingPeriod)
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
                throw new EFinanceSystemDataObjectNullOrEmptyException(String.Format(Catalog.GetString(
                            "Function:{0} - The Gift Batch dataset is null!"),
                        Utilities.GetMethodName(true)));
            }

            if (AGiftDetailRow == null)
            {
                throw new EFinanceSystemDataObjectNullOrEmptyException(String.Format(Catalog.GetString("Function:{0} - The Gift Detail row is null!"),
                        Utilities.GetMethodName(true)));
            }
            else if (AFeeCode.Length == 0)
            {
                throw new ArgumentException(String.Format(Catalog.GetString("Function:{0} - The Fee code is empty!"),
                        Utilities.GetMethodName(true)));
            }

            #endregion Validate Arguments

            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("AddToFeeTotals");
            TLedgerInfo info = new TLedgerInfo(ALedgerNumber, db);
            string LedgerBaseCurrency = info.GetLedgerBaseCurrency();
            int NumDecPlaces = 2;

            try
            {
                //Round AFeeAmount

                /* 0003 Finds for ledger base currency format, for report currency format */
                db.ReadTransaction(
                    ref Transaction,
                    delegate
                    {
                        ACurrencyTable currencyInfo = ACurrencyAccess.LoadByPrimaryKey(LedgerBaseCurrency, Transaction);

                        #region Validate Data

                        if ((currencyInfo == null) || (currencyInfo.Count == 0))
                        {
                            throw new EFinanceSystemDataTableReturnedNoDataException(String.Format(Catalog.GetString(
                                        "Function:{0} - Currency data for Ledger base currency {1} does not exist or could not be accessed!"),
                                    Utilities.GetMethodName(true),
                                    LedgerBaseCurrency));
                        }

                        #endregion Validate Data

                        ACurrencyRow currencyRow = (ACurrencyRow)currencyInfo.Rows[0];

                        string numericFormat = currencyRow.DisplayFormat;
                        NumDecPlaces = THelperNumeric.CalcNumericFormatDecimalPlaces(numericFormat);
                    });

                //Round the fee amount
                AFeeAmount = Math.Round(AFeeAmount, NumDecPlaces);


                /* Get the record for the totals of the processed fees. */
                AProcessedFeeTable ProcessedFeeDataTable = AMainDS.AProcessedFee;
                AProcessedFeeRow ProcessedFeeRow =
                    (AProcessedFeeRow)ProcessedFeeDataTable.Rows.Find(new object[] { AGiftDetailRow.LedgerNumber,
                                                                                     AGiftDetailRow.BatchNumber,
                                                                                     AGiftDetailRow.GiftTransactionNumber,
                                                                                     AGiftDetailRow.DetailNumber,
                                                                                     AFeeCode });

                if (ProcessedFeeRow == null)
                {
                    ProcessedFeeRow = (AProcessedFeeRow)ProcessedFeeDataTable.NewRowTyped(false);
                    ProcessedFeeRow.LedgerNumber = AGiftDetailRow.LedgerNumber;
                    ProcessedFeeRow.BatchNumber = AGiftDetailRow.BatchNumber;
                    ProcessedFeeRow.GiftTransactionNumber = AGiftDetailRow.GiftTransactionNumber;
                    ProcessedFeeRow.DetailNumber = AGiftDetailRow.DetailNumber;
                    ProcessedFeeRow.FeeCode = AFeeCode;
                    ProcessedFeeRow.PeriodicAmount = 0;

                    ProcessedFeeDataTable.Rows.Add(ProcessedFeeRow);
                }

                ProcessedFeeRow.CostCentreCode = AGiftDetailRow.CostCentreCode;
                ProcessedFeeRow.PeriodNumber = APostingPeriod;

                /* Add the amount to the existing total. */
                ProcessedFeeRow.PeriodicAmount += AFeeAmount;
            }
            catch (Exception ex)
            {
                TLogging.LogException(ex, Utilities.GetMethodSignature());
                throw;
            }

            db.CloseDBConnection();
        }

        /// <summary>
        /// Prepares the gift batch for posting.
        /// Public for test.
        /// </summary>
        [NoRemoting]
        public static GiftBatchTDS PrepareGiftBatchForPosting(Int32 ALedgerNumber,
            Int32 ABatchNumber,
            ref TDBTransaction ATransaction,
            out TVerificationResultCollection AVerifications)
        {
            #region Validate Arguments

            if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }
            else if (ABatchNumber <= 0)
            {
                throw new EFinanceSystemInvalidBatchNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Batch number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber, ABatchNumber);
            }
            else if (ATransaction == null)
            {
                throw new EFinanceSystemDBTransactionNullException(String.Format(Catalog.GetString(
                            "Function:{0} - Database Transaction must not be NULL!"),
                        Utilities.GetMethodName(true)));
            }

            #endregion Validate Arguments

            bool ChangesToCommit = false;

            GiftBatchTDS MainDS = LoadAGiftBatchSingle(ALedgerNumber, ABatchNumber, ref ATransaction);

            string LedgerBaseCurrency = MainDS.ALedger[0].BaseCurrency;
            string LedgerIntlCurrency = MainDS.ALedger[0].IntlCurrency;

            AVerifications = new TVerificationResultCollection();

            //Check Batch status
            if (MainDS.AGiftBatch[0].BatchStatus != MFinanceConstants.BATCH_UNPOSTED)
            {
                AVerifications.Add(
                    new TVerificationResult(
                        "Posting Gift Batch",
                        String.Format("Cannot post batch ({0}, {1}) with status: {2}",
                            ALedgerNumber,
                            ABatchNumber,
                            MainDS.AGiftBatch[0].BatchStatus),
                        TResultSeverity.Resv_Critical));
                return null;
            }

            //Load all other related data for the batch and commit any changes
            MainDS.Merge(LoadAGiftBatchAndRelatedData(ALedgerNumber, ABatchNumber, ATransaction, out ChangesToCommit, true));

            if (ChangesToCommit)
            {
                GiftBatchTDSAccess.SubmitChanges(MainDS, ATransaction.DataBaseObj);
            }

            AGiftBatchRow GiftBatchRow = MainDS.AGiftBatch[0];

            string BatchTransactionCurrency = GiftBatchRow.CurrencyCode;

            // check that the Gift Batch BatchPeriod matches the date effective
            DateTime GLEffectiveDate = GiftBatchRow.GlEffectiveDate;
            DateTime StartOfMonth = new DateTime(GLEffectiveDate.Year, GLEffectiveDate.Month, 1);
            int DateEffectivePeriod, DateEffectiveYear;

            TFinancialYear.IsValidPostingPeriod(GiftBatchRow.LedgerNumber,
                GiftBatchRow.GlEffectiveDate,
                out DateEffectivePeriod,
                out DateEffectiveYear,
                ATransaction);

            decimal IntlToBaseExchRate;
            TExchangeRateTools.GetCorporateExchangeRate(LedgerBaseCurrency,
                LedgerIntlCurrency,
                StartOfMonth,
                GLEffectiveDate,
                out IntlToBaseExchRate,
                ATransaction.DataBaseObj);

            //Check Batch period
            if (GiftBatchRow.BatchPeriod != DateEffectivePeriod)
            {
                AVerifications.Add(
                    new TVerificationResult(
                        "Posting Gift Batch",
                        String.Format("Invalid gift batch period {0} for date {1}",
                            GiftBatchRow.BatchPeriod,
                            GLEffectiveDate),
                        TResultSeverity.Resv_Critical));
            }

            //Check international exchange rate
            if (IntlToBaseExchRate == 0)
            {
                AVerifications.Add(
                    new TVerificationResult(
                        "Posting Gift Batch",
                        String.Format(Catalog.GetString("No Corporate Exchange rate exists for the month: {0:MMMM yyyy}!"),
                            GLEffectiveDate),
                        TResultSeverity.Resv_Critical));
            }

            //Check Hash total
            if ((GiftBatchRow.HashTotal != 0) && (GiftBatchRow.BatchTotal != GiftBatchRow.HashTotal))
            {
                StringHelper myStringHelper = new StringHelper();
                myStringHelper.CurrencyFormatTable = ATransaction.DataBaseObj.SelectDT("SELECT * FROM PUB_a_currency", "a_currency", ATransaction);

                AVerifications.Add(
                    new TVerificationResult(
                        "Posting Gift Batch",
                        String.Format("The gift batch total ({0}) does not equal the hash total ({1}).",
                            myStringHelper.FormatUsingCurrencyCode(GiftBatchRow.BatchTotal, GiftBatchRow.CurrencyCode),
                            myStringHelper.FormatUsingCurrencyCode(GiftBatchRow.HashTotal, GiftBatchRow.CurrencyCode)),
                        TResultSeverity.Resv_Critical));
            }

            DataView GiftDetailsDV = new DataView(MainDS.AGiftDetail);
            GiftDetailsDV.Sort = string.Format("{0} ASC, {1} ASC",
                AGiftDetailTable.GetGiftTransactionNumberDBName(),
                AGiftDetailTable.GetDetailNumberDBName());

            //Check validity at the gift detail level
            //foreach (GiftBatchTDSAGiftDetailRow giftDetail in MainDS.AGiftDetail.Rows)
            foreach (DataRowView dRV in GiftDetailsDV)
            {
                GiftBatchTDSAGiftDetailRow giftDetail = (GiftBatchTDSAGiftDetailRow)dRV.Row;

                // find motivation detail row
                AMotivationDetailRow motivationRow =
                    (AMotivationDetailRow)MainDS.AMotivationDetail.Rows.Find(new object[] { ALedgerNumber,
                                                                                            giftDetail.MotivationGroupCode,
                                                                                            giftDetail.MotivationDetailCode });

                /*TODO: put this back in if GiftBatches can get posted from elsewhere
                 * //  i.e. bypassing the check for zero donor and or recipient from the Gift form
                 * //  Will also then need to pass system default values that allow donor/recip zero
                 * //  or user defaults if FINANCE-3 level user
                 *
                 * //do not allow posting gifts with no donor
                 * if (giftDetail.DonorKey == 0)
                 * {
                 *  AVerifications.Add(
                 *      new TVerificationResult(
                 *          "Posting Gift Batch",
                 *          String.Format(Catalog.GetString("Donor Key needed in gift {0}"),
                 *              giftDetail.GiftTransactionNumber),
                 *          TResultSeverity.Resv_Critical));
                 * }
                 *
                 * //do not allow posting gifts with no recipient
                 * if (giftDetail.RecipientKey == 0)
                 * {
                 *  AVerifications.Add(
                 *      new TVerificationResult(
                 *          "Posting Gift Batch",
                 *          String.Format(Catalog.GetString("Recipient Key needed in gift {0} and detail {1}"),
                 *              giftDetail.GiftTransactionNumber,
                 *              giftDetail.DetailNumber),
                 *          TResultSeverity.Resv_Critical));
                 * }
                 */

                //check for valid motivation detail code
                if (motivationRow == null)
                {
                    AVerifications.Add(
                        new TVerificationResult(
                            "Posting Gift Batch",
                            String.Format("Invalid motivation detail {0}/{1} in gift {2}",
                                giftDetail.MotivationGroupCode,
                                giftDetail.MotivationDetailCode,
                                giftDetail.GiftTransactionNumber),
                            TResultSeverity.Resv_Critical));
                }

                // data is only updated if the gift amount is positive
                if (giftDetail.GiftTransactionAmount >= 0)
                {
                    // The recipient ledger number must not be 0 if the motivation group is 'GIFT'
                    if ((giftDetail.RecipientClass == TPartnerClass.FAMILY.ToString())
                        && (giftDetail.IsRecipientLedgerNumberNull() || (giftDetail.RecipientLedgerNumber == 0))
                        && (giftDetail.MotivationGroupCode == MFinanceConstants.MOTIVATION_GROUP_GIFT))
                    {
                        AVerifications.Add(
                            new TVerificationResult(
                                "Posting Gift Batch",
                                String.Format(Catalog.GetString("No valid Gift Destination exists for the recipient {0} ({1}) of gift {2}."),
                                    giftDetail.RecipientDescription,
                                    giftDetail.RecipientKey.ToString("0000000000"),
                                    giftDetail.GiftTransactionNumber) +
                                "\n\n" +
                                Catalog.GetString(
                                    " A Gift Destination will need to be assigned to this Partner before this gift can be posted with the Motivation Group 'GIFT'."),
                                TResultSeverity.Resv_Critical));
                    }
                    //Check for missing cost centre code
                    else if (giftDetail.IsCostCentreCodeNull() || (giftDetail.CostCentreCode == string.Empty))
                    {
                        AVerifications.Add(
                            new TVerificationResult(
                                "Posting Gift Batch",
                                String.Format(Catalog.GetString("No valid Cost Centre Code exists for the recipient {0} ({1}) of gift {2}."),
                                    giftDetail.RecipientDescription,
                                    giftDetail.RecipientKey.ToString("0000000000"),
                                    giftDetail.GiftTransactionNumber) +
                                "\n\n" +
                                Catalog.GetString(
                                    "A Gift Destination will need to be assigned to this Partner."),
                                TResultSeverity.Resv_Critical));
                    }

                    //Check Cost centre code
                    if (!TPartnerServerLookups.PartnerOfTypeCCIsLinked(ALedgerNumber, giftDetail.RecipientKey))
                    {
                        AVerifications.Add(
                            new TVerificationResult(
                                "Posting Gift Batch",
                                String.Format(Catalog.GetString(
                                        "Recipient: {0} ({1} - with partner type 'Cost Centre') in Gift {2} has no linked cost center."),
                                    giftDetail.RecipientDescription,
                                    giftDetail.RecipientKey.ToString("0000000000"),
                                    giftDetail.GiftTransactionNumber) +
                                "\n\n" +
                                Catalog.GetString(
                                    " A linked Cost Centre needs to be added to this Recipient."),
                                TResultSeverity.Resv_Critical));
                    }
                }

                // set column giftdetail.AccountCode motivation
                giftDetail.AccountCode = motivationRow.AccountCode;

                // validate exchange rate to base
                if (GiftBatchRow.ExchangeRateToBase == 0)
                {
                    AVerifications.Add(
                        new TVerificationResult(
                            "Posting Gift Batch",
                            String.Format(Catalog.GetString("Exchange rate to base currency is 0 in Batch {0}!"),
                                ABatchNumber),
                            TResultSeverity.Resv_Critical));
                }

                if (AVerifications.Count > 0)
                {
                    continue;
                }

                //Calculate GiftAmount
                giftDetail.GiftAmount = GLRoutines.Divide(giftDetail.GiftTransactionAmount, GiftBatchRow.ExchangeRateToBase);

                if (BatchTransactionCurrency != LedgerIntlCurrency)
                {
                    giftDetail.GiftAmountIntl = GLRoutines.Divide(giftDetail.GiftAmount, IntlToBaseExchRate);
                }
                else
                {
                    giftDetail.GiftAmountIntl = giftDetail.GiftTransactionAmount;
                }

                //Redo Tax calculations
                AGiftDetailRow giftDetailRow = (AGiftDetailRow)giftDetail;
                TaxDeductibility.UpdateTaxDeductibiltyAmounts(ref giftDetailRow);

                // for calculation of admin fees
                LoadAdminFeeTablesForGiftDetail(MainDS, giftDetail, ATransaction);

                // get all motivation detail fees for this gift
                foreach (AMotivationDetailFeeRow motivationFeeRow in MainDS.AMotivationDetailFee.Rows)
                {
                    // If the charge flag is not set, still process fees for GIF and ICT but do not process other fees.
                    if (giftDetail.ChargeFlag
                        || (motivationFeeRow.FeeCode == MFinanceConstants.ADMIN_FEE_GIF)
                        || (motivationFeeRow.FeeCode == MFinanceConstants.ADMIN_FEE_ICT))
                    {
                        TVerificationResultCollection Verifications2;

                        decimal FeeAmount = CalculateAdminFee(MainDS,
                            ALedgerNumber,
                            motivationFeeRow.FeeCode,
                            giftDetail.GiftAmount,
                            out Verifications2);

                        if (!TVerificationHelper.IsNullOrOnlyNonCritical(Verifications2))
                        {
                            AVerifications.AddCollection(Verifications2);
                            continue;
                        }

                        if (FeeAmount != 0)
                        {
                            AddToFeeTotals(ALedgerNumber, MainDS, giftDetail, motivationFeeRow.FeeCode, FeeAmount, GiftBatchRow.BatchPeriod);
                        }
                    }
                }
            }

            if (AVerifications.Count > 0)
            {
                return null;
            }

            //Further changes would have been made at the gift detail level in recalculating amounts,
            //  but submitting changes will be done by the calling method
            return MainDS;
        }

        /// <summary>
        /// Loads tables needed for the calculation of admin fees for a gift detail.
        /// </summary>
        private static void LoadAdminFeeTablesForGiftDetail(GiftBatchTDS AMainDS,
            AGiftDetailRow AGiftDetail,
            TDBTransaction ATransaction)
        {
            // only needs to be loaded once for the whole batch
            if ((AMainDS.AProcessedFee == null) || (AMainDS.AProcessedFee.Rows.Count == 0))
            {
                AProcessedFeeAccess.LoadViaAGiftBatch(AMainDS, AGiftDetail.LedgerNumber, AGiftDetail.BatchNumber, ATransaction);
            }

            // only needs to be loaded once for the whole batch
            if ((AMainDS.AFeesPayable == null) || (AMainDS.AFeesPayable.Rows.Count == 0))
            {
                AFeesPayableAccess.LoadViaALedger(AMainDS, AGiftDetail.LedgerNumber, ATransaction);
            }

            // if motivation detail has changed from the last gift detail
            if ((AMainDS.AMotivationDetailFee == null) || (AMainDS.AMotivationDetailFee.Rows.Count == 0)
                || (AMainDS.AMotivationDetailFee[0].MotivationGroupCode != AGiftDetail.MotivationGroupCode)
                || (AMainDS.AMotivationDetailFee[0].MotivationDetailCode != AGiftDetail.MotivationDetailCode))
            {
                AMainDS.AMotivationDetailFee.Rows.Clear();
                AMotivationDetailFeeAccess.LoadViaAMotivationDetail(
                    AMainDS, AGiftDetail.LedgerNumber, AGiftDetail.MotivationGroupCode, AGiftDetail.MotivationDetailCode, ATransaction);
            }

            // If this gift is for the local field, don't charge the fee to itself. So we don't need the fees receivable.
            string Query = "SELECT a_fees_receivable.* FROM a_fees_receivable" +
                           " WHERE EXISTS (SELECT * FROM a_cost_centre" +
                           " WHERE a_cost_centre.a_ledger_number_i = " + AGiftDetail.LedgerNumber +
                           " AND a_cost_centre.a_cost_centre_code_c = '" + AGiftDetail.CostCentreCode + "'" +
                           " AND a_cost_centre.a_cost_centre_type_c = '" + MFinanceConstants.FOREIGN_CC_TYPE + "')" +
                           " AND a_fees_receivable.a_ledger_number_i = " + AGiftDetail.LedgerNumber;

            AMainDS.AFeesReceivable.Rows.Clear();

            // need to use a typed table to avoid problems with SQLite in the Merge
            AFeesReceivableTable tmpFeesReceivable = new AFeesReceivableTable();
            ATransaction.DataBaseObj.SelectDT(tmpFeesReceivable, Query, ATransaction);
            AMainDS.AFeesReceivable.Merge(tmpFeesReceivable);

            /*
             * So, previously it was !!a fatal exception!! if there were no applicaple fees returned!
             *
             * Modified Jan 2017 Tim Ingham
             *
             *         if ((AMainDS.AMotivationDetailFee != null) && (AMainDS.AMotivationDetailFee.Count > 0)
             *             && ((AMainDS.AFeesPayable == null) || (AMainDS.AFeesPayable.Rows.Count == 0))
             *             && ((AMainDS.AFeesReceivable == null) || (AMainDS.AFeesReceivable.Rows.Count == 0)))
             *         {
             *             throw new EFinanceSystemDataTableReturnedNoDataException(String.Format(Catalog.GetString(
             *                         "Function:{0} - Admin fee data for Gift Detail {1}, from Gift {2} in Batch {3} and Ledger {4} does not exist or could not be accessed!"),
             *                     Utilities.GetMethodSignature(),
             *                     AGiftDetail.DetailNumber,
             *                     AGiftDetail.GiftTransactionNumber,
             *                     AGiftDetail.BatchNumber,
             *                     AGiftDetail.LedgerNumber));
             *         }
             */
        }

        /// <summary>
        /// post a Gift Batch
        /// </summary>
        /// <param name="ALedgerNumber"></param>
        /// <param name="AGiftBatchNumber"></param>
        /// <param name="AGeneratedGlBatchNumber">If posting succeeds, this is the GL Batch</param>
        /// <param name="AVerifications"></param>
        /// <param name="ADataBase"></param>
        /// <returns>True if the batch posting went ahead</returns>
        [RequireModulePermission("FINANCE-2")]
        public static bool PostGiftBatch(Int32 ALedgerNumber,
            Int32 AGiftBatchNumber,
            out Int32 AGeneratedGlBatchNumber,
            out TVerificationResultCollection AVerifications,
            TDataBase ADataBase = null)
        {
            #region Validate Arguments

            if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }
            else if (AGiftBatchNumber <= 0)
            {
                throw new EFinanceSystemInvalidBatchNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Batch number must be greater than 0!"),
                        Utilities.GetMethodSignature()), ALedgerNumber, AGiftBatchNumber);
            }

            #endregion Validate Arguments

            List <Int32>GiftBatches = new List <int>();
            GiftBatches.Add(AGiftBatchNumber);
            AGeneratedGlBatchNumber = -1;

            List <Int32>GeneratedGLBatchNumbers = new List <int>();

            bool postWasOk = PostGiftBatches(ALedgerNumber, GiftBatches, GeneratedGLBatchNumbers, out AVerifications, ADataBase);

            if (postWasOk)
            {
                // there might not be a gl batch necessary, if no money was moved but only the donor updated
                if (GeneratedGLBatchNumbers.Count > 0)
                {
                    AGeneratedGlBatchNumber = GeneratedGLBatchNumbers[0];
                }
            }

            return postWasOk;
        }

        /// <summary>
        /// post several gift batches at once
        /// </summary>
        /// <returns>True if the batch posting went ahead</returns>
        //[RequireModulePermission("FINANCE-2")]
        [NoRemoting]
        public static bool PostGiftBatches(Int32 ALedgerNumber,
            List <Int32>AGiftBatchNumbers,
            List <Int32>AGeneratedGLBatchNumbers,
            out TVerificationResultCollection AVerifications,
            TDataBase ADataBase = null)
        {
            //Used in validation of arguments
            AVerifications = new TVerificationResultCollection();

            #region Validate Arguments

            if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }
            else if (AGiftBatchNumbers.Count == 0)
            {
                throw new ArgumentException(String.Format(Catalog.GetString(
                            "Function:{0} - No batches present to post!"),
                        Utilities.GetMethodName(true)));
            }

            foreach (Int32 batchNumber in AGiftBatchNumbers)
            {
                if (batchNumber <= 0)
                {
                    throw new EFinanceSystemInvalidBatchNumberException(String.Format(Catalog.GetString(
                                "Function:{0} - The Batch number must be greater than 0!"),
                            Utilities.GetMethodName(true)), ALedgerNumber, batchNumber);
                }
            }

            #endregion Validate Arguments

            Dictionary <Int32, String>BatchCurrencyCode = new Dictionary <Int32, String>();

            //For use in transaction delegate
            TVerificationResultCollection VerificationResult = AVerifications;
            TVerificationResultCollection SingleVerificationResultCollection;

            //Error handling
            string ErrorContext = "Posting a Gift Batch";
            string ErrorMessage = String.Empty;
            TResultSeverity ErrorType = TResultSeverity.Resv_Noncritical;

            //TVerificationResultCollection VerificationResult = null;

            TDBTransaction Transaction = new TDBTransaction();
            bool SubmissionOK = false;

            TDataBase db = DBAccess.Connect("PostGiftBatches", ADataBase);

            try
            {
                db.WriteTransaction(
                    ref Transaction,
                    ref SubmissionOK,
                    delegate
                    {
                        TProgressTracker.InitProgressTracker(DomainManager.GClientID.ToString(),
                            Catalog.GetString("Posting gift batches"),
                            AGiftBatchNumbers.Count * 3 + 1);

                        bool GLBatchIsRequired = false;

                        // first prepare all the gift batches, mark them as posted, and create the GL batches
                        foreach (Int32 BatchNumber in AGiftBatchNumbers)
                        {
                            TProgressTracker.SetCurrentState(DomainManager.GClientID.ToString(),
                                Catalog.GetString("Posting gift batches"),
                                AGiftBatchNumbers.IndexOf(BatchNumber) * 3);

                            GiftBatchTDS MainDS = PrepareGiftBatchForPosting(ALedgerNumber,
                                BatchNumber,
                                ref Transaction,
                                out SingleVerificationResultCollection);

                            VerificationResult.AddCollection(SingleVerificationResultCollection);

                            if (MainDS == null)
                            {
                                return;
                            }

                            BatchCurrencyCode[BatchNumber] = MainDS.AGiftBatch[0].CurrencyCode;
                            TProgressTracker.SetCurrentState(DomainManager.GClientID.ToString(),
                                Catalog.GetString("Posting gift batches"),
                                AGiftBatchNumbers.IndexOf(BatchNumber) * 3 + 1);

                            // create GL batch
                            GLBatchTDS GLDataset = CreateGLBatchAndTransactionsForPostingGifts(ALedgerNumber, ref MainDS, db);

                            ABatchRow batch = GLDataset.ABatch[0];

                            // it is possible that gl transactions are not actually needed for a gift posting.
                            // E.g. it is only a donor name adjustment -- there is no change in the general ledger account.
                            bool GLBatchNotRequired = GLDataset.ATransaction.Count == 0;

                            if (GLBatchNotRequired)
                            {
                                TGLPosting.DeleteGLBatch(ALedgerNumber, batch.BatchNumber, out SingleVerificationResultCollection, db);

                                VerificationResult.AddCollection(SingleVerificationResultCollection);
                            }
                            else
                            {
                                GLBatchIsRequired = true;
                            }

                            // save the batch (or delete if it is not actually needed)
                            if (GLBatchNotRequired || (TGLTransactionWebConnector.SaveGLBatchTDS(ref GLDataset,
                                                           out SingleVerificationResultCollection, db) == TSubmitChangesResult.scrOK))
                            {
                                if (!GLBatchNotRequired)  // i.e. GL batch is required and saved OK
                                {
                                    VerificationResult.AddCollection(SingleVerificationResultCollection);
                                    AGeneratedGLBatchNumbers.Add(batch.BatchNumber);
                                }

                                //
                                // Assign ReceiptNumbers to Gifts
                                //
                                ALedgerAccess.LoadByPrimaryKey(MainDS, ALedgerNumber, Transaction);

                                #region Validate Data

                                if ((MainDS.ALedger == null) || (MainDS.ALedger.Count == 0))
                                {
                                    throw new EFinanceSystemDataTableReturnedNoDataException(String.Format(Catalog.GetString(
                                                "Function:{0} - Ledger data for Ledger number {1} does not exist or could not be accessed!"),
                                            Utilities.GetMethodName(true),
                                            ALedgerNumber));
                                }

                                #endregion Validate Data

                                Int32 LastReceiptNumber = MainDS.ALedger[0].LastHeaderRNumber;

                                foreach (AGiftRow GiftRow in MainDS.AGift.Rows)
                                {
                                    LastReceiptNumber++;
                                    GiftRow.ReceiptNumber = LastReceiptNumber;
                                }

                                MainDS.ALedger[0].LastHeaderRNumber = LastReceiptNumber;

                                //Mark gift batch as posted
                                MainDS.AGiftBatch[0].BatchStatus = MFinanceConstants.BATCH_POSTED;

                                MainDS.ThrowAwayAfterSubmitChanges = true;
                                GiftBatchTDSAccess.SubmitChanges(MainDS, Transaction.DataBaseObj);
                            }
                            else
                            {
                                VerificationResult.AddCollection(SingleVerificationResultCollection);
                                return;
                            }
                        } // foreach BatchNumber

                        TProgressTracker.SetCurrentState(DomainManager.GClientID.ToString(),
                            Catalog.GetString("Posting gift batches"),
                            AGiftBatchNumbers.Count * 3 - 1);

                        // now post the GL batches
                        if (!TGLPosting.PostGLBatches(ALedgerNumber, AGeneratedGLBatchNumbers,
                                out SingleVerificationResultCollection, Transaction.DataBaseObj) && GLBatchIsRequired)
                        {
                            VerificationResult.AddCollection(SingleVerificationResultCollection);
                            // Transaction will be rolled back, no open GL batch flying around
                            SubmissionOK = true;
                        }
                        else
                        {
                            VerificationResult.AddCollection(SingleVerificationResultCollection);
                            SubmissionOK = true;

                            //
                            // I previously used "Client Tasks" to print the Batch Detail report on the client,
                            // but this functionality has now moved to the client PostGiftBatch method.
                        }
                    }); // Begin AutoTransaction
            }
            catch (EVerificationResultsException ex)
            {
                ErrorMessage = String.Format(Catalog.GetString("Function:{0} - Unexpected error while posting Gift batch to Ledger {1}!{2}{2}{3}"),
                    Utilities.GetMethodName(true),
                    ALedgerNumber,
                    Environment.NewLine,
                    ex.Message);
                ErrorType = TResultSeverity.Resv_Critical;

                VerificationResult = new TVerificationResultCollection();
                VerificationResult.Add(new TVerificationResult(ErrorContext, ErrorMessage, ErrorType));

                if (ex.InnerException != null)
                {
                    throw new EVerificationResultsException(ErrorMessage, VerificationResult, ex.InnerException);
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                if (TDBExceptionHelper.IsTransactionSerialisationException(ex))
                {
                    VerificationResult = new TVerificationResultCollection();
                    VerificationResult.Add(new TVerificationResult("PostGiftBatches",
                            ErrorCodeInventory.RetrieveErrCodeInfo(PetraErrorCodes.ERR_DB_SERIALIZATION_EXCEPTION)));
                }
                else
                {
                    TLogging.LogException(ex, Utilities.GetMethodSignature());
                    throw;
                }
            }
            finally
            {
                TProgressTracker.FinishJob(DomainManager.GClientID.ToString());
            }

            if (ADataBase == null)
            {
                db.CloseDBConnection();
            }

            AVerifications = VerificationResult;

            return SubmissionOK;
        }

        /// <summary>
        /// export all the Data of the batches matching the parameters to a String
        /// </summary>
        /// <param name="ALedgerNumber"></param>
        /// <param name="ABatchNumberStart"></param>
        /// <param name="ABatchNumberEnd"></param>
        /// <param name="ABatchDateFrom"></param>
        /// <param name="ABatchDateTo"></param>
        /// <param name="ADateFormatString"></param>
        /// <param name="ASummary"></param>
        /// <param name="AUseBaseCurrency"></param>
        /// <param name="ADateForSummary"></param>
        /// <param name="ANumberFormat">American or European</param>
        /// <param name="ATransactionsOnly"></param>
        /// <param name="AExtraColumns"></param>
        /// <param name="ARecipientNumber"></param>
        /// <param name="AFieldNumber"></param>
        /// <param name="AIncludeUnposted"></param>
        /// <param name="AExportExcel">the export file as Excel file</param>
        /// <param name="AMessages">Additional messages to display in a messagebox</param>
        /// <returns>number of exported batches</returns>
        [RequireModulePermission("FINANCE-1")]
        static public Int32 ExportAllGiftBatchData(
            Int32 ALedgerNumber,
            Int32 ABatchNumberStart,
            Int32 ABatchNumberEnd,
            DateTime? ABatchDateFrom,
            DateTime? ABatchDateTo,
            string ADateFormatString,
            bool ASummary,
            bool AUseBaseCurrency,
            DateTime? ADateForSummary,
            string ANumberFormat,
            bool ATransactionsOnly,
            bool AExtraColumns,
            Int64 ARecipientNumber,
            Int64 AFieldNumber,
            bool AIncludeUnposted,
            out String AExportExcel,
            out TVerificationResultCollection AMessages)
        {
            TGiftExporting Exporting = new TGiftExporting();

            return Exporting.ExportAllGiftBatchData(
                ALedgerNumber,
                ABatchNumberStart,
                ABatchNumberEnd,
                ABatchDateFrom,
                ABatchDateTo,
                ADateFormatString,
                ASummary,
                AUseBaseCurrency,
                ADateForSummary,
                ANumberFormat,
                ATransactionsOnly,
                AExtraColumns,
                ARecipientNumber,
                AFieldNumber,
                AIncludeUnposted,
                out AExportExcel, out AMessages);
        }

        /// <summary>
        /// Import Gift batch data
        /// The data file contents from the client is sent as a string, imported in the database
        /// and committed immediately
        /// </summary>
        /// <param name="requestParams">Hashtable containing the given params </param>
        /// <param name="importString">The import file as a simple String</param>
        /// <param name="ANeedRecipientLedgerNumber">Gifts in this table are responsible for failing the
        /// import becuase their Family recipients do not have an active Gift Destination</param>
        /// <param name="AClientRefreshRequired">Will be set to true if the client should refresh its data after importing.
        /// Normally this will be obvious (because the import was successful) but some handled Exceptions imply that the data has changed
        /// behind the client's back!</param>
        /// <param name="AMessages">Additional messages to display in a messagebox</param>
        /// <returns>false if error</returns>
        [RequireModulePermission("FINANCE-1")]
        public static bool ImportGiftBatches(
            Hashtable requestParams,
            String importString,
            out GiftBatchTDSAGiftDetailTable ANeedRecipientLedgerNumber,
            out bool AClientRefreshRequired,
            out TVerificationResultCollection AMessages
            )
        {
            TGiftImporting Importing = new TGiftImporting();

            return Importing.ImportGiftBatches(requestParams, importString, out ANeedRecipientLedgerNumber, out AClientRefreshRequired, out AMessages);
        }

        /// <summary>
        /// Import Gift batch transactions only into an existing batch
        /// The data file contents from the client is sent as a string, imported in the database
        /// and committed immediately
        /// </summary>
        /// <param name="requestParams">Hashtable containing the given params </param>
        /// <param name="importString">The import file as a simple String</param>
        /// <param name="AGiftBatchNumber">The gift batch number into which the transactions will be imported</param>
        /// <param name="ANeedRecipientLedgerNumber">Gifts in this table are responsible for failing the
        /// import becuase their Family recipients do not have an active Gift Destination</param>
        /// <param name="AClientRefreshRequired">Will be true if the client should update the GUI due to missing or updated information</param>
        /// <param name="AMessages">Additional messages to display in a messagebox</param>
        /// <returns>false if error</returns>
        [RequireModulePermission("FINANCE-1")]
        public static bool ImportGiftTransactions(
            Hashtable requestParams,
            String importString,
            Int32 AGiftBatchNumber,
            out GiftBatchTDSAGiftDetailTable ANeedRecipientLedgerNumber,
            out bool AClientRefreshRequired,
            out TVerificationResultCollection AMessages
            )
        {
            TGiftImporting Importing = new TGiftImporting();

            return Importing.ImportGiftTransactions(requestParams,
                importString,
                AGiftBatchNumber,
                out ANeedRecipientLedgerNumber,
                out AClientRefreshRequired,
                out AMessages);
        }

        /// <summary>
        /// Return the ESR defaults table (creating if necessary) for use in importing, or for client side editing.
        /// </summary>
        /// <returns></returns>
        [RequireModulePermission("FINANCE-1")]
        public static DataTable GetEsrDefaults()
        {
            return TGiftImporting.GetEsrDefaults();
        }

        /// <summary>
        /// Commit the ESR defaults table after client side editing.
        /// </summary>
        /// <param name="AEsrDefaults"></param>
        /// <returns></returns>
        [RequireModulePermission("FINANCE-3")]
        public static Boolean CommitEsrDefaults(DataTable AEsrDefaults)
        {
            return TGiftImporting.CommitEsrDefaults(AEsrDefaults);
        }

        /// <summary>
        /// Check if the partner key is valid
        /// </summary>
        /// <returns>If exists</returns>
        [RequireModulePermission("FINANCE-1")]
        public static bool VerifyPartnerKey(Int64 APartnerKey)
        {
            #region Validate Arguments

            if (APartnerKey < 0)
            {
                throw new ArgumentException(String.Format(Catalog.GetString("Function:{0} - The Partner Key cannot be negative!"),
                        Utilities.GetMethodName(true)));
            }

            #endregion Validate Arguments

            PPartnerTable PartnerTable = null;

            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("VerifyPartnerKey");

            db.ReadTransaction(
                ref Transaction,
                delegate
                {
                    PartnerTable = PPartnerAccess.LoadByPrimaryKey(APartnerKey, Transaction);
                });

            return PartnerTable != null && PartnerTable.Rows.Count > 0;
        }

        /// <summary>
        /// Load Partner Data
        /// </summary>
        /// <param name="APartnerKey">Partner Key </param>
        /// <returns>Partnertable for the partner Key</returns>
        [RequireModulePermission("FINANCE-1")]
        public static PPartnerTable LoadPartnerData(long APartnerKey)
        {
            #region Validate Arguments

            if (APartnerKey < 0)
            {
                throw new ArgumentException(String.Format(Catalog.GetString("Function:{0} - The Partner Key cannot be negative!"),
                        Utilities.GetMethodName(true)));
            }

            #endregion Validate Arguments

            PPartnerTable PartnerTbl = null;

            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("LoadPartnerData");

            db.ReadTransaction(
                ref Transaction,
                delegate
                {
                    PartnerTbl = PPartnerAccess.LoadByPrimaryKey(APartnerKey, Transaction);
                });

            PartnerTbl.AcceptChanges();

            return PartnerTbl;
        }

        /// <summary>
        /// Load Partner Data
        /// </summary>
        /// <param name="ALedgerNumber">Ledger number</param>
        /// <param name="ABatchNumber">Batch number</param>
        /// <returns>Partnertable for the partner Key</returns>
        [RequireModulePermission("FINANCE-1")]
        public static PPartnerTable LoadAllPartnerDataForBatch(Int32 ALedgerNumber, Int32 ABatchNumber)
        {
            #region Validate Arguments

            if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }
            else if (ABatchNumber <= 0)
            {
                throw new EFinanceSystemInvalidBatchNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Batch number must be greater than 0!"),
                        Utilities.GetMethodSignature()), ALedgerNumber, ABatchNumber);
            }

            #endregion Validate Arguments

            PPartnerTable PartnerTbl = new PPartnerTable();

            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("LoadAllPartnerDataForBatch");

            db.ReadTransaction(
                ref Transaction,
                delegate
                {
                    // load all partners for specified batch
                    string sQL =
                        String.Format("SELECT DISTINCT p.*" +
                            " FROM public.p_partner p, public.a_gift g" +
                            " WHERE p.p_partner_key_n = g.p_donor_key_n" +
                            "   And g.a_ledger_number_i = {0}" +
                            "   And g.a_batch_number_i = {1};",
                            ALedgerNumber,
                            ABatchNumber);

                    DataTable pTbl = (DataTable)db.SelectDT(sQL, "PartnerTable", Transaction);

                    if (pTbl.Rows.Count > 0)
                    {
                        DataUtilities.ChangeDataTableToTypedDataTable(ref pTbl, typeof(PPartnerTable), "");
                        PartnerTbl = (PPartnerTable)pTbl;
                        PartnerTbl.AcceptChanges();
                    }
                });

            db.CloseDBConnection();

            return PartnerTbl;
        }

        /// <summary>
        /// Load Donor Banking Details
        /// </summary>
        /// <param name="APartnerKey">Partner Key </param>
        /// <param name="ABankingDetailsKey">Banking Details Key Key </param>
        /// <returns>Partnertable for the partner Key</returns>
        [RequireModulePermission("FINANCE-1")]
        public static PBankingDetailsTable GetDonorBankingDetails(long APartnerKey, int ABankingDetailsKey = 0)
        {
            #region Validate Arguments

            if (APartnerKey < 0)
            {
                throw new ArgumentException(String.Format(Catalog.GetString("Function:{0} - The Partner Key cannot be negative!"),
                        Utilities.GetMethodName(true)));
            }

            #endregion Validate Arguments

            PBankingDetailsTable DonorBankingDetails = new PBankingDetailsTable();

            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("GetDonorBankingDetails");

            db.ReadTransaction(
                ref Transaction,
                delegate
                {
                    if (ABankingDetailsKey == 0)
                    {
                        PBankingDetailsTable BankingDetailsTable =
                            PBankingDetailsAccess.LoadViaPPartner(APartnerKey, Transaction);

                        // Find partner's 'main' bank account
                        foreach (PBankingDetailsRow Row in BankingDetailsTable.Rows)
                        {
                            if (PBankingDetailsUsageAccess.Exists(APartnerKey, Row.BankingDetailsKey, "MAIN", Transaction))
                            {
                                DonorBankingDetails.Rows.Add((object[])Row.ItemArray.Clone());
                                break;
                            }
                        }
                    }
                    else
                    {
                        DonorBankingDetails = PBankingDetailsAccess.LoadByPrimaryKey(ABankingDetailsKey, Transaction);
                    }
                });

            if (DonorBankingDetails != null)
            {
                DonorBankingDetails.AcceptChanges();
            }

            return DonorBankingDetails;
        }

        /// <summary>
        /// Load Partner Tax Deductible Pct
        /// </summary>
        /// <param name="APartnerKey">Partner Key </param>
        /// <returns>PPartnerTaxDeductiblePctTable for the partner Key</returns>
        [RequireModulePermission("FINANCE-1")]
        public static PPartnerTaxDeductiblePctTable LoadPartnerTaxDeductiblePct(long APartnerKey)
        {
            #region Validate Arguments

            if (APartnerKey < 0)
            {
                throw new ArgumentException(String.Format(Catalog.GetString("Function:{0} - The Partner Key cannot be negative!"),
                        Utilities.GetMethodName(true)));
            }

            #endregion Validate Arguments

            PPartnerTaxDeductiblePctTable PartnerTaxDeductiblePct = null;

            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("LoadPartnerTaxDeductiblePct");

            db.ReadTransaction(
                ref Transaction,
                delegate
                {
                    PartnerTaxDeductiblePct = PPartnerTaxDeductiblePctAccess.LoadViaPPartner(APartnerKey, Transaction);
                });

            PartnerTaxDeductiblePct.AcceptChanges();

            return PartnerTaxDeductiblePct;
        }

        /// <summary>
        /// Load the most recent Partner Tax Deductible Pct
        /// </summary>
        /// <param name="APartnerKey">Partner Key </param>
        /// <param name="ADateValidFrom">To match nearest date valid from</param>
        /// <returns>PPartnerTaxDeductiblePctTable for the partner Key</returns>
        [RequireModulePermission("FINANCE-1")]
        public static PPartnerTaxDeductiblePctTable LoadPartnerTaxDeductiblePct(long APartnerKey, DateTime ADateValidFrom)
        {
            #region Validate Arguments

            if (APartnerKey < 0)
            {
                throw new ArgumentException(String.Format(Catalog.GetString("Function:{0} - The Partner Key cannot be negative!"),
                        Utilities.GetMethodName(true)));
            }

            #endregion Validate Arguments

            PPartnerTaxDeductiblePctTable PartnerTaxDeductiblePct = null;

            PPartnerTaxDeductiblePctTable PartnerTaxPercentTable = new PPartnerTaxDeductiblePctTable();
            PPartnerTaxDeductiblePctRow PartnerTaxPercentTemplateRow = (PPartnerTaxDeductiblePctRow)PartnerTaxPercentTable.NewRowTyped(false);

            PartnerTaxPercentTemplateRow.PartnerKey = APartnerKey;
            PartnerTaxPercentTemplateRow.DateValidFrom = ADateValidFrom;

            StringCollection Operators0 = StringHelper.InitStrArr(new string[] { "=", "<=" });
            StringCollection OrderList0 = new StringCollection();

            OrderList0.Add("ORDER BY");
            OrderList0.Add(PPartnerTaxDeductiblePctTable.GetDateValidFromDBName() + " DESC");

            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("LoadPartnerTaxDeductiblePct");

            db.ReadTransaction(
                ref Transaction,
                delegate
                {
                    PartnerTaxDeductiblePct = PPartnerTaxDeductiblePctAccess.LoadUsingTemplate(PartnerTaxPercentTemplateRow,
                        Operators0,
                        null,
                        Transaction,
                        OrderList0,
                        0,
                        0);
                });

            if (PartnerTaxDeductiblePct != null)
            {
                //Only want the most recent row
                int numRecs = PartnerTaxDeductiblePct.Count;

                if (numRecs > 1)
                {
                    for (int i = numRecs - 1; i > 0; i--)
                    {
                        PartnerTaxDeductiblePct.Rows[i].Delete();
                    }
                }

                PartnerTaxDeductiblePct.AcceptChanges();
            }

            return PartnerTaxDeductiblePct;
        }

        /// <summary>
        /// Load any Tax record for this partner
        /// </summary>
        /// <param name="APartnerKey"></param>
        /// <returns></returns>
        [RequireModulePermission("FINANCE-1")]
        public static PTaxTable LoadPartnerPtax(long APartnerKey)
        {
            PTaxTable taxTbl = new PTaxTable();
            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("LoadPartnerPtax");

            db.ReadTransaction(
                ref Transaction,
                delegate
                {
                    taxTbl = PTaxAccess.LoadViaPPartner(APartnerKey, Transaction);
                });

            return taxTbl;
        }

        /// <summary>
        /// Find the cost centre associated with the partner
        /// </summary>
        /// <returns>Cost Centre code</returns>
        [RequireModulePermission("FINANCE-1")]
        public static string IdentifyPartnerCostCentre(Int32 ALedgerNumber, Int64 AFieldNumber)
        {
            #region Validate Arguments

            if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }
            else if (AFieldNumber < 0)
            {
                throw new ArgumentException(String.Format(Catalog.GetString("Function:{0} - The Field number cannot be negative!"),
                        Utilities.GetMethodName(true)));
            }

            #endregion Validate Arguments

            TCacheable CachePopulator = new TCacheable();
            Type typeOfTable;

            AValidLedgerNumberTable ValidLedgerNumbers = (AValidLedgerNumberTable)
                                                         CachePopulator.GetCacheableTable(TCacheableFinanceTablesEnum.ValidLedgerNumberList,
                "",
                false,
                out typeOfTable);

            AValidLedgerNumberRow ValidLedgerNumberRow = null;

            if (ValidLedgerNumbers != null)
            {
                ValidLedgerNumberRow = (AValidLedgerNumberRow)ValidLedgerNumbers.Rows.Find(new object[] { ALedgerNumber, AFieldNumber });
            }

            if (ValidLedgerNumberRow != null)
            {
                return ValidLedgerNumberRow.CostCentreCode;
            }
            else
            {
                return TGLTransactionWebConnector.GetStandardCostCentre(ALedgerNumber);
            }
        }

        /// <summary>
        /// get the recipient ledger partner for a unit or the gift destination for a family
        /// </summary>
        /// <param name="APartnerKey"></param>
        /// <param name="AGiftDate">Gift Date (needed for getting a family's Gift Destination)</param>
        /// <returns></returns>
        [RequireModulePermission("FINANCE-1")]
        public static Int64 GetRecipientFundNumber(Int64 APartnerKey, DateTime? AGiftDate = null)
        {
            #region Validate Arguments

            if (APartnerKey < 0)
            {
                throw new ArgumentException(String.Format(Catalog.GetString("Function:{0} - The Partner Key cannot be negative!"),
                        Utilities.GetMethodName(true)));
            }

            #endregion Validate Arguments

            bool DataLoaded = false;
            Int64 Result = -1;

            GiftBatchTDS MainDS = new GiftBatchTDS();

            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("GetRecipientFundNumber");

            try
            {
                db.ReadTransaction(
                    ref Transaction,
                    delegate
                    {
                        MainDS.LedgerPartnerTypes.Merge(PPartnerTypeAccess.LoadViaPType(MPartnerConstants.PARTNERTYPE_LEDGER, Transaction));
                        MainDS.RecipientPartners.Merge(PPartnerAccess.LoadByPrimaryKey(APartnerKey, Transaction));
                        MainDS.RecipientFamily.Merge(PFamilyAccess.LoadByPrimaryKey(APartnerKey, Transaction));
                        MainDS.RecipientPerson.Merge(PPersonAccess.LoadByPrimaryKey(APartnerKey, Transaction));
                        MainDS.RecipientUnit.Merge(PUnitAccess.LoadByPrimaryKey(APartnerKey, Transaction));

                        #region Validate Data

                        if ((MainDS.LedgerPartnerTypes == null) || (MainDS.LedgerPartnerTypes.Count == 0))
                        {
                            throw new EFinanceSystemDataTableReturnedNoDataException(Catalog.GetString(
                                    "GetRecipientFundNumber: Ledger Partner Types data does not exist or could not be accessed."));
                        }
                        else if ((MainDS.RecipientPartners == null) || (MainDS.RecipientPartners.Count == 0))
                        {
                            throw new EFinanceSystemDataTableReturnedNoDataException(String.Format(Catalog.GetString(
                                        "GetRecipientFundNumber: Recipient data for Partner Key {0} does not exist or could not be accessed."),
                                    APartnerKey));
                        }

                        #endregion Validate Data

                        DataLoaded = true;
                    });

                if (DataLoaded)
                {
                    Result = GetRecipientFundNumberInner(MainDS, APartnerKey, AGiftDate, db);
                }
                else
                {
                    Result = 0;
                }
            }
            catch (Exception ex)
            {
                TLogging.LogException(ex, Utilities.GetMethodSignature());
                throw;
            }

            db.CloseDBConnection();

            return Result;
        }

        private static Int64 GetRecipientFundNumberInner(GiftBatchTDS AMainDS, Int64 APartnerKey, DateTime? AGiftDate = null, TDataBase ADataBase = null)
        {
            #region Validate Arguments

            if (AMainDS == null)
            {
                throw new EFinanceSystemDataObjectNullOrEmptyException(String.Format(Catalog.GetString(
                            "Function:{0} - The Gift Batch dataset is null!"),
                        Utilities.GetMethodName(true)));
            }
            else if (APartnerKey < 0)
            {
                throw new ArgumentException(String.Format(Catalog.GetString("Function:{0} - The Partner Key must be greater than 0!"),
                        Utilities.GetMethodName(true)));
            }

            #endregion Validate Arguments

            if (APartnerKey == 0)
            {
                return 0;
            }

            //Look in RecipientFamily table
            PFamilyRow FamilyRow = (PFamilyRow)AMainDS.RecipientFamily.Rows.Find(APartnerKey);

            if (FamilyRow != null)
            {
                return GetRecipientGiftDestination(APartnerKey, AGiftDate);
            }

            //Look in RecipientPerson table
            PPersonRow PersonRow = (PPersonRow)AMainDS.RecipientPerson.Rows.Find(APartnerKey);

            if (PersonRow != null)
            {
                return GetRecipientGiftDestination(PersonRow.FamilyKey, AGiftDate);
            }

            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("GetRecipientFundNumberInner", ADataBase);

            //Check that LedgerPartnertypes are already loaded
            if ((AMainDS.LedgerPartnerTypes != null) && (AMainDS.LedgerPartnerTypes.Count == 0))
            {
                PPartnerTypeTable PPTTable = null;

                db.ReadTransaction(
                    ref Transaction,
                    delegate
                    {
                        PPTTable = PPartnerTypeAccess.LoadViaPType(MPartnerConstants.PARTNERTYPE_LEDGER, Transaction);

                        #region Validate Data

                        if ((PPTTable == null) || (PPTTable.Count == 0))
                        {
                            throw new EFinanceSystemDataTableReturnedNoDataException(String.Format(Catalog.GetString(
                                        "Function:{0} - Ledger Partner Types data does not exist or could not be accessed!"),
                                    Utilities.GetMethodName(true)));
                        }

                        #endregion Validate Data
                    });

                AMainDS.LedgerPartnerTypes.Merge(PPTTable);
            }

            if ((AMainDS.LedgerPartnerTypes != null)
                && (AMainDS.LedgerPartnerTypes.Rows.Find(new object[] { APartnerKey, MPartnerConstants.PARTNERTYPE_LEDGER }) != null))
            {
                //TODO Warning on inactive Fund from p_partner table
                if (ADataBase == null)
                {
                    db.CloseDBConnection();
                }

                return APartnerKey;
            }

            UmUnitStructureTable UnitStructTbl = null;

            db.ReadTransaction(
                ref Transaction,
                delegate
                {
                    UnitStructTbl = UmUnitStructureAccess.LoadViaPUnitChildUnitKey(APartnerKey, Transaction);
                });

            if ((UnitStructTbl != null) && (UnitStructTbl.Rows.Count > 0))
            {
                UmUnitStructureRow structureRow = UnitStructTbl[0];

                if (structureRow.ParentUnitKey == structureRow.ChildUnitKey)
                {
                    // should not get here
                    TLogging.Log("GetRecipientFundNumberInner: - should not get here");
                    return 0;
                }

                // recursive call until we find a partner that has partnertype LEDGER
                Int64 result = GetRecipientFundNumberInner(AMainDS, structureRow.ParentUnitKey, null, db);

                if (ADataBase == null)
                {
                    db.CloseDBConnection();
                }

                return result;
            }
            else
            {
                if (ADataBase == null)
                {
                    db.CloseDBConnection();
                }

                return APartnerKey;
            }
        }

        /// <summary>
        /// Check if Key Ministry exists
        /// </summary>
        /// <param name="APartnerKey">Partner Key </param>
        /// <param name="AIsActive">return true if Key Ministry is active </param>
        /// <returns>return true if APartnerKey identifies a Key Ministry</returns>
        [RequireModulePermission("FINANCE-1")]
        public static Boolean KeyMinistryExists(Int64 APartnerKey, out Boolean AIsActive)
        {
            #region Validate Arguments

            if (APartnerKey < 0)
            {
                throw new ArgumentException(String.Format(Catalog.GetString("Function:{0} - The Partner Key cannot be negative!"),
                        Utilities.GetMethodName(true)));
            }

            #endregion Validate Arguments

            Boolean KeyMinistryExists = false;
            bool IsActive = false;

            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("KeyMinistryExists");

            try
            {
                db.ReadTransaction(
                    ref Transaction,
                    delegate
                    {
                        PUnitTable UnitTable = PUnitAccess.LoadByPrimaryKey(APartnerKey, Transaction);

                        if ((UnitTable != null) && (UnitTable.Rows.Count == 1))
                        {
                            // this partner is indeed a unit
                            PUnitRow UnitRow = UnitTable[0];

                            if (UnitRow.UnitTypeCode.Equals(MPartnerConstants.UNIT_TYPE_KEYMIN))
                            {
                                KeyMinistryExists = true;

                                PPartnerTable PartnerTable = PPartnerAccess.LoadByPrimaryKey(APartnerKey, Transaction);

                                #region Validate Data

                                if ((PartnerTable == null) || (PartnerTable.Count == 0))
                                {
                                    throw new EFinanceSystemDataTableReturnedNoDataException(String.Format(Catalog.GetString(
                                                "Function:{0} - Partner data for Partner Key {1} does not exist or could not be accessed!"),
                                            Utilities.GetMethodName(true),
                                            APartnerKey));
                                }

                                #endregion Validate Data

                                PPartnerRow PartnerRow = PartnerTable[0];

                                if (SharedTypes.StdPartnerStatusCodeStringToEnum(PartnerRow.StatusCode) == TStdPartnerStatusCode.spscACTIVE)
                                {
                                    IsActive = true;
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

            AIsActive = IsActive;

            return KeyMinistryExists;
        }

        /// <summary>
        /// Check if Key Ministry exists
        /// </summary>
        /// <param name="AKeyMinPartnerKey">Partner Key </param>
        /// <returns>return true if AKeyMinPartnerKey identifies an active Key Ministry</returns>
        [RequireModulePermission("FINANCE-1")]
        public static Boolean KeyMinistryIsActive(Int64 AKeyMinPartnerKey)
        {
            #region Validate Arguments

            if (AKeyMinPartnerKey < 0)
            {
                throw new ArgumentException(String.Format(Catalog.GetString("Function:{0} - The Key Ministry Partner Key cannot be negative!"),
                        Utilities.GetMethodName(true)));
            }

            #endregion Validate Arguments

            Boolean KeyMinistryIsActive = false;

            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("KeyMinistryIsActive");

            try
            {
                db.ReadTransaction(
                    ref Transaction,
                    delegate
                    {
                        PPartnerTable PartnerTable = PPartnerAccess.LoadByPrimaryKey(AKeyMinPartnerKey, Transaction);

                        #region Validate Data

                        if ((PartnerTable == null) || (PartnerTable.Count == 0))
                        {
                            throw new EFinanceSystemDataTableReturnedNoDataException(String.Format(Catalog.GetString(
                                        "Function:{0} - Partner data for Partner Key {1} does not exist or could not be accessed!"),
                                    Utilities.GetMethodName(true),
                                    AKeyMinPartnerKey));
                        }

                        #endregion Validate Data

                        PPartnerRow PartnerRow = PartnerTable[0];

                        KeyMinistryIsActive =
                            (SharedTypes.StdPartnerStatusCodeStringToEnum(PartnerRow.StatusCode) == TStdPartnerStatusCode.spscACTIVE);
                    });
            }
            catch (Exception ex)
            {
                TLogging.LogException(ex, Utilities.GetMethodSignature());
                throw;
            }

            return KeyMinistryIsActive;
        }

        /// <summary>
        /// Load key Ministry
        /// </summary>
        /// <param name="APartnerKey">Partner Key </param>
        /// <param name="AFieldNumber">Field Number </param>
        /// <param name="AActiveOnly">Field Number </param>
        /// <returns>ArrayList for loading the key ministry combobox</returns>
        [RequireModulePermission("FINANCE-1")]
        public static PUnitTable LoadKeyMinistry(Int64 APartnerKey, out Int64 AFieldNumber, bool AActiveOnly = true)
        {
            #region Validate Arguments

            if (APartnerKey < 0)
            {
                throw new ArgumentException(String.Format(Catalog.GetString("Function:{0} - The Partner Key cannot be negative!"),
                        Utilities.GetMethodName(true)));
            }

            #endregion Validate Arguments

            AFieldNumber = 0;
            Int64 FieldNumber = AFieldNumber;

            PUnitTable UnitTable = null;
            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("LoadKeyMinistry");

            try
            {
                db.ReadTransaction(
                    ref Transaction,
                    delegate
                    {
                        UnitTable = LoadKeyMinistries(APartnerKey, Transaction, AActiveOnly);
                        FieldNumber = GetRecipientFundNumber(APartnerKey);
                    });

                UnitTable.AcceptChanges();
            }
            catch (Exception ex)
            {
                TLogging.LogException(ex, Utilities.GetMethodSignature());
                throw;
            }

            AFieldNumber = FieldNumber;

            return UnitTable;
        }

        /// <summary>
        /// get the key ministries. If Recipient is a field, get the key ministries of that field.
        /// If Recipient is a key ministry itself, get all key ministries of the same field
        /// </summary>
        private static PUnitTable LoadKeyMinistries(Int64 ARecipientPartnerKey, TDBTransaction ATransaction, bool AActiveOnly)
        {
            #region Validate Arguments

            if (ARecipientPartnerKey < 0)
            {
                throw new ArgumentException(String.Format(Catalog.GetString("Function:{0} - The Recipient Partner Key cannot be negative!"),
                        Utilities.GetMethodName(true)));
            }
            else if (ATransaction == null)
            {
                throw new EFinanceSystemDBTransactionNullException(String.Format(Catalog.GetString(
                            "Function:{0} - Database Transaction must not be NULL!"),
                        Utilities.GetMethodName(true)));
            }

            #endregion Validate Arguments

            PUnitTable UnitTable = PUnitAccess.LoadByPrimaryKey(ARecipientPartnerKey, ATransaction);

            if ((UnitTable != null) && (UnitTable.Rows.Count == 1))
            {
                // this partner is a unit
                PUnitRow unitRow = UnitTable[0];

                switch (unitRow.UnitTypeCode)
                {
                    case MPartnerConstants.UNIT_TYPE_KEYMIN:
                        Int64 fieldNumber = GetRecipientFundNumber(ARecipientPartnerKey);
                        UnitTable = LoadKeyMinistriesOfField(fieldNumber, ATransaction, AActiveOnly);
                        break;

                    case MPartnerConstants.UNIT_TYPE_FIELD:
                    case MPartnerConstants.UNIT_TYPE_AREA:
                        UnitTable = LoadKeyMinistriesOfField(ARecipientPartnerKey, ATransaction, AActiveOnly);
                        break;
                }
            }

            return UnitTable;
        }

        private static PUnitTable LoadKeyMinistriesOfField(Int64 APartnerKey, TDBTransaction ATransaction, bool AActiveOnly)
        {
            #region Validate Arguments

            if (APartnerKey < 0)
            {
                throw new ArgumentException(String.Format(Catalog.GetString("Function:{0} - The Partner Key cannot be negative!"),
                        Utilities.GetMethodName(true)));
            }
            else if (ATransaction == null)
            {
                throw new EFinanceSystemDBTransactionNullException(String.Format(Catalog.GetString(
                            "Function:{0} - Database Transaction must not be NULL!"),
                        Utilities.GetMethodName(true)));
            }

            #endregion Validate Arguments

            string sqlLoadKeyMinistriesOfField =
                "SELECT unit.* FROM PUB_um_unit_structure us, PUB_p_unit unit, PUB_p_partner partner " +
                "WHERE us.um_parent_unit_key_n = " + APartnerKey.ToString() + " " +
                "AND unit.p_partner_key_n = us.um_child_unit_key_n " +
                "AND unit.u_unit_type_code_c = '" + MPartnerConstants.UNIT_TYPE_KEYMIN + "' " +
                "AND partner.p_partner_key_n = unit.p_partner_key_n ";

            if (AActiveOnly)
            {
                sqlLoadKeyMinistriesOfField += " AND partner.p_status_code_c = '" + MPartnerConstants.PARTNERSTATUS_ACTIVE + "'";
            }

            PUnitTable UnitTable = new PUnitTable();

            ATransaction.DataBaseObj.SelectDT(UnitTable, sqlLoadKeyMinistriesOfField, ATransaction, new OdbcParameter[0], 0, 0);

            return UnitTable;
        }

        /// <summary>
        /// Load Inactive Key Ministries Found In Batch
        /// </summary>
        /// <param name="ALedgerNumber"></param>
        /// <param name="ABatchNumber"></param>
        /// <param name="AInactiveKMsTable"></param>
        /// <param name="ARecurringGift"></param>
        /// <returns>Return true if inactive ones found</returns>
        [RequireModulePermission("FINANCE-1")]
        public static bool InactiveKeyMinistriesFoundInBatch(Int32 ALedgerNumber,
            Int32 ABatchNumber,
            out DataTable AInactiveKMsTable,
            bool ARecurringGift = false)
        {
            #region Validate Arguments

            if (ALedgerNumber <= 0)
            {
                throw new EFinanceSystemInvalidLedgerNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Ledger number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber);
            }
            else if (ABatchNumber <= 0)
            {
                throw new EFinanceSystemInvalidBatchNumberException(String.Format(Catalog.GetString(
                            "Function:{0} - The Batch number must be greater than 0!"),
                        Utilities.GetMethodName(true)), ALedgerNumber, ABatchNumber);
            }

            #endregion Validate Arguments

            string SQLLoadInactiveKeyMinistriesInBatch = string.Empty;

            AInactiveKMsTable = new DataTable();
            AInactiveKMsTable.Columns.Add(new DataColumn(AGiftDetailTable.GetGiftTransactionNumberDBName(), typeof(Int32)));
            AInactiveKMsTable.Columns.Add(new DataColumn(AGiftDetailTable.GetDetailNumberDBName(), typeof(Int32)));
            AInactiveKMsTable.Columns.Add(new DataColumn(AGiftDetailTable.GetRecipientKeyDBName(), typeof(Int64)));
            AInactiveKMsTable.Columns.Add(new DataColumn(PUnitTable.GetUnitNameDBName(), typeof(String)));

            if (!ARecurringGift)
            {
                AInactiveKMsTable.Columns.Add(new DataColumn(AGiftDetailTable.GetModifiedDetailDBName(), typeof(Boolean)));
            }

            DataTable InactiveKMsTable = AInactiveKMsTable;

            TDBTransaction Transaction = new TDBTransaction();
            TDataBase db = DBAccess.Connect("InactiveKeyMinistriesFoundInBatch");

            try
            {
                db.ReadTransaction(
                    ref Transaction,
                    delegate
                    {
                        SQLLoadInactiveKeyMinistriesInBatch =
                            "SELECT gd.a_gift_transaction_number_i, a_detail_number_i, p_recipient_key_n, unit.p_unit_name_c" +
                            (ARecurringGift ? " FROM a_recurring_gift_detail gd," : ", gd.a_modified_detail_l FROM a_gift_detail gd,") +
                            "   um_unit_structure us, p_unit unit, p_partner partner" +
                            " WHERE gd.p_recipient_key_n = partner.p_partner_key_n" +
                            "   AND gd.a_ledger_number_i = " + ALedgerNumber.ToString() +
                            "   AND gd.a_batch_number_i = " + ABatchNumber.ToString() +
                            (ARecurringGift ? " AND gd.a_gift_amount_n > 0" : "") +
                            "   AND partner.p_partner_key_n = unit.p_partner_key_n" +
                            "   AND partner.p_status_code_c = '" + MPartnerConstants.PARTNERSTATUS_INACTIVE + "'" +
                            "   AND unit.p_partner_key_n = us.um_child_unit_key_n" +
                            "   AND unit.u_unit_type_code_c = '" + MPartnerConstants.UNIT_TYPE_KEYMIN + "';";

                        db.SelectDT(InactiveKMsTable, SQLLoadInactiveKeyMinistriesInBatch, Transaction, new OdbcParameter[0], 0, 0);
                    });
            }
            catch (Exception ex)
            {
                TLogging.LogException(ex, Utilities.GetMethodSignature());
                throw;
            }

            db.CloseDBConnection();

            return AInactiveKMsTable.Rows.Count > 0;
        }

        #region Data Validation

        static partial void ValidateGiftBatch(ref TVerificationResultCollection AVerificationResult, TTypedDataTable ASubmitTable);
        static partial void ValidateGiftBatchManual(ref TVerificationResultCollection AVerificationResult, TTypedDataTable ASubmitTable);
        static partial void ValidateGiftDetail(ref TVerificationResultCollection AVerificationResult, TTypedDataTable ASubmitTable);
        static partial void ValidateGiftDetailManual(ref TVerificationResultCollection AVerificationResult, TTypedDataTable ASubmitTable);

        static partial void ValidateRecurringGiftBatch(ref TVerificationResultCollection AVerificationResult, TTypedDataTable ASubmitTable);
        static partial void ValidateRecurringGiftBatchManual(ref TVerificationResultCollection AVerificationResult, TTypedDataTable ASubmitTable);
        static partial void ValidateRecurringGiftDetail(ref TVerificationResultCollection AVerificationResult, TTypedDataTable ASubmitTable);
        static partial void ValidateRecurringGiftDetailManual(ref TVerificationResultCollection AVerificationResult, TTypedDataTable ASubmitTable);

        #endregion Data Validation
    }
}
