using System;
using System.Collections.Generic;
using DelftTools.Hydro;
using DelftTools.Hydro.Helpers;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;
using NetTopologySuite.Extensions.Networks;
using NUnit.Framework;

namespace DelftTools.Tests.Hydo.Helpers
{
    [TestFixture]
    public class CrossSectionHelperTest
    {
        [Test]
        [ExpectedException(typeof (ArgumentException))]
        public void SetDefaultGeometryNoBranche()
        {
            var crossSection = new CrossSection();
            CrossSectionHelper.SetDefaultGeometry(crossSection, 1);
        }
        [Test]
        public void AddDefaultZToGeometry()
        {
            var crossSection = new CrossSection();
            // just a horizontal line from 10 to 40. Now just set z to 0 to make it work. 
            // could be something more fancy.
            var coordinates = new List<ICoordinate>
                                  {
                                      new Coordinate(0, 0),
                                      new Coordinate(10, 0),
                                      new Coordinate(20, 0),
                                      new Coordinate(30, 0),
                                      new Coordinate(40, 0)
                                  };
            crossSection.Geometry = new LineString(coordinates.ToArray());
            CrossSectionHelper.AddDefaultZToGeometry(crossSection);
            Assert.AreEqual(10.0, crossSection.Geometry.Coordinates[0].Z);
            Assert.AreEqual(5.0, crossSection.Geometry.Coordinates[1].Z);
            Assert.AreEqual(0.0, crossSection.Geometry.Coordinates[2].Z);
            Assert.AreEqual(5.0, crossSection.Geometry.Coordinates[3].Z);
            Assert.AreEqual(10.0, crossSection.Geometry.Coordinates[4].Z);
        }

        [Test]
        public void SetDefaultGeometryWithBrancheGeometry()
        {
            const double defaultLength = 2; 
            var hydroNetwork = new HydroNetwork();

            var channel = new Channel {Geometry = new LineString(new[] {new Coordinate(0, 0), new Coordinate(20, 0)})};

            var crossSection = new CrossSection
                                   {
                                       CrossSectionType = CrossSectionType.GeometryBased,
                                       Geometry = new LineString(new[] {new Coordinate(10, 0), new Coordinate(10, 0)}),
                                       ThalWay = defaultLength/2
                                   };

            hydroNetwork.Branches.Add(channel);
            NetworkHelper.AddBranchFeatureToBranch(channel, crossSection, crossSection.Offset);
            crossSection.Offset = 12;

            CrossSectionHelper.SetDefaultGeometry(crossSection, defaultLength);

            Assert.AreEqual(defaultLength, crossSection.Geometry.Length);
            Assert.AreEqual(2, crossSection.Geometry.Coordinates.Length);

            Assert.AreEqual(12, crossSection.Geometry.Coordinates[0].X);
            Assert.AreEqual(12, crossSection.Geometry.Coordinates[1].X);
            Assert.AreEqual(-defaultLength/2, crossSection.Geometry.Coordinates[0].Y);
            Assert.AreEqual(defaultLength/2, crossSection.Geometry.Coordinates[1].Y);
        }


        /// <summary>
        /// Tests the creation of a simple linestring (only coordinate at start and end) geometry for a cross section
        /// </summary>
        [Test]
        public void TabulatedToSimpleGeometryTest()
        {
            var branchGeometry = new LineString(new[] {new Coordinate(0, 0), new Coordinate(100, 0)});
            IList<HeightFlowStorageWidth> tabulatedCrossSectionData = new List<HeightFlowStorageWidth>
                                                         {
                                                             // height, totalwidth, flowingwidth
                                                             new HeightFlowStorageWidth(0.0, 20.0, 10.0),
                                                             new HeightFlowStorageWidth(10.0, 30.0, 12.0),
                                                             new HeightFlowStorageWidth(20.0, 20.0, 10.0)
                                                         };
            IGeometry geometry = CrossSectionHelper.CreateCrossSectionGeometryFromTabulated(branchGeometry, 30, tabulatedCrossSectionData);
            // The expected length of the geometry is the maximum of the flowwidth
            Assert.AreEqual(12.0, geometry.Length, 1.0e-6);
            Assert.IsInstanceOfType(typeof(LineString), geometry);
            Assert.AreEqual(2, geometry.Coordinates.Length);
        }

        /// <summary>
        /// Tests the creation of a simple linestring (only coordinate at start and end) geometry for a cross section
        /// </summary>
        [Test]
        public void YZTableToSimpleGeometryTest()
        {
            var branchGeometry = new LineString(new[] { new Coordinate(0, 0), new Coordinate(100, 0) });
            IList<ICoordinate> yzCoordinates = new List<ICoordinate>
                                                         {
                                                             // note: x, y of coordinate are interpreted as the yz for 
                                                             // the cross section.
                                                             new Coordinate(0.0, 0.0),
                                                             new Coordinate(5.0, -20.0),
                                                             new Coordinate(15.0, -20.0),
                                                             new Coordinate(20.0, 0.0)
                                                         };
            IGeometry geometry = CrossSectionHelper.CreateCrossSectionGeometryFromYz(branchGeometry, 30, yzCoordinates);
            Assert.AreEqual(20.0, geometry.Length, 1.0e-6);
            Assert.IsInstanceOfType(typeof(LineString), geometry);
            Assert.AreEqual(2, geometry.Coordinates.Length);
        }

        /// <summary>
        /// Tests the creation of a simple linestring (only coordinate at start and end) geometry for a cross section
        /// on a horizontal branch. The expected cross section geometry will thus be vertical.
        /// </summary>
        [Test]
        public void YZTableToXyzGeometryAtHorizontalBranchTest()
        {
            var branchGeometry = new LineString(new[] { new Coordinate(11, 0), new Coordinate(111, 0) });
            IList<ICoordinate> yzCoordinates = new List<ICoordinate>
                                                         {
                                                             // note: x, y of coordinate are interpreted as the yz for 
                                                             // the cross section.
                                                             new Coordinate(0.0, 0.0),
                                                             new Coordinate(5.0, -20.0),
                                                             new Coordinate(15.0, -20.0),
                                                             new Coordinate(20.0, 0.0)
                                                         };
            IGeometry geometry = CrossSectionHelper.CreateCrossSectionGeometryForXyzCrossSectionFromYZ(branchGeometry, 30, yzCoordinates);
            Assert.AreEqual(20.0, geometry.Length, 1.0e-6);
            Assert.IsInstanceOfType(typeof(LineString), geometry);
            Assert.AreEqual(4, geometry.Coordinates.Length);
            // test if z values are set correctly to geometry
            Assert.AreEqual(0.0, geometry.Coordinates[0].Z, 1.0e-6);
            Assert.AreEqual(-20.0, geometry.Coordinates[1].Z, 1.0e-6);
            Assert.AreEqual(-20.0, geometry.Coordinates[2].Z, 1.0e-6);
            Assert.AreEqual(0.0, geometry.Coordinates[3].Z, 1.0e-6);
            // test is geometry is a vertical line
            Assert.AreEqual(41.0, geometry.Coordinates[0].X, 1.0e-6);
            Assert.AreEqual(41.0, geometry.Coordinates[1].X, 1.0e-6);
            Assert.AreEqual(41.0, geometry.Coordinates[2].X, 1.0e-6);
            Assert.AreEqual(41.0, geometry.Coordinates[3].X, 1.0e-6);
            //
            //                  cs[count-1] (10)
            //                        |
            // branch start(11) ------41---------------> end (111)
            //                        |
            //                   cs[0] (-10)
            //
            Assert.AreEqual(-10.0, geometry.Coordinates[0].Y, 1.0e-6);
            Assert.AreEqual(10.0, geometry.Coordinates[3].Y, 1.0e-6);
        }

        /// <summary>
        /// Tests the creation of a simple linestring (only coordinate at start and end) geometry for a cross section
        /// on a vertical branch. The expected cross section geometry will thus be vertical.
        /// </summary>
        [Test]
        public void YZTableToXyzGeometryAtVerticalBranchTest()
        {
            var branchGeometry = new LineString(new[] { new Coordinate(0, 11), new Coordinate(0, 111) });
            IList<ICoordinate> yzCoordinates = new List<ICoordinate>
                                                         {
                                                             // note: x, y of coordinate are interpreted as the yz for 
                                                             // the cross section.
                                                             new Coordinate(0.0, 0.0),
                                                             new Coordinate(5.0, -20.0),
                                                             new Coordinate(15.0, -20.0),
                                                             new Coordinate(20.0, 0.0)
                                                         };
            IGeometry geometry = CrossSectionHelper.CreateCrossSectionGeometryForXyzCrossSectionFromYZ(branchGeometry, 30, yzCoordinates);
            Assert.AreEqual(20.0, geometry.Length, 1.0e-6);
            Assert.AreEqual(41.0, geometry.Coordinates[0].Y, 1.0e-6);
            Assert.AreEqual(41.0, geometry.Coordinates[1].Y, 1.0e-6);
            Assert.AreEqual(41.0, geometry.Coordinates[2].Y, 1.0e-6);
            Assert.AreEqual(41.0, geometry.Coordinates[3].Y, 1.0e-6);
        }

        /// <summary>
        /// Tests the creation of a simple linestring (only coordinate at start and end) geometry for a cross section
        /// on a horizontal branch. The expected cross section geometry will thus be vertical.
        /// </summary>
        [Test]
        public void YZTableToXyzGeometryAtInvertedHorizontalBranchTest()
        {
            var branchGeometry = new LineString(new[] { new Coordinate(111, 0), new Coordinate(11, 0) });
            IList<ICoordinate> yzCoordinates = new List<ICoordinate>
                                                         {
                                                             // note: x, y of coordinate are interpreted as the yz for 
                                                             // the cross section.
                                                             new Coordinate(0.0, 0.0),
                                                             new Coordinate(5.0, -20.0),
                                                             new Coordinate(15.0, -20.0),
                                                             new Coordinate(20.0, 0.0)
                                                         };
            IGeometry geometry = CrossSectionHelper.CreateCrossSectionGeometryForXyzCrossSectionFromYZ(branchGeometry, 30, yzCoordinates);
            Assert.AreEqual(20.0, geometry.Length, 1.0e-6);
            Assert.AreEqual(81.0, geometry.Coordinates[0].X, 1.0e-6);
            Assert.AreEqual(81.0, geometry.Coordinates[1].X, 1.0e-6);
            Assert.AreEqual(81.0, geometry.Coordinates[2].X, 1.0e-6);
            Assert.AreEqual(81.0, geometry.Coordinates[3].X, 1.0e-6);
            //
            //                             cs[0] (10)
            //                                  |
            // branch end(11) <-----------------81------ start (111)
            //                                  |
            //                          cs[count-1] (-10)
            //
            Assert.AreEqual(10.0, geometry.Coordinates[0].Y, 1.0e-6);
            Assert.AreEqual(-10.0, geometry.Coordinates[3].Y, 1.0e-6);
        }

        /// <summary>
        /// Tests the creation of a simple linestring (only coordinate at start and end) geometry for a cross section
        /// on a horizontal branch. The expected cross section geometry will thus be vertical.
        /// </summary>
        [Test]
        public void YZTableToXyzGeometryAt90DegreeBranchTest()
        {
            var branchGeometry = new LineString(new[] { new Coordinate(11, 22), new Coordinate(111, 122) });
            IList<ICoordinate> yzCoordinates = new List<ICoordinate>
                                                         {
                                                             // note: x, y of coordinate are interpreted as the yz for 
                                                             // the cross section.
                                                             new Coordinate(0.0, 0.0),
                                                             new Coordinate(5.0, -20.0),
                                                             new Coordinate(15.0, -20.0),
                                                             new Coordinate(20.0, 0.0)
                                                         };
            IGeometry geometry = CrossSectionHelper.CreateCrossSectionGeometryForXyzCrossSectionFromYZ(branchGeometry, 30, yzCoordinates);
            Assert.AreEqual(20.0, geometry.Length, 1.0e-6);

            //
            //
            //                 111, 122
            //                   ^
            //                  /
            //                 /
            //                /
            //               /
            //  cs[count-1] /
            //           \ /
            //            +
            //           / \cs[0]
            //          /
            //      11, 22
            //
            Assert.Greater(geometry.Coordinates[0].X, geometry.Coordinates[1].X);
            Assert.Greater(geometry.Coordinates[1].X, geometry.Coordinates[2].X);
            Assert.Greater(geometry.Coordinates[2].X, geometry.Coordinates[3].X);

            Assert.Less(geometry.Coordinates[0].Y, geometry.Coordinates[1].Y);
            Assert.Less(geometry.Coordinates[1].Y, geometry.Coordinates[2].Y);
            Assert.Less(geometry.Coordinates[2].Y, geometry.Coordinates[3].Y);
        }

        [Test]
        public void YZProfileFromHeightFlowStorageWidth()
        {
            // [---------------------------] width 20
            //
            //
            //
            //       [---------------] width 15
            IList<HeightFlowStorageWidth> tabulatedData = new List<HeightFlowStorageWidth>
                                                                 {
                                                                     new HeightFlowStorageWidth(-10.0, 25, 15),
                                                                     new HeightFlowStorageWidth(0.0, 30, 20)
                                                                 };
            IList<ICoordinate> yZValues = new List<ICoordinate>();
            CrossSectionHelper.CalculateYZProfileFromTabulatedCrossSection(yZValues, tabulatedData,"",true);
            Assert.AreEqual(4, yZValues.Count);
            // left part:
            // 0 (-10, 0)
            //  \
            //   \
            //    1 
            // (-7.5, -10)
            Assert.AreEqual(-10.0, yZValues[0].X, 1.0e-6);
            Assert.AreEqual(0.0, yZValues[0].Y, 1.0e-6);
            Assert.AreEqual(-7.5, yZValues[1].X, 1.0e-6);
            Assert.AreEqual(-10.0, yZValues[1].Y, 1.0e-6);

            // left part:
            //               3 (10, 0)           
            //              /
            //             /
            //   ---------2 
            //       (7.5, -10)
            Assert.AreEqual(7.5, yZValues[2].X, 1.0e-6);
            Assert.AreEqual(-10.0, yZValues[2].Y, 1.0e-6);
            Assert.AreEqual(10.0, yZValues[3].X, 1.0e-6);
            Assert.AreEqual(0.0, yZValues[3].Y, 1.0e-6);
        }

        /// <summary>
        /// If a HeightFlowStorage cross section has a vertical side the yz profile will have to have a minimal shift
        /// to avoid problems in the calculation engine.
        /// </summary>
        [Test]
        public void YZProfileFromHeightFlowStorageWidthWithVerticalSide()
        {
            // [---------------------------] width 15
            //
            //
            //
            // [---------------------------] width 15
            IList<HeightFlowStorageWidth> tabulatedData = new List<HeightFlowStorageWidth>
                                                                 {
                                                                     new HeightFlowStorageWidth(-10.0, 25, 15),
                                                                     new HeightFlowStorageWidth(0.0, 25, 15)
                                                                 };
            IList<ICoordinate> yZValues = new List<ICoordinate>();
            CrossSectionHelper.CalculateYZProfileFromTabulatedCrossSection(yZValues, tabulatedData,"",true);
            Assert.AreEqual(4, yZValues.Count);
            // left part:
            // 0 (-7.5001, 0)
            //  \
            //   \
            //    1 
            // (-7.5, -10)
            Assert.AreEqual(-7.501, yZValues[0].X, 1.0e-6);
            Assert.AreEqual(0.0, yZValues[0].Y, 1.0e-6);
            Assert.AreEqual(-7.5, yZValues[1].X, 1.0e-6);
            Assert.AreEqual(-10.0, yZValues[1].Y, 1.0e-6);

            // left part:
            //               3 (7.5001, 0)           
            //              /
            //             /
            //   ---------2 
            //       (7.5, -10)
            Assert.AreEqual(7.5, yZValues[2].X, 1.0e-6);
            Assert.AreEqual(-10.0, yZValues[2].Y, 1.0e-6);
            Assert.AreEqual(7.501, yZValues[3].X, 1.0e-6);
            Assert.AreEqual(0.0, yZValues[3].Y, 1.0e-6);
        }

        /// <summary>
        /// If a HeightFlowStorage cross section has a vertical side the yz profile will have to have a minimal shift
        /// to avoid problems in the calculation engine.
        /// </summary>
        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void YZProfileFromHeightFlowStorageExceptionNonAscendingYZProfile()
        {
            //       [---------------] width 15
            //
            //
            //
            // [---------------------------] width 20
            IList<HeightFlowStorageWidth> tabulatedData = new List<HeightFlowStorageWidth>
                                                                 {
                                                                     new HeightFlowStorageWidth(0.0, 25, 15),
                                                                     new HeightFlowStorageWidth(-10.0, 30, 20)
                                                                 };
            IList<ICoordinate> yZValues = new List<ICoordinate>();
            CrossSectionHelper.CalculateYZProfileFromTabulatedCrossSection(yZValues, tabulatedData,"kees",true);
        }


        /// <summary>
        /// same test as YZProfileFromHeightFlowStorageWidth but now for class CrossSection
        /// </summary>
        [Test]
        public void YZProfileFromTabulatedCrossSections()
        {
            var branchGeometry = new LineString(new[] { new Coordinate(111, 0), new Coordinate(11, 0) });
            var channel = new Channel { Geometry = branchGeometry };
            var crossSection = new CrossSection { CrossSectionType = CrossSectionType.HeightFlowStorageWidth, Offset = 10 };
            NetworkHelper.AddBranchFeatureToBranch(channel, crossSection, crossSection.Offset);
            crossSection.HeightFlowStorageWidthData.Add(new HeightFlowStorageWidth(-10.0, 15, 25));
            crossSection.HeightFlowStorageWidthData.Add(new HeightFlowStorageWidth(0.0, 20, 30));

            CrossSectionHelper.ConvertCrossSectionType(crossSection, CrossSectionType.YZTable);
            Assert.AreEqual(CrossSectionType.YZTable, crossSection.CrossSectionType);
            Assert.AreEqual(4, crossSection.YZValues.Count);
        }

        /// <summary>
        /// Test is a cross section of type CrossSectionType.GeometryBased converted to CrossSectionType.YZTable
        /// has no loss of data.
        /// </summary>
        [Test]
        public void GeometryToYzToGeometryOrThereAndBackAgain()
        {
            var branchGeometry = new LineString(new[] { new Coordinate(111, 0), new Coordinate(11, 0) });
            var channel = new Channel { Geometry = branchGeometry };
            IList<ICoordinate> yzCoordinates = new List<ICoordinate>
                                                         {
                                                             // note: x, y of coordinate are interpreted as the yz for 
                                                             // the cross section.
                                                             new Coordinate(0.0, 0.0),
                                                             new Coordinate(5.0, -20.0),
                                                             new Coordinate(15.0, -20.0),
                                                             new Coordinate(20.0, 0.0)
                                                         };
            IGeometry geometry = CrossSectionHelper.CreateCrossSectionGeometryForXyzCrossSectionFromYZ(branchGeometry,
                                                                                                       30, yzCoordinates);
            CrossSection crossSection = new CrossSection { Geometry = geometry, Offset = 10 };
            NetworkHelper.AddBranchFeatureToBranch(channel, crossSection, crossSection.Offset);
            Assert.AreEqual(CrossSectionType.GeometryBased, crossSection.CrossSectionType);
            const int coordinateCount = 4;
            Assert.AreEqual(coordinateCount, crossSection.Geometry.Coordinates.Length);
            Assert.AreEqual(coordinateCount, crossSection.YZValues.Count);

            CrossSectionHelper.ConvertCrossSectionType(crossSection, CrossSectionType.YZTable);
            Assert.AreEqual(coordinateCount, crossSection.YZValues.Count);
            Assert.AreEqual(2, crossSection.Geometry.Coordinates.Length);
            Assert.AreEqual(4, crossSection.YZValues.Count);
            CrossSectionHelper.ConvertCrossSectionType(crossSection, CrossSectionType.GeometryBased);
            Assert.AreEqual(CrossSectionType.GeometryBased, crossSection.CrossSectionType);
            Assert.AreEqual(coordinateCount, crossSection.Geometry.Coordinates.Length);
        }
    }
}
