using System.Configuration;
using System.Web;
using SharpMap.Data;
using SharpMap.Data.Providers;
using SharpMap.Layers;
using SharpMap.Presentation.AspNet.Impl;
using SharpMap.Presentation.AspNet.IoC;
using SharpMap.Renderer;
using SharpMap.Rendering.Thematics;

namespace SharpMap.Presentation.AspNet.Demo
{
    public class DemoWebMap
        : WebMapBase
    {

        protected override IMapRenderer CreateMapRenderer()
        {
            return Container.Instance.Resolve<IMapRenderer>();
        }

        protected override IMapRequestConfigFactory CreateConfigFactory()
        {
            return Container.Instance.Resolve<IMapRequestConfigFactory>();
        }

        protected override IMapCacheProvider CreateCacheProvider()
        {
            return Container.Instance.Resolve<IMapCacheProvider>();
        }

        public override void LoadLayers()
        {
            VectorLayer l = new VectorLayer(
                    "layer1",
                    new ShapeFile(Context.Server.MapPath(ConfigurationManager.AppSettings["shpfilePath"])));


            l.Theme = new CustomTheme(
                new CustomTheme.GetStyleMethod(
                    delegate(FeatureDataRow fdr)
                    {
                        return RandomStyle.RandomVectorStyleNoSymbols();
                    }
                ));
            Map.Layers.Add(l);
        }

        public DemoWebMap(HttpContext context)
            : base(context)
        { }



    }
}
