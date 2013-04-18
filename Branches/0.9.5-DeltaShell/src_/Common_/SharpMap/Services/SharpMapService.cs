using SharpMap.Layers;

namespace SharpMap.Services
{
    public class SharpMapService
    {
        public static LabelLayer GetLabelLayer(ILayer layer)
        {
            Map map = layer.Map;

            if (map != null)
            {
                foreach (ILayer labelLayer in map.Layers)
                {
                    if (labelLayer is LabelLayer && labelLayer.DataSource != null && labelLayer.DataSource.Equals(layer.DataSource))
                    {
                        return (LabelLayer)labelLayer;
                    }
                }
            }

            return null;
        }
    }
}
