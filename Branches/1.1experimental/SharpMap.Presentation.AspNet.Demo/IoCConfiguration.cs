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


#warning Unity causes a SynchronizationLockException on start up - only with the dev web server - iis is ok. Appears to work after. keep track of unity releases!
            try
            {
                Container.Instance.RegisterInstance<Func<Image, Stream>>(
                    delegate(Image im)
                    {
                        MemoryStream ms = new MemoryStream();
                        im.Save(ms, ImageFormat.Png);
                        return ms;
                    });
            }
            catch (SynchronizationLockException ex) { }
            { }
        }
    }
}
