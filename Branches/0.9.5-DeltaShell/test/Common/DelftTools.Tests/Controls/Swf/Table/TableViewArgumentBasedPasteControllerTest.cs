using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using DelftTools.Controls.Swf.Table;
using DelftTools.TestUtils;
using NUnit.Framework;

namespace DelftTools.Tests.Controls.Swf.Table
{
    [TestFixture]
    [Category(TestCategory.WindowsForms)]
    public class TableViewArgumentBasedPasteControllerTest
    {
        private DataTable dataTable;
        private TableViewArgumentBasedPasteController tableViewCopyPasteController;
        private TableView tableView;
        
        private void SetupOneArgumentTable()
        {
            tableView = new TableView();
            dataTable = new DataTable();
            dataTable.Columns.Add("Alpha", typeof (string));
            dataTable.Columns.Add("Numeric", typeof (int));
            dataTable.DefaultView.Sort = "Alpha";
            dataTable.Rows.Add("c", 3);
            dataTable.Rows.Add("b", 2);
            dataTable.Rows.Add("f", 1);
            dataTable.Rows.Add("e", 5);
            dataTable.Rows.Add("d", 4);
            tableView.Data = dataTable;

            tableViewCopyPasteController = new TableViewArgumentBasedPasteController(tableView, new List<int>(new [] {0}));
        }

        private void SetupTwoArgumentTable()
        {
            tableView = new TableView();
            dataTable = new DataTable();
            dataTable.Columns.Add("Branch", typeof(string));
            dataTable.Columns.Add("Chainage", typeof(double));
            dataTable.Columns.Add("Value", typeof(int));
            dataTable.DefaultView.Sort = "Branch, Chainage";
            dataTable.Rows.Add("a", 5.0, 3);
            dataTable.Rows.Add("a", 10.0, 2);
            dataTable.Rows.Add("a", 20.0, 1);
            dataTable.Rows.Add("b", 5.0, 5);
            dataTable.Rows.Add("b", 20.0, 4);
            tableView.Data = dataTable;

            tableViewCopyPasteController = new TableViewArgumentBasedPasteController(tableView, new List<int>(new int[] { 0, 1 }));
        }

        [Test]
        public void ReorderingPaste()
        {
            SetupOneArgumentTable();

            tableViewCopyPasteController.PasteLines(new[] { "d\t6", "b\t9" });

            Assert.IsTrue(DataTableContains(new object[] { "c", 3 }));
            Assert.IsTrue(DataTableContains(new object[] { "b", 9 }));
            Assert.IsTrue(DataTableContains(new object[] { "f", 1 }));
            Assert.IsTrue(DataTableContains(new object[] { "e", 5 }));
            Assert.IsTrue(DataTableContains(new object[] { "d", 6 }));
        }
        
        [Test]
        public void ReorderAndAddPaste()
        {
            SetupOneArgumentTable();

            tableViewCopyPasteController.PasteLines(new[] { "d\t6", "g\t10", "b\t9", "y\t12" });

            Assert.IsTrue(DataTableContains(new object[] {"c", 3}));
            Assert.IsTrue(DataTableContains(new object[] { "b", 9 }));
            Assert.IsTrue(DataTableContains(new object[] { "f", 1 }));
            Assert.IsTrue(DataTableContains(new object[] { "e", 5 }));
            Assert.IsTrue(DataTableContains(new object[] { "d", 6 }));
            Assert.IsTrue(DataTableContains(new object[] { "g", 10 }));
            Assert.IsTrue(DataTableContains(new object[] { "y", 12 }));
        }

        [Test]
        public void TwoArgumentPasteTooFewArguments()
        {
            SetupTwoArgumentTable();

            int callCount = 0;

            tableViewCopyPasteController.PasteFailed += (s, e) =>
                {
                    Assert.AreEqual(
                        "This table contains multiple argument columns (Branch,Chainage). When pasting, please paste the full row width, or paste into non-argument columns only.",
                        e.Value);
                    callCount++;
                };

            tableView.SelectCells(0,1,1,2);
            tableViewCopyPasteController.PasteLines(new[] {"5.0\t4"});

            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void TwoArgumentPasteOnlyComponents()
        {
            SetupTwoArgumentTable();

            int callCount = 0;

            tableViewCopyPasteController.PasteFailed += (s, e) =>
            {
                callCount++;
            };

            tableView.SelectCells(0, 2, 1, 2);
            tableViewCopyPasteController.PasteLines(new[] { "4" });

            Assert.AreEqual(0, callCount);
        }

        [Test]
        public void TwoArgumentPasteOnlyArguments()
        {
            SetupTwoArgumentTable();

            int callCount = 0;

            tableViewCopyPasteController.PasteFailed += (s, e) =>
            {
                Assert.AreEqual(
                    "This table contains multiple argument columns (Branch,Chainage). When pasting, please paste the full row width, or paste into non-argument columns only.",
                    e.Value);
                callCount++;
            };

            tableView.SelectCells(0, 0, 1, 2);
            tableViewCopyPasteController.PasteLines(new[] { "5.0\t4" });

            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void TwoArgumentReorderAndAdd()
        {
            SetupTwoArgumentTable();

            tableViewCopyPasteController.PasteLines(new[] { 
                String.Format("a\t{0}\t8",15.0), 
                String.Format("c\t{0}\t12",5.0), 
                String.Format("b\t{0}\t13",3.0),
                String.Format("a\t{0}\t1", 20.0)});

            Assert.IsTrue(DataTableContains(new object[] { "a", 5.0, 3}));
            Assert.IsTrue(DataTableContains(new object[] { "a", 10.0, 2 }));
            Assert.IsTrue(DataTableContains(new object[] { "a", 20.0, 1 }));
            Assert.IsTrue(DataTableContains(new object[] { "a", 15.0, 8 }));
            Assert.IsTrue(DataTableContains(new object[] { "b", 5.0, 5 }));
            Assert.IsTrue(DataTableContains(new object[] { "b", 20.0, 4 }));
            Assert.IsTrue(DataTableContains(new object[] { "b", 3.0, 13 }));
            Assert.IsTrue(DataTableContains(new object[] { "c", 5.0, 12 }));
        }

        private bool DataTableContains(object[] objects)
        {
            foreach(var row in dataTable.Rows.OfType<DataRow>())
            {
                if (row.ItemArray.SequenceEqual(objects))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
