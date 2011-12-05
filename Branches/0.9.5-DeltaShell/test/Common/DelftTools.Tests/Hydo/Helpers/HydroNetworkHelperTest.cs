using System;
using System.Linq;
using DelftTools.Hydro;
using DelftTools.Hydro.Helpers;
using DelftTools.Hydro.Structures;
using GeoAPI.Extensions.Coverages;
using GisSharpBlog.NetTopologySuite.Geometries;
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

            var node1 = new HydroNode {Network = network, Geometry = new Point(new Coordinate(0, 0))};
            var node2 = new HydroNode {Network = network, Geometry = new Point(new Coordinate(100, 100))};

            network.Branches.Add(branch1);
            network.Nodes.Add(node1);
            network.Nodes.Add(node2);

            var crossSection1 = new CrossSection { Geometry = new LineString(new[] {new Coordinate(15, 20), new Coordinate(15, 20)}) };
            double offset1 = Math.Sqrt(15 * 15 + 20 * 20);
            crossSection1.Offset = offset1;

            var crossSection2 = new CrossSection { Geometry = new LineString(new[] {new Coordinate(85, 70), new Coordinate(85, 70)}) };
            double offset2 = Math.Sqrt(30 * 30 + 40 * 40) + 40 + Math.Sqrt(15 * 15 + 20 * 20);
            crossSection2.Offset = offset2;

            branch1.Source = node1;
            branch1.Target = node2;
            NetworkHelper.AddBranchFeatureToBranch(branch1, crossSection1, crossSection1.Offset);
            NetworkHelper.AddBranchFeatureToBranch(branch1, crossSection2, crossSection2.Offset);

            return network;
        }


        /// <summary>
        /// Creates the testnetwork, inserts a node and test if it added correctly.
        /// </summary>
        [Test]
        [Category("Integration")]
        public void SplitBranchIn2()
        {
            IHydroNetwork network = CreateTestNetwork();
            var branch1 = network.Channels.First();
            double length = branch1.Geometry.Length;

            int nodesCount = network.Nodes.Count;
            IHydroNode hydroNode = HydroNetworkHelper.SplitChannelAtNode(branch1, length / 2);
            Assert.AreEqual(nodesCount + 1, network.Nodes.Count);
            Assert.AreNotEqual(-1, network.Nodes.IndexOf(hydroNode));
        }

        /// <summary>
        /// Creates the testnetwork, adds a route and split the branch.
        /// TOOLS-1199 collection changed events caused recreating routing network triggered
        /// by changing of geometry of branch (removeunused nodes) and temporarily invalid network.
        /// </summary>
        [Test]
        [Category("Integration")]
        public void SplitBranchWithRouteIn2()
        {
            IHydroNetwork network = CreateTestNetwork();
            var branch1 = network.Channels.First();
            double length = branch1.Geometry.Length;

            NetworkCoverage route = new NetworkCoverage
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
        public void SplitBranchWithCrossSections()
        {
            IHydroNetwork network = CreateTestNetwork();

            var branch1 = network.Channels.First();
            double length = branch1.Geometry.Length;

            HydroNetworkHelper.SplitChannelAtNode(branch1, new Coordinate(50, 40), 0, 0);
            
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
            HydroNetworkHelper.GenerateDiscretization(networkCoverage, branch1, new[] {0.0, length/3, 2*length/3, length});

            Assert.AreEqual(4, networkCoverage.Locations.Values.Count);
            Assert.AreEqual(3, networkCoverage.Segments.Values.Count);

            Assert.AreEqual(0, networkCoverage.Locations.Values[0].Offset, 1.0e-6);
            Assert.AreEqual(length/3, networkCoverage.Locations.Values[1].Offset, 1.0e-6);
            Assert.AreEqual(2*length/3, networkCoverage.Locations.Values[2].Offset, 1.0e-6);
            Assert.AreEqual(length, networkCoverage.Locations.Values[3].Offset, 1.0e-6);

            Assert.AreEqual(0, networkCoverage.Segments.Values[0].Offset, 1.0e-6);
            Assert.AreEqual(length / 3, networkCoverage.Segments.Values[0].EndOffset, 1.0e-6);
            Assert.AreEqual(length / 3, networkCoverage.Segments.Values[0].Length, 1.0e-6);

            Assert.AreEqual(length / 3, networkCoverage.Segments.Values[1].Offset, 1.0e-6);
            Assert.AreEqual(2*length/3, networkCoverage.Segments.Values[1].EndOffset, 1.0e-6);

            Assert.AreEqual(2 * length / 3, networkCoverage.Segments.Values[2].Offset, 1.0e-6);
            Assert.AreEqual(length, networkCoverage.Segments.Values[2].EndOffset, 1.0e-6);

            Assert.AreEqual(length / 3, networkCoverage.Segments.Values[0].Length, 1.0e-6);
            Assert.AreEqual(length / 3, networkCoverage.Segments.Values[1].Length, 1.0e-6);
            Assert.AreEqual(length / 3, networkCoverage.Segments.Values[2].Length, 1.0e-6);
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

            Assert.AreEqual(2, networkCoverage.Segments.Values.Count);
            Assert.AreEqual(2, networkCoverage.Segments.Values.Where(s => s.Branch == branch1).Count());
            Assert.AreEqual(0, networkCoverage.Segments.Values.Where(s => s.Branch == branch2).Count());
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

            var node = HydroNetworkHelper.SplitChannelAtNode(branch1, new Coordinate(50, 40), 0, 0); 
           
            // remove the newly added node
            HydroNetworkHelper.MergeNodeBranches(node, network);

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
            var branch1 = (IChannel) network.Branches[0];

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

        private void AddTestStructureAt(IHydroNetwork network, IChannel branch, double offset)
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
            var crossSection = new CrossSection
                                   {
                                       Network = network,
                                       Geometry =
                                           new LineString(new[] {new Coordinate(offset, 0), new Coordinate(offset, 0)}),
                                       Offset = offset
                                   };
            branch.BranchFeatures.Add(crossSection);
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
 
    }
}