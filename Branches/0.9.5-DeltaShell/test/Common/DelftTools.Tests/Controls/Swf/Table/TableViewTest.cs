using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using DelftTools.Controls;
using DelftTools.Controls.Swf.Editors;
using DelftTools.Controls.Swf.Table;
using DelftTools.Controls.Swf.Test.Table.TestClasses;
using DelftTools.Functions;
using DelftTools.Functions.Binding;
using DelftTools.Functions.Filters;
using DelftTools.Functions.Generic;
using DelftTools.Tests.Controls.Swf.Table.TestClasses;
using DelftTools.TestUtils;
using DelftTools.Units;
using DelftTools.Utils.Collections.Generic;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;
using log4net;
using NUnit.Framework;
using Category = NUnit.Framework.CategoryAttribute;
using SortOrder = DelftTools.Controls.SortOrder;

namespace DelftTools.Tests.Controls.Swf.Table
{
    [TestFixture]
    public class TableViewTest
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(TableViewTest));

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            LogHelper.ConfigureLogging();
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            LogHelper.ResetLogging();
        }
        

        [Test]
        [Category("Windows.Forms")]
        public void ShowReadOnlyTableView()
        {
            var x = new Variable<int>("x");
            var y = new Variable<int>("y");
            y.DefaultValue = -99;

            var f = new Function { Arguments = { x }, Components = { y } };

            f[1] = 1;
            f[5] = 5;
            f[10] = 10;
            f[15] = 15;

            var tableView = new TableView { ReadOnly = true };
            var bindingList = new FunctionBindingList(f) { SynchronizeInvoke = tableView};
            tableView.Data = bindingList;

            WindowsFormsTestHelper.ShowModal(tableView);
        }

        [Test]
        [Category("Windows.Forms")]
        public void ShowReadOnlyFirstColumn()
        {
            var x = new Variable<int>("x");
            var y = new Variable<int>("y");
            y.DefaultValue = -99;

            var f = new Function { Arguments = { x }, Components = { y } };

            f[1] = 1;
            f[5] = 5;
            f[10] = 10;
            f[15] = 15;

            var tableView = new TableView();
            var bindingList = new FunctionBindingList(f) { SynchronizeInvoke = tableView};
            tableView.Data = bindingList;
            tableView.Columns[0].ReadOnly = true;

            WindowsFormsTestHelper.ShowModal(tableView);
        }



        [Test]
        [Category("Windows.Forms")]
        public void InsertingValuesinTableAreSortedAutomatically()
        {
            var x = new Variable<int>("x");
            var y = new Variable<int>("y");
            y.DefaultValue = -99;

            var f = new Function { Arguments = { x }, Components = { y } };

            f[1] = 1;
            f[5] = 5;
            f[10] = 10;
            f[15] = 15;

            var tableView = new TableView();
            var bindingList = new FunctionBindingList(f) { SynchronizeInvoke = tableView };
            tableView.Data = bindingList;

            WindowsFormsTestHelper.ShowModal(tableView);
        }

        [Test]
        [Category("Windows.Forms")]
        public void TableSortingAndFilteringCanBeDisabled()
        {
            var x = new Variable<int>("x");
            var y = new Variable<int>("y");
            y.DefaultValue = -99;

            var f = new Function { Arguments = { x }, Components = { y } };

            f[1] = 15;
            f[5] = 5;
            f[10] = 1;
            f[15] = 10;

            var tableView = new TableView();
            var bindingList = new FunctionBindingList(f) { SynchronizeInvoke = tableView };
            tableView.Data = bindingList;

            tableView.AllowColumnSorting(false);
            tableView.AllowColumnFiltering(false);

            WindowsFormsTestHelper.ShowModal(tableView);
        }

        [Test]
        public void TableViewShouldUpdateColums()
        {
            var table = new DataTable("tablename");
            var view = new TableView { Data = table };
            //view.Show();
            var column = new DataColumn("testName", typeof(double)) { Caption = "testCaption" };
            table.Columns.Add(column);
            Assert.AreEqual(1, view.Columns.Count);
            Assert.AreEqual("testName", view.Columns[0].Name);
            Assert.AreEqual("testCaption", view.Columns[0].Caption);

            column.Caption = "newTestCaption";
            view.ResetBindings();

            Assert.AreEqual("newTestCaption", view.Columns[0].Caption);

        }

        [Test]
        [Category("Windows.Forms")]
        public void ContextMenuPasteTextIntoGridBoundToBindingList()
        {
            IFunction function = new Function();
            function.Arguments.Add(new Variable<string>("A"));
            function.Components.Add(new Variable<string>("B"));
            function["a"] = "b";
            var bindingList = new FunctionBindingList(function);

            var gridControl = new GridControl();
            var xtraGridContextMenu = new XtraGridContextMenu { SourceGrid = gridControl };
            gridControl.ContextMenuStrip = xtraGridContextMenu;
            gridControl.DataSource = bindingList;

            bindingList.SynchronizeInvoke = gridControl;
            bindingList.SynchronizeWaitMethod = delegate { Application.DoEvents(); };

            const string argumentvalue1 = "argumentvalue1";
            const string componentvalue1 = "componentvalue1";
            const string argumentvalue2 = "argumentvalue2";
            string componentvalue2 = "componentvalue2" + (char)0x03A9;

            Clipboard.SetText(string.Format("{0}\t{1}" + Environment.NewLine +
                                            "{2}\t{3}", argumentvalue1, componentvalue1, argumentvalue2,
                                            componentvalue2));
            
            WindowsFormsTestHelper.Show(gridControl);

            for (int i = 0; i < 5; i++)
            {
                Application.DoEvents();
                Thread.Sleep(50);
            }

            var v = (GridView)gridControl.FocusedView;
            xtraGridContextMenu.PasteClipboardContents();

            for (int i = 0; i < 5; i++)
            {
                Application.DoEvents();
                Thread.Sleep(50);
            }

            Assert.AreEqual(argumentvalue1, v.GetRowCellValue(0, v.Columns[0]));
            Assert.AreEqual(componentvalue1, v.GetRowCellValue(0, v.Columns[1]));
            Assert.AreEqual(argumentvalue2, v.GetRowCellValue(1, v.Columns[0]));
            Assert.AreEqual(componentvalue2, v.GetRowCellValue(1, v.Columns[1]));
            WindowsFormsTestHelper.ShowModal(gridControl);
        }

        [Test]
        [Category("Windows.Forms")]
        public void DevExpressTableView_ContextMenuPasteTextIntoGridBoundToDataTable()
        {
            var tableWithTwoStrings = new DataTable();
            tableWithTwoStrings.Columns.Add("A", typeof(string));
            tableWithTwoStrings.Columns.Add("B", typeof(string));
            tableWithTwoStrings.Rows.Add("a", "b");

            var gridControl = new GridControl();
            var xtraGridContextMenu = new XtraGridContextMenu { SourceGrid = gridControl };
            gridControl.ContextMenuStrip = xtraGridContextMenu;
            gridControl.DataSource = tableWithTwoStrings;


            Clipboard.SetText(string.Format("{0}\t{1}" + Environment.NewLine + "{0}\t{1}", "oe", "oe1"));
            WindowsFormsTestHelper.Show(gridControl);
            var v = (GridView)gridControl.FocusedView;

            Assert.AreEqual(1, v.RowCount);
            gridControl.Select();
            v.SelectRow(0);

            //copies data to the grid (two new rows are added.

            xtraGridContextMenu.PasteClipboardContents();

            Assert.AreEqual("oe", v.GetRowCellValue(0, v.Columns[0]));
            Assert.AreEqual("oe1", v.GetRowCellValue(0, v.Columns[1]));
            Assert.AreEqual("oe", v.GetRowCellValue(1, v.Columns[0]));
            Assert.AreEqual("oe1", v.GetRowCellValue(1, v.Columns[1]));

            WindowsFormsTestHelper.ShowModal(gridControl);
        }


        [Test]
        [Category("Windows.Forms")]
        public void Init()
        {
            var tableView = new TableView();

            var table = new DataTable();

            table.Columns.Add("A", typeof(DateTime));
            table.Columns.Add("B", typeof(int));
            for (int i = 0; i < 50; i++)
            {
                DataRow row = table.NewRow();
                row["A"] = DateTime.Now;
                row["B"] = i;
                table.Rows.Add(row);
            }

            tableView.Data = table;
            WindowsFormsTestHelper.ShowModal(tableView);
        }

        [Test]
        [Category("Windows.Forms")]
        public void DevExpressTableView_Init2()
        {
            var gc = new GridControl();
            var gv = new GridView(gc);
            gc.ViewCollection.Add(gv);
            gc.Dock = DockStyle.Right;
            gc.LookAndFeel.UseDefaultLookAndFeel = false;
            //todo : check if these settings should be moved to the designer.

            //gv.Appearance.SelectedRow.Assign(gv.Appearance.SelectedRow);

            gv.OptionsSelection.MultiSelect = true;
            gv.OptionsSelection.MultiSelectMode = GridMultiSelectMode.CellSelect;


            var table = new DataTable();

            table.Columns.Add("A", typeof(double));
            table.Columns.Add("B", typeof(int));
            DataRow row = table.NewRow();
            row["A"] = 10.0;
            row["B"] = 1;
            table.Rows.Add(row);

            for (int i = 90; i < 120; i++)
            {
                row = table.NewRow();
                row["A"] = i;
                row["B"] = i + 2;
                table.Rows.Add(row);
            }

            var bs = new BindingSource();
            bs.DataSource = table;
            bs.AllowNew = true;
            gc.DataSource = bs;
            var pg = new PropertyGrid();
            pg.SelectedObject = gv;
            pg.Dock = DockStyle.Left;

            pg.Width = 300;

            var pg2 = new PropertyGrid { Width = 300, Location = new Point(310, 0), Height = 500 };
            pg2.SelectedObject = gc;

            var form = new Form { Width = 1200 };

            form.Controls.Add(gc);
            form.Controls.Add(pg);
            form.Controls.Add(pg2);
            WindowsFormsTestHelper.ShowModal(form);
        }

        [Test]
        [Category("Windows.Forms")]
        public void Init3()
        {
            var tableView = new TableView();

            var dataList = new BindingList<SomeData>();
            dataList.Add(new SomeData(10.0, 1));
            dataList.Add(new SomeData(11.0, 2));

            tableView.Data = dataList;
            WindowsFormsTestHelper.ShowModal(tableView);
        }

        [Test]
        [Category("Integration")] // crete control is slow
        public void SetDataSource()
        {
            var table = new DataTable("table1");

            var tableView = new TableView { Data = table };

            Assert.AreEqual(table, tableView.Data, "Data assigned to TableView must not change by itself");
        }

        [Test]
        [Category("Windows.Forms")]
        public void ShowGridView()
        {
            var table = new DataTable();

            table.Columns.Add("A", typeof(double));
            table.Columns.Add("B", typeof(int));

            for (int i = 0; i < 50; i++)
            {
                DataRow row = table.NewRow();
                row["A"] = i * 10.0;
                row["B"] = i;
                table.Rows.Add(row);
            }

            var tableView = new TableView { Data = table };
            WindowsFormsTestHelper.ShowModal(tableView);
        }

        [Test]
        [Category("Windows.Forms")]
        [Ignore("Doesn't work on buildserver because next test is start before previous is finished")]
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
            };
            WindowsFormsTestHelper.ShowModal(tableView, onShown);
            Assert.AreEqual(0, table.Rows[0].ItemArray[1]);
            Assert.AreEqual(0, table.Rows[1].ItemArray[1]);
        }

        [Test]
        [Category("Windows.Forms")]
        [Ignore("Doesn't work on buildserver because next test is start before previous is finished")]
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
            };
            WindowsFormsTestHelper.ShowModal(tableView, onShown);
            Assert.AreEqual(0, table.Rows[0].ItemArray[1]);
            Assert.AreEqual(0, table.Rows[1].ItemArray[1]);
            Assert.AreEqual(0, table.Rows[2].ItemArray[1]);
        }

        [Test]
        [Category("Windows.Forms")]
        [Ignore("Doesn't work on buildserver because next test is start before previous is finished")]
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
            };
            WindowsFormsTestHelper.ShowModal(tableView, onShown);
            Assert.AreEqual(0, table.Rows[0].ItemArray[1]);
            Assert.AreEqual(1, table.Rows[1].ItemArray[1]);
            Assert.AreEqual(0, table.Rows[2].ItemArray[1]); // paste 0 1 to 2 3 4 expects pattern 0 1 0
            Assert.AreEqual(1, table.Rows[3].ItemArray[1]);
            Assert.AreEqual(0, table.Rows[4].ItemArray[1]);
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


        [Test]
        [Category("Windows.Forms")]
        public void DefaultCellEditor()
        {
            var items = new List<ClassWithTwoProperties>(new[]
                                                              {
                                                                  new ClassWithTwoProperties { Property1 = "11", Property2 = "12" },
                                                                  new ClassWithTwoProperties { Property1 = "21", Property2 = "22" },
                                                                  new ClassWithTwoProperties { Property1 = "31", Property2 = "32" },
                                                              });

            var tableView = new TableView { Data = new BindingList<ClassWithTwoProperties>(items) };

            WindowsFormsTestHelper.ShowModal(tableView);
        }

        public class FilePathEntry { public string FilePath { get; set; } }

        [Test]
        [Category("Windows.Forms")]
        public void FilePathTypeEditor()
        {
            var items = new List<FilePathEntry>();

            var tableView = new TableView { Data = new BindingList<FilePathEntry>(items) };

            var filePathTypeEditor = DefaultTypeEditorFactory.CreateFilePathEditor();
            filePathTypeEditor.FileFilter = "Assembly Files (*.dll;*.exe)|*.dll;*,exe";
            filePathTypeEditor.Title = "Select assemblies ...";

            tableView.Columns[0].Editor = DefaultTypeEditorFactory.CreateFilePathEditor();

            WindowsFormsTestHelper.ShowModal(tableView);
        }

        public class ItemType
        {
            public string Name { get; set; }
            public override string ToString() { return Name; }
        }

        public class Item
        {
            public string Name { get; set; }
            public ItemType Type { get; set; }
        }

        [Test]
        [Category("Windows.Forms")]
        public void ComboBoxTypeEditor()
        {

            // types of the item, to be shown in the combo box
            var itemTypes = new[]
                                {
                                    new ItemType{ Name = "item type1"},
                                    new ItemType{ Name = "item type2"},
                                    new ItemType{ Name = "item type3"}
                                };

            // default items
            var items = new EventedList<Item>
                            {
                                new Item { Name = "item1", Type = itemTypes[0] },
                                new Item { Name = "item2", Type = itemTypes[1] }
                            };

            var tableView = new TableView { Data = new BindingList<Item>(items) };

            var comboBoxTypeEditor = DefaultTypeEditorFactory.CreateComboBoxTypeEditor();
            comboBoxTypeEditor.Items = itemTypes; // items to be shown in the combo box

            tableView.Columns[1].Editor = comboBoxTypeEditor; // inject it under the 2nd column

            WindowsFormsTestHelper.ShowModal(tableView);
        }

        [Test]
        public void ChangingDataUpdatesColumns()
        {
            var levelTimeSeries = new TimeSeries();
            levelTimeSeries.Arguments[0].DefaultValue = new DateTime(2000, 1, 1);
            levelTimeSeries.Components.Add(new Variable<double>("level", new Unit("m", "m")));
            levelTimeSeries.Name = "water level";

            var tableView = new TableView(); 
            var functionBindingList = new FunctionBindingList(levelTimeSeries) {SynchronizeInvoke = tableView};
            tableView.Data = functionBindingList;

            var flowSeries = new TimeSeries();
            flowSeries.Arguments[0].DefaultValue = new DateTime(2000, 1, 1);
            flowSeries.Components.Add(new Variable<double>("flow", new Unit("m³/s", "m³/s")));
            flowSeries.Name = "flow";
            tableView.Data = new FunctionBindingList(flowSeries) { SynchronizeInvoke = tableView };

            //WindowsFormsTestHelper.ShowModal(tableView);

            Assert.AreEqual("flow [m³/s]", tableView.Columns[1].Caption);
        }

        [Test]
        [Category("Windows.Forms")]
        public void SelectMultipleCellsUsingApi()
        {
            var table = new DataTable();
            table.Columns.Add("column1", typeof(string));
            table.Columns.Add("column2", typeof(string));
            table.Rows.Add(new object[] { "1", "2" });
            table.Rows.Add(new object[] { "3", "4" });

            var tableView = new TableView { Data = table };

            var selectionTableView = new TableView { Data = new BindingList<TableViewCell>(tableView.SelectedCells) };

            WindowsFormsTestHelper.Show(selectionTableView);

            WindowsFormsTestHelper.ShowModal(tableView);
        }

        [Test]
        public void DefaultSelectionIsEmpty()
        {
            var table = new DataTable();
            table.Columns.Add("column1", typeof(string));
            table.Columns.Add("column2", typeof(string));
            table.Rows.Add(new object[] { "1", "2" });
            table.Rows.Add(new object[] { "3", "4" });

            var tableView = new TableView { Data = table };

            tableView.SelectedCells
                .Should().Be.Empty();
        }

        [Test]
        [Category("Windows.Forms")]
        public void ShowWithCustomClass()
        {
            var view = new TableView {Data = new BindingList<Person>()};
            
            WindowsFormsTestHelper.ShowModal(view);
        }

        [Test]
        [Category("Windows.Forms")]
        public void FillOutCustomClass()
        {
            //don't run on on buildserver because the keys will go everywhere ;)
            if (WindowsFormsTestHelper.IsBuildServer)
                return;
            var persons = new BindingList<Person>();
            var tableView = new TableView { Data = persons };
            bool ranOnShown = false;

            Action<Form> onShown = delegate
            {
                tableView.Focus();
                SendKeys.SendWait("J"); // goto row 1 column 2
                SendKeys.SendWait("{RIGHT}"); // goto row 1 column 2
                SendKeys.SendWait("3"); // also select cell below
                SendKeys.SendWait("{DOWN}"); //commit cells

                Assert.AreEqual(1, persons.Count);
                ranOnShown = true;
            };
            WindowsFormsTestHelper.Show(tableView, onShown);
            Assert.IsTrue(ranOnShown);
        }

        [Test]
        public void SetRowCellValue()
        {
            var persons = new BindingList<Person> { new Person { Age = 22, Name = "Jan" } };
            var tableView = new TableView { Data = persons };

            //action! change the top left cell
            tableView.SetRowCellValue(0, 0, "Kees");

            Assert.AreEqual("Kees", persons[0].Name);
        }

        [Test]
        public void SetRowCellValueIsBasedOnVisibleIndexes()
        {
            var persons = new BindingList<Person> { new Person { Age = 22, Name = "Jan" } };
            var tableView = new TableView { Data = persons };

            //hide the first column
            tableView.Columns[0].Visible = false;
            
            //action! change the top left cell
            tableView.SetRowCellValue(0, 0, "23");

            Assert.AreEqual(23, persons[0].Age);
        }

        [Test]
        public void SetRowCellValueDoesNotWorkOnReadOnlyTableView()
        {
            var table = new DataTable();
            table.Columns.Add("readonlycolumn", typeof(string));

            table.Rows.Add(new object[] { "1" });

            var tableView = new TableView { Data = table};

            //read only first column
            tableView.ReadOnly = true;

            tableView.SetRowCellValue(0, 0, "2");

            Assert.AreEqual("1", tableView.GetRowCellValue(0, 0));
        }

        
        [Test]
        public void SetRowCellValueDoesNotWorkOnReadOnlyColumns()
        {
            var table = new DataTable();
            table.Columns.Add("readonlycolumn", typeof(string));
            
            table.Rows.Add(new object[] { "1" });
            
            var tableView = new TableView { Data = table};

            //read only first column
            tableView.Columns[0].ReadOnly = true;
            
            tableView.SetRowCellValue(0,0,"2");

            Assert.AreEqual("1",tableView.GetRowCellValue(0,0));
        }

        [Test]
        public void SetRowCellValueDoesNotWorkOnReadOnlyCell()
        {
            var table = new DataTable();
            table.Columns.Add("readonlycolumn", typeof(string));

            table.Rows.Add(new object[] { "1" });

            var tableView = new TableView { Data = table };

            //read only cell
            tableView.ReadOnlyCellFilter += delegate { return true; };
            

            tableView.SetRowCellValue(0, 0, "2");

            Assert.AreEqual("1", tableView.GetRowCellValue(0, 0));
        }



        [Test]
        public void RowCount()
        {
            //empty tableview should have 0 rowcount
            var persons = new BindingList<Person>();
            var tableView = new TableView { Data = persons };
            Assert.AreEqual(0, tableView.RowCount);

            //1 person one row right?
            persons = new BindingList<Person> { new Person { Age = 22, Name = "Jan" } };
            tableView = new TableView { Data = persons };
            Assert.AreEqual(1, tableView.RowCount);
        }

        [Test]
        public void SelectCells()
        {
            //empty tableview should have 0 rowcount
            var persons = new BindingList<Person>();
            for (int i = 0; i < 10; i++)
            {
                persons.Add(new Person());
            }
            var tableView = new TableView { Data = persons };

            //select cells
            tableView.SelectCells(5, 0, 9, 1);

            //check the bottom cells are all selected
            Assert.AreEqual(10, tableView.SelectedCells.Count);
        }

        [Test]
        public void ChangeSelection()
        {
            //empty tableview should have 0 rowcount
            var persons = new BindingList<Person>();
            for (int i = 0; i < 10; i++)
            {
                persons.Add(new Person());
            }
            var tableView = new TableView { Data = persons };
            var selectionChangedcount = 0;
            var selectedRowChanged = 0;
            
            tableView.SelectedRowChanged += delegate
                                              {
                                                  selectedRowChanged++;
                                              };
            tableView.SelectionChanged += delegate
                                              {
                                                  selectionChangedcount++;
                                              };
            //select cells
            tableView.SelectCells(5, 0, 9, 1);

            Assert.AreEqual(10, selectionChangedcount);

            var currentOld = tableView.CurrentRowObject;
            tableView.SelectedDisplayRowIndex = 1;
            var currentnew = tableView.CurrentRowObject;

            Assert.AreEqual(1, selectedRowChanged);
            Assert.AreNotEqual(currentOld, currentnew);
        }

        [Test]
        public void AddNewRowToDataSource()
        {
            var persons = new BindingList<Person>();
            var tableView = new TableView { Data = persons };
            tableView.AddNewRowToDataSource();
            Assert.AreEqual(1, persons.Count);
        }

        [Test]
        public void SortTableViewWhenSettingSortOrder()
        {

            var person = new List<Person>
                             {new Person {Age = 12, Name = "Aaltje"}, 
                              new Person {Age = 11, Name = "Berend"}};
            var tableView = new TableView { Data = person };

            //first sort descending..the first value should be berend
            tableView.Columns[0].SortOrder = SortOrder.Descending;
            Assert.AreEqual("Berend", tableView.GetRowCellValue(0, 0));

            //sort ascending..the first value should be aaltje
            tableView.Columns[0].SortOrder = SortOrder.Ascending;

            Assert.AreEqual("Aaltje", tableView.GetRowCellValue(0, 0));
        }

        [Test]
        [Category("Windows.Forms")]
        public void ShowRowNumbers()
        {
            var person = new List<Person>
                             {new Person {Age = 12, Name = "Aaltje"}, 
                              new Person {Age = 11, Name = "Berend"}};

            for (var i = 0; i < 10; i++)
            {
                person.Add(new Person {Age = 11, Name = "Berend"});
            }

            var tableView = new TableView
                                {
                                    Data = person, ShowRowNumbers = true
                                };

            WindowsFormsTestHelper.ShowModal(tableView);
        }

        [Test]
        public void FilterTableOnSettingFilterText()
        {
            var person = new List<Person>
                             {new Person {Age = 12, Name = "Aaltje"}, 
                              new Person {Age = 11, Name = "Berend"}};
            var tableView = new TableView { Data = person };

            //action! set a filter
            tableView.Columns[0].FilterString = "[Name] Like 'Jaap%'";

            //no row should be visible
            Assert.AreEqual(0, tableView.RowCount);
        }

        [Test]
        public void IsSorted()
        {
            var person = new List<Person>();
            var tableView = new TableView { Data = person };

            //should be false first
            Assert.IsFalse(tableView.IsSorted);
            //action! set a sort
            tableView.Columns[0].SortOrder = SortOrder.Descending;

            Assert.IsTrue(tableView.IsSorted);
        }

        [Test]
        [Category("Windows.Forms")]
        public void ShowTableViewWithListEnumType()
        {
            //demonstrates how easy it is to add custom editor to tableview for bindinglist<T> :)
            //In this case (BindingList<T> you don't need 
            //a typeconverter or comboboxhelper :)
            var tableView = new TableView();
            var list = new BindingList<ClassWithEnum> { new ClassWithEnum { Type = FruitType.Appel } };
            tableView.Data = list;


            var comboBox = new RepositoryItemImageComboBox();
            comboBox.Items.AddEnum(typeof(FruitType));

            tableView.SetColumnEditor(comboBox, 0);
            WindowsFormsTestHelper.ShowModal(tableView);
        }

        [Test]
        [Category("Windows.Forms")]
        public void ShowTableViewWithDatatableEnumType()
        {
            //demonstrates how hard it is to add custom editor to tableview for datatable
            //In this case you need 
            //a typeconverter and comboboxhelper :(
            //TODO: look into the differences and abstract away from it
            var dataTable = new DataTable();
            dataTable.Columns.Add("Code", typeof(string));
            dataTable.Columns.Add("SomeEnum", typeof(FruitType));

            for (int i = 0; i < 10; i++)
            {
                dataTable.Rows.Add(new object[] { String.Format("Item{0:000}", i), FruitType.Appel });
            }

            dataTable.AcceptChanges();

            var tableView = new TableView {Data = dataTable};

            var comboBox = new RepositoryItemImageComboBox();
            XtraGridComboBoxHelper.Populate(comboBox, typeof(FruitType));

            tableView.SetColumnEditor(comboBox, 1);
            WindowsFormsTestHelper.ShowModal(tableView);
        }

        

        [Test]
        public void DeleteSelectionRemovesValueIfCellWasSelect()
        {
            var persons = new List<Person>
                             {new Person {Name = "Aaltje",Age = 12 }, 
                              new Person {Name = "Berend",Age = 11 }};
            var tableView = new TableView { Data = persons };

            //select the twelve
            tableView.SelectCells(0, 1, 0, 1);

            //action! delete the row
            tableView.DeleteCurrentSelection();

            //assert we 'deleted' the age
            Assert.AreEqual(0,persons[0].Age);
        }

        [Test]
        public void DeleteSelectionRemovesRowIfAllCellsInARowAreSelected()
        {
            var persons = new List<Person>
                             {new Person {Name = "Aaltje",Age = 12}, 
                              new Person {Name = "Berend",Age = 11 }};
            var tableView = new TableView { Data = persons };

            //select the top row
            tableView.SelectCells(0, 0, 0, 1);

            //action! delete the row
            tableView.DeleteCurrentSelection();

            //assert we only have berend now
            Assert.AreEqual(new[] { "Berend" }, persons.Select(p => p.Name).ToArray());
        }

        [Test]
        [Category("Windows.Forms")]
        public void DeleteSelectionTakesAllowDeleteRowsIntoAccount()
        {
            var persons = new List<Person>
                             {new Person {Name = "Aaltje",Age = 12 }, 
                              new Person {Name = "Berend",Age = 11 }};
            var tableView = new TableView { Data = persons };

            //select the top row
            tableView.SelectCells(0, 0, 0, 1);

            //action! delete the row
            tableView.AllowDeleteRow = false;
            tableView.DeleteCurrentSelection();

            //assert aaltje got 'reset'
            Assert.AreEqual(new[] {"", "Berend" }, persons.Select(p => p.Name).ToArray());
            Assert.AreEqual(new[] { 0, 11 }, persons.Select(p => p.Age).ToArray());
        }

        [Test]
        public void DeleteSelectionOnReadOnlyTableViewDoesNothing()
        {
            var persons = new List<Person>
                             {new Person {Name = "Aaltje",Age = 12 }, 
                              new Person {Name = "Berend",Age = 11 }};
            var tableView = new TableView { Data = persons };

            //select the top row
            tableView.SelectCells(0, 0, 0, 1);
            tableView.ReadOnly = true;

            //action! try to delete something
            tableView.DeleteCurrentSelection();
            
            //assert all is well
            Assert.AreEqual("Aaltje",persons[0].Name);
            Assert.AreEqual(2, persons.Count);
        }

        [Test]
        public void DeleteSelectionOnReadOnlyColumnDoesNothing()
        {
            var persons = new List<Person>
                             {new Person {Name = "Aaltje",Age = 12 }, 
                              new Person {Name = "Berend",Age = 11 }};
            var tableView = new TableView { Data = persons };

            //select the top row
            tableView.SelectCells(0, 1, 0, 1);
            tableView.Columns[1].ReadOnly = true;

            //action! try to delete something
            tableView.DeleteCurrentSelection();

            //assert all is well
            Assert.AreEqual(12, persons[0].Age);
            Assert.AreEqual(2, persons.Count);
        }



        [Test]
        [Category("Windows.Forms")]
        public void ShowTableViewWithoutDeleteRecordButton()
        {
            var persons = new List<Person>
                             {new Person {Name = "Aaltje",Age = 12 }, 
                              new Person {Name = "Berend",Age = 11 }};
            var tableView = new TableView { Data = persons };

            
            tableView.AllowDeleteRow = false;

            WindowsFormsTestHelper.ShowModal(tableView);
        }

        [Test]
        [Category("Windows.Forms")]
        public void ShowWithAllowColumnMoveEnabled()
        {
            var persons = new List<Person>
                             {new Person {Name = "Aaltje",Age = 12 }, 
                              new Person {Name = "Berend",Age = 11 }};
            var tableView = new TableView { Data = persons };


            //action! delete the row
            tableView.AllowColumnMoving = true;

            WindowsFormsTestHelper.ShowModal(tableView);
        }

        [Test]
        public void ColumnReadOnly()
        {
            var persons = new List<Person>
                             {new Person {Name = "Aaltje",Age = 12 }, 
                              new Person {Name = "Berend",Age = 11 }};
            var tableView = new TableView { Data = persons };

            tableView.Columns[0].ReadOnly = true;
            Assert.IsTrue(tableView.Columns[0].ReadOnly);
        }

        [Test]
        [Category("Windows.Forms")]
        public void AllowAddNewRow()
        {
            var persons = new List<Person>
                             {new Person {Name = "Aaltje",Age = 12 }, 
                              new Person {Name = "Berend",Age = 11 }};

            //var tableView = new TableView { Data = new BindingList<Person>(persons) { AllowNew = false }, AllowAddNewRow = false };
            var tableView = new TableView { Data = new BindingList<Person>(persons) { AllowNew = true }, AllowAddNewRow = true };

            WindowsFormsTestHelper.ShowModal(tableView);
        }

        [Test]
        [Category("Windows.Forms")]
        public void FunctionBindingListWithFilteredFunction()
        {
            IFunction function = new Function
            {
                Arguments = { new Variable<int>("x"), new Variable<int>("y") },
                Components = { new Variable<int>("f") }
            };

            function[1, 1] = 1;
            function[1, 2] = 2;
            function[2, 1] = 3;
            function[2, 2] = 4;

            var filteredFunction = function.Filter(new VariableValueFilter<int>(function.Arguments[0], new[] { 1 }));

            var tableView = new TableView { Data = new FunctionBindingList(filteredFunction) };

            WindowsFormsTestHelper.ShowModal(tableView);

        }

        [Test]
        public void SetRowCellValueGeneratesCellChangedEvent()
        {
            var table = new DataTable();

            table.Columns.Add("column", typeof (string));
            table.Rows.Add(new object[] {"1"});

            var tableView = new TableView {Data = table};

            int callCount = 0;
            tableView.CellChanged += (s, e) =>
                                         {
                                             callCount++;
                                             Assert.AreEqual(0, e.Value.ColumnIndex);
                                             Assert.AreEqual(0, e.Value.RowIndex);
                                         };
            tableView.SetRowCellValue(0, 0, "2");
            Assert.AreEqual(1, callCount);
        }

        [Test]
        [Category("Windows.Forms")]
        public void TableViewAllowsCreatingColumnsManually()
        {
            var persons = new List<Person>
                              {
                                  new Person {Name = "Aaltje", Age = 12},
                                  new Person {Name = "Berend", Age = 11}
                              };

            var tableView = new TableView {AutoGenerateColumns = false, Data = persons};


            tableView.AddColumn("Name", "Name");
            
            WindowsFormsTestHelper.ShowModal(tableView);
        }


        [Test]
        [Category(TestCategory.Performance)]
        public void RefreshShouldHapenFastWhenFunctionDataSourceHasManyChanges()
        {
            IFunction function = new Function
                                     {
                                         Arguments = {new Variable<int>("x")},
                                         Components = {new Variable<int>("f")}
                                     };

            var values = new int[1000];
            for (var i = 0; i < values.Length; i++)
            {
                values[i] = i;
            }

            // create function binding list before to exclude it when measuring time
            var functionBindingList = new FunctionBindingList(function);

            var tableView = new TableView { Data = functionBindingList };

            // now do the same when table view is shown
            Action<Form> onShown = delegate
                                       {
                                           var stopwatch = new Stopwatch();
                                           stopwatch.Start();
                                           function.SetValues(values, new VariableValueFilter<int>(function.Arguments[0], values));
                                           stopwatch.Stop();

                                           log.DebugFormat("Refreshing grid while inserting values into function took {0}ms", stopwatch.ElapsedMilliseconds);
                                       };

            WindowsFormsTestHelper.ShowModal(tableView, onShown);
        }
    }
}
