using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using DelftTools.Utils;
using DelftTools.Utils.Collections.Generic;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Extensions.Feature;
using GeoAPI.Extensions.Networks;
using SharpMap.Api;
using SharpMap.Api.Editors;
using SharpMap.Data.Providers;
using SharpMap.Editors;
using SharpMap.Editors.Interactors.Network;
using SharpMap.Editors.Snapping;
using SharpMap.Rendering.Thematics;
using SharpMap.Styles;
using ShapeType = SharpMap.Styles.ShapeType;

namespace SharpMap.Layers
{
    public class NetworkCoverageGroupLayer : GroupLayer, INetworkCoverageGroupLayer, ITimeNavigatable
    {
        private DateTime timeSelectionStart;
        private INetworkCoverage networkCoverage;

        public NetworkCoverageGroupLayer(): base("NetworkCoverage")
        {
            HasReadOnlyLayersCollection = true;

            SetFeatureEditorForNetworkCoverage(this);
        }

        private static void SetFeatureEditorForNetworkCoverage(INetworkCoverageGroupLayer networkCoverageGroupLayer)
        {
            ISnapRule networkCoverageSnapRule = new SnapRule
            {
                Criteria = (layer, feature) => feature is IBranch && ((IBranch)feature).Network == networkCoverageGroupLayer.NetworkCoverage.Network,
                SnapRole = SnapRole.FreeAtObject,
                Obligatory = true,
                PixelGravity = 40,
                NewFeatureLayer = networkCoverageGroupLayer.LocationLayer
            };

            networkCoverageGroupLayer.FeatureEditor = new NetworkLocationFeatureEditor(networkCoverageGroupLayer);

            var locationFeatureEditor = new NetworkLocationFeatureEditor(networkCoverageGroupLayer) { SnapRules = { networkCoverageSnapRule } };

            networkCoverageGroupLayer.LocationLayer.FeatureEditor = locationFeatureEditor;
        }

        private class NetworkLocationFeatureEditor : FeatureEditor
        {
            private readonly INetworkCoverageGroupLayer networkCoverageGroupLayer;

            public NetworkLocationFeatureEditor(INetworkCoverageGroupLayer networkCoverageGroupLayer)
            {
                this.networkCoverageGroupLayer = networkCoverageGroupLayer;
            }

            public override IFeatureInteractor CreateInteractor(ILayer layer, IFeature feature)
            {
                return new NetworkLocationFeatureInteractor(layer, feature, ((VectorLayer) layer).Style, networkCoverageGroupLayer.NetworkCoverage);
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
                    hasReadOnlyLayersCollection = false;

                    base.Layers.Add(new NetworkCoverageLocationLayer());
                    //don't show in legend initially, as themes are exactly the same
                    base.Layers.Add(new NetworkCoverageSegmentLayer {ShowInLegend = false});

                    hasReadOnlyLayersCollection = true;
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
                if (networkCoverage != null)
                {
                    ((INotifyPropertyChange)networkCoverage).PropertyChanged -= NetworkCoveragePropertyChanged;
                    if (networkCoverage.IsTimeDependent)
                    {
                        networkCoverage.Time.ValuesChanged -= TimeValuesChanged;
                    }
                }

                networkCoverage = value;
                
                Initialize();

                if (networkCoverage != null)
                {
                    ((INotifyPropertyChange)networkCoverage).PropertyChanged += NetworkCoveragePropertyChanged;
                    if (networkCoverage.IsTimeDependent)
                    {
                        timeSelectionStart = GetDefaultTimeFromCoverage(networkCoverage);
                        networkCoverage.Time.ValuesChanged += TimeValuesChanged;
                    }
                    OnCoverageNameChanged();
                }
            }
        }

        private void NetworkCoveragePropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender == networkCoverage && e.PropertyName.Equals("Name", StringComparison.Ordinal))
            {
                OnCoverageNameChanged();
            }
        }

        private void OnCoverageNameChanged()
        {
            if (NameIsReadOnly)
            {
                NameIsReadOnly = false;
                base.Name = networkCoverage.Name;
                NameIsReadOnly = true;
            }
            else
            {
                base.Name = networkCoverage.Name;
            }
        }

        public virtual ICoverage Coverage
        {
            get { return NetworkCoverage; }
            set { NetworkCoverage = (INetworkCoverage)value; }
        }

        public virtual IComparable MinValue
        {
            get
            {
                return null;
            }
        }

        public virtual IComparable MaxValue
        {
            get
            {
                return null;
            }
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
            if (SegmentLayer != null && (SegmentLayer.DataSource as NetworkCoverageFeatureCollection) != null)
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
        /// Generates theme for route type network coverage layers.
        /// </summary>
        /// <param name="groupLayer"></param>
        /// <param name="color"></param>
        public static void SetupRouteLayerTheme(INetworkCoverageGroupLayer groupLayer, Color? color)
        {
            if (null == color)
            {
                color = Color.FromArgb(100, Color.Green);
            }

            // customize theme
            var segmentTheme = ThemeFactory.CreateSingleFeatureTheme(groupLayer.SegmentLayer.Style.GeometryType, (Color)color, 10);
            var locationTheme = ThemeFactory.CreateSingleFeatureTheme(groupLayer.LocationLayer.Style.GeometryType, (Color)color, 15);

            groupLayer.SegmentLayer.Theme = segmentTheme;
            groupLayer.LocationLayer.Theme = locationTheme;

            var locationStyle = (VectorStyle)locationTheme.DefaultStyle;
            locationStyle.Fill = Brushes.White;
            locationStyle.Shape = ShapeType.Ellipse;
            locationStyle.ShapeSize = 15;

            var segmentStyle = (VectorStyle)segmentTheme.DefaultStyle;
            segmentStyle.Line.EndCap = LineCap.ArrowAnchor;
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
            var layerTheme = LocationLayer.Theme as Theme;
            if (layerTheme != null)
            {
                layerTheme.NoDataValues = NetworkCoverage.Components[0].NoDataValues;
            }
            if (SegmentLayer != null)
            {
                SegmentLayer.Coverage = NetworkCoverage;
                layerTheme = SegmentLayer.Theme as Theme;
                if (layerTheme != null)
                {
                    layerTheme.NoDataValues = NetworkCoverage.Components[0].NoDataValues;
                }
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