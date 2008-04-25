using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using SharpMap.Presentation.AspNet.Impl;
using SharpMap.Presentation.AspNet.IoC;
using SharpMap.Renderer;

namespace SharpMap.Presentation.AspNet.Demo
{
    public static class IoCConfiguration
    {
        /// <summary>
        /// Call to ensure the IocContainer has been configured.
        /// </summary>
        public static void Ensure()
        { }

        static IoCConfiguration()
        {
            Container.Instance.RegisterType<IMapRenderer, DefaultImageRenderer>();
            Container.Instance.RegisterType<IMapRenderer<Image>, DefaultImageRenderer>();
            Container.Instance.RegisterType<IMapCacheProvider, NoCacheProvider>();
            Container.Instance.RegisterType<IMapCacheProvider<Image>, AspNetCacheProvider<Image>>();
            Container.Instance.RegisterType<IMapRequestConfigFactory, BasicMapConfigFactory>();
        }
    }
}
