using System.Configuration;
using System.Web;
using SharpMap.Layers;
using SharpMap.Presentation.AspNet.Impl;
using SharpMap.Styles;
using SharpMap.Renderer;
using System;
using SharpMap.Data.Providers;
using SharpMap.Rendering.Thematics;
using SharpMap.Data;
using SharpMap.Presentation.AspNet.IoC;

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
