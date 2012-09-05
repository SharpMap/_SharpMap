using System;
using System.Collections.Generic;
using System.Linq;
using DelftTools.Hydro;
using DelftTools.Hydro.CrossSections;
using DelftTools.Hydro.Helpers;
using DelftTools.Hydro.Structures;
using DelftTools.TestUtils;
using DelftTools.Utils;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;
using NetTopologySuite.Extensions.Actions;
using NetTopologySuite.Extensions.Coverages;
using NetTopologySuite.Extensions.Networks;
using NUnit.Framework;

namespace DelftTools.Tests.Hydo.Helpers
{
    [TestFixture]
    public class HydroNetworkHelperTest
    {
        
        [Test]
        public void DetectAndUpdateBranchBoundaries()
        {
            var network = new HydroNetwork();

            var branch1 = new Channel();
            var node1 = new HydroNode();
            var node2 = new HydroNode();

            branch1.Source = node1;
            branch1.Target = node2;

            network.Branches.Add(branch1);
            network.Nodes.Add(node1);
            network.Nodes.Add(node2);

            Assert.IsTrue(node1.IsBoundaryNode);
            Assert.IsTrue(node2.IsBoundaryNode);
        }

        [Test]
        public void GenerateCalculationPointsOnCrossSectionsSkipsIfAlsoStructurePresent()
        {
            var network = CreateTestNetwork();

            var cs1 = network.CrossSections.First();

            var branch = cs1.Branch as IChannel;

            var weir = new Weir();
            NetworkHelper.AddBranchFeatureToBranch(weir, branch, cs1.Offset);

            IDiscretization computationalGrid = new Discretization()
            {
                Network = network,
                SegmentGenerationMethod =
                    SegmentGenerationMethod.SegmentBetweenLocations
            };
            HydroNetworkHelper.GenerateDiscretization(computationalGrid, network, false, false, 1.0 ,false, 1.0, true, false, 0.0);

            Assert.AreEqual(
                new INetworkLocation[]
                    {
                        new NetworkLocation(branch, 0), new NetworkLocation(branch, 115),
                        new NetworkLocation(branch, branch.Length)
                    }, computationalGrid.Locations.Values);
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
        private static IHydroNetwork CreateTestNetwork()
        {
            var network = new Hydro.HydroNetwork();
            var branch1 = new Channel
                              {
                                  Geometry = new LineString(new[]
                                                                {
                                                                    new Coordinate(0, 0), new Coordinate(30, 40),
                                                                    new Coordinate(70, 40), new Coordinate(100, 100)
                                                                })
                              };

            var node1 = new HydroNode { Network = network, Geometry = new Point(new Coordinate(0, 0)) };
            var node2 = new HydroNode { Network = network, Geometry = new Point(new Coordinate(100, 100)) };

            network.Branches.Add(branch1);
            network.Nodes.Add(node1);
            network.Nodes.Add(node2);

            var crossSection1 = new CrossSectionDefinitionXYZ { Geometry = new LineString(new[] { new Coordinate(15, 20), new Coordinate(16, 20) }) };
            double offset1 = Math.Sqrt(15 * 15 + 20 * 20);
            var crossSectionBranchFeature1 = new CrossSection(crossSection1) {Offset = offset1};

            var crossSection2 = new CrossSectionDefinitionXYZ { Geometry = new LineString(new[] { new Coordinate(85, 70), new Coordinate(86, 70) }) };
            double offset2 = Math.Sqrt(30 * 30 + 40 * 40) + 40 + Math.Sqrt(15 * 15 + 20 * 20);
            var crossSectionBranchFeature2 = new CrossSection(crossSection2) { Offset = offset2 };
            
            branch1.Source = node1;
            branch1.Target = node2;
            NetworkHelper.AddBranchFeatureToBranch(crossSectionBranchFeature1, branch1, crossSectionBranchFeature1.Offset);
            NetworkHelper.AddBranchFeatureToBranch(crossSectionBranchFeature2, branch1, crossSectionBranchFeature2.Offset);

            return network;
        }


        /// <summary>
        /// Creates the testnetwork, inserts a node and test if it added correctly.
        /// </summary>
        [Test]
        [Category(TestCategory.Integration)]
        public void SplitBranchIn2()
        {
            IHydroNetwork network = CreateTestNetwork();
            var branch1 = network.Channels.First();
            branch1.Name = "branch1";
            branch1.LongName = "maas";
            double length = branch1.Geometry.Length;

            int nodesCount = network.Nodes.Count;
            IHydroNode hydroNode = HydroNetworkHelper.SplitChannelAtNode(branch1, length / 2);
            Assert.AreEqual(nodesCount + 1, network.Nodes.Count);
            Assert.AreNotEqual(-1, network.Nodes.IndexOf(hydroNode));
            
            Assert.AreEqual("branch1_A",branch1.Name);
            Assert.AreEqual("maas_A", branch1.LongName);
            
            var branch2 = network.Channels.ElementAt(1);
            Assert.AreEqual("branch1_B", branch2.Name);
            Assert.AreEqual("maas_B", branch2.LongName);

            Assert.AreEqual(0, hydroNode.Geometry.Coordinate.Z);
        }

        [Test]
        public void SplitBranchAndRemoveNode()
        {
            // related to TOOLS-3665 : Insert nodes and remove changes nodetype to incorrect type
            var network = CreateTestNetwork();
            var leftBranch = network.Channels.First();
            var startNode = leftBranch.Source;
            Assert.IsTrue(startNode.IsBoundaryNode);
            var endNode = leftBranch.Target;
            Assert.IsTrue(endNode.IsBoundaryNode);

            var insertdeNode = HydroNetworkHelper.SplitChannelAtNode(leftBranch, leftBranch.Geometry.Length / 2);
            Assert.IsTrue(startNode.IsBoundaryNode);
            Assert.IsFalse(insertdeNode.IsBoundaryNode);
            Assert.IsTrue(endNode.IsBoundaryNode);

            NetworkHelper.MergeNodeBranches(insertdeNode, network);

            Assert.IsTrue(startNode.IsBoundaryNode);
            // would fail for TOOLS-3665
            Assert.IsTrue(endNode.IsBoundaryNode);
        }

        [Test]
        [Category(TestCategory.Integration)]
        public void SplitBranchDoesNotCreateANaNInBranchGeometry()
        {
            //relates to issue 2477
            IHydroNetwork network = CreateTestNetwork();
            var branch1 = network.Channels.First();
            double length = branch1.Geometry.Length;

            int nodesCount = network.Nodes.Count;
            IHydroNode hydroNode = HydroNetworkHelper.SplitChannelAtNode(branch1, length / 2);

            Assert.AreEqual(nodesCount + 1, network.Nodes.Count);
            Assert.AreNotEqual(-1, network.Nodes.IndexOf(hydroNode));

            //the network should not contain branches with coordinates as NaN (messes up wkbwriter )
            Assert.IsFalse(network.Branches.Any(b => b.Geometry.Coordinates.Any(c => double.IsNaN(c.Z))));
        }


        /// <summary>
        /// Creates the testnetwork, adds a route and split the branch.
        /// TOOLS-1199 collection changed events caused recreating routing network triggered
        /// by changing of geometry of branch (removeunused nodes) and temporarily invalid network.
        /// </summary>
        [Test]
        [Category(TestCategory.Integration)]
        public void SplitBranchWithRouteIn2()
        {
            IHydroNetwork network = CreateTestNetwork();
            var branch1 = network.Channels.First();
            double length = branch1.Geometry.Length;

            NetworkCoverage route = new Route
                                        {
                                            Network = network,
                                            SegmentGenerationMethod = SegmentGenerationMethod.RouteBetweenLocations
                                        };
            route.Locations.Values.Add(new NetworkLocation(branch1, length / 12));
            route.Locations.Values.Add(new NetworkLocation(branch1, length / 8));

            int nodesCount = network.Nodes.Count;
            IHydroNode hydroNode = HydroNetworkHelper.SplitChannelAtNode(branch1, length / 2);
            Assert.AreEqual(nodesCount + 1, network.Nodes.Count);
            Assert.AreNotEqual(-1, network.Nodes.IndexOf(hydroNode));

            Assert.AreEqual(2, route.Locations.Values.Count);
            Assert.AreEqual(1, route.Segments.Values.Count);
        }

        /// <summary>
        /// Split the test network in the center.
        /// </summary>
        [Test]
        public void SplitCustomLengthBranchWithCrossSections()
        {
            IHydroNetwork network = CreateTestNetwork();

            var branch1 = network.Channels.First();
            branch1.IsLengthCustom = true;
            double length = branch1.Geometry.Length;

            HydroNetworkHelper.SplitChannelAtNode(branch1, new Coordinate(50, 40));

            double offset1 = Math.Sqrt(15 * 15 + 20 * 20);
            double offset2 = Math.Sqrt(30 * 30 + 40 * 40) + 40 + Math.Sqrt(15 * 15 + 20 * 20);
            double length1 = Math.Sqrt(30 * 30 + 40 * 40) + 20;
            double length2 = 20 + Math.Sqrt(30 * 30 + 60 * 60);

            Assert.AreEqual(length, length1 + length2);

            Assert.AreEqual(2, network.Branches.Count);
            var branch2 = network.Channels.Skip(1).First();
            Assert.AreEqual(3, network.Nodes.Count);
            Assert.AreEqual(1, network.Nodes[0].OutgoingBranches.Count);
            Assert.AreEqual(1, network.Nodes[1].IncomingBranches.Count);
            Assert.AreEqual(1, network.Nodes[2].IncomingBranches.Count);
            Assert.AreEqual(1, network.Nodes[2].OutgoingBranches.Count);

            Assert.AreEqual(2, network.CrossSections.Count());
            Assert.AreEqual(1, branch1.CrossSections.Count());
            Assert.AreEqual(1, branch2.CrossSections.Count());
            Assert.AreEqual(offset1, branch1.CrossSections.First().Offset);
            Assert.AreEqual(length1, branch1.Geometry.Length);
            Assert.AreEqual(offset2 - length1, branch2.CrossSections.First().Offset);
            Assert.AreEqual(length2, branch2.Geometry.Length);
            Assert.AreEqual(branch1, branch1.CrossSections.First().Branch);
            Assert.AreEqual(branch2, branch2.CrossSections.First().Branch);
        }

        /// <summary>
        /// Split the test network in the center.
        /// </summary>
        [Test]
        public void SplitBranchWithCrossSections()
        {
            IHydroNetwork network = CreateTestNetwork();

            var branch1 = network.Channels.First();
            double length = branch1.Geometry.Length;

            HydroNetworkHelper.SplitChannelAtNode(branch1, new Coordinate(50, 40));

            double offset1 = Math.Sqrt(15 * 15 + 20 * 20);
            double offset2 = Math.Sqrt(30 * 30 + 40 * 40) + 40 + Math.Sqrt(15 * 15 + 20 * 20);
            double length1 = Math.Sqrt(30 * 30 + 40 * 40) + 20;
            double length2 = 20 + Math.Sqrt(30 * 30 + 60 * 60);

            Assert.AreEqual(length, length1 + length2);


            Assert.AreEqual(2, network.Branches.Count);
            var branch2 = network.Channels.Skip(1).First();
            Assert.AreEqual(3, network.Nodes.Count);
            Assert.AreEqual(1, network.Nodes[0].OutgoingBranches.Count);
            Assert.AreEqual(1, network.Nodes[1].IncomingBranches.Count);
            Assert.AreEqual(1, network.Nodes[2].IncomingBranches.Count);
            Assert.AreEqual(1, network.Nodes[2].OutgoingBranches.Count);

            Assert.AreEqual(2, network.CrossSections.Count());
            Assert.AreEqual(1, branch1.CrossSections.Count());
            Assert.AreEqual(1, branch2.CrossSections.Count());
            Assert.AreEqual(offset1, branch1.CrossSections.First().Offset);
            Assert.AreEqual(length1, branch1.Geometry.Length);
            Assert.AreEqual(offset2 - length1, branch2.CrossSections.First().Offset);
            Assert.AreEqual(length2, branch2.Geometry.Length);
            Assert.AreEqual(branch1, branch1.CrossSections.First().Branch);
            Assert.AreEqual(branch2, branch2.CrossSections.First().Branch);
        }

        /// <summary>
        /// split at begin or end of branch should not work
        /// split on chainage = branch.length or 0 returns null
        /// </summary>
        [Test]
        public void SplitBranchOnExistingNodeShouldNotWork()
        {
            IHydroNetwork network = CreateTestNetwork();
            var numberOfChannels = network.Channels.Count();
            var branch1 = network.Channels.First();
            double length = branch1.Geometry.Length;

            var result = HydroNetworkHelper.SplitChannelAtNode(branch1, length);
            Assert.IsNull(result);
            Assert.AreEqual(numberOfChannels, network.Channels.Count());
        }

        [Test]
        public void CreateNetworkCoverageSegments()
        {
            IHydroNetwork network = CreateTestNetwork();

            INetworkCoverage networkCoverage = new NetworkCoverage
                                                   {
                                                       Network = network,
                                                       SegmentGenerationMethod =
                                                           SegmentGenerationMethod.SegmentBetweenLocations
                                                   };
            var branch1 = network.Channels.First();
            var length = branch1.Geometry.Length;
            HydroNetworkHelper.GenerateDiscretization(networkCoverage, branch1, new[] { 0.0, length / 3, 2 * length / 3, length });

            Assert.AreEqual(4, networkCoverage.Locations.Values.Count);
            Assert.AreEqual(3, networkCoverage.Segments.Values.Count);

            Assert.AreEqual(0, networkCoverage.Locations.Values[0].Offset, 1.0e-6);
            Assert.AreEqual(length / 3, networkCoverage.Locations.Values[1].Offset, 1.0e-6);
            Assert.AreEqual(2 * length / 3, networkCoverage.Locations.Values[2].Offset, 1.0e-6);
            Assert.AreEqual(length, networkCoverage.Locations.Values[3].Offset, 1.0e-6);

            Assert.AreEqual(0, networkCoverage.Segments.Values[0].Offset, 1.0e-6);
            Assert.AreEqual(length / 3, networkCoverage.Segments.Values[0].EndOffset, 1.0e-6);
            Assert.AreEqual(length / 3, networkCoverage.Segments.Values[0].Length, 1.0e-6);

            Assert.AreEqual(length / 3, networkCoverage.Segments.Values[1].Offset, 1.0e-6);
            Assert.AreEqual(2 * length / 3, networkCoverage.Segments.Values[1].EndOffset, 1.0e-6);

            Assert.AreEqual(2 * length / 3, networkCoverage.Segments.Values[2].Offset, 1.0e-6);
            Assert.AreEqual(length, networkCoverage.Segments.Values[2].EndOffset, 1.0e-6);

            Assert.AreEqual(length / 3, networkCoverage.Segments.Values[0].Length, 1.0e-6);
            Assert.AreEqual(length / 3, networkCoverage.Segments.Values[1].Length, 1.0e-6);
            Assert.AreEqual(length / 3, networkCoverage.Segments.Values[2].Length, 1.0e-6);
        }

        [Test]
        public void CreateSegementsAndIgnoreFor1Channel()
        {
            var network = new HydroNetwork();
            var channel1 = new Channel
            {
                Geometry = new LineString(new[] { new Coordinate(0, 0), new Coordinate(100, 0) })
            };
            var channel2 = new Channel
            {
                Geometry = new LineString(new[] { new Coordinate(100, 0), new Coordinate(200, 0) })
            };
            var node1 = new HydroNode { Network = network, Geometry = new Point(new Coordinate(0, 0)) };
            var node2 = new HydroNode { Network = network, Geometry = new Point(new Coordinate(100, 0)) };
            var node3 = new HydroNode { Network = network, Geometry = new Point(new Coordinate(200, 0)) };

            network.Branches.Add(channel1);
            network.Branches.Add(channel2);
            network.Nodes.Add(node1);
            network.Nodes.Add(node2);
            network.Nodes.Add(node3);

            channel1.Source = node1;
            channel1.Target = node2;
            channel2.Source = node2;
            channel2.Target = node3;


            var discretization = new Discretization
            {
                Network = network
            };

            HydroNetworkHelper.GenerateDiscretization(discretization, network, true, false, 100.0, false, 0.0, false, true, 20.0, null);
            // 6 + 6
            Assert.AreEqual(12, discretization.Locations.Values.Count);
            HydroNetworkHelper.GenerateDiscretization(discretization, network, true, false, 100.0, false, 0.0, false,
                                                      true, 10.0, new List<IChannel> { channel2 });
            // 11 + 6
            Assert.AreEqual(17, discretization.Locations.Values.Count);
            HydroNetworkHelper.GenerateDiscretization(discretization, network, true, false, 100.0, false, 0.0, false,
                                                      true, 10.0, new List<IChannel> { channel1 });
            // 11 + 11
            Assert.AreEqual(22, discretization.Locations.Values.Count);
        }

        /// <summary>
        /// Creates the testnetwork, adds 3 branch segments and splits the branch in 2.
        /// </summary>
        [Test]
        public void SplitBranchWithBranchSegments()
        {
            IHydroNetwork network = CreateTestNetwork();
            var branch1 = network.Channels.First();
            double length = branch1.Geometry.Length;
            // see also test GenerateDiscretization
            INetworkCoverage networkCoverage = new NetworkCoverage
                                                   {
                                                       Network = network,
                                                       SegmentGenerationMethod = SegmentGenerationMethod.SegmentBetweenLocationsFullyCovered
                                                   };
            HydroNetworkHelper.GenerateDiscretization(networkCoverage, branch1, new[] { 0.0, length / 3, 2 * length / 3, length });

            HydroNetworkHelper.SplitChannelAtNode(branch1, length / 2);

            var branch2 = network.Channels.Skip(1).First();

            //4 segments are craeted...2 on branch 1 and 2 on branch 2
            Assert.AreEqual(4, networkCoverage.Segments.Values.Count);
            Assert.AreEqual(2, networkCoverage.Segments.Values.Where(s => s.Branch == branch1).Count());
            Assert.AreEqual(2, networkCoverage.Segments.Values.Where(s => s.Branch == branch2).Count());
        }

        /// <summary>
        /// Creates the testnetwork, splits the branch in 2 and merges them again.
        /// </summary>
        [Test]
        public void MergeBranchWithCrossSections()
        {
            var network = CreateTestNetwork();
            var branch1 = network.Channels.First();

            var offset1 = Math.Sqrt(15 * 15 + 20 * 20);
            var offset2 = Math.Sqrt(30 * 30 + 40 * 40) + 40 + Math.Sqrt(15 * 15 + 20 * 20);
            var length1 = Math.Sqrt(30 * 30 + 40 * 40) + 20;
            var length2 = 20 + Math.Sqrt(30 * 30 + 60 * 60);

            var node = HydroNetworkHelper.SplitChannelAtNode(branch1, new Coordinate(50, 40));

            // remove the newly added node
            NetworkHelper.MergeNodeBranches(node, network);

            Assert.AreEqual(1, network.Branches.Count);
            Assert.AreEqual(2, network.Nodes.Count);
            Assert.AreEqual(2, network.CrossSections.Count());
            Assert.AreEqual(2, branch1.CrossSections.Count());
            Assert.AreEqual(offset1, branch1.CrossSections.First().Offset);
            Assert.AreEqual(offset2, branch1.CrossSections.Skip(1).First().Offset);
            Assert.AreEqual(length1 + length2, branch1.Geometry.Length);

            Assert.AreEqual(branch1, branch1.CrossSections.First().Branch);
            Assert.AreEqual(branch1, branch1.CrossSections.Skip(1).First().Branch);
        }

        [Test]
        public void ReverseBranchWithCrossSections()
        {
            var network = CreateTestNetwork();
            var branch1 = (IChannel)network.Branches[0];

            var nodeFrom = branch1.Source;
            var nodeTo = branch1.Target;

            double offsetCrossSection1 = branch1.CrossSections.First().Offset;
            double offsetCrossSection2 = branch1.CrossSections.Skip(1).First().Offset;
            double length = branch1.Geometry.Length;

            HydroNetworkHelper.ReverseBranch(branch1);

            Assert.AreEqual(nodeFrom, branch1.Target);
            Assert.AreEqual(nodeTo, branch1.Source);
            Assert.AreEqual(length - offsetCrossSection2, branch1.CrossSections.First().Offset);
            Assert.AreEqual(length - offsetCrossSection1, branch1.CrossSections.Skip(1).First().Offset);
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
        private static IHydroNetwork CreateSegmentTestNetwork()
        {
            var network = new Hydro.HydroNetwork();
            var branch1 = new Channel
                              {
                                  Geometry = new LineString(new[]
                                                                {
                                                                    new Coordinate(0, 0), new Coordinate(0, 100),
                                                                })
                              };

            var node1 = new HydroNode { Network = network, Geometry = new Point(new Coordinate(0, 0)) };
            var node2 = new HydroNode { Network = network, Geometry = new Point(new Coordinate(100, 0)) };

            network.Branches.Add(branch1);
            network.Nodes.Add(node1);
            network.Nodes.Add(node2);

            branch1.Source = node1;
            branch1.Target = node2;

            return network;
        }

        private static void AddTestStructureAt(IHydroNetwork network, IChannel branch, double offset)
        {
            IWeir weir = new Weir { Offset = offset };
            CompositeBranchStructure compositeBranchStructure = new CompositeBranchStructure
                                                                    {
                                                                        Network = network,
                                                                        Geometry = new Point(offset, 0),
                                                                        Offset = offset
                                                                    };
            compositeBranchStructure.Structures.Add(weir);
            branch.BranchFeatures.Add(compositeBranchStructure);
        }

        private static void AddTestCrossSectionAt(IHydroNetwork network, IChannel branch, double offset)
        {
            var crossSectionXyz = new CrossSectionDefinitionXYZ
                                      {
                                          Geometry =  new LineString(new[]
                                                                 {
                                                                     new Coordinate(offset - 1, 0),
                                                                     new Coordinate(offset + 1, 0)
                                                                 })
                                      };
            HydroNetworkHelper.AddCrossSectionDefinitionToBranch(branch, crossSectionXyz, offset);
        }

        [Test]
        public void CreateSegments1Structure()
        {
            IHydroNetwork network = CreateSegmentTestNetwork();
            var branch1 = network.Channels.First();

            AddTestStructureAt(network, branch1, 10);

            var networkCoverage = new Discretization
                                      {
                                          Network = network,
                                          SegmentGenerationMethod = SegmentGenerationMethod.SegmentBetweenLocations
                                      };

            HydroNetworkHelper.GenerateDiscretization(networkCoverage, // networkCoverage
                                                      branch1,  // branch
                                                      0, // minimumDistance
                                                      true,  // gridAtStructure
                                                      0.5, // structureDistance
                                                      false, // gridAtCrossSection
                                                      false, // gridAtFixedLength
                                                      -1); // fixedLength
            Assert.AreEqual(4, networkCoverage.Locations.Values.Count);
            Assert.AreEqual(0.0, networkCoverage.Locations.Values[0].Offset, 1.0e-6);
            Assert.AreEqual(9.5, networkCoverage.Locations.Values[1].Offset, 1.0e-6);
            Assert.AreEqual(10.5, networkCoverage.Locations.Values[2].Offset, 1.0e-6);
            Assert.AreEqual(100.0, networkCoverage.Locations.Values[3].Offset, 1.0e-6);
        }

        [Test]
        public void CreateSegmentsMultipleStructures()
        {
            IHydroNetwork network = CreateSegmentTestNetwork();
            var branch1 = network.Channels.First();

            AddTestStructureAt(network, branch1, 20);
            AddTestStructureAt(network, branch1, 40);
            AddTestStructureAt(network, branch1, 60);

            var networkCoverage = new Discretization
                                      {
                                          Network = network,
                                          SegmentGenerationMethod = SegmentGenerationMethod.SegmentBetweenLocations
                                      };

            HydroNetworkHelper.GenerateDiscretization(networkCoverage, // networkCoverage
                                                      branch1,  // branch
                                                      0, // minimumDistance
                                                      true,  // gridAtStructure
                                                      0.5, // structureDistance
                                                      false, // gridAtCrossSection
                                                      false, // gridAtFixedLength
                                                      -1); // fixedLength
            Assert.AreEqual(8, networkCoverage.Locations.Values.Count);
            Assert.AreEqual(0.0, networkCoverage.Locations.Values[0].Offset, 1.0e-6);
            Assert.AreEqual(19.5, networkCoverage.Locations.Values[1].Offset, 1.0e-6);
            Assert.AreEqual(20.5, networkCoverage.Locations.Values[2].Offset, 1.0e-6);
            Assert.AreEqual(39.5, networkCoverage.Locations.Values[3].Offset, 1.0e-6);
            Assert.AreEqual(40.5, networkCoverage.Locations.Values[4].Offset, 1.0e-6);
            Assert.AreEqual(59.5, networkCoverage.Locations.Values[5].Offset, 1.0e-6);
            Assert.AreEqual(60.5, networkCoverage.Locations.Values[6].Offset, 1.0e-6);
            Assert.AreEqual(100.0, networkCoverage.Locations.Values[7].Offset, 1.0e-6);
        }


        [Test]
        public void CreateSegments1StructureAtMinimumBeginBranch()
        {
            IHydroNetwork network = CreateSegmentTestNetwork();
            var branch1 = network.Channels.First();

            AddTestStructureAt(network, branch1, 0.4);

            var networkCoverage = new Discretization
                                      {
                                          Network = network,
                                          SegmentGenerationMethod = SegmentGenerationMethod.SegmentBetweenLocations
                                      };

            HydroNetworkHelper.GenerateDiscretization(networkCoverage, // networkCoverage
                                                      branch1, // branch
                                                      0.5, // minimumDistance
                                                      true,  // gridAtStructure
                                                      0.5, // structureDistance
                                                      false, // gridAtCrossSection
                                                      false, // gridAtFixedLength
                                                      -1); // fixedLength

            // structure at less than minimumdistance; expect 1 point left out
            // [----------------------
            //        0.4
            // x                   x ----------------------------- x
            // 0                  0.9                             100
            Assert.AreEqual(3, networkCoverage.Locations.Values.Count);
            Assert.AreEqual(0.0, networkCoverage.Locations.Values[0].Offset, 1.0e-6);
            Assert.AreEqual(0.9, networkCoverage.Locations.Values[1].Offset, 1.0e-6);
            Assert.AreEqual(100.0, networkCoverage.Locations.Values[2].Offset, 1.0e-6);
        }

        [Test]
        public void CreateSegments1StructureAtNearMinimumBeginBranch()
        {
            IHydroNetwork network = CreateSegmentTestNetwork();
            var branch1 = network.Channels.First();

            AddTestStructureAt(network, branch1, 0.8);

            var networkCoverage = new Discretization
                                      {
                                          Network = network,
                                          SegmentGenerationMethod = SegmentGenerationMethod.SegmentBetweenLocations
                                      };

            HydroNetworkHelper.GenerateDiscretization(networkCoverage, // networkCoverage
                                                      branch1, // branch
                                                      0.5, // minimumDistance
                                                      true,  // gridAtStructure
                                                      0.5, // structureDistance
                                                      false, // gridAtCrossSection
                                                      false, // gridAtFixedLength
                                                      -1); // fixedLength

            // structure at near minimumdistance; expect point centered at 0.8 - 0.5 = 0.3 not created
            // [----------------------
            //                0.8
            // x       x             x ----------------------------- x
            // 0    (0.3)           1.3                             100
            //        ^

            Assert.AreEqual(3, networkCoverage.Locations.Values.Count);
            Assert.AreEqual(0.0, networkCoverage.Locations.Values[0].Offset, 1.0e-6);
            Assert.AreEqual(1.3, networkCoverage.Locations.Values[1].Offset, 1.0e-6);
            Assert.AreEqual(100.0, networkCoverage.Locations.Values[2].Offset, 1.0e-6);
        }

        [Test]
        public void CreateSegments2StructureAtNearMinimumBeginBranch()
        {
            IHydroNetwork network = CreateSegmentTestNetwork();
            var branch1 = network.Channels.First();

            AddTestStructureAt(network, branch1, 0.8);
            AddTestStructureAt(network, branch1, 1.2);

            var networkCoverage = new Discretization()
                                      {
                                          Network = network,
                                          SegmentGenerationMethod = SegmentGenerationMethod.SegmentBetweenLocations
                                      };

            HydroNetworkHelper.GenerateDiscretization(networkCoverage, // networkCoverage
                                                      branch1, // branch
                                                      0.001, // minimumDistance
                                                      true,  // gridAtStructure
                                                      0.5, // structureDistance
                                                      false, // gridAtCrossSection
                                                      false, // gridAtFixedLength
                                                      -1); // fixedLength

            // structure at near minimumdistance; expect 1 point centered at first segment
            // [----------------------
            //                0.8   1.2
            // x       x          x             x ------------------ x
            // 0      0.3        1.0           1.7                  100
            //         ^

            Assert.AreEqual(5, networkCoverage.Locations.Values.Count);
            Assert.AreEqual(0.0, networkCoverage.Locations.Values[0].Offset, 1.0e-6);
            Assert.AreEqual(0.3, networkCoverage.Locations.Values[1].Offset, 1.0e-6);
            Assert.AreEqual(1.0, networkCoverage.Locations.Values[2].Offset, 1.0e-6);
            Assert.AreEqual(1.7, networkCoverage.Locations.Values[3].Offset, 1.0e-6);
            Assert.AreEqual(100.0, networkCoverage.Locations.Values[4].Offset, 1.0e-6);

            // repeat with minimumDistance set to 0.5
            HydroNetworkHelper.GenerateDiscretization(networkCoverage, // networkCoverage
                                                      branch1, // branch
                                                      0.5, // minimumDistance
                                                      true,  // gridAtStructure
                                                      0.5, // structureDistance
                                                      false, // gridAtCrossSection
                                                      false, // gridAtFixedLength
                                                      -1); // fixedLength
            // expect gridpoints at 0.3 eliminated
            Assert.AreEqual(4, networkCoverage.Locations.Values.Count);
            Assert.AreEqual(0.0, networkCoverage.Locations.Values[0].Offset, 1.0e-6);
            Assert.AreEqual(1.0, networkCoverage.Locations.Values[1].Offset, 1.0e-6);
            Assert.AreEqual(1.7, networkCoverage.Locations.Values[2].Offset, 1.0e-6);
            Assert.AreEqual(100.0, networkCoverage.Locations.Values[3].Offset, 1.0e-6);
        }

        [Test]
        public void CreateSegments1StructureAtMinimumEndBranch()
        {
            IHydroNetwork network = CreateSegmentTestNetwork();
            var branch1 = network.Channels.First();

            AddTestStructureAt(network, branch1, 99.6);

            var networkCoverage = new Discretization()
                                      {
                                          Network = network,
                                          SegmentGenerationMethod = SegmentGenerationMethod.SegmentBetweenLocations
                                      };

            HydroNetworkHelper.GenerateDiscretization(networkCoverage, // networkCoverage
                                                      branch1, // branch
                                                      0.5, // minimumDistance
                                                      true,  // gridAtStructure
                                                      0.5, // structureDistance
                                                      false, // gridAtCrossSection
                                                      false, // gridAtFixedLength
                                                      -1); // fixedLength

            // structure at less than minimumdistance; expect 1 point left out
            // [-----------------------------------------------------]
            //                                               99.6
            // x-------------------------------------------x-----(x)--- x
            // 0                                          99.1  (99.8) 100
            Assert.AreEqual(3, networkCoverage.Locations.Values.Count);
            Assert.AreEqual(0.0, networkCoverage.Locations.Values[0].Offset, 1.0e-6);
            Assert.AreEqual(99.1, networkCoverage.Locations.Values[1].Offset, 1.0e-6);
            Assert.AreEqual(100.0, networkCoverage.Locations.Values[2].Offset, 1.0e-6);
        }

        [Test]
        public void CreateSegments2StructureAtNearMinimumEndBranch()
        {
            IHydroNetwork network = CreateSegmentTestNetwork();
            var branch1 = network.Channels.First();

            AddTestStructureAt(network, branch1, 99.2);
            AddTestStructureAt(network, branch1, 98.8);

            var networkCoverage = new Discretization()
                                      {
                                          Network = network,
                                          SegmentGenerationMethod = SegmentGenerationMethod.SegmentBetweenLocations
                                      };

            HydroNetworkHelper.GenerateDiscretization(networkCoverage, // networkCoverage
                                                      branch1, // branch
                                                      0.5, // minimumDistance
                                                      true,  // gridAtStructure
                                                      0.5, // structureDistance
                                                      false, // gridAtCrossSection
                                                      false, // gridAtFixedLength
                                                      -1); // fixedLength

            // structure at near minimumdistance; expect 1 point centered at first segment
            // structure at less than minimumdistance; expect 1 point left out
            // [-----------------------------------------------------------]
            //                                             98.8   99.2
            // x----------------------------------------x-------x------x---x
            // 0                                      98.3     99   (99.6) 100

            Assert.AreEqual(4, networkCoverage.Locations.Values.Count);
            Assert.AreEqual(0.0, networkCoverage.Locations.Values[0].Offset, 1.0e-6);
            Assert.AreEqual(98.3, networkCoverage.Locations.Values[1].Offset, 1.0e-6);
            Assert.AreEqual(99.0, networkCoverage.Locations.Values[2].Offset, 1.0e-6);
            //Assert.AreEqual(99.6, networkCoverage.Locations.Values[3].Offset, 1.0e-6);
            Assert.AreEqual(100.0, networkCoverage.Locations.Values[3].Offset, 1.0e-6);
        }

        [Test]
        public void CreateSegmentsCrossSection()
        {
            IHydroNetwork network = CreateSegmentTestNetwork();
            var branch1 = network.Channels.First();

            AddTestCrossSectionAt(network, branch1, 50.0);

            var networkCoverage = new Discretization()
                                      {
                                          Network = network,
                                          SegmentGenerationMethod = SegmentGenerationMethod.SegmentBetweenLocations
                                      };

            HydroNetworkHelper.GenerateDiscretization(networkCoverage, // networkCoverage
                                                      branch1, // branch
                                                      0.5, // minimumDistance
                                                      false,  // gridAtStructure
                                                      0.5, // structureDistance
                                                      true, // gridAtCrossSection
                                                      false, // gridAtFixedLength
                                                      -1); // fixedLength


            Assert.AreEqual(3, networkCoverage.Locations.Values.Count);
            Assert.AreEqual(50.0, networkCoverage.Locations.Values[1].Offset, 1.0e-6);
        }

        [Test]
        public void CreateSegmentsFixedLocations()
        {
            IHydroNetwork network = CreateSegmentTestNetwork();
            var branch1 = network.Channels.First();
            var discretization = new Discretization()
                                      {
                                          Network = network,
                                          SegmentGenerationMethod = SegmentGenerationMethod.SegmentBetweenLocations
                                      };

            HydroNetworkHelper.GenerateDiscretization(discretization, // networkCoverage
                                                      branch1, // branch
                                                      0.5, // minimumDistance
                                                      false,  // gridAtStructure
                                                      0.5, // structureDistance
                                                      false, // gridAtCrossSection
                                                      true, // gridAtFixedLength
                                                      10); // fixedLength
            Assert.AreEqual(11, discretization.Locations.Values.Count);
            Assert.AreEqual(0.0, discretization.Locations.Values[0].Offset, 1.0e-6);
            Assert.AreEqual(50.0, discretization.Locations.Values[5].Offset, 1.0e-6);
            Assert.AreEqual(100.0, discretization.Locations.Values[10].Offset, 1.0e-6);

            INetworkLocation networkLocation = discretization.Locations.Values[7];
            Assert.AreEqual(70.0, networkLocation.Offset, 1.0e-6);
            discretization.ToggleFixedPoint(networkLocation);
            //DiscretizationHelper.SetUserDefinedGridPoint(networkLocation, true);

            HydroNetworkHelper.GenerateDiscretization(discretization, // networkCoverage
                                                      branch1, // branch
                                                      0.5, // minimumDistance
                                                      false,  // gridAtStructure
                                                      0.5, // structureDistance
                                                      false, // gridAtCrossSection
                                                      true, // gridAtFixedLength
                                                      40); // fixedLength
            // expect values at 
            // - 0 and 100 start and end
            // - 70 for fixed location
            // - none between 70 and 100
            // - (0 - 70) > 40, divide in equal parts -> 35
            Assert.AreEqual(4, discretization.Locations.Values.Count);
            Assert.AreEqual(0.0, discretization.Locations.Values[0].Offset, 1.0e-6);
            Assert.AreEqual(35.0, discretization.Locations.Values[1].Offset, 1.0e-6);
            Assert.AreEqual(70.0, discretization.Locations.Values[2].Offset, 1.0e-6);
            Assert.AreEqual(100.0, discretization.Locations.Values[3].Offset, 1.0e-6);
        }

        [Test]
        public void CreateSegmentsForChannelWithCustomLength()
        {
            var network = CreateTestNetwork();
            IChannel firstBranch = (IChannel)network.Branches[0];

            var networkCoverage = new Discretization()
            {
                Network = network,
                SegmentGenerationMethod = SegmentGenerationMethod.SegmentBetweenLocations
            };

            HydroNetworkHelper.GenerateDiscretization(networkCoverage, // networkCoverage
                                                      firstBranch, // branch
                                                      5.0, // minimumDistance
                                                      false,  // gridAtStructure
                                                      0.5, // structureDistance
                                                      false, // gridAtCrossSection
                                                      false, // gridAtFixedLength
                                                      -1); // fixedLength

            Assert.AreEqual(2, networkCoverage.Locations.Values.Count);
            Assert.AreEqual(1, networkCoverage.Segments.Values.Count);
            firstBranch.Length = firstBranch.Length * 2;
            firstBranch.IsLengthCustom = true;
            HydroNetworkHelper.GenerateDiscretization(networkCoverage, // networkCoverage
                                                      firstBranch, // branch
                                                      5.0, // minimumDistance
                                                      false,  // gridAtStructure
                                                      0.5, // structureDistance
                                                      false, // gridAtCrossSection
                                                      false, // gridAtFixedLength
                                                      -1); // fixedLength

            Assert.AreEqual(2, networkCoverage.Locations.Values.Count);
            Assert.AreEqual(1, networkCoverage.Segments.Values.Count);
        }

        [Test]
        public void CreateSegmentsCrossSectionAndMinimumDistance()
        {
            IHydroNetwork network = CreateSegmentTestNetwork();
            var branch1 = network.Channels.First();

            AddTestCrossSectionAt(network, branch1, 1.0);

            var networkCoverage = new Discretization()
                                      {
                                          Network = network,
                                          SegmentGenerationMethod = SegmentGenerationMethod.SegmentBetweenLocations
                                      };

            HydroNetworkHelper.GenerateDiscretization(networkCoverage, // networkCoverage
                                                      branch1, // branch
                                                      5.0, // minimumDistance
                                                      false,  // gridAtStructure
                                                      0.5, // structureDistance
                                                      true, // gridAtCrossSection
                                                      false, // gridAtFixedLength
                                                      -1); // fixedLength

            Assert.AreEqual(2, networkCoverage.Locations.Values.Count);
            Assert.AreEqual(0.0, networkCoverage.Locations.Values[0].Offset, 1.0e-6);
            Assert.AreEqual(100.0, networkCoverage.Locations.Values[1].Offset, 1.0e-6);
        }

        [Test]
        public void CreateSegmentsCrossSectionAndMinimumDistanceNearEnd()
        {
            IHydroNetwork network = CreateSegmentTestNetwork();
            var branch1 = network.Channels.First();

            AddTestCrossSectionAt(network, branch1, 99.0);

            var networkCoverage = new Discretization()
                                      {
                                          Network = network,
                                          SegmentGenerationMethod = SegmentGenerationMethod.SegmentBetweenLocations
                                      };

            HydroNetworkHelper.GenerateDiscretization(networkCoverage, // networkCoverage
                                                      branch1, // branch
                                                      5.0, // minimumDistance
                                                      false,  // gridAtStructure
                                                      0.5, // structureDistance
                                                      true, // gridAtCrossSection
                                                      false, // gridAtFixedLength
                                                      -1); // fixedLength

            Assert.AreEqual(2, networkCoverage.Locations.Values.Count);
            Assert.AreEqual(0.0, networkCoverage.Locations.Values[0].Offset, 1.0e-6);
            Assert.AreEqual(100.0, networkCoverage.Locations.Values[1].Offset, 1.0e-6);
        }

        [Test]
        public void CreateSegmentsMultipleCrossSection()
        {
            IHydroNetwork network = CreateSegmentTestNetwork();
            var branch1 = network.Channels.First();

            // add multiple cross sections and generate calculation points at the cross section locations
            // Grid cells too smal should not be generated.
            AddTestCrossSectionAt(network, branch1, 10.0);
            AddTestCrossSectionAt(network, branch1, 20.0);
            AddTestCrossSectionAt(network, branch1, 30.0);
            AddTestCrossSectionAt(network, branch1, 40.0);
            AddTestCrossSectionAt(network, branch1, 50.0);
            AddTestCrossSectionAt(network, branch1, 60.0);

            var networkCoverage = new Discretization()
                                      {
                                          Network = network,
                                          SegmentGenerationMethod = SegmentGenerationMethod.SegmentBetweenLocations
                                      };

            HydroNetworkHelper.GenerateDiscretization(networkCoverage, // networkCoverage
                                                      branch1, // branch
                                                      5.0, // minimumDistance
                                                      false,  // gridAtStructure
                                                      0.5, // structureDistance
                                                      true, // gridAtCrossSection
                                                      false, // gridAtFixedLength
                                                      -1); // fixedLength

            Assert.AreEqual(8, networkCoverage.Locations.Values.Count);
            Assert.AreEqual(0.0, networkCoverage.Locations.Values[0].Offset, 1.0e-6);
            Assert.AreEqual(10.0, networkCoverage.Locations.Values[1].Offset, 1.0e-6);
            Assert.AreEqual(20.0, networkCoverage.Locations.Values[2].Offset, 1.0e-6);
            Assert.AreEqual(30.0, networkCoverage.Locations.Values[3].Offset, 1.0e-6);
            Assert.AreEqual(40.0, networkCoverage.Locations.Values[4].Offset, 1.0e-6);
            Assert.AreEqual(50.0, networkCoverage.Locations.Values[5].Offset, 1.0e-6);
            Assert.AreEqual(60.0, networkCoverage.Locations.Values[6].Offset, 1.0e-6);
            Assert.AreEqual(100.0, networkCoverage.Locations.Values[7].Offset, 1.0e-6);
        }

        [Test]
        public void CreateSegmentsMultipleCrossSectionAndMinimumDistance()
        {
            IHydroNetwork network = CreateSegmentTestNetwork();
            var branch1 = network.Channels.First();

            // add multiple cross sections and generate calculation points at the cross section locations
            // Grid cells too smal should not be generated.
            AddTestCrossSectionAt(network, branch1, 1.0);
            AddTestCrossSectionAt(network, branch1, 2.0);
            AddTestCrossSectionAt(network, branch1, 3.0);
            AddTestCrossSectionAt(network, branch1, 4.0);
            AddTestCrossSectionAt(network, branch1, 5.0);
            AddTestCrossSectionAt(network, branch1, 6.0);

            var networkCoverage = new Discretization()
                                      {
                                          Network = network,
                                          SegmentGenerationMethod = SegmentGenerationMethod.SegmentBetweenLocations
                                      };

            HydroNetworkHelper.GenerateDiscretization(networkCoverage, // networkCoverage
                                                      branch1, // branch
                                                      5.0, // minimumDistance
                                                      false,  // gridAtStructure
                                                      0.5, // structureDistance
                                                      true, // gridAtCrossSection
                                                      false, // gridAtFixedLength
                                                      -1); // fixedLength

            Assert.AreEqual(3, networkCoverage.Locations.Values.Count);
            Assert.AreEqual(0.0, networkCoverage.Locations.Values[0].Offset, 1.0e-6);
            Assert.AreEqual(5.0, networkCoverage.Locations.Values[1].Offset, 1.0e-6);
            Assert.AreEqual(100.0, networkCoverage.Locations.Values[2].Offset, 1.0e-6);
        }

        [Test]
        public void CreateSegmentsMultipleCrossSectionsAndFixedPoint()
        {
            IHydroNetwork network = CreateSegmentTestNetwork();
            var branch1 = network.Channels.First();

            var discretization = new Discretization()
                                      {
                                          Network = network,
                                          SegmentGenerationMethod = SegmentGenerationMethod.SegmentBetweenLocations
                                      };

            HydroNetworkHelper.GenerateDiscretization(discretization, // networkCoverage
                                                      branch1, // branch
                                                      5.0, // minimumDistance
                                                      false,  // gridAtStructure
                                                      0.5, // structureDistance
                                                      false, // gridAtCrossSection
                                                      true, // gridAtFixedLength
                                                      2); // fixedLength
            Assert.AreEqual(51, discretization.Locations.Values.Count);


            INetworkLocation networkLocation = discretization.Locations.Values.Where(nl => nl.Offset == 8).First();
            //DiscretizationHelper.SetUserDefinedGridPoint(networkLocation, true);
            discretization.ToggleFixedPoint(networkLocation);
            networkLocation = discretization.Locations.Values.Where(nl => nl.Offset == 32).First();
            discretization.ToggleFixedPoint(networkLocation);

            AddTestCrossSectionAt(network, branch1, 10.0);
            AddTestCrossSectionAt(network, branch1, 20.0);
            AddTestCrossSectionAt(network, branch1, 30.0);

            HydroNetworkHelper.GenerateDiscretization(discretization, // networkCoverage
                                                      branch1, // branch
                                                      5.0, // minimumDistance
                                                      false,  // gridAtStructure
                                                      0.5, // structureDistance
                                                      true, // gridAtCrossSection
                                                      false, // gridAtFixedLength
                                                      -1); // fixedLength
            // expect gridpoints at:
            // begin and end 0 and 100
            // fixed locations 8 and 32.
            // 20 for the cross section, 10 and 30 should not be generated due to existing 
            // fixed points and minimium distance 0f 5.
            Assert.AreEqual(5, discretization.Locations.Values.Count);
            Assert.AreEqual(0.0, discretization.Locations.Values[0].Offset, 1.0e-6);
            Assert.AreEqual(8.0, discretization.Locations.Values[1].Offset, 1.0e-6);
            Assert.AreEqual(20.0, discretization.Locations.Values[2].Offset, 1.0e-6);
            Assert.AreEqual(32.0, discretization.Locations.Values[3].Offset, 1.0e-6);
            Assert.AreEqual(100.0, discretization.Locations.Values[4].Offset, 1.0e-6);
        }

        [Test]
        public void CreateSegmentsMultipleStructuresAndFixedPoint()
        {
            IHydroNetwork network = CreateSegmentTestNetwork();
            var branch1 = network.Channels.First();

            var discretization = new Discretization()
                                      {
                                          Network = network,
                                          SegmentGenerationMethod = SegmentGenerationMethod.SegmentBetweenLocations
                                      };

            HydroNetworkHelper.GenerateDiscretization(discretization, // networkCoverage
                                                      branch1, // branch
                                                      5.0, // minimumDistance
                                                      false,  // gridAtStructure
                                                      0.5, // structureDistance
                                                      false, // gridAtCrossSection
                                                      true, // gridAtFixedLength
                                                      2); // fixedLength
            Assert.AreEqual(51, discretization.Locations.Values.Count);


            INetworkLocation networkLocation = discretization.Locations.Values.Where(nl => nl.Offset == 8).First();
            //DiscretizationHelper.SetUserDefinedGridPoint(networkLocation, true);
            discretization.ToggleFixedPoint(networkLocation);
            networkLocation = discretization.Locations.Values.Where(nl => nl.Offset == 32).First();
            discretization.ToggleFixedPoint(networkLocation);
            //DiscretizationHelper.SetUserDefinedGridPoint(networkLocation, true);

            AddTestStructureAt(network, branch1, 10.0);
            AddTestStructureAt(network, branch1, 20.0);
            AddTestStructureAt(network, branch1, 30.0);

            HydroNetworkHelper.GenerateDiscretization(discretization, // networkCoverage
                                                      branch1, // branch
                                                      6.0, // minimumDistance
                                                      true,  // gridAtStructure
                                                      4.0, // structureDistance
                                                      false, // gridAtCrossSection
                                                      false, // gridAtFixedLength
                                                      -1); // fixedLength
            // expect gridpoints with no minimumDistance
            // 0  8 (6 14) (16 24) (26 34) 32 100 
            // 0  6 8 14 16 24 26 32 34 100
            //        10   20   30                 // structure locations
            // 0    8   14    24    32      100    // result 

            // fixed locations 8 and 32.
            // first structure (6) and 14
            // second structure 16 and 24; 16 will be merged into 14 -> 15
            // third structure 26 and (34); 26 will be merged into 24 -> 25
            // fixed points and minimium distance 0f 5.

            Assert.AreEqual(6, discretization.Locations.Values.Count);
            Assert.AreEqual(0.0, discretization.Locations.Values[0].Offset, 1.0e-6);
            Assert.AreEqual(8.0, discretization.Locations.Values[1].Offset, 1.0e-6);
            Assert.AreEqual(15.0, discretization.Locations.Values[2].Offset, 1.0e-6);
            Assert.AreEqual(25.0, discretization.Locations.Values[3].Offset, 1.0e-6);
            Assert.AreEqual(32.0, discretization.Locations.Values[4].Offset, 1.0e-6);
            Assert.AreEqual(100.0, discretization.Locations.Values[5].Offset, 1.0e-6);
        }

        /// <summary>
        /// Test for Jira Issue 2213. Grid points at channel Ovk98 are to close connected.
        /// grid points generated at:
        /// (OVK98, 0)
        /// (OVK98, 0.9999)
        /// (OVK98, 227.6)
        /// (OVK98, 229.6)
        /// (OVK98, 241.4)
        /// (OVK98, 241.5)
        /// (OVK98, 243.4)
        /// (OVK98, 243.4)
        /// (OVK98, 595)
        /// (OVK98, 597)
        /// (OVK98, 730.2)
        /// (OVK98, 732.2)
        /// (OVK98, 1253)
        /// (OVK98, 1255)
        /// (OVK98, 1260.51113164371)
        /// (OVK98, 1261.51114183887)
        /// settings at structure and crosssection
        /// 1m before and after structure
        /// minimum 0.5 m.
        /// thus point at 241.4, 241.5 and 243.4, 243.4 should either be merged or eliminated.
        /// </summary>
        [Test]
        public void JiraTools2213Ovk98()
        {
            var network = new HydroNetwork();
            var channel = new Channel
            {
                Geometry = new LineString(new[]
                                    {
                                        new Coordinate(0, 0), new Coordinate(1262.0, 0),
                                    })
            };
            var node1 = new HydroNode { Network = network, Geometry = new Point(new Coordinate(0, 0)) };
            var node2 = new HydroNode { Network = network, Geometry = new Point(new Coordinate(1262.0, 0)) };
            network.Branches.Add(channel);
            network.Nodes.Add(node1);
            network.Nodes.Add(node2);

            channel.Source = node1;
            channel.Target = node2;

            AddCrossSection(channel, 1.0);
            AddCrossSection(channel, 241.47);
            AddCrossSection(channel, 243.44);
            AddCrossSection(channel, 1260.51);
            AddTestStructureAt(network, channel, 228.61);
            AddTestStructureAt(network, channel, 242.42);
            AddTestStructureAt(network, channel, 596.01);
            AddTestStructureAt(network, channel, 731.25);
            AddTestStructureAt(network, channel, 1253.95);

            var discretization = new Discretization
            {
                Network = network,
                SegmentGenerationMethod = SegmentGenerationMethod.SegmentBetweenLocations
            };

            HydroNetworkHelper.GenerateDiscretization(discretization, // networkCoverage
                                                      channel, // branch
                                                      0.5, // minimumDistance
                                                      true,  // gridAtStructure
                                                      1.0, // structureDistance
                                                      true, // gridAtCrossSection
                                                      false, // gridAtFixedLength
                                                      -1); // fixedLength

            // expected at:
            //  0: 0 = start channel
            //  1: 1 = cross section
            //  2: 227.61 = 1 m before struct
            //  3: 229.61 = 1 m after struct
            //  4: 241.42 = 1 m before struct
            //  5: 243.42 = 1 m after struct
            //  6: 595.01 = 1 m before struct
            //  7: 597.01 = 1 m after struct
            //  8: 730.25 = 1 m before struct
            //  9: 732.25 = 1 m after struct
            // 10: 1252.95 = 1 m before struct
            // 11: 1254.95 = 1 m after struct
            // 12: 1260.51 = cross section
            // 13: 1262 = length channel
            // = skipped cross sections at 241.47 and 243.44

            var gridPoints = discretization.Locations.Values;
            Assert.AreEqual(14, gridPoints.Count);
            Assert.AreEqual(0.0, gridPoints[0].Offset, 1.0e-5);
            Assert.AreEqual(1.0, gridPoints[1].Offset, 1.0e-5);
            Assert.AreEqual(227.61, gridPoints[2].Offset, 1.0e-5);
            Assert.AreEqual(229.61, gridPoints[3].Offset, 1.0e-5);
            Assert.AreEqual(241.42, gridPoints[4].Offset, 1.0e-5);
            Assert.AreEqual(243.42, gridPoints[5].Offset, 1.0e-5);
            Assert.AreEqual(595.01, gridPoints[6].Offset, 1.0e-5);
            Assert.AreEqual(597.01, gridPoints[7].Offset, 1.0e-5);
            Assert.AreEqual(730.25, gridPoints[8].Offset, 1.0e-5);
            Assert.AreEqual(732.25, gridPoints[9].Offset, 1.0e-5);
            Assert.AreEqual(1252.95, gridPoints[10].Offset, 1.0e-5);
            Assert.AreEqual(1254.95, gridPoints[11].Offset, 1.0e-5);
            Assert.AreEqual(1260.51, gridPoints[12].Offset, 1.0e-5);
            Assert.AreEqual(1262, gridPoints[13].Offset, 1.0e-5);

        }

        private static void AddCrossSection(Channel branch, double chainage)
        {
            var crossSection = new CrossSectionDefinitionXYZ
                                   {
                                       Geometry = new LineString(new[] { new Coordinate(chainage, 0), new Coordinate(chainage + 1, 0) }),
                                   };
            HydroNetworkHelper.AddCrossSectionDefinitionToBranch(branch, crossSection, chainage);
        }

        [Test]
        public void SendCustomActionForSplitBranch()
        {
            var network = HydroNetworkHelper.GetSnakeHydroNetwork(new Point(0, 0), new Point(0, 100));
            int callCount = 0;
            IChannel channelToSplit = network.Channels.First();
            ((INotifyPropertyChange)network).PropertyChanged += (s, e) =>
                                                                     {
                                                                         //finished editing
                                                                         if ((e.PropertyName == "IsEditing") &&
                                                                             (!network.IsEditing))
                                                                         {
                                                                             callCount++;
                                                                             var editAction =
                                                                                 (BranchSplitAction)
                                                                                 network.CurrentEditAction;
                                                                             Assert.AreEqual(channelToSplit,
                                                                                             editAction.SplittedBranch);
                                                                             Assert.AreEqual(50,
                                                                                             editAction.SplittedBranch.Length);
                                                                             Assert.AreEqual(
                                                                                 network.Channels.ElementAt(1),
                                                                                 editAction.NewBranch);
                                                                         }
                                                                     };

            HydroNetworkHelper.SplitChannelAtNode(channelToSplit, 50);
            Assert.AreEqual(1, callCount);
        }
    }
}