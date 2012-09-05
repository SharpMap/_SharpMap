using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DelftTools.Functions.Filters;
using DelftTools.Functions.Generic;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Extensions.Feature;
using GeoAPI.Extensions.Networks;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.LinearReferencing;
using NetTopologySuite.Extensions.Coverages;

namespace SharpMap.Data.Providers
{
    // TODO: remove this class?
    public class NetworkCoverageFeatureCollection : FeatureCollection
    {
        public virtual INetworkCoverage NetworkCoverage { get; set; }

        private INetworkCoverage GetRenderedCoverage(INetworkCoverage networkCoverage)
        {
            if (networkCoverage.IsTimeDependent)
            {
                //use current time or some other time.
                var timeToSet = CurrentTime ?? DateTime.MinValue;
                //is it already filtered..update the time
                if (networkCoverage.Filters.Any(f => f.Variable is Variable<DateTime>) || networkCoverage.Parent != null)
                {
                    var currentTimeFilter = networkCoverage.Filters
                        .OfType<IVariableValueFilter>()
                        .Where(f => f.Variable is Variable<DateTime>)
                        .FirstOrDefault();

                    //update the time filter and we're done
                    currentTimeFilter.Values[0] = timeToSet;
                    return networkCoverage;

                }

                //create a filtered version
                return (INetworkCoverage)networkCoverage.FilterTime(timeToSet);
            }
            return networkCoverage;
        }

        private INetworkCoverage renderedCoverage;
        public virtual INetworkCoverage RenderedCoverage
        {
            get
            {
                if (renderedCoverage == null)
                {
                    renderedCoverage = GetRenderedCoverage(NetworkCoverage);
                }
                else
                {
                    UpdateCoverageTimeFilters(renderedCoverage);
                }
                return renderedCoverage;
            }
        }

        private DateTime? currentTime;

        /// <summary>
        /// Current time of the rendered coverage
        /// </summary>
        public virtual DateTime? CurrentTime
        {
            get { return currentTime; }
            set
            {
                currentTime = value;
            }
        }

        private void UpdateCoverageTimeFilters(ICoverage coverage)
        {
            var currentTimeFilter = coverage.Filters
                        .OfType<IVariableValueFilter>()
                        .Where(f => f.Variable is Variable<DateTime>)
                        .FirstOrDefault();

            //update the time filter and we're done
            if (currentTimeFilter != null)
            {
                currentTimeFilter.Values[0] = CurrentTime; 
            }
        }


        public virtual NetworkCoverageFeatureType NetworkCoverageFeatureType { get; set; }

        public override IList Features
        {
            get
            {
                if (NetworkCoverage == null)
                {
                    //return an empty list...this is what base class expects
                    return new List<IFeature>();
                }

                switch(NetworkCoverageFeatureType)
                {
                    case NetworkCoverageFeatureType.Locations:
                        return NetworkCoverage.Locations.Values;
                    case NetworkCoverageFeatureType.Segments:
                        return NetworkCoverage.Segments.Values;
                }

                throw new InvalidOperationException();
            }

            set { throw new NotImplementedException(); }
        }

        public override Type FeatureType
        {
            get
            {
                switch (NetworkCoverageFeatureType)
                {
                    case NetworkCoverageFeatureType.Locations:
                        return typeof (INetworkLocation);
                    case NetworkCoverageFeatureType.Segments:
                        return typeof (NetworkSegment);
                }

                throw new InvalidOperationException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }
    }
}
