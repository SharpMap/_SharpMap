using System;
using System.Data;
using System.Configuration;
using System.Web;
using SharpMap.Presentation.AspNet.Impl;
using System.Drawing;
using SharpMap.Renderer;
using System.IO;
using System.Drawing.Imaging;

namespace SharpMap.Presentation.AspNet.Demo
{
    public class DemoMapHandler
        : MapHandlerBase<DemoWebMap<Image>, Image, BasicMapRequestConfig>
    {
        static DemoMapHandler()
        {
            IoCConfiguration.Ensure();
        }

        public override DemoWebMap<Image> CreateWebMap()
        {
            return new DemoWebMap<Image>(Context);
        }
    }
}
