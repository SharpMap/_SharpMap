using System;
using System.Data;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using SharpMap.Renderers.ImageMap;
using SharpMap.Renderer;
using SharpMap.Styles;
using SharpMap.Layers;
using SharpMap.Data;

namespace SharpMap.Presentation.AspNet.Demo.ImageMap
{
    public class DemoImageMapRendererConfig
        : IMapRendererConfig<IMapRequestConfig, ImageMapRenderer>
    {

        public void ConfigureRenderer(IMapRequestConfig requestConfig, ImageMapRenderer renderer)
        {
            renderer.Context = requestConfig.Context;  
            renderer.ImageMapStyle = new ImageMapStyle(0, 1000000, true);
            renderer.AttributeProviders
                .Add("onmouseover", new Func<ILayer, FeatureDataRow, string>(
                    delegate(ILayer l, FeatureDataRow fdr)
                    {
                        return string.Format("javascript:showInfo('{0}');", (string)fdr[0]);
                    }
                ));
        }

        public void ConfigureRenderer(IMapRequestConfig requestConfig, IMapRenderer renderer)
        {
            ConfigureRenderer(requestConfig, (ImageMapRenderer)renderer);
        }

    }
}
