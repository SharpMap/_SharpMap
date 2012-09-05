using GisSharpBlog.NetTopologySuite.Geometries;
using NetTopologySuite.Extensions.Features;
using NetTopologySuite.Extensions.Geometries;
using NUnit.Framework;

namespace NetTopologySuite.Extensions.Tests.Geometries
{
    [TestFixture]
    public class GeometryHelperTest
    {
        [Test]
        public void GetNearestFeatureReturnsLastNearestFeature()
        {
            var features = new[]
                               {
                                   new Feature {Geometry = new Point(0, 0)},
                                   new Feature {Geometry = new Point(2, 0)},
                                   new Feature {Geometry = new Point(2, 2)}
                               };

            var feature1 = GeometryHelper.GetNearestFeature(new Coordinate(1, 1), features, 3);

            feature1
                .Should("the last feature is chosen if more than 1 featres with the same distance are found")
                    .Be.EqualTo(features[2]);
        }

        [Test]
        public void GetNearestFeatureReturnsNullWhenNoNearestFeaturesCanBeFound()
        {
            var features = new[]
                               {
                                   new Feature {Geometry = new Point(0, 0)},
                                   new Feature {Geometry = new Point(2, 0)},
                                   new Feature {Geometry = new Point(2, 2)}
                               };

            var feature2 = GeometryHelper.GetNearestFeature(new Coordinate(1, 1), features, 0.5);

            feature2
                .Should("tolerance is too small")
                    .Be.Null();
        }
    }
}