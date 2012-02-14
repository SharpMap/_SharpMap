using DelftTools.Hydro;
using NUnit.Framework;

namespace DelftTools.Tests.Hydo
{
    [TestFixture]
    public class ChannelTest
    {
        [Test]
        public void Clone()
        {
            var channel = new Channel { LongName = "Long" };
            var clone = (Channel)channel.Clone();

            //todo expand to cover functionality
            Assert.AreEqual(channel.LongName, clone.LongName);
        }
    }
}