using DelftTools.Functions.Generic;
using GeoAPI.Extensions.Networks;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;
using NetTopologySuite.Extensions.Coverages;
using NetTopologySuite.Extensions.Networks;
using NUnit.Framework;
using SharpMap.Layers;
using SharpMap.Styles;

namespace SharpMap.Tests.Layers
{
    [TestFixture]
    public class FeatureCoverageLayerTest
    {
        [Test]
        public void GeometryTypeGeneratedThemeIsCorrect()
        {
            var featureCoverage = new FeatureCoverage();

            featureCoverage.Arguments.Add(new Variable<Branch>());
            featureCoverage.Components.Add(new Variable<double>());

            var network = new Network() { Name = "Test network" };

            var branch1 = new Branch()
                              {
                                  Name = "Link1",
                                  Geometry =
                                      new LineString(new[] {new Point(0, 0).Coordinate, new Point(10, 0).Coordinate})
                              };

            var branch2 = new Branch()
                              {
                                  Name = "Link2",
                                  Geometry =
                                      new LineString(new[] {new Point(10, 0).Coordinate, new Point(20, 0).Coordinate})
                              };


            network.Branches.AddRange(new[] {branch1, branch2});

            featureCoverage[branch1] = 1;
            featureCoverage[branch2] = 2;
            
            var fcLayer = new FeatureCoverageLayer { Coverage = featureCoverage };

            Assert.AreEqual(typeof(ILineString) ,fcLayer.Style.GeometryType);
            Assert.AreEqual(typeof(ILineString), ((VectorStyle)fcLayer.Theme.ThemeItems[0].Style).GeometryType);
        }
    }
}