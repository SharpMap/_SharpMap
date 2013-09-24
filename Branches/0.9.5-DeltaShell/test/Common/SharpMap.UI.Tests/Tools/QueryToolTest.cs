using System.Drawing;
using System.IO;
using DelftTools.TestUtils;
using DelftTools.Utils.Reflection;
using GisSharpBlog.NetTopologySuite.Geometries;
using NUnit.Framework;
using SharpMap.Extensions.Layers;
using SharpMap.UI.Forms;
using SharpMap.UI.Tools;

namespace SharpMap.UI.Tests.Tools
{
    [TestFixture]
    public class QueryToolTest
    {
        [Test,Category(TestCategory.WindowsForms)]
        public void ShowMapWithQueryToolActive()
        {
            var mapControl = new MapControl{ AllowDrop = false };
            var map = new Map(new Size(1, 1));
            //const string path = @"..\..\..\..\..\test-data\DeltaShell\DeltaShell.Plugins.SharpMapGis.Tests\RasterData\bodem.bil";
            const string path = @"..\..\..\..\..\test-data\DeltaShell\DeltaShell.Plugins.SharpMapGis.Tests\RasterData\test.asc";

            var layer = new GdalRegularGridRasterLayer(path);
            layer.Coverage.Components[0].NoDataValues.Add(100);

            map.Layers.Add(layer);
            mapControl.Map = map;

            var tool = mapControl.GetToolByType(typeof (QueryTool));
            mapControl.ActivateTool(tool);
            WindowsFormsTestHelper.ShowModal(mapControl);
        }

        [Test]
        public void GetCoverageValues()
        {
            var rasterDataFolderPath = TestHelper.GetTestDataPath(TestDataPath.DeltaShell.DeltaShellDeltaShellPluginsSharpMapGisTestsRasterData);
            var gridFilePath = Path.Combine(rasterDataFolderPath, "test.asc");

            var mapControl = new MapControl();
            var map = new Map();
            var layer = new GdalRegularGridRasterLayer(gridFilePath);

            map.Layers.Add(layer);
            mapControl.Map = map;

            var tool = mapControl.GetToolByType(typeof(QueryTool));
            mapControl.ActivateTool(tool);

            var noDataCellText = TypeUtils.CallPrivateMethod<string>(tool, "GetCoverageValues", new object[] {new Coordinate(25, 275)});
            Assert.AreEqual("image layer1 : No data\n", noDataCellText);

            var validDataCellText = TypeUtils.CallPrivateMethod<string>(tool, "GetCoverageValues", new object[] { new Coordinate(125, 125) });
            Assert.AreEqual("image layer1 : 50\n", validDataCellText);
        }
    }
}