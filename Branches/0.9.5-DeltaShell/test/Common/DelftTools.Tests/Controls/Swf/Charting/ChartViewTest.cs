using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using DelftTools.Controls.Swf.Charting;
using DelftTools.Controls.Swf.Charting.Series;
using DelftTools.Functions;
using DelftTools.Functions.Binding;
using DelftTools.Functions.Filters;
using DelftTools.Functions.Generic;
using DelftTools.TestUtils;
using DelftTools.Utils;
using NUnit.Framework;
using Chart=DelftTools.Controls.Swf.Charting.Chart;
using Function=DelftTools.Functions.Function;
using HorizontalAxis=DelftTools.Controls.Swf.Charting.HorizontalAxis;
using IChart=DelftTools.Controls.Swf.Charting.IChart;
using VerticalAxis=DelftTools.Controls.Swf.Charting.VerticalAxis;

namespace DelftTools.Tests.Controls.Swf.Charting
{
    [TestFixture]
    public class ChartViewTest
    {
        #region Setup/Teardown

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
        

        #endregion

        private static DataTable InitTable()
        {
            var dataTable = new DataTable("ShowTableWithValues");
            dataTable.Columns.Add("Y", typeof (double));
            dataTable.Columns.Add("Z", typeof (double));
            dataTable.Columns.Add("n", typeof (float));

            var y = new[] {  0.0,   5.0,  6.0,  7.0,  3.0,   0.0};
            var z = new[] {  0.0,  10.0, 15.0, 21.0, 15.0,   0.0};
            var n = new []{0.001, 0.001, 0.01, 0.01, 0.01, 0.001};

            for (int i = 0; i < 6; i++)
            {
                var row = dataTable.NewRow();

                row["Y"] = y[i];
                row["Z"] = z[i];
                row["n"] = n[i];

                dataTable.Rows.Add(row);
            }

            return dataTable;
        }
        
        [Test]
        [Category(TestCategory.WindowsForms)]
        public void HidingPointWithZeroY()
        {
            var view = new ChartView();
            var series = ChartSeriesFactory.CreateLineSeries();
            
            series.NoDataValues.Add(1.0);

            series.Add(new double?[] { 0.0, 1.0, 3.0, 4.0, 5.0, 7.0, 8.0}, 
                       new double?[] { 0.0, 1.0, 0.5, 0.0, 5.0, 3.0, 4.0 });
            
            view.Chart.Series.Add(series);

            WindowsFormsTestHelper.ShowModal(view);
        }

        [Test]
        [Category(TestCategory.WindowsForms)]
        public void AreaSeriesView()
        {
            var chartView = new ChartView();

            var series = ChartSeriesFactory.CreateLineSeries();

            series.DataSource = InitTable();
            series.XValuesDataMember = "Y";
            series.YValuesDataMember = "Z";
            
            chartView.Chart.Series.Add(series);
            
            WindowsFormsTestHelper.ShowModal(chartView);
        }

        [Test]
        [Category(TestCategory.WindowsForms)]
        public void SeriesBandToolView()
        {
           var chartView = new ChartView();

            var dataTable1 = new DataTable();
            var dataTable2 = new DataTable();

            dataTable1.Columns.AddRange(new []
                                            {
                                                new DataColumn("Y", typeof(double)),
                                                new DataColumn("Z", typeof(double))
                                            });
            dataTable2.Columns.AddRange(new[]
                                            {
                                                new DataColumn("Y", typeof(double)),
                                                new DataColumn("Z", typeof(double))
                                            });

            var ySeries1 = new[] {0.0, 2.0,   5.0,  10.0, 13.0, 15.0};
            var zSeries1 = new[] {0.0, 0.0, -10.0, -10.0,  0.0,  0.0};
            var ySeries2 = new[] {0.0, 5.0,  5.0, 10.0, 10.0, 15.0};
            var zSeries2 = new[] {1.0, 1.0, -9.0, -9.0,  1.0,  1.0};

            for (int i = 0; i < ySeries1.Length; i++)
            {
                var row = dataTable1.NewRow();
                row["Y"] = ySeries1[i];
                row["Z"] = zSeries1[i];
                dataTable1.Rows.Add(row);
            }

            for (int i = 0; i < ySeries2.Length; i++)
            {
                var row = dataTable2.NewRow();
                row["Y"] = ySeries2[i];
                row["Z"] = zSeries2[i];
                dataTable2.Rows.Add(row);
            }

            var series1 = ChartSeriesFactory.CreateLineSeries();
            var series2 = ChartSeriesFactory.CreateLineSeries();

            series1.DataSource = dataTable1;
            series1.XValuesDataMember = "Y";
            series1.YValuesDataMember = "Z";
            
            series2.DataSource = dataTable2;
            series2.XValuesDataMember = "Y";
            series2.YValuesDataMember = "Z";
            
            chartView.Chart.Series.AddRange(new []{series1, series2});
            
            //tool
            var tool = chartView.NewSeriesBandTool(series1, series2, Color.Yellow, HatchStyle.BackwardDiagonal,Color.Red);
            chartView.Tools.Add(tool);

            WindowsFormsTestHelper.ShowModal(chartView);
        }

        [Test]
        [Category(TestCategory.WindowsForms)]
        public void ChangeYMemberSeriesView()
        {
            var chartView = new ChartView();

            var series = ChartSeriesFactory.CreateLineSeries();

            series.DataSource = InitTable();
            series.XValuesDataMember = "Y";
            series.YValuesDataMember = "Z";
            
            chartView.Chart.Series.Add(series);

            series.YValuesDataMember = "n";
            series.CheckDataSource();

            WindowsFormsTestHelper.ShowModal(chartView);
        }

        [Test]
        [Category(TestCategory.WindowsForms)]
        public void ChangeYMemberSeriesViewWithFunctionAsDataSource()
        {
            var function = new Function();
            var Y = new Variable<double>("Y");
            var Z = new Variable<double>("Z");
            var n = new Variable<double>("n");

            function.Arguments.Add(Y);
            function.Components.Add(Z);
            function.Components.Add(n);

            Y.SetValues(new[] { 0.0,  3.0 ,5.0, 6.0, 7.0});
            Z.SetValues(new[] { 0.0, 10.0, 15.0, 21.0, 15.0 });
            n.SetValues(new[] { 0.001, 0.001, 0.01, 0.01, 0.01 });

            var chartView = new ChartView();

            IChartSeries series = ChartSeriesFactory.CreateLineSeries();
            series.XValuesDataMember = Y.DisplayName;
            series.YValuesDataMember = Z.DisplayName;
            series.DataSource = new FunctionBindingList(function) { SynchronizeInvoke = chartView};
            chartView.Chart.Series.Add(series);

            WindowsFormsTestHelper.ShowModal(chartView);
        }

        private static IChart CreateMultipleSeriesChart()
        {
            IChart chart = new Chart();

            var dataTable = new DataTable("sinandcosinus");
            dataTable.Columns.Add("X", typeof(double));
            dataTable.Columns.Add("sin", typeof(double));
            dataTable.Columns.Add("cos", typeof(double));

            const double pi2 = Math.PI * 2;

            for (int i = 0; i < 100; i++)
            {
                double angle = i * (pi2 / 100);
                DataRow row = dataTable.NewRow();
                row["X"] = angle;
                row["sin"] = Math.Sin(angle);
                row["cos"] = Math.Cos(angle);
                dataTable.Rows.Add(row);
            }

            ILineChartSeries sin = ChartSeriesFactory.CreateLineSeries();
            sin.Title = "sinus";
            sin.DataSource = dataTable;
            sin.XValuesDataMember = "X";
            sin.YValuesDataMember = "sin";
            chart.Series.Add(sin);
            sin.BrushColor = Color.Red;
            sin.PointerVisible = false;

            ILineChartSeries cos = ChartSeriesFactory.CreateLineSeries();
            cos.Title = "cosinus";
            cos.DataSource = dataTable;
            cos.XValuesDataMember = "X";
            cos.YValuesDataMember = "cos";
            chart.Series.Add(cos);
            cos.BrushColor = Color.Blue;
            cos.PointerVisible = false;

            chart.Legend.Visible = true;
            return chart;
        }

        [Test]
        [Category(TestCategory.WindowsForms)]
        public void MultipleSeriesView()
        {
            WindowsFormsTestHelper.ShowModal(new ChartView
                                                 {
                                                     Chart = CreateMultipleSeriesChart()
                                                 });
        }

        [Test]
        [Category(TestCategory.WindowsForms)]
        public void ShowDisabledChartView()
        {
            WindowsFormsTestHelper.ShowModal(new ChartView
                                                 {
                                                     Chart = CreateMultipleSeriesChart(),
                                                     Enabled = false
                                                 });
        }
        
        [Test]
        [Category(TestCategory.WindowsForms)]
        public void MultipleSeriesExtraAxesView()
        {
            var chart = CreateMultipleSeriesChart();
            var chartSeries = chart.Series[0];
            
            chartSeries.HorizAxis = HorizontalAxis.Top;
            chartSeries.VertAxis = VerticalAxis.Right;

            WindowsFormsTestHelper.ShowModal(new ChartView { Chart = chart });
        }

        [Test]
        [Category(TestCategory.WindowsForms)]
        public void MultipleSeriesChartViaImage()
        {
            WindowsFormsTestHelper.ShowModal(new PictureBox
                                                 {
                                                     Image = CreateMultipleSeriesChart().Image(300, 300)
                                                 });
        }

        [Test]
        [Category(TestCategory.Performance)]
        public void RefreshShouldBeFastWhenFunctionDataSourceHasManyChanges()
        {
            var random = new Random();
            IFunction function = new Function
            {
                Arguments = { new Variable<int>("x") },
                Components = { new Variable<int>("f") }
            };

            int count = 1000;
            var componentvalues = Enumerable.Range(1, count).Select(i => random.Next(100)).ToArray();
            var argumentvalues = Enumerable.Range(1, count).ToArray();

            var chartView = new ChartView();

            var lineSeries = ChartSeriesFactory.CreateLineSeries();

            var functionBindingList = new FunctionBindingList(function) { SynchronizeInvoke = chartView};
            lineSeries.DataSource = functionBindingList;
            //don't update on every change...
            lineSeries.UpdateASynchronously = true;
            lineSeries.XValuesDataMember = function.Arguments[0].DisplayName;
            lineSeries.YValuesDataMember = function.Components[0].DisplayName;
            chartView.Chart.Series.Add(lineSeries);
            lineSeries.PointerVisible = false;

            // call one time to make sure that internal HACK TypeUtils.CallGeneric is done, otherwise timing varies a lot
            function.SetValues(componentvalues, new VariableValueFilter<int>(function.Arguments[0], argumentvalues));
            function.Arguments[0].Clear();

            // now do the same when table view is shown
            Action<Form> onShown = delegate
                                       {
                                           //the slowdown of chart is absolute minimal
                                           TestHelper.AssertIsFasterThan(50, ()=>
                                           function.SetValues(componentvalues, new VariableValueFilter<int>(function.Arguments[0], argumentvalues)));
                                       };

            WindowsFormsTestHelper.ShowModal(chartView, onShown);
        }

        [Test]
        [Category(TestCategory.WindowsForms)]
        public void ApplyCustomDateTimeFormatSeconds()
        {
            var random = new Random();
            var chartView = new ChartView();
            var startTime = DateTime.Now;
            
            var times = Enumerable.Range(1, 1000).Select(i => startTime.AddSeconds(i)).ToArray();
            var y = Enumerable.Range(1000, 1000).Select(i => Convert.ToDouble(random.Next(100))).ToArray();

            var pointList = new List<Tuple<DateTime, double>>();
            var lineSeries = ChartSeriesFactory.CreateLineSeries();
            var chart = chartView.Chart;

            chart.Series.Add(lineSeries);

            for (int i = 0; i < 1000; i++)
            {
                pointList.Add(new Tuple<DateTime, double>(times[i], y[i]));
            }

            lineSeries.DataSource = pointList;
            lineSeries.XValuesDataMember = "First";
            lineSeries.YValuesDataMember = "Second";
            lineSeries.CheckDataSource();
            WindowsFormsTestHelper.ShowModal(chartView);
        }

        [Test]
        [Category(TestCategory.WindowsForms)]
        public void ApplyCustomDateTimeFormatYears()
        {
            var random = new Random();
            var chartView = new ChartView();
            var startTime = DateTime.Now;
            
            var times = Enumerable.Range(1, 1000).Select(startTime.AddYears).ToArray();
            var y = Enumerable.Range(1000, 1000).Select(i => Convert.ToDouble(random.Next(100))).ToArray();

            var pointList = new List<Utils.Tuple<DateTime, double>>();
            var lineSeries = ChartSeriesFactory.CreateLineSeries();
            var chart = chartView.Chart;

            chart.Series.Add(lineSeries);

            chartView.DateTimeLabelFormatProvider = new QuarterNavigatableLabelFormatProvider();

            for (int i = 0; i < 1000; i++)
            {
                pointList.Add(new Tuple<DateTime, double>(times[i], y[i]));
            }

            lineSeries.DataSource = pointList;
            lineSeries.XValuesDataMember = "First";
            lineSeries.YValuesDataMember = "Second";
            lineSeries.CheckDataSource();
            WindowsFormsTestHelper.ShowModal(chartView);
        }
    }
}