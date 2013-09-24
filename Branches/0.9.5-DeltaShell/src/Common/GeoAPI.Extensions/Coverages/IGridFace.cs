using GeoAPI.Extensions.Feature;

namespace GeoAPI.Extensions.Coverages
{
    /// <summary>
    /// Defines face on any discrete grid (regular, curvilinear, etc).
    /// </summary>
    public interface IGridFace : IFeature
    {
        ICoverage Grid { get; }
    }
}