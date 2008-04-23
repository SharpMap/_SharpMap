using System;
using System.Data;
using System.Configuration;
using System.Web;
using SharpMap.Presentation.AspNet.Impl;
using System.Drawing;

namespace SharpMap.Presentation.AspNet.Demo
{
    public class DemoCachingMapHandler
        : MapHandlerBase<DemoCachingWebMap<Image>, Image, BasicMapRequestConfig>
    {
        static DemoCachingMapHandler()
        {
            IoCConfiguration.Ensure();

        }

        public override DemoCachingWebMap<Image> CreateWebMap()
        {
            return new DemoCachingWebMap<Image>(Context);
        }
    }
}
