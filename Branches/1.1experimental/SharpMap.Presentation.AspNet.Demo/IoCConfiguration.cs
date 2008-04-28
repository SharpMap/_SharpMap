/*
 *  The attached / following is part of SharpMap.Presentation.AspNet
 *  SharpMap.Presentation.AspNet is free software © 2008 Newgrove Consultants Limited, 
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
using System;
using System.Drawing;
using System.IO;
using SharpMap.Presentation.AspNet.Demo.GeoJson;
using SharpMap.Presentation.AspNet.Demo.ImageMap;
using SharpMap.Presentation.AspNet.Demo.Wms;
using SharpMap.Presentation.AspNet.Impl;
using SharpMap.Presentation.AspNet.IoC;
using SharpMap.Presentation.AspNet.WmsServer;
using SharpMap.Renderer;
using SharpMap.Renderers.GeoJson;
using SharpMap.Renderers.ImageMap;

namespace SharpMap.Presentation.AspNet.Demo
{
    public static class IoCConfiguration
    {
        /// <summary>
        /// Call to ensure the IocContainer has been configured.
        /// No user code is actually run but it ensures that the static constructor has been called.
        /// </summary>
        public static void Configure()
        { }

        /// <summary>
        /// This is where implementations for each expected interface are set.
        /// By changing the objects here completely different 
        /// combinations can be tested without trawling through all the code.
        /// </summary>
        static IoCConfiguration()
        {
            ConfigureCommon();
            ConfigureCachingDemo();
            ConfigureWmsServerDemo();
            ConfigureImageMapDemo();
            ConfigureGeoJsonDemo();

            Container.Instance.RegisterInstance<Func<Stream, Image>>(
                new Func<Stream, Image>(
                    delegate(Stream s)
                    {
                        return new Bitmap(s);
                    }
                )
            );
        }

        private static void ConfigureGeoJsonDemo()
        {
            Container.Instance.RegisterType<IMapRenderer, GeoJsonRenderer>("geoJsonRenderer");
            Container.Instance.RegisterType<IMapRendererConfig, DemoGeoJsonRendererConfig>("geoJsonRendererConfig");
            Container.Instance.RegisterType<IMapRequestConfigFactory, DemoGeoJsonConfigFactory>("geoJsonDemoConfigFactory");
        }

        static void ConfigureCommon()
        {
            Container.Instance.RegisterType<IMapRenderer, DefaultImageRenderer>();
            Container.Instance.RegisterType<IMapCacheProvider, NoCacheProvider>();
            Container.Instance.RegisterType<IMapRequestConfigFactory, BasicMapRequestConfigFactory>();
            Container.Instance.RegisterType<IMapRendererConfig, DefaultImageRendererConfig>();
        }

        static void ConfigureImageMapDemo()
        {
            Container.Instance.RegisterType<IMapRenderer, ImageMapRenderer>("imageMapRenderer");
            Container.Instance.RegisterType<IMapRendererConfig, DemoImageMapRendererConfig>("imageMapRendererConfig");
        }


        static void ConfigureCachingDemo()
        {
            Container.Instance.RegisterType<IMapCacheProvider, AspNetCacheProvider>("cachingDemoCacheProvider");
        }


        static void ConfigureWmsServerDemo()
        {
            Container.Instance.RegisterType<IMapCacheProvider, NoCacheProvider>("wmsServerCacheProvider");
            Container.Instance.RegisterType<IMapRenderer, WmsRenderer>("wmsServerRenderer");
            Container.Instance.RegisterType<IMapRequestConfigFactory, DemoWmsConfigFactory>("wmsServerConfigFactory");
            Container.Instance.RegisterType<IMapRendererConfig, WmsRendererConfig>("wmsRendererConfig");
        }
    }
}
