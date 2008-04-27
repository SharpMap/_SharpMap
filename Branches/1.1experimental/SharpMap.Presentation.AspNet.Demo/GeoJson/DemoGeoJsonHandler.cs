using System;
using System.Data;
using System.Configuration;
using System.Web;
using System.Web.Security;
using SharpMap.Presentation.AspNet.Impl;

namespace SharpMap.Presentation.AspNet.Demo.GeoJson
{
    public class DemoGeoJsonHandler
        : AsyncMapHandlerBase
    {
        public override IWebMap CreateWebMap()
        {
            return new DemoGeoJsonWebMap(Context);
        }
    }
}
