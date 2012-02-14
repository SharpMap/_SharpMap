using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using DelftTools.Functions;
using DelftTools.Functions.Generic;
using DelftTools.Utils;
using DelftTools.Utils.Aop.NotifyPropertyChange;
using DelftTools.Utils.Collections.Generic;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;
using log4net;
using NetTopologySuite.Extensions.Coverages;
using SharpMap.Data.Providers;
using SharpMap.Rendering;
using SharpMap.Rendering.Thematics;
using DelftTools.Utils.Collections;

namespace SharpMap.Layers
{
    [NotifyPropertyChange]
    public class FeatureCoverageLayer : VectorLayer, IFeatureCoverageLayer, ITimeNavigatable
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(FeatureCoverageLayer));
        
        private IFeatureCoverage featureCoverage;
        private DateTime? currentTime;
        private readonly FeatureCollection featureCollection;
        private readonly FeatureCoverageRenderer featureCoverageRenderer;

        //todo: rather nasty: but nhibernate complains when overriding / newing the original method
        public virtual FeatureCoverageLabelLayer LabelLayer
        {
            get { return (FeatureCoverageLabelLayer) base.LabelLayer; }
            set { base.LabelLayer = (FeatureCoverageLabelLayer) value; }
        }

        LabelLayer ILayer.LabelLayer
        {
            get { return LabelLayer; }
            set { LabelLayer = (FeatureCoverageLabelLayer) value; }
        }

        public FeatureCoverageLayer():this(new FeatureCoverageRenderer())
        {

        }

        public FeatureCoverageLayer(FeatureCoverageRenderer renderer)
        {
            this.featureCollection = new FeatureCollection();

            // Use the VectorLayer with the custom featureRenderer to render this feature coverage (using values form the data store)
            featureCoverageRenderer = renderer;

            LabelLayer = new FeatureCoverageLabelLayer();
        }

        /// <summary>
        /// Renders the layer's coverage features to a graphics object
        /// </summary>
        /// <param name="g">Graphics object reference</param>
        /// <param name="map">Map which is rendered</param>
        public override void OnRender(Graphics g, Map map)
        {
            // Use the FeatureCoverageRenderer to render the actual applicable features
            featureCoverageRenderer.Render(featureCoverage, g, this);
        }

        public override object Clone()
        {
            var fcLayer = (FeatureCoverageLayer) base.Clone();
            
            fcLayer.FeatureCoverage = featureCoverage;
            fcLayer.CurrentTime = CurrentTime;
            
            return fcLayer;
        }

        #region Implementation of ICoverageLayer

        public virtual ICoverage Coverage
        {
            get { return FeatureCoverage; }
            set
            {
                if (FeatureCoverage != null && FeatureCoverage.IsTimeDependent)
                {
                    FeatureCoverage.Time.ValuesChanged -= TimeValuesChanged;
                }

                FeatureCoverage = (IFeatureCoverage) value;

                if (FeatureCoverage != null && FeatureCoverage.IsTimeDependent)
                {
                    FeatureCoverage.Time.ValuesChanged += TimeValuesChanged;
                }
            }
        }

        void TimeValuesChanged(object sender, FunctionValuesChangingEventArgs e)
        {
            if (TimesChanged != null)
                TimesChanged();
        }

        public virtual DateTime? CurrentTime
        {
            get { return currentTime; }
            set
            {
                currentTime = value;
                RenderRequired = true;
            }
        }

        #endregion

        public virtual IFeatureCoverage FeatureCoverageToRender
        {
            get
            {
                if (featureCoverage.IsTimeDependent)
                {
                    if (featureCoverage.Time.Values.Count == 0)
                    {
                        return featureCoverage;
                    }
                    var timeToUse = TimeSelectionStart ?? featureCoverage.Time.MinValue;

                    return (IFeatureCoverage) featureCoverage.FilterTime(timeToUse);
                }

                return featureCoverage;
            }
        }

        public virtual IFeatureCoverage FeatureCoverage
        {
            get { return featureCoverage; }
            set
            {
                if (featureCoverage != null)
                {
                    if (featureCoverage.Features is INotifyCollectionChange)
                    {
                        var collection = (INotifyCollectionChange) featureCoverage.Features;
                        collection.CollectionChanged -= this.CollectionCollectionChanged;
                    }

                    ((INotifyPropertyChanged)featureCoverage).PropertyChanged -= this.PropertyChangedPropertyChanged;
                }

                featureCoverage = value; 
                LabelLayer.Coverage = featureCoverage;
                CreateDefaultTheme();
                RenderRequired = true;

                if (featureCoverage != null)
                {
                    if (featureCoverage.Features is INotifyCollectionChange)
                    {
                        var collection = (INotifyCollectionChange) featureCoverage.Features;
                        collection.CollectionChanged += this.CollectionCollectionChanged;
                    }

                    ((INotifyPropertyChanged)featureCoverage).PropertyChanged += this.PropertyChangedPropertyChanged;
                    ((FeatureCoverage)featureCoverage).ValuesChanged += this.ValuesChanged;
                }
            }
        }

        public override ITheme Theme
        {
            get
            {
                if (themeIsDirty)
                {
                    themeIsDirty = false;
                    UpdateTheme();
                }
                return base.Theme;
            }
            set
            {
                base.Theme = value;
            }
        }

        private void ValuesChanged(object sender, FunctionValuesChangingEventArgs e)
        {
            themeIsDirty = true;
            base.RenderRequired = true;
        }

        private void PropertyChangedPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.RenderRequired = true;
        }

        private void CollectionCollectionChanged(object sender, NotifyCollectionChangingEventArgs e)
        {
            themeIsDirty = true;
            base.RenderRequired = true;
        }

        private void CreateDefaultTheme()
        {
            if (Theme != null)
            {
                return;
            }
            UpdateTheme();
        }
        private void UpdateTheme()
        {            
            // If there was no theme attached to the layer yet, generate a default theme
            if (featureCoverage == null || featureCoverage.Features == null || featureCoverage.Features.Count == 0)
                return;

            Style.GeometryType = GetFeatureGeometryType(featureCoverage.Features[0] as IFeature);

            // Values to base the theme on
            //List<IVariableFilter> filters = new List<IVariableFilter>();
            if (Coverage.Time != null)
            {
                if (CurrentTime == null && Coverage.Time.Values.Count != 0)
                {
                    CurrentTime = Coverage.Time.Values[0];
                }
                //filters.Add(new VariableValueFilter(featureCoverage.Time, CurrentTime));
            }
            IMultiDimensionalArray<double> values = featureCoverage.GetValues<double>();
            if (null == values)
                return;
            // NOTE: we're getting all values here!

            var featureValues = new List<double>(values.Where(v => !double.IsNaN(v)));

            if (0 == featureValues.Count)
            {
                log.Error("Unable to generate default theme; no values available");
                return;
                //throw new ArgumentException();
            }

            featureValues.Sort();
            double minValue = featureValues.Min();
            double maxValue = featureValues.Max();

            if (minValue == maxValue)
            {
                // Only a single value, so no gradient theme needed/wanted: create a 'blue' single feature theme
                Theme = ThemeFactory.CreateSingleFeatureTheme(Style.GeometryType, Color.Blue, 10);
            }
            else
            {
                // Create 'green to blue' gradient theme
                Theme = ThemeFactory.CreateGradientTheme(Coverage.Components[0].Name, Style,
                                                         new ColorBlend(new Color[] { Color.Green, Color.Blue },
                                                                        new float[] { 0f, 1f }),
                                                         (float)minValue, (float)maxValue, 1, 1, false, true);
            }
        }

        private static Type GetFeatureGeometryType(IFeature feature)
        {
            var defaultType = typeof (IPolygon);

            if (feature == null || feature.Geometry == null)
            {
                return defaultType;
            }

            var typeList = new List<Type>
                               {
                                   typeof(IPoint),
                                   typeof (IPolygon),typeof(IMultiPolygon),
                                   typeof(ILineString),typeof(IMultiLineString)
                               };

            foreach (var type in typeList)
            {
                if (type.IsAssignableFrom(feature.Geometry.GetType()))
                {
                    return type;
                }
            }
            return defaultType;
        }

        private FeatureCollection FeatureCollection
        {
            get
            {
                if (featureCoverage == null) return null;
                featureCollection.Features = featureCoverage.Features.ToList();
                return featureCollection;
            }
        }

        public override IFeatureProvider DataSource
        {
            get
            {
                if (featureCoverage == null) return null;
                //update the current featurecollection with the features of the coverage
                if (featureCoverage.Features != null)
                {
                    featureCollection.Features = featureCoverage.Features.ToList(); ;    
                }
                return featureCollection;
            }
            set 
            {
                //TODO tricky..the datasource should be leading? review and adapt.
                if (FeatureCoverage != null)
                {
                    FeatureCoverage.Features = new EventedList<IFeature>(value.Features.Cast<IFeature>());    
                }

                LabelLayer.DataSource = value;
            }
        }
        public override IEnvelope Envelope
        {
            get
            {
                if (DataSource == null)
                {
                    return new Envelope();
                }

                return DataSource.GetExtents();
            }
        }

        public override bool ReadOnly
        {
            get
            {
                if (featureCoverage != null) 
                    return !featureCoverage.IsEditable;
                return false;
            }
        }

        public virtual DateTime? TimeSelectionStart { get; protected set; }

        public virtual DateTime? TimeSelectionEnd { get; protected set; }

        public virtual TimeNavigatableLabelFormatProvider CustomDateTimeFormatProvider
        {
            get { return null; }
        }

        public virtual void SetCurrentTimeSelection(DateTime? start, DateTime? end)
        {
            TimeSelectionStart = start;
            TimeSelectionEnd = end;

            if(CurrentTimeSelectionChanged != null)
            {
                CurrentTimeSelectionChanged();
            }
        }

        public virtual event Action CurrentTimeSelectionChanged;

        public virtual IEnumerable<DateTime> Times
        {
            get { return (Coverage.Time != null)? Coverage.Time.Values : null; }
        }

        public virtual event Action TimesChanged;

        public virtual TimeSelectionMode SelectionMode
        {
            get { return TimeSelectionMode.Single; }
        }


        private SnappingMode snappingMode = SnappingMode.Nearest; //default value
        public virtual SnappingMode SnappingMode
        {
            get { return snappingMode; }
            set { snappingMode = value; }
        }
    }
}
