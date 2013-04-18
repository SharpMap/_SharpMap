using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GeoAPI.Extensions.Coverages;

namespace SharpMap.Layers
{
    public interface IRegularGridCoverageLayer: ICoverageLayer
    {
        /// <summary>
        /// Every coverage layer provides a coverage. Usually coverage is just a 1st IFeature of the layer's DataSource (IFeatureProvider)
        /// </summary>
        IRegularGridCoverage Grid { get; set; }
    }
}
