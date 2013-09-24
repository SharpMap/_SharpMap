using System.Linq;
using DelftTools.TestUtils;
using GeoAPI.Extensions.Feature;
using NUnit.Framework;
using SharpMap.Extensions.Data.Providers;

namespace SharpMap.Extensions.Tests.Data.Providers
{
    [TestFixture]
    public class LdbFileTest
    {
        [Test]
        public void ReadZeelandLdb()
        {
            var zeelandLdb = new LdbFile();
            zeelandLdb.Open(TestHelper.GetTestFilePath("zeeland.ldb"));
            Assert.AreEqual(3824, zeelandLdb.Features.Count);
            Assert.AreEqual(zeelandLdb.Features.Count, zeelandLdb.GetFeatureCount());
            Assert.AreEqual(zeelandLdb.Features.Count, zeelandLdb.GetFeatures(zeelandLdb.GetExtents()).Count());

            var firstFeature = (IFeature) zeelandLdb.Features[0];
            Assert.AreEqual(2, firstFeature.Geometry.Coordinates.Count());
            Assert.AreEqual(71180.0, firstFeature.Geometry.Coordinates[0].X);

            var lastFeature = zeelandLdb.Features.OfType<IFeature>().Last();
            Assert.AreEqual(2, lastFeature.Geometry.Coordinates.Count());
            Assert.AreEqual(73610.2, lastFeature.Geometry.Coordinates[0].X);
            Assert.AreEqual(376726.0, lastFeature.Geometry.Coordinates[0].Y);
        }

        [Test]
        public void ReadHarlingenLdb()
        {
            var harlingenLdb = new LdbFile();
            harlingenLdb.Open(TestHelper.GetTestFilePath("Harlingen_haven.ldb"));
            
            Assert.AreEqual(1, harlingenLdb.Features.Count);
            
            var firstFeature = (IFeature)harlingenLdb.Features[0];
            Assert.AreEqual(362, firstFeature.Geometry.Coordinates.Count());
            Assert.AreEqual(156431.364520612, firstFeature.Geometry.Coordinates[0].X);
        }
    }
}