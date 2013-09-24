using System;
using System.Drawing;
using System.Windows.Forms;
using DelftTools.TestUtils;
using DelftTools.Utils.Collections.Generic;
using GeoAPI.Extensions.Networks;
using NetTopologySuite.Extensions.Networks;
using NUnit.Framework;
using SharpMap.Api;
using SharpMap.Api.Editors;
using SharpMap.Data.Providers;
using SharpMap.Editors.FallOff;
using SharpMap.Layers;
using SharpMap.Styles;
using SharpMap.UI.Forms;
using SharpMap.UI.Tools;

namespace SharpMap.UI.Tests.Forms
{
    [TestFixture]
    public class GeometryEditorTests
    {
        private Form geometryEditorForm;
        private MapControl mapControl;

        private ListBox listBoxTools;

        private void InitializeControls()
        {
            geometryEditorForm = new Form();
            // Create map and map control
            Map map = new Map();

            mapControl = new MapControl();
            mapControl.Map = map;
            mapControl.Resize += delegate { mapControl.Refresh(); };
            mapControl.ActivateTool(mapControl.PanZoomTool);
            mapControl.Dock = DockStyle.Fill;
            // disable dragdrop because it breaks the test runtime
            mapControl.AllowDrop = false;

            // Create listbox to show all registered tools
            listBoxTools = new ListBox();
            listBoxTools.Dock = DockStyle.Left;
            listBoxTools.SelectedIndexChanged += this.listBoxTools_SelectedIndexChanged;

            map.ZoomToExtents();

            geometryEditorForm.Controls.Add(listBoxTools);
            geometryEditorForm.Controls.Add(mapControl);
        }

        private void AddBranchLayerAndTool()
        {
            var branches = new EventedList<IBranch>();
            branchLayer.DataSource = new FeatureCollection(branches, typeof(Branch));

            //reachLayer.VectorLayer.Name = "reaches";
            branchLayer.Visible = true;
            branchLayer.Style = new VectorStyle
                                    {
                                        Fill = new SolidBrush(Color.Tomato),
                                        Symbol = null,
                                        Line = new Pen(Color.Turquoise, 3)
                                    };
            mapControl.Map.Layers.Insert(0, branchLayer);

            var newLineTool = new NewLineTool(l => l.DataSource.FeatureType.IsSubclassOf(typeof(Branch)), branchLayer.Name) { AutoCurve = true, MinDistance = 0 };
            mapControl.Tools.Add(newLineTool);
            //newLineTool.EditorLayer = reachLayer;

            //BranchNodeTopology t = new BranchNodeTopology();

            //t.Branches = reachLayer.DataSource;
            //t.Nodes = nodeLayer.DataSource;
            // MoveTool.Endoperation += t.endMove();

        }

        public class TestBranchFeature : BranchFeature
        {
            
        }

        private void ADdNewFeature()
        {
            
            var features = new EventedList<TestBranchFeature>(); // ?!?!
            featuresLayer.DataSource = new FeatureCollection {Features = features};
            //featuresLayer.VectorLayer.Name = "culverts"; 
            featuresLayer.Visible = true;
            featuresLayer.Style = new VectorStyle();
            featuresLayer.Style.Fill = new SolidBrush(Color.Tomato);
            featuresLayer.Style.Symbol = null;
            featuresLayer.Style.Line = new Pen(Color.Turquoise, 3);
            mapControl.Map.Layers.Insert(0, featuresLayer);

            //mapControl.SnappingStrategiesByLayer[featuresLayer].Add(
            //    new SnapStrategy(branchLayer, SnapRole.FreeAtObject, 40));

            var newNodeTool = new NewNetworkFeatureTool(l => l.DataSource.FeatureType.IsSubclassOf(typeof(TestBranchFeature)), "add new feature");
            mapControl.Tools.Add(newNodeTool);
        }

        private void AddDiscretisationLayerAndTool()
        {
            var branches = new EventedList<TestBranchFeature>();
            discretisationLayer.DataSource = new FeatureCollection {Features = branches};

        //discretisationLayer.VectorLayer.Name = "calcgrid"; 
            mapControl.Map.Layers.Insert(1, discretisationLayer);
            discretisationLayer.Visible = true;
            discretisationLayer.Style = new VectorStyle
                                            {
                                                Fill = new SolidBrush(Color.Tomato),
                                                Symbol = null,
                                                Line = new Pen(Color.DarkSalmon, 12)
                                            };

            //SegmentedLayer segmentedLayer = new SegmentedLayer();
            //segmentedLayer.SourceLayer = geometryEditor.LayerEditors["reaches"];
            //segmentedLayer.TargetLayer = geometryEditor.LayerEditors["calcgrid"];
        }

        VectorLayer branchLayer = new VectorLayer("branches");
        VectorLayer featuresLayer = new VectorLayer("culverts");
        VectorLayer discretisationLayer = new VectorLayer("calcgrid");

        [Test]
        [Category(TestCategory.WindowsForms)]
        public void Create()
        {
            InitializeControls();
            AddBranchLayerAndTool();
            ADdNewFeature();
            AddDiscretisationLayerAndTool();

            mapControl.MoveTool.FallOffPolicy = FallOffType.Linear;

            // TODO: does not sound logical to have a tool with a name of layer??
            mapControl.ActivateTool(mapControl.GetToolByName(branchLayer.Name));

            foreach (IMapTool tool in mapControl.Tools)
            {
                if (null != tool.Name)
                    listBoxTools.Items.Add(tool.Name);
            }

            WindowsFormsTestHelper.ShowModal(geometryEditorForm);
        }

        [Test]
        public void DefaultMapControlTools()
        {
            InitializeControls();

            // check for all default tools
            IMapTool mapTool = mapControl.SelectTool;
            Assert.IsNotNull(mapTool);
            SelectTool selectTool = mapTool as SelectTool;
            Assert.IsNotNull(selectTool);

            mapTool = mapControl.MoveTool;
            Assert.IsNotNull(mapTool);

            MoveTool moveTool = mapTool as MoveTool; 
            Assert.IsNotNull(moveTool);
            Assert.AreEqual(FallOffType.None, moveTool.FallOffPolicy);

            mapTool = mapControl.GetToolByName("CurvePoint");
            Assert.IsNotNull(mapTool);
            CurvePointTool curvePointTool = mapTool as CurvePointTool;
            Assert.IsNotNull(curvePointTool);
        }

        [Test]
        // TODO: this test is strange, there should be tests for tools, checking how many Tools exist in map control or state of default tools should be in MapControlTest class and not here
        public void CustomGeometryEditorTools()
        {
            InitializeControls();
            AddBranchLayerAndTool();

            IMapTool mapTool = mapControl.GetToolByName(branchLayer.Name);
            Assert.IsNotNull(mapTool);
            NewLineTool newLineTool = mapTool as NewLineTool;
            Assert.IsNotNull(newLineTool);
        }

        private void listBoxTools_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (-1 != listBoxTools.SelectedIndex)
            {
                mapControl.ActivateTool(mapControl.GetToolByName(listBoxTools.Items[listBoxTools.SelectedIndex].ToString()));
            }
        }
    }
}