using DelftTools.Utils.Editing;
using NUnit.Framework;
using NetTopologySuite.Extensions.Coverages;
using NetTopologySuite.Extensions.Networks;
using SharpMap.Data.Providers;
using SharpMap.Layers;
using SharpMap.Rendering;

namespace SharpMap.Tests.Rendering
{
    [TestFixture]
    public class NetworkCoverageSegmentRendererTest
    {
        [Test]
        public void DelayRenderingWhenNetworkIsEditingIsTrue()
        {
            var network = new Network();
            network.BeginEdit(new DefaultEditAction("edit"));
            var layer = new NetworkCoverageSegmentLayer
                            {
                                DataSource = new NetworkCoverageFeatureCollection
                                                 {
                                                     NetworkCoverage =
                                                         new NetworkCoverage {Network = network}
                                                 }
                            };

            var renderer = new NetworkCoverageSegmentRenderer();
            var result = renderer.Render(null, null, layer);

            Assert.IsTrue(result); // True because rendering did not fail
            Assert.IsTrue(layer.RenderRequired); // True, to indicate rendering is delayed
        }

        [Test]
        public void GetIndex()
        {
            //                       0     1      2     3     4    5     6     7     8     9     10    11
            var positions = new[] {0.0F, 0.0F, 0.0F, 0.1F, 0.1F, 0.1F, 0.2F, 0.3F, 0.4F, 0.5F, 1.0F, 1.0F};

            Assert.AreEqual(2, NetworkCoverageSegmentRenderer.GetStartIndex(positions, 0.05F));
            Assert.AreEqual(3, NetworkCoverageSegmentRenderer.GetEndIndex(positions, 0.05F));
            Assert.AreEqual(6, NetworkCoverageSegmentRenderer.GetStartIndex(positions, 0.25F));
            Assert.AreEqual(7, NetworkCoverageSegmentRenderer.GetEndIndex(positions, 0.25F));

            Assert.AreEqual(2, NetworkCoverageSegmentRenderer.GetStartIndex(positions, 0.0F));
            Assert.AreEqual(5, NetworkCoverageSegmentRenderer.GetStartIndex(positions, 0.1F));
            Assert.AreEqual(8, NetworkCoverageSegmentRenderer.GetStartIndex(positions, 0.4F));
            Assert.AreEqual(11, NetworkCoverageSegmentRenderer.GetStartIndex(positions, 1.0F));

            Assert.AreEqual(0, NetworkCoverageSegmentRenderer.GetEndIndex(positions, 0.0F));
            Assert.AreEqual(3, NetworkCoverageSegmentRenderer.GetEndIndex(positions, 0.1F));
            Assert.AreEqual(8, NetworkCoverageSegmentRenderer.GetEndIndex(positions, 0.4F));
            Assert.AreEqual(10, NetworkCoverageSegmentRenderer.GetEndIndex(positions, 1.0F));
        }

        [Test]
        public void GetIndex2()
        {
            //                       0       1             2       3
            var positions = new[] {0.0F, 0.4787943F, 0.4787943F, 1.0F};

            Assert.AreEqual(2, NetworkCoverageSegmentRenderer.GetStartIndex(positions, 0.5962113F));
            Assert.AreEqual(3, NetworkCoverageSegmentRenderer.GetEndIndex(positions, 0.5962113F));

            //positions = new[] { 0.0F, 1.0F, 1.0F, 1.0F };

            //Assert.AreEqual(0, NetworkCoverageSegmentRenderer.GetStartIndex(positions, 0.5F));
            //Assert.AreEqual(1, NetworkCoverageSegmentRenderer.GetEndIndex(positions, 0.5F));
            //Assert.AreEqual(0, NetworkCoverageSegmentRenderer.GetStartIndex(positions, 0.0F));
            //Assert.AreEqual(0, NetworkCoverageSegmentRenderer.GetEndIndex(positions, 0.0F));
            //Assert.AreEqual(0, NetworkCoverageSegmentRenderer.GetStartIndex(positions, 1.0F));
            //Assert.AreEqual(1, NetworkCoverageSegmentRenderer.GetEndIndex(positions, 1.0F));


            //positions = new[] { 0.0F, 0.0F, 0.0F, 1.0F };

            //Assert.AreEqual(2, NetworkCoverageSegmentRenderer.GetStartIndex(positions, 0.5F));
            //Assert.AreEqual(3, NetworkCoverageSegmentRenderer.GetEndIndex(positions, 0.5F));
            //Assert.AreEqual(2, NetworkCoverageSegmentRenderer.GetStartIndex(positions, 0.0F));
            //Assert.AreEqual(3, NetworkCoverageSegmentRenderer.GetEndIndex(positions, 0.0F));
            //Assert.AreEqual(2, NetworkCoverageSegmentRenderer.GetStartIndex(positions, 1.0F));
            //Assert.AreEqual(3, NetworkCoverageSegmentRenderer.GetEndIndex(positions, 1.0F));
        }
    }
}
