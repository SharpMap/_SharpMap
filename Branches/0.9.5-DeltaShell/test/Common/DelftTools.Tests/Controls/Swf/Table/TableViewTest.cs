using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
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
using DelftTools.Utils;
using DelftTools.Utils.Collections.Generic;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;
using GisSharpBlog.NetTopologySuite.Geometries;
using log4net;
using NUnit.Framework;
using Category = NUnit.Framework.CategoryAttribute;
using Point = System.Drawing.Point;
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
        [Category(TestCategory.WindowsForms)]
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
        public void EmptyTableViewHasNoFocusedCell()
        {
            var tableView = new TableView();
            Assert.AreEqual(null,tableView.FocusedCell);
        }

        [Test]
        public void TableViewWithNoDataHasNoFocusedCell()
        {
            var tableView = new TableView {Data = new List<Person>()};
            Assert.AreEqual(null, tableView.FocusedCell);
        }

        [Test]
        public void DefaultFocusedCellIsTopLeft()
        {
            //tableview with a single row
            var tableView = new TableView
                                {
                                    Data = new List<Person> {new Person {Age = 10, Name = "Piet"}}
                                };

            Assert.AreEqual(0, tableView.FocusedCell.ColumnIndex);
            Assert.AreEqual(0, tableView.FocusedCell.RowIndex);
        }


        [Test]
        [Category(TestCategory.WindowsForms)]
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
        [Category(TestCategory.WindowsForms)]
        public void ShowReadOnlyFirstColumnWithCopyHeadersEnabled()
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
            tableView.Columns[0].ReadOnly = true;
            tableView.IncludeHeadersOnCopyEntireTable = true;

            WindowsFormsTestHelper.ShowModal(tableView);
        }

        [Test]
        [Category(TestCategory.WindowsForms)]
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
        [Category(TestCategory.WindowsForms)]
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

        [Test] //devexpress code modified to prevent pascal casing to be split into words
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
        [Category(TestCategory.WindowsForms)]
        public void ContextMenuPasteTextIntoGridBoundToBindingList()
        {
            IFunction function = new Function();
            function.Arguments.Add(new Variable<string>("A"){NextValueGenerator = new FuncNextValueGenerator<string>(()=> "new")});
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
        [Category(TestCategory.WindowsForms)]
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
        public void SetTextToTableViewWithDateTimeColumnUsCulture()
        {
            var oldCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-US");

            var tableView = new TableView();

            var table = new DataTable();

            table.Columns.Add("A", typeof(DateTime));
            table.Columns.Add("B", typeof(double));
            var row = table.NewRow();
            row["A"] = DateTime.Now;
            row["B"] = 0;
            table.Rows.Add(row);
            tableView.Data = table;

            tableView.SetCellValue(0, 0, "2010/11/18 01:02:03");
            var dateTime = DateTime.Parse(tableView.GetCellValue(0, 0).ToString());
            Assert.AreEqual(new DateTime(2010, 11, 18, 01, 02, 03), dateTime);

            tableView.SetCellValue(0, 1, "1.3");
            var value = double.Parse(tableView.GetCellValue(0, 1).ToString());
            Assert.AreEqual(1.3, value);

            Thread.CurrentThread.CurrentCulture = oldCulture;
        }

        [Test]
        public void SetTextToTableViewWithDateTimeColumnNlCulture()
        {
            var oldCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("nl-NL");

            var tableView = new TableView();

            var table = new DataTable();

            table.Columns.Add("A", typeof(DateTime));
            table.Columns.Add("B", typeof(double));
            var row = table.NewRow();
            row["A"] = DateTime.Now;
            row["B"] = 0;
            table.Rows.Add(row);
            tableView.Data = table;

            tableView.SetCellValue(0, 0, "2010/11/18 01:02:03");
            var dateTime = DateTime.Parse(tableView.GetCellValue(0, 0).ToString());
            Assert.AreEqual(new DateTime(2010, 11, 18, 01, 02, 03), dateTime);

            tableView.SetCellValue(0, 1, "1,3");
            var value = double.Parse(tableView.GetCellValue(0, 1).ToString());
            Assert.AreEqual(1.3, value);

            Thread.CurrentThread.CurrentCulture = oldCulture;
        }

        [Test]
        [Category(TestCategory.WindowsForms)]
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
        [Category(TestCategory.WindowsForms)]
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
        [Category(TestCategory.WindowsForms)]
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
        [Category(TestCategory.Integration)] // crete control is slow
        public void SetDataSource()
        {
            var table = new DataTable("table1");

            var tableView = new TableView { Data = table };

            Assert.AreEqual(table, tableView.Data, "Data assigned to TableView must not change by itself");
        }

        [Test]
        [Category(TestCategory.WindowsForms)]
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
        [Category(TestCategory.WindowsForms)]
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
        [Category(TestCategory.WindowsForms)]
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
        [Category(TestCategory.WindowsForms)]
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
        [Category(TestCategory.WindowsForms)]
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
        [Category(TestCategory.WindowsForms)]
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
        [Category(TestCategory.WindowsForms)]
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
        [Category(TestCategory.WindowsForms)]
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
        [Category(TestCategory.WindowsForms)]
        public void ShowWithCustomClass()
        {
            var view = new TableView {Data = new BindingList<Person>()};
            
            WindowsFormsTestHelper.ShowModal(view);
        }

        [Test]
        [Category(TestCategory.WindowsForms)]
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
            tableView.SetCellValue(0, 0, "Kees");

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
            tableView.SetCellValue(0, 0, "23");

            Assert.AreEqual(23, persons[0].Age);
        }
        [Test]
        public void SetRowCellValueForCustomColumnOrder()
        {
            var tableView = new TableView();
            var persons = new List<Person>
                              {
                                  new Person {Age = 12, Name = "Hoi"},
                                  new Person {Age = 11, Name = "keers"}
                              };
            tableView.Data = persons;
            //set the age first and name second 
            tableView.Columns[0].DisplayIndex = 1;
            tableView.Columns[1].DisplayIndex = 0;

            tableView.SetCellValue(0,1,"jantje");
            Assert.AreEqual("jantje",persons[0].Name);
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

            tableView.SetCellValue(0, 0, "2");

            Assert.AreEqual("1", tableView.GetCellValue(0, 0));
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
            
            tableView.SetCellValue(0,0,"2");

            Assert.AreEqual("1",tableView.GetCellValue(0,0));
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
            

            tableView.SetCellValue(0, 0, "2");

            Assert.AreEqual("1", tableView.GetCellValue(0, 0));
        }
        
        [Test]
        public void SetRowCellValueOnInvalidCellDoesNotCommit()
        {
            var persons = new List<Person>
                              {
                                  new Person {Age = 12, Name = "Hoi"},
                                  new Person {Age = 11, Name = "keers"}
                              };
            var tableView = new TableView
                                {
                                    Data = persons,
                                    InputValidator = (tvc, obj) => new Utils.Tuple<string, bool>("first", false)
                                };

            //try to set the name on the first column...should fail
            
            tableView.SetCellValue(0,0,"oeps");

            //assert the new value did not get commited
            Assert.AreEqual("Hoi", persons[0].Name);
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
            var selectionChangedCount = 0;
            var selectionChangedRowsCount = 0;
            var selectedRowChangedCount = 0;
            
            tableView.SelectedRowChanged += delegate
                                              {
                                                  selectedRowChangedCount++;
                                              };
            tableView.SelectionChanged += delegate(object sender, TableSelectionChangedEventArgs e)
                                              {
                                                  selectionChangedCount++;
                                                  selectionChangedRowsCount = e.Cells.Count;
                                              };
            //select cells
            tableView.SelectCells(5, 0, 9, 1);

            Assert.AreEqual(1, selectionChangedCount);
            Assert.AreEqual(10, selectionChangedRowsCount);

            var currentOld = tableView.CurrentRowObject;
            tableView.SelectedDisplayRowIndex = 1;
            var currentnew = tableView.CurrentRowObject;

            Assert.AreEqual(1, selectedRowChangedCount);
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
        [Category(TestCategory.WorkInProgress)]
        public void AddDataToViewUpdatesDatasource()
        {
            var data = new BindingList<Coordinate> {AllowEdit = true, AllowNew = true};
            var tableView = new TableView { Data = data};
            Assert.IsTrue(tableView.SetCellValue(0, 0, 1.ToString()));
            Assert.AreEqual(1, data.Count);
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
            Assert.AreEqual("Berend", tableView.GetCellValue(0, 0));

            //sort ascending..the first value should be aaltje
            tableView.Columns[0].SortOrder = SortOrder.Ascending;

            Assert.AreEqual("Aaltje", tableView.GetCellValue(0, 0));
        }

        [Test]
        [Category(TestCategory.WindowsForms)]
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
        [Category(TestCategory.WindowsForms)]
        public void ShowUnboundColumns()
        {
            var person = new List<Person>
                             {new Person {Age = 12, Name = "Aaltje"}, 
                              new Person {Age = 11, Name = "Berend"}};

            for (var i = 0; i < 10; i++)
            {
                person.Add(new Person { Age = 11, Name = "Berend" });
            }

            var tableView = new TableView
            {
                Data = person,
                ShowRowNumbers = true
            };
            var doubleColumn = tableView.AddUnboundColumn("double", typeof(double), 0, null);
            var buttonColumn = tableView.AddUnboundColumn("button", typeof(int), 2, CreateMessageBoxColumn());
            var comboColumn = tableView.AddUnboundColumn("combo", typeof(string), -1, GetFruit());
            tableView.UnboundColumnData += (o, s) =>
                                               {
                                                   if (s.Column == doubleColumn)
                                                   {
                                                       s.Value = s.Column;
                                                   }
                                                   else if (s.Column == buttonColumn)
                                                   {
                                                      if ( 0 == (s.Row % 2))
                                                       {
                                                           s.Value = 0;
                                                       }
                                                       else
                                                       {
                                                           s.Value = s.Column;
                                                       }
                                                   }
                                                   if (s.Column == comboColumn)
                                                   {
                                                       s.Value = FruitType.Peer.ToString();
                                                   }
                                               };

            WindowsFormsTestHelper.ShowModal(tableView);
        }

        private static void WaitForUi()
        {
            for (var i = 0; i < 5; i++)
            {
                Application.DoEvents();
                Thread.Sleep(50);
            }
        }

        [Test]
        [Category(TestCategory.WindowsForms)]
        public void PasteFunctionTableView()
        {
            IFunction person = new Function
            {
                Arguments = { new Variable<double>("eenmaal") },
                Components = { new Variable<double>("andermaal")}
            };

            person[1.0] = 1.5;
            person[2.0] = 3.0;

            var tableView = new TableView
            {
                ShowRowNumbers = true
            };
            var bindingList = new FunctionBindingList(person) { SynchronizeInvoke = tableView };
            tableView.Data = bindingList;

            var fields = tableView.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var gridControlField = fields.Where(p => p.FieldType.Name == "GridControl").FirstOrDefault();
            var gridControl = (GridControl)gridControlField.GetValue(tableView);

            Clipboard.SetText(string.Format("{0}\t{1}" + Environment.NewLine + "{2}\t{3}",
                                    1.0, 15,
                                    2.0, 30));

            WindowsFormsTestHelper.Show(tableView);

            gridControl.Focus();
            WaitForUi();
            var xtraGridContextMenu = new XtraGridContextMenu { SourceGrid = gridControl };
            xtraGridContextMenu.PasteClipboardContents();
            WaitForUi();

            Assert.AreEqual(1.0, tableView.GetCellValue(0, 0));
            Assert.AreEqual(15.0, tableView.GetCellValue(0, 1));
            Assert.AreEqual(2.0, tableView.GetCellValue(1, 0));
            Assert.AreEqual(30.0, tableView.GetCellValue(1, 1));
        }

        // Tests if adding unbound columns does not break pasting behaviour
        [Test]
        [Category(TestCategory.WindowsForms)]
        public void PasteFunctionTableViewUnboundColumn()
        {
            IFunction person = new Function
            {
                Arguments = { new Variable<double>("eenmaal") },
                Components = { new Variable<double>("andermaal") }
            };

            person[1.0] = 1.5;
            person[2.0] = 3.0;

            var tableView = new TableView
            {
                ShowRowNumbers = true
            };
            var bindingList = new FunctionBindingList(person) { SynchronizeInvoke = tableView };
            tableView.Data = bindingList;

            // add unbound column that shows value as string
            var stringColumn = tableView.AddUnboundColumn("string", typeof(string), 1, null);
            var text = new[] { "oneandahalluf", "three" };

            tableView.UnboundColumnData += (o, s) =>
            {
                if (s.Column == stringColumn)
                {
                    if ((s.Row >= 0) && (s.Row < text.Length))
                    {
                        if (s.IsGetData)
                        {
                            s.Value = text[s.Row];
                        }
                        else if (s.IsSetData)
                        {
                            text[s.Row] = (string)s.Value;
                        }
                    }
                }
            };


            var fields = tableView.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var gridControlField = fields.Where(p => p.FieldType.Name == "GridControl").FirstOrDefault();
            var gridControl = (GridControl)gridControlField.GetValue(tableView);


            Assert.AreEqual(3, tableView.Columns.Count);
            Assert.AreEqual(3, tableView.ColumnCount); // ??

            Clipboard.SetText(string.Format("{0}\t{1}\t{2}" + Environment.NewLine + "{3}\t{4}\t{5}",
                                    1.0, "fifteen", 15,
                                    2.0, "thirty", 30));

            WindowsFormsTestHelper.Show(tableView);

            gridControl.Focus();
            WaitForUi();
            var xtraGridContextMenu = new XtraGridContextMenu { SourceGrid = gridControl };
            xtraGridContextMenu.PasteClipboardContents();
            WaitForUi();
            // first test if bound columns are supported
            Assert.AreEqual(1.0, tableView.GetCellValue(0, 0));
            Assert.AreEqual(15.0, tableView.GetCellValue(0, 2));
            Assert.AreEqual(2.0, tableView.GetCellValue(1, 0));
            Assert.AreEqual(30.0, tableView.GetCellValue(1, 2));

            // unbound columns would be nice; depends on implementation tableView.UnboundColumnData
            // Assert.AreEqual("fifteen", tableView.GetCellValue(0, 1));
            // Assert.AreEqual("thirty", tableView.GetCellValue(1, 1));
        }

        static RepositoryItemButtonEdit CreateMessageBoxColumn()
        {
            var button = new RepositoryItemButtonEdit();
            button.Click += (s, o) => MessageBox.Show("DoMessageBox");
            button.TextEditStyle = TextEditStyles.Standard;
            button.Buttons[0].Kind = ButtonPredefines.Ellipsis;
            return button;
        }

        static RepositoryItemImageComboBox GetFruit()
        {
            var repositoryItemComboBox = new RepositoryItemImageComboBox();

            foreach (var value in Enum.GetValues(typeof(FruitType)))
            {
                // cast value to int to make databinding work
                repositoryItemComboBox.Items.Add(new ImageComboBoxItem(
                                                     Enum.GetName(typeof(FruitType), (int)value), (int)value, -1));
            }
            return repositoryItemComboBox;
        }

        [Test]
        [Category(TestCategory.WindowsForms)]
        public void KleurtjesTable()
        {
            var kleurtjes = new List<Color>();
            foreach (KnownColor knownColor in Enum.GetValues(typeof(KnownColor)))
            {
                kleurtjes.Add(Color.FromKnownColor(knownColor));
            }
            var tableView = new TableView
                                {
                                    Data = kleurtjes,
                                    BestFitMaxRowCount = -1,
                                    DisplayCellFilter = celStyle =>
                                                            {
                                                                if ((celStyle.RowIndex >= 0) && (!celStyle.Selected))
                                                                {
                                                                    celStyle.BackColor = kleurtjes[celStyle.RowIndex];
                                                                    return true;
                                                                }
                                                                return false;
                                                            }
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
        [Category(TestCategory.WindowsForms)]
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
        [Category(TestCategory.WindowsForms)]
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
            //XtraGridComboBoxHelper.Populate(comboBox, typeof(FruitType));
            comboBox.Items.Add((new ImageComboBoxItem("Piet", Convert.ToInt32(0), -1)));
            comboBox.Items.Add((new ImageComboBoxItem("Kees", Convert.ToInt32(1), -1)));

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
            tableView.SelectCells(0, 0, 0, 2);

            //action! delete the row
            tableView.DeleteCurrentSelection();

            //assert we only have berend now
            Assert.AreEqual(new[] { "Berend" }, persons.Select(p => p.Name).ToArray());
        }

        [Test]
        public void DeleteSelectionRemovesRowIfRowSelectedAndRowSelectIsEnabled()
        {
            var persons = new List<Person>
                             {new Person {Name = "Aaltje",Age = 12}, 
                              new Person {Name = "Berend",Age = 11 }};
            var tableView = new TableView { Data = persons };
            tableView.RowSelect = true;

            //select the top row
            tableView.SelectRow(0);

            //action! delete the row
            tableView.DeleteCurrentSelection();

            //assert we only have berend now
            Assert.AreEqual(new[] { "Berend" }, persons.Select(p => p.Name).ToArray());
        }

        [Test]
        [Category(TestCategory.WindowsForms)]
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
        [Category(TestCategory.WindowsForms)]
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
        [Category(TestCategory.WindowsForms)]
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

        /// <summary>
        /// The visual representation of readonly items should be identical for 
        /// cells, columns and table.
        /// </summary>
        [Test]
        [Category(TestCategory.WindowsForms)]
        public void ColumnReadOnlyShow()
        {
            
            var persons = new List<Person>();
            for (int i = 0; i < 10; i++)
            {
                persons.Add(new Person { Name = string.Format("user{0}", i), Age = 10 + 1 });
            }
            var tableView = new TableView
                                {
                                    Data = persons/*,
                                    //use some crazy colors to demonstrate this works
                                    ReadOnlyCellBackColor = Color.Pink, 
                                    ReadOnlyCellForeColor = Color.Blue*/
                                };
            tableView.Columns[0].ReadOnly = true;
            WindowsFormsTestHelper.ShowModal(tableView);
        }

        [Test]
        [Category(TestCategory.WindowsForms)]
        public void CellReadOnlyShow()
        {
            var persons = new List<Person>();
            for (int i=0; i< 10 ; i++)
            {
                persons.Add(new Person{ Name = string.Format("user{0}", i), Age = 10 + 1});
            }
            var tableView = new TableView
            {
                Data = persons,
                //use some crazy colors to demonstrate this works
                ReadOnlyCellBackColor = Color.Pink,
                ReadOnlyCellForeColor = Color.Blue
            };
            tableView.ReadOnlyCellFilter += cell => (0 == (cell.RowIndex % 2) ? false : true);
            WindowsFormsTestHelper.ShowModal(tableView);
        }

        [Test]
        [Category(TestCategory.WindowsForms)]
        public void TableReadOnlyShow()
        {
            var persons = new List<Person>();
            for (int i = 0; i < 10; i++)
            {
                persons.Add(new Person { Name = string.Format("user{0}", i), Age = 10 + 1 });
            }
            var tableView = new TableView
                                {
                                    Data = persons,
                                    ReadOnly = true
                                    /*ReadOnlyCellBackColor = Color.Pink,
                                    ReadOnlyCellForeColor = Color.Blue*/
                                };
            WindowsFormsTestHelper.ShowModal(tableView);
        }

        [Test]
        [Category(TestCategory.WindowsForms)]
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
        [Category(TestCategory.WindowsForms)]
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
            tableView.SetCellValue(0, 0, "2");
            Assert.AreEqual(1, callCount);
        }

        [Test]
        [Category(TestCategory.WindowsForms)]
        public void TableViewAllowsCreatingColumnsManually()
        {
            var persons = new List<Person>
                              {
                                  new Person {Name = "Aaltje", Age = 12},
                                  new Person {Name = "Berend", Age = 11}
                              };

            var tableView = new TableView {AutoGenerateColumns = false, Data = persons};


            tableView.AddColumn("Name", "Naam");
            tableView.AddColumn("DateOfBirth", "Verjaardag");
            
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
                                           //1000 ms is not really fast but better than no check at all
                                           TestHelper.AssertIsFasterThan(900, () =>
                                                                              function.SetValues(values,
                                                                                                 new VariableValueFilter
                                                                                                     <int>(
                                                                                                     function.Arguments[
                                                                                                         0], values)));
                                       };

            WindowsFormsTestHelper.ShowModal(tableView, onShown);
        }

        [Test, Category(TestCategory.WindowsForms)] 
        public void FormatStringsShouldWorkCorrectly()
        {
            var persons = new List<Person>
                              {
                                  new Person {Name = "Aaltje", Age = 12, DateOfBirth = new DateTime(1980,1,1)},
                                  new Person {Name = "Berend", Age = 11, DateOfBirth = new DateTime(1990,1,1)}
                              };

            var tableView = new TableView { ColumnAutoWidth = true, Data = persons };

            tableView.GetColumnByName("Name").DisplayFormat = "Name = {0}";
            tableView.GetColumnByName("Age").DisplayFormat = "D3";

            tableView.GetColumnByName("Name").DisplayFormat.Should().Be.EqualTo("Name = {0}");

            tableView.GetCellDisplayText(0, 0).Should().Be.EqualTo("Name = Aaltje");
            tableView.GetCellDisplayText(0, 1).Should().Be.EqualTo("012");

            WindowsFormsTestHelper.ShowModal(tableView);
        }


        [Test, Category(TestCategory.WindowsForms)]
        public void CustomFormattingShouldWorkCorrectly()
        {

            var persons = new List<Person>
                              {
                                  new Person {Name = "Aaltje", Age = 12, DateOfBirth = new DateTime(1980,1,1)},
                                  new Person {Name = "Berend", Age = 11, DateOfBirth = new DateTime(1990,1,1)}
                              };

            var tableView = new TableView { ColumnAutoWidth = true, Data = persons };

            var nameColumn = tableView.GetColumnByName("Name");

            nameColumn.DisplayFormat = "Name = {0}";
            nameColumn.CustomFormatter = new NameTableCellFormatter();

            tableView.GetCellDisplayText(0, 0).Should().Be.EqualTo("Name with custom formatter : Aaltje");
            
            WindowsFormsTestHelper.ShowModal(tableView);
        }


        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "Unable to select cells when tableView has RowSelect enabled. Use SelectRow instead.")]
        public void SelectCellsForTableViewWithRowSelectThrowsException()
        {
            //this test relates to issue 3069...demonstrating a problem paste lines when rowselect is enabled.
            //had to do with a  call to SelectCells for a table with RowSelect. Therefore tableView.gridView.GetSelectedCells() was no longer in synch with tablveView.SelectedCells
            //causing a error when pasting
            var persons = new List<Person>
                              {
                                  new Person {Name = "Aaltje", Age = 12, DateOfBirth = new DateTime(1980,1,1)},
                                  new Person {Name = "Berend", Age = 11, DateOfBirth = new DateTime(1990,1,1)}
                              };

            var tableView = new TableView { ColumnAutoWidth = true, Data = persons,RowSelect = true};

            //should throw because tableview is in RowSelect modus.
            tableView.SelectCells(0,0,0,1);
        }

        [Test]
        [Category(TestCategory.WindowsForms)]
        public void ShowWithRowSelect()
        {
            var persons = new List<Person>
                              {
                                  new Person {Name = "Aaltje", Age = 12, DateOfBirth = new DateTime(1980,1,1)},
                                  new Person {Name = "Berend", Age = 11, DateOfBirth = new DateTime(1990,1,1)}
                              };

            var tableView = new TableView { ColumnAutoWidth = true, Data = persons, RowSelect = true };
            
            WindowsFormsTestHelper.ShowModal(tableView);
        }


        [Test]
        public void BindFunctionSetsColumnType()
        {
            IFunction function = new Function();

            function.Arguments.Add(new Variable<int>("x"));
            function.Components.Add(new Variable<double>("y"));

            function[0] = 1.0;

            IFunctionBindingList functionBindingList = new FunctionBindingList(function);

            Assert.AreEqual(typeof(int), ((FunctionBindingListRow)functionBindingList[0])[0].GetType());
            Assert.AreEqual(typeof(double), ((FunctionBindingListRow)functionBindingList[0])[1].GetType());

            var tableView = new TableView();
            tableView.Data = functionBindingList;

            Assert.AreEqual(typeof(int), tableView.Columns[0].ColumnType);
            Assert.AreEqual(typeof(double), tableView.Columns[1].ColumnType);

        }

        [Test]
        [Category(TestCategory.WindowsForms)]
        public void ShowWithCheckbox()
        {
            var bools = new List<ClassWithBool>
                              {
                                  new ClassWithBool{Enabled= true},
                                  new ClassWithBool{Enabled= false}
                              };

            var tableView = new TableView { Data = bools};

            WindowsFormsTestHelper.ShowModal(tableView);
        }

        
        private class NameTableCellFormatter : ICustomFormatter
        {
            /// <summary>
            /// Converts the value of a specified object to an equivalent string representation using specified format and culture-specific formatting information.
            /// </summary>
            /// <returns>
            /// The string representation of the value of <paramref name="arg"/>, formatted as specified by <paramref name="format"/> and <paramref name="formatProvider"/>.
            /// </returns>
            /// <param name="format">A format string containing formatting specifications. 
            ///                 </param><param name="arg">An object to format. 
            ///                 </param><param name="formatProvider">An <see cref="T:System.IFormatProvider"/> object that supplies format information about the current instance. 
            ///                 </param><filterpriority>2</filterpriority>
            public string Format(string format, object arg, IFormatProvider formatProvider)
            {
                return "Name with custom formatter : " + arg;
            }
        }
    }
}
