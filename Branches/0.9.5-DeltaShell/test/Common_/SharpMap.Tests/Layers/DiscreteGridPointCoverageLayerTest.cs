using System.Linq;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;
using NetTopologySuite.Extensions.Coverages;
using NUnit.Framework;
using SharpMap.Layers;
using SharpMapTestUtils;

namespace SharpMap.Tests.Layers
{
    [TestFixture]
    public class DiscreteGridPointCoverageLayerTest
    {
        [Test]
        [Category("Windows.Forms")]
        public void ShowUsingMapView()
        {
            var points = new[,]
                             {
                                 {new Point(0, 0), new Point(1, 0)},
                                 {new Point(2, 1), new Point(3, 1.5)},
                                 {new Point(1, 2), new Point(3, 3)}
                             };

            var coverage = new DiscreteGridPointCoverage(2, 3, points.Cast<IPoint>());

            var values = new[,]
                             {
                                 {1.0, 2.0},
                                 {3.0, 4.0},
                                 {5.0, 6.0}
                             };

            coverage.SetValues(values);

            var coverageLayer = new DiscreteGridPointCoverageLayer {Coverage = coverage};

            var map = new Map {Layers = {coverageLayer}};

            MapTestHelper.Show(map);
        }

        [Test]
        [Category("Windows.Forms")]
        public void ShowUsingMapViewWhereValuesContainNoDataValue()
        {
            var points = new[,]
                             {
                                 {new Point(0, 0), new Point(1, 0)},
                                 {new Point(2, 1), new Point(3, 1.5)},
                                 {new Point(1, 2), new Point(3, 3)}
                             };

            var coverage = new DiscreteGridPointCoverage(2, 3, points.Cast<IPoint>());

            var values = new[,]
                             {
                                 {1.0, 2.0},
                                 {3.0, 4.0},
                                 {5.0, -999.0}
                             };

            coverage.Components[0].NoDataValues = new [] {-999.0};
            
            coverage.SetValues(values);

            var coverageLayer = new DiscreteGridPointCoverageLayer { Coverage = coverage };

            var map = new Map { Layers = { coverageLayer } };

            MapTestHelper.Show(map);
        }
    }
}