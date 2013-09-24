using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using GisSharpBlog.NetTopologySuite.Geometries;
using NUnit.Framework;
using NetTopologySuite.Extensions.Networks;
using SharpMap.Data.Providers;
using SharpMap.Layers;
using SharpMap.Styles;
using SharpMap.UI.Forms;
using SharpMap.UI.Tools;

namespace SharpMap.UI.Tests.Tools
{
    [TestFixture]
    public class NewNetworkFeatureToolTest
    {
        [Test]
        public void NewNodeToolShouldWorkWithoutSnapRules()
        {
            // Create map and map control
            Map map = new Map();
            var nodeList = new List<Node>();

            var nodeLayer = new VectorLayer
                {
                    DataSource = new FeatureCollection(nodeList, typeof (Node)),
                    Visible = true,
                    Style = new VectorStyle
                        {
                            Fill = new SolidBrush(Color.Tomato),
                            Symbol = null,
                            Line = new Pen(Color.Turquoise, 3)
                        }
                };
            map.Layers.Add(nodeLayer);

            var mapControl = new MapControl {Map = map};
            mapControl.Resize += delegate { mapControl.Refresh(); };
            mapControl.Dock = DockStyle.Fill;

            var newNodeTool = new NewNetworkFeatureTool(l => true, "new node");
            mapControl.Tools.Add(newNodeTool);

            var args = new MouseEventArgs(MouseButtons.Left, 1, -1, -1, -1);

            newNodeTool.OnMouseDown(new Coordinate(0, 20), args);
            newNodeTool.OnMouseMove(new Coordinate(0, 20), args);
            newNodeTool.OnMouseUp(new Coordinate(0, 20), args);

            Assert.IsFalse(newNodeTool.IsBusy);
            Assert.AreEqual(1, nodeList.Count);
        }
    }
}