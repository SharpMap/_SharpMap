using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using DelftTools.Functions.Filters;
using DelftTools.Functions.Generic;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;
using SharpMap.Api;
using SharpMap.Layers;

namespace SharpMap.Rendering
{
    /// <summary>
    /// Base class for NetworkCoverageLocationRenderer and NetworkCoverageSegmentRenderer
    /// </summary>
    public abstract class NetworkCoverageRenderer:IFeatureRenderer
    {
        protected INetworkCoverage GetRenderedCoverage(INetworkCoverage networkCoverage, DateTime time)
        {
            if (networkCoverage.IsTimeDependent)
            {
                //is it already filtered..update the time
                if (networkCoverage.Filters.Any(f => f.Variable is Variable<DateTime>) || networkCoverage.Parent != null)
                {
                    var currentTimeFilter = networkCoverage.Filters
                        .OfType<IVariableValueFilter>()
                        .Where(f => f.Variable is Variable<DateTime>)
                        .FirstOrDefault();

                    //update the time filter and we're done
                    currentTimeFilter.Values[0] = time;
                    return networkCoverage;

                }

                //create a filtered version
                return (INetworkCoverage)networkCoverage.FilterTime(time);
            }
            return networkCoverage;
        }


        public abstract bool Render(IFeature feature, Graphics g, ILayer layer);
        
        public IGeometry GetRenderedFeatureGeometry(IFeature feature, ILayer layer)
        {
            return feature.Geometry;
        }

        public bool UpdateRenderedFeatureGeometry(IFeature feature, ILayer layer)
        {
            throw new NotImplementedException();
        }

        public abstract IEnumerable<IFeature> GetFeatures(IGeometry geometry, ILayer layer);

        public abstract IEnumerable<IFeature> GetFeatures(IEnvelope box, ILayer layer);
    }
}