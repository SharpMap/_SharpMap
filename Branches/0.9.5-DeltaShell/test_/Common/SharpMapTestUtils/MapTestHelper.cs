using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using DelftTools.TestUtils;
using DelftTools.Utils.Collections.Generic;
using GeoAPI.Extensions.Networks;
using GeoAPI.Geometries;
using NetTopologySuite.Extensions.Networks;
using Rhino.Mocks;
using SharpMap;
using SharpMap.Converters.WellKnownText;
using SharpMap.UI.Forms;
using SharpMapTestUtils.TestClasses;

namespace SharpMapTestUtils
{
    /// <summary>
    /// Provides common GIS testing functionality.
    /// </summary>

    public class MapTestHelper : Form // TODO: incapsulate Form as a local variable in ShowMap()
    {       
        /// <summary>
        /// Method show a new map control.
        /// </summary>
        /// <param name="map"></param>
        public static void Show(Map map)
        {
            new MapTestHelper().ShowMap(map);
        }

        private Label coordinateLabel;

        public MapTestHelper()
        {
            MapControl = new MapControl();
            MapControl.Dock = DockStyle.Fill;
            // disable dragdrop because it breaks the test runtime
            MapControl.AllowDrop = false;
            
            
            Controls.Add(MapControl);

            coordinateLabel = new Label();
            coordinateLabel.Width = 500;
            coordinateLabel.Location = new Point(5, 5);

            MapControl.Controls.Add(coordinateLabel);
            MapControl.Resize += delegate { MapControl.Refresh(); };
            MapControl.ActivateTool(MapControl.PanZoomTool);

        }

        public void ShowMap(Map map)
        {
            MapControl.Map = map;

            map.ZoomToExtents();

            MapControl.MouseMove += delegate(object sender, MouseEventArgs e)
                                        {
                                            ICoordinate point = map.ImageToWorld(new PointF(e.X, e.Y));
                                            coordinateLabel.Text = string.Format("{0}:{1}", point.X, point.Y);
                                        };

            WindowsFormsTestHelper.ShowModal(this);
        }

        public  MapControl MapControl { get; set; }

        static readonly MockRepository mocks = new MockRepository();
        
        public static INetwork CreateTestNetwork()
        {
            var network = new Network();
            var branch = new Branch();
            network.Branches.Add(branch);
            branch.BranchFeatures.Add(new TestBranchFeature());
            var node = new Node();
            network.Nodes.Add(node);
            node.NodeFeatures.Add(new TestNodeFeature { Node = network.Nodes[0] });
            return network;
        }

        public static INetwork CreateMockNetwork()
        {
            var network = mocks.Stub<INetwork>();

            // branche
            var branch1 = mocks.Stub<IBranch>();
            var branch2 = mocks.Stub<IBranch>();
            branch1.Name = "branch1";
            branch2.Name = "branch2";
            branch1.Length = 100;
            branch2.Length = 100;
            branch1.Geometry = GeometryFromWKT.Parse("LINESTRING (0 0, 100 0)");
            branch2.Geometry = GeometryFromWKT.Parse("LINESTRING (100 0, 200 10)");

            var branches = new EventedList<IBranch>(new[] { branch1, branch2 });
            network.Branches = branches;

            branch1.Network = network;
            branch1.BranchFeatures = new EventedList<IBranchFeature>();
            
            branch2.Network = network;
            branch2.BranchFeatures = new EventedList<IBranchFeature>();

            branch1.Network = network;
            branch2.Network = network;


            // nodes
            var node1 = mocks.Stub<INode>();
            var node2 = mocks.Stub<INode>();
            var node3 = mocks.Stub<INode>();

            node1.Name = "node1";
            node2.Name = "node2";
            node3.Name = "node2";
            node1.Geometry = GeometryFromWKT.Parse("POINT (0 0)");
            node2.Geometry = GeometryFromWKT.Parse("POINT (100 0)");
            node3.Geometry = GeometryFromWKT.Parse("POINT (200 10)");

            node1.IncomingBranches = new List<IBranch>();
            node2.IncomingBranches = new List<IBranch> {branch1};
            node3.IncomingBranches = new List<IBranch> {branch2};

            node1.OutgoingBranches = new List<IBranch> {branch1};
            node2.OutgoingBranches = new List<IBranch> {branch2};
            node3.OutgoingBranches = new List<IBranch>();

            branch1.Source = node1;
            branch1.Target = node2;
            branch2.Source = node2;
            branch2.Target = node3;

            var nodes = new EventedList<INode>(new[] { node1, node2, node3 });
            network.Nodes = nodes;
            node1.Network = network;
            node2.Network = network;
            node3.Network = network;

            // node feature
            var nodeFeature1 = mocks.Stub<INodeFeature>();

            nodeFeature1.Name = "nodeFeature1";
            nodeFeature1.Geometry = GeometryFromWKT.Parse("POINT (0 0)");
            nodeFeature1.Node = node1;

            // new[] { nodeFeature1 }
            var nodeFeatures = new List<INodeFeature>();
            Expect.Call(network.NodeFeatures).Return(nodeFeatures).Repeat.Any();

            nodeFeature1.Network = network;

            // branch feature
            var branchFeature1 = mocks.Stub<IBranchFeature>();

            branchFeature1.Name = "branchFeature1";
            branchFeature1.Geometry = GeometryFromWKT.Parse("POINT (50 0)");
            branchFeature1.Branch = branch1;

            // new[] { branchFeature1 }
            var branchFeatures = new List<IBranchFeature>();
            Expect.Call(network.BranchFeatures).Return(branchFeatures).Repeat.Any();
            
            branchFeature1.Network = network;
            mocks.ReplayAll();

            return network;
        }
    }
}