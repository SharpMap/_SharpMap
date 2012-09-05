using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using DelftTools.Hydro;
using DelftTools.Hydro.CrossSections;
using DelftTools.Hydro.CrossSections.DataSets;
using DelftTools.Hydro.Structures;
using DelftTools.TestUtils;
using NUnit.Framework;

namespace DelftTools.Tests.Hydo.Structures
{
    [TestFixture]
    public class BridgeTest
    {
        [Test]
        [NUnit.Framework.Category(TestCategory.WorkInProgress)]
        public void PropertyChangedForTabulatedCrossection()
            // TODO: remove test? It seems to be testing NotifyPropertyChangeAspect
        {
            //TS: CrossSection is no longer sending property changed. It does have a manual property, but this would
            //require some hacking in Culvert to propogate this event through PostSharp. Since this seems only used
            //by the view, for now it has been solved there.

            int callCount = 0;
            //use a default 
            var bridge = new Bridge();
            bridge.TabulatedCrossSectionDefinition.SetWithHfswData(new[]
                                                             {
                                                                 new HeightFlowStorageWidth(10, 50, 50),
                                                                 new HeightFlowStorageWidth(16, 100, 100)
                                                             });

            ((INotifyPropertyChanged) bridge).PropertyChanged += (s, e) =>
                                                                     {
                                                                         Assert.AreEqual(
                                                                             bridge.TabulatedCrossSectionDefinition.
                                                                                 ZWDataTable[0], s);
                                                                         Assert.AreEqual("Width", e.PropertyName);
                                                                         callCount++;
                                                                     };

            CrossSectionDataSet.CrossSectionZWRow CrossSectionZWRow = bridge.TabulatedCrossSectionDefinition.ZWDataTable[0];
            CrossSectionZWRow.Width  = 22;

            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void CopyFrom()
        {
            var sourceBridge = new Bridge("source");
            var targetBridge = new Bridge("target")
                                   {
                                       InletLossCoefficient = 4.2,
                                       OutletLossCoefficient = 4.2,
                                       FlowDirection = FlowDirection.Positive,
                                       FrictionType = BridgeFrictionType.StricklerKs,
                                       Friction = 4.2,
                                       GroundLayerRoughness = 4.2,
                                       GroundLayerThickness = 1.1,
                                       BridgeType = BridgeType.Tabulated,
                                       BottomLevel = 4.2,
                                       Width = 4.2,
                                       Height = 4.2,
                                       OffsetY = 12.0,
                                       ShapeFactor = 1.1,
                                       PillarWidth = 11.12
                                   };
            targetBridge.CopyFrom(sourceBridge);
            Assert.AreEqual(sourceBridge.InletLossCoefficient, targetBridge.InletLossCoefficient);
            Assert.AreEqual(sourceBridge.OutletLossCoefficient, targetBridge.OutletLossCoefficient);
            Assert.AreEqual(sourceBridge.FlowDirection, targetBridge.FlowDirection);
            Assert.AreEqual(sourceBridge.FrictionType, targetBridge.FrictionType);
            Assert.AreEqual(sourceBridge.Friction, targetBridge.Friction);
            Assert.AreEqual(sourceBridge.GroundLayerEnabled, targetBridge.GroundLayerEnabled);
            Assert.AreEqual(sourceBridge.GroundLayerThickness, targetBridge.GroundLayerThickness);
            Assert.AreEqual(sourceBridge.GroundLayerRoughness, targetBridge.GroundLayerRoughness);
            Assert.AreEqual(sourceBridge.BottomLevel, targetBridge.BottomLevel);
            Assert.AreEqual(sourceBridge.Width, targetBridge.Width);
            Assert.AreEqual(sourceBridge.Height, targetBridge.Height);
            Assert.AreEqual(sourceBridge.OffsetY, targetBridge.OffsetY);
            Assert.AreEqual(sourceBridge.ShapeFactor, targetBridge.ShapeFactor);
            Assert.AreEqual(sourceBridge.PillarWidth, targetBridge.PillarWidth);
        }
    }
}