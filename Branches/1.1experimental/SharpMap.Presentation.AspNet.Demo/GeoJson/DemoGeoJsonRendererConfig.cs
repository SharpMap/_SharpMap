using System;
using System.Data;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using SharpMap.Renderers.GeoJson;
using SharpMap.Layers;
using SharpMap.Data;

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
