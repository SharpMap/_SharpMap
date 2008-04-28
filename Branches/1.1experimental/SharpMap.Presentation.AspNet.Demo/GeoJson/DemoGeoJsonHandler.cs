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
