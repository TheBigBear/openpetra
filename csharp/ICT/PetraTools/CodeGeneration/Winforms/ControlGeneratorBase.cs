﻿/*************************************************************************
 *
 * DO NOT REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
 *
 * @Authors:
 *       timop
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
using System.Xml;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Windows.Forms;
using Ict.Tools.CodeGeneration;
using Ict.Tools.DBXML;
using Ict.Common.IO;
using Ict.Common;

namespace Ict.Tools.CodeGeneration.Winforms
{
    public class TControlGenerator : IControlGenerator
    {
        public string FPrefix;
        public string FControlType;
        public bool FAutoSize = false;
        public bool FLocation = true;
        public bool FGenerateLabel = true;
        public bool FAddControlToContainer = true;
        public bool FRequiresChildren = false;
        public Int32 FWidth = 150;
        public Int32 FHeight = 28;

        public static TDataDefinitionStore FPetraXMLStore;

        public TControlGenerator(string APrefix, System.Type AControlType)
        {
            FPrefix = APrefix;
            FControlType = AControlType.ToString();
        }

        public TControlGenerator(string APrefix, string AControlType)
        {
            FPrefix = APrefix;
            FControlType = AControlType;
        }

        /// <summary>
        /// should this control only be created if there are children controls? eg toolbar
        /// </summary>
        public bool RequiresChildren
        {
            get
            {
                return FRequiresChildren;
            }
        }

        public bool GenerateLabel(TControlDef ctrl)
        {
            if (ctrl.HasAttribute("NoLabel") && (ctrl.GetAttribute("NoLabel").ToLower() == "true"))
            {
                return false;
            }

            return FGenerateLabel;
        }

        public String ControlType
        {
            get
            {
                return FControlType;
            }
            set
            {
                FControlType = value;
            }
        }

        public bool AddControlToContainer
        {
            get
            {
                return FAddControlToContainer;
            }
            set
            {
                AddControlToContainer = value;
            }
        }

        // overwrite for more complicated matches,
        // if the same prefix is used for more than one control type
        // e.g. txt
        public virtual bool ControlFitsNode(XmlNode curNode)
        {
            return curNode.Name.StartsWith(FPrefix);
        }

        public virtual void GenerateDeclaration(IFormWriter writer, TControlDef ctrl)
        {
            string localControlType = ControlType;

            if (ctrl.controlType.Length > 0)
            {
                localControlType = ctrl.controlType;
            }

            writer.Template.AddToCodelet("CONTROLDECLARATION", "private " + localControlType + " " + ctrl.controlName + ";" + Environment.NewLine);
            writer.Template.AddToCodelet("CONTROLCREATION", "this." + ctrl.controlName + " = new " + localControlType + "();" + Environment.NewLine);

            // TODO generate a property that can be accessed from outside
        }

        protected virtual string AssignValue(TControlDef ctrl, string AFieldOrNull, string AFieldTypeDotNet)
        {
            if (AFieldOrNull == null)
            {
                return ctrl.controlName + ".Value = null;";
            }

            return ctrl.controlName + ".Value = " + AFieldOrNull + ";";
        }

        /// <summary>
        /// for coding the transfer of the value from control to dataset
        /// </summary>
        /// <param name="ctrl"></param>
        /// <param name="AFieldTypeDotNet">if this is null, check for the NULL value of the control; otherwise cast the value of the control to the value of the field in the dataset</param>
        /// <returns></returns>
        protected virtual string GetControlValue(TControlDef ctrl, string AFieldTypeDotNet)
        {
            if (AFieldTypeDotNet == null)
            {
                return ctrl.controlName + ".Value == null";
            }

            return ctrl.controlName + ".Value";
        }

        /// <summary>
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="ctrl"></param>
        /// <param name="AEvent">Click or DoubleClick or other</param>
        /// <param name="ActionToPerform"></param>
        public void AssignEventHandlerToControl(IFormWriter writer, TControlDef ctrl, string AEvent, string ActionToPerform)
        {
            if (ActionToPerform.StartsWith("act"))
            {
                TActionHandler ActionHandler = writer.CodeStorage.FActionList[ActionToPerform];

                if (ActionHandler.actionId.Length > 0)
                {
                    // actionId is managed by FPetraUtilsObject
                    // need a special function that wraps calls to FPetraUtilsObject, otherwise problems in designer
                    ActionToPerform = ActionHandler.actionName;
                }
                else if (ActionHandler.actionClick.Length > 0)
                {
                    if (ActionHandler.actionClick.StartsWith("FPetraUtilsObject"))
                    {
                        // need a special function that wraps calls to FPetraUtilsObject, otherwise problems in designer
                        ActionToPerform = ActionHandler.actionName;
                    }
                    else
                    {
                        // direct call
                        ActionToPerform = ActionHandler.actionClick;
                    }
                }
                else
                {
                    ActionToPerform = "";
                }
            }
            else
            {
                // direct call: use ActionToPerform
            }

            if (ActionToPerform.Length > 0)
            {
                writer.SetEventHandlerToControl(ctrl.controlName, AEvent, ActionToPerform);
            }
        }

        public void AddToActionEnabledEvent(IFormWriter writer, string ActionCondition, string ControlName)
        {
            writer.Template.AddToCodelet(
                "ENABLEDEPENDINGACTIONS_" + ActionCondition,
                ControlName + ".Enabled = e.Enabled;" + Environment.NewLine);
        }

        public virtual void SetControlProperties(IFormWriter writer, TControlDef ctrl)
        {
            writer.Template.AddToCodelet("CONTROLINITIALISATION",
                "//" + Environment.NewLine + "// " + ctrl.controlName + Environment.NewLine + "//" + Environment.NewLine);

            if (TXMLParser.HasAttribute(ctrl.xmlNode, "Location"))
            {
                // this control has already been there in the designer file, it is not defined in yaml
                writer.SetControlProperty(ctrl, "Location");
                writer.SetControlProperty(ctrl.controlName, "Name", "\"" + ctrl.controlName + "\"");
                writer.SetControlProperty(ctrl, "Size");
                writer.SetControlProperty(ctrl, "TabIndex");
                writer.SetControlProperty(ctrl, "Dock");
                return;
            }

            if (FLocation && !TXMLParser.HasAttribute(ctrl.xmlNode, "Dock"))
            {
                writer.SetControlProperty(ctrl.controlName, "Location", "new System.Drawing.Point(2,2)");
            }

            writer.SetControlProperty(ctrl.controlName, "Name", "\"" + ctrl.controlName + "\"");

            if (ctrl.HasAttribute("Align"))
            {
                if (ctrl.GetAttribute("Align").ToLower() == "right")
                {
                    writer.SetControlProperty(ctrl.controlName,
                        "Anchor",
                        "((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)))");
                }
            }

            if (TYml2Xml.HasAttribute(ctrl.xmlNode, "Dock"))
            {
                writer.SetControlProperty(ctrl, "Dock");

                if (TYml2Xml.GetAttribute(ctrl.xmlNode, "Dock").ToLower() != "fill")
                {
                    writer.SetControlProperty(ctrl.controlName, "AutoSize", "true");
                }
            }
            else if (FAutoSize)
            {
                writer.SetControlProperty(ctrl.controlName, "AutoSize", "true");
            }
            else
            {
                string thisWidth = FWidth.ToString();

                if (TXMLParser.HasAttribute(ctrl.xmlNode, "Width"))
                {
                    thisWidth = TXMLParser.GetAttribute(ctrl.xmlNode, "Width");
                }

                writer.SetControlProperty(ctrl.controlName, "Size", "new System.Drawing.Size(" + thisWidth + ", " + FHeight.ToString() + ")");
            }

            string ActionToPerform = ctrl.GetAttribute("Action");

            if (writer.CodeStorage.FActionList.ContainsKey(ActionToPerform))
            {
                // deal with enabling and disabling of action, affecting the menu item
                if (!writer.Template.FCodelets.Contains("ENABLEDEPENDINGACTIONS_" + ActionToPerform))
                {
                    string ActionEnabling = "";
                    ActionEnabling += "if (e.ActionName == \"" + ActionToPerform + "\")" + Environment.NewLine;
                    ActionEnabling += "{" + Environment.NewLine;
                    ActionEnabling += "    {#ENABLEDEPENDINGACTIONS_" + ActionToPerform + "}" + Environment.NewLine;
                    ActionEnabling += "}" + Environment.NewLine;
                    writer.Template.AddToCodelet("ACTIONENABLING", ActionEnabling);
                }

                AddToActionEnabledEvent(writer, ActionToPerform, ctrl.controlName);

                // deal with action handler
                AssignEventHandlerToControl(writer, ctrl, "Click", ActionToPerform);

                TActionHandler ActionHandler = writer.CodeStorage.FActionList[ActionToPerform];
                SetControlActionProperties(writer, ctrl, ActionHandler);

                // use the label from the action
                ctrl.Label = ActionHandler.actionLabel;
            }
            else if (ctrl.HasAttribute("ActionClick"))
            {
                AssignEventHandlerToControl(writer, ctrl, "Click", ctrl.GetAttribute("ActionClick"));
            }
            else if (ctrl.HasAttribute("ActionDoubleClick"))
            {
                AssignEventHandlerToControl(writer, ctrl, "DoubleClick", ctrl.GetAttribute("ActionDoubleClick"));
            }

            if (ctrl.HasAttribute("Enabled"))
            {
                AddToActionEnabledEvent(writer, ctrl.GetAttribute("Enabled"), ctrl.controlName);
            }

            if (ctrl.HasAttribute("OnChange"))
            {
                AssignEventHandlerToControl(writer, ctrl, "ValueChanged", ctrl.GetAttribute("OnChange"));
            }

            if (ctrl.HasAttribute("Tooltip"))
            {
                writer.Template.AddToCodelet("INITUSERCONTROLS", "FPetraUtilsObject.SetStatusBarText(" + ctrl.controlName +
                    ", Catalog.GetString(\"" +
                    ctrl.GetAttribute("Tooltip") +
                    "\"));" + Environment.NewLine);
            }

            if (ctrl.HasAttribute("PartnerShortNameLookup"))
            {
                LinkControlPartnerShortNameLookup(writer, ctrl);
            }
            else if (ctrl.HasAttribute("DataField"))
            {
                string dataField = ctrl.GetAttribute("DataField");

                if (dataField.IndexOf(".") == -1)
                {
                    dataField = writer.CodeStorage.FMainEditTable + "." + dataField;
                }

                LinkControlDataField(writer, ctrl, GetTableField(dataField, true));
            }
            else if (writer.CodeStorage.FMainEditTable != "")
            {
                //if (ctrl.controlTypePrefix != "lbl" && ctrl.controlTypePrefix != "pnl" && ctrl.controlTypePrefix != "grp" &&
                if (!(this is LabelGenerator || this is GroupBoxGenerator))
                {
                    TTableField field =
                        GetTableField(writer.CodeStorage.FMainEditTable + "." + ctrl.controlName.Substring(ctrl.controlTypePrefix.Length), false);

                    if (field != null)
                    {
                        LinkControlDataField(writer, ctrl, field);
                    }
                }
            }
        }

        /// <summary>
        /// fetch the partner short name from the server;
        /// this control is readonly, therefore we don't need statusbar help
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="ctrl"></param>
        private void LinkControlPartnerShortNameLookup(IFormWriter writer, TControlDef ctrl)
        {
            string PartnerShortNameLookup = ctrl.GetAttribute("PartnerShortNameLookup");
            string tablename = writer.CodeStorage.FMainEditTable;
            string fieldname = PartnerShortNameLookup;

            if (PartnerShortNameLookup.IndexOf(".") > -1)
            {
                tablename = PartnerShortNameLookup.Split('.')[0];
                fieldname = PartnerShortNameLookup.Split('.')[1];
            }

            string showData = "TPartnerClass partnerClass;" + Environment.NewLine;
            showData += "string partnerShortName;" + Environment.NewLine;
            showData += "TRemote.MPartner.Partner.ServerLookups.GetPartnerShortName(" + Environment.NewLine;
            showData += "    FMainDS." + tablename + "[0]." + fieldname + "," + Environment.NewLine;
            showData += "    out partnerShortName," + Environment.NewLine;
            showData += "    out partnerClass);" + Environment.NewLine;
            showData += ctrl.controlName + ".Text = partnerShortName;" + Environment.NewLine;

            writer.Template.AddToCodelet("SHOWDATA", showData);
        }

        private TTableField GetTableField(string ADataFieldName, bool AShowWarningNonExistingField)
        {
            string tablename = ADataFieldName.Split('.')[0];
            string fieldname = ADataFieldName.Split('.')[1];

            TTable table = FPetraXMLStore.GetTable(tablename);

            if (table == null)
            {
                throw new Exception("Cannot find table: " + tablename);
            }

            return table.GetField(fieldname, AShowWarningNonExistingField);
        }

        private void LinkControlDataField(IFormWriter writer, TControlDef ctrl, TTableField AField)
        {
            string AssignValue = "";
            string tablename = TTable.NiceTableName(AField.strTableName);
            string fieldname = TTable.NiceFieldName(AField);

            if (!AField.bNotNull)
            {
                // need to check for IsNull
                AssignValue += "if (FMainDS." + tablename + "[0].Is" + fieldname + "Null())" + Environment.NewLine;
                AssignValue += "{" + Environment.NewLine;
                AssignValue += "    " + this.AssignValue(ctrl, null, null) + Environment.NewLine;
                AssignValue += "}" + Environment.NewLine;
                AssignValue += "else" + Environment.NewLine;
                AssignValue += "{" + Environment.NewLine;
                AssignValue += "    " +
                               this.AssignValue(ctrl, "FMainDS." + tablename + "[0]." + fieldname, AField.GetDotNetType()) + Environment.NewLine;
                AssignValue += "}" + Environment.NewLine;
            }
            else
            {
                AssignValue += this.AssignValue(ctrl, "FMainDS." + tablename + "[0]." + fieldname, AField.GetDotNetType()) + Environment.NewLine;
            }

            writer.Template.AddToCodelet("SHOWDATA", AssignValue);

            string GetValue = "";

            if (!AField.bNotNull && (this.GetControlValue(ctrl, null) != null))
            {
                // need to check for IsNull
                GetValue += "if (" + this.GetControlValue(ctrl, null) + ")" + Environment.NewLine;
                GetValue += "{" + Environment.NewLine;
                GetValue += "    FMainDS." + tablename + "[0].Set" + fieldname + "Null();" + Environment.NewLine;
                GetValue += "}" + Environment.NewLine;
                GetValue += "else" + Environment.NewLine;
                GetValue += "{" + Environment.NewLine;
                GetValue += "    FMainDS." + tablename + "[0]." + fieldname + " = " +
                            this.GetControlValue(ctrl, AField.GetDotNetType()) + ";" + Environment.NewLine;
                GetValue += "}" + Environment.NewLine;
            }
            else
            {
                GetValue += "FMainDS." + tablename + "[0]." + fieldname + " = " +
                            this.GetControlValue(ctrl, AField.GetDotNetType()) + ";" + Environment.NewLine;
            }

            writer.Template.AddToCodelet("SAVEDATA", GetValue);

            // setstatusbar tooltips for datafields, with getstring plus value from petra.xml
            string helpText = AField.strHelp;

            if (helpText.Length == 0)
            {
                helpText = AField.strDescription;
            }

            if (helpText.Length > 0)
            {
                writer.Template.AddToCodelet("INITUSERCONTROLS", "FPetraUtilsObject.SetStatusBarText(" + ctrl.controlName +
                    ", Catalog.GetString(\"" +
                    helpText +
                    "\"));" + Environment.NewLine);
            }
        }

        /// <summary>
        /// Sets the properties of a control which are defined under "Actions:" in the .yaml file
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="ctrl"></param>
        /// <param name="AActionHandler"></param>
        public virtual void SetControlActionProperties(IFormWriter writer, TControlDef ctrl, TActionHandler AActionHandler)
        {
            if (AActionHandler.actionImage.Length > 0)
            {
                /* Get the name of the image for the toolbar button
                 * and put it into the resources.
                 * The images must be in the directory specified by the ResourceDir command line parameter
                 */
                writer.SetControlProperty(ctrl.controlName, "Image",
                    "((System.Drawing.Bitmap)resources" + ctrl.controlType + ".GetObject(\"" + ctrl.controlName + ".Glyph\"))");
                writer.AddImageToResource(ctrl.controlName, AActionHandler.actionImage, "Bitmap");
            }

            if (AActionHandler.actionTooltip.Length > 0)
            {
                writer.SetControlProperty(ctrl.controlName, "ToolTipText", "\"" + AActionHandler.actionTooltip + "\"");
            }
        }

        public virtual void OnChangeDataType(IFormWriter writer, XmlNode curNode)
        {
            OnChangeDataType(writer, curNode, curNode.Name);
        }

        public virtual void OnChangeDataType(IFormWriter writer, XmlNode curNode, string controlName)
        {
            // the selection of this control triggers the available options in other controls
            if (TXMLParser.HasAttribute(curNode, "OnChangeDataType"))
            {
                writer.Template.AddToCodelet("CONTROLINITIALISATION",
                    "this." + controlName + ".Leave += new EventHandler(this." + StringHelper.UpperCamelCase(controlName,
                        ",",
                        false,
                        false) + "_SelectionChangeCommitted);" + Environment.NewLine +
                    "this." + controlName + ".SelectionChangeCommitted += new EventHandler(this." +
                    StringHelper.UpperCamelCase(controlName, ",", false, false) + "_SelectionChangeCommitted);" + Environment.NewLine);
                writer.CodeStorage.FEventHandlersImplementation +=
                    "private void " +
                    StringHelper.UpperCamelCase(controlName, ",", false,
                        false) + "_SelectionChangeCommitted(System.Object sender, System.EventArgs e)" + Environment.NewLine +
                    "{" + Environment.NewLine +
                    "  " +
                    StringHelper.UpperCamelCase(controlName, ",", false,
                        false) + "_Initialise(" + controlName + ".GetSelected" + TXMLParser.GetAttribute(
                        curNode,
                        "OnChangeDataType") + "());" + Environment.NewLine +
                    "}" + Environment.NewLine + Environment.NewLine;
                writer.CodeStorage.FEventHandlersImplementation +=
                    "private void " + StringHelper.UpperCamelCase(controlName, ",", false, false) + "_Initialise(" + TXMLParser.GetAttribute(curNode,
                        "OnChangeDataType") + " AParam)" + Environment.NewLine +
                    "{" + Environment.NewLine +
                    "  Int32 Index;" + Environment.NewLine +
                    "  Index = this." + controlName + ".Find" +
                    TXMLParser.GetAttribute(curNode, "OnChangeDataType") + "InComboBox(AParam);" + Environment.NewLine +
                    "  if ((Index >= 0) && (Index < this." + controlName + ".Items.Count) && (Index != this." + controlName + ".SelectedIndex)) " +
                    Environment.NewLine +
                    "  {" + Environment.NewLine +
                    "    this." + controlName + ".SelectedIndex = Index;" + Environment.NewLine +
                    "  }" + Environment.NewLine +
                    "  {#INITIALISE_" + controlName + "}" + Environment.NewLine +
                    "}" + Environment.NewLine + Environment.NewLine;
            }
        }

        // e.g. used for controls on Reports (readparameter, etc)
        public virtual void ApplyDerivedFunctionality(IFormWriter writer, XmlNode curNode)
        {
        }
    }

    /// <summary>
    /// Providers are not added to any control; they don't have a name, size of position
    /// </summary>
    public class ProviderGenerator : TControlGenerator
    {
        public ProviderGenerator(string APrefix, System.Type AType)
            : base(APrefix, AType)
        {
            FLocation = false;
            FGenerateLabel = false;
            FAddControlToContainer = false;
        }

        public override void SetControlProperties(IFormWriter writer, TControlDef ctrl)
        {
            // don't call base, because it should not have size, location, or name
            writer.Template.AddToCodelet("CONTROLINITIALISATION",
                "//" + Environment.NewLine + "// " + ctrl.controlName + Environment.NewLine + "//" + Environment.NewLine);
        }
    }
    public class ContainerGenerator : TControlGenerator
    {
        public ContainerGenerator(string prefix, System.Type type)
            : base(prefix, type)
        {
        }

        public override void GenerateDeclaration(IFormWriter writer, TControlDef ctrl)
        {
            base.GenerateDeclaration(writer, ctrl);
            writer.AddContainer(ctrl.controlName);
        }

        public override void SetControlProperties(IFormWriter writer, TControlDef container)
        {
            base.SetControlProperties(writer, container);

            // add all the children
            List <TControlDef>children = new List <TControlDef>();

            foreach (TControlDef child in container.FCodeStorage.FControlList.Values)
            {
                if (child.parentName == container.controlName)
                {
                    children.Add(child);
                }
            }

            children.Sort(new CtrlItemOrderComparer());

            foreach (TControlDef child in children)
            {
                writer.CallControlFunction(container.controlName,
                    "Controls.Add(this." +
                    child.controlName + ")");
            }
        }
    }
    public class ToolStripGenerator : TControlGenerator
    {
        public string FDocking = "Top";
        public ToolStripGenerator(string prefix, System.Type AType)
            : base(prefix, AType)
        {
            FGenerateLabel = false;
            FLocation = false;
            FHeight = 24;
            FWidth = 10;
        }

        public override void GenerateDeclaration(IFormWriter writer, TControlDef ctrl)
        {
            base.GenerateDeclaration(writer, ctrl);
            writer.AddContainer(ctrl.controlName);
        }

        public static string GetListOfChildren(IFormWriter writer, TControlDef container)
        {
            // add all the children
            string addChildren = "";

            // TODO add Container elements in statusbar
            if (container.controlName.StartsWith("stb"))
            {
                return addChildren;
            }

            //Console.WriteLine("Container: " + container.controlName);
            foreach (XmlNode child in TYml2Xml.GetChildren(container.xmlNode, true))
            {
                // Console.WriteLine("Child: " + child.Name);
                if (addChildren.Length > 0)
                {
                    addChildren += "," + Environment.NewLine + "            ";
                }

                /* Get unique name if we need it
                 * at the moment we need it only for menu separators
                 */
                String UniqueChildName = child.Name;
                TControlDef ControlDefChild = container.FCodeStorage.GetControl(child.Name);

                if (ControlDefChild == null)
                {
                    UniqueChildName = TYml2Xml.GetAttribute(child, "UniqueName");
                    ControlDefChild = container.FCodeStorage.GetControl(UniqueChildName);
                }

                addChildren = addChildren + UniqueChildName;                 //child.Name; //controlName;

                IControlGenerator ctrlGenerator = writer.FindControlGenerator(child);

                // add control itself
                if ((ControlDefChild != null) && (ctrlGenerator != null))
                {
                    ctrlGenerator.GenerateDeclaration(writer, ControlDefChild);
                    ctrlGenerator.SetControlProperties(writer, ControlDefChild);
                }
            }

            return addChildren;
        }

        public override void SetControlProperties(IFormWriter writer, TControlDef container)
        {
            string controlName = container.controlName;

            // add all the children
            string addChildren = GetListOfChildren(writer, container);

            container.SetAttribute("Dock", FDocking);
            base.SetControlProperties(writer, container);

            if (addChildren.Length > 0)
            {
                writer.CallControlFunction(controlName,
                    "Items.AddRange(new System.Windows.Forms.ToolStripItem[] {" + Environment.NewLine +
                    "               " + addChildren +
                    "})");
            }

            // todo: location?
            // todo: event handler

            /*
             * this.menuStrip1.Dock = System.Windows.Forms.DockStyle.None;
             * this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
             * this.toolStripMenuItem1});
             * this.menuStrip1.Location = new System.Drawing.Point(0, 0);
             * this.menuStrip1.Name = "menuStrip1";
             * this.menuStrip1.Size = new System.Drawing.Size(138, 24);
             * this.menuStrip1.TabIndex = 1;
             * this.menuStrip1.Text = "menuStrip1";
             * this.menuStrip1.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.MenuStrip1ItemClicked);
             */
        }
    }
}