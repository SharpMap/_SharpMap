using System;
using System.Data;
using System.Configuration;
using System.Web;
using SharpMap.Presentation.AspNet.Impl;
using SharpMap.Presentation.AspNet.IoC;
using SharpMap.Renderer;
using System.Xml;
using SharpMap.Renderers.ImageMap;
using SharpMap.Styles;

namespace SharpMap.Presentation.AspNet.Demo.ImageMap
{
    public class DemoImageWebMap : WebMapBase
    {
        public DemoImageWebMap(HttpContext context)
            : base(context)
        { }

        protected override IMapCacheProvider CreateCacheProvider()
        {
            return Container.Instance.Resolve<IMapCacheProvider>();
        }

        protected override SharpMap.Renderer.IMapRenderer CreateMapRenderer()
        {
            return Container.Instance.Resolve<IMapRenderer>("imageMapRenderer");
        }

        protected override IMapRequestConfigFactory CreateConfigFactory()
        {
            return Container.Instance.Resolve<IMapRequestConfigFactory>();
        }

        public override void LoadLayers()
        {
            DemoMapSetupUtility.SetupMap(Context, Map);
        }

        public override void ConfigureRenderer()
        {
            IMapRendererConfig cfg = Container.Instance.Resolve<IMapRendererConfig>("imageMapRendererConfig");
            if (cfg != null)
                cfg.ConfigureRenderer(MapRequestConfig, MapRenderer);
        }
    }
}
