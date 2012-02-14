using System;
using DelftTools.Functions;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Extensions.Feature;
using GeoAPI.Extensions.Networks;

namespace NetTopologySuite.Extensions.Coverages
{
    public class FeatureTypeConverter : TypeConverterBase<IFeature>
    {
        private IFeatureCoverage featureCoverage;

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
            get { return new[] { typeof(int) }; }
        }

        public override string[] VariableNames
        {
            get { return new[] { "feature_index" }; }
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

            return features[featureIndex];
        }

        public override object[] ConvertToStore(IFeature source)
        {
            var indexOfFeature = FeatureCoverage.Features.IndexOf(source);

            if (indexOfFeature == -1)
            {
                throw new ArgumentException("Attempting to save coverage values for feature not in Features of coverage");
            }

            return new object[] { indexOfFeature };
        }
    }
}