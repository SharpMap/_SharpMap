using System;
using System.Drawing;
using System.Linq;
using DelftTools.Controls.Swf.Charting.Tools;
using DelftTools.Controls.Swf.TimeNavigation;
using DelftTools.TestUtils;
using DelftTools.Utils;
using DelftTools.Utils.Reflection;
using log4net;
using NUnit.Framework;

namespace DelftTools.Tests.Controls.Swf.TimeNavigation
{
    [TestFixture]
    public class TimeNavigationChartTest
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(TimeNavigationChartTest));

        [Test]
        [Category(TestCategory.WindowsForms)]
        public void ShowWithTimes()
        {
            var timeNavigationChart = new TimeNavigationChart
                                          {
                                              Times = Enumerable.Range(2010, 10).Select(i => new DateTime(i, 1, 1))
                                          };
            WindowsFormsTestHelper.ShowModal(timeNavigationChart);

        }

        [Test]
        [Category(TestCategory.WindowsForms)]
        public void ShowWithTimesAndValues()
        {
            var timeNavigationChart = new TimeNavigationChart
                                          {
                                              //1000 jaar + sin(jaar)
                                              TimesAndValues =
                                                  Enumerable.Range(1000, 1000).Select(
                                                      i =>
                                                      new Utils.Tuple<DateTime, double>(new DateTime(i, 1, 1),
                                                                                  Math.Sin(2 * 3.14 * i / 360)))
                                          };

            WindowsFormsTestHelper.ShowModal(timeNavigationChart);
        }

        [Test]
        public void SettingTimesChangesViewMode()
        {
            var timeNavigationChart = new TimeNavigationChart
                                          {
                                              ViewMode = ViewMode.TimesAndValues
                                          };
            //setting viewmode should work
            Assert.AreEqual(ViewMode.TimesAndValues, timeNavigationChart.ViewMode);

            //setting Times should change the viewmode 
            timeNavigationChart.Times = new[] { new DateTime(2000, 1, 1), };
            Assert.AreEqual(ViewMode.Times, timeNavigationChart.ViewMode);

            //setting TimesAndValues should change the viewmode as well
            timeNavigationChart.TimesAndValues = new[] { new DateTime(2000, 1, 1), }.Select(t => new Utils.Tuple<DateTime, double>(t, 0.0));
            Assert.AreEqual(ViewMode.TimesAndValues, timeNavigationChart.ViewMode);
        }

        [Test]
        [Category(TestCategory.WindowsForms)]
        public void ShowWithSinglePointSelection()
        {
            var timeNavigationChart = new TimeNavigationChart
            {
                Times = Enumerable.Range(2010, 10).Select(i => new DateTime(i, 1, 1)),
                SelectionMode = TimeSelectionMode.Single
            };
            WindowsFormsTestHelper.ShowModal(timeNavigationChart);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "Can not set SelectionEnd while TimeSelectionMode is Single. Switch to Range first")]
        public void SettingSelectionEndWithSelectionModeSingleGivesException()
        {
            var timeNavigationChart = new TimeNavigationChart
            {
                Times = Enumerable.Range(2010, 20).Select(i => new DateTime(i, 1, 1)),
                SelectionMode = TimeSelectionMode.Single
            };
            timeNavigationChart.SelectionEnd = new DateTime(2011,1,1);
        }
    }
}