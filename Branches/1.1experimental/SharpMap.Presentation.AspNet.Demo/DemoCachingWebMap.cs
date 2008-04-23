/*
 *  The attached / following is free software © 2008 Newgrove Consultants Limited, 
 *  www.newgrove.com; you can redistribute it and/or modify it under the terms 
 *  of the current GNU Lesser General Public License (LGPL) as published by and 
 *  available from the Free Software Foundation, Inc., 
 *  59 Temple Place, Suite 330, Boston, MA 02111-1307 USA: http://fsf.org/    
 *  This program is distributed without any warranty; 
 *  without even the implied warranty of merchantability or fitness for purpose.  
 *  See the GNU Lesser General Public License for the full details. 
 *  
 *  Author: John Diss 2008
 * 
 */
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
    public class DemoCachingWebMap<TOutput>
        : CachingWebMapBase<BasicMapRequestConfig, TOutput, AspNetCache<BasicMapRequestConfig, TOutput>>
    {
        public DemoCachingWebMap(HttpContext context)
            : base(context)
        { }

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

        protected override AspNetCache<BasicMapRequestConfig, TOutput> CreateCacheProvider()
        {
            return new AspNetCache<BasicMapRequestConfig, TOutput>(Context);
        }

        protected override IMapRequestConfigFactory<BasicMapRequestConfig> CreateConfigFactory()
        {
            return new BasicMapConfigFactory();
        }


        protected override IMapRenderer<TOutput> CreateMapRenderer()
        {
            return IoC.Container.Resolve<IMapRenderer<TOutput>>();
        }
    }
}
