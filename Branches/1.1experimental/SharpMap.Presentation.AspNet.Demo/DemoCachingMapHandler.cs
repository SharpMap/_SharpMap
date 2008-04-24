using System;
using System.Data;
using System.Configuration;
using System.Web;
using SharpMap.Presentation.AspNet.Impl;
using System.Drawing;

namespace SharpMap.Presentation.AspNet.Demo
{
    public class DemoCachingMapHandler
        : DemoMapHandler
    {

        public override IWebMap CreateWebMap()
        {
            return new DemoCachingWebMap(Context);
        }
    }
}
