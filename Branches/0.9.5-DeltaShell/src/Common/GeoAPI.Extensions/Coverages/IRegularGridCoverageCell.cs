using GeoAPI.Extensions.Feature;

namespace GeoAPI.Extensions.Coverages
{
    public interface IRegularGridCoverageCell : IFeature
    {
        double X { get; set; }

        double Y { get; set; }

        IRegularGridCoverage RegularGridCoverage { get; set; }
    }
}
