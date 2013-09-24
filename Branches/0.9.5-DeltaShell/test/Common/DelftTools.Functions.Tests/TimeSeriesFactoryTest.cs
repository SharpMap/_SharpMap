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
            Assert.AreEqual("m AD", waterLevelSeries.Components[0].Unit.Symbol);
        }
    }
}
