using System;
using NetTopologySuite.Extensions.Coverages;
using NetTopologySuite.Extensions.Networks;
using NUnit.Framework;
using SharpMap.Converters.WellKnownText;

namespace NetTopologySuite.Extensions.Tests.Coverages
{
    [TestFixture]
    public class NetworkLocationTypeConverterTest
    {
        [Test]
        public void ConvertToStore()
        {
            var network = new Network();

            var node1 = new Node("node1");
            var node2 = new Node("node2");
            var node3 = new Node("node3");
            network.Nodes.Add(node1);
            network.Nodes.Add(node2);
            network.Nodes.Add(node3);

            var branch1 = new Branch("branch1", node1, node2, 100.0) { Geometry = GeometryFromWKT.Parse("LINESTRING (0 0, 100 0)") };
            var branch2 = new Branch("branch2", node2, node3, 200.0) { Geometry = GeometryFromWKT.Parse("LINESTRING (100 0, 200 300)") };
            network.Branches.Add(branch1);
            network.Branches.Add(branch2);

            var location = new NetworkLocation(network.Branches[1], 22);

            var typeConverter = new NetworkLocationTypeConverter(network);
            var tuple = typeConverter.ConvertToStore(location); 
            Assert.AreEqual(4,tuple.Length);

            //id 
            Assert.AreEqual(1,tuple[0]);
            //offset
            Assert.AreEqual(22.0d, tuple[1]);
            //x
            Assert.AreEqual(106.957d, (double)tuple[2],0.001d);
            //y
            Assert.AreEqual(20.871d, (double)tuple[3], 0.001d);
        }
    }
}