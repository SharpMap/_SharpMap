using System;
using System.Drawing;
using System.Windows.Forms;
using DelftTools.TestUtils;
using DelftTools.Utils.Editing;

using GeoAPI.Extensions.Coverages;
using GeoAPI.Extensions.Feature;
using GeoAPI.Extensions.Networks;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;
using NetTopologySuite.Extensions.Coverages;
using NetTopologySuite.Extensions.Geometries;
using NetTopologySuite.Extensions.Networks;
using NUnit.Framework;
using SharpMap.Api;
using SharpMap.Api.Editors;
using SharpMap.Data.Providers;
using SharpMap.Editors.FallOff;
using SharpMap.Editors.Interactors;
using SharpMap.Editors.Snapping;
using SharpMap.Layers;
using SharpMap.Styles;
using SharpMap.UI.Forms;
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

            mapControl.MoveTool.FallOffPolicy = FallOffType.Linear;

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
        readonly NetworkCoverageGroupLayer networkCoverageGroupLayer = new NetworkCoverageGroupLayer();

        private MapTool AddBranchLayerAndTool()
        {
            branchLayer.DataSource = new FeatureCollection((IList)network.Branches, typeof(Branch));
            branchLayer.Visible = true;
            branchLayer.Style = new VectorStyle
            {
                Fill = new SolidBrush(Color.Tomato),
                Symbol = null,
                Line = new Pen(Color.Turquoise, 3)
            };
            mapControl.Map.Layers.Insert(0, branchLayer);

            var newLineTool = new NewLineTool(l => l.Equals(branchLayer), "new branch") { AutoCurve = true, MinDistance = 0 };
            mapControl.Tools.Add(newLineTool);
            return newLineTool;
        }


        private IFeature AddFeatureFromGeometryDelegate(IFeatureProvider provider, IGeometry geometry)
        {
            IBranch branch = (IBranch) mapControl.SnapTool.SnapResult.SnappedFeature;
            double offset = GeometryHelper.Distance((ILineString) branch.Geometry, geometry.Coordinates[0]);
            var feature = new NetworkLocation(branch, offset) { Geometry = geometry };
            //IFeatureInteractor FeatureInteractor = new NetworkLocationEditor(new CoordinateConverter(mapControl.Map),
            //                                               networkCoverageLayer.LocationLayer, feature,
            //                                               new VectorStyle());
            //FeatureInteractor.Start();
            //FeatureInteractor.Stop(mapControl.SnapTool.SnapResult); // hack
            provider.Features.Add(feature);
            return feature;
        }

        [Test]
        [Category(TestCategory.WindowsForms)]
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
            
            WindowsFormsTestHelper.ShowModal(geometryEditorForm);
        }

        private void AddNetworkCoverageAndTool()
        {
            INetworkCoverage networkCoverage = new NetworkCoverage { Network = network };
            networkCoverageGroupLayer.NetworkCoverage = networkCoverage;

            ((FeatureCollection)networkCoverageGroupLayer.LocationLayer.DataSource).AddNewFeatureFromGeometryDelegate =
                AddFeatureFromGeometryDelegate;

/* no references to DeltaShell, TODO: move LayerPropertiesEditor into SharpMap.UI
            mapControl.MouseDoubleClick += delegate
            {
                var dialog = new LayerPropertiesEditorDialog(networkCoverageLayer.SegmentLayer);
                dialog.Show(mapControl);
            };
*/

            mapControl.Map.Layers.Add(networkCoverageGroupLayer);

            var networkCoverageTool = new NewNetworkFeatureTool(l => l.Equals(networkCoverageGroupLayer.LocationLayer), "new location");
            mapControl.Tools.Add(networkCoverageTool);
            
            networkCoverageGroupLayer.LocationLayer.FeatureEditor.SnapRules.Add(new SnapRule
            {
                SnapRole = SnapRole.FreeAtObject,
                Obligatory = true,
                PixelGravity = 40
            });
            return;
        }

        [Test]
        [Category(TestCategory.WindowsForms)]
        public void NewLineToolAndNetworkCoverageTool()
        {
            // same test as NewLineTool but adds the possibility to networklocation to a branch.
            // This test does not support topologyrles that update networklocations in response
            // to a branch geometry change.
            // A theme interactor is available via a double click in the canvas.
            InitializeControls();

            var newLineTool = AddBranchLayerAndTool();
            AddNetworkCoverageAndTool();

            mapControl.ActivateTool(newLineTool);

            foreach (IMapTool tool in mapControl.Tools)
            {
                if (null != tool.Name)
                    listBoxTools.Items.Add(tool.Name);
            }

            WindowsFormsTestHelper.ShowModal(geometryEditorForm);
        }

        [Test]
        [Category(TestCategory.WindowsForms)]
        public void EmptyLineOrLineWithOnePointShouldDoNothingTools9933()
        {
            InitializeControls();

            var newLineTool = AddBranchLayerAndTool();
            AddNetworkCoverageAndTool();

            ((NewLineTool)newLineTool).AutoCurve = false;
            ((NewLineTool)newLineTool).CloseLine = true;
            mapControl.ActivateTool(newLineTool);

            var args = new MouseEventArgs(MouseButtons.Left, 1, -1, -1, -1);
            WindowsFormsTestHelper.ShowModal(geometryEditorForm,
                                             f =>
                                             {
                                                 newLineTool.OnMouseDown(new Coordinate(0, 10), args); //click 1
                                                 newLineTool.OnMouseUp(new Coordinate(0, 10), args);
                                                 newLineTool.OnMouseDown(new Coordinate(0, 10), args); //click 2
                                                 newLineTool.OnMouseUp(new Coordinate(0, 10), args);
                                                 newLineTool.OnMouseDown(new Coordinate(0, 10), args); //click 3
                                                 newLineTool.OnMouseDoubleClick(null, args); //double click
                                                 newLineTool.OnMouseUp(new Coordinate(0, 10), args);
                                                 
                                                 Assert.IsTrue(newLineTool.IsBusy);
                                             });
        }

        [Test]
        [Category(TestCategory.WindowsForms)]
        public void ToolShouldNoLongerBeBusyAfterDoubleClick()
        {
            InitializeControls();

            var newLineTool = AddBranchLayerAndTool();
            AddNetworkCoverageAndTool();

            ((NewLineTool) newLineTool).AutoCurve = false;
            mapControl.ActivateTool(newLineTool);

            var args = new MouseEventArgs(MouseButtons.Left, 1, -1, -1, -1);
            WindowsFormsTestHelper.ShowModal(geometryEditorForm,
                                             f =>
                                                 {
                                                     newLineTool.OnMouseDown(new Coordinate(0, 10), args); //click 1
                                                     newLineTool.OnMouseUp(new Coordinate(0, 10), args);

                                                     newLineTool.OnMouseMove(new Coordinate(0, 20), args); // move

                                                     newLineTool.OnMouseDown(new Coordinate(0, 20), args); //click 2
                                                     newLineTool.OnMouseUp(new Coordinate(0, 20), args);

                                                     newLineTool.OnMouseDown(new Coordinate(0, 20), args); //2nd click 2
                                                     newLineTool.OnMouseDoubleClick(null, args); //first double click
                                                     newLineTool.OnMouseUp(new Coordinate(0, 20), args); //then up

                                                     Assert.IsFalse(newLineTool.IsBusy);
                                                 });
        }
    }

    class LineStringFeatureInteractor : LineStringInteractor
    {
        public LineStringFeatureInteractor(ILayer layer, IFeature feature, VectorStyle vectorStyle, IEditableObject editableObject)
            : base(layer, feature, vectorStyle, editableObject)
        {
        }

        protected override bool AllowDeletionCore()
        {
            return true;
        }

        protected override bool AllowMoveCore()
        {
            return true;
        }


        //public override IEnumerable<IFeatureRelationInteractor> GetRelationEditorRules(IFeature feature)
        //{
        //    yield return new BranchToBranchFeatureRelationEditor<INetworkLocation>();
        //}

        //public override void Stop()
        //{
        //    base.Stop();
        //}
    }
}
