using System;
using System.Collections.Generic;
using System.Linq;
using DelftTools.Utils;
using DelftTools.Utils.Collections.Generic;
using GeoAPI.Extensions.Coverages;
using SharpMap.Data.Providers;

namespace SharpMap.Layers
{
    public class NetworkCoverageGroupLayer : GroupLayer, INetworkCoverageGroupLayer, ITimeNavigatable
    {
        private DateTime timeSelectionStart;
        private INetworkCoverage networkCoverage;

        public NetworkCoverageGroupLayer(): base("NetworkCoverage")
        {
            HasReadOnlyLayersCollection = true;
        }

        public override string Name
        {
            get
            {
                //if a coverage is defined the layer name is based on the coverage name
                if (Coverage != null)
                {
                    return Coverage.Name;
                }
                return base.Name;
            }
            set
            {
                //if this is problematic switch to a propertychanged implementation
                base.Name = value;
            }
        }

        public override bool ReadOnly
        {
            get
            {
                if (NetworkCoverage != null)
                {
                    return !NetworkCoverage.IsEditable;
                }
                return false;
            }
        }

        public override IEventedList<ILayer> Layers
        {
            get
            {
                
                // initialize
                if (base.Layers.Count == 0)
                {
                    base.Layers.Add(new NetworkCoverageLocationLayer());
                    base.Layers.Add(new NetworkCoverageSegmentLayer());
                }

                return base.Layers;
            }
            set { base.Layers = value; }
        }

        public virtual INetworkCoverage NetworkCoverage
        {
            get { return networkCoverage; }
            set
            {
                if (networkCoverage != null && networkCoverage.IsTimeDependent)
                {
                    networkCoverage.Time.ValuesChanged -= TimeValuesChanged;
                }

                networkCoverage = value;
                Initialize();

                if (networkCoverage != null && networkCoverage.IsTimeDependent)
                {
                    timeSelectionStart = GetDefaultTimeFromCoverage(networkCoverage);
                    networkCoverage.Time.ValuesChanged += TimeValuesChanged;
                }
            }
        }

        public virtual ICoverage Coverage
        {
            get { return NetworkCoverage; }
            set { NetworkCoverage = (INetworkCoverage)value; }
        }

        //MOVE this into interface ISO SetCurrentTime
        public virtual DateTime? TimeSelectionStart
        {
            get { return timeSelectionStart; }
        }

        public virtual DateTime? TimeSelectionEnd
        {
            get { return null; }
        }

        public virtual TimeNavigatableLabelFormatProvider CustomDateTimeFormatProvider
        {
            get { return null; }
        }

        public virtual IEnumerable<DateTime> Times
        {
            get
            {
                if (NetworkCoverage.IsTimeDependent)
                {
                    return NetworkCoverage.Time.Values;
                }

                return Enumerable.Empty<DateTime>();
            }
        }

        public virtual TimeSelectionMode SelectionMode
        {
            get { return TimeSelectionMode.Single; }
        }

        public virtual SnappingMode SnappingMode
        {
            get { return SnappingMode.Nearest; }
        }

        public virtual NetworkCoverageLocationLayer LocationLayer
        {
            get { return (NetworkCoverageLocationLayer)Layers[0]; }
        }

        public virtual NetworkCoverageSegmentLayer SegmentLayer
        {
            get
            {
                //no segment layer defined
                if (Layers.Count < 2)
                {
                    return null;
                }
                return (NetworkCoverageSegmentLayer)Layers[1];
            }
        }

        public virtual void SetCurrentTimeSelection(DateTime? start, DateTime? end)
        {
            timeSelectionStart = start.Value;

            //set it in the 'child' layers
            //update child datasources.
            if ((LocationLayer.DataSource as NetworkCoverageFeatureCollection) != null)
            {
                (LocationLayer.DataSource as NetworkCoverageFeatureCollection).SetCurrentTimeSelection(timeSelectionStart, null);
            }
            if ((SegmentLayer.DataSource as NetworkCoverageFeatureCollection) != null)
            {
                (SegmentLayer.DataSource as NetworkCoverageFeatureCollection).SetCurrentTimeSelection(timeSelectionStart, null);
            }
            RenderRequired = true;
        }

        public virtual event Action CurrentTimeSelectionChanged;

        public virtual event Action TimesChanged;

        private void Initialize()
        {
            // If theme is not set generate default one.

            if (networkCoverage.SegmentGenerationMethod == SegmentGenerationMethod.None && SegmentLayer != null) // ugly, refactor it
            {
                HasReadOnlyLayersCollection = false;
                Layers.Remove(SegmentLayer);
                HasReadOnlyLayersCollection = true;
            }
            InitializeFeatureProviders();
        }

        /// <summary>
        /// Late initialization, when Layers and Network is set.
        /// </summary>
        private void InitializeFeatureProviders()
        {
            if (NetworkCoverage == null || Layers.Count <= 0)
            {
                return;
            }

            LocationLayer.Coverage = NetworkCoverage;
            if (SegmentLayer != null)
            {
                SegmentLayer.Coverage = NetworkCoverage;
            }

        }

        private void TimeValuesChanged(object sender, DelftTools.Functions.FunctionValuesChangingEventArgs e)
        {
            if (TimesChanged != null)
            {
                TimesChanged();
            }
        }

        private static DateTime GetDefaultTimeFromCoverage(ICoverage coverage)
        {
            //if no time is specified we set a default (first or minvalue)
            return coverage.Time.AllValues.Count > 0 ? coverage.Time.AllValues[0] : DateTime.MinValue;
        }
    }
}