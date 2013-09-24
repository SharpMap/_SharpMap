using DelftTools.TestUtils;
using GisSharpBlog.NetTopologySuite.Geometries;
using NetTopologySuite.Extensions.Coverages;
using NetTopologySuite.Extensions.Networks;
using NUnit.Framework;

namespace NetTopologySuite.Extensions.Tests.Coverages
{
    [TestFixture]
    public class RouteTest
    {
        [Test]
        [Category(TestCategory.Jira)] // TOOLS-7315
        [Category(TestCategory.Integration)]
        public void BranchSplitWillCompensateRouteLocations()
        {
            // Create Network
            var nodeA = new Node("nodeA"){ Geometry = new Point(0,0) };
            var nodeB = new Node("nodeB"){ Geometry = new Point(100,0) };
            var branch = new Branch(nodeA, nodeB)
                             {
                                 Geometry = new LineString(new[] { new Coordinate(0, 0), new Coordinate(100, 0) }),
                                 Name = "branch"
                             };
            var network = new Network();
            network.Branches.Add(branch);

            // Create route network coverage
            var route = new Route { Network = network };
            route.Locations.AddValues(new[] { new NetworkLocation(branch, 10.0), new NetworkLocation(branch, 90.0) });

            NetworkHelper.SplitBranchAtNode(branch, 50.0);

            Assert.AreEqual(2, route.Locations.Values.Count);
            Assert.AreEqual(network.Branches[0], route.Locations.Values[0].Branch);
            Assert.AreEqual(network.Branches[1], route.Locations.Values[1].Branch);
            Assert.AreEqual(10.0, route.Locations.Values[0].Chainage, BranchFeature.Epsilon);
            Assert.AreEqual(40.0, route.Locations.Values[1].Chainage, BranchFeature.Epsilon);
        }
    }
}