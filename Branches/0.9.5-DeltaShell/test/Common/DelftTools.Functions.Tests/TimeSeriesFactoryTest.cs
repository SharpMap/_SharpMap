using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace DelftTools.Functions.Tests
{
    [TestFixture]
    public class TimeSeriesFactoryTest
    {
        [Test]
        public void CanCreateWaterLevelSeries()
        {
            var waterLevelSeries = TimeSeriesFactory.CreateWaterLevelTimeSeries();

            Assert.IsTrue(waterLevelSeries.IsWaterLevelSeries());
            Assert.AreEqual("m AD", waterLevelSeries.Components[0].Unit.Symbol);
        }
    }
}
