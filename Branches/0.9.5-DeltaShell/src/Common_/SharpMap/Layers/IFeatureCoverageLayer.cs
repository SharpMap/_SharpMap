using GeoAPI.Extensions.Coverages;

namespace SharpMap.Layers
{
    public interface IFeatureCoverageLayer : ICoverageLayer
    {
        IFeatureCoverage FeatureCoverage { get; set; }
    }
}
