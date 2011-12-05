using NetTopologySuite.Extensions.Coverages;
using NUnit.Framework;

namespace NetTopologySuite.Extensions.Tests.Coverages
{
    [TestFixture]
    public class NetworkSegmentTest
    {
        [Test]
        public void EndOffsetDependsOnDirection()
        {
            NetworkSegment segment = new NetworkSegment();
            segment.Offset = 40;
            segment.Length = 10;
            segment.DirectionIsPositive = true;

            Assert.AreEqual(50,segment.EndOffset);
            //change direction changes the end offset
            segment.DirectionIsPositive = false;
            Assert.AreEqual(30, segment.EndOffset);
        }
    }
}