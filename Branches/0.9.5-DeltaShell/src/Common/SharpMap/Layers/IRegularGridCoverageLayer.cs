using GeoAPI.Extensions.Coverages;

namespace SharpMap.Layers
{
    public interface IRegularGridCoverageLayer: ICoverageLayer
    {
        /// <summary>
        /// Every coverage layer provides a coverage. Usually coverage is just a 1st IFeature of the layer's DataSource (IFeatureProvider)
        /// </summary>
        IRegularGridCoverage Grid { get; set; }

        /// <summary>
        /// Coverage to render. Might be time filtered version of Grid
        /// </summary>
        IRegularGridCoverage RenderedCoverage { get; }
    }
}
