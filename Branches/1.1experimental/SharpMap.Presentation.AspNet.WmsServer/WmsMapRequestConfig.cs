using System.Collections.Generic;
using System.Drawing;
using System.Web;
using SharpMap.Geometries;
using SharpMap.Layers;
using SharpMap.Web.Wms;

namespace SharpMap.Presentation.AspNet.WmsServer
{
    public class WmsMapRequestConfig : IMapRequestConfig
    {
        private HttpContext _context;
        public HttpContext Context
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

        private WmsMode _mode;
        public WmsMode WmsMode
        {
            get
            {
                return _mode;
            }
            set
            {
                _mode = value;
            }
        }

        private Capabilities.WmsServiceDescription _description;

        public Capabilities.WmsServiceDescription Description
        {
            get { return _description; }
            set { _description = value; }
        }



        #region IMapRequestConfig Members
        private string _cacheKey;
        public string CacheKey
        {
            get
            {
                return _cacheKey;
            }
            set
            {
                _cacheKey = value;
            }
        }
        private string _mimeType;
        public string MimeType
        {
            get { return _mimeType; }
            internal set
            {
                _mimeType = value;
            }
        }

        private BoundingBox _rwb;
        public SharpMap.Geometries.BoundingBox RealWorldBounds
        {
            get
            {
                return _rwb;
            }
            set
            {
                _rwb = value;
            }
        }
        private Size _size;
        public System.Drawing.Size OutputSize
        {
            get
            {
                return _size;
            }
            set
            {
                _size = value;
            }
        }

        private Color _backgroundColor;
        public Color BackgroundColor
        {
            get { return _backgroundColor; }
            set { _backgroundColor = value; }
        }


        private ICollection<string> _enabledLayerNames;
        public ICollection<string> EnabledLayerNames
        {
            get { return _enabledLayerNames; }
            set { _enabledLayerNames = value; }
        }

        private string _crs;
        public string Crs
        {
            get { return _crs; }
            set { _crs = value; }
        }

        public void ConfigureMap(Map map)
        {
            if (WmsMode == WmsMode.Capabilites)
                return;


            if (this.Crs != "EPSG:" + map.Layers[0].SRID.ToString())
            {
                WmsException.ThrowWmsException(WmsExceptionCode.InvalidCRS, "CRS not supported");
            }


            map.Size = this.OutputSize;
            map.BackColor = this.BackgroundColor;
            map.ZoomToBox(this.RealWorldBounds);

            foreach (ILayer l in map.Layers)
            {
                l.Enabled = false;
            }

            foreach (string layerName in this.EnabledLayerNames)
                map.GetLayerByName(layerName).Enabled = true;


        }

        #endregion
    }
}
