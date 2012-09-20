﻿/*
 * >>>> Describe the functionality of this file. <<<<
 *
 * Comment: >>>> Optional comment. <<<<
 *
 * Author:  >>>> Put your full name here <<<<
 *
 * Version: $Revision: 1.3 $ / $Date: 2008/11/27 11:47:02 $
 */

using System;
using System.Xml;
using System.Drawing;
using System.Windows.Forms;

using Ict.Common.Controls;

namespace ControlTestBench
{
/// <summary>
/// Description of ShepherdTest.
/// </summary>
public partial class ShepherdTest : Form
{
    TVisualStylesEnum FEnumStyle = TVisualStylesEnum.vsShepherd;
    
    /// <summary>
    /// Constructor.
    /// </summary>
    public ShepherdTest()
    {
        //
        // The InitializeComponent() call is required for Windows Forms designer support.
        //
        InitializeComponent();
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="AXmlNode"></param>
    /// <param name="AEnumStyle"></param>
    public ShepherdTest(XmlNode AXmlNode, TVisualStylesEnum AEnumStyle)
    {
        //
        // The InitializeComponent() call is required for Windows Forms designer support.
        //
        InitializeComponent();

        FEnumStyle = AEnumStyle;
        tPnlCollapsible1.VisualStyleEnum = FEnumStyle;
        tPnlCollapsible1.TaskListNode = AXmlNode;
        tPnlCollapsible1.Collapse();
        tPnlCollapsible1.Expand();
    }
}
}