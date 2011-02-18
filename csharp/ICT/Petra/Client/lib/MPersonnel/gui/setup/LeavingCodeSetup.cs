// auto generated with nant generateWinforms from LeavingCodeSetup.yaml and template windowMaintainCachableTable
//
// DO NOT edit manually, DO NOT edit with the designer
//
//
// DO NOT REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
//
// @Authors:
//       auto generated
//
// Copyright 2004-2011 by OM International
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
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using Ict.Petra.Shared;
using System.Resources;
using System.Collections.Specialized;
using GNU.Gettext;
using Ict.Common;
using Ict.Common.Data;
using Ict.Common.Verification;
using Ict.Petra.Client.App.Core;
using Ict.Petra.Client.App.Core.RemoteObjects;
using Ict.Common.Controls;
using Ict.Petra.Client.CommonForms;
using Ict.Petra.Shared.MPersonnel.Units.Data;

namespace Ict.Petra.Client.MPersonnel.Gui.Setup
{

  /// auto generated: Maintain Leaving Codes
  public partial class TFrmLeavingCodeSetup: System.Windows.Forms.Form, IFrmPetraEdit
  {
    private TFrmPetraEditUtils FPetraUtilsObject;

    private class FMainDS
    {
        public static PtLeavingCodeTable PtLeavingCode;
    }
    /// constructor
    public TFrmLeavingCodeSetup(IntPtr AParentFormHandle) : base()
    {
      Control[] FoundCheckBoxes;

      //
      // Required for Windows Form Designer support
      //
      InitializeComponent();
      #region CATALOGI18N

      // this code has been inserted by GenerateI18N, all changes in this region will be overwritten by GenerateI18N
      this.btnNew.Text = Catalog.GetString("New");
      this.lblDetailLeavingCodeInd.Text = Catalog.GetString("Leaving Code:");
      this.lblDetailLeavingCodeDescr.Text = Catalog.GetString("Description:");
      this.lblDetailUnassignableFlag.Text = Catalog.GetString("Unassignable:");
      this.lblDetailUnassignableDate.Text = Catalog.GetString("Unassignable Date:");
      this.lblDetailDeletableFlag.Text = Catalog.GetString("Deletable:");
      this.tbbSave.ToolTipText = Catalog.GetString("Saves changed data");
      this.tbbSave.Text = Catalog.GetString("&Save");
      this.tbbNew.Text = Catalog.GetString("New Leaving Code");
      this.mniFileSave.ToolTipText = Catalog.GetString("Saves changed data");
      this.mniFileSave.Text = Catalog.GetString("&Save");
      this.mniFilePrint.Text = Catalog.GetString("&Print...");
      this.mniClose.ToolTipText = Catalog.GetString("Closes this window");
      this.mniClose.Text = Catalog.GetString("&Close");
      this.mniFile.Text = Catalog.GetString("&File");
      this.mniEditUndoCurrentField.Text = Catalog.GetString("Undo &Current Field");
      this.mniEditUndoScreen.Text = Catalog.GetString("&Undo Screen");
      this.mniEditFind.Text = Catalog.GetString("&Find...");
      this.mniEdit.Text = Catalog.GetString("&Edit");
      this.mniHelpPetraHelp.Text = Catalog.GetString("&Petra Help");
      this.mniHelpBugReport.Text = Catalog.GetString("Bug &Report");
      this.mniHelpAboutPetra.Text = Catalog.GetString("&About Petra");
      this.mniHelpDevelopmentTeam.Text = Catalog.GetString("&The Development Team...");
      this.mniHelp.Text = Catalog.GetString("&Help");
      this.Text = Catalog.GetString("Maintain Leaving Codes");
      #endregion

      this.txtDetailLeavingCodeInd.Font = TAppSettingsManager.GetDefaultBoldFont();
      this.txtDetailLeavingCodeDescr.Font = TAppSettingsManager.GetDefaultBoldFont();

      FPetraUtilsObject = new TFrmPetraEditUtils(AParentFormHandle, this, stbMain);
            FPetraUtilsObject.SetStatusBarText(txtDetailLeavingCodeInd, Catalog.GetString("Enter the appropriate one letter code."));
      FPetraUtilsObject.SetStatusBarText(txtDetailLeavingCodeDescr, Catalog.GetString("Enter the appropriate description of the leaving code."));
      FPetraUtilsObject.SetStatusBarText(chkDetailUnassignableFlag, Catalog.GetString("Check box if qualification level is no longer assignable."));
      FPetraUtilsObject.SetStatusBarText(dtpDetailUnassignableDate, Catalog.GetString("Date from which the qualification level is unassignable."));
      FPetraUtilsObject.SetStatusBarText(chkDetailDeletableFlag, Catalog.GetString("Indicates if a record can be deleted."));

      /*
       * Automatically disable 'Deletable' CheckBox (it must not get changed by the user because records where the
       * 'Deletable' flag is true are system records that must not be deleted)
       */
      FoundCheckBoxes = this.Controls.Find("chkDetailDeletable", true);

      if (FoundCheckBoxes.Length > 0)
      {
          FoundCheckBoxes[0].Enabled = false;
      }

      LoadDataAndFinishScreenSetup();
    }

    private void TFrmPetra_Activated(object sender, EventArgs e)
    {
        FPetraUtilsObject.TFrmPetra_Activated(sender, e);
    }

    private void TFrmPetra_Load(object sender, EventArgs e)
    {
        FPetraUtilsObject.TFrmPetra_Load(sender, e);
    }

    private void TFrmPetra_Closing(object sender, CancelEventArgs e)
    {
        FPetraUtilsObject.TFrmPetra_Closing(sender, e);
    }

    private void Form_KeyDown(object sender, KeyEventArgs e)
    {
        FPetraUtilsObject.Form_KeyDown(sender, e);
    }

    /// <summary>Loads the data for the screen and finishes the setting up of the screen.</summary>
    /// <returns>void</returns>
    private void LoadDataAndFinishScreenSetup()
    {
      Type DataTableType;

      // Load Data
      FMainDS.PtLeavingCode = new PtLeavingCodeTable();
      DataTable CacheDT = TDataCache.GetCacheableDataTableFromCache("LeavingCodeList", String.Empty, null, out DataTableType);
      FMainDS.PtLeavingCode.Merge(CacheDT);

      grdDetails.Columns.Clear();
      grdDetails.AddTextColumn("Leaving Code", FMainDS.PtLeavingCode.ColumnLeavingCodeInd);
      grdDetails.AddTextColumn("Description", FMainDS.PtLeavingCode.ColumnLeavingCodeDescr);
      grdDetails.AddCheckBoxColumn("Unassignable?", FMainDS.PtLeavingCode.ColumnUnassignableFlag);
      grdDetails.AddDateColumn("Date", FMainDS.PtLeavingCode.ColumnUnassignableDate);
      grdDetails.AddCheckBoxColumn("Deletable", FMainDS.PtLeavingCode.ColumnDeletableFlag);

      FPetraUtilsObject.ActionEnablingEvent += ActionEnabledEvent;

      DataView myDataView = FMainDS.PtLeavingCode.DefaultView;
      myDataView.AllowNew = false;
      grdDetails.DataSource = new DevAge.ComponentModel.BoundDataView(myDataView);

      // Ensure that the Details Panel is disabled if there are no records
      if (FMainDS.PtLeavingCode.Rows.Count == 0)
      {
        ShowDetails(null);
      }

      FPetraUtilsObject.InitActionState();
    }

    private void TFrmPetra_Closed(object sender, EventArgs e)
    {
        // TODO? Save Window position

    }

    /// automatically generated, create a new record of PtLeavingCode and display on the edit screen
    /// we create the table locally, no dataset
    public bool CreateNewPtLeavingCode()
    {
        PtLeavingCodeRow NewRow = FMainDS.PtLeavingCode.NewRowTyped();
        NewRowManual(ref NewRow);
        FMainDS.PtLeavingCode.Rows.Add(NewRow);

        FPetraUtilsObject.SetChangedFlag();

        grdDetails.DataSource = new DevAge.ComponentModel.BoundDataView(FMainDS.PtLeavingCode.DefaultView);
        grdDetails.Refresh();
        SelectDetailRowByDataTableIndex(FMainDS.PtLeavingCode.Rows.Count - 1);

        return true;
    }

    private void SelectDetailRowByDataTableIndex(Int32 ARowNumberInTable)
    {
        Int32 RowNumberGrid = -1;
        for (int Counter = 0; Counter < grdDetails.DataSource.Count; Counter++)
        {
            bool found = true;
            foreach (DataColumn myColumn in FMainDS.PtLeavingCode.PrimaryKey)
            {
                string value1 = FMainDS.PtLeavingCode.Rows[ARowNumberInTable][myColumn].ToString();
                string value2 = (grdDetails.DataSource as DevAge.ComponentModel.BoundDataView).DataView[Counter][myColumn.Ordinal].ToString();
                if (value1 != value2)
                {
                    found = false;
                }
            }
            if (found)
            {
                RowNumberGrid = Counter + 1;
            }
        }
        grdDetails.Selection.ResetSelection(false);
        grdDetails.Selection.SelectRow(RowNumberGrid, true);
        // scroll to the row
        grdDetails.ShowCell(new SourceGrid.Position(RowNumberGrid, 0), true);

        FocusedRowChanged(this, new SourceGrid.RowEventArgs(RowNumberGrid));
    }

    /// return the selected row
    private PtLeavingCodeRow GetSelectedDetailRow()
    {
        DataRowView[] SelectedGridRow = grdDetails.SelectedDataRowsAsDataRowView;

        if (SelectedGridRow.Length >= 1)
        {
            return (PtLeavingCodeRow)SelectedGridRow[0].Row;
        }

        return null;
    }

    private void SetPrimaryKeyReadOnly(bool AReadOnly)
    {
        txtDetailLeavingCodeInd.ReadOnly = AReadOnly;
    }

    private void ShowDetails(PtLeavingCodeRow ARow)
    {
        FPetraUtilsObject.DisableDataChangedEvent();
        if (ARow == null)
        {
            pnlDetails.Enabled = false;
        }
        else
        {
            FPreviouslySelectedDetailRow = ARow;
            txtDetailLeavingCodeInd.Text = ARow.LeavingCodeInd;
            txtDetailLeavingCodeInd.ReadOnly = (ARow.RowState != DataRowState.Added);
            txtDetailLeavingCodeDescr.Text = ARow.LeavingCodeDescr;
            chkDetailUnassignableFlag.Checked = ARow.UnassignableFlag;
            if (ARow.IsUnassignableDateNull())
            {
                dtpDetailUnassignableDate.Date = null;
            }
            else
            {
                dtpDetailUnassignableDate.Date = ARow.UnassignableDate;
            }
            if (ARow.IsDeletableFlagNull())
            {
                chkDetailDeletableFlag.Checked = false;
            }
            else
            {
                chkDetailDeletableFlag.Checked = ARow.DeletableFlag;
            }
            pnlDetails.Enabled = !FPetraUtilsObject.DetailProtectedMode;
        }
        FPetraUtilsObject.EnableDataChangedEvent();
    }

    private PtLeavingCodeRow FPreviouslySelectedDetailRow = null;
    private void FocusedRowChanged(System.Object sender, SourceGrid.RowEventArgs e)
    {
        // get the details from the previously selected row
        if (FPreviouslySelectedDetailRow != null)
        {
            GetDetailsFromControls(FPreviouslySelectedDetailRow);
        }
        // display the details of the currently selected row
        FPreviouslySelectedDetailRow = GetSelectedDetailRow();
        ShowDetails(FPreviouslySelectedDetailRow);
        pnlDetails.Enabled = true;
    }

    private void GetDetailsFromControls(PtLeavingCodeRow ARow)
    {
        if (ARow != null)
        {
            ARow.LeavingCodeInd = txtDetailLeavingCodeInd.Text;
            ARow.LeavingCodeDescr = txtDetailLeavingCodeDescr.Text;
            ARow.UnassignableFlag = chkDetailUnassignableFlag.Checked;
            if (dtpDetailUnassignableDate.Date == null)
            {
                ARow.SetUnassignableDateNull();
            }
            else
            {
                ARow.UnassignableDate = dtpDetailUnassignableDate.Date.Value;
            }
            ARow.DeletableFlag = chkDetailDeletableFlag.Checked;
        }
    }

#region Implement interface functions

    /// auto generated
    public void RunOnceOnActivation()
    {
        RunOnceOnActivationManual();
    }

    /// <summary>
    /// Adds event handlers for the appropiate onChange event to call a central procedure
    /// </summary>
    public void HookupAllControls()
    {
    }

    /// auto generated
    public void HookupAllInContainer(Control container)
    {
        FPetraUtilsObject.HookupAllInContainer(container);
    }

    /// auto generated
    public bool CanClose()
    {
        return FPetraUtilsObject.CanClose();
    }

    /// auto generated
    public TFrmPetraUtils GetPetraUtilsObject()
    {
        return (TFrmPetraUtils)FPetraUtilsObject;
    }

    /// auto generated
    public void FileSave(object sender, EventArgs e)
    {
        SaveChanges();
    }

    /// <summary>
    /// save the changes on the screen
    /// </summary>
    /// <returns></returns>
    public bool SaveChanges()
    {
        FPetraUtilsObject.OnDataSavingStart(this, new System.EventArgs());

//TODO?  still needed?      FMainDS.AApDocument.Rows[0].BeginEdit();
        GetDetailsFromControls(FPreviouslySelectedDetailRow);

        // TODO: verification

        if (FPetraUtilsObject.VerificationResultCollection.Count == 0)
        {
            foreach (DataRow InspectDR in FMainDS.PtLeavingCode.Rows)
            {
                InspectDR.EndEdit();
            }

            if (!FPetraUtilsObject.HasChanges)
            {
                return true;
            }
            else
            {
                FPetraUtilsObject.WriteToStatusBar("Saving data...");
                this.Cursor = Cursors.WaitCursor;

                TSubmitChangesResult SubmissionResult;
                TVerificationResultCollection VerificationResult;

                Ict.Common.Data.TTypedDataTable SubmitDT = FMainDS.PtLeavingCode.GetChangesTyped();

                if (SubmitDT == null)
                {
                    // There is nothing to be saved.
                    // Update UI
                    FPetraUtilsObject.WriteToStatusBar(Catalog.GetString("There is nothing to be saved."));
                    this.Cursor = Cursors.Default;

                    // We don't have unsaved changes anymore
                    FPetraUtilsObject.DisableSaveButton();

                    return true;
                }

                // Submit changes to the PETRAServer
                try
                {
                    SubmissionResult = TDataCache.SaveChangedCacheableDataTableToPetraServer("LeavingCodeList", ref SubmitDT, out VerificationResult);
                }
                catch (System.Net.Sockets.SocketException)
                {
                    FPetraUtilsObject.WriteToStatusBar("Data could not be saved!");
                    this.Cursor = Cursors.Default;
                    MessageBox.Show("The PETRA Server cannot be reached! Data cannot be saved!",
                        "No Server response",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Stop);
                    bool ReturnValue = false;

                    // TODO OnDataSaved(this, new TDataSavedEventArgs(ReturnValue));
                    return ReturnValue;
                }
/* TODO ESecurityDBTableAccessDeniedException
*                  catch (ESecurityDBTableAccessDeniedException Exp)
*                  {
*                      FPetraUtilsObject.WriteToStatusBar("Data could not be saved!");
*                      this.Cursor = Cursors.Default;
*                      // TODO TMessages.MsgSecurityException(Exp, this.GetType());
*                      bool ReturnValue = false;
*                      // TODO OnDataSaved(this, new TDataSavedEventArgs(ReturnValue));
*                      return ReturnValue;
*                  }
*/
                catch (EDBConcurrencyException)
                {
                    FPetraUtilsObject.WriteToStatusBar("Data could not be saved!");
                    this.Cursor = Cursors.Default;

                    // TODO TMessages.MsgDBConcurrencyException(Exp, this.GetType());
                    bool ReturnValue = false;

                    // TODO OnDataSaved(this, new TDataSavedEventArgs(ReturnValue));
                    return ReturnValue;
                }
                catch (Exception exp)
                {
                    FPetraUtilsObject.WriteToStatusBar("Data could not be saved!");
                    this.Cursor = Cursors.Default;
                    TLogging.Log(
                        "An error occured while trying to connect to the PETRA Server!" + Environment.NewLine + exp.ToString(),
                        TLoggingType.ToLogfile);
                    MessageBox.Show(
                        "An error occured while trying to connect to the PETRA Server!" + Environment.NewLine +
                        "For details see the log file: " + TLogging.GetLogFileName(),
                        "Server connection error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Stop);

                    // TODO OnDataSaved(this, new TDataSavedEventArgs(ReturnValue));
                    return false;
                }

                switch (SubmissionResult)
                {
                    case TSubmitChangesResult.scrOK:

                        // Call AcceptChanges to get rid now of any deleted columns before we Merge with the result from the Server
                        FMainDS.PtLeavingCode.AcceptChanges();

                        // Merge back with data from the Server (eg. for getting Sequence values)
                        FMainDS.PtLeavingCode.Merge(SubmitDT, false);

                        // need to accept the new modification ID
                        FMainDS.PtLeavingCode.AcceptChanges();

                        // Update UI
                        FPetraUtilsObject.WriteToStatusBar("Data successfully saved.");
                        this.Cursor = Cursors.Default;

                        // TODO EnableSave(false);

                        // We don't have unsaved changes anymore
                        FPetraUtilsObject.DisableSaveButton();

                        SetPrimaryKeyReadOnly(true);

                        // TODO OnDataSaved(this, new TDataSavedEventArgs(ReturnValue));
                        return true;

                    case TSubmitChangesResult.scrError:

                        // TODO scrError
                        this.Cursor = Cursors.Default;
                        break;

                    case TSubmitChangesResult.scrNothingToBeSaved:

                        // TODO scrNothingToBeSaved
                        this.Cursor = Cursors.Default;
                        return true;

                    case TSubmitChangesResult.scrInfoNeeded:

                        // TODO scrInfoNeeded
                        this.Cursor = Cursors.Default;
                        break;
                }
            }
        }

        return false;
    }

#endregion

#region Action Handling

    /// auto generated
    public void ActionEnabledEvent(object sender, ActionEventArgs e)
    {
        if (e.ActionName == "actNew")
        {
            btnNew.Enabled = e.Enabled;
            tbbNew.Enabled = e.Enabled;
        }
        if (e.ActionName == "actSave")
        {
            tbbSave.Enabled = e.Enabled;
            mniFileSave.Enabled = e.Enabled;
        }
        if (e.ActionName == "actClose")
        {
            mniClose.Enabled = e.Enabled;
        }
        mniFilePrint.Enabled = false;
        mniEditUndoCurrentField.Enabled = false;
        mniEditUndoScreen.Enabled = false;
        mniEditFind.Enabled = false;
        mniHelpPetraHelp.Enabled = false;
        mniHelpBugReport.Enabled = false;
        mniHelpAboutPetra.Enabled = false;
        mniHelpDevelopmentTeam.Enabled = false;
    }

    /// auto generated
    protected void actClose(object sender, EventArgs e)
    {
        FPetraUtilsObject.ExecuteAction(eActionId.eClose);
    }

#endregion
  }
}