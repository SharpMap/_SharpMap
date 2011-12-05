using System.Collections;
using System.Drawing;
using DelftTools.Utils.Collections.Generic;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Extensions.Networks;
using NetTopologySuite.Extensions.Coverages;
using NUnit.Framework;
using Rhino.Mocks;
using SharpMap.Converters.WellKnownText;
using SharpMap.Data.Providers;
using SharpMap.Layers;
using SharpMapTestUtils;

namespace SharpMap.Tests.Layers
{
    [TestFixture]
    public class NetworkCoverageLayerTest
    {
        readonly MockRepository mocks = new MockRepository();


        [Test]
        [Category("Windows.Forms")]
        public void ShowNetworkCoverageAsLayer()
        {
            // create network
            var network = MapTestHelper.CreateMockNetwork();

            mocks.ReplayAll();

            var networkCoverage = new NetworkCoverage { Network = network };
            var networkCoverageLayer = new NetworkCoverageLayer {NetworkCoverage = networkCoverage};

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

            MapTestHelper.Show(map);
        }

        [Test]
        [Category("Windows.Forms")]
        public void ShowNetworkCoverageAsLayerWithNoneSegmentationType()
        {
            // create network
            var network = MapTestHelper.CreateMockNetwork();

            mocks.ReplayAll();

            var networkCoverage = new NetworkCoverage { Network = network, SegmentGenerationMethod = SegmentGenerationMethod.None };
            var networkCoverageLayer = new NetworkCoverageLayer { NetworkCoverage = networkCoverage };

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

            MapTestHelper.Show(map);
        }

        [Test]
        [Category("Windows.Forms")]
        public void ShowNetworkCoverageAsRoute()
        {
            // create network
            var network = MapTestHelper.CreateMockNetwork();

            mocks.ReplayAll();

            var networkCoverage = new NetworkCoverage { Network = network, SegmentGenerationMethod = SegmentGenerationMethod.RouteBetweenLocations };

            var networkCoverageLayer = new NetworkCoverageLayer {NetworkCoverage = networkCoverage};

            NetworkCoverageLayerHelper.SetupRouteNetworkCoverageLayerTheme(networkCoverageLayer, null);

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

            MapTestHelper.Show(map);
        }
    }
}