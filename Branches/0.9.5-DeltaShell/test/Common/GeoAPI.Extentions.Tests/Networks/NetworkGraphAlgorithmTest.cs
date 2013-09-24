using System;
using System.Collections.Generic;
using NUnit.Framework;
using QuickGraph;
using QuickGraph.Algorithms.Observers;
using QuickGraph.Algorithms.ShortestPath;

namespace GeoAPI.Extentions.Tests.Networks
{
    [TestFixture]
    public class NetworkGraphAlgorithmTest
    {
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