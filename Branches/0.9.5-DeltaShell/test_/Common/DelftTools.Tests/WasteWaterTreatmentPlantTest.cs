using DelftTools.Hydro;
using DelftTools.TestUtils;
using GisSharpBlog.NetTopologySuite.Geometries;
using NUnit.Framework;

namespace DelftTools.Tests
{
    [TestFixture]
    public class WasteWaterTreatmentPlantTest
    {
        [Test]
        public void Clone()
        {
            var wwtp = new WasteWaterTreatmentPlant {Geometry = new Point(15, 15), Name = "aa", LongName = "bb", Network = new HydroNetwork()};
            wwtp.Attributes.Add("Milage",15);

            var clone = wwtp.Clone();

            ReflectionTestHelper.AssertPublicPropertiesAreEqual(wwtp, clone);
        }
    }
}