using System;
using System.Linq;
using DelftTools.TestUtils;
using GeoAPI.Extensions.Networks;
using GisSharpBlog.NetTopologySuite.Geometries;
using log4net.Config;
using NetTopologySuite.Extensions.Coverages;
using NetTopologySuite.Extensions.Networks;
using NetTopologySuite.Extensions.Tests.Features;
using NUnit.Framework;
using SharpMap.Converters.WellKnownText;

namespace NetTopologySuite.Extensions.Tests.Networks
{
    [TestFixture]
    public class NetworkHelperTest
    {
        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            LogHelper.ConfigureLogging();
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            LogHelper.ResetLogging();
        }

        /// <summary>
        /// Creates a simple test network of 1 branch amd 2 nodes. The branch has '3' parts, in the center of
        /// the first aand last is a cross section.
        ///                 n
        ///                /
        ///               /
        ///              cs
        ///             /
        ///     -------/
        ///    /
        ///   cs
        ///  /
        /// n
        /// </summary>
        /// <returns></returns>
        private static INetwork CreateTestNetwork()
        {
            var network = new Network();
            var branch1 = new Branch
            {
                Geometry = new LineString(new[]
                                                         {
                                                             new Coordinate(0, 0), new Coordinate(30, 40),
                                                             new Coordinate(70, 40), new Coordinate(100, 100)
                                                         })
            };

            var node1 = new Node { Network = network, Geometry = new Point(new Coordinate(0, 0)) ,Name = "StartNode"};
            var node2 = new Node { Network = network, Geometry = new Point(new Coordinate(100, 100)) ,Name = "EndNode"};

            branch1.Source = node1;
            branch1.Target = node2;

            network.Branches.Add(branch1);
            network.Nodes.Add(node1);
            network.Nodes.Add(node2);

            return network;
        }
        [Test]
        public void GetShortetsRouteOnASingleBranchReversed()
        {
            var network = CreateTestNetwork();
            var branch1 = network.Branches[0];
            var segments = NetworkHelper.GetShortestPathBetweenBranchFeaturesAsNetworkSegments(network,new NetworkLocation(branch1,20),
                new NetworkLocation(branch1,10));
            Assert.AreEqual(1,segments.Count);
            Assert.AreEqual(20, segments[0].Offset);
            Assert.AreEqual(10, segments[0].EndOffset);
        }

        [Test]
        public void GetShortestRouteOnASingleBranch()
        {
            var network = CreateTestNetwork();
            var branch1 = network.Branches[0];
            var segments = NetworkHelper.GetShortestPathBetweenBranchFeaturesAsNetworkSegments(network, new NetworkLocation(branch1, 10),
                new NetworkLocation(branch1, 20));
            Assert.AreEqual(1, segments.Count);
            Assert.AreEqual(10,segments[0].Offset);
            Assert.AreEqual(20, segments[0].EndOffset);
        }
        [Test]
        public void CreateLightCopyNetworkOldItemsAsAttributes()
        {
            var network = CreateTestNetwork();
            var lightNetwork = NetworkHelper.CreateLightNetworkCopyWithOldItemsAsAttributes(network, null);
            Assert.AreEqual(network.Branches.Count, lightNetwork.Branches.Count);
            Assert.AreEqual(network.Nodes.Count, lightNetwork.Nodes.Count);
            Assert.AreEqual(lightNetwork.Nodes[0], lightNetwork.Branches[0].Source);
            Assert.AreEqual(lightNetwork.Nodes[1], lightNetwork.Branches[0].Target);
        }

        [Test]
        public void GetShortestPathBetweeTwoBranchFeatures()
        {
            var network = new Network();

            var node1 = new Node {Network = network, Geometry = new Point(new Coordinate(0, 0)), Name = "node1"};
            var node2 = new Node {Network = network, Geometry = new Point(new Coordinate(0, 100)), Name = "node2"};
            var node3 = new Node {Network = network, Geometry = new Point(new Coordinate(100, 0)), Name = "node3"};

            network.Nodes.Add(node1);
            network.Nodes.Add(node2);
            network.Nodes.Add(node3);

            var branch1 = new Branch
                              {
                                  Geometry = GeometryFromWKT.Parse("LINESTRING (0 0, 0 100)"),
                                  Source = node1,
                                  Target = node2,
                                  Name = "branch1"
                              };
            var branch2 = new Branch
                              {
                                  Geometry = GeometryFromWKT.Parse("LINESTRING (0 100, 100 0)"),
                                  Source = node2,
                                  Target = node3,
                                  Name = "branch2"
                              };
            var branch3 = new Branch
                              {
                                  Geometry = GeometryFromWKT.Parse("LINESTRING (100 0, 0 0)"),
                                  Source = node3,
                                  Target = node1,
                                  Name = "branch3"
                              };
            network.Branches.Add(branch1);
            network.Branches.Add(branch2);
            network.Branches.Add(branch3);

            var networkLocation1 = new NetworkLocation
                                       {
                                           Geometry = new Point(new Coordinate(90, 0)),
                                           Branch = branch1,
                                           Offset = 90,
                                           Name = "source"
                                       };
            var networkLocation2 = new NetworkLocation
                                       {
                                           Geometry = new Point(new Coordinate(0, 90)),
                                           Branch = branch2,
                                           Offset = 90,
                                           Name = "target"
                                       };

            var segments = NetworkHelper.GetShortestPathBetweenBranchFeaturesAsNetworkSegments(network, networkLocation1,
                                                                                               networkLocation2);

            Assert.AreEqual(2, segments.Count);
            Assert.IsTrue(segments[0].DirectionIsPositive);
            Assert.AreEqual(90, segments[0].Offset);
            Assert.AreEqual(100, segments[0].EndOffset);
        }


        [Test]
        public void GetShortestPathBetweenTwoBranchFeaturesReversed()
        {
            var network = new Network();

            var node1 = new Node { Network = network, Geometry = new Point(new Coordinate(0, 0)), Name = "node1" };
            var node2 = new Node { Network = network, Geometry = new Point(new Coordinate(0, 100)), Name = "node2" };
            var node3 = new Node { Network = network, Geometry = new Point(new Coordinate(100, 0)), Name = "node3" };

            network.Nodes.Add(node1);
            network.Nodes.Add(node2);
            network.Nodes.Add(node3);

            var branch1 = new Branch
            {
                Geometry = GeometryFromWKT.Parse("LINESTRING (0 0, 0 100)"),
                Source = node1,
                Target = node2,
                Name = "branch1"
            };
            var branch2 = new Branch
            {
                Geometry = GeometryFromWKT.Parse("LINESTRING (0 100, 100 0)"),
                Source = node2,
                Target = node3,
                Name = "branch2"
            };
            var branch3 = new Branch
            {
                Geometry = GeometryFromWKT.Parse("LINESTRING (100 0, 0 0)"),
                Source = node3,
                Target = node1,
                Name = "branch3"
            };
            network.Branches.Add(branch1);
            network.Branches.Add(branch2);
            network.Branches.Add(branch3);

            var networkLocation1 = new NetworkLocation
            {
                Geometry = new Point(new Coordinate(90, 0)),
                Branch = branch1,
                Offset = 90,
                Name = "source"
            };
            var networkLocation2 = new NetworkLocation
            {
                Geometry = new Point(new Coordinate(0, 90)),
                Branch = branch2,
                Offset = 90,
                Name = "target"
            };

            var segments = NetworkHelper.GetShortestPathBetweenBranchFeaturesAsNetworkSegments(network, networkLocation2,
                                                                                               networkLocation1);

            Assert.AreEqual(2, segments.Count);
            Assert.IsFalse(segments[0].DirectionIsPositive);
            Assert.AreEqual(90, segments[0].Offset);
            Assert.AreEqual(0, segments[0].EndOffset);
        }
        [Test]
        public void GetShortestPathSingleBranchReversed()
        {
            var network = new Network();

            var node1 = new Node { Network = network, Geometry = new Point(new Coordinate(0, 0)), Name = "node1" };
            var node2 = new Node { Network = network, Geometry = new Point(new Coordinate(0, 100)), Name = "node2" };
            var node3 = new Node { Network = network, Geometry = new Point(new Coordinate(100, 0)), Name = "node3" };

            network.Nodes.Add(node1);
            network.Nodes.Add(node2);
            network.Nodes.Add(node3);

            var branch1 = new Branch
            {
                Geometry = GeometryFromWKT.Parse("LINESTRING (0 0, 0 100)"),
                Source = node1,
                Target = node2,
                Name = "branch1"
            };
            var branch2 = new Branch
            {
                Geometry = GeometryFromWKT.Parse("LINESTRING (0 100, 100 0)"),
                Source = node2,
                Target = node3,
                Name = "branch2"
            };
            var branch3 = new Branch
            {
                Geometry = GeometryFromWKT.Parse("LINESTRING (100 0, 0 0)"),
                Source = node3,
                Target = node1,
                Name = "branch3"
            };
            network.Branches.Add(branch1);
            network.Branches.Add(branch2);
            network.Branches.Add(branch3);

            var networkLocation1 = new NetworkLocation
            {
                Geometry = new Point(new Coordinate(90, 0)),
                Branch = branch1,
                Offset = 90,
                Name = "source"
            };
            var networkLocation2 = new NetworkLocation
            {
                Geometry = new Point(new Coordinate(0, 40)),
                Branch = branch1,
                Offset = 40,
                Name = "target"
            };

            var segments = NetworkHelper.GetShortestPathBetweenBranchFeaturesAsNetworkSegments(network, networkLocation1,
                                                                                               networkLocation2);

            Assert.AreEqual(1, segments.Count);
            Assert.IsFalse(segments[0].DirectionIsPositive);
            Assert.AreEqual(90, segments[0].Offset);
            Assert.AreEqual(40, segments[0].EndOffset);
        }
        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void UpdateLengthAfterSplitNonGeometryBasedBranchWithLength0ThrowsException()
        {
            var network = new Network();
            network.Branches.Add(new Branch
            {
                Source = new Node("n1"),
                Target = new Node("n2"),
                Geometry = GeometryFromWKT.Parse("LINESTRING(0 0, 100 0)")
            });

            var branch = network.Branches.First();
            Assert.AreEqual(100.0, branch.Length);

            branch.IsLengthCustom = true;
            branch.Length = 0;
            
            NetworkHelper.SplitBranchAtNode(network.Branches.First(), 50);
        }

        [Test]
        public void UpdateLengthAfterSplitNonGeometryBasedBranch()
        {
            var network = new Network();
            network.Branches.Add(new Branch
            {
                Source = new Node("n1"),
                Target = new Node("n2"),
                IsLengthCustom = true,
                Length = 1000,
                Geometry = GeometryFromWKT.Parse("LINESTRING(0 0, 100 0)")
            });

            var node = NetworkHelper.SplitBranchAtNode(network.Branches.First(), 200);

            Assert.AreEqual(200, node.IncomingBranches[0].Length);
            Assert.AreEqual(800, node.OutgoingBranches[0].Length);
        }

        [Test]
        public void UpdateLengthAfterSplitGeometryBasedBranch()
        {
            var network = new Network();
            network.Branches.Add(new Branch
            {
                Source = new Node("n1"),
                Target = new Node("n2"),
                IsLengthCustom = false,
                Length = 1000,
                Geometry = GeometryFromWKT.Parse("LINESTRING(0 0, 100 0)")
            });

            var node = NetworkHelper.SplitBranchAtNode(network.Branches.First(), 20);

            Assert.AreEqual(20, node.IncomingBranches[0].Length);
            Assert.AreEqual(80, node.OutgoingBranches[0].Length);
        }

        [Test]
        public void GetNeighboursOnBranchNone()
        {
            var network = new Network();
            network.Branches.Add(new Branch
            {
                Source = new Node("n1"),
                Target = new Node("n2"),
                Geometry = GeometryFromWKT.Parse("LINESTRING(0 0, 100 0)")
            });

            IBranchFeature before;
            IBranchFeature after;
            NetworkHelper.GetNeighboursOnBranch(network.Branches[0], 100, out before, out after);

            Assert.IsNull(before);
            Assert.IsNull(after);
        }

        [Test]
        public void GetNeighboursOnBranch()
        {
            var network = new Network();
            Branch branch = new Branch
                                 {
                                     Source = new Node("n1"),
                                     Target = new Node("n2"),
                                     Geometry = GeometryFromWKT.Parse("LINESTRING(0 0, 100 0)")
                                 };

            network.Branches.Add(branch);

            for (int i=1; i<10; i++)
            {
                branch.BranchFeatures.Add(new SimpleBranchFeature {Branch = branch, Offset = 10*i});
            }

            IBranchFeature before = null;
            IBranchFeature after = null;

            NetworkHelper.GetNeighboursOnBranch(branch, 5, out before, out after);

            Assert.IsNull(before);
            Assert.AreEqual(branch.BranchFeatures[0], after);

            NetworkHelper.GetNeighboursOnBranch(branch, 45, out before, out after);

            Assert.AreEqual(branch.BranchFeatures[3], before);
            Assert.AreEqual(branch.BranchFeatures[4], after);

            NetworkHelper.GetNeighboursOnBranch(branch, 50, out before, out after);

            Assert.AreEqual(branch.BranchFeatures[3], before);
            Assert.AreEqual(branch.BranchFeatures[5], after);

            NetworkHelper.GetNeighboursOnBranch(branch, 95, out before, out after);

            Assert.AreEqual(branch.BranchFeatures[8], before);
            Assert.IsNull(after);
        }

        [Test]
        [Ignore("Now not working. Review after 24-6")]
        public void SplitChannelAtNode()
        {
            var network = new Network();
            var branch = new Branch
            {
                Source = new Node("n1"),
                Target = new Node("n2"),
                Geometry = GeometryFromWKT.Parse("LINESTRING(219478.546875 495899.46875,219434.578125 495917.3125,219202.234375 495979,219074.015625 496013.59375,219046.5 496021.15625,218850.078125 496073.59375,218732.421875 496105.65625,218572.546875 496148.9375,218473.515625 496173.90625,218463.546875 496176.65625,218454.734375 496178.9375,218264.84375 496229.65625,218163.8125 496256.53125,218073.453125 496280.59375,217862.65625 496336.71875,217672.5 496387.3125,217487.9375 496436.4375,217293.546875 496488.1875,217103.90625 496538.65625,216945.078125 496580.9375,216748.34375 496633.3125,216581.8125 496677.65625,216380.8125 496731.15625,216205.03125 496777.9375,215978.53125 496838.21875,215819.328125 496880.59375,215646.3125 496926.65625,215563.609375 496948.6875,215502.453125 496966.28125,215501.234375 496966.625,215448 496981.40625,215216.5 497069.90625,215083.546875 497124.40625,214913.109375 497191.34375,214733.015625 497260.15625,214557.265625 497329.3125,214369.578125 497403.1875,214147.828125 497490.4375,213947.328125 497569.34375,213883.34375 497594.0625,213862.03125 497601.0625,213848.140625 497603.0625,213627.5625 497609.375,213513.796875 497612.53125,213494.71875 497614.9375,213471.40625 497621.9375,213325.015625 497681.5,213317.515625 497684.40625,213049.609375 497788.21875,212896.21875 497844.78125,212675.125 497868.21875,212426.046875 497843.375,212282.171875 497830.03125,212042.1875 497810.25,211798.59375 497791.46875,211515.140625 497766.625,211509.109375 497766.09375,211234.28125 497741.8125,211070.875 497729.75,210538.3125 497760.78125,210445.53125 497768.15625,210408.921875 497763.28125,210372.796875 497752.5625,210325.953125 497745.0625,210279.109375 497742.5625,210237.625 497753.625,210209.734375 497769.71875,210001.203125 497891.03125,209796.765625 498006.6875,209763.90625 498020.5625,209722.265625 498029.3125,209691.59375 498026.40625,209600.1875 498008.53125,209498.828125 497991.75,209441.78125 497987.15625,209390.375 497987.96875,209337.359375 497996.8125,208898.703125 498072.90625,208331.625 498166.03125,208286.40625 498172.8125)")
            };
            /*branch.IsLengthCustom = true;
            double originalLength = 11597.6724261341;
            branch.Length = originalLength;*/

            var originalLength = branch.Length;
            network.Branches.Add(branch);
            //twice as long..

            NetworkHelper.SplitBranchAtNode(branch, 763);
            //the first branch is EXACTLY clipped..
            Assert.AreEqual(763,network.Branches[0].Length);
            //and the other branch
            Assert.AreEqual(originalLength-763, network.Branches[1].Length);
        }
    }
}