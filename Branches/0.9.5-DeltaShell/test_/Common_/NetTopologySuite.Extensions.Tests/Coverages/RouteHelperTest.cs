using System;
using System.Collections.Generic;
using System.Linq;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Extensions.Networks;
using GisSharpBlog.NetTopologySuite.Geometries;
using NetTopologySuite.Extensions.Coverages;
using NetTopologySuite.Extensions.Networks;
using NUnit.Framework;
using SharpMap.Converters.WellKnownText;

namespace NetTopologySuite.Extensions.Tests.Coverages
{
    [TestFixture]
    public class RouteHelperTest
    {
        public static INetwork GetSnakeNetwork(int numberOfBranches, bool generateIDs)
        {
            IList<Point> points = new List<Point>();
            //create a random network by moving constantly right
            double currentX = 0;
            double currentY = 0;
            var random = new Random();
            int numberOfNodes = numberOfBranches + 1;
            for (int i = 0; i < numberOfNodes; i++)
            {
                //generate a network of branches of length 100 moving right by random angle.
                points.Add(new Point(currentX, currentY));
                //angle between -90 and +90
                double angle = random.Next(180) - 90;
                //x is cos between 0<1
                //y is sin between 1 and -1
                currentX += 100 * Math.Cos(DegreeToRadian(angle));//between 0 and 100
                currentY += 100 * Math.Sin(DegreeToRadian(angle));//between -100 and 100
            }
            return GetSnakeHydroNetwork(generateIDs, points.ToArray());
        }

        public static INetwork GetSnakeHydroNetwork(bool generateIDs, params Point[] points)
        {
            var network = new Network();
            for (int i = 0; i < points.Length; i++)
            {
                var nodeName = "node" + (i + 1);

                network.Nodes.Add(new Node(nodeName) { Geometry = points[i] });
            }
            for (int i = 1; i < points.Length; i++)
            {
                var lineGeometry = new LineString(new[]
                                              {
                                                  new Coordinate(points[i-1].X, points[i-1].Y),
                                                  new Coordinate(points[i].X, points[i].Y)
                                              });

                var branchName = "branch" + i;
                var branch = new Branch(network.Nodes[i - 1], network.Nodes[i], lineGeometry.Length)
                {
                    Geometry = lineGeometry,
                    Name = branchName,

                };
                //setting id is optional ..needed for netcdf..but fatal for nhibernate (thinks it saved already)
                if (generateIDs)
                {
                    branch.Id = i;
                }
                network.Branches.Add(branch);
            }
            return network;
        }

        private static double DegreeToRadian(double angle)
        {
            return Math.PI*angle/180.0;
        }

        [Test]
        public void CreateSegments()
        {
            var network = GetSnakeHydroNetwork(false, new Point(0, 0), new Point(100, 0), new Point(200, 100));
                //; var network = GetNetwork();
            var branch1 = network.Branches[0];
            var routeCoverage = RouteHelper.CreateRoute(
                new[] { new NetworkLocation(branch1, 0), new NetworkLocation(branch1, 50), new NetworkLocation(branch1, 10) });

            Assert.AreEqual(2, routeCoverage.Segments.Values.Count);
            Assert.AreEqual(0, routeCoverage.Segments.Values[0].Offset);
            Assert.AreEqual(50, routeCoverage.Segments.Values[0].EndOffset);
            Assert.AreEqual(50, routeCoverage.Segments.Values[1].Offset);
            Assert.AreEqual(10, routeCoverage.Segments.Values[1].EndOffset);

        }
        
        [Test]
        public void GetLocationsForRoute()
        {
            var network = CreateThreeNodesNetwork();
            var branch = (Branch)network.Branches[0];

            var source = new NetworkCoverage { Network = network };
            source[new NetworkLocation(branch, 10.0)] = 10;
            source[new NetworkLocation(branch, 50.0)] = 30;
            source[new NetworkLocation(branch, 60.0)] = 20;
            source[new NetworkLocation(branch, 80.0)] = 10;

            var route = RouteHelper.CreateRoute(new[] { new NetworkLocation(branch, 0.0)
                , new NetworkLocation(branch, 90.0) });
            var expectedLocations = new[]
                                        {
                                            new NetworkLocation(branch, 0.0)
                                            , new NetworkLocation(branch, 10.0)
                                            , new NetworkLocation(branch, 50.0)
                                            , new NetworkLocation(branch, 60.0)
                                            , new NetworkLocation(branch, 80.0)
                                            , new NetworkLocation(branch, 90)
                                        };
            Assert.AreEqual(expectedLocations, RouteHelper.GetLocationsInRoute(source, route));
        }
        [Test] 
        public void GetLocationsForAnotherRoute()
        {
            var network = CreateThreeNodesNetwork();
            var branch = (Branch)network.Branches[0];

            var source = new NetworkCoverage { Network = network };
            source[new NetworkLocation(branch, 10.0)] = 10;
            source[new NetworkLocation(branch, 50.0)] = 30;
            source[new NetworkLocation(branch, 60.0)] = 20;
            source[new NetworkLocation(branch, 80.0)] = 10;

            //90-->0-->80
            var route = RouteHelper.CreateRoute(new[] { new NetworkLocation(branch, 90.0)
                , new NetworkLocation(branch, 0.0),new NetworkLocation(branch,80) });
            var expectedLocations = new[]
                                        {
                                            new NetworkLocation(branch, 90.0)
                                            , new NetworkLocation(branch, 80.0)
                                            , new NetworkLocation(branch, 60.0)
                                            , new NetworkLocation(branch, 50.0)
                                            , new NetworkLocation(branch, 10.0)
                                            , new NetworkLocation(branch, 0)
                                            , new NetworkLocation(branch, 10.0)
                                            , new NetworkLocation(branch, 50.0)
                                            , new NetworkLocation(branch, 60.0)
                                            , new NetworkLocation(branch, 80.0)
                                        };

            var actualLocations = RouteHelper.GetLocationsInRoute(source, route);
            Assert.AreEqual(expectedLocations, actualLocations);
        }
        public void GetLocationsForSegmentInCorrectOrder()
        {
            var network = CreateThreeNodesNetwork();
            var branch = (Branch) network.Branches[0];
            
            var source = new NetworkCoverage { Network = network };
            source[new NetworkLocation(branch, 10.0)] = 10;
            source[new NetworkLocation(branch, 50.0)] = 30;
            source[new NetworkLocation(branch, 60.0)] = 20;
            source[new NetworkLocation(branch, 80.0)] = 10;

            // retrieve the location of source that overlap with segmentUp
            var segmentUp = new NetworkSegment { Branch = branch, Offset = 30, Length = 40 };
            var locationsUp = RouteHelper.GetLocationsForSegment(segmentUp, source, false).ToList();

            Assert.AreEqual(new NetworkLocation(branch, 50.0),locationsUp[0]);
            Assert.AreEqual(new NetworkLocation(branch, 60.0), locationsUp[1]);

            // retrieve the location of source that overlap with segmentDown; the direction
            // is negative and the location offset are thus descending
            var segmentDown = new NetworkSegment
                                  {
                                      Branch = branch,
                                      Offset = 90,
                                      Length = 50,
                                      DirectionIsPositive = false
                                  };
            var locationsDown = RouteHelper.GetLocationsForSegment(segmentDown, source, false).ToList();
            Assert.AreEqual(new NetworkLocation(branch, 80.0), locationsDown[0]);
            Assert.AreEqual(new NetworkLocation(branch, 60.0), locationsDown[1]);
            Assert.AreEqual(new NetworkLocation(branch, 50.0), locationsDown[2]);
        }
        
        /*private static Network GetNetwork()
        {
            var network = new Network();
            var node1 = new Node("node1") {Geometry = new Point(0, 0)};
            var node2 = new Node("node2"); 
            var node3 = new Node("node3");
            

            var geometry1 = new LineString(new[]
                                              {
                                                  new Coordinate(0, 0),
                                                  new Coordinate(0, 100)
                                              });
            var geometry2 = new LineString(new[]
                                              {
                                                  new Coordinate(0, 100),
                                                  new Coordinate(0, 200)
                                              });
            IBranch branch1 = new Branch(node1, node2, 100) { Geometry = geometry1, Name = "branch1" };
            IBranch branch2 = new Branch(node2, node3, 100) { Geometry = geometry2, Name = "branch2" };
            
            network.Nodes.Add(node1);
            network.Nodes.Add(node2);
            network.Nodes.Add(node3);
            network.Branches.Add(branch1);
            network.Branches.Add(branch2);

            return network;
        }*/

        

        [Test]
        public void GetRouteLength()
        {
            var network = CreateThreeNodesNetwork();

            NetworkCoverage route = new NetworkCoverage
            {
                Network = network,
                SegmentGenerationMethod = SegmentGenerationMethod.RouteBetweenLocations
            };
            route.Locations.AutoSort = false;
            //route going back to branch 0
            route.Locations.Values.Add(new NetworkLocation(network.Branches[0], 5.0));
            route.Locations.Values.Add(new NetworkLocation(network.Branches[1], 60.0));
            route.Locations.Values.Add(new NetworkLocation(network.Branches[0], 10.0));
            Assert.AreEqual(305.0, RouteHelper.GetRouteLength(route));            
        }

        [Test]
        public void LocationAreUniqueIsCorrect()
        {
            //take a input coverage e.g depth Ci
            //take a route coverage Cr
            //result is Function dependend of X with components equal to the components of Ci
            //where x is distance along Cr
            var network = CreateThreeNodesNetwork();

            NetworkCoverage source = new NetworkCoverage { Network = network };
            source[new NetworkLocation(network.Branches[0], 10.0)] = 10;
            source[new NetworkLocation(network.Branches[0], 50.0)] = 30;
            source[new NetworkLocation(network.Branches[1], 50.0)] = 20;
            source[new NetworkLocation(network.Branches[1], 150.0)] = 10;
            
            NetworkCoverage returningRoute = new NetworkCoverage
            {
                Network = network,
                SegmentGenerationMethod = SegmentGenerationMethod.RouteBetweenLocations
            };
            //route going back to branch 0
            returningRoute.Locations.Values.Add(new NetworkLocation(network.Branches[0], 5.0));
            returningRoute.Locations.Values.Add(new NetworkLocation(network.Branches[1], 60.0));
            returningRoute.Locations.Values.Add(new NetworkLocation(network.Branches[0], 10.0));

            Assert.IsFalse((RouteHelper.LocationsAreUniqueOnRoute(source, returningRoute)));

            NetworkCoverage singleRoute = new NetworkCoverage
            {
                Network = network,
                SegmentGenerationMethod = SegmentGenerationMethod.SegmentBetweenLocations
            };
            //route going one way
            singleRoute.Locations.Values.Add(new NetworkLocation(network.Branches[0], 5.0));
            singleRoute.Locations.Values.Add(new NetworkLocation(network.Branches[1], 60.0));

            Assert.IsTrue((RouteHelper.LocationsAreUniqueOnRoute(source, singleRoute)));
        }
        
        [Test]
        public void GetRouteSegments()
        {
            INetwork network = CreateThreeNodesNetwork();
            NetworkCoverage route = new NetworkCoverage
                                        {
                                            Network = network,
                                            SegmentGenerationMethod = SegmentGenerationMethod.RouteBetweenLocations
                                        };
            route.Locations.AutoSort = false;
            route.Locations.Values.Add(new NetworkLocation(network.Branches[0], 0));
            route.Locations.Values.Add(new NetworkLocation(network.Branches[0], 100.0));
            Assert.AreEqual(1, route.Segments.Values.Count);
            Assert.AreEqual(100, route.Segments.Values[0].EndOffset);
            Assert.AreEqual(0, route.Segments.Values[0].Offset);
        }

        

        private static INetwork CreateThreeNodesNetwork()
        {
            return GetSnakeHydroNetwork(false, new Point(0, 0), new Point(100, 0), new Point(300, 0));
        }

        [Test]
        public void GetSegmentForNetworkLocationTest()
        {
            var network = CreateThreeNodesNetwork(); // new Point(0, 0), new Point(100, 0), new Point(300, 0)
            var branch0T100 = (Branch)network.Branches[0];
            var branch100T300 = (Branch)network.Branches[1];

            var route = RouteHelper.CreateRoute(new[]
                                            {
                                                new NetworkLocation(branch0T100, 20.0), 
                                                new NetworkLocation(branch0T100, 80.0),
                                                new NetworkLocation(branch100T300, 20.0)
                                            });
            Assert.AreEqual(3, route.Segments.Values.Count);
            INetworkSegment networkSegment;
            networkSegment = RouteHelper.GetSegmentForNetworkLocation(route, new NetworkLocation(branch0T100, 10));
            Assert.IsNull(networkSegment);
            networkSegment = RouteHelper.GetSegmentForNetworkLocation(route, new NetworkLocation(branch0T100, 50));
            Assert.AreEqual(networkSegment, route.Segments.Values[0]);
            networkSegment = RouteHelper.GetSegmentForNetworkLocation(route, new NetworkLocation(branch0T100, 90));
            Assert.AreEqual(networkSegment, route.Segments.Values[1]);
            networkSegment = RouteHelper.GetSegmentForNetworkLocation(route, new NetworkLocation(branch100T300, 10));
            Assert.AreEqual(networkSegment, route.Segments.Values[2]);
            networkSegment = RouteHelper.GetSegmentForNetworkLocation(route, new NetworkLocation(branch100T300, 30));
            Assert.IsNull(networkSegment);
        }


    }
}

