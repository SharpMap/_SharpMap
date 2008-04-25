using SharpMap.Presentation.AspNet.Impl;

namespace SharpMap.Presentation.AspNet.Demo
{
    public class DemoMapHandler
        : AsyncMapHandlerBase
    {

        public override IWebMap CreateWebMap()
        {
            return new DemoWebMap(Context);
        }
    }
}
