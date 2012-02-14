using System;
using System.Collections.Generic;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Extensions.Networks;
using GisSharpBlog.NetTopologySuite.Geometries;
using NetTopologySuite.Extensions.Coverages;
using NetTopologySuite.Extensions.Networks;
using NUnit.Framework;

namespace NetTopologySuite.Extensions.Tests.Coverages
{
    [TestFixture]
    public class NetworkCoverageHelperTest
    {
        private static Network GetNetwork()
        {
            var network = new Network();
            var node1 = new Node("node1");
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
        }

        [Test]
        public void UpdateSegmentsForRoute()
        {
            var network = GetNetwork();
            NetworkCoverage route = new NetworkCoverage
                                        {
                                            Network = network,
                                            SegmentGenerationMethod = SegmentGenerationMethod.RouteBetweenLocations
                                        };
            // [---------------------------------------------------]
            //      5                        60
            route.Locations.Values.Add(new NetworkLocation(network.Branches[0], 5.0));
            route.Locations.Values.Add(new NetworkLocation(network.Branches[0], 60.0));

            // expected result
            //      [5-----------------------60]

            Assert.AreEqual(1, route.Segments.Values.Count);
            Assert.AreEqual(5.0, route.Segments.Values[0].Offset);
            Assert.AreEqual(60.0, route.Segments.Values[0].EndOffset);
        }

        [Test]
        public void UpdateSegmentsForSegmentBetweenLocations()
        {
            var network = GetNetwork();
            NetworkCoverage route = new NetworkCoverage
            {
                Network = network,
                SegmentGenerationMethod = SegmentGenerationMethod.SegmentBetweenLocations
            };
            // [---------------------------------------------------]
            //      5                        60
            //            40
            route.Locations.Values.Add(new NetworkLocation(network.Branches[0], 5.0));
            route.Locations.Values.Add(new NetworkLocation(network.Branches[0], 60.0));
            route.Locations.Values.Add(new NetworkLocation(network.Branches[0], 40.0));

            // expect location to be sorted uysing offset
            // [    5-----40
            //            40-----------------60

            Assert.AreEqual(2, route.Segments.Values.Count);
            Assert.AreEqual(5.0, route.Segments.Values[0].Offset);
            Assert.AreEqual(40.0, route.Segments.Values[0].EndOffset);
            Assert.AreEqual(40.0, route.Segments.Values[1].Offset);
            Assert.AreEqual(60.0, route.Segments.Values[1].EndOffset);
        }

        [Test]
        public void UpdateSegmentsForSegmentBetweenLocationsReversed()
        {
            var network = GetNetwork();
            NetworkCoverage route = new NetworkCoverage
            {
                Network = network,
                SegmentGenerationMethod = SegmentGenerationMethod.SegmentBetweenLocations
            };
            route.Locations.IsAutoSorted = false;
            route.Locations.Values.Add(new NetworkLocation(network.Branches[0], 60.0));
            route.Locations.Values.Add(new NetworkLocation(network.Branches[0], 40.0));
            route.Locations.Values.Add(new NetworkLocation(network.Branches[0], 5.0));

            // expect location not to be sorted
            // [    60-----40
            //             40-----------------5
            //NetworkCoverageHelper.UpdateSegments(route);
            Assert.AreEqual(2, route.Segments.Values.Count);
            Assert.AreEqual(60.0, route.Segments.Values[0].Offset);
            Assert.AreEqual(40.0, route.Segments.Values[0].EndOffset);
            Assert.IsFalse(route.Segments.Values[0].DirectionIsPositive);
            
            Assert.AreEqual(40.0, route.Segments.Values[1].Offset);
            Assert.AreEqual(5.0, route.Segments.Values[1].EndOffset);
            Assert.IsFalse(route.Segments.Values[1].DirectionIsPositive);
        }

        [Test]
        public void UpdateSegmentsForSegmentBetweenLocationsReversedForRouteBetweenLocation()
        {
            var network = GetNetwork();
            NetworkCoverage route = new NetworkCoverage
            {
                Network = network,
                SegmentGenerationMethod = SegmentGenerationMethod.RouteBetweenLocations
            };
            route.Locations.IsAutoSorted = false;
            route.Locations.Values.Add(new NetworkLocation(network.Branches[0], 60.0));
            route.Locations.Values.Add(new NetworkLocation(network.Branches[0], 40.0));
            route.Locations.Values.Add(new NetworkLocation(network.Branches[0], 5.0));

            // expect location not to be sorted
            // [    60-----40
            //             40-----------------5
            //NetworkCoverageHelper.UpdateSegments(route);
            Assert.AreEqual(2, route.Segments.Values.Count);
            Assert.AreEqual(60.0, route.Segments.Values[0].Offset);
            Assert.AreEqual(40.0, route.Segments.Values[0].EndOffset);
            Assert.IsFalse(route.Segments.Values[0].DirectionIsPositive);

            Assert.AreEqual(40.0, route.Segments.Values[1].Offset);
            Assert.AreEqual(5.0, route.Segments.Values[1].EndOffset);
            Assert.IsFalse(route.Segments.Values[1].DirectionIsPositive);
        }


        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void ExtraTimeSliceFromNonTimeDependentNetworkCoverage()
        {
            var network = GetNetwork();
            var networkCoverage = new NetworkCoverage("test", false) { Network = network };
            networkCoverage[new NetworkLocation(network.Branches[0], 10.0)] = 10.0;
            networkCoverage[new NetworkLocation(network.Branches[0], 90.0)] = 90.0;
            networkCoverage[new NetworkLocation(network.Branches[1], 10.0)] = 110.0;
            networkCoverage[new NetworkLocation(network.Branches[1], 90.0)] = 190.0;
            // networkcoverage is not time dependent thus expect an argumentexception
            NetworkCoverageHelper.ExtractTimeSlice(networkCoverage, DateTime.Now);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void ExtractNonExistingTimeSliceFromNetworkCoverage()
        {
            var network = GetNetwork();
            var networkCoverage = new NetworkCoverage("test", true) { Network = network };

            DateTime dateTime = DateTime.Now;
            networkCoverage[dateTime, new NetworkLocation(network.Branches[0], 10.0)] = 10.0;
            networkCoverage[dateTime, new NetworkLocation(network.Branches[0], 90.0)] = 90.0;
            networkCoverage[dateTime, new NetworkLocation(network.Branches[1], 10.0)] = 110.0;
            networkCoverage[dateTime, new NetworkLocation(network.Branches[1], 90.0)] = 190.0;
            // networkcoverage is time dependent but queried time is not available.
            NetworkCoverageHelper.ExtractTimeSlice(networkCoverage, dateTime + new TimeSpan(1, 0, 0));
        }

        [Test]
        public void ExtractTimeSlice()
        {
            var network = GetNetwork();
            var networkCoverage = new NetworkCoverage("test", true) { Network = network };

            DateTime []dateTimes = new DateTime[10];

            for (int i = 0; i < 10; i++)
            {
                dateTimes[i] = new DateTime(2000, 1, 1, 1, /* minute */i, 0);
                networkCoverage[dateTimes[i], new NetworkLocation(network.Branches[0], 10.0)] = 10.0 + i;
                networkCoverage[dateTimes[i], new NetworkLocation(network.Branches[0], 90.0)] = 90.0 + i;
                networkCoverage[dateTimes[i], new NetworkLocation(network.Branches[1], 10.0)] = 110.0 + i;
                networkCoverage[dateTimes[i], new NetworkLocation(network.Branches[1], 90.0)] = 190.0 + i;
            }

            INetworkCoverage slice = NetworkCoverageHelper.ExtractTimeSlice(networkCoverage, new DateTime(2000, 1, 1, 1, /* minute */0, 0));
            Assert.AreEqual(false, slice.IsTimeDependent);
            Assert.AreEqual(10.0 + 0, slice.Evaluate(new NetworkLocation(network.Branches[0], 10.0)));
            Assert.AreEqual(90.0 + 0, slice.Evaluate(new NetworkLocation(network.Branches[0], 90.0)));
            Assert.AreEqual(110.0 + 0, slice.Evaluate(new NetworkLocation(network.Branches[1], 10.0)));
            Assert.AreEqual(190.0 + 0, slice.Evaluate(new NetworkLocation(network.Branches[1], 90.0)));

            //slice = NetworkCoverageHelper.ExtractTimeSlice(networkCoverage, new DateTime(2000, 1, 1, 1, /* minute */9, 0));
            slice = new NetworkCoverage(networkCoverage.Name, false);
            NetworkCoverageHelper.ExtractTimeSlice(networkCoverage, slice,
                                                           new DateTime(2000, 1, 1, 1, /* minute */9, 0), true);
            Assert.AreEqual(false, slice.IsTimeDependent);
            Assert.AreEqual(10.0 + 9, slice.Evaluate(new NetworkLocation(network.Branches[0], 10.0)));
            Assert.AreEqual(90.0 + 9, slice.Evaluate(new NetworkLocation(network.Branches[0], 90.0)));
            Assert.AreEqual(110.0 + 9, slice.Evaluate(new NetworkLocation(network.Branches[1], 10.0)));
            Assert.AreEqual(190.0 + 9, slice.Evaluate(new NetworkLocation(network.Branches[1], 90.0)));

            // just repeat the previous action; refilling a coverage should also work
            NetworkCoverageHelper.ExtractTimeSlice(networkCoverage, slice,
                                                           new DateTime(2000, 1, 1, 1, /* minute */9, 0), true);
            Assert.AreEqual(false, slice.IsTimeDependent);
            Assert.AreEqual(10.0 + 9, slice.Evaluate(new NetworkLocation(network.Branches[0], 10.0)));
            Assert.AreEqual(90.0 + 9, slice.Evaluate(new NetworkLocation(network.Branches[0], 90.0)));
            Assert.AreEqual(110.0 + 9, slice.Evaluate(new NetworkLocation(network.Branches[1], 10.0)));
            Assert.AreEqual(190.0 + 9, slice.Evaluate(new NetworkLocation(network.Branches[1], 90.0)));
        }

        /// <summary>
        /// Extract values from coverage but use networklocation from the target coverage.
        /// </summary>
        [Test]
        [Ignore]
        public void ExtractTimeSliceUseLocationsFromTarget()
        {
            var network = GetNetwork();
            var networkCoverage = new NetworkCoverage("test", true) { Network = network };

            DateTime[] dateTimes = new DateTime[10];

            for (int i = 0; i < 10; i++)
            {
                dateTimes[i] = new DateTime(2000, 1, 1, 1, /* minute */i, 0);
                networkCoverage[dateTimes[i], new NetworkLocation(network.Branches[0], 10.0)] = 10.0 + i;
                networkCoverage[dateTimes[i], new NetworkLocation(network.Branches[0], 50.0)] = 50.0 + i;
                networkCoverage[dateTimes[i], new NetworkLocation(network.Branches[0], 90.0)] = 90.0 + i;
                networkCoverage[dateTimes[i], new NetworkLocation(network.Branches[1], 10.0)] = 110.0 + i;
                networkCoverage[dateTimes[i], new NetworkLocation(network.Branches[1], 50.0)] = 150.0 + i;
                networkCoverage[dateTimes[i], new NetworkLocation(network.Branches[1], 90.0)] = 190.0 + i;
            }

            var slice = new NetworkCoverage("slice", false) { Network = network };
            Assert.AreEqual(false, slice.IsTimeDependent);
            slice[new NetworkLocation(network.Branches[0], 20.0)] = 2;
            slice[new NetworkLocation(network.Branches[0], 80.0)] = 8;
            slice[new NetworkLocation(network.Branches[1], 20.0)] = 12;
            slice[new NetworkLocation(network.Branches[1], 80.0)] = 18;

            NetworkCoverageHelper.ExtractTimeSlice(networkCoverage, slice,
                                                           new DateTime(2000, 1, 1, 1, /* minute */0, 0), false);

            // expected results at time step 0 are 20 80 120 180; this are interpolated values
            Assert.AreEqual(4, slice.Locations.Values.Count);
            Assert.AreEqual(10 /*20*/, slice.Evaluate(new NetworkLocation(network.Branches[0], 20.0)), 1.0e-6);
            Assert.AreEqual(50 /*80*/, slice.Evaluate(new NetworkLocation(network.Branches[0], 80.0)), 1.0e-6);
            Assert.AreEqual(90 /*120*/, slice.Evaluate(new NetworkLocation(network.Branches[1], 20.0)), 1.0e-6);
            Assert.AreEqual(110 /*180*/, slice.Evaluate(new NetworkLocation(network.Branches[1], 80.0)), 1.0e-6);

            NetworkCoverageHelper.ExtractTimeSlice(networkCoverage, slice,
                                                           new DateTime(2000, 1, 1, 1, /* minute */3, 0), false);

            // expected results at time step 3 are 23 83 123 183; this are interpolated values
            Assert.AreEqual(4, slice.Locations.Values.Count);
            Assert.AreEqual(13 /*23*/, slice.Evaluate(new NetworkLocation(network.Branches[0], 20.0)), 1.0e-6);
            Assert.AreEqual(53 /*83*/, slice.Evaluate(new NetworkLocation(network.Branches[0], 80.0)), 1.0e-6);
            Assert.AreEqual(93 /*123*/, slice.Evaluate(new NetworkLocation(network.Branches[1], 20.0)), 1.0e-6);
            Assert.AreEqual(113 /*183*/, slice.Evaluate(new NetworkLocation(network.Branches[1], 80.0)), 1.0e-6);
        }

        /// <summary>
        /// test if the nodatavalues - magic values - aree also copied from source to target.
        /// </summary>
        [Test]
        public void ExtractTimeSliceNoDataValues()
        {
            var network = GetNetwork();
            var networkCoverage = new NetworkCoverage("test", true) { Network = network };

            DateTime dateTime = DateTime.Now;

            networkCoverage[dateTime, new NetworkLocation(network.Branches[0], 10.0)] = 10.0;
            networkCoverage[dateTime, new NetworkLocation(network.Branches[0], 90.0)] = 90.0;
            networkCoverage[dateTime, new NetworkLocation(network.Branches[1], 10.0)] = 110.0;
            networkCoverage[dateTime, new NetworkLocation(network.Branches[1], 90.0)] = 190.0;
            networkCoverage.Components[0].NoDataValues.Add(16.0);

            INetworkCoverage slice = NetworkCoverageHelper.ExtractTimeSlice(networkCoverage, dateTime);

            Assert.AreEqual(networkCoverage.Components[0].NoDataValues.Count, slice.Components[0].NoDataValues.Count);
            for (int i = 0; i < slice.Components[0].NoDataValues.Count; i++)
            {
                Assert.AreEqual(networkCoverage.Components[0].NoDataValues[i], slice.Components[0].NoDataValues[i]);
            }
        }
    }
}
