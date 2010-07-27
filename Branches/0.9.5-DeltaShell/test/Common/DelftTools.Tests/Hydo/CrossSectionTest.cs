using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DelftTools.Hydro;
using DelftTools.Hydro.Helpers;
using DelftTools.Utils.Collections;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;
using NetTopologySuite.Extensions.Networks;
using NUnit.Framework;

namespace DelftTools.Tests.Hydo
{
    [TestFixture]
    public class CrossSectionTest
    {
        // Create default cross section for cs not linked to branch; expect exception
        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void TestDefaultCrossSectionGeometry()
        {
            var crossSection = new CrossSection();
            CrossSectionHelper.CreateDefaultYZTableAndGeometryForYZCrossSection(crossSection, 100.0);
        }

        [Test]
        [NUnit.Framework.Category("Integration")]
        public void CreateDefaultCrossSectionTest()
        {
            CrossSection crossSection = CreateDefaultCrossSection();
            // The geometry length is calculated only in x, y and is 100
            Assert.AreEqual(100.0, crossSection.Geometry.Length, 1.0e-6);

            // the total delta x; used as y for the crosssectional view should be 100.
            double length =
                Math.Abs(crossSection.YZValues[crossSection.YZValues.Count - 1].X - crossSection.YZValues[0].X);
            Assert.AreEqual(100.0, length, 1.0e-6);
        }

        [Test]
        public void SetZValueCausesPropertyChangedForXYZCrossSection()
        {
            IChannel channel = new Channel { Geometry = new LineString(new[] { new Coordinate(0, 0), new Coordinate(20, 0) }) };
            var crossSection = new CrossSection { Branch = channel };

            NetworkHelper.AddBranchFeatureToBranch(channel, crossSection, 10.0);

            var yzCoordinates = new List<ICoordinate>
                                    {
                                        new Coordinate(0.0, 0.0),
                                        new Coordinate(100.0, 0.0),
                                    };

            crossSection.Geometry = CrossSectionHelper.CreateCrossSectionGeometryForXyzCrossSectionFromYZ(channel.Geometry,
                                                                                         crossSection.Offset,
                                                                                         yzCoordinates);

            int callCount = 0;
            ((INotifyPropertyChanged)(crossSection)).PropertyChanged += (s, e) =>
                                                                             {
                                                                                 callCount++;
                                                                                 Assert.AreEqual("Geometry",
                                                                                                 e.PropertyName);
                                                                             };

            crossSection.SetZValue(0, 100);
            Assert.AreEqual(1, callCount);

        }

        [Test]
        public void ChangeRoughnessTypeCausesPropertyChanged()
        {
            IChannel channel = new Channel { Geometry = new LineString(new[] { new Coordinate(0, 0), new Coordinate(20, 0) }) };
            var crossSection = new CrossSection { Branch = channel, CrossSectionType = CrossSectionType.YZTable };

            CrossSectionHelper.CreateDefaultYZTableAndGeometryForYZCrossSection(crossSection, 100.0);
            crossSection.SetRoughness(10.0, 1.0, RoughnessType.Chezy, "");

            int callCount = 0;
            ((INotifyPropertyChanged)(crossSection)).PropertyChanged += (s, e) =>
                                                                             {
                                                                                 callCount++;
                                                                                 Assert.AreEqual("RoughnessType",
                                                                                                 e.PropertyName);
                                                                             };

            crossSection.RoughnessSections[0].RoughnessType = RoughnessType.StricklerKn;
            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void DefaultFriction()
        {
            CrossSection crossSection = CreateDefaultCrossSection();

            double friction = crossSection.GetCrossSectionRoughnessSection(0.0).Roughness;
            RoughnessType roughnessType = crossSection.GetCrossSectionRoughnessSection(0.0).RoughnessType;
            Assert.AreEqual(45.0, friction, 1.0e-6);
            Assert.AreEqual(RoughnessType.Chezy, roughnessType);
        }

        private static CrossSection CreateDefaultCrossSection()
        {
            IChannel channel = new Channel { Geometry = new LineString(new[] { new Coordinate(0, 0), new Coordinate(20, 0) }) };
            var crossSection = new CrossSection { Branch = channel, CrossSectionType = CrossSectionType.YZTable };
            CrossSectionHelper.CreateDefaultYZTableAndGeometryForYZCrossSection(crossSection, 100.0);
            return crossSection;
        }


        [Test]
        public void RoughnessDefaultValue()
        {
            CrossSection crossSection = CreateDefaultCrossSection();
            crossSection.DefaultRoughnessValue = 10.0;

            double friction = crossSection.GetCrossSectionRoughnessSection(0.0).Roughness;
            Assert.AreEqual(10.0, friction, 1.0e-6);
        }

        [Test]
        public void RoughnessDefaultType()
        {
            CrossSection crossSection = CreateDefaultCrossSection();
            crossSection.DefaultRoughnessValue = 10.0;

            RoughnessType roughnessType = crossSection.GetCrossSectionRoughnessSection(0.0).RoughnessType;
            Assert.AreEqual(RoughnessType.Chezy, roughnessType);
        }

        [Test]
        public void RoughnessValueAndTypeUsingCurveCoverage()
        {
            // obsolete
            //CrossSection crossSection = CreateDefaultCrossSection();
            //const double offset = 10.0;
            //crossSection.Friction[new FeatureLocation { Feature = crossSection, Offset = offset }] = new object[]
            //                                                                                       {
            //                                                                                           88.0,
            //                                                                                           roughnessType.DeBosandBijkerk,
            //                                                                                           9
            //                                                                                       };
            //IMultiDimensionalArray<double> frictionValue = crossSection.Friction.GetValues<double>(
            //    new ComponentFilter(crossSection.Friction.Components[0]),
            //    new VariableValueFilter<FeatureLocation>(crossSection.Friction.Arguments[0],
            //                                             new FeatureLocation {Feature = crossSection, Offset = offset})
            //    );

            //IMultiDimensionalArray<roughnessType> frictionType = crossSection.Friction.GetValues<roughnessType>(
            //    new ComponentFilter(crossSection.Friction.Components[1]),
            //    new VariableValueFilter<FeatureLocation>(crossSection.Friction.Arguments[0],
            //                                             new FeatureLocation {Feature = crossSection, Offset = offset})
            //    );

            //IMultiDimensionalArray<int> frictionClass = crossSection.Friction.GetValues<int>(
            //    new ComponentFilter(crossSection.Friction.Components[2]),
            //    new VariableValueFilter<FeatureLocation>(crossSection.Friction.Arguments[0],
            //                                             new FeatureLocation {Feature = crossSection, Offset = offset})
            //    );

            //Assert.AreEqual(roughnessType.DeBosandBijkerk, frictionType[0]);
            //Assert.AreEqual(88.0, frictionValue[0]);
            //Assert.AreEqual(9, frictionClass[0]);
        }

        [Test]
        public void RoughnessValueAndType()
        {
            CrossSection crossSection = CreateDefaultCrossSection();
            const double offset = 10.0;
            crossSection.SetRoughness(offset, 88.0, RoughnessType.DeBosandBijkerk, "9");
            Assert.AreEqual(RoughnessType.DeBosandBijkerk, crossSection.GetCrossSectionRoughnessSection(offset).RoughnessType);
            Assert.AreEqual(88.0, crossSection.GetCrossSectionRoughnessSection(offset).Roughness, 1.0e-6);
            Assert.AreEqual("9", crossSection.GetCrossSectionRoughnessSection(offset).Name);
        }

        [Test]
        public void AddingYZValuesDoesNotCauseCollectionChanged()
        {
            //collection changed of YZValues are bubbled a lot. This is a big performance penaly
            var crossSection = new CrossSection();
            crossSection.CrossSectionType = CrossSectionType.YZTable;

            ((INotifyCollectionChanged)(crossSection)).CollectionChanged +=
                (s, e) =>
                {
                    Assert.Fail("Should not be called!");
                };
            crossSection.YZValues.Add(new Coordinate(0, 0, 0));
        }

        [Test]
        public void Clone()
        {
            var crossSection = new CrossSection();
            //don set static members ..will mess up other tests
            //crossSection.DefaultRoughnessType = RoughnessType.StricklerKn;
            
            //action ! clone
            var clone = (CrossSection)crossSection.Clone();

            Assert.AreEqual(crossSection.DefaultRoughnessType,clone.DefaultRoughnessType);
        }


        [Test]
        public void SetReferenceLevelYZTest()
        {
            CrossSection crossSection = new CrossSection { CrossSectionType = CrossSectionType.YZTable };
            crossSection.YZValues.Add(new Coordinate(0, 0));
            crossSection.YZValues.Add(new Coordinate(0, -10));
            crossSection.YZValues.Add(new Coordinate(20, -10));
            crossSection.YZValues.Add(new Coordinate(20, 0));

            Assert.AreEqual(-10.0, crossSection.LowestPoint, 1.0e-6);
            Assert.AreEqual(0.0, crossSection.HighestPoint, 1.0e-6);

            crossSection.LevelShift(111);

            Assert.AreEqual(111.0, crossSection.YZValues[0].Y, 1.0e-6);
            Assert.AreEqual(101.0, crossSection.YZValues[1].Y, 1.0e-6);
            Assert.AreEqual(101.0, crossSection.YZValues[2].Y, 1.0e-6);
            Assert.AreEqual(111.0, crossSection.YZValues[3].Y, 1.0e-6);

            Assert.AreEqual(101.0, crossSection.LowestPoint, 1.0e-6);
            Assert.AreEqual(111.0, crossSection.HighestPoint, 1.0e-6);
        }

        [Test]
        public void SetReferenceLevelHeightWidthWidthTest()
        {
            CrossSection crossSection = new CrossSection { CrossSectionType = CrossSectionType.HeightFlowStorageWidth };
            crossSection.HeightFlowStorageWidthData.Add(new HeightFlowStorageWidth(0, 10.0, 10.0));
            crossSection.HeightFlowStorageWidthData.Add(new HeightFlowStorageWidth(10, 100.0, 100.0));

            Assert.AreEqual(0.0, crossSection.LowestPoint, 1.0e-6);
            Assert.AreEqual(10.0, crossSection.HighestPoint, 1.0e-6);

            crossSection.LevelShift(111);

            Assert.AreEqual(111.0, crossSection.HeightFlowStorageWidthData[0].Height, 1.0e-6);
            Assert.AreEqual(121.0, crossSection.HeightFlowStorageWidthData[1].Height, 1.0e-6);

            Assert.AreEqual(111.0, crossSection.LowestPoint, 1.0e-6);
            Assert.AreEqual(121.0, crossSection.HighestPoint, 1.0e-6);
        }

        [Test]
        public void SetReferenceLevelGeometry()
        {
            var crossSection = new CrossSection();
            var coordinates = new List<ICoordinate>
                                  {
                                      new Coordinate(0, 0, 0),
                                      new Coordinate(10, 0, 0),
                                      new Coordinate(30, 0, 0),
                                      new Coordinate(40, 0, 0)
                                  };
            crossSection.Geometry = new LineString(coordinates.ToArray());


            Assert.AreEqual(0.0, crossSection.LowestPoint, 1.0e-6);
            Assert.AreEqual(0.0, crossSection.HighestPoint, 1.0e-6);

            crossSection.LevelShift(111);

            Assert.AreEqual(111.0, crossSection.Geometry.Coordinates[0].Z, 1.0e-6);
            Assert.AreEqual(111.0, crossSection.Geometry.Coordinates[0].Z, 1.0e-6);

            Assert.AreEqual(111.0, crossSection.LowestPoint, 1.0e-6);
            Assert.AreEqual(111.0, crossSection.HighestPoint, 1.0e-6);
        }

        [Test]
        public void ListenCarefulllyItShouldGeometryChangeforLevelShiftOnlyOnce()
        {
            CrossSection crossSection = new CrossSection { CrossSectionType = CrossSectionType.YZTable };
            crossSection.YZValues.Add(new Coordinate(0, 0));
            crossSection.YZValues.Add(new Coordinate(20, 0));

            int callCount = 0;

            ((INotifyPropertyChanged)(crossSection)).PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "Geometry")
                {
                    callCount++;
                }
            };

            crossSection.LevelShift(111);

            Assert.AreEqual(1, callCount);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void YZValuesGivesExceptionWhenMonotonousAscending()
        {
            var crossSection = new CrossSection { CrossSectionType = CrossSectionType.HeightFlowStorageWidth };
            /* 'invalid' since it flows back like this:
               / \
              |   |
               \ /
            */
            crossSection.HeightFlowStorageWidthData.Add(new HeightFlowStorageWidth(0,1,1));
            crossSection.HeightFlowStorageWidthData.Add(new HeightFlowStorageWidth(10, 20, 20));
            crossSection.HeightFlowStorageWidthData.Add(new HeightFlowStorageWidth(20,1, 1));

            //action! this should result in a exception
            var values = crossSection.YZValues;
        }


        [Test]
        public void YZValuesCanFlowIfValidationIsTurnedOff()
        {
            var crossSection = new CrossSection { CrossSectionType = CrossSectionType.HeightFlowStorageWidth };
            /* crossection flows back like this:
               / \
              |   |
               \ /
            */
            crossSection.HeightFlowStorageWidthData.Add(new HeightFlowStorageWidth(0, 1, 1));
            crossSection.HeightFlowStorageWidthData.Add(new HeightFlowStorageWidth(10, 20, 20));
            crossSection.HeightFlowStorageWidthData.Add(new HeightFlowStorageWidth(20, 1, 1));

            //set up the crossection to not throw an exception.
            crossSection.ThrowExceptionIfTabulatedCrossSectionIsNotMonotonousAscending = false;
            
            var xValues = crossSection.YZValues.Select(c => c.X).ToList();
            Assert.AreEqual(new[]{-0.5,-10,-0.5,0.5,10,0.5},xValues);

            var yValues = crossSection.YZValues.Select(c => c.Y).ToList();
            Assert.AreEqual(new[] { 20, 10, 0, 0, 10, 20}, yValues);
        }
    }
}
