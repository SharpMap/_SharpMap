using System.Drawing.Imaging;
using SharpMap.Renderer;

namespace SharpMap.Presentation.AspNet.WmsServer
{
    public class WmsRendererConfig
        : IMapRendererConfig<WmsMapRequestConfig, WmsRenderer>
    {

        #region IMapRendererConfig<WmsRequestConfig,WmsRenderer> Members

        public void ConfigureRenderer(WmsMapRequestConfig requestConfig, WmsRenderer renderer)
        {
            renderer.Context = requestConfig.Context;
            renderer.ServiceDescription = requestConfig.Description;
            renderer.RenderMode = requestConfig.WmsMode;

            if (requestConfig.WmsMode == WmsMode.Capabilites)
                return;

            ImageCodecInfo codecInfo = DefaultImageRenderer.FindCodec(requestConfig.MimeType);
            if (codecInfo == null)
            {
                WmsException.ThrowWmsException(string.Format("Invalid MimeType specified in FORMAT parameter. {0} not found", requestConfig.MimeType));
            }

            renderer.ImageCodec = codecInfo;
        }

        #endregion

        #region IMapRendererConfig Members

        public void ConfigureRenderer(IMapRequestConfig requestConfig, IMapRenderer renderer)
        {
            ConfigureRenderer((WmsMapRequestConfig)requestConfig, (WmsRenderer)renderer);
        }

        #endregion
    }
}
