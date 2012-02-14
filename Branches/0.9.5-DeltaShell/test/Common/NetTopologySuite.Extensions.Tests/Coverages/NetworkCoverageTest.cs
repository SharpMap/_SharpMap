using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using DelftTools.Functions;
using DelftTools.Functions.Filters;
using DelftTools.Functions.Generic;
using DelftTools.TestUtils;
using DelftTools.Units;
using DelftTools.Utils;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Extensions.Networks;
using GisSharpBlog.NetTopologySuite.Geometries;
using log4net;
using NetTopologySuite.Extensions.Coverages;
using NetTopologySuite.Extensions.Networks;
using NUnit.Framework;
using SharpMap;
using SharpMap.Converters.WellKnownText;
using SharpMap.Data.Providers;
using SharpMap.Layers;
using SharpMapTestUtils;
using Point=GisSharpBlog.NetTopologySuite.Geometries.Point;

namespace NetTopologySuite.Extensions.Tests.Coverages
{
    [TestFixture]
    public class NetworkCoverageTest
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(NetworkCoverageTest));

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
        [NUnit.Framework.Category(TestCategory.Performance)]
        public void AddingTimeSlicesShouldBeFastUsingMemoryStore()
        {
            var random = new Random();
            //50 branches 
            var network = RouteHelperTest.GetSnakeNetwork(false, 50);
            //10 offsets
            var offsets = new[] { 0, 10, 20, 30, 40, 50, 60, 70, 80, 90 };

            //500 locations
            var locations = from o in offsets
                            from b in network.Branches
                            select new NetworkLocation(b, o);

            var values = (from o in locations
                          select (double)random.Next(100) / 10).ToList();

            //setup the coverage with fixed size locations
            var networkCoverage = new NetworkCoverage() { IsTimeDependent = true, Network = network };
            networkCoverage.Locations.FixedSize = 500;
            networkCoverage.Locations.SetValues(locations.OrderBy(l => l));

            var startTime = new DateTime(2000, 1, 1);
            // add 10000 slices in time..
            var times = from i in Enumerable.Range(1, 1000)
                        select startTime.AddDays(i);

            int outputTimeStepIndex = 0;
            //write like a flowmodel ..
            TestHelper.AssertIsFasterThan(1700, () =>
                                                    {
                                                        foreach (var t in times)
                                                        {
                                                            //high performance writing. Using indexes instead of values.
                                                            var locationsIndexFilter =
                                                                new VariableIndexRangeFilter(networkCoverage.Locations,
                                                                                             0,
                                                                                             networkCoverage.Locations.
                                                                                                 FixedSize - 1);
                                                            //current timestep starts at 1 and is increased before outputvalues are set now..hence -2 to get a 0 for the 1st
                                                            //int timeIndex = currentTimeStep - 1;

                                                            var timeIndexFilter =
                                                                new VariableIndexRangeFilter(networkCoverage.Time,
                                                                                             outputTimeStepIndex);

                                                            networkCoverage.Time.AddValues(new[] { t });
                                                            networkCoverage.SetValues(values,
                                                                                      new[]
                                                                                          {
                                                                                              locationsIndexFilter,
                                                                                              timeIndexFilter
                                                                                          });
                                                            outputTimeStepIndex++;
                                                        }
                                                    });
        }

        [Test]
        public void ShouldNotBeAbleToAddTheSameLocationTwice()
        {
            var network = CreateNetwork();


            var networkCoverage = new NetworkCoverage("test", false) { Network = network };
            var l1 = new NetworkLocation(network.Branches[0], 10.0d);
            var l2 = new NetworkLocation(network.Branches[0], 10.0d);
            networkCoverage.Components[0][l1] = 1.0d;
            networkCoverage.Components[0][l2] = 3.0d;
            Assert.AreEqual(1,networkCoverage.Locations.Values.Count);


        }
        [Test]
        public void WriteSlicesUsingTimeFilterShouldBeTheSameAsUsingIndexFilter()
        {
            var network = CreateNetwork();


            var networkCoverage = new NetworkCoverage("test", true) { Network = network };
            var locations = new[] { new NetworkLocation(network.Branches[0], 0.0),
                                    new NetworkLocation(network.Branches[0], 100.0), 
                                    new NetworkLocation(network.Branches[1], 100.0) };
            networkCoverage.SetLocations(locations);
            networkCoverage.Locations.FixedSize = locations.Length;

            var networkCoverage2 = new NetworkCoverage("test", true) { Network = network };
            networkCoverage2.SetLocations(locations);
            networkCoverage2.Locations.FixedSize = locations.Length;


            // set 1000 values using time filters in coverage 1 and 2
            var startTime = new DateTime(2000, 1, 1);
            for (int i = 0; i < 1000; i++)
            {
                IEnumerable<double> values = new[] { 1.0, 2.0, 3.0 }.Select(d => d * i);
                DateTime currentTime = startTime.AddMinutes(i);
                //set values for coverage 1 using value filter
                networkCoverage.Time.AddValues(new[] { currentTime });
                var timeValueFilter = new VariableValueFilter<DateTime>(networkCoverage.Time, currentTime);
                networkCoverage.SetValues(values, timeValueFilter);

                //set values for coverage 2 using index filter
                networkCoverage2.Time.AddValues(new[] { currentTime });
                var timeIndexFilter = new VariableIndexRangeFilter(networkCoverage2.Time, i);
                networkCoverage2.SetValues(values, timeIndexFilter);

            }

            Assert.AreEqual(networkCoverage.Components[0].Values, networkCoverage2.Components[0].Values);
        }

        [Test]
        public void CreateForExistingNetwork()
        {
            var network = CreateNetwork();

            INetworkCoverage networkCoverage = new NetworkCoverage { Network = network };

            // set values
            networkCoverage[new NetworkLocation(network.Branches[0], 0.0)] = 0.1;
            networkCoverage[new NetworkLocation(network.Branches[0], 100.0)] = 0.2;
            networkCoverage[new NetworkLocation(network.Branches[1], 0.0)] = 0.3;

            //check the last set was OK
            Assert.AreEqual(new NetworkLocation(network.Branches[1], 0.0), networkCoverage.Locations.Values[2]);

            networkCoverage[new NetworkLocation(network.Branches[1], 50.0)] = 0.4;
            networkCoverage[new NetworkLocation(network.Branches[1], 200.0)] = 0.5;

            // asserts
            Assert.AreEqual(typeof(double), networkCoverage.Components[0].ValueType);
            Assert.AreEqual(typeof(INetworkLocation), networkCoverage.Arguments[0].ValueType);
            Assert.AreEqual(5, networkCoverage.Components[0].Values.Count);

            // Assert.AreEqual(2, networkCoverage.Locations.Components.Count, "networkLocation = (branch, grid), 2 components");
            Assert.AreEqual(0, networkCoverage.Locations.Arguments.Count,
                            "networkLocation = (branch, grid), has no arguments");

            //Networklocation is value type :)
            Assert.AreEqual(0.5, networkCoverage[new NetworkLocation(network.Branches[1], 200.0)]);

            // logging
            log.Debug("Network coverage values:");
            var networkLocations = networkCoverage.Arguments[0].Values;
            var values = networkCoverage.Components[0].Values;
            for (var i = 0; i < networkCoverage.Components[0].Values.Count; i++)
            {
                var networkLocation = (INetworkLocation)networkLocations[i];
                log.DebugFormat("NetworkCoverage[location = ({0} - {1,6:F})] = {2}", networkLocation.Branch,
                                networkLocation.Offset, values[i]); // ... trying to change formatting
            }
        }

        private static INetwork CreateNetwork()
        {
            return RouteHelperTest.GetSnakeNetwork(false, new Point(0, 0), new Point(100, 0), new Point(300, 0));
        }


        [Test]
        public void SplitDividesCoverageAmongBranches()
        {
            //a network with a single branch
            var network = RouteHelperTest.GetSnakeNetwork(false, new Point(0, 0), new Point(100, 0));


        }
        [Test]
        public void DefaultValueTest()
        {
            var network = CreateNetwork();

            INetworkCoverage networkCoverage = new NetworkCoverage { Network = network, DefaultValue = 0.33 };
            INetworkLocation nl11 = new NetworkLocation(network.Branches[0], 0.0);

            // no network location set in networkCoverage expect the default value to return
            Assert.AreEqual(0.33, networkCoverage.Evaluate(nl11));
        }

        [Test]
        public void DefaultValueTestValidTime()
        {
            var network = CreateNetwork();

            var networkCoverage = new NetworkCoverage("test", true) { Network = network, DefaultValue = 0.33 };

            var dateTime = DateTime.Now;
            networkCoverage[dateTime, new NetworkLocation(network.Branches[0], 50.0)] = 0.1;
            networkCoverage.Locations.ExtrapolationType = ExtrapolationType.Constant;

            // ask value form other branch; default value is expected
            INetworkLocation nl11 = new NetworkLocation(network.Branches[1], 0.0);

            // no network location set in networkCoverage expect the default value to return
            Assert.AreEqual(0.33, networkCoverage.Evaluate(dateTime, nl11));
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void DefaultValueTestInvalidTime()
        {
            var network = CreateNetwork();

            var networkCoverage = new NetworkCoverage("test", true) { Network = network, DefaultValue = 0.33 };

            INetworkLocation nl11 = new NetworkLocation(network.Branches[0], 0.0);

            // no network location set in networkCoverage expect the default value to return
            Assert.AreEqual(0.33, networkCoverage.Evaluate(DateTime.Now, nl11));
        }

        [Test]
        public void InterpolationNoTime()
        {
            var network = CreateNetwork();

            INetworkCoverage networkCoverage = new NetworkCoverage { Network = network };

            // test for defaultvalue

            // set values
            INetworkLocation nl11 = new NetworkLocation(network.Branches[0], 0.0);
            INetworkLocation nl12 = new NetworkLocation(network.Branches[0], 100.0);
            INetworkLocation nl13 = new NetworkLocation(network.Branches[1], 100.0);
            networkCoverage[nl11] = 0.1;
            networkCoverage[nl12] = 0.2;
            networkCoverage[nl13] = 0.3;

            // test the exact networklocation
            Assert.AreEqual(0.1, networkCoverage.Evaluate(nl11));
            Assert.AreEqual(0.2, networkCoverage.Evaluate(nl12));
            Assert.AreEqual(0.3, networkCoverage.Evaluate(nl13));

            INetworkLocation nl21 = new NetworkLocation(network.Branches[0], 0.0);
            INetworkLocation nl22 = new NetworkLocation(network.Branches[0], 100.0);
            INetworkLocation nl23 = new NetworkLocation(network.Branches[1], 0.0);
            INetworkLocation nl24 = new NetworkLocation(network.Branches[1], 200.0);

            // test for networklocations at same location but other instances
            // branch and offset nl21 equals nl11 
            Assert.AreEqual(0.1, networkCoverage.Evaluate(nl21));
            // branch and offset nl22 equals nl12 
            Assert.AreEqual(0.2, networkCoverage.Evaluate(nl22));

            // test for value at new location with constant interpolation (1 values available at branch)
            // expect value of nl13 to be set for complete branches[1]
            Assert.AreEqual(0.3, networkCoverage.Evaluate(nl23));
            Assert.AreEqual(0.3, networkCoverage.Evaluate(nl24));

            // test for interpolation
            INetworkLocation nl1 = new NetworkLocation(network.Branches[0], 50.0);
            Assert.AreEqual(0.15, networkCoverage.Evaluate(nl1), 1e-6);
        }
        [Test]
        public void ExtraPolationNoTime()
        {
            var network = CreateNetwork();

            INetworkCoverage networkCoverage = new NetworkCoverage { Network = network };

            // test for defaultvalue

            // set two values
            INetworkLocation nl11 = new NetworkLocation(network.Branches[0], 5.0);
            INetworkLocation nl12 = new NetworkLocation(network.Branches[0], 10.0);
            networkCoverage[nl11] = 0.1;
            networkCoverage[nl12] = 0.1;

            //extrapolation 
            Assert.AreEqual(0.1, networkCoverage.Evaluate(new NetworkLocation(network.Branches[0], 20.0)));
        }
        [Test]
        public void InterpolationTime()
        {
            var network = CreateNetwork();

            var dateTime = DateTime.Now;

            var networkCoverage = new NetworkCoverage("test", true) { Network = network };
            // test for defaultvalue

            // set values
            INetworkLocation nl11 = new NetworkLocation(network.Branches[0], 0.0);
            INetworkLocation nl12 = new NetworkLocation(network.Branches[0], 100.0);
            INetworkLocation nl13 = new NetworkLocation(network.Branches[1], 100.0);
            networkCoverage[dateTime, nl11] = 0.1;
            networkCoverage[dateTime, nl12] = 0.2;
            networkCoverage[dateTime, nl13] = 0.3;

            // test the exact networklocation
            Assert.AreEqual(0.1, networkCoverage.Evaluate(dateTime, nl11));
            Assert.AreEqual(0.2, networkCoverage.Evaluate(dateTime, nl12));
            Assert.AreEqual(0.3, networkCoverage.Evaluate(dateTime, nl13));

            INetworkLocation nl21 = new NetworkLocation(network.Branches[0], 0.0);
            INetworkLocation nl22 = new NetworkLocation(network.Branches[0], 100.0);
            INetworkLocation nl23 = new NetworkLocation(network.Branches[1], 0.0);
            INetworkLocation nl24 = new NetworkLocation(network.Branches[1], 200.0);

            // test for networklocations at same location but other instances
            // branch and offset nl21 equals nl11 
            Assert.AreEqual(0.1, networkCoverage.Evaluate(dateTime, nl21));
            // branch and offset nl22 equals nl12 
            Assert.AreEqual(0.2, networkCoverage.Evaluate(dateTime, nl22));

            // test for value at new location with constant interpolation (1 values available at branch)
            // expect value of nl13 to be set for complete branches[1]
            Assert.AreEqual(0.3, networkCoverage.Evaluate(dateTime, nl23));
            Assert.AreEqual(0.3, networkCoverage.Evaluate(dateTime, nl24));

            // test for interpolation
            INetworkLocation nl1 = new NetworkLocation(network.Branches[0], 50.0);
            Assert.AreEqual(0.15, networkCoverage.Evaluate(dateTime, nl1), 1e-6);
        }

        [Test]
        public void AnotherInterpolationTime()
        {
            var network = CreateNetwork();

            var dateTime = DateTime.Now;
            var networkCoverage = new NetworkCoverage("test", true) { Network = network };

            networkCoverage[dateTime, new NetworkLocation(network.Branches[0], 10.0)] = 0.1;
            networkCoverage[dateTime, new NetworkLocation(network.Branches[0], 90.0)] = 0.9;

            // at the exact locations
            Assert.AreEqual(0.1, networkCoverage.Evaluate(dateTime, new NetworkLocation(network.Branches[0], 10.0)), 1e-6);
            Assert.AreEqual(0.9, networkCoverage.Evaluate(dateTime, new NetworkLocation(network.Branches[0], 90.0)), 1e-6);

            // at start and end outside the locations
            Assert.AreEqual(0.10, networkCoverage.Evaluate(dateTime, new NetworkLocation(network.Branches[0], 5.0)), 1e-6);
            Assert.AreEqual(0.90, networkCoverage.Evaluate(dateTime, new NetworkLocation(network.Branches[0], 95.0)), 1e-6);

            // in between the 2 locations
            Assert.AreEqual(0.35, networkCoverage.Evaluate(dateTime, new NetworkLocation(network.Branches[0], 35.0)), 1e-6);
        }

        [Test]
        public void AnotherInterpolationNoTime()
        {
            var network = CreateNetwork();

            INetworkCoverage networkCoverage = new NetworkCoverage { Network = network };

            networkCoverage[new NetworkLocation(network.Branches[0], 10.0)] = 0.1;
            networkCoverage[new NetworkLocation(network.Branches[0], 90.0)] = 0.9;

            networkCoverage.Locations.ExtrapolationType = ExtrapolationType.Constant;
            networkCoverage.Locations.InterpolationType = InterpolationType.Linear;

            // at the exact locations
            Assert.AreEqual(0.1, networkCoverage.Evaluate(new NetworkLocation(network.Branches[0], 10.0)));
            Assert.AreEqual(0.9, networkCoverage.Evaluate(new NetworkLocation(network.Branches[0], 90.0)));

            // at start and end outside the locations
            Assert.AreEqual(0.10, networkCoverage.Evaluate(new NetworkLocation(network.Branches[0], 5.0)));
            Assert.AreEqual(0.90, networkCoverage.Evaluate(new NetworkLocation(network.Branches[0], 95.0)), 1e-6);

            // in between the 2 locations
            Assert.AreEqual(0.35, networkCoverage.Evaluate(new NetworkLocation(network.Branches[0], 35.0)), 1e-6);
        }

        [Test]
        public void GetTimeSeriesForCoverageOnLocation()
        {
            var network = CreateNetwork();
            //set up  a coverage on one location for three moments
            INetworkCoverage networkCoverage = new NetworkCoverage { Network = network, IsTimeDependent = true };
            var networkLocation = new NetworkLocation(network.Branches[0], 0);

            for (var i = 1; i < 4; i++)
            {
                networkCoverage[new DateTime(2000, 1, i), networkLocation] = (double)i;
            }

            //filter the function for the networkLocation
            IFunction filteredCoverage = networkCoverage.GetTimeSeries(networkLocation);

            Assert.AreEqual(3, filteredCoverage.Components[0].Values.Count);
            Assert.AreEqual(1, filteredCoverage.Arguments.Count);
            Assert.AreEqual(filteredCoverage.Arguments[0].Values, networkCoverage.Time.Values);
        }

        [Test]
        public void GetTimeSeriesForCoverageOnCoordinate()
        {
            var network = CreateNetwork();

            //set up  a coverage on one location for three moments
            INetworkCoverage networkCoverage = new NetworkCoverage { Network = network, IsTimeDependent = true };
            var networkLocation = new NetworkLocation(network.Branches[0], 0);

            for (int i = 1; i < 4; i++)
            {
                networkCoverage[new DateTime(2000, 1, i), networkLocation] = (double)i;
            }

            //filter the function for the networkLocation
            IFunction filteredCoverage = networkCoverage.GetTimeSeries(networkLocation.Geometry.Coordinate);

            Assert.AreEqual(3, filteredCoverage.Components[0].Values.Count);
            Assert.AreEqual(1, filteredCoverage.Arguments.Count);
            Assert.AreEqual(filteredCoverage.Arguments[0].Values, networkCoverage.Time.Values);
        }

        [Test]
        public void GenerageSegmentsWhenLocationsAreAdded()
        {
            var network = CreateNetwork();

            var networkCoverage = new NetworkCoverage { Network = network };
            networkCoverage[new NetworkLocation(network.Branches[0], 10.0)] = 0.1;
            networkCoverage[new NetworkLocation(network.Branches[0], 50.0)] = 0.5;
            networkCoverage[new NetworkLocation(network.Branches[0], 90.0)] = 0.9;

            Assert.AreEqual(3, networkCoverage.Segments.Values.Count);
        }

        [Test]
        public void GenerateSegmentPerLocation()
        {
            var network = CreateNetwork();

            var networkCoverage = new NetworkCoverage
                                      {
                                          Network = network,
                                          SegmentGenerationMethod = SegmentGenerationMethod.SegmentPerLocation
                                      };
            networkCoverage[new NetworkLocation(network.Branches[0], 10.0)] = 0.1;
            networkCoverage[new NetworkLocation(network.Branches[0], 50.0)] = 0.5;
            networkCoverage[new NetworkLocation(network.Branches[0], 90.0)] = 0.9;

            Assert.AreEqual(3, networkCoverage.Segments.Values.Count);
            // [--10--------------50-------------------------------90---]
            // [----------][-----------------------][-------------------]
            // 0          30                       70                  100
            Assert.AreEqual(0, networkCoverage.Segments.Values[0].Offset, 1.0e-6);
            Assert.AreEqual(30, networkCoverage.Segments.Values[0].EndOffset, 1.0e-6);
            Assert.AreEqual(30, networkCoverage.Segments.Values[1].Offset, 1.0e-6);
            Assert.AreEqual(70, networkCoverage.Segments.Values[1].EndOffset, 1.0e-6);
            Assert.AreEqual(70, networkCoverage.Segments.Values[2].Offset, 1.0e-6);
            Assert.AreEqual(100, networkCoverage.Segments.Values[2].EndOffset, 1.0e-6);
        }

        [Test]
        public void GenerateSegmentPerLocationOrderModified()
        {
            var network = CreateNetwork();

            var networkCoverage = new NetworkCoverage
            {
                Network = network,
                SegmentGenerationMethod = SegmentGenerationMethod.SegmentPerLocation
            };
            var valueAt10 = new NetworkLocation(network.Branches[0], 1.0);
            var valueAt90 = new NetworkLocation(network.Branches[0], 2.0);
            var valueAt50 = new NetworkLocation(network.Branches[0], 3.0);
            networkCoverage[valueAt10] = 0.1;
            networkCoverage[valueAt90] = 0.9;
            networkCoverage[valueAt50] = 0.5;

            // change offset and check result are identical to above test GenerateSegmentPerLocation
            valueAt50.Offset = 50;
            valueAt90.Offset = 90;
            valueAt10.Offset = 10;

            Assert.AreEqual(3, networkCoverage.Segments.Values.Count);
            // [--10--------------50-------------------------------90---]
            // [----------][-----------------------][-------------------]
            // 0          30                       70                  100
            Assert.AreEqual(0, networkCoverage.Segments.Values[0].Offset, 1.0e-6);
            Assert.AreEqual(30, networkCoverage.Segments.Values[0].EndOffset, 1.0e-6);
            Assert.AreEqual(30, networkCoverage.Segments.Values[1].Offset, 1.0e-6);
            Assert.AreEqual(70, networkCoverage.Segments.Values[1].EndOffset, 1.0e-6);
            Assert.AreEqual(70, networkCoverage.Segments.Values[2].Offset, 1.0e-6);
            Assert.AreEqual(100, networkCoverage.Segments.Values[2].EndOffset, 1.0e-6);
        }

        [Test]
        public void ChangeOffsetOfNeworkLocationShouldChangeNetworkLocationOrder()
        {
            var network = CreateNetwork();

            var networkCoverage = new NetworkCoverage
            {
                Network = network,
                SegmentGenerationMethod = SegmentGenerationMethod.SegmentPerLocation
            };
            var valueAt10 = new NetworkLocation(network.Branches[0], 10.0);
            var valueAt20 = new NetworkLocation(network.Branches[0], 20.0);
            var valueAt30 = new NetworkLocation(network.Branches[0], 30.0);
            var valueAt40 = new NetworkLocation(network.Branches[0], 40.0);
            networkCoverage[valueAt10] = 0.1;
            networkCoverage[valueAt20] = 0.2;
            networkCoverage[valueAt30] = 0.3;
            networkCoverage[valueAt40] = 0.4;
            valueAt20.Offset = 35;
            Assert.AreEqual(valueAt10, networkCoverage.Locations.Values[0]);
            Assert.AreEqual(valueAt30, networkCoverage.Locations.Values[1]);
            Assert.AreEqual(valueAt20, networkCoverage.Locations.Values[2]);
            Assert.AreEqual(valueAt40, networkCoverage.Locations.Values[3]);
        }


        [Test]
        public void GenerateSegmentBetweenLocations()
        {
            var network = CreateNetwork();

            var networkCoverage = new NetworkCoverage { Network = network, SegmentGenerationMethod = SegmentGenerationMethod.RouteBetweenLocations };
            networkCoverage[new NetworkLocation(network.Branches[0], 10.0)] = 0.0;
            networkCoverage[new NetworkLocation(network.Branches[0], 50.0)] = 50.0;
            networkCoverage[new NetworkLocation(network.Branches[0], 90.0)] = 90.0;

            Assert.AreEqual(2, networkCoverage.Segments.Values.Count);

            var firstSegment = networkCoverage.Segments.Values.First();
            Assert.AreEqual(10.0, firstSegment.Offset);
            Assert.AreEqual(40.0, firstSegment.Length);

            var lastSegment = networkCoverage.Segments.Values.Last();
            Assert.AreEqual(50.0, lastSegment.Offset);
            Assert.AreEqual(40.0, lastSegment.Length);
        }

        /// <summary>
        /// o--------------------o----------------------------------o
        ///           s1       s2  s3     s4
        ///     [------------][--][--][---------]
        /// 
        ///     10           90  100 10        90   
        /// </summary>
        [Test]
        [NUnit.Framework.Category(TestCategory.WindowsForms)]
        public void GenerateSegmentsForNetworkCoverageOnTwoBranches()
        {
            var network = CreateNetwork();

            var networkCoverage = new NetworkCoverage { Network = network, SegmentGenerationMethod = SegmentGenerationMethod.RouteBetweenLocations };
            networkCoverage[new NetworkLocation(network.Branches[0], 10.0)] = 0.0;
            networkCoverage[new NetworkLocation(network.Branches[0], 90.0)] = 90.0;
            networkCoverage[new NetworkLocation(network.Branches[1], 10.0)] = 110.0;
            networkCoverage[new NetworkLocation(network.Branches[1], 90.0)] = 190.0;

            Assert.AreEqual(4, networkCoverage.Segments.Values.Count);

            var segments = networkCoverage.Segments.Values;

            Assert.AreEqual(network.Branches[0], segments[0].Branch);
            Assert.AreEqual(10.0, segments[0].Offset);
            Assert.AreEqual(80.0, segments[0].Length);

            Assert.AreEqual(network.Branches[0], segments[1].Branch);
            Assert.AreEqual(90.0, segments[1].Offset);
            Assert.AreEqual(10.0, segments[1].Length, 1e-6);

            Assert.AreEqual(network.Branches[1], segments[2].Branch);
            Assert.AreEqual(0.0, segments[2].Offset);
            Assert.AreEqual(10.0, segments[2].Length);

            Assert.AreEqual(network.Branches[1], segments[3].Branch);
            Assert.AreEqual(10.0, segments[3].Offset);
            Assert.AreEqual(80.0, segments[3].Length);

            var networkCoverageLayer = new NetworkCoverageGroupLayer { NetworkCoverage = networkCoverage };

            var map = new Map(new Size(1000, 1000));
            map.Layers.Add(networkCoverageLayer);

            NetworkCoverageLayerHelper.SetupRouteNetworkCoverageLayerTheme(networkCoverageLayer, null);

            // add branch/node layers
            var branchLayer = new VectorLayer { DataSource = new FeatureCollection { Features = (IList)network.Branches } };
            map.Layers.Add(branchLayer);
            var nodeLayer = new VectorLayer { DataSource = new FeatureCollection { Features = (IList)network.Nodes } };
            map.Layers.Add(nodeLayer);

            MapTestHelper.Show(map);
        }

        [Test]
        public void FilterCoverageDoesNotReturnCoverageWhenReduced()
        {
            NetworkCoverage coverage = new NetworkCoverage();
            coverage.IsTimeDependent = true;
            //fix the coverage on a location so we have a funtion with only one argument...time.
            var filtered = coverage.Filter(new VariableReduceFilter(coverage.Locations));
            Assert.IsFalse(filtered is INetworkCoverage);
        }

        [Test]
        public void CloneNetworkCoverage()
        {
            var network = CreateNetwork();

            INetworkCoverage networkCoverage = new NetworkCoverage { Network = network };

            // set values
            networkCoverage[new NetworkLocation(network.Branches[0], 0.0)] = 0.1;
            networkCoverage[new NetworkLocation(network.Branches[0], 100.0)] = 0.2;
            networkCoverage[new NetworkLocation(network.Branches[1], 0.0)] = 0.3;
            networkCoverage[new NetworkLocation(network.Branches[1], 50.0)] = 0.4;
            networkCoverage[new NetworkLocation(network.Branches[1], 200.0)] = 0.5;

            networkCoverage.Locations.Unit = new Unit("networklocation","b/o");

            INetworkCoverage clone = (INetworkCoverage)networkCoverage.Clone();
            Assert.AreEqual(5, clone.Locations.Values.Count);
            Assert.AreNotSame(clone.Locations.Values[0], networkCoverage.Locations.Values[0]);

            Assert.AreNotSame(networkCoverage.Locations.Unit,clone.Locations.Unit);
            Assert.AreEqual(networkCoverage.Locations.Unit.Name, clone.Locations.Unit.Name);
        }



        /* todo make independent on networkeditorplugin or move it to a good place        [Test]
                [Category(TestCategory.WindowsForms)]
                public void EditRoute()
                {
                    var network = new HydroNetwork();

                    var mapControl = new MapControl();

                    var networkMapLayer = HydroNetworkEditorHelper.CreateMapLayerForHydroNetwork(network);
                    mapControl.Map.Layers.Add(networkMapLayer);

                    HydroNetworkEditorHelper.InitializeNetworkEditor(null, mapControl);

                    // add 2 branches
                    networkMapLayer.ChannelLayer.DataSource.Add(GeometryFromWKT.Parse("LINESTRING (0 0, 100 0)"));
                    networkMapLayer.ChannelLayer.DataSource.Add(GeometryFromWKT.Parse("LINESTRING (100 0, 200 0)"));
                    networkMapLayer.ChannelLayer.DataSource.Add(GeometryFromWKT.Parse("LINESTRING (200 0, 300 0)"));
                    networkMapLayer.ChannelLayer.DataSource.Add(GeometryFromWKT.Parse("LINESTRING (200 0, 200 100)"));

                    network.Branches[0].Name = "branch1";
                    network.Branches[1].Name = "branch2";
                    network.Branches[2].Name = "branch3";
                    network.Branches[3].Name = "branch4";

                    network.Branches[0].Length = 100;
                    network.Branches[1].Length = 100;
                    network.Branches[2].Length = 100;
                    network.Branches[3].Length = 100;

                    network.Nodes[0].Name = "node1";
                    network.Nodes[1].Name = "node2";
                    network.Nodes[2].Name = "node3";
                    network.Nodes[3].Name = "node4";
                    network.Nodes[4].Name = "node5";

                    // add network coverage layer 
                    var routeCoverage = new NetworkCoverage { Network = network, SegmentGenerationMethod = SegmentGenerationMethod.RouteBetweenLocations };
                    routeCoverage[new NetworkLocation(network.Branches[0], 10.0)] = 0.0;
                    routeCoverage[new NetworkLocation(network.Branches[0], 90.0)] = 90.0;
                    routeCoverage[new NetworkLocation(network.Branches[1], 90.0)] = 190.0;

                    var networkCoverageLayer = new NetworkCoverageLayer { NetworkCoverage = routeCoverage };
                    NetworkCoverageLayerHelper.SetupRouteNetworkCoverageLayerTheme(networkCoverageLayer, null);
                    mapControl.Map.Layers.Insert(0, networkCoverageLayer);

                    // add label layers
                    var nodeLabelLayer = new LabelLayer("Nodes")
                                             {
                                                 DataSource = networkMapLayer.NodeLayer.DataSource,
                                                 LabelColumn = "Name"
                                             };
                    nodeLabelLayer.Style.VerticalAlignment = LabelStyle.VerticalAlignmentEnum.Top;
                    mapControl.Map.Layers.Insert(0, nodeLabelLayer);

                    var branchLabelLayer = new LabelLayer("Branches")
                    {
                        DataSource = networkMapLayer.ChannelLayer.DataSource,
                        LabelColumn = "Name"
                    };
                    mapControl.Map.Layers.Insert(0, branchLabelLayer);


                    // show all controls
                    var toolsListBox = new ListBox { DataSource = mapControl.Tools, DisplayMember = "Name"};
                    toolsListBox.SelectedIndexChanged += delegate { mapControl.ActivateTool((IMapTool) toolsListBox.SelectedItem); };

                    WindowsFormsTestHelper.ShowModal(new List<Control> { toolsListBox, mapControl }, true);
                }
         */
        [Test]
        public void EvaluateWithLinearInterpolation()
        {
            // create network
            var network = new Network();

            var node1 = new Node("node1");
            var node2 = new Node("node2");
            network.Nodes.Add(node1);
            network.Nodes.Add(node2);

            var branch1 = new Branch("branch1", node1, node2, 100.0) { Geometry = GeometryFromWKT.Parse("LINESTRING (0 0, 100 0)") };
            network.Branches.Add(branch1);

            // create network coverage
            INetworkCoverage networkCoverage = new NetworkCoverage { Network = network };
            networkCoverage.Locations.InterpolationType = InterpolationType.Linear;

            networkCoverage[new NetworkLocation(network.Branches[0], 0.0)] = 0.0;
            networkCoverage[new NetworkLocation(network.Branches[0], 100.0)] = 10.0;

            // evaluate
            var value = networkCoverage.Evaluate<double>(50.0, 0.0);

            value.Should().Be.EqualTo(5); // linear interpolation
        }

        [Test]
        public void EvaluateWithConstantInterpolation()
        {
            // create network
            var network = new Network();

            var node1 = new Node("node1");
            var node2 = new Node("node2");
            network.Nodes.Add(node1);
            network.Nodes.Add(node2);

            var branch1 = new Branch("branch1", node1, node2, 100.0) { Geometry = GeometryFromWKT.Parse("LINESTRING (0 0, 100 0)") };
            network.Branches.Add(branch1);

            // create network coverage
            INetworkCoverage networkCoverage = new NetworkCoverage { Network = network };
            networkCoverage.Locations.InterpolationType = InterpolationType.Constant;

            networkCoverage[new NetworkLocation(network.Branches[0], 0.0)] = 0.0;
            networkCoverage[new NetworkLocation(network.Branches[0], 100.0)] = 10.0;

            // evaluate
            var value = networkCoverage.Evaluate<double>(60.0, 0.0);

            value.Should().Be.EqualTo(10.0); // constant interpolation
        }

        [Test]
        public void EvaluateCoordinateOnTimeFilteredCoverage()
        {
            // create network
            var network = new Network();

            var node1 = new Node("node1");
            var node2 = new Node("node2");
            network.Nodes.Add(node1);
            network.Nodes.Add(node2);

            var branch1 = new Branch("branch1", node1, node2, 100.0) { Geometry = GeometryFromWKT.Parse("LINESTRING (0 0, 100 0)") };
            network.Branches.Add(branch1);

            // create network coverage
            INetworkCoverage networkCoverage = new NetworkCoverage("",true) { Network = network };

            var time0 = new DateTime(2000);
            networkCoverage[time0, new NetworkLocation(network.Branches[0], 0.0)] = 5.0;
            networkCoverage[time0, new NetworkLocation(network.Branches[0], 100.0)] = 10.0;
            networkCoverage[new DateTime(2001), new NetworkLocation(network.Branches[0], 0.0)] = 20.0;
            networkCoverage[new DateTime(2001), new NetworkLocation(network.Branches[0], 100.0)] = 30.0;

            networkCoverage = (INetworkCoverage) networkCoverage.FilterTime(time0);

            // evaluate
            var value = networkCoverage.Evaluate(new Coordinate(1, 0));

            value.Should().Be.EqualTo(5.0*1.01); // constant interpolation
        }

        [Test]
        public void SegmentsForTimeFilteredCoverage()
        {
            var network = CreateNetwork();

            var dateTime = DateTime.Now;

            var networkCoverage = new NetworkCoverage("test", true) { Network = network };
            // test for defaultvalue

            // set values for only one t.
            INetworkLocation nl11 = new NetworkLocation(network.Branches[0], 0.0);
            INetworkLocation nl12 = new NetworkLocation(network.Branches[0], 100.0);
            INetworkLocation nl13 = new NetworkLocation(network.Branches[1], 100.0);
            networkCoverage[dateTime, nl11] = 0.1;
            networkCoverage[dateTime, nl12] = 0.2;
            networkCoverage[dateTime, nl13] = 0.3;

            //action! filter on t1
            var filtered = (INetworkCoverage)networkCoverage.FilterTime(dateTime);

            //segments should not be affected
            Assert.AreEqual(networkCoverage.Segments.Values.Count, filtered.Segments.Values.Count);
        }

        [Test]
        public void GeometryForCoverage()
        {
            var network = RouteHelperTest.GetSnakeNetwork(false, new Point(0, 0), new Point(100, 0), new Point(100, 200));


            var networkCoverage = new NetworkCoverage { Network = network };
            // test for defaultvalue

            // set values for only one t.
            INetworkLocation networkLocation1 = new NetworkLocation(network.Branches[0], 50.0);
            INetworkLocation networkLocation2 = new NetworkLocation(network.Branches[1], 50.0);

            networkCoverage[networkLocation1] = 0.1;
            networkCoverage[networkLocation2] = 0.2;


            //envelope is based on network locations now
            Assert.AreEqual(new GeometryCollection(new[] { networkLocation1.Geometry, networkLocation2.Geometry }), networkCoverage.Geometry);
        }

        [Test]
        public void GeometryForTimeFilteredCoverage()
        {
            var network = RouteHelperTest.GetSnakeNetwork(false, new Point(0, 0), new Point(100, 0), new Point(100, 200));

            var dateTime = DateTime.Now;

            var networkCoverage = new NetworkCoverage("test", true) { Network = network };
            // test for defaultvalue

            // set values for only one t.
            INetworkLocation nl11 = new NetworkLocation(network.Branches[0], 0.0);
            INetworkLocation nl12 = new NetworkLocation(network.Branches[0], 100.0);
            INetworkLocation nl13 = new NetworkLocation(network.Branches[1], 100.0);
            networkCoverage[dateTime, nl11] = 0.1;
            networkCoverage[dateTime, nl12] = 0.2;
            networkCoverage[dateTime, nl13] = 0.3;

            //action! filter on t1
            var filtered = networkCoverage.FilterTime(dateTime);

            //segments should not be affected
            Assert.AreEqual(networkCoverage.Geometry.EnvelopeInternal, filtered.Geometry.EnvelopeInternal);
        }

        [Test]
        public void AllTimeValuesForFilteredCoverage()
        {
            var network = RouteHelperTest.GetSnakeNetwork(false, new Point(0, 0), new Point(100, 0), new Point(100, 200));
            var networkCoverage = new NetworkCoverage("test", true) { Network = network };
            // test for defaultvalue

            //set values for 2 times
            var times = new[] { 1, 2 }.Select(i => new DateTime(2000, 1, i)).ToList();
            foreach (var time in times)
            {
                INetworkLocation nl11 = new NetworkLocation(network.Branches[0], 0.0);
                networkCoverage[time, nl11] = 0.3;
            }

            var filteredCoverage = networkCoverage.FilterTime(times[0]);

            Assert.AreEqual(times, filteredCoverage.Time.AllValues);
        }
        [Test]
        public void DoesNotBubbleEventsFromNetwork()
        {
            var network = RouteHelperTest.GetSnakeNetwork(false, new Point(0, 0), new Point(100, 0), new Point(100, 200));
            var networkCoverage = new NetworkCoverage("test", true) { Network = network };
            (networkCoverage as INotifyPropertyChanged).PropertyChanged += (s, e) =>
                                                                               {
                                                                                   Assert.Fail("No Bubbling please!");
                                                                               };

            network.Nodes[0].Name = "kees";
        }

        [Test]
        public void NetworkCoverageLocationsAreUpdatedWhenBranchGeometryChanges()
        {
            var network = RouteHelperTest.GetSnakeNetwork(false, new Point(0, 0), new Point(100, 0));
            var networkCoverage = new NetworkCoverage { Network = network };

            //define a point halfway the branch
            IBranch firstBranch = network.Branches[0];
            networkCoverage[new NetworkLocation(firstBranch, 50)] = 2.0;

            //make sure we are 'initialized' (CODE SMELL SNIFF UGH)
            Assert.AreEqual(1, networkCoverage.Locations.Values.Count);
            //change the branch geometry
            firstBranch.Length = 200;
            firstBranch.Geometry = new LineString(new[]
                                                      {
                                                          new Coordinate(0, 0),
                                                          new Coordinate(200, 0)
                                                      });

            //assert the coverage scaled along..so the point should now be at 100
            Assert.AreEqual(100, networkCoverage.Locations.Values[0].Offset);
        }

        [Test]
        public void NetworkCoverageUpdatesWhenBranchIsDeleted()
        {
            //network 2 branches
            var network = RouteHelperTest.GetSnakeNetwork(false, new Point(0, 0), new Point(100, 0),
                                                               new Point(100, 100));
            var networkCoverage = new NetworkCoverage { Network = network };

            //set values on the second branch
            IBranch secondBranch = network.Branches[1];
            networkCoverage[new NetworkLocation(secondBranch, 1)] = 10.0;

            //remove the branch
            network.Branches.Remove(secondBranch);

            //check the coverage updated
            Assert.AreEqual(0, networkCoverage.Locations.Values.Count);
        }

        [Test]
        public void CloneNetworkCoverageWithTwoComponents()
        {
            var network = RouteHelperTest.GetSnakeNetwork(false, new Point(0, 0), new Point(100, 0), new Point(100, 100));
            var f = new NetworkCoverage { Network = network };
            f.Components.Add(new Variable<string>("s"));
            var firstBranch = network.Branches[0];
            f[new NetworkLocation(firstBranch, 50)] = new object[] { 2.0, "test" };

            var fClone = f.Clone();
            Assert.AreEqual(f.Components[0].Values[0], ((Function)fClone).Components[0].Values[0]);
            Assert.AreEqual(f.Components[1].Values[0], ((Function)fClone).Components[1].Values[0]);
        }

        [Test]
        public void EvaluateWithinBranchPerformsProperInterAndExtrapolation()
        {
            //network 2 branches
            var network = RouteHelperTest.GetSnakeNetwork(false, new Point(0, 0), new Point(100, 0),
                                                               new Point(100, 100));
            var networkCoverage = new NetworkCoverage { Network = network };

            IBranch branch = network.Branches[0];

            networkCoverage[new NetworkLocation(branch, 1)] = 1.0;
            networkCoverage[new NetworkLocation(branch, 2)] = 2.0;
            networkCoverage[new NetworkLocation(branch, 3)] = 3.0;

            var location0 = new NetworkLocation(branch, 0);
            var location1 = new NetworkLocation(branch, 1.4);
            var location2 = new NetworkLocation(branch, 2.4);
            var location3 = new NetworkLocation(branch, 3.1);

            var locations = networkCoverage.GetLocationsForBranch(branch);
            locations.Insert(0, location0);
            locations.Insert(2, location1);
            locations.Insert(4, location2);
            locations.Insert(6, location3);

            IList<double> results;

            networkCoverage.Locations.ExtrapolationType = ExtrapolationType.Constant;
            networkCoverage.Locations.InterpolationType = InterpolationType.Linear;
            results = networkCoverage.EvaluateWithinBranch(locations);
            results[0].Should("1").Be.EqualTo(1.0);
            results[2].Should("2").Be.EqualTo(1.4);
            results[4].Should("3").Be.EqualTo(2.4);
            results[6].Should("4").Be.EqualTo(3.0);

            /*
             * Linear extrapolation is not support until the 'normal' evaluate does this also to prevent differences
                        networkCoverage.Locations.ExtrapolationType = ExtrapolationType.Linear;
                        networkCoverage.Locations.InterpolationType = InterpolationType.Constant;
                        results = networkCoverage.EvaluateWithinBranch(locations);
                        results[0].Should("5").Be.EqualTo(0.0);
                        results[2].Should("6").Be.EqualTo(1.0);
                        results[4].Should("7").Be.EqualTo(2.0);
                        results[6].Should("8").Be.EqualTo(3.1);
            */
        }

        [Test]
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "Evaluation failed : Currently only constant Extrapolation for locations is supported.")]
        public void EvaluateWithinBranchThrowsExceptionForNonConstantExtraPolation()
        {
            var network = RouteHelperTest.GetSnakeNetwork(false, 1);
            var networkCoverage = new NetworkCoverage { Network = network };

            IBranch branch = network.Branches[0];

            networkCoverage[new NetworkLocation(branch, 10)] = 1.0;
            networkCoverage[new NetworkLocation(branch, 20)] = 2.0;
            networkCoverage[new NetworkLocation(branch, 30)] = 3.0;
            networkCoverage.Locations.ExtrapolationType = ExtrapolationType.Linear;

            networkCoverage.EvaluateWithinBranch(new[] { new NetworkLocation(branch, 5) });

        }

        [Test]
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "Evaluation failed : currently only constant Extrapolation for locations is supported.")]
        public void EvaluateThrowsExceptionWhenEvaluatingWithNonConstantExtrapolation()
        {
            //network 1 branch
            var network = RouteHelperTest.GetSnakeNetwork(false, 1);

            var networkCoverage = new NetworkCoverage { Network = network };

            //set values on the branch
            IBranch firstBranch = network.Branches[0];
            networkCoverage[new NetworkLocation(firstBranch, 10)] = 10.0;
            networkCoverage[new NetworkLocation(firstBranch, 20)] = 10.0;

            //evaluate before the first location (extrapolation)
            networkCoverage.Locations.ExtrapolationType = ExtrapolationType.Linear;
            networkCoverage.Evaluate(new NetworkLocation(firstBranch, 5));


        }

        [Test]
        public void ClearNetworkOnPropertyChangedShouldBeOK()
        {
            //relates to issue 5019 where flow model sets network to null after which NPC is handled in the coverage. 
            var network = RouteHelperTest.GetSnakeNetwork(false, 1);

            INetworkCoverage networkCoverage = null;//= new NetworkCoverage { Network = network };

            //be sure to subscribe before the coverage subscribes..otherwise the problem does not emerge
            ((INotifyPropertyChanged)network).PropertyChanged += (s, e) =>
                                                                      {
                                                                          //when setting network to null subscribtion is removed for the NEXT change
                                                                          networkCoverage.Network = null;
                                                                      };
            networkCoverage = new NetworkCoverage { Network = network };
            network.BeginEdit("Delete");
        }

        [Test]
        [NUnit.Framework.Category(TestCategory.Integration)]
        public void SplittingABranchMovesExistingPoints()
        {
            //unfortunately this test cannot be defined near networkcoverage because the splitting functionality is defined in HydroNH iso NH
            var network = RouteHelperTest.GetSnakeNetwork(false, new Point(0, 0), new Point(0, 100));
            var channelToSplit = network.Branches.First();
            var networkCoverage = new NetworkCoverage { Network = network };
            //set values at 0..10...90 etc
            networkCoverage.Locations.AddValues(Enumerable.Range(0, 10).Select(i => new NetworkLocation(channelToSplit, i * 10)));

            NetworkHelper.SplitBranchAtNode(channelToSplit, 50);

            //points 0..10.20.30.40 are on branch 1
            //points 0..10.20.30.40 are on branch 2
            Assert.AreEqual(new[] { 0, 10, 20, 30, 40, 50, 0, 10, 20, 30, 40 }, networkCoverage.Locations.Values.Select(l => l.Offset).ToList());
            //take 5 ;)
            var newChannel = network.Branches.ElementAt(1);
            Assert.IsTrue(networkCoverage.Locations.Values.Take(6).All(l => l.Branch == channelToSplit));
            Assert.IsTrue(networkCoverage.Locations.Values.Skip(6).Take(5).All(l => l.Branch == newChannel));
        }

        [Test]
        [NUnit.Framework.Category(TestCategory.Integration)]
        public void SplittingABranchAddInterpolatedValueAtSplitPoint()
        {
            //unfortunately this test cannot be defined near networkcoverage because the splitting functionality is defined in HydroNH iso NH
            var network = RouteHelperTest.GetSnakeNetwork(false, new Point(0, 0), new Point(0, 100));
            var channelToSplit = network.Branches.First();
            var networkCoverage = new NetworkCoverage { Network = network };
            
            //from 1000 to 0
            networkCoverage[new NetworkLocation(channelToSplit, 0.0)] = new[] {1000.0};
            networkCoverage[new NetworkLocation(channelToSplit, 100.0)] = new[] {0.0};

            //split at 50 should add two points
            var newBranch = NetworkHelper.SplitBranchAtNode(channelToSplit, 50.0).NewBranch;

            //one at the end of the original
            Assert.AreEqual(500.0, networkCoverage[new NetworkLocation(channelToSplit, 50.0)]);
            
            
            //one at start of new
            Assert.AreEqual(500.0, networkCoverage[new NetworkLocation(newBranch, 0.0)]);
            
        }

        [Test]
        public void SplittingABranchWithNoValuesDoesNotAddPoints()
        {
            var network = RouteHelperTest.GetSnakeNetwork(false, new Point(0, 0), new Point(0, 100));
            var channelToSplit = network.Branches.First();
            var networkCoverage = new NetworkCoverage { Network = network };

            NetworkHelper.SplitBranchAtNode(channelToSplit, 50.0);

            //still there should be no values
            Assert.AreEqual(0, networkCoverage.Locations.Values.Count);
        }

        [Test]
        public void ChangeBranchLengthAfterSplitOfBranchWithNoLocationsDoesNotThrowException()
        {
            var network = RouteHelperTest.GetSnakeNetwork(false, new Point(0, 0), new Point(0, 100));
            var channelToSplit = network.Branches.First();
            
            //do not remove network coverage..the test is that NO exception occurs when the branch is split
            var networkCoverage = new NetworkCoverage { Network = network };

            var result = NetworkHelper.SplitBranchAtNode(channelToSplit, 50.0);
            var newChannel = result.NewBranch;

            newChannel.Geometry = new LineString(new[] { new Coordinate(-10, 0), new Coordinate(50, 0) });

        }

        [Test]
        public void MergingABranchesRemovesDataOnAffectedBranches()
        {
            //L-shaped network
            var network = RouteHelperTest.GetSnakeNetwork(false,new Point(0, 0), new Point(0, 100),
                                                                  new Point(100, 100));
            var networkCoverage = new NetworkCoverage { Network = network };

            //add locations on both branches
            var networkLocation = new NetworkLocation(network.Branches[0], 50);
            var networkLocation2 = new NetworkLocation(network.Branches[1], 50);
            networkCoverage[networkLocation] = 1.0d;
            networkCoverage[networkLocation2] = 21.0d;

            //merge the branches/remove the node
            NetworkHelper.MergeNodeBranches(network.Nodes[1], network);

            //check the values got removed
            Assert.AreEqual(0, networkCoverage.Components[0].Values.Count);
        }

        [Test]
        public void SplittingABranchShouldGiveSameValuesAtIntermediatePoints()
        {
            var network = RouteHelperTest.GetSnakeNetwork(false, new Point(0, 0), new Point(0, 100));
                                                           
            var networkCoverage = new NetworkCoverage { Network = network };
            networkCoverage.Components[0].InterpolationType = InterpolationType.Linear;
            networkCoverage.Components[0].ExtrapolationType = ExtrapolationType.Constant;

            //add locations
            var networkLocation1 = new NetworkLocation(network.Branches[0], 50);
            var networkLocation2 = new NetworkLocation(network.Branches[0], 100);
            networkCoverage[networkLocation1] = 1.0d;
            networkCoverage[networkLocation2] = 30.0d;

            double beforeLeft = networkCoverage.Evaluate(new NetworkLocation(network.Branches[0], 25));
            double beforeRight = networkCoverage.Evaluate(new NetworkLocation(network.Branches[0], 75));

            //merge the branches/remove the node
            NetworkHelper.SplitBranchAtNode(network.Branches[0], 60);

            double afterLeft = networkCoverage.Evaluate(new NetworkLocation(network.Branches[0], 25));
            double afterRight = networkCoverage.Evaluate(new NetworkLocation(network.Branches[1], 15));

            Assert.AreEqual(beforeLeft, afterLeft);
            Assert.AreEqual(beforeRight, afterRight);
        }

        [Test]
        public void FireNetworkCollectionChangedWhenNetworkBranchesCollectionChanges()
        {
            var network = RouteHelperTest.GetSnakeNetwork(false, new Point(0, 0), new Point(0, 100),
                                                                  new Point(100, 100));
            var networkCoverage = new NetworkCoverage { Network = network };
            
            int callCount = 0;
            networkCoverage.NetworkCollectionChanged += (s, e) => { callCount++; };

            network.Branches.Add(new Branch());

            Assert.AreEqual(1,callCount);
        }
    }

}
