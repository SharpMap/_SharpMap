using NetTopologySuite.Extensions.Coverages;
using NUnit.Framework;
using SharpMap.Data.Providers;
using SharpMapTestUtils;

namespace SharpMap.Tests.Data.Providers
{
    [TestFixture]
    public class NetworkCoverageFeatureCollectionTest
    {
        [Test]
        public void CreateFeatureCollectionForNetworkLocationsAndSegments()
        {
            var network = MapTestHelper.CreateMockNetwork();

            var networkCoverage = new NetworkCoverage { Network = network };
            var networkLocationFeatureProvider = new NetworkCoverageFeatureCollection
                                                     {
                                                         NetworkCoverage = networkCoverage,
                                                         NetworkCoverageFeatureType = NetworkCoverageFeatureType.Locations
                                                     };
            var segmentFeatureProvider = new NetworkCoverageFeatureCollection
                                             {
                                                 NetworkCoverage = networkCoverage,
                                                 NetworkCoverageFeatureType = NetworkCoverageFeatureType.Segments
                                             };

            networkCoverage.Locations.Values.Add(new NetworkLocation {Branch = network.Branches[0], Offset = 0.0 });

            Assert.AreEqual(1, networkLocationFeatureProvider.Features.Count);
            Assert.AreEqual(1, segmentFeatureProvider.Features.Count);
        }
    }
}