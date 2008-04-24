using SharpMap.Presentation.AspNet.Impl;

namespace SharpMap.Presentation.AspNet.Demo
{
    public class DemoMapHandler
        : MapHandlerBase
    {

        public override IWebMap CreateWebMap()
        {
            return new DemoWebMap(Context);
        }
    }
}
