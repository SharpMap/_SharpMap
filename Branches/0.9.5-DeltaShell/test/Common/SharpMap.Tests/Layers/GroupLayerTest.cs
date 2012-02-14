using System;
using System.ComponentModel;
using NUnit.Framework;
using SharpMap.Layers;

namespace SharpMap.Tests.Layers
{
    [TestFixture]
    public class GroupLayerTest
    {
        [Test]
        public void EnablingChildLayerBubblesOnePropertyChangedEvent()
        {
            //this is needed to let the mapcontrol refresh see issue 2749
            int callCount = 0;
            var layerGroup = new GroupLayer();
            var childLayer = new VectorLayer();
            layerGroup.Layers.Add(childLayer);
            ((INotifyPropertyChanged)layerGroup).PropertyChanged += (sender, args) => callCount++;
            childLayer.Visible = false;
            
            Assert.AreEqual(1,callCount);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "It is not allowed to add or remove layers from a grouplayer that has a read-only layers collection")]
        public void MutatingAGroupLayerWithHasReadonlyLayerCollectionThrows()
        {
            var layerGroup = new GroupLayer {HasReadOnlyLayersCollection = true};
            var childLayer = new VectorLayer();
            layerGroup.Layers.Add(childLayer);
            
        }
    }
}