using System;
using System.Collections.Generic;
using System.Linq;
using DelftTools.Functions.Generic;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;
using NUnit.Framework;
using NetTopologySuite.Extensions.Coverages;
using SharpMap.UI.Tools;

namespace SharpMap.UI.Tests.Tools
{
    [TestFixture]
    public class CoverageProfileTest
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
            IEnumerable<ICoordinate> intersection = CoverageProfile.GetGridProfileCoordinates(gridProfile, stepSize);
            var stepCount = (gridProfile.Length / stepSize) + 1;
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
            IEnumerable<ICoordinate> intersection = CoverageProfile.GetGridProfileCoordinates(gridProfile, stepSize);
            var stepCount = (gridProfile.Length / stepSize) + 1;
            Assert.AreEqual(stepCount, intersection.Count());
        }

        [Test]
        public void GetGridValuesTest()
        {
            var vertices = new List<ICoordinate>
                               {
                                   new Coordinate(0, 0),
                                   new Coordinate(100, 100)
                               };
            var gridProfile = new LineString(vertices.ToArray());

            var regularGridCoverage = new RegularGridCoverage(2, 3, 100, 50)
            {
                Name = "pressure",
            };
            regularGridCoverage.Components.Clear();
            regularGridCoverage.Components.Add(new Variable<float>("pressure"));

            regularGridCoverage.SetValues(new[] { 1.1f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f });


            var gridValues = CoverageProfile.GetCoverageValues(regularGridCoverage, gridProfile, null);
            Assert.AreEqual(101, gridValues.Components[0].Values.Count);
            Assert.AreEqual(1.1f, (float)gridValues[0.0], 1e-3f);
            // We can not use the linestring's length directly due to rounding errors
            Assert.AreEqual((double)gridValues.Arguments[0].Values[gridValues.Components[0].Values.Count - 1], gridProfile.Length,
                            1e-6);
            var length = (double)gridValues.Arguments[0].Values[gridValues.Components[0].Values.Count - 1];
            Assert.AreEqual(6.0, gridValues[length]);

            gridValues = CoverageProfile.GetCoverageValues(regularGridCoverage, gridProfile, null, 10.0);
            Assert.AreEqual(15, gridValues.Components[0].Values.Count);
            Assert.AreEqual(1.1f, (float)gridValues[0.0], 1e-3f);

            length = (double)gridValues.Arguments[0].Values[gridValues.Components[0].Values.Count - 1];
            // We can not use the linestring's length directly due to rounding errors
            Assert.AreEqual(((int)(gridProfile.Length / 10.0))*10.0, length,
                            1e-6);
            Assert.AreEqual(3.0, gridValues[length],
                "value at end is 3.0 due to rounding that caused the evaluated line to fall short to fall into cell with value of 6.0");
        }
    }
}