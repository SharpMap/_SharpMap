using System;
using System.Linq;
using DelftTools.Functions;
using DelftTools.Functions.Filters;
using DelftTools.Functions.Generic;
using DelftTools.Utils;
using DelftTools.Utils.Aop;
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
    [Entity(FireOnCollectionChange=false)]
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

        [Aggregation]
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
                        ThrowExceptionOnModificationWhenInNetCdf(features, value); // HACK: NetCDF?!?!?!
                    }
                }
                
                features = value;
                
                if (features != null)
                {
                    features.CollectionChanged += FeaturesCollectionChanged;
                }
            }
        }
        
        [EditAction]
        private void ThrowExceptionOnModificationWhenInNetCdf(IEnumerable<IFeature> values, IEnumerable<IFeature> newValues)
        {
           if (!values.SequenceEqual(newValues)) //modifcation check
           {
               ThrowExceptionOnModificationWhenInNetCdf();
           }
        }

        [EditAction]
        private void ThrowExceptionOnModificationWhenInNetCdf()
        {
            if (FeatureVariable.Values.Count > 0 && FeatureVariable.Values.IsReadOnly) //already saved (in netcdf)
            {
                throw new NotSupportedException("Changing the feature list after setting and persisting coverage values is not allowed!");
            }
        }
        
        void FeaturesCollectionChanged(object sender, NotifyCollectionChangingEventArgs e)
        {
            if (sender != features || e.Action == NotifyCollectionChangeAction.Replace)
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
        public virtual double EvaluateTolerance { get; set; }

        public override T Evaluate<T>(ICoordinate coordinate)
        {
            throw new NotImplementedException();
        }

        public override object Evaluate(ICoordinate coordinate)
        {
            var point = new Point(coordinate);
            return GeometryHelper.GetFeaturesInRange(coordinate, features, EvaluateTolerance)
                                 .OrderBy(f => point.Distance(f.Geometry))
                                 .Select(f => (!IsTimeDependent) ? this[f] : this[Time.MinValue, f])
                                 .FirstOrDefault();
        }

        public override T Evaluate<T>(double x, double y)
        {
            throw new NotImplementedException();
        }

        public override object Evaluate(ICoordinate coordinate, DateTime? time)
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

        public virtual double Evaluate(DateTime dateTime, IFeature feature)
        {
            if (!IsTimeDependent)
                throw new ArgumentException(
                    "Please do not specify time filter to retrieve value from non-time related feature coverage");

            var times = Time.Values;

            var timeToEvaluate = dateTime;

            if (!times.Contains(dateTime))
            {
                //grab last time less than dateTime, filter, and call Evaluate on that
                timeToEvaluate = times.LastOrDefault(t => t < dateTime);
                if (timeToEvaluate == default(DateTime))
                {
                    return (double) Components[0].NoDataValue;
                }
            }

            return (double)this[timeToEvaluate, feature];
        }

        public override IFunction GetTimeSeries(ICoordinate coordinate)
        {
            //find a feature whose coordinate matches with the queried coordinate..this might not fly with complexer features..
            //adapt to a different stategy when needed
            var feature = FeatureVariable.Values.OfType<IFeature>().FirstOrDefault(f => f.Geometry.Coordinate.Equals2D(coordinate));

            if (feature == null)
            {
                feature =
                    FeatureVariable.Values.OfType<IFeature>().FirstOrDefault(
                        f => f.Geometry.Centroid.Coordinate.Equals2D(coordinate));
            }

            if (feature != null)
            {
                return GetTimeSeries(feature);
            }
            throw new InvalidOperationException(string.Format("No data defined on location {0}",coordinate));
        }

        public override IFunction GetTimeSeries(IFeature feature)
        {
            //filter on the specific feature
            var valueFilter = FeatureVariable.CreateValueFilter(feature);
            var reduceFilter = new VariableReduceFilter(FeatureVariable);
            var timeSeries = Filter(valueFilter, reduceFilter);
            var name = Name + " at " + feature;
            //set both names as sometimes the component name is used
            timeSeries.Name = name;
            timeSeries.Components[0].Name = name;

            return timeSeries;
        }

        public override object Clone(bool copyValues, bool skipArguments, bool skipComponents)
        {
            var clone = (FeatureCoverage)base.Clone(copyValues, skipArguments, skipComponents);

            clone.Features = new EventedList<IFeature>(Features);

            clone.Attributes = new Dictionary<string, string>();
            foreach (var attributeKey in Attributes.Keys)
            {
                clone.Attributes.Add(attributeKey, Attributes[attributeKey]);
            }

            return clone;
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

        public static void RefreshAfterClone(IFeatureCoverage featureCoverage, 
            IEnumerable<IFeature> featuresSuperSetBefore, 
            IEnumerable<IFeature> featuresSuperSetAfter)
        {
            var featuresBefore = featuresSuperSetBefore.ToList();
            var featuresAfter = featuresSuperSetAfter.ToList();

            if (featuresBefore.Count != featuresAfter.Count)
            {
                throw new ArgumentException("Non matching feature count before / after");
            }
            
            ObjectHelper.RefreshItemsInList(featureCoverage.Features, featuresBefore, featuresAfter);

            var featuresMDA = featureCoverage.FeatureVariable.Values;

            var wasStored = featuresMDA.IsAutoSorted;
            var wasReadOnly = featuresMDA.IsReadOnly;
            featuresMDA.IsAutoSorted = false;
            featuresMDA.IsReadOnly = false; //we just do a replace, so ignore read-only is safe (right?)

            try
            {
                ObjectHelper.RefreshItemsInList(featuresMDA, featuresBefore, featuresAfter);
            }
            finally
            {
                featuresMDA.IsAutoSorted = wasStored;
                featuresMDA.IsReadOnly = wasReadOnly;
            }
        }

        [NoNotifyPropertyChange]
        public override IFunctionStore Store
        {
            get
            {
                return base.Store;
            }
            set
            {
                base.Store = value;
                if (value != null)
                {
                    //to prevent events on features in the feature variable are bubbled
                    value.SkipChildItemEventBubbling = true;
                }
            }
        }
    }
}
