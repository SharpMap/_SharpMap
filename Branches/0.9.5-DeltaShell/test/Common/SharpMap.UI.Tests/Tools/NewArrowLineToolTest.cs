using System.Collections.Generic;
using System.Windows.Forms;
using GisSharpBlog.NetTopologySuite.Geometries;
using NetTopologySuite.Extensions.Networks;
using NUnit.Framework;
using SharpMap.Data.Providers;
using SharpMap.Layers;
using SharpMap.UI.Forms;
using SharpMap.UI.Tools;

namespace SharpMap.UI.Tests.Tools
{
    [TestFixture]
    public class NewArrowLineToolTest
    {
        private NewArrowLineTool newArrowLineTool;
        private List<Branch> branchList;

        [SetUp]
        public void Setup()
        {
            branchList = new List<Branch>();

            var mapControl = new MapControl();
            var vectorLayer = new VectorLayer()
                                  {
                                      DataSource = new FeatureCollection(branchList, typeof (Branch)),
                                  };

            newArrowLineTool = new NewArrowLineTool(vectorLayer);

            mapControl.Tools.Add(newArrowLineTool);
            mapControl.Map.Layers.Add(vectorLayer);
        }

        [Test]
        public void CantCreateALinkWithTheSameBeginAndEndPosition()
        {
            // start point
            newArrowLineTool.OnMouseDown(new Coordinate(0,0), new MouseEventArgs(MouseButtons.Left,1,0,0,0));

            // end point (on the same location as that of the startpoint)
            newArrowLineTool.OnMouseDown(new Coordinate(0, 0), new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0));

            Assert.AreEqual(0, branchList.Count);

            // end point
            newArrowLineTool.OnMouseDown(new Coordinate(1, 1), new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0));

            Assert.AreEqual(1, branchList.Count);
        }

        [Test]
        public void MakingTheSameLinkTwiceFails()
        {
            // start point
            newArrowLineTool.OnMouseDown(new Coordinate(0, 0), new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0));

            // end point
            newArrowLineTool.OnMouseDown(new Coordinate(1, 1), new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0));

            Assert.AreEqual(1, branchList.Count);

            // start point
            newArrowLineTool.OnMouseDown(new Coordinate(0, 0), new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0));

            // end point
            newArrowLineTool.OnMouseDown(new Coordinate(1, 1), new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0));

            Assert.AreEqual(1, branchList.Count);

        }
    }
}