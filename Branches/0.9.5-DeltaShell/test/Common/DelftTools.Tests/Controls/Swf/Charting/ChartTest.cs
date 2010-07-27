using System.Collections;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using DelftTools.Controls.Swf.Charting;
using DelftTools.Controls.Swf.Charting.Series;
using DelftTools.Functions;
using DelftTools.Functions.Binding;
using DelftTools.Functions.Generic;
using DelftTools.TestUtils;
using NUnit.Framework;

namespace DelftTools.Tests.Controls.Swf.Charting
{
    [TestFixture]
    public class ChartTest
    {
        [Test]
        [Category("Windows.Forms")]
        public void ChartWithDataTableAsSeriesSource()
        {
            var table = new DataTable();
            table.Columns.Add("x", typeof (double));
            table.Columns.Add("y", typeof (double));

            table.Rows.Add(2.5, 33.3);
            table.Rows.Add(0.5, 13.3);

            // create chart and add function as a data source using object adapter class FunctionSeriesDataSource
            //IChart chart = new Chart();

            IChartSeries series = ChartSeriesFactory.CreateLineSeries();

            series.DataSource = table;
            series.XValuesDataMember = "x";
            series.YValuesDataMember = "y";

            var chartView1 = new ChartView() ;

            chartView1.Chart.Series.Add(series);

            var form = new Form {Width = 600, Height = 100};
            form.Controls.Add(chartView1);
            WindowsFormsTestHelper.ShowModal(form);
        }

        [Test]
        [Category("Windows.Forms")]
        public void ChartWithFunctionAsSeriesSource()
        {
            // create function

            var fn = new Function();
            var x = new Variable<double>("x");
            var y = new Variable<double>("y");
            fn.Arguments.Add(x);
            fn.Components.Add(y);

            fn[2.5] = 33.3;
            fn[0.5] = 13.3;
            fn[1.5] = 4.0;

            var chartView1 = new ChartView();// { Data = chart };

            // create chart and add function as a data source using object adapter class FunctionSeriesDataSource
            // var chart = new Chart();

            ILineChartSeries series = ChartSeriesFactory.CreateLineSeries();
            series.DataSource = new FunctionBindingList(fn) { SynchronizeInvoke = chartView1};
            series.XValuesDataMember = x.DisplayName;
            series.YValuesDataMember = y.DisplayName;

            //chart.Series.Add(series);

            // TODO: add asserts
            // TODO: make all windows forms public from WindowsFormsTest which shows and hides forms on build server
            
            // show form
            chartView1.Chart.Series.Add(series);

            // set colors afterwards. TChart changes colors when adding a series to ChartView
            series.LinePenColor = Color.Black;

            //var form = new Form {Width = 600, Height = 100};
            //form.Controls.Add(chartView1);
            WindowsFormsTestHelper.ShowModal(chartView1);
        }

        [Test]
        [Category("Windows.Forms")]
        public void ChartWithObjectsAsSeriesSource()
        {
            IList objects = new ArrayList
                                {
                                    new {X = 2.5, Y = 33.3}, 
                                    new {X = 0.5, Y = 13.3}
                                };

            // create chart and add function as a data source using object adapter class FunctionSeriesDataSource

            IChartSeries series = ChartSeriesFactory.CreateLineSeries();
            series.DataSource = objects;
            series.XValuesDataMember = "X";
            series.YValuesDataMember = "Y";

            var chartView1 = new ChartView();

            chartView1.Chart.Series.Add(series);

            // show form
            var form = new Form {Width = 600, Height = 100};
            form.Controls.Add(chartView1);
            WindowsFormsTestHelper.ShowModal(form);
        }

        [Test]
        public void CreateChartImage()
        {
            IChart chart = new Chart();
            Image image = chart.Image(300, 200);
            Assert.AreEqual(300, image.Width);
            Assert.AreEqual(200, image.Height);
        }
    }
}