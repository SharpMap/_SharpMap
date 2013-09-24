using GeoAPI.Extensions.Feature;
using SharpMap.Api;
using SharpMap.Api.Editors;
using SharpMap.Editors;
using SharpMap.Layers;

namespace SharpMap.UI.Tools
{
    public class CoverageProfileEditor : FeatureEditor
    {
        public override IFeatureInteractor CreateInteractor(ILayer layer, IFeature feature)
        {
            return new CoverageProfileInteractor(layer, feature, ((VectorLayer)layer).Style, null);
        }
    }
}