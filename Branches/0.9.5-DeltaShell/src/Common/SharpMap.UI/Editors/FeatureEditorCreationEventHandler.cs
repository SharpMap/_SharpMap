using GeoAPI.Extensions.Feature;
using SharpMap.Layers;
using SharpMap.Styles;

namespace SharpMap.UI.Editors
{
    public delegate IFeatureEditor FeatureEditorCreationEventHandler(ILayer layer, IFeature feature, VectorStyle vectorStyle);
}