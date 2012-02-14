using GeoAPI.Extensions.Feature;
using GisSharpBlog.NetTopologySuite.Geometries;
using NetTopologySuite.Extensions.Features;
using NUnit.Framework;
using SharpMap.Data.Providers;
using SharpMap.Layers;
using SharpMap.UI.Forms;
using SharpMap.UI.Tools;

namespace SharpMap.UI.Tests.Tools
{
    [TestFixture]
    public class SelectToolTest
    {
        [Test]
        public void DeActiveSelectionShouldResetMultiSelectionMode()
        {
            MapControl mapControl = new MapControl();

            SelectTool selectTool = mapControl.SelectTool;
            selectTool.MultiSelectionMode = MultiSelectionMode.Lasso;

            mapControl.ActivateTool(selectTool);
            Assert.AreEqual(MultiSelectionMode.Lasso, selectTool.MultiSelectionMode);

            mapControl.ActivateTool(mapControl.MoveTool);
            Assert.AreEqual(MultiSelectionMode.Rectangle, selectTool.MultiSelectionMode);
        }

        [Test]
        public void FindNearestFeature()
        {
            MapControl mapControl = new MapControl();

            VectorLayer layer1 = new VectorLayer();
            FeatureCollection layer1Data = new FeatureCollection();
            layer1.DataSource = layer1Data;
            layer1Data.FeatureType = typeof (Feature);
            layer1Data.Add(new Point(5, 5));
            layer1Data.Add(new Point(1, 1));

            VectorLayer layer2 = new VectorLayer();
            FeatureCollection layer2Data = new FeatureCollection();
            layer2.DataSource = layer2Data;
            layer2Data.FeatureType = typeof(Feature);
            layer2Data.Add(new Point(4, 5));
            layer2Data.Add(new Point(0, 1));

            mapControl.Map.Layers.Add(layer1);
            mapControl.Map.Layers.Add(layer2);

            ILayer outLayer;
            IFeature feature = mapControl.SelectTool.FindNearestFeature(new Coordinate(4, 4), 2.2f, out outLayer, null);

            // expect coordinate in topmost layer
            Assert.AreEqual(outLayer, layer1);
            Assert.AreEqual(5, feature.Geometry.Coordinate.X);
            Assert.AreEqual(5, feature.Geometry.Coordinate.Y);

            layer2.Visible = false;

            feature = mapControl.SelectTool.FindNearestFeature(new Coordinate(4, 4), 2.2f, out outLayer, null);

            Assert.AreEqual(outLayer, layer1);
            Assert.AreEqual(5, feature.Geometry.Coordinate.X);
            Assert.AreEqual(5, feature.Geometry.Coordinate.Y);
        }
    }
}
