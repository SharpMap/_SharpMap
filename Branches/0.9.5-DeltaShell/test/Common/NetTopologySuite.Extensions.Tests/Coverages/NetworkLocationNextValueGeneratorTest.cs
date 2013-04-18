using GeoAPI.Extensions.Coverages;
using NetTopologySuite.Extensions.Coverages;
using NetTopologySuite.Extensions.Networks;
using NUnit.Framework;

namespace NetTopologySuite.Extensions.Tests.Coverages
{
    [TestFixture]
    public class NetworkLocationNextValueGeneratorTest
    {
        [Test]
        public void NextValueOnSameBranch()
        {
            var network = new Network();

            var node1 = new Node("node1");
            var node2 = new Node("node2");
            network.Nodes.Add(node1);
            network.Nodes.Add(node2);
            
            var branch1 = new Branch("branch1", node1, node2, 100.0);
            network.Branches.Add(branch1);
            
            // create network coverate
            var networkCoverage = new NetworkCoverage { Network = network };
            
            NetworkLocationNextValueGenerator networkLocationNextValueGenerator = new NetworkLocationNextValueGenerator(networkCoverage);
            
            INetworkLocation location = networkLocationNextValueGenerator.GetNextValue();
            Assert.AreEqual(new NetworkLocation(branch1, 0), location);
            networkCoverage.Locations.Values.Add(location);
            
            Assert.AreEqual(new NetworkLocation(branch1, 1), networkLocationNextValueGenerator.GetNextValue());
        }

        [Test]
        public void NextValueOnNextBranch()
        {
            var network = new Network();

            var node1 = new Node("node1");
            var node2 = new Node("node2");
            var node3 = new Node("node3");
            network.Nodes.Add(node1);
            network.Nodes.Add(node2);
            network.Nodes.Add(node3);

            var branch1 = new Branch("branch1", node1, node2, 100.0);
            var branch2 = new Branch("branch2", node1, node2, 200.0);
            network.Branches.Add(branch1);
            network.Branches.Add(branch2);

            // create network coverate
            var networkCoverage = new NetworkCoverage { Network = network };

            NetworkLocationNextValueGenerator networkLocationNextValueGenerator = new NetworkLocationNextValueGenerator(networkCoverage);
            networkCoverage.Locations.Values.Add(new NetworkLocation(branch1, 100));
            
            Assert.AreEqual(new NetworkLocation(branch2, 0), networkLocationNextValueGenerator.GetNextValue());
        }
    }
}
