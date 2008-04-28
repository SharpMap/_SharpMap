using System.Web;

namespace SharpMap.Presentation.AspNet.Demo.GeoJson
{
    public class DemoGeoJsonConfigFactory : IMapRequestConfigFactory<BasicMapRequestConfig>
    {
        #region IMapRequestConfigFactory<BasicMapRequestConfig> Members

        public BasicMapRequestConfig CreateConfig(HttpContext context)
        {
            BasicMapRequestConfig config = new BasicMapRequestConfig();
            config.Context = context;
            config.MimeType = "application/json";

            if (context.Request["BBOX"] != null)
                config.RealWorldBounds = SharpMap.Web.Wms.WmsServer.ParseBBOX(context.Request["BBOX"]);

            return config;
        }

        #endregion

        #region IMapRequestConfigFactory Members

        IMapRequestConfig IMapRequestConfigFactory.CreateConfig(HttpContext context)
        {
            return CreateConfig(context);
        }

        #endregion
    }
}
