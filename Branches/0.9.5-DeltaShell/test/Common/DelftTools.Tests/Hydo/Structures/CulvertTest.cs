using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using DelftTools.Hydro;
using DelftTools.Hydro.CrossSections;
using DelftTools.Hydro.Structures;
using DelftTools.TestUtils;
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
            Assert.AreEqual(2, culvert.CrossSectionDefinitionAtInletAbsolute.ZWDataTable.Count);

            //the inletlevel is included for the crossection. Is this ok for model api?
            Assert.AreEqual(5, culvert.CrossSectionDefinitionAtInletAbsolute.ZWDataTable[0].Z);
            Assert.AreEqual(15, culvert.CrossSectionDefinitionAtInletAbsolute.ZWDataTable[1].Z);

            Assert.AreEqual(0, culvert.CrossSectionDefinitionAtInletAbsolute.ZWDataTable[0].StorageWidth);
            Assert.AreEqual(0, culvert.CrossSectionDefinitionAtInletAbsolute.ZWDataTable[1].StorageWidth);

            Assert.AreEqual(20, culvert.CrossSectionDefinitionAtInletAbsolute.ZWDataTable[0].Width);
            Assert.AreEqual(20, culvert.CrossSectionDefinitionAtInletAbsolute.ZWDataTable[1].Width);
        }

        [Test]
        public void AbsoluteCrossSectionIncludesInletLevelForTabulated()
        {
            //set it up as rectangle
            var culvert = new Culvert();
            culvert.GeometryType = CulvertGeometryType.Tabulated;
            culvert.TabulatedCrossSectionDefinition.SetWithHfswData(new[]
                                                              {
                                                                  new HeightFlowStorageWidth(0, 20, 20),
                                                                  new HeightFlowStorageWidth(10, 20, 20)
                                                              });
            culvert.InletLevel = 5;

            //TODO: add a small spike on top of the crossection (for modelapi only)
            Assert.AreEqual(2, culvert.CrossSectionDefinitionAtInletAbsolute.ZWDataTable.Count);

            //the inletlevel is included for the crossection. Is this ok for model api?
            Assert.AreEqual(5, culvert.CrossSectionDefinitionAtInletAbsolute.ZWDataTable[0].Z);
            Assert.AreEqual(15, culvert.CrossSectionDefinitionAtInletAbsolute.ZWDataTable[1].Z);

            Assert.AreEqual(0, culvert.CrossSectionDefinitionAtInletAbsolute.ZWDataTable[0].StorageWidth);
            Assert.AreEqual(0, culvert.CrossSectionDefinitionAtInletAbsolute.ZWDataTable[1].StorageWidth);

            Assert.AreEqual(20, culvert.CrossSectionDefinitionAtInletAbsolute.ZWDataTable[0].Width);
            Assert.AreEqual(20, culvert.CrossSectionDefinitionAtInletAbsolute.ZWDataTable[1].Width);
        }

        [Test]
        [NUnit.Framework.Category(TestCategory.WorkInProgress)]
        public void PropertyChangedForTabulatedCrossection()
        {
            //TS: CrossSection is no longer sending property changed. It does have a manual property, but this would
            //require some hacking in Culvert to propogate this event through PostSharp. Since this seems only used
            //by the view, for now it has been solved there.

            //since structureview only listens to changes in the structure itself a change in the crossection 
            //should cause a PC in the Culvert itself

            int callCount = 0;
            //use a default 
            var culvert = new Culvert();
            culvert.TabulatedCrossSectionDefinition.ZWDataTable.AddCrossSectionZWRow(0, 0, 0);

            ((INotifyPropertyChanged) culvert).PropertyChanged += (s, e) =>
                                                                      {
                                                                          Assert.AreEqual(
                                                                              culvert.TabulatedCrossSectionDefinition.
                                                                                  ZWDataTable[0], s);
                                                                          Assert.AreEqual("Width", e.PropertyName);
                                                                          callCount++;
                                                                      };

            culvert.TabulatedCrossSectionDefinition.ZWDataTable[0].Width = 22;

            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void CopyIntoSiphon()
        {
            var siphon = new Culvert {IsSiphon = true};
            var culvert = new Culvert {FlowDirection = FlowDirection.Both};
            siphon.CopyFrom(culvert);
            Assert.IsFalse(siphon.IsSiphon);
            Assert.AreEqual(FlowDirection.Both,siphon.FlowDirection);
        }

        [Test]
        public void CopyFrom()
        {
            var targetCulvert = new Culvert("target");
            var sourceCulvert = new Culvert("source")
            
                                    {
                                        Diameter = 20.0,
                                        FlowDirection = FlowDirection.Positive,
                                        Friction = 3.0,
                                        FrictionType = CulvertFrictionType.WhiteColebrook,
                                        GateInitialOpening = 4.2,
                                        //GateOpeningLossCoefficientFunction = ,
                                        GeometryType = CulvertGeometryType.SteelCunette,
                                        Height = 5.0,
                                        InletLevel = 3.11,
                                        InletLossCoefficient = 0.42,
                                        IsGated = true,
                                        IsSiphon = true,
                                        OutletLevel = 0.42,
                                        OutletLossCoefficient = 0.42,
                                        Radius = 42.0,
                                        Radius1 = 42.1,
                                        Radius2 = 42.3,
                                        Radius3 = 42.4,
                                        SiphonOffLevel = 1.2,
                                        SiphonOnLevel = 1.2,
                                        TabulatedCrossSectionDefinition =
                                            new CrossSectionDefinitionZW(),
                                        Width = 14.0,
                                        GroundLayerRoughness = 0.42,
                                        GroundLayerThickness = 4.2
                                    };
            targetCulvert.CopyFrom(sourceCulvert);
            Assert.AreEqual(sourceCulvert.Diameter, targetCulvert.Diameter);
            Assert.AreEqual(sourceCulvert.FlowDirection, targetCulvert.FlowDirection);
            Assert.AreEqual(sourceCulvert.Friction, targetCulvert.Friction);
            Assert.AreEqual(sourceCulvert.FrictionType, targetCulvert.FrictionType);
            Assert.AreEqual(sourceCulvert.GateInitialOpening, targetCulvert.GateInitialOpening);
            Assert.AreEqual(sourceCulvert.GeometryType, targetCulvert.GeometryType);
            Assert.AreEqual(sourceCulvert.Height, targetCulvert.Height);
            Assert.AreEqual(sourceCulvert.InletLevel, targetCulvert.InletLevel);
            Assert.AreEqual(sourceCulvert.OutletLossCoefficient, targetCulvert.OutletLossCoefficient);
            Assert.AreEqual(sourceCulvert.Radius, targetCulvert.Radius);
            Assert.AreEqual(sourceCulvert.SiphonOffLevel, targetCulvert.SiphonOffLevel);
            Assert.AreEqual(sourceCulvert.TabulatedCrossSectionDefinition.CrossSectionType, targetCulvert.TabulatedCrossSectionDefinition.CrossSectionType);
            Assert.AreEqual(sourceCulvert.Width, targetCulvert.Width);
            Assert.AreEqual(sourceCulvert.GroundLayerThickness, targetCulvert.GroundLayerThickness);
            Assert.AreEqual(sourceCulvert.GroundLayerRoughness, targetCulvert.GroundLayerRoughness);
            Assert.AreNotEqual(sourceCulvert.Name, targetCulvert.Name);
        }
        [Test]
        public void NoNegativeFlowForSiphon()
        {
            var culvert = new Culvert();
            Assert.IsTrue(culvert.AllowNegativeFlow);
            culvert.IsSiphon = true;
            Assert.IsFalse(culvert.AllowNegativeFlow);
            
        }

        [Test]
        public void ValidFlowForSiphon()
        {
            var culvert = new Culvert();
            Assert.IsTrue(culvert.AllowNegativeFlow);
            culvert.IsSiphon = true;
            Assert.IsFalse(culvert.AllowNegativeFlow);
            culvert.FlowDirection = FlowDirection.Positive;
            culvert.FlowDirection = FlowDirection.None;
        }

        [Test]
        [ExpectedException(typeof(ArgumentException), ExpectedMessage = "Negative flow is not allowed for siphons")]
        public void InValidFlowForSiphonThrowsException()
        {
            var culvert = new Culvert();
            Assert.IsTrue(culvert.AllowNegativeFlow);
            culvert.IsSiphon = true;
            Assert.IsFalse(culvert.AllowNegativeFlow);
            culvert.FlowDirection = FlowDirection.Both;
        }
    }

    
}