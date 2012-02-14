using System.Collections.Generic;
using DelftTools.Controls;
using DelftTools.Controls.Swf.Table;
using DelftTools.Functions;
using DelftTools.Functions.Binding;
using DelftTools.Functions.Generic;
using NUnit.Framework;

namespace DelftTools.Tests.Controls.Swf.Table
{
    [TestFixture]
    public class TableViewFunctionPasteControllerTest
    {
        private Function f;
        private TableView tableView;
        private TableViewFunctionPasteController tableViewCopyPasteController;

        private void SetupFunctionTable()
        {
            tableView = new TableView();

            var x = new Variable<int>("x");
            var y = new Variable<int>("y");
            y.DefaultValue = -99;

            f = new Function { Arguments = { x }, Components = { y } };

            f[1] = 15;
            f[5] = 5;
            f[10] = 1;
            f[15] = 10;

            var bindingList = new FunctionBindingList(f) { SynchronizeInvoke = tableView };
            tableView.Data = bindingList;

            tableView.AllowColumnSorting(false);
            tableView.AllowColumnFiltering(false);
            
            tableViewCopyPasteController = new TableViewFunctionPasteController(tableView, new List<int>(new [] { 0 }));
        }

        [Test]
        public void PasteValues()
        {
            SetupFunctionTable();

            tableViewCopyPasteController.PasteLines(new[] {"11\t2", "3\t3","2\t9", "17\t12", "-1\t17", "5\t6"});

            Assert.IsTrue(Function1DContains(new object[] { 1, 15 }));
            Assert.IsTrue(Function1DContains(new object[] { 2, 9 }));
            Assert.IsTrue(Function1DContains(new object[] { 3, 3 }));
            Assert.IsTrue(Function1DContains(new object[] { 5, 6 }));
            Assert.IsTrue(Function1DContains(new object[] { 10, 1 }));
            Assert.IsTrue(Function1DContains(new object[] { 11, 2 }));
            Assert.IsTrue(Function1DContains(new object[] { 15, 10 }));
            Assert.IsTrue(Function1DContains(new object[] { 17, 12 }));
            Assert.IsTrue(Function1DContains(new object[] { -1, 17 }));
            Assert.AreEqual(9, f.Arguments[0].Values.Count);
        }

        private bool Function1DContains(object[] objects)
        {
            return f[objects[0]].Equals(objects[1]);
        }
    }
}
