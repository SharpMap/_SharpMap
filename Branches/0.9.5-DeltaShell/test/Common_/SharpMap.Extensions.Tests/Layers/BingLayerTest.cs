using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SharpMap.Extensions.Layers;

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
        
    }
}
