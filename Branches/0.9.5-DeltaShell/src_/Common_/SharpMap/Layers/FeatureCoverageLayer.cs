using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using DelftTools.Functions;
using DelftTools.Functions.Generic;
using DelftTools.Utils.Aop.NotifyPropertyChanged;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;
using log4net;
using NetTopologySuite.Extensions.Coverages;
using SharpMap.Data.Providers;
using SharpMap.Rendering;
using SharpMap.Rendering.Thematics;
using SharpMap.Styles;
using DelftTools.Utils.Collections;

namespace SharpMap.Layers
{
    [NotifyPropertyChanged]
    public class FeatureCoverageLayer : VectorLayer, IFeatureCoverageLayer
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(FeatureCoverageLayer));
        
        private IFeatureCoverage featureCoverage;
        private DateTime? currentTime;
        private readonly FeatureCollection featureCollection;
        private readonly FeatureCoverageRenderer featureCoverageRenderer;

        public FeatureCoverageLayer()
        {
            this.featureCollection = new FeatureCollection();

            // Use the VectorLayer with the custom featureRenderer to render this feature coverage (using values form the data store)
            featureCoverageRenderer = new FeatureCoverageRenderer();
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
            set { FeatureCoverage = (IFeatureCoverage) value; }
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

        public virtual IFeatureCoverage FeatureCoverage
        {
            get { return featureCoverage; }
            set
            {
                if (featureCoverage != null)
                {
                    if (featureCoverage.Features is INotifyCollectionChanged)
                    {
                        var collection = (INotifyCollectionChanged) featureCoverage.Features;
                        collection.CollectionChanged -= this.CollectionCollectionChanged;
                    }

                    ((FeatureCoverage)featureCoverage).PropertyChanged -= this.PropertyChangedPropertyChanged;
                }
                
                featureCoverage = value;
                CreateDefaultTheme();
                RenderRequired = true;

                if (featureCoverage != null)
                {
                    if (featureCoverage.Features is INotifyCollectionChanged)
                    {
                        var collection = (INotifyCollectionChanged) featureCoverage.Features;
                        collection.CollectionChanged += this.CollectionCollectionChanged;
                    }

                    ((FeatureCoverage)featureCoverage).PropertyChanged += this.PropertyChangedPropertyChanged;
                    ((FeatureCoverage)featureCoverage).ValuesChanged += this.ValuesChanged;
                }
            }
        }

        private void ValuesChanged(object sender, FunctionValuesChangedEventArgs e)
        {
            base.RenderRequired = true;
        }

        private void PropertyChangedPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.RenderRequired = true;
        }

        private void CollectionCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            base.RenderRequired = true;
        }

        private void CreateDefaultTheme()
        {
            // If there was no theme attached to the layer yet, generate a default theme
            if (Theme != null || featureCoverage == null || featureCoverage.Features == null || featureCoverage.Features.Count == 0) 
                return;

            Style.GeometryType = GetFeatureGeometryType(featureCoverage.Features[0] as IFeature);

            // Values to base the theme on
            //List<IVariableFilter> filters = new List<IVariableFilter>();
            if (Coverage.Time != null)
            {
                if (CurrentTime == null)
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
            double minValue = featureValues[0];
            double maxValue = featureValues[featureValues.Count - 1];

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
                                                         (float) minValue, (float) maxValue, 1 , 1, false, false);
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
                featureCollection.Features = featureCoverage.Features;
                return featureCollection;
            }
        }

        public override IFeatureProvider DataSource
        {
            get
            {
                return this.FeatureCollection;
            }
            set 
            {
                FeatureCoverage.Features = value.Features;
            }
        }
        public override IEnvelope Envelope
        {
            get
            {
                if (DataSource == null)
                    throw (new ApplicationException("DataSource property not set on layer '" + Name + "'"));

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
        
    }
}
