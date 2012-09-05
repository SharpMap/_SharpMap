using System.Drawing;
using DelftTools.Controls.Swf.Charting;
using DelftTools.Controls.Swf.Charting.Series;
using DelftTools.Functions;
using DelftTools.Functions.Binding;
using DelftTools.Functions.Generic;
using DelftTools.TestUtils;
using NUnit.Framework;

namespace DelftTools.Tests.Controls.Swf.Charting.Tools
{
    [TestFixture]
    public class BandToolTest
    {
        [Test]
        [Category("Windows.Forms")]
        public void Show()
        {
            IFunction function1 = GetFunction1Arg1Comp();
            IFunction function2 = GetFunction1Arg1Comp();
            IFunction function3 = GetFunction1Arg1Comp();

            function1[1.0] = 2.0;
            function1[2.0] = 5.0;
            function1[3.0] = 1.0;

            function2[1.0] = 20.0;
            function2[2.0] = 50.0;
            function2[3.0] = 10.0;

            function3[3.0] = 20;
            function3[4.0] = 50;

            var view = new ChartView();

            ILineChartSeries lineSeries1 = GetLineSeries(function1, view);
            ILineChartSeries lineSeries2 = GetLineSeries(function2, view);
            ILineChartSeries lineSeries3 = GetLineSeries(function3, view);

            view.Chart.Series.Add(lineSeries1);
            view.Chart.Series.Add(lineSeries2);
            view.Chart.Series.Add(lineSeries3);

            var tool = view.NewSeriesBandTool(lineSeries1, lineSeries2, Color.Green);

            WindowsFormsTestHelper.ShowModal(view);
        }

        private static ILineChartSeries GetLineSeries(IFunction function, ChartView view)
        {
            ILineChartSeries ls = ChartSeriesFactory.CreateLineSeries();
            ls.YValuesDataMember = function.Components[0].DisplayName;
            ls.XValuesDataMember = function.Arguments[0].DisplayName;

            var functionBindingList = new FunctionBindingList(function) { SynchronizeInvoke = view};
            ls.DataSource = functionBindingList;
            return ls;
        }

        private static IFunction GetFunction1Arg1Comp()
        {
            IFunction function = new Function();
            IVariable yVariable = new Variable<double>("y");
            IVariable xVariable = new Variable<double>("x");
            function.Arguments.Add(xVariable);
            function.Components.Add(yVariable);
            return function;
        }
    }
}