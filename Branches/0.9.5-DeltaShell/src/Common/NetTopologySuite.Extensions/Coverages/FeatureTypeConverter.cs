using System;
using DelftTools.Functions;
using DelftTools.Utils;
using DelftTools.Utils.Aop;
using DelftTools.Utils.Reflection;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Extensions.Feature;

namespace NetTopologySuite.Extensions.Coverages
{
    public class FeatureTypeConverter : TypeConverterBase<IFeature>
    {
        private IFeatureCoverage featureCoverage;

        [Aggregation]
        public IFeatureCoverage FeatureCoverage
        {
            private get { return featureCoverage; }
            set
            {
                featureCoverage = value;
                foreach (IVariable argument in featureCoverage.Arguments)
                {
                    if (typeof(IFeature).IsAssignableFrom(argument.ValueType))
                    {
                        SpecificType = argument.ValueType;
                        break;
                    }
                }
            }
        }

        public override Type ConvertedType
        {
            get { return SpecificType ?? typeof(IFeature); }
        }

        public override Type[] StoreTypes
        {
            get { return new[] { typeof(int), typeof(char[]), typeof(double), typeof(double) }; }
        }

        public override string[] VariableNames
        {
            get { return new[] { "feature_index", "feature_name", "x", "y" }; }
        }

        public override string[] VariableStandardNames
        {
            get { return new[] { "feature_index", "feature_name", FunctionAttributes.StandardNames.Longitude_X, FunctionAttributes.StandardNames.Latitude_Y }; }
        }

        public override string[] VariableUnits
        {
            get { return new[] { "-", "-", FunctionAttributes.StandardUnits.Long_X_Degr, FunctionAttributes.StandardUnits.Lat_Y_Degr }; }
        }

        public Type SpecificType { get; set; }

        public override IFeature ConvertFromStore(object source)
        {
            var featureIndex = (int)(((object[])source)[0]);

            var features = FeatureCoverage.Features;

            if (featureIndex >= features.Count)
            {
                throw new ArgumentException("Attempting to load coverage values for a feature not in Features of coverage");
            }

            return TypeUtils.Unproxy(features[featureIndex]);
        }

        public override object[] ConvertToStore(IFeature source)
        {
            var indexOfFeature = FeatureCoverage.Features.IndexOf(source);

            if (indexOfFeature == -1)
            {
                throw new ArgumentException("Attempting to save coverage values for feature not in Features of coverage");
            }

            var nameable = source as INameable;
            var name = nameable != null ? nameable.Name : "_nameless_";
            
            var x = 0.0;
            var y = 0.0;

            if (source.Geometry != null)
            {
                var centroid = source.Geometry.Centroid;
                x = centroid.X;
                y = centroid.Y;
            }

            return new object[] { indexOfFeature, ConvertToCharArray(name), x, y };
        }
    }
}