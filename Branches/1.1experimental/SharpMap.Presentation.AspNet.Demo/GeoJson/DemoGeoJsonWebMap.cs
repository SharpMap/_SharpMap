using System.Web;
using SharpMap.Presentation.AspNet.Impl;
using SharpMap.Presentation.AspNet.IoC;
using SharpMap.Renderer;

namespace SharpMap.Presentation.AspNet.Demo.GeoJson
{
    public class DemoGeoJsonWebMap
        : WebMapBase
    {
        public DemoGeoJsonWebMap(HttpContext context)
            : base(context) { }

        protected override IMapCacheProvider CreateCacheProvider()
        {
            return Container.Instance.Resolve<IMapCacheProvider>();
        }

        protected override SharpMap.Renderer.IMapRenderer CreateMapRenderer()
        {
            return Container.Instance.Resolve<IMapRenderer>("geoJsonRenderer");
        }

        protected override IMapRequestConfigFactory CreateConfigFactory()
        {
            return Container.Instance.Resolve<IMapRequestConfigFactory>("geoJsonDemoConfigFactory");
        }

        public override void LoadLayers()
        {
            DemoMapSetupUtility.SetupMap(Context, Map);
        }

        public override void ConfigureRenderer()
        {
            IMapRendererConfig cfg = Container.Instance.Resolve<IMapRendererConfig>("geoJsonRendererConfig");
            if (cfg != null)
                cfg.ConfigureRenderer(MapRequestConfig, MapRenderer);
        }
    }
}
