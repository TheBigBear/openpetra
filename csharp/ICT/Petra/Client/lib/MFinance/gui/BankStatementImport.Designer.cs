/* auto generated with nant generateWinforms from BankStatementImport.yaml
 *
 * DO NOT edit manually, DO NOT edit with the designer
 * use a user control if you need to modify the screen content
 *
 */
/*************************************************************************
 *
 * DO NOT REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
 *
 * @Authors:
 *       auto generated
 *
 * Copyright 2004-2009 by OM International
 *
 * This file is part of OpenPetra.org.
 *
 * OpenPetra.org is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * OpenPetra.org is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with OpenPetra.org.  If not, see <http://www.gnu.org/licenses/>.
 *
 ************************************************************************/
using System;
using System.Windows.Forms;
using Mono.Unix;
using Ict.Common.Controls;
using Ict.Petra.Client.CommonControls;

namespace Ict.Petra.Client.MFinance.Gui
{
    partial class TFrmBankStatementImport
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TFrmBankStatementImport));

            this.pnlContent = new System.Windows.Forms.Panel();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.pnlStatementInfo = new System.Windows.Forms.Panel();
            this.tabTransactions = new Ict.Common.Controls.TTabVersatile();
            this.tpgAll = new System.Windows.Forms.TabPage();
            this.grdAllTransactions = new Ict.Common.Controls.TSgrdDataGridPaged();
            this.tpgUnmatched = new System.Windows.Forms.TabPage();
            this.tpgGifts = new System.Windows.Forms.TabPage();
            this.tpgGL = new System.Windows.Forms.TabPage();
            this.tbrMain = new System.Windows.Forms.ToolStrip();
            this.tbbImportNewStatement = new System.Windows.Forms.ToolStripButton();
            this.tbbSeparator0 = new System.Windows.Forms.ToolStripSeparator();
            this.tbcSelectStatement = new System.Windows.Forms.ToolStripComboBox();
            this.mnuMain = new System.Windows.Forms.MenuStrip();
            this.mniFile = new System.Windows.Forms.ToolStripMenuItem();
            this.mniImportNewStatement = new System.Windows.Forms.ToolStripMenuItem();
            this.mniClose = new System.Windows.Forms.ToolStripMenuItem();
            this.mniHelp = new System.Windows.Forms.ToolStripMenuItem();
            this.mniHelpPetraHelp = new System.Windows.Forms.ToolStripMenuItem();
            this.mniSeparator0 = new System.Windows.Forms.ToolStripSeparator();
            this.mniHelpBugReport = new System.Windows.Forms.ToolStripMenuItem();
            this.mniSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.mniHelpAboutPetra = new System.Windows.Forms.ToolStripMenuItem();
            this.mniHelpDevelopmentTeam = new System.Windows.Forms.ToolStripMenuItem();
            this.stbMain = new Ict.Common.Controls.TExtStatusBarHelp();

            this.pnlContent.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.pnlStatementInfo.SuspendLayout();
            this.tabTransactions.SuspendLayout();
            this.tpgAll.SuspendLayout();
            this.tpgUnmatched.SuspendLayout();
            this.tpgGifts.SuspendLayout();
            this.tpgGL.SuspendLayout();
            this.tbrMain.SuspendLayout();
            this.mnuMain.SuspendLayout();
            this.stbMain.SuspendLayout();

            //
            // pnlContent
            //
            this.pnlContent.Name = "pnlContent";
            this.pnlContent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlContent.AutoSize = true;
            //
            // tableLayoutPanel1
            //
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.AutoSize = true;
            this.pnlContent.Controls.Add(this.tableLayoutPanel1);
            //
            // pnlStatementInfo
            //
            this.pnlStatementInfo.Location = new System.Drawing.Point(2,2);
            this.pnlStatementInfo.Name = "pnlStatementInfo";
            this.pnlStatementInfo.AutoSize = true;
            //
            // tpgAll
            //
            this.tpgAll.Location = new System.Drawing.Point(2,2);
            this.tpgAll.Name = "tpgAll";
            this.tpgAll.AutoSize = true;
            this.tpgAll.Controls.Add(this.grdAllTransactions);
            //
            // grdAllTransactions
            //
            this.grdAllTransactions.Name = "grdAllTransactions";
            this.grdAllTransactions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tpgAll.Text = "All";
            this.tpgAll.Dock = System.Windows.Forms.DockStyle.Fill;
            //
            // tpgUnmatched
            //
            this.tpgUnmatched.Location = new System.Drawing.Point(2,2);
            this.tpgUnmatched.Name = "tpgUnmatched";
            this.tpgUnmatched.AutoSize = true;
            this.tpgUnmatched.Text = "Unmatched";
            this.tpgUnmatched.Dock = System.Windows.Forms.DockStyle.Fill;
            //
            // tpgGifts
            //
            this.tpgGifts.Location = new System.Drawing.Point(2,2);
            this.tpgGifts.Name = "tpgGifts";
            this.tpgGifts.AutoSize = true;
            this.tpgGifts.Text = "Gifts";
            this.tpgGifts.Dock = System.Windows.Forms.DockStyle.Fill;
            //
            // tpgGL
            //
            this.tpgGL.Location = new System.Drawing.Point(2,2);
            this.tpgGL.Name = "tpgGL";
            this.tpgGL.AutoSize = true;
            this.tpgGL.Text = "GL";
            this.tpgGL.Dock = System.Windows.Forms.DockStyle.Fill;
            //
            // tabTransactions
            //
            this.tabTransactions.Name = "tabTransactions";
            this.tabTransactions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabTransactions.Controls.Add(this.tpgAll);
            this.tabTransactions.Controls.Add(this.tpgUnmatched);
            this.tabTransactions.Controls.Add(this.tpgGifts);
            this.tabTransactions.Controls.Add(this.tpgGL);
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.Controls.Add(this.pnlStatementInfo, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.tabTransactions, 0, 1);
            //
            // tbbImportNewStatement
            //
            this.tbbImportNewStatement.Name = "tbbImportNewStatement";
            this.tbbImportNewStatement.AutoSize = true;
            this.tbbImportNewStatement.Click += new System.EventHandler(this.ImportNewStatement);
            this.tbbImportNewStatement.Text = "&Import new statement";
            //
            // tbbSeparator0
            //
            this.tbbSeparator0.Name = "tbbSeparator0";
            this.tbbSeparator0.AutoSize = true;
            this.tbbSeparator0.Text = "Separator";
            //
            // tbcSelectStatement
            //
            this.tbcSelectStatement.Name = "tbcSelectStatement";
            this.tbcSelectStatement.AutoSize = true;
            //
            // tbrMain
            //
            this.tbrMain.Name = "tbrMain";
            this.tbrMain.Dock = System.Windows.Forms.DockStyle.Top;
            this.tbrMain.AutoSize = true;
            this.tbrMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                           tbbImportNewStatement,
                        tbbSeparator0,
                        tbcSelectStatement});
            //
            // mniImportNewStatement
            //
            this.mniImportNewStatement.Name = "mniImportNewStatement";
            this.mniImportNewStatement.AutoSize = true;
            this.mniImportNewStatement.Click += new System.EventHandler(this.ImportNewStatement);
            this.mniImportNewStatement.Text = "&Import new statement";
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
                           mniImportNewStatement,
                        mniClose});
            this.mniFile.Text = "&File";
            //
            // mniHelpPetraHelp
            //
            this.mniHelpPetraHelp.Name = "mniHelpPetraHelp";
            this.mniHelpPetraHelp.AutoSize = true;
            this.mniHelpPetraHelp.Text = "&Petra Help";
            //
            // mniSeparator0
            //
            this.mniSeparator0.Name = "mniSeparator0";
            this.mniSeparator0.AutoSize = true;
            this.mniSeparator0.Text = "-";
            //
            // mniHelpBugReport
            //
            this.mniHelpBugReport.Name = "mniHelpBugReport";
            this.mniHelpBugReport.AutoSize = true;
            this.mniHelpBugReport.Text = "Bug &Report";
            //
            // mniSeparator1
            //
            this.mniSeparator1.Name = "mniSeparator1";
            this.mniSeparator1.AutoSize = true;
            this.mniSeparator1.Text = "-";
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
                        mniSeparator0,
                        mniHelpBugReport,
                        mniSeparator1,
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
            // TFrmBankStatementImport
            //
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(754, 623);
            // this.rpsForm.SetRestoreLocation(this, false);  for the moment false, to avoid problems with size
            this.Controls.Add(this.pnlContent);
            this.Controls.Add(this.tbrMain);
            this.Controls.Add(this.mnuMain);
            this.MainMenuStrip = mnuMain;
            this.Controls.Add(this.stbMain);
            this.Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            this.Name = "TFrmBankStatementImport";
            this.Text = "Import Bank Statements";

	        this.Activated += new System.EventHandler(this.TFrmPetra_Activated);
	        this.Load += new System.EventHandler(this.TFrmPetra_Load);
	        this.Closing += new System.ComponentModel.CancelEventHandler(this.TFrmPetra_Closing);
	        this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Form_KeyDown);
	        this.Closed += new System.EventHandler(this.TFrmPetra_Closed);
	
            this.stbMain.ResumeLayout(false);
            this.mnuMain.ResumeLayout(false);
            this.tbrMain.ResumeLayout(false);
            this.tpgGL.ResumeLayout(false);
            this.tpgGifts.ResumeLayout(false);
            this.tpgUnmatched.ResumeLayout(false);
            this.tpgAll.ResumeLayout(false);
            this.tabTransactions.ResumeLayout(false);
            this.pnlStatementInfo.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.pnlContent.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }
        private System.Windows.Forms.Panel pnlContent;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Panel pnlStatementInfo;
        private Ict.Common.Controls.TTabVersatile tabTransactions;
        private System.Windows.Forms.TabPage tpgAll;
        private Ict.Common.Controls.TSgrdDataGridPaged grdAllTransactions;
        private System.Windows.Forms.TabPage tpgUnmatched;
        private System.Windows.Forms.TabPage tpgGifts;
        private System.Windows.Forms.TabPage tpgGL;
        private System.Windows.Forms.ToolStrip tbrMain;
        private System.Windows.Forms.ToolStripButton tbbImportNewStatement;
        private System.Windows.Forms.ToolStripSeparator tbbSeparator0;
        private System.Windows.Forms.ToolStripComboBox tbcSelectStatement;
        private System.Windows.Forms.MenuStrip mnuMain;
        private System.Windows.Forms.ToolStripMenuItem mniFile;
        private System.Windows.Forms.ToolStripMenuItem mniImportNewStatement;
        private System.Windows.Forms.ToolStripMenuItem mniClose;
        private System.Windows.Forms.ToolStripMenuItem mniHelp;
        private System.Windows.Forms.ToolStripMenuItem mniHelpPetraHelp;
        private System.Windows.Forms.ToolStripSeparator mniSeparator0;
        private System.Windows.Forms.ToolStripMenuItem mniHelpBugReport;
        private System.Windows.Forms.ToolStripSeparator mniSeparator1;
        private System.Windows.Forms.ToolStripMenuItem mniHelpAboutPetra;
        private System.Windows.Forms.ToolStripMenuItem mniHelpDevelopmentTeam;
        private Ict.Common.Controls.TExtStatusBarHelp stbMain;
    }
}
