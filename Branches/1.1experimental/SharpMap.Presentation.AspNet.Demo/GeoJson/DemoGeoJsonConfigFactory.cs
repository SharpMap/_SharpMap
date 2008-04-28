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
