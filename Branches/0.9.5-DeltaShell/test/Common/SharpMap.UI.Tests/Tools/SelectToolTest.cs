using System.Linq;
using DelftTools.TestUtils;
using GeoAPI.Extensions.Feature;
using GisSharpBlog.NetTopologySuite.Geometries;
using GisSharpBlog.NetTopologySuite.IO;
using NetTopologySuite.Extensions.Features;
using NUnit.Framework;
using NetTopologySuite.Extensions.Networks;
using SharpMap.Api;
using SharpMap.Data.Providers;
using SharpMap.Layers;
using SharpMap.UI.Forms;
using SharpMap.UI.Tools;
using SharpTestsEx;

namespace SharpMap.UI.Tests.Tools
{
    [TestFixture]
    public class SelectToolTest
    {
        [Test]
        [Category(TestCategory.WindowsForms)]
        public void ClearSelectionOnLayerRemove()
        {
            var featureProvider = new DataTableFeatureProvider();
            featureProvider.Add(new WKTReader().Read("POINT(0 0)"));
            var layer = new VectorLayer { DataSource = featureProvider };

            using (var mapControl = new MapControl { Map = { Layers = { layer } }, AllowDrop = false })
            {
                var selectTool = mapControl.SelectTool;

                selectTool.Select(featureProvider.Features.Cast<IFeature>());

                WindowsFormsTestHelper.Show(mapControl);

                mapControl.Map.Layers.Clear();

                mapControl.WaitUntilAllEventsAreProcessed();

                selectTool.Selection
                    .Should("selection is cleared on layer remove").Be.Empty();
            }

            WindowsFormsTestHelper.CloseAll();
        }

        [Test]
        [Category(TestCategory.WindowsForms)]
        public void ClearSelectionOnParentGroupLayerRemove()
        {
            var featureProvider = new DataTableFeatureProvider();
            featureProvider.Add(new WKTReader().Read("POINT(0 0)"));
            var layer = new VectorLayer { DataSource = featureProvider };
            var groupLayer = new GroupLayer { Layers = { layer } };

            using (var mapControl = new MapControl { Map = { Layers = { groupLayer } }, AllowDrop = false })
            {
                var selectTool = mapControl.SelectTool;

                selectTool.Select(featureProvider.Features.Cast<IFeature>());

                WindowsFormsTestHelper.Show(mapControl);

                mapControl.Map.Layers.Remove(groupLayer);

                mapControl.WaitUntilAllEventsAreProcessed();

                selectTool.Selection
                    .Should("selection is cleared on layer remove").Be.Empty();
            }

            WindowsFormsTestHelper.CloseAll();
        }

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

        [Test]
        public void TestAddSelection()
        {
            var mapControl = new MapControl();
            var layerData = new FeatureCollection();

            var layer = new VectorLayer {Visible = false, DataSource = layerData};
            var feature = new Node {Geometry = new Point(0, 0)};
            layerData.FeatureType = typeof(Node);
            layerData.Add(feature);

            mapControl.Map.Layers.Add(layer);

            Assert.AreEqual(0, mapControl.SelectTool.SelectedTrackersCount);

            mapControl.SelectTool.AddSelection(new IFeature[0]);

            Assert.AreEqual(0, mapControl.SelectTool.SelectedTrackersCount, "No features should be added as none are passed");

            mapControl.SelectTool.AddSelection(new[] { feature });

            Assert.AreEqual(0, mapControl.SelectTool.SelectedTrackersCount, "No features should be added as none are visible");

            layer.Visible = true;
            mapControl.SelectTool.AddSelection(new[] { feature });

            Assert.AreEqual(1, mapControl.SelectTool.SelectedTrackersCount);
        }

        [Test]
        [Category(TestCategory.Jira)]
        public void GetFeatureInteractorForLayerWithoutFeatureEditorShouldNotGiveErrorMessage_Tools8065()
        {
            var tool = new SelectTool();
            var layer = new RegularGridCoverageLayer();
            Assert.IsNotNull(layer.FeatureEditor);

            // No message should be generated and null should be returned
            TestHelper.AssertLogMessagesCount(() => Assert.IsNull(tool.GetFeatureInteractor(layer, null)), 0);
        }
    }
}
