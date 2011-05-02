using System.Threading;
using System.Windows.Forms;
using DelftTools.Controls.Swf.Charting.Series;
using DelftTools.Functions;
using DelftTools.Functions.Binding;
using DelftTools.Functions.Generic;
using DelftTools.TestUtils;
using NUnit.Framework;

namespace DelftTools.Tests.Controls.Swf.Charting
{
    [TestFixture]
    public class SeriesTest
    {
        [SetUp]
        public void SetUpFixture()
        {
            LogHelper.ConfigureLogging();
        }

        [Test]
        public void SeriesChangesWhenFunctionBindingListChanges()
        {
            IFunction function = new Function();
            IVariable yVariable = new Variable<double>("y");
            IVariable xVariable = new Variable<double>("x");
            function.Arguments.Add(xVariable);
            function.Components.Add(yVariable);
            
            function[1.0] = 2.0;
            function[2.0] = 5.0;
            function[3.0] = 1.0;

            ILineChartSeries ls = ChartSeriesFactory.CreateLineSeries();
            ls.YValuesDataMember = function.Components[0].DisplayName;
            ls.XValuesDataMember = function.Arguments[0].DisplayName;

            var synchronizeObject = new Control();
            synchronizeObject.Show();

            var functionBindingList = new FunctionBindingList(function) { SynchronizeInvoke = synchronizeObject };
            ls.DataSource = functionBindingList;
            
            //a change in the function should change the series
            function[1.0] = 20.0;

            while(functionBindingList.IsProcessing)
            {
                Application.DoEvents();
            }
            
            Assert.AreEqual(20, ls.YValues[0]);
            
        }
    }
}