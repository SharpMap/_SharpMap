using System;
using System.Data;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using SharpMap.Renderer;
using System.Drawing.Imaging;
using System.Diagnostics;

namespace SharpMap.Presentation.AspNet.Demo
{
    public class DefaultImageRendererConfig
        : IMapRendererConfig<BasicMapRequestConfig, DefaultImageRenderer>
    {


        public void ConfigureRenderer(BasicMapRequestConfig requestConfig, DefaultImageRenderer renderer)
        {
            ImageCodecInfo codecInfo = DefaultImageRenderer.FindCodec(requestConfig.MimeType);
            if (codecInfo == null)
            {
                Trace.WriteLine(string.Format("ImageCodecInfo for MimeType {0} not found. Using Defaults.", requestConfig.MimeType));
                return;
            }

            renderer.ImageCodec = codecInfo;

            ///as more properties get added to the BasicMapRequestConfig we can start creating encoder params
            ///to allow different image compression settings etc
        }

        public void ConfigureRenderer(IMapRequestConfig requestConfig, IMapRenderer renderer)
        {
            ConfigureRenderer((BasicMapRequestConfig)requestConfig, (DefaultImageRenderer)renderer);
        }

    }
}
