#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DelftTools.Functions.Filters;
using GeoAPI.Extensions.Feature;
using NetTopologySuite.Extensions.Features;
using SharpMap.Api;

#endregion

namespace SharpMap.Layers
{
    public class LayerAttribute
    {
        private readonly string attributeName;
        private readonly ILayer layer;
        private IComparable maxValue;
        private IComparable minValue;

        public override string ToString()
        {
            return AttributeName;
        }

        public string AttributeName
        {
            get { return attributeName; }
        }

        public string DisplayName
        {
            get
            {
                if (IsCoverageLayer)
                    return AttributeName;
                
                //try to find to the display name on the feature otherwise use attribute name
                if (layer.DataSource.Features.Count != 0)
                {
                    return FeatureAttributeAccessorHelper.GetAttributeDisplayName(layer.DataSource.GetFeature(0), attributeName);
                }
                return FeatureAttributeAccessorHelper.GetPropertyDisplayName(layer.DataSource.FeatureType, attributeName);
            }
        }
        
        public LayerAttribute(ILayer layer, string attributeName)
        {
            this.layer = layer;
            this.attributeName = attributeName;
            //TODO : check the attribute name is ok...?>
        }

        /// <summary>
        /// TODO: these are not attribute values but attribute values without NoDataValues
        /// </summary>
        public IEnumerable<IComparable> AttributeValues
        {
            get { return GetAttributeValues(); }
        }

        private IEnumerable<IComparable> GetAttributeValues()
        {
            if (IsCoverageLayer)
            {
                return GetCoverageAttributeValues();
            }
            return GetAttributeValuesFromFeatures();
        }

        private IEnumerable<IComparable> GetCoverageAttributeValues()
        {
            var coverage = ((ICoverageLayer) layer).Coverage;
            var coverageFirstComponent = coverage.Components[0];

            var values = coverage.IsTimeDependent && coverage.Time.Values.Count > 0
                         ? coverageFirstComponent.GetValues(new VariableIndexRangeFilter(coverage.Time, 0))
                                                 .Cast<IComparable>()
                                                 .Concat(new[]
                                                     {
                                                         (IComparable) coverageFirstComponent.MinValue,
                                                         (IComparable) coverageFirstComponent.MaxValue
                                                     })
                         : coverageFirstComponent.Values.Cast<IComparable>();

            return GetValuesExceptNoData(values);
        }

        private IEnumerable<IComparable> GetValuesExceptNoData(IEnumerable<IComparable> values)
        {
            if (NoDataValues == null)
            {
                return values;
            }

            var noDataValues = NoDataValues.Cast<IComparable>().ToArray();

            if (noDataValues.Length == 1)
            {
                return values.Where(v2 => !Equals(v2, noDataValues[0]));
            }
            
            if (noDataValues.Length > 1)
            {
                throw new NotSupportedException("Multiple NODATA value are not supported");
            }

            return values;
        }

        public IComparable MinValue
        {
            get { return minValue ?? (minValue = GetMinValue()); }
        }

        public IComparable MaxValue
        {
            get { return maxValue ?? (maxValue = GetMaxValue()); }
        }

        public IList NoDataValues
        {
            get
            {
                return IsCoverageLayer ? ((ICoverageLayer)layer).Coverage.Components[0].NoDataValues : null;
            }
        }

        private bool IsCoverageLayer
        {
            get { return layer is ICoverageLayer; }
        }

        public List<IComparable> UniqueValues
        {
            get
            {
                var uniqueValues = new HashSet<IComparable>();

                IEnumerable values = null;
                
                if (layer is ICoverageLayer)
                {
                    values = ((ICoverageLayer) layer).Coverage.Components[0].Values;
                }
                else if(layer is VectorLayer)
                {
                    values = GetAttributeValuesFromFeatures();
                }
                else
                {
                    return new List<IComparable>();
                }
                
                foreach (IComparable attributeValue in values)
                {
                    uniqueValues.Add(attributeValue);
                }

                return GetValuesExceptNoData(uniqueValues).ToList();
            }
        }

        public bool IsNumerical
        {
            get
            {
                return (MinValue != null) && IsNumericalType(MinValue.GetType());
            }
        }

        public float MinNumValue
        {
            get
            {
                return (IsNumerical) ? Convert.ToSingle(MinValue) : 0;
            }
        }

        public float MaxNumValue
        {
            get
            {
                return (IsNumerical) ? Convert.ToSingle(MaxValue) : 0;
            }
        }

        private IEnumerable<IComparable> GetAttributeValuesFromFeatures()
        {
            var attributeValuesFound = new List<IComparable>();

            if (layer == null || layer.DataSource == null)
                return Enumerable.Empty<IComparable>();

            foreach (var feature in layer.DataSource.Features.Cast<IFeature>())
            {
                //check if value can be cast to icomparable ie DBnull value wil yield null
                var value =
                    FeatureAttributeAccessorHelper.GetAttributeValue(feature, attributeName, false) as IComparable;

                if (value != null)
                {
                    attributeValuesFound.Add(value);
                }
            }

            return attributeValuesFound;
        }

        private IComparable GetMinValue()
        {
            if (IsCoverageLayer)
            {
                return ((ICoverageLayer) layer).MinValue;
            }

            return (AttributeValues != null) ? AttributeValues.Min() : null;
        }

        private IComparable GetMaxValue()
        {
            if (IsCoverageLayer)
            {
                return ((ICoverageLayer) layer).MaxValue;
            }

            return (AttributeValues != null) ? AttributeValues.Max() : null;
        }

        private static bool IsNumericalType(Type type)
        {
            return (type == typeof(Int64) ||
                    type == typeof(Single) ||
                    type == typeof(UInt32) ||
                    type == typeof(UInt16) ||
                    type == typeof(Int32) ||
                    type == typeof(Int16) ||
                    type == typeof(Double));
        }
    }
}