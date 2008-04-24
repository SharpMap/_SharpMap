
namespace SharpMap.Presentation.AspNet.Demo
{
    public class DemoCachingMapHandler
        : DemoMapHandler
    {

        public override IWebMap CreateWebMap()
        {
            return new DemoCachingWebMap(Context);
        }
    }
}
