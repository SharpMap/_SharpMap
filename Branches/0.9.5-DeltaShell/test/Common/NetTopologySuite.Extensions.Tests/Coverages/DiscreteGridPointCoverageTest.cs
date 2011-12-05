using System;
using System.Linq;
using DelftTools.Functions.Filters;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;
using NetTopologySuite.Extensions.Coverages;
using NUnit.Framework;

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
    }
}