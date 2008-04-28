using System;
using SharpMap.Data;
using SharpMap.Layers;
using SharpMap.Renderers.GeoJson;

namespace SharpMap.Presentation.AspNet.Demo.GeoJson
{
    public class DemoGeoJsonRendererConfig
        : IMapRendererConfig<IMapRequestConfig, GeoJsonRenderer>
    {

        public void ConfigureRenderer(IMapRequestConfig requestConfig, GeoJsonRenderer renderer)
        {
            renderer.AttributeProviders.Add("name", new Func<SharpMap.Layers.ILayer, SharpMap.Data.FeatureDataRow, string>(
                delegate(ILayer lyr, FeatureDataRow fdr)
                {
                    return Convert.ToString(fdr[0]);
                }));
        }

        public void ConfigureRenderer(IMapRequestConfig requestConfig, SharpMap.Renderer.IMapRenderer renderer)
        {
            ConfigureRenderer(requestConfig, (GeoJsonRenderer)renderer);
        }

    }
}
