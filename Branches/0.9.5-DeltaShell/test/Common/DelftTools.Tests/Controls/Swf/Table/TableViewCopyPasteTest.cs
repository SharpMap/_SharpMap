using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using DelftTools.Controls.Swf.Table;
using DelftTools.Functions;
using DelftTools.Functions.Binding;
using DelftTools.Functions.Generic;
using DelftTools.TestUtils;
using DevExpress.XtraGrid;
using NUnit.Framework;

namespace DelftTools.Tests.Controls.Swf.Table
{
    [TestFixture]
    public class TableViewCopyPasteTest
    {
        [Test]
        [NUnit.Framework.Category(TestCategory.Integration)]
        public void PasteTextIntoFunctionBindingList()
        {
            var argument = new Variable<string>("A");
            var component = new Variable<string>("B");
            //cumbersome stuff for string...shouldn't we allow a null
            argument.NextValueGenerator = new FuncNextValueGenerator<string>(()=> "neeeeeext");
            IFunction function = new Function
                                     {
                                         Arguments = {argument},
                                         Components = {component}
                                     };
            
            var view = new TableView();
            IBindingList bindingList = new FunctionBindingList(function) { SynchronizeInvoke = view };
            view.Data = bindingList;

            const string argumentvalue1 = "argumentvalue1";
            const string componentvalue1 = "componentvalue1";
            const string argumentvalue2 = "argumentvalue2";
            string componentvalue2 = "componentvalue2" + (char)0x03A9;


            var line1 = string.Format("{0}\t{1}", argumentvalue1, componentvalue1);
            var line2 = string.Format("{0}\t{1}", argumentvalue2, componentvalue2);
            
            //Clipboard.SetText(format);

            //action! pasting the text should fill out the function
            
            view.PasteLines(new[]{line1,line2});


            Assert.AreEqual(argumentvalue1, argument.Values[0]);
            Assert.AreEqual(componentvalue1, component.Values[0]);
            Assert.AreEqual(argumentvalue2, argument.Values[1]);
            Assert.AreEqual(componentvalue2, component.Values[1]);

        }

        
        [Test]
        [NUnit.Framework.Category("CrashesOnWindows7")]
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
        [NUnit.Framework.Category("CrashesOnWindows7")]
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
        }

        [Test]
        [NUnit.Framework.Category("CrashesOnWindows7")]
        public void PasteColumnIntoEmptyTableView()
        {
            var persons = new List<Person>();
            //set two persons in clipboard
            const string clipBoardContents = "1\r\n2\r\n3\r\n";
            Clipboard.SetText(clipBoardContents);
            //setup a tableview
            var tableView = new TableView {Data = persons};

            //action!
            tableView.PasteClipboardContents();

            Assert.AreEqual(new[] { "1", "2", "3" }, persons.Select(p => p.Name).ToArray());
            //assert the data is not pasted into the other column
            Assert.AreEqual(new[] { 0, 0, 0 }, persons.Select(p => p.Age).ToArray());
        }

        [Test]
        [NUnit.Framework.Category(TestCategory.WindowsForms)]
        public void CopyPasteCustomClass()
        {
            var list = new BindingList<Person>();// {new Person {Name = "kees", Age = 25}, new Person {Name = "fon", Age = 33}};

            var tableView = new TableView { Data = list };
            WindowsFormsTestHelper
                .ShowModal(tableView);
        }


        [Test]
        [NUnit.Framework.Category("CrashesOnWindows7")]
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
        [NUnit.Framework.Category("CrashesOnWindows7")]
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

        [Test]
        [NUnit.Framework.Category(TestCategory.WorkInProgress)]
        [NUnit.Framework.Category(TestCategory.Jira)]
        public void PasteIntoNewRow()
        {
            //this test relates to issue 3069...demonstrating a problem paste lines when rowselect is enabled.

            var table = new DataTable();
            table.Columns.Add("column1", typeof (string));
            table.Columns.Add("column2", typeof (string));
            table.Rows.Add(new object[] {"1", "2"});
            table.Rows.Add(new object[] {"3", "4"});

            var tableView = new TableView {Data = table, RowSelect = true};

            const string clipBoardContents = "5\t6\r\n";
            Clipboard.SetText(clipBoardContents);

            //first select the 1st row
            tableView.SelectRow(0);

            //focus on the left cell of the new row
            tableView.SetFocus(GridControl.NewItemRowHandle, 0);

            tableView.PasteClipboardContents();

            //check a new row was added to the table
            Assert.AreEqual(3, table.Rows.Count);

        }


    }
}
