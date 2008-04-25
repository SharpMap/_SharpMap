using System.Drawing;
using SharpMap.Presentation.AspNet.Impl;
using SharpMap.Presentation.AspNet.IoC;
using SharpMap.Renderer;
using System.IO;
using System;

namespace SharpMap.Presentation.AspNet.Demo
{
    public static class IoCConfiguration
    {
        /// <summary>
        /// Call to ensure the IocContainer has been configured.
        /// </summary>
        public static void Configure()
        { }

        static IoCConfiguration()
        {
            Container.Instance.RegisterType<IMapRenderer, DefaultImageRenderer>();
            Container.Instance.RegisterType<IMapRenderer<Image>, DefaultImageRenderer>();
            Container.Instance.RegisterType<IMapCacheProvider, NoCacheProvider>();
            Container.Instance.RegisterType<IMapCacheProvider<Image>, AspNetCacheProvider<Image>>();
            Container.Instance.RegisterType<IMapRequestConfigFactory, BasicMapConfigFactory>();
            Container.Instance.RegisterType<IMapRendererConfig, DefaultImageRendererConfig>();

            Container.Instance.RegisterInstance<Func<Stream, Image>>(
                new Func<Stream, Image>(
                    delegate(Stream s)
                    {
                        return new Bitmap(s);
                    }
                )
            );
        }
    }
}
