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
