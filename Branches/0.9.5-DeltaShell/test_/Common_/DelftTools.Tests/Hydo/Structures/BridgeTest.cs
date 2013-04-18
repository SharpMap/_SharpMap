using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using DelftTools.Hydro;
using DelftTools.Hydro.Structures;
using NUnit.Framework;

namespace DelftTools.Tests.Hydo.Structures
{
    [TestFixture]
    public class BridgeTest
    {
        

        [Test]
        public void PropertyChangedForTabulatedCrossection()
        {
            int callCount = 0;
            //use a default 
            var bridge = new Bridge();
            bridge.TabulatedCrossSection.HeightFlowStorageWidthData.Add(new HeightFlowStorageWidth(10, 50, 50));
            bridge.TabulatedCrossSection.HeightFlowStorageWidthData.Add(new HeightFlowStorageWidth(16, 100, 100));
            
            var senders = new List<object>();
            var propertyNames = new List<string>();
            ((INotifyPropertyChanged) bridge).PropertyChanged += (s, e) =>
                                                                     {
                                                                         senders.Add(s);
                                                                         propertyNames.Add(e.PropertyName);
                                                                         callCount++;
                                                                     };
            
            bridge.TabulatedCrossSection.HeightFlowStorageWidthData[0].FlowingWidth = 22;
            Assert.AreEqual(new object[] { bridge.TabulatedCrossSection.HeightFlowStorageWidthData[0], bridge }, senders);
            Assert.AreEqual(new object[] { "FlowingWidth", "TabulatedCrossSection" }, propertyNames);

            //expect two property changes ..one for the heighflowstoragewidth, one 'translated' to tabulated crossection
            
            Assert.AreEqual(2,callCount);

        }
    }
}
