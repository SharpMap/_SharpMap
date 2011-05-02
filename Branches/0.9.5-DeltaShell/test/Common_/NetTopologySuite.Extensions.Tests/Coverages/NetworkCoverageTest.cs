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
using GeoAPI.Extensions.Coverages;
using GeoAPI.Extensions.Networks;
using GisSharpBlog.NetTopologySuite.Geometries;
using log4net;
using log4net.Config;
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
        private static readonly ILog log = LogManager.GetLogger(typeof (NetworkCoverageTest));

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
            for (int i = 0; i < 1000;i++ )
            {
                IEnumerable<double> values = new[] { 1.0, 2.0, 3.0 }.Select(d => d * i);
                DateTime currentTime = startTime.AddMinutes(i);
                //set values for coverage 1 using value filter
                networkCoverage.Time.AddValues(new[] { currentTime });
                var timeValueFilter = new VariableValueFilter<DateTime>(networkCoverage.Time, currentTime);
                networkCoverage.SetValues(values,timeValueFilter);

                //set values for coverage 2 using index filter
                networkCoverage2.Time.AddValues(new[] { currentTime });
                var timeIndexFilter = new VariableIndexRangeFilter(networkCoverage2.Time, i);
                networkCoverage2.SetValues(values, timeIndexFilter);

            }
            
            Assert.AreEqual(networkCoverage.Components[0].Values,networkCoverage2.Components[0].Values);
        }

        [Test]
        public void CreateForExistingNetwork()
        {
            var network = CreateNetwork();

            INetworkCoverage networkCoverage = new NetworkCoverage {Network = network};

            // set values
            networkCoverage[new NetworkLocation(network.Branches[0], 0.0)] = 0.1;
            networkCoverage[new NetworkLocation(network.Branches[0], 100.0)] = 0.2;
            networkCoverage[new NetworkLocation(network.Branches[1], 0.0)] = 0.3;
            networkCoverage[new NetworkLocation(network.Branches[1], 50.0)] = 0.4;
            networkCoverage[new NetworkLocation(network.Branches[1], 200.0)] = 0.5;

            // asserts
            Assert.AreEqual(typeof (double), networkCoverage.Components[0].ValueType);
            Assert.AreEqual(typeof (INetworkLocation), networkCoverage.Arguments[0].ValueType);
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
                var networkLocation = (INetworkLocation) networkLocations[i];
                log.DebugFormat("NetworkCoverage[location = ({0} - {1,6:F})] = {2}", networkLocation.Branch,
                                networkLocation.Offset, values[i]); // ... trying to change formatting
            }
        }

        private static INetwork CreateNetwork()
        {
            return RouteHelperTest.GetSnakeHydroNetwork(false, new Point(0, 0), new Point(100, 0), new Point(300, 0));
        }

    

        [Test]
        public void DefaultValueTest()
        {
            var network = CreateNetwork();

            INetworkCoverage networkCoverage = new NetworkCoverage {Network = network, DefaultValue = 0.33};
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
            networkCoverage.Locations.ExtrapolationType = ApproximationType.Constant;

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

            var networkCoverage = new NetworkCoverage("test", true) {Network = network};
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

            INetworkCoverage networkCoverage = new NetworkCoverage {Network = network};

            networkCoverage[new NetworkLocation(network.Branches[0], 10.0)] = 0.1;
            networkCoverage[new NetworkLocation(network.Branches[0], 90.0)] = 0.9;

            networkCoverage.Locations.ExtrapolationType = ApproximationType.Constant;
            networkCoverage.Locations.InterpolationType = ApproximationType.Linear;
            
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
            INetworkCoverage networkCoverage = new NetworkCoverage {Network = network,IsTimeDependent = true};
            var networkLocation = new NetworkLocation(network.Branches[0], 0);

            for (var i = 1; i < 4; i++)
            {
                networkCoverage[new DateTime(2000, 1, i), networkLocation] = i;
            }

            //filter the function for the networkLocation
            IFunction filteredCoverage = networkCoverage.GetTimeSeries(networkLocation);
                                                    
            Assert.AreEqual(3,filteredCoverage.Components[0].Values.Count);
            Assert.AreEqual(1, filteredCoverage.Arguments.Count);
            Assert.AreEqual(filteredCoverage.Arguments[0].Values,networkCoverage.Time.Values);
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
                networkCoverage[new DateTime(2000, 1, i), networkLocation] = i;
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

            var networkCoverage = new NetworkCoverage {Network = network};
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
        public void GenerateSegmentBetweenLocations()
        {
            var network = CreateNetwork();

            var networkCoverage = new NetworkCoverage { Network = network, SegmentGenerationMethod = SegmentGenerationMethod.RouteBetweenLocations };
            networkCoverage[new NetworkLocation(network.Branches[0], 10.0)] = 0;
            networkCoverage[new NetworkLocation(network.Branches[0], 50.0)] = 50;
            networkCoverage[new NetworkLocation(network.Branches[0], 90.0)] = 90;

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
        [NUnit.Framework.Category("Windows.Forms")]
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

            var networkCoverageLayer = new NetworkCoverageLayer { NetworkCoverage = networkCoverage };

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

            INetworkCoverage clone = (INetworkCoverage) networkCoverage.Clone();
            Assert.AreEqual(5, clone.Locations.Values.Count);
            Assert.AreNotSame(clone.Locations.Values[0],networkCoverage.Locations.Values[0]);
        }

        

/* todo make independent on networkeditorplugin or move it to a good place        [Test]
        [Category("Windows.Forms")]
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
        public void Evaluate()
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
            INetworkCoverage networkCoverage = new NetworkCoverage {Network = network};
            networkCoverage.Locations.InterpolationType = ApproximationType.Linear;

            networkCoverage[new NetworkLocation(network.Branches[0], 0.0)] = 0;
            networkCoverage[new NetworkLocation(network.Branches[0], 100.0)] = 10;

            // evaluate
            var value = networkCoverage.Evaluate<double>(50.0, 0.0);

            value.Should().Be.EqualTo(5); // linear interpolation
        }

        [Test]
        public void SegmentsForTimeFilteredCoverage()
        {
            var network = CreateNetwork();

            var dateTime = DateTime.Now;

            var networkCoverage = new NetworkCoverage("test", true) {Network = network};
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
            Assert.AreEqual(networkCoverage.Segments.Values.Count,filtered.Segments.Values.Count);
        }

        [Test]
        public void GeometryForCoverage()
        {
            var network = RouteHelperTest.GetSnakeHydroNetwork(false, new Point(0, 0), new Point(100, 0), new Point(100, 200));

            
            var networkCoverage = new NetworkCoverage {Network = network};
            // test for defaultvalue

            // set values for only one t.
            INetworkLocation networkLocation1 = new NetworkLocation(network.Branches[0], 50.0);
            INetworkLocation networkLocation2 = new NetworkLocation(network.Branches[1], 50.0);
            
            networkCoverage[networkLocation1] = 0.1;
            networkCoverage[networkLocation2] = 0.2;
            
            
            //envelope is based on network locations now
            Assert.AreEqual(new GeometryCollection(new[]{networkLocation1.Geometry,networkLocation2.Geometry}),networkCoverage.Geometry);
        }

        [Test]
        public void GeometryForTimeFilteredCoverage()
        {
            var network = RouteHelperTest.GetSnakeHydroNetwork(false, new Point(0, 0), new Point(100, 0), new Point(100, 200));

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
            var network = RouteHelperTest.GetSnakeHydroNetwork(false, new Point(0, 0), new Point(100, 0), new Point(100, 200));
            var networkCoverage = new NetworkCoverage("test", true) {Network = network};
            // test for defaultvalue

            //set values for 2 times
            var times = new[] {1, 2}.Select(i => new DateTime(2000, 1, i)).ToList();
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
            var network = RouteHelperTest.GetSnakeHydroNetwork(false, new Point(0, 0), new Point(100, 0), new Point(100, 200));
            var networkCoverage = new NetworkCoverage("test", true) { Network = network };
            (networkCoverage as INotifyPropertyChanged).PropertyChanged += (s,e) =>
                                                                               {
                                                                                   Assert.Fail("No Bubbling please!");
                                                                               };
            
            network.Nodes[0].Name = "kees";
        }

        [Test]
        public void NetworkCoverageLocationsAreUpdatedWhenBranchGeometryChanges()
        {
            var network = RouteHelperTest.GetSnakeHydroNetwork(false, new Point(0, 0), new Point(100, 0));
            var networkCoverage = new NetworkCoverage {Network = network};

            //define a point halfway the branch
            IBranch firstBranch = network.Branches[0];
            networkCoverage[new NetworkLocation(firstBranch, 50)] = 2.0;

            //make sure we are 'initialized' (CODE SMELL SNIFF UGH)
            Assert.AreEqual(1,networkCoverage.Locations.Values.Count);
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

    }
}
