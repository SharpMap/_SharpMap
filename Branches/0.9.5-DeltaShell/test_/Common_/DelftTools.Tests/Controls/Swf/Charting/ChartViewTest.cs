using System;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using DelftTools.Controls.Swf.Charting;
using DelftTools.Controls.Swf.Charting.Series;
using DelftTools.Functions;
using DelftTools.Functions.Binding;
using DelftTools.Functions.Filters;
using DelftTools.Functions.Generic;
using DelftTools.TestUtils;
using log4net;
using log4net.Config;
using NUnit.Framework;
using Steema.TeeChart;
using Steema.TeeChart.Functions;
using Steema.TeeChart.Styles;
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
        private static readonly ILog log = LogManager.GetLogger(typeof(ChartViewTest));

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
            DataTable dataTable = new DataTable("ShowTableWithValues");
            dataTable.Columns.Add("Y", typeof (double));
            dataTable.Columns.Add("Z", typeof (double));
            dataTable.Columns.Add("n", typeof (float));

            DataRow row = dataTable.NewRow();
            row["Y"] = 0.0;
            row["Z"] = 0.0;
            row["n"] = 0.001;
            dataTable.Rows.Add(row);

            row = dataTable.NewRow();
            row["Y"] = 5.0;
            row["Z"] = 10.0;
            row["n"] = 0.001;
            dataTable.Rows.Add(row);

            row = dataTable.NewRow();
            row["Y"] = 6.0;
            row["Z"] = 15.0;
            row["n"] = 0.01;
            dataTable.Rows.Add(row);

            row = dataTable.NewRow();
            row["Y"] = 7.0;
            row["Z"] = 21.0;
            row["n"] = 0.01;
            dataTable.Rows.Add(row);

            //
            row = dataTable.NewRow();
            row["Y"] = 3.0;
            row["Z"] = 15.0;
            row["n"] = 0.01;
            dataTable.Rows.Add(row);

            row = dataTable.NewRow();
            row["Y"] = dataTable.Rows[0]["Y"];
            row["Z"] = dataTable.Rows[0]["Z"];
            row["n"] = dataTable.Rows[0]["n"]; 
            dataTable.Rows.Add(row);

            return dataTable;
        }
        
        [Test]
        [Category("Windows.Forms")]
        public void HidingPointWithZeroY()
        {
            double?[] X = new double?[] { 0.0, 1.0, 3.0, 4.0, 5.0, 7.0, 8.0} ;
            double?[] Y = new double?[] { 0.0, 1.0, 0.5, 0.0, 5.0, 3.0, 4.0 };

            IChartView view = new ChartView();

            ILineChartSeries series = ChartSeriesFactory.CreateLineSeries();
            series.NoDataValues.Add(1.0);
            series.Add(X, Y);
            view.Chart.Series.Add(series);
            //CustomPoint customPoint = ((TeeChartSeriesDecorator)((LineSeries) series).decorator).series;
            //customPoint.DefaultNullValue = 3.0;
            //customPoint.YValues[0] = 89;
            //customPoint.SetNull(0);
            ////customPoint.SetNull(3);
            ////customPoint.YValues[3] = 0;
            //customPoint.TreatNulls = TreatNullsStyle.DoNotPaint;

            WindowsFormsTestHelper.ShowModal((UserControl)view);
        }

        [Test]
        [Category("Windows.Forms")]
        public void AreaSeriesView()
        {
            ChartView chartView = new ChartView();

            ILineChartSeries series = ChartSeriesFactory.CreateLineSeries();

            series.DataSource = InitTable();
            series.XValuesDataMember = "Y";
            series.YValuesDataMember = "Z";
            chartView.Chart.Series.Add(series);
            
            
            WindowsFormsTestHelper windowsFormsTestHelper = new WindowsFormsTestHelper();

            windowsFormsTestHelper.ShowControlModal(chartView);
        }
        [Test]
        [Category("Windows.Forms")]
        public void DownSampledTimeSeries()
        {
            var random = new Random();
            ChartView chartView = new ChartView();
            var startTime = DateTime.Now;
            DateTime[] times = Enumerable.Range(1, 1000).Select(i => startTime.AddSeconds(i)).ToArray();
            double[] y = Enumerable.Range(1000, 1000).Select(i => Convert.ToDouble(random.Next(100))).ToArray();

            var pointSeries = new Points();
            var lineSeries = new Points();
            TChart chart = chartView.TeeChart;
            chart.Series.Add(pointSeries);
            chart.Series.Add(lineSeries);

            for (int i = 0; i < 1000;i++ )
            {
                pointSeries.Add(times[i], y[i]);
            }
                
            pointSeries.Active = false;

            lineSeries.DataSource = pointSeries;
            lineSeries.Function = new DownSampling(chart.Chart) {DisplayedPointCount = 4000,Method = DownSamplingMethod.Max};
            lineSeries.CheckDataSource();
            chart.Zoomed += delegate
                                {
                                    lineSeries.CheckDataSource();
                                };
            WindowsFormsTestHelper.ShowModal(chartView);
        }


        [Test]
        [Category("Windows.Forms")]
        public void ChangeYMemberSeriesView()
        {
            ChartView chartView = new ChartView();

            IChartSeries series = ChartSeriesFactory.CreateLineSeries();

            series.DataSource = InitTable();
            series.XValuesDataMember = "Y";
            series.YValuesDataMember = "Z";
            chartView.Chart.Series.Add(series);

            series.YValuesDataMember = "n";
            series.CheckDataSource();


            WindowsFormsTestHelper windowsFormsTestHelper = new WindowsFormsTestHelper();

            windowsFormsTestHelper.ShowControlModal(chartView);

        }

        [Test]
        [Category("Windows.Forms")]
        public void ChangeYMemberSeriesViewWithFunctionAsDataSource()
        {
            var function = new Function();
            var Y = new Variable<double>("Y");
            var Z = new Variable<double>("Z");
            var n = new Variable<double>("n");
            function.Arguments.Add(Y);
            function.Components.Add(Z);
            function.Components.Add(n);

            Y.SetValues(new[] { 0.0, 5.0, 6.0, 7.0, 3.0 });
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

            DataTable dataTable = new DataTable("sinandcosinus");
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
        [Category("Windows.Forms")]
        public void MultipleSeriesView()
        {
            IChart chart = CreateMultipleSeriesChart();
            IChartView chartView = new ChartView { Chart = chart };
            WindowsFormsTestHelper windowsFormsTestHelper = new WindowsFormsTestHelper();
            windowsFormsTestHelper.ShowControlModal((ChartView)chartView);
        }

        [Test]
        [Category("Windows.Forms")]
        public void MultipleSeriesExtraAxesView()
        {
            IChart chart = CreateMultipleSeriesChart();
            IChartSeries chartSeries = chart.Series[0];
            chartSeries.HorizAxis = HorizontalAxis.Top;
            chartSeries.VertAxis = VerticalAxis.Right;

            IChartView chartView = new ChartView { Chart = chart };
            WindowsFormsTestHelper windowsFormsTestHelper = new WindowsFormsTestHelper();
            windowsFormsTestHelper.ShowControlModal((ChartView)chartView);
        }

        [Test]
        [Category("Windows.Forms")]
        public void MultipleSeriesChartViaImage()
        {
            PictureBox pictureBox = new PictureBox();
            IChart chart = CreateMultipleSeriesChart();
            pictureBox.Image = chart.Image(300, 300);
            WindowsFormsTestHelper windowsFormsTestHelper = new WindowsFormsTestHelper();
            windowsFormsTestHelper.ShowControlModal(pictureBox);
        }

        [Test]
        [Category(TestCategory.Performance)]
        public void RefreshShouldBeFastWhenFunctionDataSourceHasManyChanges()
        {
            IFunction function = new Function
            {
                Arguments = { new Variable<int>("x") },
                Components = { new Variable<int>("f") }
            };

            var values = new int[1000];
            for (var i = 0; i < values.Length; i++)
            {
                values[i] = i;
            }

            var chartView = new ChartView();

            var lineSeries = ChartSeriesFactory.CreateLineSeries();

            var functionBindingList = new FunctionBindingList(function) { SynchronizeInvoke = chartView};
            lineSeries.DataSource = functionBindingList;
            
            lineSeries.XValuesDataMember = function.Arguments[0].DisplayName;
            lineSeries.YValuesDataMember = function.Components[0].DisplayName;
            chartView.Chart.Series.Add(lineSeries);
            lineSeries.PointerVisible = false;

            // now do the same when table view is shown
            Action<Form> onShown = delegate
                                       {
                                           var stopwatch = new Stopwatch();
                stopwatch.Reset();
                stopwatch.Start();
                SetFunctionValuesWrappedWithChartView(function, values);
                stopwatch.Stop();

                log.DebugFormat("Refreshing chart while inserting values into function took {0}ms", stopwatch.ElapsedMilliseconds);

                stopwatch.ElapsedMilliseconds.Should("Insert of 1k values should take < 100ms").Be.LessThan(300);
            };

            WindowsFormsTestHelper.ShowModal(chartView, onShown);
        }

        private void SetFunctionValuesWrappedWithChartView(IFunction function, int[] values)
        {
            function.SetValues(values, new VariableValueFilter<int>(function.Arguments[0], values));
        }

    }
}