using SharpMap.Presentation.AspNet.Impl;

namespace SharpMap.Presentation.AspNet.Demo.GeoJson
{
    public class DemoGeoJsonMapHandler
        : AsyncMapHandlerBase
    {
        public override IWebMap CreateWebMap()
        {
            return new DemoGeoJsonWebMap(Context);
        }
    }
}
