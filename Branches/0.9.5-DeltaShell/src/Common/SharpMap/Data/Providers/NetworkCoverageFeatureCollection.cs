using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DelftTools.Functions.Filters;
using DelftTools.Functions.Generic;
using DelftTools.Utils.Aop.NotifyPropertyChange;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Extensions.Feature;
using NetTopologySuite.Extensions.Coverages;

namespace SharpMap.Data.Providers
{
    [NotifyPropertyChange]
    public class NetworkCoverageFeatureCollection : FeatureCollection
    {
        private INetworkCoverage networkCoverage;
        public virtual INetworkCoverage NetworkCoverage
        {
            get { return networkCoverage; }
            set { networkCoverage = value; }
        }

        private INetworkCoverage GetRenderedCoverage(INetworkCoverage networkCoverage)
        {
            if (networkCoverage.IsTimeDependent)
            {
                if(TimeSelectionStart == null)
                {
                    SetCurrentTimeSelection(networkCoverage.Time.Values.FirstOrDefault(), null);
                }

                //is it already filtered..update the time
                if (networkCoverage.Filters.Any(f => f.Variable is Variable<DateTime>) || networkCoverage.Parent != null)
                {
                    var currentTimeFilter = networkCoverage.Filters
                        .OfType<IVariableValueFilter>()
                        .Where(f => f.Variable is Variable<DateTime>)
                        .FirstOrDefault();

                    //update the time filter and we're done
                    currentTimeFilter.Values[0] = TimeSelectionStart;
                    return networkCoverage;

                }

                //create a filtered version
                return (INetworkCoverage)networkCoverage.FilterTime(TimeSelectionStart.Value);
            
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

        private void UpdateCoverageTimeFilters(ICoverage coverage)
        {
            var currentTimeFilter = coverage.Filters
                        .OfType<IVariableValueFilter>()
                        .Where(f => f.Variable is Variable<DateTime>)
                        .FirstOrDefault();

            //update the time filter and we're done
            if (currentTimeFilter != null)
            {
                if (TimeSelectionStart.HasValue && TimeSelectionStart.Value == DateTime.MinValue)
                    SetCurrentTimeSelection(coverage.Time.AllValues.FirstOrDefault(), null);

                currentTimeFilter.Values[0] = TimeSelectionStart;
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
