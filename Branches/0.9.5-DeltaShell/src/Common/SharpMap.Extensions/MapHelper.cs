using System.Collections.Generic;
using SharpMap.Layers;

namespace SharpMap.Extensions
{
    public class MapHelper
    {
        public static IEnumerable<ILayer> GetAllMapLayers(IEnumerable<ILayer> layers, bool includeGroupLayers)
        {
            foreach (ILayer layer in layers)
            {
                if (layer is LayerGroup)
                {
                    if (includeGroupLayers)
                    {
                        yield return layer;
                    }
                    IEnumerable<ILayer> childLayers = GetAllMapLayers(((LayerGroup)layer).Layers, includeGroupLayers);
                    foreach (ILayer childLayer in childLayers)
                    {
                        yield return childLayer;
                    }
                }
                else
                {
                    yield return layer;
                }
            }
        }
    }
}
