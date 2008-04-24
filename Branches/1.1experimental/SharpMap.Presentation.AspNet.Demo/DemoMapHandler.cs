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
        : MapHandlerBase
    {

        public override IWebMap CreateWebMap()
        {
            return new DemoWebMap(Context);
        }
    }
}
