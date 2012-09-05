using System;
using System.Linq;
using DelftTools.Functions.Filters;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;
using NetTopologySuite.Extensions.Coverages;
using NUnit.Framework;
using SharpMap;
using SharpMap.Layers;
using SharpMapTestUtils;

namespace NetTopologySuite.Extensions.Tests.Coverages
{
    [TestFixture]
    public class DiscreteGridPointCoverageTest
    {
        [Test]
        public void Create()
        {
            var points = new IPoint[,]
                             {
                                 {new Point(0.0, 1.0), new Point(0.0, 0.0)}, 
                                 {new Point(0.5, 1.5), new Point(1.0, 0.0)}, 
                                 {new Point(1.0, 2.0), new Point(2.0, 2.0)}
                             };


            var coverage = new DiscreteGridPointCoverage(3, 2, points.Cast<IPoint>());

            var values = new[,]
                             {
                                 {1.0, 2.0},
                                 {3.0, 4.0},
                                 {5.0, 6.0}
                             };

            coverage.SetValues(values);

/*
            var coverageLayer = new DiscreteGridPointCoverageLayer { Coverage = coverage, ShowFaces = true };
            var map = new Map { Layers = { coverageLayer } };
            MapTestHelper.Show(map);
*/

            var value = coverage.Evaluate(points[1, 1].Coordinate);

            const double expectedValue = 4.0;
            Assert.AreEqual(expectedValue, value);
            
            Assert.IsTrue(coverage.Components[0].Values.Cast<double>().SequenceEqual(new [] { 1.0, 2.0, 3.0, 4.0, 5.0, 6.0 }));
            Assert.AreEqual("new grid point coverage", coverage.Name);
        }

        [Test]
        public void CreateTimeDependent()
        {
            var points = new[,]
                             {
                                 {new Point(0, 0), new Point(1, 0)},
                                 {new Point(2, 1), new Point(3, 1.5)},
                                 {new Point(1, 2), new Point(3, 3)}
                             };


            var coverage = new DiscreteGridPointCoverage(3, 2, points.Cast<IPoint>()) { IsTimeDependent = true };

            var values = new[,]
                             {
                                 {1.0, 2.0},
                                 {3.0, 4.0},
                                 {5.0, 6.0}
                             };

            var t = DateTime.Now;
            coverage.SetValues(values, new VariableValueFilter<DateTime>(coverage.Time, t));

            values = new[,]
                             {
                                 {10.0, 20.0},
                                 {30.0, 40.0},
                                 {50.0, 60.0}
                             };
            coverage.SetValues(values, new VariableValueFilter<DateTime>(coverage.Time, t.AddYears(1)));

            var values1 = coverage.GetValues<double>(new VariableValueFilter<DateTime>(coverage.Time, t));
            values1.Should().Have.SameSequenceAs(new[] {1.0, 2.0, 3.0, 4.0, 5.0, 6.0});

            var values2 = coverage.GetValues<double>(new VariableValueFilter<DateTime>(coverage.Time, t.AddYears(1)));
            values2.Should().Have.SameSequenceAs(new[] { 10.0, 20.0, 30.0, 40.0, 50.0, 60.0 });
        }

        [Test]
        public void Faces()
        {
            // create coverage
            var points = new IPoint[,]
                             {
                                 {new Point(0, 0), new Point(1, 0)},
                                 {new Point(2, 1), new Point(3, 1.5)},
                                 {new Point(1, 2), new Point(3, 3)}
                             };

            var coverage = new DiscreteGridPointCoverage(3, 2, points.Cast<IPoint>());

            var values = new[,]
                             {
                                 {1.0, 2.0},
                                 {3.0, 4.0},
                                 {5.0, 6.0}
                             };

            coverage.SetValues(values);

            // check faces
            coverage.Faces.Count
                .Should().Be.EqualTo(2);

            var geometry = coverage.Faces.First().Geometry;

            geometry.Coordinates[0]
                .Should().Be.EqualTo(points[0, 0].Coordinate);

            geometry.Coordinates[3]
                .Should().Be.EqualTo(points[1, 0].Coordinate);

            geometry.Coordinates[2]
                .Should().Be.EqualTo(points[1, 1].Coordinate);

            geometry.Coordinates[1]
                .Should().Be.EqualTo(points[0, 1].Coordinate);

            geometry.Coordinates[4]
                .Should().Be.EqualTo(points[0, 0].Coordinate);
        }
    }
}