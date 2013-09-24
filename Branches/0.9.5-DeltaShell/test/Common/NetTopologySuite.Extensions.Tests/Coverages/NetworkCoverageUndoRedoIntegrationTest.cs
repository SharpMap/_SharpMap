using System;
using System.Collections.Generic;
using DelftTools.TestUtils;
using DelftTools.Utils.UndoRedo;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Extensions.Networks;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;
using NUnit.Framework;
using NetTopologySuite.Extensions.Coverages;
using NetTopologySuite.Extensions.Networks;
using log4net;

namespace NetTopologySuite.Extensions.Tests.Coverages
{
    [TestFixture]
    class NetworkCoverageUndoRedoIntegrationTest
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(NetworkCoverageTest));

        [SetUp]
        public void TestFixtureSetUp()
        {
            LogHelper.ConfigureLogging();
        }

        [TearDown]
        public void TestFixtureTearDown()
        {
            LogHelper.ResetLogging();
        }

        private static Network CreateNetworkWithSingleBranch(double branchLength)
        {
            var network = new Network {Name = "network"};
            var sourceNode = new Node { Geometry = new Point(0, 0), Network = network, Name = "source"};
            var targetNode = new Node { Geometry = new Point(branchLength, 0), Network = network, Name = "target"};
            var branch = new Branch(sourceNode, targetNode, branchLength)
                {
                    Name = "branch",
                    Geometry = new LineString(new ICoordinate[]
                        {
                            new Coordinate(0, 0),
                            new Coordinate(branchLength, 0)
                        }),
                    Network = network
                };
            network.Nodes.Add(sourceNode);
            network.Nodes.Add(targetNode);
            network.Branches.Add(branch);
            return network;
        }

        private static INetworkCoverage CreateNetworkDiscretisation(INetwork network, double offset)
        {
            var locations = new List<INetworkLocation>();
            var chainage = offset;
            foreach (var branch in network.Branches)
            {
                chainage = Math.Max(0, chainage);
                while (chainage < branch.Length)
                {
                    locations.Add(new NetworkLocation {Branch = branch, Chainage = chainage});
                    chainage += offset;
                }
                chainage -= branch.Length;
            }
            var coverage = new Discretization
                {
                    Name = "grid",
                    Network = network
                };
            coverage.Locations.Values.AddRange(locations);
            return coverage;
        }

        [Test]
        public void SplitBranchNearGrid()
        {
            var network = CreateNetworkWithSingleBranch(50);
            var coverage = CreateNetworkDiscretisation(network, 10);
            using (new UndoRedoManager(coverage))
            {
                var branchToSplit = network.Branches[0];
                NetworkHelper.SplitBranchAtNode(branchToSplit, 1);
                var locations = coverage.Locations.Values;
            }
        }
    }
}
