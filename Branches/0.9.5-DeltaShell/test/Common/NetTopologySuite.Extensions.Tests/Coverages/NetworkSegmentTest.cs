using NetTopologySuite.Extensions.Coverages;
using NUnit.Framework;

namespace NetTopologySuite.Extensions.Tests.Coverages
{
    [TestFixture]
    public class NetworkSegmentTest
    {
        [Test]
        public void EndChainageDependsOnDirection()
        {
            var segment = new NetworkSegment { Chainage = 40, Length = 10, DirectionIsPositive = true };

            Assert.AreEqual(50,segment.EndChainage);
            
            //change direction changes the end offset
            segment.DirectionIsPositive = false;
            
            Assert.AreEqual(30, segment.EndChainage);
        }
    }
}