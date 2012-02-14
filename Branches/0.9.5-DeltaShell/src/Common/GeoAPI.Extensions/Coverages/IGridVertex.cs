using GeoAPI.Extensions.Feature;

namespace GeoAPI.Extensions.Coverages
{
    /// <summary>
    /// Defines vertex on any discrete grid (regular, curvilinear, etc).
    /// </summary>
    public interface IGridVertex : IFeature
    {
        ICoverage Grid { get; }
    }
}