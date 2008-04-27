using System.IO;
using System.Web;
using SharpMap.Presentation.AspNet.Impl;
using SharpMap.Presentation.AspNet.IoC;
using SharpMap.Renderer;

namespace SharpMap.Presentation.AspNet.WmsServer
{
    public abstract class WmsWebMapBase : WebMapBase
    {
        public WmsWebMapBase(HttpContext context)
            : base(context) { }

        protected override IMapCacheProvider CreateCacheProvider()
        {
            return Container.Instance.Resolve<IMapCacheProvider>("wmsServerCacheProvider");
        }

        protected override SharpMap.Renderer.IMapRenderer CreateMapRenderer()
        {
            return Container.Instance.Resolve<IMapRenderer>("wmsServerRenderer");
        }

        protected override IMapRequestConfigFactory CreateConfigFactory()
        {
            return Container.Instance.Resolve<IMapRequestConfigFactory>("wmsServerConfigFactory");
        }

        public override void ConfigureRenderer()
        {
            IMapRendererConfig cfg = Container.Instance.Resolve<IMapRendererConfig>("wmsRendererConfig");
            if (cfg != null)
                cfg.ConfigureRenderer(MapRequestConfig, MapRenderer);
        }

        public override Stream Render(out string mimeType)
        {
            try
            {
                return base.Render(out mimeType);
            }
            catch (WmsException ex)
            {
                mimeType = "text/xml";
                MemoryStream ms = new MemoryStream();

                StreamWriter sw = new StreamWriter(ms);
                sw.Write("<?xml version=\"1.0\" encoding=\"utf-8\" ?>\n");
                sw.Write("<ServiceExceptionReport version=\"1.3.0\" xmlns=\"http://www.opengis.net/ogc\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:schemaLocation=\"http://www.opengis.net/ogc http://schemas.opengis.net/wms/1.3.0/exceptions_1_3_0.xsd\">\n");
                sw.Write("<ServiceException");
                if (ex.WmsExceptionCode != WmsExceptionCode.NotApplicable)
                    sw.Write(" code=\"" + ex.WmsExceptionCode.ToString() + "\"");
                sw.Write(">" + ex.Message + "</ServiceException>\n");
                sw.Write("</ServiceExceptionReport>");
                sw.Flush();
                return ms;
            }
        }
    }
}
