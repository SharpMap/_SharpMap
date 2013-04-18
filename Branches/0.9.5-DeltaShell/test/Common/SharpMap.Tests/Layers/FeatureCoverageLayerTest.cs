using DelftTools.Functions.Generic;
using DelftTools.Utils.Collections.Generic;
using GeoAPI.Extensions.Feature;
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


            var branches = new[] {branch1, branch2};
            network.Branches.AddRange(branches);

            featureCoverage.Features = new EventedList<IFeature>(branches);
            featureCoverage[branch1] = 1.0;
            featureCoverage[branch2] = 2.0;
            
            var fcLayer = new FeatureCoverageLayer { Coverage = featureCoverage };

            Assert.AreEqual(typeof(ILineString) ,fcLayer.Style.GeometryType);
            Assert.AreEqual(typeof(ILineString), ((VectorStyle)fcLayer.Theme.ThemeItems[0].Style).GeometryType);
        }

        [Test]
        public void LabelLayerIsCorrect()
        {
            var featureCoverage = new FeatureCoverage();

            featureCoverage.Arguments.Add(new Variable<Branch>());
            featureCoverage.Components.Add(new Variable<double>());

            var network = new Network() { Name = "Test network" };

            var branch1 = new Branch()
            {
                Name = "Link1",
                Geometry =
                    new LineString(new[] { new Point(0, 0).Coordinate, new Point(10, 0).Coordinate })
            };

            var branch2 = new Branch()
            {
                Name = "Link2",
                Geometry =
                    new LineString(new[] { new Point(10, 0).Coordinate, new Point(20, 0).Coordinate })
            };


            var branches = new[] { branch1, branch2 };
            network.Branches.AddRange(branches);

            featureCoverage.Features = new EventedList<IFeature>(branches);
            featureCoverage[branch1] = 1.0;
            featureCoverage[branch2] = 2.0;

            var fcLayer = new FeatureCoverageLayer { Coverage = featureCoverage };

            var fcLayerCloned = (FeatureCoverageLayer)fcLayer.Clone();

            Assert.AreSame(fcLayer.Coverage, fcLayerCloned.Coverage);
            Assert.IsNotNull(fcLayer.LabelLayer.Coverage);
            Assert.AreSame(fcLayer.LabelLayer.Coverage, fcLayerCloned.LabelLayer.Coverage);
        }
    }
}