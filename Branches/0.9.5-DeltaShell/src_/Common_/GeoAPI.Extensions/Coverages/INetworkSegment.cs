using GeoAPI.Extensions.Networks;

namespace GeoAPI.Extensions.Coverages
{
    /// <summary>
    /// This class defines a segment for a specific network location.
    /// </summary>
    public interface INetworkSegment: IBranchFeature
    {
        bool DirectionIsPositive { get; set; }
        /// <summary>
        /// Location on the branch
        /// </summary>
        double EndOffset { get;  }
    }
}