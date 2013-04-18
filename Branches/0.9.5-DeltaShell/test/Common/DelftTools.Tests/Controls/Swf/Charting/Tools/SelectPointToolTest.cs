using System;
using System.Collections;
using System.Windows.Forms;
using DelftTools.Controls.Swf.Charting;
using DelftTools.Controls.Swf.Charting.Series;
using DelftTools.Controls.Swf.Charting.Tools;
using DelftTools.Functions;
using DelftTools.Functions.Binding;
using DelftTools.Functions.Generic;
using DelftTools.TestUtils;
using DelftTools.Utils.Reflection;
using NUnit.Framework;
using Steema.TeeChart.Tools;

namespace DelftTools.Tests.Controls.Swf.Charting.Tools
{
    [TestFixture]
    public class SelectPointToolTest
    {
        private ChartView chartView;

        [Test]
        [Category(TestCategory.WindowsForms)]
        public void Add()
        {
            var chart = new Chart();

            var lineSeries = chart.NewLineSeries();

            lineSeries.DataSource = new ArrayList
                                        {
                                            new {X = 1, Y = 1},
                                            new {X = 2, Y = 3},
                                            new {X = 3, Y = 2},
                                            new {X = 4, Y = 4}
                                        };
            lineSeries.XValuesDataMember = "X";
            lineSeries.YValuesDataMember = "Y";

            chart.Series.Add(lineSeries);
            chartView = new ChartView { Chart = chart };
            chartView.NewSelectPointTool();
            //chartView.NewAddPointTool();
            WindowsFormsTestHelper.ShowModal(chartView);
        }      
    }
}