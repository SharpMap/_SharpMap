using System;
using System.Data;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using SharpMap.Presentation.AspNet.Impl;

namespace SharpMap.Presentation.AspNet.Demo.ImageMap
{
    public class DemoImageMapHandler
        : AsyncMapHandlerBase
    {
        public override IWebMap CreateWebMap()
        {
            return new DemoImageWebMap(Context);
        }
    }
}
