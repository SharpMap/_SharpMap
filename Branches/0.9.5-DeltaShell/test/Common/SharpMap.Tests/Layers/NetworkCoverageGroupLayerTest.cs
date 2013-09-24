using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using DelftTools.Functions.Generic;
using DelftTools.TestUtils;
using GeoAPI.Extensions.Coverages;
using NetTopologySuite.Extensions.Coverages;
using NUnit.Framework;
using Rhino.Mocks;
using SharpMap.Data.Providers;
using SharpMap.Layers;
using SharpMap.Rendering.Thematics;
using SharpMapTestUtils;

namespace SharpMap.Tests.Layers
{
    [TestFixture]
    public class NetworkCoverageGroupLayerTest
    {
        readonly MockRepository mocks = new MockRepository();
        
        [Test]
        [NUnit.Framework.Category(TestCategory.WindowsForms)]
        public void ShowNetworkCoverageAsLayer()
        {
            // create network
            var network = MapTestHelper.CreateMockNetwork();

            mocks.ReplayAll();

            var networkCoverage = new NetworkCoverage { Network = network };
            var networkCoverageLayer = new NetworkCoverageGroupLayer {NetworkCoverage = networkCoverage};

            // set values
            var branch1 = network.Branches[0];
            var branch2 = network.Branches[1];
            networkCoverage[new NetworkLocation(branch1, 4.0)] = 0.1;
            networkCoverage[new NetworkLocation(branch1, 16.0)] = 0.2;
            networkCoverage[new NetworkLocation(branch2, 4.0)] = 0.3;
            networkCoverage[new NetworkLocation(branch2, 12.0)] = 0.4;
            networkCoverage[new NetworkLocation(branch2, 16.0)] = 0.5;

            var map = new Map(new Size(1000, 1000));
            map.Layers.Add(networkCoverageLayer);

            MapTestHelper.ShowModal(map);
        }

        [Test]
        [NUnit.Framework.Category(TestCategory.WindowsForms)]
        public void ShowNetworkCoverageAsLayerWithNoneSegmentationType()
        {
            // create network
            var network = MapTestHelper.CreateMockNetwork();

            mocks.ReplayAll();

            var networkCoverage = new NetworkCoverage { Network = network, SegmentGenerationMethod = SegmentGenerationMethod.None };
            var networkCoverageLayer = new NetworkCoverageGroupLayer { NetworkCoverage = networkCoverage };

            // set values
            var branch1 = network.Branches[0];
            var branch2 = network.Branches[1];
            networkCoverage[new NetworkLocation(branch1, 4.0)] = 0.1;
            networkCoverage[new NetworkLocation(branch1, 16.0)] = 0.2;
            networkCoverage[new NetworkLocation(branch2, 4.0)] = 0.3;
            networkCoverage[new NetworkLocation(branch2, 12.0)] = 0.4;
            networkCoverage[new NetworkLocation(branch2, 16.0)] = 0.5;

            var map = new Map(new Size(1000, 1000));
            map.Layers.Add(networkCoverageLayer);

            MapTestHelper.ShowModal(map);
        }

        [Test]
        [NUnit.Framework.Category(TestCategory.WindowsForms)]
        public void ShowNetworkCoverageAsRoute()
        {
            // create network
            var network = MapTestHelper.CreateMockNetwork();

            mocks.ReplayAll();

            var networkCoverage = new NetworkCoverage { Network = network, SegmentGenerationMethod = SegmentGenerationMethod.RouteBetweenLocations };

            var networkCoverageLayer = new NetworkCoverageGroupLayer {NetworkCoverage = networkCoverage};

            NetworkCoverageGroupLayer.SetupRouteLayerTheme(networkCoverageLayer, null);

            // set values
            var branch1 = network.Branches[0];
            var branch2 = network.Branches[1];
            networkCoverage[new NetworkLocation(branch1, 4.0)] = 0.1;
            networkCoverage[new NetworkLocation(branch1, 16.0)] = 0.2;
            networkCoverage[new NetworkLocation(branch2, 4.0)] = 0.3;
            networkCoverage[new NetworkLocation(branch2, 12.0)] = 0.4;
            networkCoverage[new NetworkLocation(branch2, 16.0)] = 0.5;

            var map = new Map(new Size(1000, 1000));
            map.Layers.Add(networkCoverageLayer);

            // add branch layer
            var branchLayer = new VectorLayer {DataSource = new FeatureCollection{Features = (IList) network.Branches} };
            branchLayer.Style.Outline.Width = 25;
            branchLayer.Style.Outline.Color = Pens.LightGray.Color;
            map.Layers.Add(branchLayer);

            MapTestHelper.ShowModal(map);
        }
        
        [Test]
        [NUnit.Framework.Category(TestCategory.WindowsForms)]
        public void ShowNetworkCoverageAsLayerWithSegmentBetweenLocationsType()
        {
            // create network
            var network = MapTestHelper.CreateMockNetwork();

            mocks.ReplayAll();

            var networkCoverage = new NetworkCoverage
                                      {
                                          Network = network, 
                                          SegmentGenerationMethod = SegmentGenerationMethod.SegmentBetweenLocations
                                      };
            var networkCoverageLayer = new NetworkCoverageGroupLayer { NetworkCoverage = networkCoverage };
            
            // set values
            var branch1 = network.Branches[0];
            var branch2 = network.Branches[1];
            networkCoverage[new NetworkLocation(branch1, 4.0)] = 0.1;
            networkCoverage[new NetworkLocation(branch1, 16.0)] = 0.2;
            networkCoverage[new NetworkLocation(branch1, 100.0)] = 0.4;

            networkCoverage[new NetworkLocation(branch2, 0.0)] = 0.4;
            networkCoverage[new NetworkLocation(branch2, 4.0)] = 0.5;
            networkCoverage[new NetworkLocation(branch2, 12.0)] = 0.6;
            networkCoverage[new NetworkLocation(branch2, 16.0)] = 0.7;
            networkCoverage[new NetworkLocation(branch2, 100.0)] = 0.8;

            networkCoverage.Locations.InterpolationType = InterpolationType.Constant;
            networkCoverageLayer.SegmentLayer.LabelLayer.LabelColumn = "SegmentNumber";
            networkCoverageLayer.SegmentLayer.LabelLayer.Visible = true;

            var map = new Map(new Size(1000, 1000));
            map.Layers.Add(networkCoverageLayer);

            MapTestHelper.ShowModal(map);
        }

        [Test]
        [NUnit.Framework.Category(TestCategory.WindowsForms)]
        public void ShowNetworkCoverageAsLayerWithSegmentBetweenLocationsFullyCoveredType()
        {
            // create network
            var network = MapTestHelper.CreateMockNetwork();

            mocks.ReplayAll();

            var networkCoverage = new NetworkCoverage
            {
                Network = network,
                IsTimeDependent = true,
                SegmentGenerationMethod = SegmentGenerationMethod.SegmentBetweenLocationsFullyCovered
            };
            var networkCoverageLayer = new NetworkCoverageGroupLayer { NetworkCoverage = networkCoverage };
            var time1 = new DateTime(2010,1,1);

            // set values
            var branch1 = network.Branches[0];
            var branch2 = network.Branches[1];
            networkCoverage[time1, new NetworkLocation(branch1, 0.0)] = 0.1;
            networkCoverage[time1, new NetworkLocation(branch1, 10.0)] = 0.2;
            networkCoverage[time1, new NetworkLocation(branch1, 50.0)] = 0.3;
            networkCoverage[time1, new NetworkLocation(branch1, 100.0)] = 0.4;

            // no coverage point for branch2 offset 0
            // the fully covered option will look for the begin node point
            // on other branches (branch 1 offset 100 == node2 == branch2 offset 0)

            networkCoverage[time1, new NetworkLocation(branch2, 20.0)] = 0.5;
            networkCoverage[time1, new NetworkLocation(branch2, 40.0)] = 0.6;
            networkCoverage[time1, new NetworkLocation(branch2, 60.0)] = 0.7;
            networkCoverage[time1, new NetworkLocation(branch2, 100.0)] = 0.8;

            var time2 = new DateTime(2010, 1, 2);

            foreach (var location in networkCoverage.Locations.Values)
            {
                networkCoverage[time2, location] = (double) networkCoverage[time1, location]*2;
            }

            networkCoverage.Locations.InterpolationType = InterpolationType.Constant;
            networkCoverageLayer.SegmentLayer.LabelLayer.LabelColumn = "SegmentNumber";
            networkCoverageLayer.SegmentLayer.LabelLayer.Visible = true;

            var map = new Map(new Size(1000, 1000));
            map.Layers.Add(networkCoverageLayer);

            networkCoverageLayer.SetCurrentTimeSelection(time2, time2);
            MapTestHelper.ShowModal(map);
        }

        [Test]
        public void AutoUpdateGradientThemeOnValuesChanged()
        {
            NetworkCoverage networkCoverage = GetNetworkCoverage();

            //create a layer
            var networkCoverageLayer = new NetworkCoverageGroupLayer { NetworkCoverage = networkCoverage };

            //get the location layer
            var networkCoverageLocationLayer = networkCoverageLayer.Layers[0];

            //assert the theme has a default min/max
            Assert.AreEqual(0.5,((GradientTheme) networkCoverageLocationLayer.Theme).Max);
            
            //change a value in the coverage
            networkCoverage[networkCoverage.Locations.Values[0]] = 10.0;

            //check the theme on the location layer got updated
            var currentTheme = (GradientTheme)networkCoverageLocationLayer.Theme; ;
            Assert.AreEqual(10, currentTheme.Max);
            
        }
        [Test]
        public void UpdateLayerNameWhenCoverageNameChanges()
        {
            NetworkCoverage networkCoverage = GetNetworkCoverage();
            networkCoverage.Name = "kees";
            
            //create a layer
            var networkCoverageLayer = new NetworkCoverageGroupLayer { NetworkCoverage = networkCoverage };
            Assert.AreEqual("kees",networkCoverageLayer.Name);

            int called = 0;
            ((INotifyPropertyChanged)networkCoverageLayer).PropertyChanged += (s, e) => called++;
            //change the coverage name
            networkCoverage.Name = "jan";

            Assert.AreEqual(1, called);
            Assert.AreEqual("jan", networkCoverageLayer.Name);
        }

        [Test]
        public void UpdateThemeAttributeNameWhenLayerNameChanges()
        {
            NetworkCoverage networkCoverage = GetNetworkCoverage();
            networkCoverage.Components[0].Name = "kees";

            //create a layer
            var networkCoverageLayer = new NetworkCoverageGroupLayer { NetworkCoverage = networkCoverage };
            Assert.AreEqual("kees", networkCoverageLayer.LocationLayer.Theme.AttributeName);

            //change the component name
            
            networkCoverage.Components[0].Name = "jan";
            Assert.AreEqual("jan", networkCoverageLayer.LocationLayer.Theme.AttributeName);
        }

        [Test]
        public void LayerCollectionIsReadOnly()
        {
            //create a layer with a coverate
            NetworkCoverage networkCoverage = GetNetworkCoverage();
            networkCoverage.Components[0].Name = "kees";

            var networkCoverageLayer = new NetworkCoverageGroupLayer {NetworkCoverage = networkCoverage};
            Assert.IsTrue(networkCoverageLayer.HasReadOnlyLayersCollection);

            Assert.IsTrue(networkCoverageLayer.LocationLayer.ShowInLegend);
            Assert.IsFalse(networkCoverageLayer.SegmentLayer.ShowInLegend);
        }

        [Test]
        [NUnit.Framework.Category(TestCategory.WindowsForms)]
        public void NetworkCoverageWithNaNValuesDoesNotCrashTools9794()
        {
            //create a layer with a coverate
            NetworkCoverage networkCoverage = GetNetworkCoverage();
            networkCoverage[new NetworkLocation(networkCoverage.Network.Branches[1], 17.0)] = double.NaN;
            networkCoverage.Components[0].Name = "kees";

            var networkCoverageLayer = new NetworkCoverageGroupLayer { NetworkCoverage = networkCoverage };
            Assert.IsTrue(networkCoverageLayer.HasReadOnlyLayersCollection);

            var map = new Map(new Size(1000, 1000));
            map.Layers.Add(networkCoverageLayer);
            MapTestHelper.ShowModal(map);
        }

        [Test]
        [NUnit.Framework.Category(TestCategory.WindowsForms)]
        public void NetworkCoverageWithInfiniteValuesDoesNotCrashTools9794()
        {
            //create a layer with a coverate
            NetworkCoverage networkCoverage = GetNetworkCoverage();
            networkCoverage[new NetworkLocation(networkCoverage.Network.Branches[1], 17.0)] = double.NegativeInfinity;
            networkCoverage[new NetworkLocation(networkCoverage.Network.Branches[1], 18.0)] = double.PositiveInfinity;
            networkCoverage.Components[0].Name = "kees";

            var networkCoverageLayer = new NetworkCoverageGroupLayer {NetworkCoverage = networkCoverage};
            Assert.IsTrue(networkCoverageLayer.HasReadOnlyLayersCollection);

            var map = new Map(new Size(1000, 1000));
            map.Layers.Add(networkCoverageLayer);
            MapTestHelper.ShowModal(map);
        }

        [Test]
        public void LayerSetsNoDataValuesForChildren()
        {
            var coverage = GetNetworkCoverage();
            coverage.Components[0].NoDataValues.Add(1.2);
            coverage.Components[0].NoDataValues.Add(2.4);

            var networkCoverageGroupLayer = new NetworkCoverageGroupLayer
                {
                    Coverage = coverage
                };
            Assert.IsNull(networkCoverageGroupLayer.Theme, "Theme null, so no 'NoDataValues' on parent.");
            var locationLayerNoDataValues = ((Theme) networkCoverageGroupLayer.LocationLayer.Theme).NoDataValues;
            Assert.AreEqual(2, locationLayerNoDataValues.Count);
            Assert.AreEqual(1.2, locationLayerNoDataValues[0]);
            Assert.AreEqual(2.4, locationLayerNoDataValues[1]);

            var segmentLayerNoDataValues = ((Theme)networkCoverageGroupLayer.LocationLayer.Theme).NoDataValues;
            Assert.AreEqual(2, segmentLayerNoDataValues.Count);
            Assert.AreEqual(1.2, segmentLayerNoDataValues[0]);
            Assert.AreEqual(2.4, segmentLayerNoDataValues[1]);
        }

        [Test]
        public void CloneTest()
        {
            var networkCoverage = GetNetworkCoverage();
            var networkCoverageLayer = new NetworkCoverageGroupLayer { NetworkCoverage = networkCoverage };

            var clone = (NetworkCoverageGroupLayer) networkCoverageLayer.Clone();
        }

        private static NetworkCoverage GetNetworkCoverage()
        {
            var network = MapTestHelper.CreateMockNetwork();
            var networkCoverage = new NetworkCoverage { Network = network };
        
            // set values
            var branch1 = network.Branches[0];
            var branch2 = network.Branches[1];
            networkCoverage[new NetworkLocation(branch1, 4.0)] = 0.1;
            networkCoverage[new NetworkLocation(branch1, 16.0)] = 0.2;
            networkCoverage[new NetworkLocation(branch2, 4.0)] = 0.3;
            networkCoverage[new NetworkLocation(branch2, 12.0)] = 0.4;
            networkCoverage[new NetworkLocation(branch2, 16.0)] = 0.5;
            return networkCoverage;
        }
    }
}