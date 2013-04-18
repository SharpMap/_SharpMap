using System.ComponentModel;
using System.Drawing;
using GisSharpBlog.NetTopologySuite.Geometries;
using NetTopologySuite.Extensions.Coverages;
using NUnit.Framework;
using SharpMap.Layers;

namespace SharpMap.Tests.Layers
{
    [TestFixture]
    public class RegularGridCoverageLayerTest
    {
        [Test]
        public void ChangesInLayersBubbleUpToMap()
        {
            var coverage = new RegularGridCoverage(10, 10, 1, 1);
            var layer = new RegularGridCoverageLayer { Coverage = coverage };
            int callCount = 0;
            var senders = new object[] {coverage, layer};
            var propertyNames = new[] {"Name", "RenderRequired"};

            ((INotifyPropertyChanged)layer).PropertyChanged += (sender,args)=>
            {
                Assert.AreEqual(senders[callCount], sender);
                Assert.AreEqual(propertyNames[callCount], args.PropertyName);

                callCount++;
            };

            //change the name of the layer
            coverage.Name = "new name";

            //should result in property changed of map
            Assert.AreEqual(2, callCount);
        }

        [Test]
        public void EnvelopeChangesWhenCoverageIsCleared()
        {
            var coverage = new RegularGridCoverage(10, 10, 1, 1);
            var layer = new RegularGridCoverageLayer { Coverage = coverage };
            var envelope = new Envelope(0, 10, 0, 10);
            Assert.AreEqual(envelope, layer.Envelope);

            coverage.Clear();
            var EmptyEnvelope = new Envelope(0, 0, 0, 0);
            Assert.AreEqual(EmptyEnvelope, layer.Envelope);
        }

        [Test]
        public void ClearingCoverageCauseRenderRequired()
        {
            var coverage = new RegularGridCoverage(10, 10, 1, 1);
            var layer = new RegularGridCoverageLayer { Coverage = coverage };
            layer.Map = new Map(new Size(10,10));
            layer.Render();
            Assert.IsFalse(layer.RenderRequired);

            //action!
            coverage.Clear();

            Assert.IsTrue(layer.RenderRequired);
        }
    }
}
