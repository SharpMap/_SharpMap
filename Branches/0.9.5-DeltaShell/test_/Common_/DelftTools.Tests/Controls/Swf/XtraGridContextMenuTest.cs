using DelftTools.Controls.Swf.Table;
using DelftTools.Functions;
using DelftTools.Functions.Binding;
using DelftTools.Functions.Generic;
using DelftTools.TestUtils;
using DevExpress.XtraGrid;
using NUnit.Framework;

namespace DelftTools.Tests.Controls.Swf
{
    [TestFixture]
    public class XtraGridContextMenuTest
    {
        [Test,Category("Windows.Forms")]
        public void TestInsertRecordOnGridBoundToAFunctionBindingList()
        {
            //setup a function to bind to
            IFunction function = new Function();
            IVariable argument = new Variable<int>();
            IVariable component = new Variable<int>();
            function.Arguments.Add(argument);
            function.Components.Add(component);
            function[0] = 1;
            function[1] = 1;
            function[2] = 1;
            
            //setup a grid with a menu
            GridControl gridControl = new GridControl();
            gridControl.DataSource = new FunctionBindingList(function) { SynchronizeInvoke = gridControl};

            XtraGridContextMenu xtraGridContextMenu = new XtraGridContextMenu();
            xtraGridContextMenu.SourceGrid = gridControl;
            gridControl.ContextMenuStrip = xtraGridContextMenu;
            WindowsFormsTestHelper windowsFormsTestHelper = new WindowsFormsTestHelper();
            windowsFormsTestHelper.ShowControl(gridControl);
            Assert.AreEqual(3,gridControl.DefaultView.RowCount);
            
        }
    }
}