using System;
using System.Drawing;
using System.Windows.Forms;
using DelftTools.TestUtils;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Extensions.Feature;
using GeoAPI.Extensions.Networks;
using GeoAPI.Geometries;
using NetTopologySuite.Extensions.Coverages;
using NetTopologySuite.Extensions.Geometries;
using NetTopologySuite.Extensions.Networks;
using NUnit.Framework;
using SharpMap.Data.Providers;
using SharpMap.Layers;
using SharpMap.Styles;
using SharpMap.Topology;
using SharpMap.UI.Editors;
using SharpMap.UI.Forms;
using SharpMap.UI.Snapping;
using SharpMap.UI.Tools;
using System.Collections;

namespace SharpMap.UI.Tests.Tools
{
    [TestFixture]
    public class NewLineToolTest
    {
        private Form geometryEditorForm;
        private MapControl mapControl;
        private ListBox listBoxTools;
        readonly INetwork network = new Network();


        private void InitializeControls()
        {
            geometryEditorForm = new Form();
            // Create map and map control
            Map map = new Map();

            mapControl = new MapControl {Map = map};
            mapControl.Resize += delegate { mapControl.Refresh(); };
            mapControl.ActivateTool(mapControl.PanZoomTool);
            mapControl.Dock = DockStyle.Fill;
            // disable dragdrop because it breaks the test runtime
            mapControl.AllowDrop = false;

            // Create listbox to show all registered tools
            listBoxTools = new ListBox {Dock = DockStyle.Left};
            listBoxTools.SelectedIndexChanged += listBoxTools_SelectedIndexChanged;

            map.ZoomToExtents();

            mapControl.MoveTool.FallOffPolicy = FallOffPolicyRule.Linear;

            geometryEditorForm.Controls.Add(listBoxTools);
            geometryEditorForm.Controls.Add(mapControl);
        }

        private void listBoxTools_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (-1 != listBoxTools.SelectedIndex)
            {
                mapControl.ActivateTool(mapControl.GetToolByName(listBoxTools.Items[listBoxTools.SelectedIndex].ToString()));
            }
        }

        readonly VectorLayer branchLayer = new VectorLayer("branches");
        readonly NetworkCoverageLayer networkCoverageLayer = new NetworkCoverageLayer();

        private MapTool AddBranchLayerAndTool()
        {
            branchLayer.DataSource = new FeatureCollection((IList)network.Branches, typeof(Branch));
            branchLayer.Enabled = true;
            branchLayer.Style = new VectorStyle
            {
                Fill = new SolidBrush(Color.Tomato),
                Symbol = null,
                Line = new Pen(Color.Turquoise, 3)
            };
            mapControl.Map.Layers.Insert(0, branchLayer);

            var newLineTool = new NewLineTool(branchLayer) { AutoCurve = true, MinDistance = 0 };
            mapControl.Tools.Add(newLineTool);
            return newLineTool;
        }


        private IFeature AddFeatureFromGeometryDelegate(IFeatureProvider provider, IGeometry geometry)
        {
            IBranch branch = (IBranch) mapControl.SnapTool.SnapResult.SnappedFeature;
            double offset = GeometryHelper.Distance((ILineString) branch.Geometry, geometry.Coordinates[0]);
            var feature = new NetworkLocation(branch, offset) { Geometry = geometry };
            //IFeatureEditor featureEditor = new NetworkLocationEditor(new CoordinateConverter(mapControl.Map),
            //                                               networkCoverageLayer.LocationLayer, feature,
            //                                               new VectorStyle());
            //featureEditor.Start();
            //featureEditor.Stop(mapControl.SnapTool.SnapResult); // hack
            provider.Features.Add(feature);
            return feature;
        }


        IFeatureEditor SelectTool_FeatureEditorCreation(ILayer layer, IFeature feature, VectorStyle vectorStyle)
        {
            if (feature is Branch)
            {
                return new LineStringFeatureEditor(new CoordinateConverter(mapControl), layer, feature, vectorStyle);
            }
            return null;
        }

        [Test]
        [Category("Windows.Forms")]
        public void NewLineTool()
        {
            // A sinple test to draw linestring on a canvas
            InitializeControls();
            var newLineTool = AddBranchLayerAndTool();

            mapControl.ActivateTool(newLineTool);

            foreach (IMapTool tool in mapControl.Tools)
            {
                if (null != tool.Name)
                    listBoxTools.Items.Add(tool.Name);
            }
            mapControl.SelectTool.FeatureEditorCreation += SelectTool_FeatureEditorCreation;

            WindowsFormsTestHelper.ShowModal(geometryEditorForm);
        }

        private void AddNetworkCoverageAndTool()
        {
            INetworkCoverage networkCoverage = new NetworkCoverage { Network = network };
            networkCoverageLayer.NetworkCoverage = networkCoverage;

            ((FeatureCollection)networkCoverageLayer.LocationLayer.DataSource).AddNewFeatureFromGeometryDelegate =
                AddFeatureFromGeometryDelegate;

/* no references to DeltaShell, TODO: move LayerPropertiesEditor into SharpMap.UI
            mapControl.MouseDoubleClick += delegate
            {
                var dialog = new LayerPropertiesEditorDialog(networkCoverageLayer.SegmentLayer);
                dialog.Show(mapControl);
            };
*/

            mapControl.Map.Layers.Add(networkCoverageLayer);

            var networkCoverageTool = new NewNodeTool(networkCoverageLayer.LocationLayer);
            mapControl.Tools.Add(networkCoverageTool);
            mapControl.SnapRules.Add(new SnapRule
            {
                SourceLayer = networkCoverageLayer.LocationLayer,
                TargetLayer = branchLayer,
                SnapRole = SnapRole.FreeAtObject,
                Obligatory = true,
                PixelGravity = 40
            });
            return;
        }

        [Test]
        [Category("Windows.Forms")]
        public void NewLineToolAndNetworkCoverageTool()
        {
            // same test as NewLineTool but adds the possibility to networklocation to a branch.
            // This test does not support topologyrles that update networklocations in response
            // to a branch geometry change.
            // A theme editor is available via a double click in the canvas.
            InitializeControls();

            var newLineTool = AddBranchLayerAndTool();
            AddNetworkCoverageAndTool();

            mapControl.ActivateTool(newLineTool);

            foreach (IMapTool tool in mapControl.Tools)
            {
                if (null != tool.Name)
                    listBoxTools.Items.Add(tool.Name);
            }
            mapControl.SelectTool.FeatureEditorCreation += SelectTool_FeatureEditorCreation;

            WindowsFormsTestHelper.ShowModal(geometryEditorForm);
        }
    }

    class LineStringFeatureEditor : LineStringEditor
    {
        public LineStringFeatureEditor(ICoordinateConverter coordinateConverter, ILayer layer, IFeature feature, VectorStyle vectorStyle)
            : base(coordinateConverter, layer, feature, vectorStyle)
        {
        }

        public override bool AllowDeletion()
        {
            return true;
        }

        public override bool AllowMove()
        {
            return true;
        }


        //public override IEnumerable<IFeatureRelationEditor> GetRelationEditorRules(IFeature feature)
        //{
        //    yield return new BranchToBranchFeatureRelationEditor<INetworkLocation>();
        //}

        //public override void Stop()
        //{
        //    base.Stop();
        //}
    }
}
