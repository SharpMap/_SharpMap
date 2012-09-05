using System;
using System.Linq;
using System.Windows.Forms;
using DelftTools.Controls.Swf.Charting.Series;
using DelftTools.Functions;
using DelftTools.Functions.Binding;
using DelftTools.Functions.Filters;
using DelftTools.Functions.Generic;
using DelftTools.TestUtils;
using NUnit.Framework;

namespace DelftTools.Tests.Controls.Swf.Charting.Series
{
    [TestFixture]
    public class TeeChartSeriesDecoratorTest
    {
        [Test]
        [ExpectedException(typeof(ArgumentException),ExpectedMessage = "Invalid argument for series datasource. Are you passing IEnumerable? IList and IListSource are supported")]
        public void ThrowExceptionOnSettingInvalidDataSource()
        {
            ILineChartSeries lineChartSeries = ChartSeriesFactory.CreateLineSeries();
            lineChartSeries.DataSource = Enumerable.Range(1, 3);
        }

        [Test]
        [Category(TestCategory.WindowsForms)]
        public void ClearFunctionThatIsBoundToDecorator()
        {
            IFunction function = new Function("time series");

            function.Arguments.Add(new Variable<DateTime>("time"));
            function.Components.Add(new Variable<double>("water_discharge"));

            // set initial values
            DateTime time1 = DateTime.Now;
            DateTime time2 = time1.AddMinutes(1);
            DateTime time3 = time2.AddMinutes(1);
            function[time1] = 0.0;
            function[time2] = 1.0;
            function[time3] = 2.0;

            ILineChartSeries lineChartSeries = ChartSeriesFactory.CreateLineSeries();

            lineChartSeries.XValuesDataMember = function.Arguments[0].DisplayName;
            lineChartSeries.YValuesDataMember = function.Components[0].DisplayName;

            var control = new Control();
            WindowsFormsTestHelper.Show(control);

            var functionBindingList = new FunctionBindingList(function) { SynchronizeInvoke = control };
            lineChartSeries.DataSource = functionBindingList;
            
            function.Clear();

            
        }
        [Test]
        public void BindToFilteredFuntion()
        {
            //setup a 2D function and fix one dimension
            IFunction function = new Function();
            IVariable<int> x = new Variable<int>();

            function.Arguments.Add(x);
            function.Arguments.Add(new Variable<int>("Y"));
            function.Components.Add(new Variable<int>());

            function[0, 0] = 2;
            function[0, 1] = 3;
            function[1, 0] = 1;
            function[1, 1] = 4;

            IFunction filteredFunction = function.Filter(new VariableValueFilter<int>(x, 0));

            ILineChartSeries lineChartSeries = ChartSeriesFactory.CreateLineSeries();
            var variable = filteredFunction.Arguments[1];
            var component = filteredFunction.Components[0]; 
            
            lineChartSeries.XValuesDataMember = variable.DisplayName;
            lineChartSeries.YValuesDataMember = component.DisplayName;

            var functionBindingList = new FunctionBindingList(filteredFunction);
            lineChartSeries.DataSource = functionBindingList;
            

            Assert.AreEqual(2, lineChartSeries.XValues.Count);
            Assert.AreEqual(2, lineChartSeries.YValues.Count);
            Assert.AreEqual(0.0, lineChartSeries.XValues[0]);
            Assert.AreEqual(1.0, lineChartSeries.XValues[1]);
            Assert.AreEqual(2.0, lineChartSeries.YValues[0]);
            Assert.AreEqual(3.0, lineChartSeries.YValues[1]);
        }
    }
}