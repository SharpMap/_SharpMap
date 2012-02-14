using System;
using System.Collections.Generic;
using System.Linq;
using DelftTools.Hydro;
using DelftTools.Hydro.CrossSections;
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
        public void AddDefaultZToGeometry()
        {
            var crossSection = new CrossSectionDefinitionXYZ();
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
            var hydroNetwork = new HydroNetwork();

            var channel = new Channel {Geometry = new LineString(new[] {new Coordinate(0, 0), new Coordinate(20, 0)})};

            var crossSectionDefinition = new CrossSectionDefinitionYZ();
            crossSectionDefinition.YZDataTable.AddCrossSectionYZRow(0, 5, 0);
            crossSectionDefinition.YZDataTable.AddCrossSectionYZRow(2, 0, 1);
            crossSectionDefinition.YZDataTable.AddCrossSectionYZRow(4, 5, 0);
            crossSectionDefinition.Thalweg = 2;

            hydroNetwork.Branches.Add(channel);
            int offset = 12;
            var crossSection = HydroNetworkHelper.AddCrossSectionDefinitionToBranch(channel, crossSectionDefinition, offset);

            Assert.AreEqual(4, crossSection.Geometry.Length);
            Assert.AreEqual(2, crossSection.Geometry.Coordinates.Length);

            Assert.IsTrue(crossSection.Geometry.Coordinates.All(c => c.X == offset));
            Assert.AreEqual(new[] {2d,-2d}, crossSection.Geometry.Coordinates.Select(c => c.Y).ToList());
        }

        /// <summary>
        /// Tests the creation of a simple linestring (only coordinate at start and end) geometry for a cross section
        /// </summary>
        [Test]
        public void TabulatedToSimpleGeometryTest()
        {
            var branchGeometry = new LineString(new[] {new Coordinate(0, 0), new Coordinate(100, 0)});
            
            IGeometry geometry = CrossSectionHelper.CreatePerpendicularGeometry(branchGeometry, 30, 30);
            // The expected length of the geometry is the maximum of the totalwidth
            Assert.AreEqual(30.0, geometry.Length, 1.0e-6);
            Assert.IsInstanceOfType(typeof(LineString), geometry);
            Assert.AreEqual(2, geometry.Coordinates.Length);
        }

        /// <summary>
        /// Tests the creation of a simple linestring (only coordinate at start and end) geometry for a cross section
        /// </summary>
        [Test]
        public void YZTableToPerpendicularGeometry()
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
            double thalWegOffset = (yzCoordinates[yzCoordinates.Count - 1].X - yzCoordinates[0].X) / 2;
            var minY = yzCoordinates.Min(c => c.X);
            var maxY = yzCoordinates.Max(c => c.X);
            IGeometry geometry = CrossSectionHelper.CreatePerpendicularGeometry(branchGeometry, 30, minY, maxY, thalWegOffset);
            var expectedGeometry = new LineString(new[] { new Coordinate(30, -10, 0), new Coordinate(30, -5, -20), new Coordinate(30, 5, -20), new Coordinate(30, 10, 0) });
            Assert.AreEqual(expectedGeometry,geometry);
        }

        [Test]
        public void AreaOfEmptyProfiles()
        {
            var upper = new ICoordinate[]{};
            var lower = new ICoordinate[] { };

            Assert.AreEqual(0.0, CrossSectionHelper.CalculateStorageArea(lower, upper));
        }

        [Test]
        public void AreaOfStraightProfiles()
        {
            var upper = new ICoordinate[] { new Coordinate(0, 0), new Coordinate(1, 0), new Coordinate(2, 0), new Coordinate(3, 0) };
            var lower = new ICoordinate[] { new Coordinate(0, 3), new Coordinate(1, 3), new Coordinate(2, 3), new Coordinate(3, 3) };

            Assert.AreEqual(9.0, CrossSectionHelper.CalculateStorageArea(lower, upper));
        }


        [Test]
        public void AreaOfLine()
        {
            var upper = new ICoordinate[] { new Coordinate(0, 0)};
            var lower = new ICoordinate[] { new Coordinate(0, 3)};

            Assert.AreEqual(0.0, CrossSectionHelper.CalculateStorageArea(lower, upper));
        }

        [Test]
        public void CreatePerpendicularGeometry()
        {
            var branchGeometry = new LineString(new[] { new Coordinate(0, 0), new Coordinate(100, 0) });
            var minY = -40.0d;
            var maxY = 60.0d;
            var offsetAlongBranch = 25.0d;
            var perpendicularGeometry = CrossSectionHelper.CreatePerpendicularGeometry(branchGeometry, offsetAlongBranch, minY, maxY, 0.0);

            //compare the calculated geometry with a small tolerance. Rounding errors occur due to Sin/Cos
            LineString expected = new LineString(new[] { new Coordinate(25,40), new Coordinate(25, -60) });
            Assert.IsTrue(expected.EqualsExact(perpendicularGeometry,0.00001));
        }
    }
}
