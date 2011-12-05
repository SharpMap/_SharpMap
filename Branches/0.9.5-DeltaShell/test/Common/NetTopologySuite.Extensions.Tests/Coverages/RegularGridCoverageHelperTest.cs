using System;
using System.Collections.Generic;
using System.Linq;
using DelftTools.Functions;
using DelftTools.Functions.Generic;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;
using NetTopologySuite.Extensions.Coverages;
using NUnit.Framework;

namespace NetTopologySuite.Extensions.Tests.Coverages
{
    [TestFixture]
    public class RegularGridCoverageHelperTest
    {
        [Test]
        public void GetIntersectionCoordinatesTest()
        {
            var vertices = new List<ICoordinate>
                               {
                                   new Coordinate(0, 0),
                                   new Coordinate(1000, 0)
                               };
            ILineString gridProfile = new LineString(vertices.ToArray());

            var stepSize = gridProfile.Length / 100;
            IEnumerable<ICoordinate> intersection = RegularGridCoverageHelper.GetGridProfileCoordinates(gridProfile, stepSize);
            var stepCount = (gridProfile.Length/stepSize) + 1;
            Assert.AreEqual(stepCount, intersection.Count());
            IList<ICoordinate> coordinates = intersection.ToArray();
            Assert.AreEqual(coordinates[0].X, 0, 1e-6);
            Assert.AreEqual(coordinates[0].Y, 0, 1e-6);
            Assert.AreEqual(coordinates[coordinates.Count - 1].X, 1000, 1e-6);
            Assert.AreEqual(coordinates[coordinates.Count - 1].Y, 0, 1e-6);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void GetIntersectionCoordinatesEmptyGridProfileTest()
        {
            var vertices = new List<ICoordinate>
                               {
                                   new Coordinate(0, 0),
                                   new Coordinate(0, 0)
                               };
            ILineString gridProfile = new LineString(vertices.ToArray());
            var stepSize = gridProfile.Length / 100;
            IEnumerable<ICoordinate> intersection = RegularGridCoverageHelper.GetGridProfileCoordinates(gridProfile, stepSize);
            var stepCount = (gridProfile.Length / stepSize) + 1;
            Assert.AreEqual(stepCount, intersection.Count());
        }

        [Test]
        public void FunctionTest()
        {
            Function f = new Function();
            f.Components.Add(new Variable<double>());
            f.Arguments.Add(new Variable<double>());
            f[0.0] = new[] { 1.0 };
            Assert.AreEqual(1.0, f[0.0]);
        }

        [Test]
        public void GetGridValuesTest()
        {
            var vertices = new List<ICoordinate>
                               {
                                   new Coordinate(0, 0),
                                   new Coordinate(100, 100)
                               };
            ILineString gridProfile = new LineString(vertices.ToArray());

            IRegularGridCoverage regularGridCoverage = new RegularGridCoverage(2,3,100,50)
                                                           {
                                                               Name = "pressure",
                                                           };


            regularGridCoverage.Components.Clear();
            regularGridCoverage.Components.Add(new Variable<float>("pressure"));

            regularGridCoverage.SetValues(new[] { 1.1, 2.0, 3.0, 4.0, 5.0, 6.0 });


            Function gridValues = RegularGridCoverageHelper.GetGridValues(regularGridCoverage, gridProfile);
            Assert.AreEqual(101, gridValues.Components[0].Values.Count);
            Assert.AreEqual(1.1f, (float)gridValues[0.0], 1e-3f);
            // We can not use the linestring's length directly due to rounding errors
            Assert.AreEqual((double)gridValues.Arguments[0].Values[gridValues.Components[0].Values.Count - 1], gridProfile.Length, 
                            1e-6);
            double length = (double)gridValues.Arguments[0].Values[gridValues.Components[0].Values.Count - 1];
            Assert.AreEqual(6.0, gridValues[length]);
        }
    }
}