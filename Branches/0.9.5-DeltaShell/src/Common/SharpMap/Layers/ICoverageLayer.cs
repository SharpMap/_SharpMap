using System;
using GeoAPI.Extensions.Coverages;

namespace SharpMap.Layers
{
    public interface ICoverageLayer : ILayer
    {
        /// <summary>
        /// Every coverage layer provides a coverage. Usually coverage is just a 1st IFeature of the layer's DataSource (IFeatureProvider)
        /// </summary>
        ICoverage Coverage { get; set; }
    }
}