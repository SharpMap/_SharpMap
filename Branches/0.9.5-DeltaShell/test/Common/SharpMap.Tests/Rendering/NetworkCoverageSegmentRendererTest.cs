using NUnit.Framework;
using SharpMap.Rendering;

namespace SharpMap.Tests.Rendering
{
    [TestFixture]
    public class NetworkCoverageSegmentRendererTest
    {
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
