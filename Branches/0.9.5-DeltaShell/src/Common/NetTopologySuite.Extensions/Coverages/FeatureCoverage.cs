using System;
using System.Collections;
using System.Collections.Generic;
using DelftTools.Functions;
using DelftTools.Functions.Filters;
using DelftTools.Functions.Generic;
using DelftTools.Utils.Aop.NotifyPropertyChanged;
using DelftTools.Utils.Collections;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;

namespace NetTopologySuite.Extensions.Coverages
{
    [NotifyPropertyChanged]
    public class FeatureCoverage : Coverage, IFeatureCoverage
    {
        private IVariable featureVariable;

        private IList features;

        private const string DefaultFeatureCoverageName = "feature coverage";

        public FeatureCoverage() : this(DefaultFeatureCoverageName)
        {
        }

        public FeatureCoverage(string name)
        {
            base.Name = name;
            UpdateGeometry();

            base.Arguments.CollectionChanged += Arguments_CollectionChanged;
        }

        void Arguments_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e is MultiDimensionalArrayChangedEventArgs)
            {
                return;
            }

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Replace:
                    throw new NotImplementedException();

                case NotifyCollectionChangedAction.Add:
                    
                    var variable = (IVariable)e.Item;
                    if (variable.ValueType.GetInterface(typeof(IFeature).Name) != null)
                    {
                        FeatureVariable = variable;
                        //this interferes with copy constructor
                        //FillFeaturesAsVariableValues();
                    }
                    if (variable.ValueType.Equals(typeof(DateTime)))
                    {
                        Time = (IVariable<DateTime>)variable;
                    }
                    break;
            }
        }

        public virtual IVariable FeatureVariable
        {
            get
            {
                if (featureVariable == null && Arguments != null && Arguments.Count > 0)
                    featureVariable = Arguments[0];

                return featureVariable;
            }
            set
            {
                featureVariable = value;
                UpdateGeometry();
            }
        }

        public virtual IList Features
        {
            get
            {
                if (Parent!= null)
                {
                    return ((FeatureCoverage)Parent).Features;
                }

                if(features == null && Arguments != null)
                {
                    features = Arguments[0].Values;
                }

                return features;
            }
            set
            {
                if(features != null)
                {
                    if (features is INotifyCollectionChanged)
                    {
                        ((INotifyCollectionChanged)features).CollectionChanged -= FeatureCoverage_CollectionChanged;
                    }
                }

                features = value;

                if (features is INotifyCollectionChanged)
                {
                    ((INotifyCollectionChanged)features).CollectionChanged += FeatureCoverage_CollectionChanged;
                }
                FillFeaturesAsVariableValues();
            }
        }

        void FeatureCoverage_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    lock (Store)
                    {
                        // Add default values for the added feature
                        SetValues(new[] {0d}, FeatureVariable.CreateValueFilter(e.Item));
                    }
                    break;

                case NotifyCollectionChangedAction.Replace:
                    throw new NotImplementedException();

                case NotifyCollectionChangedAction.Remove:
                    lock (Store)
                    {
                        // Remove the values from our feature variable valuestore belonging to the removed feature
                        //RemoveValues(new VariableValueFilter(FeatureVariable, e.Item));
                        Store.RemoveFunctionValues(this, FeatureVariable.CreateValueFilter(e.Item));
                    }
                    break;
            }
        }

        private void FillFeaturesAsVariableValues()
        {
            Clear(); 
            FeatureVariable.SetValues(features);
        }

        private void UpdateGeometry()
        {
            if (featureVariable != null && featureVariable.Values.Count > 0)
            {
                // Create a geometry object that is defined by all covered feature geometries
                var geometries = new IGeometry[FeatureVariable.Values.Count];
                for (int i = 0; i < featureVariable.Values.Count; i++)
                {
                    geometries[i] = ((IFeature)featureVariable.Values[i]).Geometry;
                }
                Geometry = new GeometryCollection(geometries);

            }
            else
            {
                Geometry = new Point(0, 0);
            }

        }
        public new virtual IFeatureCoverage Filter(params IVariableFilter[] filters)
        {
            var filteredCoverage = new FeatureCoverage() {Parent = this, Filters = filters};
            foreach (IVariable arg in Arguments)
                filteredCoverage.Arguments.Add(arg.Filter(filters));
           
            foreach (IVariable comp in Components)
                filteredCoverage.Components.Add(comp.Filter(filters));
            return filteredCoverage;
            
        }

        public override T Evaluate<T>(ICoordinate coordinate)
        {
            throw new NotImplementedException();
        }

        public override object Evaluate(ICoordinate coordinate)
        {
            throw new NotImplementedException();
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
    }
}
