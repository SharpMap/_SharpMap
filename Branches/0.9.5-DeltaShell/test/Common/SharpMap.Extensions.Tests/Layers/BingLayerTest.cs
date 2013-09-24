using NUnit.Framework;
using SharpMap.Extensions.Layers;

using SharpTestsEx;

namespace SharpMap.Extensions.Tests.Layers
{
    [TestFixture]
    public class BingLayerTest
    {
        [Test]
        public void Clone()
        {
            var map = new Map();

            var layer = new BingLayer();
            map.Layers.Add(layer);

            var clone = (BingLayer)layer.Clone();
            Assert.IsNull(clone.Map);
        }

        [Test]
        public void CacheDirectoryIsDefined()
        {
            BingLayer.CacheLocation
                .Should("Cache directory is defined")
                .EndWith(@"cache_bing");
        }
    }
}
