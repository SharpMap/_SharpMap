using System.Drawing;
using System.IO;
using NUnit.Framework;
using SharpMap.Extensions.Data.Providers;
using SharpMap.Extensions.Layers;
using SharpMapTestUtils;

namespace SharpMap.Extensions.Tests.Data.Providers
{
    [TestFixture]
    public class GdalRendererTest
    {
        [Test, Category("Windows.Forms")]
        public void GdalRendererShouldRenderBilFileWithDefaultTheme()
        {
            const string rasterPath = @"..\..\..\..\..\test-data\DeltaShell\DeltaShell.Plugins.SharpMapGis.Tests\RasterData\";
            const string path = rasterPath + "Schematisatie.bil";
            Assert.IsTrue(File.Exists(path));
            var rasterLayer = new GdalRasterLayer();

            var rasterFeatureProvider = new GdalFeatureProvider {Path = path};

            rasterLayer.CustomRenderers.Add(new GdalRenderer(rasterLayer)); // add optimized custom gdal renderer
            rasterLayer.DataSource = rasterFeatureProvider;

            Map map = new Map(new Size(200,200));
            map.Layers.Add(rasterLayer);
            MapTestHelper.Show(map);




 
        }


    }
}
