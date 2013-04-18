using GeoAPI.Extensions.Coverages;

namespace SharpMap.Layers
{
    public interface INetworkCoverageLayer : ICoverageLayer
    {
        INetworkCoverage NetworkCoverage { get; set; }

        NetworkCoverageLocationLayer LocationLayer { get;  }
        NetworkCoverageSegmentLayer SegmentLayer { get; }
    }
}
