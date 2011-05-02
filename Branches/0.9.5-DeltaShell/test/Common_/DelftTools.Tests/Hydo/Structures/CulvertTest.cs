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
    public class CulvertTest
    {
        [Test]
        public void AbsoluteCrossSectionIncludesInletLevelForRectangle()
        {
            //set it up as rectangle
            var culvert = new Culvert();
            culvert.GeometryType = CulvertGeometryType.Rectangle;
            culvert.Width = 20;
            culvert.Height = 10;
            culvert.InletLevel = 5;
            
            //TODO: add a small spike on top of the crossection (for modelapi only)
            Assert.AreEqual(2, culvert.CrossSectionAtInletAbsolute.HeightFlowStorageWidthData.Count);

            //the inletlevel is included for the crossection. Is this ok for model api?
            Assert.AreEqual(5, culvert.CrossSectionAtInletAbsolute.HeightFlowStorageWidthData[0].Height);
            Assert.AreEqual(15, culvert.CrossSectionAtInletAbsolute.HeightFlowStorageWidthData[1].Height);

            Assert.AreEqual(20, culvert.CrossSectionAtInletAbsolute.HeightFlowStorageWidthData[0].FlowingWidth);
            Assert.AreEqual(20, culvert.CrossSectionAtInletAbsolute.HeightFlowStorageWidthData[1].FlowingWidth);

            Assert.AreEqual(20, culvert.CrossSectionAtInletAbsolute.HeightFlowStorageWidthData[0].TotalWidth);
            Assert.AreEqual(20, culvert.CrossSectionAtInletAbsolute.HeightFlowStorageWidthData[1].TotalWidth);

        }

        [Test]
        public void AbsoluteCrossSectionIncludesInletLevelForTabulated()
        {
            //set it up as rectangle
            var culvert = new Culvert();
            culvert.GeometryType = CulvertGeometryType.Tabulated;
            culvert.TabulatedCrossSection.HeightFlowStorageWidthData.Clear();
            culvert.TabulatedCrossSection.HeightFlowStorageWidthData.Add(new HeightFlowStorageWidth(0,20,20));
            culvert.TabulatedCrossSection.HeightFlowStorageWidthData.Add(new HeightFlowStorageWidth(10, 20, 20));
            culvert.InletLevel = 5;

            //TODO: add a small spike on top of the crossection (for modelapi only)
            Assert.AreEqual(2, culvert.CrossSectionAtInletAbsolute.HeightFlowStorageWidthData.Count);

            //the inletlevel is included for the crossection. Is this ok for model api?
            Assert.AreEqual(5, culvert.CrossSectionAtInletAbsolute.HeightFlowStorageWidthData[0].Height);
            Assert.AreEqual(15, culvert.CrossSectionAtInletAbsolute.HeightFlowStorageWidthData[1].Height);

            Assert.AreEqual(20, culvert.CrossSectionAtInletAbsolute.HeightFlowStorageWidthData[0].FlowingWidth);
            Assert.AreEqual(20, culvert.CrossSectionAtInletAbsolute.HeightFlowStorageWidthData[1].FlowingWidth);

            Assert.AreEqual(20, culvert.CrossSectionAtInletAbsolute.HeightFlowStorageWidthData[0].TotalWidth);
            Assert.AreEqual(20, culvert.CrossSectionAtInletAbsolute.HeightFlowStorageWidthData[1].TotalWidth);

        }

        [Test]
        public void PropertyChangedForTabulatedCrossection()
        {
            //since structureview only listens to changes in the structure itself a change in the crossection 
            //should cause a PC in the Culvert itself

            int callCount = 0;
            //use a default 
            var culvert = new Culvert();
            culvert.TabulatedCrossSection.HeightFlowStorageWidthData.Add(new HeightFlowStorageWidth());            
            var senders = new List<object>();
            var propertyNames = new List<string>();
            ((INotifyPropertyChanged)culvert).PropertyChanged += (s, e) =>
            {
                senders.Add(s);
                propertyNames.Add(e.PropertyName);
                callCount++;
            };

            culvert.TabulatedCrossSection.HeightFlowStorageWidthData[0].FlowingWidth = 22;
            Assert.AreEqual(new object[] { culvert.TabulatedCrossSection.HeightFlowStorageWidthData[0], culvert }, senders);
            Assert.AreEqual(new object[] { "FlowingWidth", "TabulatedCrossSection" }, propertyNames);

            //expect two property changes ..one for the heighflowstoragewidth, one 'translated' to tabulated crossection

            Assert.AreEqual(2, callCount);

        }

    }
}
