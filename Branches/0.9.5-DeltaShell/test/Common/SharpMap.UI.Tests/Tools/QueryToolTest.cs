using System.Drawing;
using DelftTools.TestUtils;
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
            var mapControl = new MapControl();
            var map = new Map(new Size(1, 1));
            const string path = @"..\..\..\..\..\test-data\DeltaShell\DeltaShell.Plugins.SharpMapGis.Tests\RasterData\bodem.bil";

            var layer = new GdalRegularGridRasterLayer(path);

            map.Layers.Add(layer);
            mapControl.Map = map;

            var tool = mapControl.GetToolByType(typeof (QueryTool));
            mapControl.ActivateTool(tool);
            WindowsFormsTestHelper.ShowModal(mapControl);
        }
    }
}
