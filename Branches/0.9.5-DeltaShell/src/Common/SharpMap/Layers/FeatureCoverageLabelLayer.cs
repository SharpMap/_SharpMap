using GeoAPI.Extensions.Coverages;
using GeoAPI.Extensions.Feature;

namespace SharpMap.Layers
{
    public class FeatureCoverageLabelLayer : LabelLayer
    {
        public virtual IFeatureCoverage Coverage { get; set; } // TODO: should be read-only property returning casted DataSoruce

        public override object Clone()
        {
            var clone = (FeatureCoverageLabelLayer) base.Clone();
            clone.Coverage = Coverage;
            return clone;
        }

        protected override string GetText(IFeature feature)
        {
            int featureIndex = this.Coverage.FeatureVariable.Values.IndexOf(feature);
            double value = (double) this.Coverage.Components[0].Values[featureIndex];

            return value.ToString("F3");
        }
    }
}
