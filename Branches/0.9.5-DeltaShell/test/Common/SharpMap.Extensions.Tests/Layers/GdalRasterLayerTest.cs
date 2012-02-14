using DelftTools.TestUtils;
using NUnit.Framework;
using SharpMap.Extensions.Data.Providers;
using SharpMap.Extensions.Layers;
using SharpMapTestUtils;

namespace SharpMap.Extensions.Tests.Layers
{
    [TestFixture]
    public class GdalRasterLayerTest
    {
        private readonly string rasterDataPath = TestHelper.GetTestDataPath(TestDataPath.DeltaShell.DeltaShellDeltaShellPluginsSharpMapGisTestsRasterData);

        [Test]
        [Category(TestCategory.WindowsForms)]
        public void ReadTryoutBergenTif()
        {
            var path = rasterDataPath + "Tryout_Bergen.tif";
            var map = new Map { Layers = { new GdalRasterLayer { DataSource = new GdalFeatureProvider { Path = path } } } };
            MapTestHelper.Show(map);
        }

        [Test]
        [Category(TestCategory.WindowsForms)]
        public void Show3BandTif()
        {
            var path = rasterDataPath + @"\..\dvim3.tif";
            var map = new Map { Layers = { new GdalRasterLayer { DataSource = new GdalFeatureProvider { Path = path } } } };
            MapTestHelper.Show(map);
        }

        [Test]
        [Category(TestCategory.WindowsForms)]
        public void ShowWsiearthTif()
        {
            var path = rasterDataPath + @"\..\wsiearth.tif";
            var map = new Map { Layers = { new GdalRasterLayer { DataSource = new GdalFeatureProvider { Path = path } } } };
            MapTestHelper.Show(map);
        }
    }
}