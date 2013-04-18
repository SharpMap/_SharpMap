using DelftTools.TestUtils;
using NUnit.Framework;
using SharpMap.Extensions.Layers;
using SharpMap.UI.Forms;

namespace SharpMap.Extensions.Tests.Layers
{
    [TestFixture]
    public class OpenStreetMapLayerTest
    {
        [Test]
        [Category("Windows.Forms")]
        public void ShowWithOsmLayer()
        {
            var map = new Map();

            var layer = new OpenStreetMapLayer();
            map.Layers.Add(layer);

            var mapControl = new MapControl {Map = map};

            WindowsFormsTestHelper.ShowModal(mapControl);
        }

    }
}