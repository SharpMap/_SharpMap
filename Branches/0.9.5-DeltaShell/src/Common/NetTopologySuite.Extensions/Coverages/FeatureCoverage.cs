using System;
using System.Collections;
using System.Linq;
using DelftTools.Functions;
using DelftTools.Functions.Filters;
using DelftTools.Functions.Generic;
using DelftTools.Utils.Aop.NotifyPropertyChange;
using DelftTools.Utils.Collections;
using DelftTools.Utils.Collections.Generic;
using DelftTools.Utils.Reflection;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;
using NetTopologySuite.Extensions.Geometries;
using System.Collections.Generic;

namespace NetTopologySuite.Extensions.Coverages
{
    [NotifyPropertyChange]
    public class FeatureCoverage : Coverage, IFeatureCoverage
    {
        private IVariable featureVariable;

        private IEventedList<IFeature> features;

        private const string DefaultFeatureCoverageName = "feature coverage";

        public FeatureCoverage() : this(DefaultFeatureCoverageName)
        {
        }

        public FeatureCoverage(string name)
        {
            base.Name = name;
            Features = new EventedList<IFeature>();
            UpdateGeometry();
        }

        public virtual IVariable FeatureVariable
        {
            get
            {
                if ((featureVariable == null) && (Arguments != null))
                {
                    featureVariable = Arguments.FirstOrDefault(v => v.ValueType.Implements<IFeature>());
                }
                return featureVariable;
            }
        }

        public override void Clear()
        {
            base.Clear();
            Features = new EventedList<IFeature>();
        }

        public virtual IEventedList<IFeature> Features
        {
            get
            {
                return features;
            }
            set
            {
                if(features != null)
                {
                    features.CollectionChanged -= FeaturesCollectionChanged;

                    if (features.Count > 0)
                    {
                        ThrowExceptionOnModificationWhenInNetCdf(features, value);
                    }
                }
                
                features = value;
                
                if (features != null)
                {
                    features.CollectionChanged += FeaturesCollectionChanged;
                }
            }
        }
        
        private void ThrowExceptionOnModificationWhenInNetCdf(IEnumerable<IFeature> values, IEnumerable<IFeature> newValues)
        {
           if (!values.SequenceEqual(newValues)) //modifcation check
           {
               ThrowExceptionOnModificationWhenInNetCdf();
           }
        }

        private void ThrowExceptionOnModificationWhenInNetCdf()
        {
            if (FeatureVariable.Values.Count > 0 && FeatureVariable.Values.IsReadOnly) //already saved (in netcdf)
            {
                throw new NotSupportedException("Changing the feature list after setting and persisting coverage values is not allowed!");
            }
        }
        
        void FeaturesCollectionChanged(object sender, NotifyCollectionChangingEventArgs e)
        {
            if (sender != features)
            {
                return;
            }

            ThrowExceptionOnModificationWhenInNetCdf();

            UpdateGeometry();
        }

        private void UpdateGeometry()
        {
            if (FeatureVariable != null && FeatureVariable.Values.Count > 0)
            {
                // Create a geometry object that is defined by all covered feature geometries
                var features = FeatureVariable.Values.Cast<IFeature>().ToArray();
                var geometries = features.Where(f => f.Geometry != null).Select(f=>f.Geometry).ToArray();
                Geometry = new GeometryCollection(geometries);
            }
            else
            {
                Geometry = new Point(0, 0);
            }

        }

        public virtual IFeatureCoverage FilterAsFeatureCoverage(params IVariableFilter[] filters)
        {
            var filteredFeatureCoverage = (IFeatureCoverage)base.Filter(filters);

            filteredFeatureCoverage.Features = Features;

            return filteredFeatureCoverage;
        }
        

        /// <summary>
        /// Tolerance used when evaluating values based on coordinate.
        /// </summary>
        [NotifyPropertyChange]
        public virtual double EvaluateTolerance { get; set; }

        public override T Evaluate<T>(ICoordinate coordinate)
        {
            throw new NotImplementedException();
        }

        public override object Evaluate(ICoordinate coordinate)
        {
            var featuresInRange = GeometryHelper.GetFeaturesInRange(coordinate, features.Cast<IFeature>(), EvaluateTolerance);
            return featuresInRange.Select(feature => (!IsTimeDependent) ? this[feature] : this[Time.MinValue, feature]);
        }

        public override T Evaluate<T>(double x, double y)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Evaluates value of coverage for a feature.
        /// </summary>
        /// <param name="feature">The feature to evealuate a value over.</param>
        /// <returns>The evaluated value for the given feature.</returns>
        public virtual T Evaluate<T>(IFeature feature)
        {
            throw new NotImplementedException();
        }
        public override IFunction GetTimeSeries(ICoordinate coordinate)
        {
            //find a feature whose coordinate matches with the queried coordinate..this might not fly with complexer features..
            //adapt to a different stategy when needed
            var feature = FeatureVariable.Values.OfType<IFeature>().FirstOrDefault(f => f.Geometry.Coordinate.Equals2D(coordinate));
            if (feature != null)
            {
                //filter on the specific feature
                var valueFilter = FeatureVariable.CreateValueFilter(feature);
                var reduceFilter = new VariableReduceFilter(FeatureVariable);
                var timeSeries = Filter(valueFilter,reduceFilter);
                var name = Name + " at " + feature;
                //set both names as sometimes the component name is used
                timeSeries.Name = name;
                timeSeries.Components[0].Name = name;

                return timeSeries;
            }
            throw new InvalidOperationException(string.Format("No data defined on location {0}",coordinate));
        }

        //TODO: create a seperate builder / factory if this gets out of hand
        public static FeatureCoverage GetTimeDependentFeatureCoverage<T>() where T : IFeature
        {
            var featureCoverage = new FeatureCoverage();
            featureCoverage.Components.Add(new Variable<double>("value") { NoDataValue = double.NaN });
            var timeVariable = new Variable<DateTime>("time");
            featureCoverage.Arguments.Add(timeVariable);
            featureCoverage.Arguments.Add(new Variable<T>("feature"));
            return featureCoverage;
        }
    }
}
