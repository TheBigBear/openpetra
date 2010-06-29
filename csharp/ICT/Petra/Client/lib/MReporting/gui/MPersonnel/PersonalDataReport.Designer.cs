// auto generated with nant generateWinforms from PersonalDataReport.yaml
//
// DO NOT edit manually, DO NOT edit with the designer
//
//
// DO NOT REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
//
// @Authors:
//       auto generated
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
using System.Windows.Forms;
using Mono.Unix;
using Ict.Common.Controls;
using Ict.Petra.Client.CommonControls;

namespace Ict.Petra.Client.MReporting.Gui.MPersonnel
{
    partial class TFrmPersonalDataReport
    {
        /// <summary>
        /// Designer variable used to keep track of non-visual components.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Disposes resources used by the form.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// This method is required for Windows Forms designer support.
        /// Do not change the method contents inside the source code editor. The Forms designer might
        /// not be able to load this method if it was changed manually.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TFrmPersonalDataReport));

            this.tabReportSettings = new Ict.Common.Controls.TTabVersatile();
            this.tpgGeneralSettings = new System.Windows.Forms.TabPage();
            this.ucoPartnerSelection = new Ict.Petra.Client.MReporting.Gui.TFrmUC_PartnerSelection();
            this.tpgReportDetails = new System.Windows.Forms.TabPage();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.grpReportDetails = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.chkPersonalInfo = new System.Windows.Forms.CheckBox();
            this.chkLocalPartnerData = new System.Windows.Forms.CheckBox();
            this.chkLocalPersonnelData = new System.Windows.Forms.CheckBox();
            this.chkJobAssignments = new System.Windows.Forms.CheckBox();
            this.chkCommitments = new System.Windows.Forms.CheckBox();
            this.chkPassport = new System.Windows.Forms.CheckBox();
            this.chkPersonalDocuments = new System.Windows.Forms.CheckBox();
            this.chkSpecialNeeds = new System.Windows.Forms.CheckBox();
            this.chkPersonalBudget = new System.Windows.Forms.CheckBox();
            this.chkVisions = new System.Windows.Forms.CheckBox();
            this.chkQualifications = new System.Windows.Forms.CheckBox();
            this.chkAbilities = new System.Windows.Forms.CheckBox();
            this.chkLanguages = new System.Windows.Forms.CheckBox();
            this.chkPreviousExperiences = new System.Windows.Forms.CheckBox();
            this.chkValuableItems = new System.Windows.Forms.CheckBox();
            this.tbrMain = new System.Windows.Forms.ToolStrip();
            this.tbbGenerateReport = new System.Windows.Forms.ToolStripButton();
            this.tbbSaveSettings = new System.Windows.Forms.ToolStripButton();
            this.tbbSaveSettingsAs = new System.Windows.Forms.ToolStripButton();
            this.tbbLoadSettingsDialog = new System.Windows.Forms.ToolStripButton();
            this.mnuMain = new System.Windows.Forms.MenuStrip();
            this.mniFile = new System.Windows.Forms.ToolStripMenuItem();
            this.mniLoadSettings = new System.Windows.Forms.ToolStripMenuItem();
            this.mniLoadSettingsDialog = new System.Windows.Forms.ToolStripMenuItem();
            this.mniSeparator0 = new System.Windows.Forms.ToolStripSeparator();
            this.mniLoadSettings1 = new System.Windows.Forms.ToolStripMenuItem();
            this.mniLoadSettings2 = new System.Windows.Forms.ToolStripMenuItem();
            this.mniLoadSettings3 = new System.Windows.Forms.ToolStripMenuItem();
            this.mniLoadSettings4 = new System.Windows.Forms.ToolStripMenuItem();
            this.mniLoadSettings5 = new System.Windows.Forms.ToolStripMenuItem();
            this.mniSaveSettings = new System.Windows.Forms.ToolStripMenuItem();
            this.mniSaveSettingsAs = new System.Windows.Forms.ToolStripMenuItem();
            this.mniMaintainSettings = new System.Windows.Forms.ToolStripMenuItem();
            this.mniSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.mniWrapColumn = new System.Windows.Forms.ToolStripMenuItem();
            this.mniSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.mniGenerateReport = new System.Windows.Forms.ToolStripMenuItem();
            this.mniSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.mniClose = new System.Windows.Forms.ToolStripMenuItem();
            this.mniHelp = new System.Windows.Forms.ToolStripMenuItem();
            this.mniHelpPetraHelp = new System.Windows.Forms.ToolStripMenuItem();
            this.mniSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.mniHelpBugReport = new System.Windows.Forms.ToolStripMenuItem();
            this.mniSeparator5 = new System.Windows.Forms.ToolStripSeparator();
            this.mniHelpAboutPetra = new System.Windows.Forms.ToolStripMenuItem();
            this.mniHelpDevelopmentTeam = new System.Windows.Forms.ToolStripMenuItem();
            this.stbMain = new Ict.Common.Controls.TExtStatusBarHelp();

            this.tabReportSettings.SuspendLayout();
            this.tpgGeneralSettings.SuspendLayout();
            this.tpgReportDetails.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.grpReportDetails.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.tbrMain.SuspendLayout();
            this.mnuMain.SuspendLayout();
            this.stbMain.SuspendLayout();

            //
            // tpgGeneralSettings
            //
            this.tpgGeneralSettings.Location = new System.Drawing.Point(2,2);
            this.tpgGeneralSettings.Name = "tpgGeneralSettings";
            this.tpgGeneralSettings.AutoSize = true;
            this.tpgGeneralSettings.Controls.Add(this.ucoPartnerSelection);
            //
            // ucoPartnerSelection
            //
            this.ucoPartnerSelection.Name = "ucoPartnerSelection";
            this.ucoPartnerSelection.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tpgGeneralSettings.Text = "General Settings";
            this.tpgGeneralSettings.Dock = System.Windows.Forms.DockStyle.Fill;
            //
            // tpgReportDetails
            //
            this.tpgReportDetails.Location = new System.Drawing.Point(2,2);
            this.tpgReportDetails.Name = "tpgReportDetails";
            this.tpgReportDetails.AutoSize = true;
            //
            // tableLayoutPanel1
            //
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.AutoSize = true;
            this.tpgReportDetails.Controls.Add(this.tableLayoutPanel1);
            //
            // grpReportDetails
            //
            this.grpReportDetails.Location = new System.Drawing.Point(2,2);
            this.grpReportDetails.Name = "grpReportDetails";
            this.grpReportDetails.AutoSize = true;
            //
            // tableLayoutPanel2
            //
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.AutoSize = true;
            this.grpReportDetails.Controls.Add(this.tableLayoutPanel2);
            //
            // chkPersonalInfo
            //
            this.chkPersonalInfo.Location = new System.Drawing.Point(2,2);
            this.chkPersonalInfo.Name = "chkPersonalInfo";
            this.chkPersonalInfo.AutoSize = true;
            this.chkPersonalInfo.Text = "Show Personal Info";
            this.chkPersonalInfo.Margin = new System.Windows.Forms.Padding(3, 5, 3, 0);
            //
            // chkLocalPartnerData
            //
            this.chkLocalPartnerData.Location = new System.Drawing.Point(2,2);
            this.chkLocalPartnerData.Name = "chkLocalPartnerData";
            this.chkLocalPartnerData.AutoSize = true;
            this.chkLocalPartnerData.Text = "Show Local Partner Data";
            this.chkLocalPartnerData.Margin = new System.Windows.Forms.Padding(3, 5, 3, 0);
            //
            // chkLocalPersonnelData
            //
            this.chkLocalPersonnelData.Location = new System.Drawing.Point(2,2);
            this.chkLocalPersonnelData.Name = "chkLocalPersonnelData";
            this.chkLocalPersonnelData.AutoSize = true;
            this.chkLocalPersonnelData.Text = "Show Local Personnel Data";
            this.chkLocalPersonnelData.Margin = new System.Windows.Forms.Padding(3, 5, 3, 0);
            //
            // chkJobAssignments
            //
            this.chkJobAssignments.Location = new System.Drawing.Point(2,2);
            this.chkJobAssignments.Name = "chkJobAssignments";
            this.chkJobAssignments.AutoSize = true;
            this.chkJobAssignments.Text = "Show Job Assignments";
            this.chkJobAssignments.Margin = new System.Windows.Forms.Padding(3, 5, 3, 0);
            //
            // chkCommitments
            //
            this.chkCommitments.Location = new System.Drawing.Point(2,2);
            this.chkCommitments.Name = "chkCommitments";
            this.chkCommitments.AutoSize = true;
            this.chkCommitments.Text = "Show Commitments";
            this.chkCommitments.Margin = new System.Windows.Forms.Padding(3, 5, 3, 0);
            //
            // chkPassport
            //
            this.chkPassport.Location = new System.Drawing.Point(2,2);
            this.chkPassport.Name = "chkPassport";
            this.chkPassport.AutoSize = true;
            this.chkPassport.Text = "Show Passport";
            this.chkPassport.Margin = new System.Windows.Forms.Padding(3, 5, 3, 0);
            //
            // chkPersonalDocuments
            //
            this.chkPersonalDocuments.Location = new System.Drawing.Point(2,2);
            this.chkPersonalDocuments.Name = "chkPersonalDocuments";
            this.chkPersonalDocuments.AutoSize = true;
            this.chkPersonalDocuments.Text = "Show Personal Documents";
            this.chkPersonalDocuments.Margin = new System.Windows.Forms.Padding(3, 5, 3, 0);
            //
            // chkSpecialNeeds
            //
            this.chkSpecialNeeds.Location = new System.Drawing.Point(2,2);
            this.chkSpecialNeeds.Name = "chkSpecialNeeds";
            this.chkSpecialNeeds.AutoSize = true;
            this.chkSpecialNeeds.Text = "Show Special Needs";
            this.chkSpecialNeeds.Margin = new System.Windows.Forms.Padding(3, 5, 3, 0);
            //
            // chkPersonalBudget
            //
            this.chkPersonalBudget.Location = new System.Drawing.Point(2,2);
            this.chkPersonalBudget.Name = "chkPersonalBudget";
            this.chkPersonalBudget.AutoSize = true;
            this.chkPersonalBudget.Text = "Show Personal Budget";
            this.chkPersonalBudget.Margin = new System.Windows.Forms.Padding(3, 5, 3, 0);
            //
            // chkVisions
            //
            this.chkVisions.Location = new System.Drawing.Point(2,2);
            this.chkVisions.Name = "chkVisions";
            this.chkVisions.AutoSize = true;
            this.chkVisions.Text = "Show Visions";
            this.chkVisions.Margin = new System.Windows.Forms.Padding(3, 5, 3, 0);
            //
            // chkQualifications
            //
            this.chkQualifications.Location = new System.Drawing.Point(2,2);
            this.chkQualifications.Name = "chkQualifications";
            this.chkQualifications.AutoSize = true;
            this.chkQualifications.Text = "Show Qualifications";
            this.chkQualifications.Margin = new System.Windows.Forms.Padding(3, 5, 3, 0);
            //
            // chkAbilities
            //
            this.chkAbilities.Location = new System.Drawing.Point(2,2);
            this.chkAbilities.Name = "chkAbilities";
            this.chkAbilities.AutoSize = true;
            this.chkAbilities.Text = "Show Abilities";
            this.chkAbilities.Margin = new System.Windows.Forms.Padding(3, 5, 3, 0);
            //
            // chkLanguages
            //
            this.chkLanguages.Location = new System.Drawing.Point(2,2);
            this.chkLanguages.Name = "chkLanguages";
            this.chkLanguages.AutoSize = true;
            this.chkLanguages.Text = "Show Languages";
            this.chkLanguages.Margin = new System.Windows.Forms.Padding(3, 5, 3, 0);
            //
            // chkPreviousExperiences
            //
            this.chkPreviousExperiences.Location = new System.Drawing.Point(2,2);
            this.chkPreviousExperiences.Name = "chkPreviousExperiences";
            this.chkPreviousExperiences.AutoSize = true;
            this.chkPreviousExperiences.Text = "Show Previous Experiences";
            this.chkPreviousExperiences.Margin = new System.Windows.Forms.Padding(3, 5, 3, 0);
            //
            // chkValuableItems
            //
            this.chkValuableItems.Location = new System.Drawing.Point(2,2);
            this.chkValuableItems.Name = "chkValuableItems";
            this.chkValuableItems.AutoSize = true;
            this.chkValuableItems.Text = "Show Valuable Items";
            this.chkValuableItems.Margin = new System.Windows.Forms.Padding(3, 5, 3, 0);
            this.tableLayoutPanel2.ColumnCount = 1;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel2.RowCount = 15;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.Controls.Add(this.chkPersonalInfo, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.chkLocalPartnerData, 0, 1);
            this.tableLayoutPanel2.Controls.Add(this.chkLocalPersonnelData, 0, 2);
            this.tableLayoutPanel2.Controls.Add(this.chkJobAssignments, 0, 3);
            this.tableLayoutPanel2.Controls.Add(this.chkCommitments, 0, 4);
            this.tableLayoutPanel2.Controls.Add(this.chkPassport, 0, 5);
            this.tableLayoutPanel2.Controls.Add(this.chkPersonalDocuments, 0, 6);
            this.tableLayoutPanel2.Controls.Add(this.chkSpecialNeeds, 0, 7);
            this.tableLayoutPanel2.Controls.Add(this.chkPersonalBudget, 0, 8);
            this.tableLayoutPanel2.Controls.Add(this.chkVisions, 0, 9);
            this.tableLayoutPanel2.Controls.Add(this.chkQualifications, 0, 10);
            this.tableLayoutPanel2.Controls.Add(this.chkAbilities, 0, 11);
            this.tableLayoutPanel2.Controls.Add(this.chkLanguages, 0, 12);
            this.tableLayoutPanel2.Controls.Add(this.chkPreviousExperiences, 0, 13);
            this.tableLayoutPanel2.Controls.Add(this.chkValuableItems, 0, 14);
            this.grpReportDetails.Text = "Report Details";
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.Controls.Add(this.grpReportDetails, 0, 0);
            this.tpgReportDetails.Text = "Report Details";
            this.tpgReportDetails.Dock = System.Windows.Forms.DockStyle.Fill;
            //
            // tabReportSettings
            //
            this.tabReportSettings.Name = "tabReportSettings";
            this.tabReportSettings.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabReportSettings.Controls.Add(this.tpgGeneralSettings);
            this.tabReportSettings.Controls.Add(this.tpgReportDetails);
            this.tabReportSettings.DrawMode = System.Windows.Forms.TabDrawMode.OwnerDrawFixed;
            //
            // tbbGenerateReport
            //
            this.tbbGenerateReport.Name = "tbbGenerateReport";
            this.tbbGenerateReport.AutoSize = true;
            this.tbbGenerateReport.Click += new System.EventHandler(this.actGenerateReport);
            this.tbbGenerateReport.Image = ((System.Drawing.Bitmap)resources.GetObject("tbbGenerateReport.Glyph"));
            this.tbbGenerateReport.ToolTipText = "Generate the report";
            this.tbbGenerateReport.Text = "&Generate";
            //
            // tbbSaveSettings
            //
            this.tbbSaveSettings.Name = "tbbSaveSettings";
            this.tbbSaveSettings.AutoSize = true;
            this.tbbSaveSettings.Click += new System.EventHandler(this.actSaveSettings);
            this.tbbSaveSettings.Image = ((System.Drawing.Bitmap)resources.GetObject("tbbSaveSettings.Glyph"));
            this.tbbSaveSettings.Text = "&Save Settings";
            //
            // tbbSaveSettingsAs
            //
            this.tbbSaveSettingsAs.Name = "tbbSaveSettingsAs";
            this.tbbSaveSettingsAs.AutoSize = true;
            this.tbbSaveSettingsAs.Click += new System.EventHandler(this.actSaveSettingsAs);
            this.tbbSaveSettingsAs.Image = ((System.Drawing.Bitmap)resources.GetObject("tbbSaveSettingsAs.Glyph"));
            this.tbbSaveSettingsAs.Text = "Save Settings &As...";
            //
            // tbbLoadSettingsDialog
            //
            this.tbbLoadSettingsDialog.Name = "tbbLoadSettingsDialog";
            this.tbbLoadSettingsDialog.AutoSize = true;
            this.tbbLoadSettingsDialog.Click += new System.EventHandler(this.actLoadSettingsDialog);
            this.tbbLoadSettingsDialog.Text = "&Open...";
            //
            // tbrMain
            //
            this.tbrMain.Name = "tbrMain";
            this.tbrMain.Dock = System.Windows.Forms.DockStyle.Top;
            this.tbrMain.AutoSize = true;
            this.tbrMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                           tbbGenerateReport,
                        tbbSaveSettings,
                        tbbSaveSettingsAs,
                        tbbLoadSettingsDialog});
            //
            // mniLoadSettingsDialog
            //
            this.mniLoadSettingsDialog.Name = "mniLoadSettingsDialog";
            this.mniLoadSettingsDialog.AutoSize = true;
            this.mniLoadSettingsDialog.Click += new System.EventHandler(this.actLoadSettingsDialog);
            this.mniLoadSettingsDialog.Text = "&Open...";
            //
            // mniSeparator0
            //
            this.mniSeparator0.Name = "mniSeparator0";
            this.mniSeparator0.AutoSize = true;
            this.mniSeparator0.Text = "-";
            //
            // mniLoadSettings1
            //
            this.mniLoadSettings1.Name = "mniLoadSettings1";
            this.mniLoadSettings1.AutoSize = true;
            this.mniLoadSettings1.Click += new System.EventHandler(this.actLoadSettings);
            this.mniLoadSettings1.Text = "RecentSettings";
            //
            // mniLoadSettings2
            //
            this.mniLoadSettings2.Name = "mniLoadSettings2";
            this.mniLoadSettings2.AutoSize = true;
            this.mniLoadSettings2.Click += new System.EventHandler(this.actLoadSettings);
            this.mniLoadSettings2.Text = "RecentSettings";
            //
            // mniLoadSettings3
            //
            this.mniLoadSettings3.Name = "mniLoadSettings3";
            this.mniLoadSettings3.AutoSize = true;
            this.mniLoadSettings3.Click += new System.EventHandler(this.actLoadSettings);
            this.mniLoadSettings3.Text = "RecentSettings";
            //
            // mniLoadSettings4
            //
            this.mniLoadSettings4.Name = "mniLoadSettings4";
            this.mniLoadSettings4.AutoSize = true;
            this.mniLoadSettings4.Click += new System.EventHandler(this.actLoadSettings);
            this.mniLoadSettings4.Text = "RecentSettings";
            //
            // mniLoadSettings5
            //
            this.mniLoadSettings5.Name = "mniLoadSettings5";
            this.mniLoadSettings5.AutoSize = true;
            this.mniLoadSettings5.Click += new System.EventHandler(this.actLoadSettings);
            this.mniLoadSettings5.Text = "RecentSettings";
            //
            // mniLoadSettings
            //
            this.mniLoadSettings.Name = "mniLoadSettings";
            this.mniLoadSettings.AutoSize = true;
            this.mniLoadSettings.Click += new System.EventHandler(this.actLoadSettings);
            this.mniLoadSettings.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                           mniLoadSettingsDialog,
                        mniSeparator0,
                        mniLoadSettings1,
                        mniLoadSettings2,
                        mniLoadSettings3,
                        mniLoadSettings4,
                        mniLoadSettings5});
            this.mniLoadSettings.Text = "&Load Settings";
            //
            // mniSaveSettings
            //
            this.mniSaveSettings.Name = "mniSaveSettings";
            this.mniSaveSettings.AutoSize = true;
            this.mniSaveSettings.Click += new System.EventHandler(this.actSaveSettings);
            this.mniSaveSettings.Image = ((System.Drawing.Bitmap)resources.GetObject("mniSaveSettings.Glyph"));
            this.mniSaveSettings.Text = "&Save Settings";
            //
            // mniSaveSettingsAs
            //
            this.mniSaveSettingsAs.Name = "mniSaveSettingsAs";
            this.mniSaveSettingsAs.AutoSize = true;
            this.mniSaveSettingsAs.Click += new System.EventHandler(this.actSaveSettingsAs);
            this.mniSaveSettingsAs.Image = ((System.Drawing.Bitmap)resources.GetObject("mniSaveSettingsAs.Glyph"));
            this.mniSaveSettingsAs.Text = "Save Settings &As...";
            //
            // mniMaintainSettings
            //
            this.mniMaintainSettings.Name = "mniMaintainSettings";
            this.mniMaintainSettings.AutoSize = true;
            this.mniMaintainSettings.Click += new System.EventHandler(this.actMaintainSettings);
            this.mniMaintainSettings.Text = "&Maintain Settings...";
            //
            // mniSeparator1
            //
            this.mniSeparator1.Name = "mniSeparator1";
            this.mniSeparator1.AutoSize = true;
            this.mniSeparator1.Text = "-";
            //
            // mniWrapColumn
            //
            this.mniWrapColumn.Name = "mniWrapColumn";
            this.mniWrapColumn.AutoSize = true;
            this.mniWrapColumn.Click += new System.EventHandler(this.actWrapColumn);
            this.mniWrapColumn.Text = "&Wrap Columns";
            //
            // mniSeparator2
            //
            this.mniSeparator2.Name = "mniSeparator2";
            this.mniSeparator2.AutoSize = true;
            this.mniSeparator2.Text = "-";
            //
            // mniGenerateReport
            //
            this.mniGenerateReport.Name = "mniGenerateReport";
            this.mniGenerateReport.AutoSize = true;
            this.mniGenerateReport.Click += new System.EventHandler(this.actGenerateReport);
            this.mniGenerateReport.Image = ((System.Drawing.Bitmap)resources.GetObject("mniGenerateReport.Glyph"));
            this.mniGenerateReport.ToolTipText = "Generate the report";
            this.mniGenerateReport.Text = "&Generate";
            //
            // mniSeparator3
            //
            this.mniSeparator3.Name = "mniSeparator3";
            this.mniSeparator3.AutoSize = true;
            this.mniSeparator3.Text = "-";
            //
            // mniClose
            //
            this.mniClose.Name = "mniClose";
            this.mniClose.AutoSize = true;
            this.mniClose.Click += new System.EventHandler(this.actClose);
            this.mniClose.Image = ((System.Drawing.Bitmap)resources.GetObject("mniClose.Glyph"));
            this.mniClose.ToolTipText = "Closes this window";
            this.mniClose.Text = "&Close";
            //
            // mniFile
            //
            this.mniFile.Name = "mniFile";
            this.mniFile.AutoSize = true;
            this.mniFile.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                           mniLoadSettings,
                        mniSaveSettings,
                        mniSaveSettingsAs,
                        mniMaintainSettings,
                        mniSeparator1,
                        mniWrapColumn,
                        mniSeparator2,
                        mniGenerateReport,
                        mniSeparator3,
                        mniClose});
            this.mniFile.Text = "&File";
            //
            // mniHelpPetraHelp
            //
            this.mniHelpPetraHelp.Name = "mniHelpPetraHelp";
            this.mniHelpPetraHelp.AutoSize = true;
            this.mniHelpPetraHelp.Text = "&Petra Help";
            //
            // mniSeparator4
            //
            this.mniSeparator4.Name = "mniSeparator4";
            this.mniSeparator4.AutoSize = true;
            this.mniSeparator4.Text = "-";
            //
            // mniHelpBugReport
            //
            this.mniHelpBugReport.Name = "mniHelpBugReport";
            this.mniHelpBugReport.AutoSize = true;
            this.mniHelpBugReport.Text = "Bug &Report";
            //
            // mniSeparator5
            //
            this.mniSeparator5.Name = "mniSeparator5";
            this.mniSeparator5.AutoSize = true;
            this.mniSeparator5.Text = "-";
            //
            // mniHelpAboutPetra
            //
            this.mniHelpAboutPetra.Name = "mniHelpAboutPetra";
            this.mniHelpAboutPetra.AutoSize = true;
            this.mniHelpAboutPetra.Text = "&About Petra";
            //
            // mniHelpDevelopmentTeam
            //
            this.mniHelpDevelopmentTeam.Name = "mniHelpDevelopmentTeam";
            this.mniHelpDevelopmentTeam.AutoSize = true;
            this.mniHelpDevelopmentTeam.Text = "&The Development Team...";
            //
            // mniHelp
            //
            this.mniHelp.Name = "mniHelp";
            this.mniHelp.AutoSize = true;
            this.mniHelp.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                           mniHelpPetraHelp,
                        mniSeparator4,
                        mniHelpBugReport,
                        mniSeparator5,
                        mniHelpAboutPetra,
                        mniHelpDevelopmentTeam});
            this.mniHelp.Text = "&Help";
            //
            // mnuMain
            //
            this.mnuMain.Name = "mnuMain";
            this.mnuMain.Dock = System.Windows.Forms.DockStyle.Top;
            this.mnuMain.AutoSize = true;
            this.mnuMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                           mniFile,
                        mniHelp});
            //
            // stbMain
            //
            this.stbMain.Name = "stbMain";
            this.stbMain.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.stbMain.AutoSize = true;

            //
            // TFrmPersonalDataReport
            //
            this.Font = new System.Drawing.Font("Verdana", 8.25f);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;

            this.ClientSize = new System.Drawing.Size(700, 500);

            this.Controls.Add(this.tabReportSettings);
            this.Controls.Add(this.tbrMain);
            this.Controls.Add(this.mnuMain);
            this.MainMenuStrip = mnuMain;
            this.Controls.Add(this.stbMain);
            this.Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");

            this.Name = "TFrmPersonalDataReport";
            this.Text = "Personal Data Report";

            this.Activated += new System.EventHandler(this.TFrmPetra_Activated);
            this.Load += new System.EventHandler(this.TFrmPetra_Load);
            this.Closing += new System.ComponentModel.CancelEventHandler(this.TFrmPetra_Closing);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Form_KeyDown);
            this.Closed += new System.EventHandler(this.TFrmPetra_Closed);

            this.stbMain.ResumeLayout(false);
            this.mnuMain.ResumeLayout(false);
            this.tbrMain.ResumeLayout(false);
            this.tableLayoutPanel2.ResumeLayout(false);
            this.grpReportDetails.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tpgReportDetails.ResumeLayout(false);
            this.tpgGeneralSettings.ResumeLayout(false);
            this.tabReportSettings.ResumeLayout(false);

            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private Ict.Common.Controls.TTabVersatile tabReportSettings;
        private System.Windows.Forms.TabPage tpgGeneralSettings;
        private Ict.Petra.Client.MReporting.Gui.TFrmUC_PartnerSelection ucoPartnerSelection;
        private System.Windows.Forms.TabPage tpgReportDetails;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.GroupBox grpReportDetails;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.CheckBox chkPersonalInfo;
        private System.Windows.Forms.CheckBox chkLocalPartnerData;
        private System.Windows.Forms.CheckBox chkLocalPersonnelData;
        private System.Windows.Forms.CheckBox chkJobAssignments;
        private System.Windows.Forms.CheckBox chkCommitments;
        private System.Windows.Forms.CheckBox chkPassport;
        private System.Windows.Forms.CheckBox chkPersonalDocuments;
        private System.Windows.Forms.CheckBox chkSpecialNeeds;
        private System.Windows.Forms.CheckBox chkPersonalBudget;
        private System.Windows.Forms.CheckBox chkVisions;
        private System.Windows.Forms.CheckBox chkQualifications;
        private System.Windows.Forms.CheckBox chkAbilities;
        private System.Windows.Forms.CheckBox chkLanguages;
        private System.Windows.Forms.CheckBox chkPreviousExperiences;
        private System.Windows.Forms.CheckBox chkValuableItems;
        private System.Windows.Forms.ToolStrip tbrMain;
        private System.Windows.Forms.ToolStripButton tbbGenerateReport;
        private System.Windows.Forms.ToolStripButton tbbSaveSettings;
        private System.Windows.Forms.ToolStripButton tbbSaveSettingsAs;
        private System.Windows.Forms.ToolStripButton tbbLoadSettingsDialog;
        private System.Windows.Forms.MenuStrip mnuMain;
        private System.Windows.Forms.ToolStripMenuItem mniFile;
        private System.Windows.Forms.ToolStripMenuItem mniLoadSettings;
        private System.Windows.Forms.ToolStripMenuItem mniLoadSettingsDialog;
        private System.Windows.Forms.ToolStripSeparator mniSeparator0;
        private System.Windows.Forms.ToolStripMenuItem mniLoadSettings1;
        private System.Windows.Forms.ToolStripMenuItem mniLoadSettings2;
        private System.Windows.Forms.ToolStripMenuItem mniLoadSettings3;
        private System.Windows.Forms.ToolStripMenuItem mniLoadSettings4;
        private System.Windows.Forms.ToolStripMenuItem mniLoadSettings5;
        private System.Windows.Forms.ToolStripMenuItem mniSaveSettings;
        private System.Windows.Forms.ToolStripMenuItem mniSaveSettingsAs;
        private System.Windows.Forms.ToolStripMenuItem mniMaintainSettings;
        private System.Windows.Forms.ToolStripSeparator mniSeparator1;
        private System.Windows.Forms.ToolStripMenuItem mniWrapColumn;
        private System.Windows.Forms.ToolStripSeparator mniSeparator2;
        private System.Windows.Forms.ToolStripMenuItem mniGenerateReport;
        private System.Windows.Forms.ToolStripSeparator mniSeparator3;
        private System.Windows.Forms.ToolStripMenuItem mniClose;
        private System.Windows.Forms.ToolStripMenuItem mniHelp;
        private System.Windows.Forms.ToolStripMenuItem mniHelpPetraHelp;
        private System.Windows.Forms.ToolStripSeparator mniSeparator4;
        private System.Windows.Forms.ToolStripMenuItem mniHelpBugReport;
        private System.Windows.Forms.ToolStripSeparator mniSeparator5;
        private System.Windows.Forms.ToolStripMenuItem mniHelpAboutPetra;
        private System.Windows.Forms.ToolStripMenuItem mniHelpDevelopmentTeam;
        private Ict.Common.Controls.TExtStatusBarHelp stbMain;
    }
}
