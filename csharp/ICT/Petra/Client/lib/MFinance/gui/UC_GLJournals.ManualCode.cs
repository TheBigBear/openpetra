//
// DO NOT REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
//
// @Authors:
//       timop
//
// Copyright 2004-2010 by OM International
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
using Mono.Unix;
using Ict.Common;
using Ict.Common.Data;
using Ict.Petra.Shared.MFinance.Account.Data;
using Ict.Petra.Client.MFinance.Logic;
using Ict.Petra.Client.App.Core.RemoteObjects;

namespace Ict.Petra.Client.MFinance.Gui.GL
{
    public partial class TUC_GLJournals
    {
        private Int32 FLedgerNumber = -1;
        private Int32 FBatchNumber = -1;


        /// <summary>
        /// load the journals into the grid
        /// </summary>
        /// <param name="ALedgerNumber"></param>
        /// <param name="ABatchNumber"></param>
        public void LoadJournals(Int32 ALedgerNumber, Int32 ABatchNumber)
        {
            if (FBatchNumber != -1)
            {
                GetDataFromControls();
            }

            FLedgerNumber = ALedgerNumber;
            FBatchNumber = ABatchNumber;

            this.btnAdd.Enabled = !FPetraUtilsObject.DetailProtectedMode;
            this.btnRemove.Enabled = !FPetraUtilsObject.DetailProtectedMode;

            FPreviouslySelectedDetailRow = null;

            DataView view = new DataView(FMainDS.AJournal);

            // only load from server if there are no journals loaded yet for this batch
            // otherwise we would overwrite journals that have already been modified
            view.Sort = StringHelper.StrMerge(TTypedDataTable.GetPrimaryKeyColumnStringList(ABatchTable.TableId), ",");

            if (view.Find(new object[] { FLedgerNumber, FBatchNumber }) == -1)
            {
                FMainDS.Merge(TRemote.MFinance.GL.WebConnectors.LoadAJournal(ALedgerNumber, ABatchNumber));
            }

            ShowData();
        }

        /// <summary>
        /// show ledger and batch number
        /// </summary>
        private void ShowDataManual()
        {
            txtLedgerNumber.Text = TFinanceControls.GetLedgerNumberAndName(FLedgerNumber);
            txtBatchNumber.Text = FBatchNumber.ToString();
        }

        private void ShowDetailsManual(AJournalRow ARow)
        {
            if (ARow == null)
            {
                ((TFrmGLBatch)ParentForm).DisableTransactions();
            }
            else
            {
                ((TFrmGLBatch)ParentForm).LoadTransactions(
                    ARow.LedgerNumber,
                    ARow.BatchNumber,
                    ARow.JournalNumber
                    );
            }
        }

        /// <summary>
        /// add a new journal
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void NewRow(System.Object sender, EventArgs e)
        {
            this.CreateNewAJournal();
        }

        /// <summary>
        /// make sure the correct journal number is assigned and the batch.lastJournal is updated
        /// </summary>
        /// <param name="ANewRow"></param>
        public void NewRowManual(ref AJournalRow ANewRow)
        {
            DataView view = new DataView(FMainDS.ABatch);

            view.Sort = StringHelper.StrMerge(TTypedDataTable.GetPrimaryKeyColumnStringList(ABatchTable.TableId), ",");
            ABatchRow row = (ABatchRow)view.FindRows(new object[] { FLedgerNumber, FBatchNumber })[0].Row;
            ANewRow.LedgerNumber = row.LedgerNumber;
            ANewRow.BatchNumber = row.BatchNumber;
            ANewRow.JournalNumber = row.LastJournal + 1;

            // manually created journals are all GL
            ANewRow.SubSystemCode = "GL";
            ANewRow.TransactionTypeCode = "STD";

            // TODO: get base currency of ledger
            ANewRow.TransactionCurrency = "EUR";

            // TODO: get exchange rate from daily or corporate exchange rate table
            // TODO: disable exchange rate if transaction currency equals base currency
            ANewRow.ExchangeRateToBase = 1;
            ANewRow.DateEffective = row.DateEffective;
            ANewRow.JournalPeriod = row.BatchPeriod;
            row.LastJournal++;
        }

        /// initialise some comboboxes
        private void BeforeShowDetailsManual(AJournalRow ARow)
        {
            // SubSystemCode: the user can only select GL, but the system can generate eg. AP journals or GR journals
            this.cmbDetailSubSystemCode.Items.Clear();
            this.cmbDetailSubSystemCode.Items.AddRange(new object[] { ARow.SubSystemCode });

            TFinanceControls.InitialiseTransactionTypeList(ref cmbDetailTransactionTypeCode, FLedgerNumber, ARow.SubSystemCode);
        }

        private void ShowTransactionTab(Object sender, EventArgs e)
        {
            ((TFrmGLBatch)ParentForm).SelectTab(TFrmGLBatch.eGLTabs.Transactions);
        }
    }
}