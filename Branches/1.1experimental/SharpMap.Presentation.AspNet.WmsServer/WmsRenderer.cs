using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Web;
using System.Xml;
using SharpMap.Renderer;
using SharpMap.Web.Wms;

namespace SharpMap.Presentation.AspNet.WmsServer
{
    public class WmsRenderer
        : IMapRenderer<Image>, IMapRenderer<XmlDocument>
    {
        private HttpContext _context;
        internal HttpContext Context
        {
            get
            {
                return _context;
            }
            set
            {
                _context = value;
            }
        }

        public Capabilities.WmsServiceDescription ServiceDescription
        {
            get
            {
                return _capsRenderer.ServiceDescription;
            }
            set
            {
                _capsRenderer.ServiceDescription = value;
            }
        }

        public ImageCodecInfo ImageCodec
        {
            get
            {
                return _imageRenderer.ImageCodec;
            }
            set
            {
                _imageRenderer.ImageCodec = value;
            }
        }

        public EncoderParameters EncoderParams
        {
            get
            {
                return _imageRenderer.EncoderParams;
            }
            set
            {
                _imageRenderer.EncoderParams = value;
            }
        }

        public ImageFormat ImageFormat
        {
            get
            {
                return _imageRenderer.ImageFormat;
            }
            set
            {
                _imageRenderer.ImageFormat = value;
            }
        }

        private DefaultImageRenderer _imageRenderer = new DefaultImageRenderer();
        private InternalWmsCapabilitiesRenderer _capsRenderer = new InternalWmsCapabilitiesRenderer();


        private WmsMode _mode;
        public WmsMode RenderMode
        {
            get { return _mode; }
            set { _mode = value; }

        }

        XmlDocument IMapRenderer<XmlDocument>.Render(Map map, out string mimeType)
        {
            _capsRenderer.Context = this.Context;
            XmlDocument d = _capsRenderer.Render(map, out mimeType);
            OnRenderDone();
            return d;
        }


        public Image Render(Map map, out string mimeType)
        {
            Image im = _imageRenderer.Render(map, out mimeType);
            OnRenderDone();
            return im;
        }
        public event EventHandler RenderDone;

        public event EventHandler<LayerRenderedEventArgs> LayerRendered;

        Stream IMapRenderer.Render(Map map, out string mimeType)
        {
            _capsRenderer.Context = this.Context;
            Stream s = RenderMode == WmsMode.Map
                ? ((IMapRenderer)_imageRenderer).Render(map, out mimeType)
                : ((IMapRenderer)_capsRenderer).Render(map, out mimeType);
            OnRenderDone();
            return s;
        }


        private void OnRenderDone()
        {
            if (this.RenderDone != null)
                this.RenderDone(this, EventArgs.Empty);
        }

        private class InternalWmsCapabilitiesRenderer
            : IMapRenderer<XmlDocument>
        {
            HttpContext _context;

            public HttpContext Context
            {
                get { return _context; }
                set { _context = value; }
            }


            private Capabilities.WmsServiceDescription _serviceDescription;
            public Capabilities.WmsServiceDescription ServiceDescription
            {
                get
                {
                    return _serviceDescription;
                }
                set
                {
                    _serviceDescription = value;
                }
            }

            public XmlDocument Render(Map map, out string mimeType)
            {
                mimeType = "text/xml";
                return Capabilities.GetCapabilities(_context, map, ServiceDescription);
            }

            public event EventHandler RenderDone;

            public event EventHandler<LayerRenderedEventArgs> LayerRendered;

            Stream IMapRenderer.Render(Map map, out string mimeType)
            {
                XmlDocument d = this.Render(map, out mimeType);
                MemoryStream ms = new MemoryStream();
                d.Save(ms);
                ms.Position = 0;
                return ms;
            }
        }
    }
}
