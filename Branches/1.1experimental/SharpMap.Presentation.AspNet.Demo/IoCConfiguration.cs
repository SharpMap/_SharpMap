using System;
using System.Data;
using System.Configuration;
using System.Web;
using System.IO;
using SharpMap.Renderer;
using System.Drawing;
using System.Drawing.Imaging;

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
            IoC.Container.Register<IMapRenderer<Image>>(typeof(DefaultImageRenderer));
            IoC.Container.RegisterObject<Func<Image, Stream>>(
                delegate(Image im)
                {
                    MemoryStream ms = new MemoryStream();
                    im.Save(ms, ImageFormat.Png);
                    return ms;
                });
        }
    }
}
