using System;
using System.Collections.Generic;
using System.Linq;
using DelftTools.TestUtils;
using log4net;
using log4net.Config;
using NetTopologySuite.Extensions.Networks;
using NUnit.Framework;
using QuickGraph;
using QuickGraph.Algorithms.Observers;
using QuickGraph.Algorithms.ShortestPath;

namespace GeoAPI.Extentions.Tests.Networks
{
    [TestFixture]
    public class NetworkGraphAlgorithmTest
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(NetworkGraphAlgorithmTest));

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


        [Test]
        public void ShortestPathOnNetwork()
        {
            var network = new Network();

            var nodeA = new Node { Name = "A" };
            var nodeB1 = new Node { Name = "B1_IAmShort" };
            var nodeB2 = new Node { Name = "B2_PleaseAvoidMeIAmLooooooooooooong" };
            var nodeC = new Node { Name = "C" };

            var edgeAB1 = new Branch(nodeA, nodeB1);
            var edgeAB2 = new Branch(nodeA, nodeB2);
            var edgeB1C = new Branch(nodeB1, nodeC);
            var edgeB2C = new Branch(nodeB2, nodeC);

            network.Nodes.Add(nodeA);
            network.Nodes.Add(nodeB1);
            network.Nodes.Add(nodeB2);
            network.Nodes.Add(nodeC);

            network.Branches.Add(edgeAB1);
            network.Branches.Add(edgeAB2);
            network.Branches.Add(edgeB1C);
            network.Branches.Add(edgeB2C);

            var shortestPath = network.GetShortestPath(nodeA, nodeC, b => b.Name.Length);

            log.Debug(String.Join(" -> ", shortestPath.Select(b => b.Name).ToArray()));

            Assert.AreEqual(2, shortestPath.Count());
            Assert.AreEqual(edgeAB1, shortestPath.First());
            Assert.AreEqual(edgeB1C, shortestPath.Last());
        }

        [Test]
        public void FindShortestPathForSimpleUndirectedGraphUsingDijkstraAlgorithm()
        {
            var graph = new UndirectedGraph<object, Edge<object>>(true);
            object v1 = "vertex1";
            object v2 = "vertex2";
            object v3 = "vertex3";
            var e1 = new Edge<object>(v1, v2);
            var e2 = new Edge<object>(v2, v3);
            var e3 = new Edge<object>(v3, v1);
            graph.AddVertex(v1);
            graph.AddVertex(v2);
            graph.AddVertex(v3);
            graph.AddEdge(e1);
            graph.AddEdge(e2);
            graph.AddEdge(e3);

            var algorithm = new UndirectedDijkstraShortestPathAlgorithm<object, Edge<object>>(graph, edge => (double)1);
            var observer = new UndirectedVertexPredecessorRecorderObserver<object, Edge<object>>();
            using (observer.Attach(algorithm))
            {
                algorithm.Compute(v1);
            }
            
            IEnumerable<Edge<object>> path;
            observer.TryGetPath(v3, out path);

            foreach (var edge in path)
            {
                Console.WriteLine(edge);
            }
        }        

    }
}