using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using DelftTools.Controls.Swf.Table;
using DelftTools.Functions;
using DelftTools.Functions.Binding;
using DelftTools.Functions.Generic;
using DelftTools.TestUtils;
using NUnit.Framework;

namespace DelftTools.Tests.Controls.Swf.Table
{
    [TestFixture]
    public class TableViewCopyPasteTest
    {

        [Test]
        public void PasteTextIntoFunctionBindingList()
        {
            IFunction function = new Function();
            var argument = new Variable<string>("A");
            function.Arguments.Add(argument);
            var component = new Variable<string>("B");
            function.Components.Add(component);

            var view = new TableView();
            IBindingList bindingList = new FunctionBindingList(function) { SynchronizeInvoke = view };
            view.Data = bindingList;

            const string argumentvalue1 = "argumentvalue1";
            const string componentvalue1 = "componentvalue1";
            const string argumentvalue2 = "argumentvalue2";
            string componentvalue2 = "componentvalue2" + (char)0x03A9;


            Clipboard.SetText(string.Format("{0}\t{1}" + Environment.NewLine +
                                            "{2}\t{3}", argumentvalue1, componentvalue1, argumentvalue2,
                                            componentvalue2));

            //action! pasting the text should fill out the function
            view.PasteClipboardContents();

            Thread.Sleep(2000);

            Assert.AreEqual(argumentvalue1, argument.Values[0]);
            Assert.AreEqual(componentvalue1, component.Values[0]);
            Assert.AreEqual(argumentvalue2, argument.Values[1]);
            Assert.AreEqual(componentvalue2, component.Values[1]);

        }

        [Test]
        [NUnit.Framework.Category("Windows.Forms")]
        [Ignore("SendKeys does not work nice on build server :(")]
        public void Copy2Paste3Cell()
        {
            DataTable table = CreateTableForCopyPaste();

            Assert.AreEqual(0, table.Rows[0].ItemArray[1]);
            Assert.AreEqual(1, table.Rows[1].ItemArray[1]);
            Assert.AreEqual(2, table.Rows[2].ItemArray[1]);
            Assert.AreEqual(3, table.Rows[3].ItemArray[1]);
            Assert.AreEqual(4, table.Rows[4].ItemArray[1]);

            var tableView = new TableView { Data = table };
            Action<Form> onShown = delegate
            {
                tableView.Focus();
                SendKeys.SendWait("{RIGHT}"); // goto row 1 column 2
                SendKeys.SendWait("+{DOWN}"); // also select cell below
                SendKeys.SendWait("^c"); // copy cells
                SendKeys.SendWait("{DOWN}"); // navigate to cell below
                SendKeys.SendWait("+{DOWN}+{DOWN}"); // also select 2 cells below
                SendKeys.SendWait("^v"); // paste
                Assert.AreEqual(0, table.Rows[0].ItemArray[1]);
                Assert.AreEqual(1, table.Rows[1].ItemArray[1]);
                Assert.AreEqual(0, table.Rows[2].ItemArray[1]); // paste 0 1 to 2 3 4 expects pattern 0 1 0
                Assert.AreEqual(1, table.Rows[3].ItemArray[1]);
                Assert.AreEqual(0, table.Rows[4].ItemArray[1]);
            };

            WindowsFormsTestHelper.ShowModal(tableView, onShown);
        }

        [Test]
        [NUnit.Framework.Category("Windows.Forms")]
        [Ignore("SendKeys does not work nice on build server :(")]
        public void Copy1Paste1Cell()
        {
            DataTable table = CreateTableForCopyPaste();

            Assert.AreEqual(0, table.Rows[0].ItemArray[1]);
            Assert.AreEqual(1, table.Rows[1].ItemArray[1]);

            var tableView = new TableView { Data = table };
            Action<Form> onShown = delegate
            {
                tableView.Focus();
                SendKeys.SendWait("{RIGHT}"); // goto row 1 column 2
                SendKeys.SendWait("^c"); // copy cell
                SendKeys.SendWait("{DOWN}"); // navigate to cell below
                SendKeys.SendWait("^v"); // paste
                Assert.AreEqual(0, table.Rows[0].ItemArray[1]);
                Assert.AreEqual(0, table.Rows[1].ItemArray[1]);
            };
            WindowsFormsTestHelper.ShowModal(tableView, onShown);
        }

        [Test]
        [NUnit.Framework.Category("Windows.Forms")]
        [Ignore("SendKeys does not work nice on build server :(")]
        public void Copy1Paste2Cell()
        {
            DataTable table = CreateTableForCopyPaste();

            Assert.AreEqual(0, table.Rows[0].ItemArray[1]);
            Assert.AreEqual(1, table.Rows[1].ItemArray[1]);
            Assert.AreEqual(2, table.Rows[2].ItemArray[1]);

            var tableView = new TableView { Data = table };
            Action<Form> onShown = delegate
            {
                tableView.Focus();
                SendKeys.SendWait("{RIGHT}"); // goto row 1 column 2
                SendKeys.SendWait("^c"); // copy cell
                SendKeys.SendWait("{DOWN}"); // navigate to cell below
                SendKeys.SendWait("+{DOWN}"); // also select cell below
                SendKeys.SendWait("^v"); // paste
                Assert.AreEqual(0, table.Rows[0].ItemArray[1]);
                Assert.AreEqual(0, table.Rows[1].ItemArray[1]);
                Assert.AreEqual(0, table.Rows[2].ItemArray[1]);
            };
            WindowsFormsTestHelper.ShowModal(tableView, onShown);
        }

        [Test]
        public void PasteClipboardContentsCanAddRows()
        {
            var person = new List<Person> { new Person { Age = 12, Name = "Hoi" }, new Person { Age = 11, Name = "keers" } };
            //set two persons in clipboard
            const string clipBoardContents = "cees anton\t34\r\nsaifon\t66\r\nmartijn\t31\r\n";
            Clipboard.SetText(clipBoardContents);
            //setup a tableview
            var tableView = new TableView { Data = person };

            tableView.PasteClipboardContents();

            Assert.AreEqual("martijn", person[2].Name);
            Assert.AreEqual(31, person[2].Age);
        }

        [Test]
        public void PasteIntoEmptyTableView()
        {
            var persons = new List<Person>();
            //set two persons in clipboard
            const string clipBoardContents = "cees anton\t34\r\nsaifon\t66\r\nmartijn\t31\r\n";
            Clipboard.SetText(clipBoardContents);
            //setup a tableview
            var tableView = new TableView { Data = persons };

            //action!
            tableView.PasteClipboardContents();

            Assert.AreEqual("martijn", persons[2].Name);
            Assert.AreEqual(31, persons[2].Age);
            Assert.AreEqual(3, persons.Count);

            //WindowsFormsTestHelper.ShowModal(tableView);
        }


        [Test]
        [NUnit.Framework.Category("Windows.Forms")]
        public void CopyPasteCustomClass()
        {
            var list = new BindingList<Person>();// {new Person {Name = "kees", Age = 25}, new Person {Name = "fon", Age = 33}};

            var tableView = new TableView { Data = list };
            WindowsFormsTestHelper
                .ShowModal(tableView);
        }


        [Test]
        public void PasteClipboardContentsOverwritesExistingRows()
        {
            var person = new List<Person> { new Person { Age = 12, Name = "Hoi" }, new Person { Age = 11, Name = "keers" } };
            //set two persons in clipboard
            const string clipBoardContents = "cees anton\t34\r\nsaifon\t66\r\n";
            Clipboard.SetText(clipBoardContents);
            //setup a tableview
            var tableView = new TableView { Data = person };

            tableView.PasteClipboardContents();

            Assert.AreEqual("cees anton", person[0].Name);
            Assert.AreEqual(34, person[0].Age);
            Assert.AreEqual("saifon", person[1].Name);
            Assert.AreEqual(66, person[1].Age);
        }

        [Test]
        public void CopySelectionIntoClipBoard()
        {
            var table = new DataTable();
            table.Columns.Add("column1", typeof(string));
            table.Columns.Add("column2", typeof(string));
            table.Rows.Add(new object[] { "1", "2" });
            table.Rows.Add(new object[] { "3", "4" });

            var tableView = new TableView { Data = table };

            //select two rows
            tableView.SelectCells(0, 0, 1, 1);

            //action! copy selection to clipboard
            //tableView.CopySelection
            tableView.CopySelectionToClipboard();

            Assert.AreEqual("1\t2\r\n3\t4\r\n", Clipboard.GetText());
        }

        private static DataTable CreateTableForCopyPaste()
        {
            var table = new DataTable();

            table.Columns.Add("A", typeof(int));
            table.Columns.Add("B", typeof(int));

            for (int i = 0; i < 5; i++)
            {
                DataRow row = table.NewRow();
                row["A"] = i;
                row["B"] = i;
                table.Rows.Add(row);
            }
            return table;
        }
    }
}
