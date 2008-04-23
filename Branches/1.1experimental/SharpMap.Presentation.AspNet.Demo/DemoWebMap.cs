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

namespace SharpMap.Presentation.AspNet.Demo
{
    public class DemoWebMap<TOutput>
        : WebMapBase<BasicMapRequestConfig, TOutput>
    {

        protected override IMapRenderer<TOutput> CreateMapRenderer()
        {
            return IoC.Container.Resolve<IMapRenderer<TOutput>>();
        }

        protected override IMapRequestConfigFactory<BasicMapRequestConfig> CreateConfigFactory()
        {
            return new BasicMapConfigFactory();
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
