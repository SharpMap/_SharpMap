using System;
using System.Collections.Generic;
using System.Drawing;
using System.Web;
using SharpMap.Web.Wms;

namespace SharpMap.Presentation.AspNet.WmsServer
{
    public abstract class WmsConfigFactoryBase
        : IMapRequestConfigFactory<WmsMapRequestConfig>
    {
        public abstract Capabilities.WmsServiceDescription Description
        {
            get;
        }

        public virtual WmsMapRequestConfig CreateConfig(HttpContext context)
        {
            WmsMapRequestConfig config = new WmsMapRequestConfig();
            config.Context = context;

            bool ignorecase = true;

            if (context.Request.Params["REQUEST"] == null)
                WmsException.ThrowWmsException("Required parameter REQUEST not specified");

            //Check if version is supported
            if (context.Request.Params["VERSION"] != null)
            {
                if (String.Compare(context.Request.Params["VERSION"], "1.3.0", ignorecase) != 0)
                    WmsException.ThrowWmsException("Only version 1.3.0 supported");
            }
            else //Version is mandatory if REQUEST!=GetCapabilities. Check if this is a capabilities request, since VERSION is null
            {
                if (String.Compare(context.Request.Params["REQUEST"], "GetCapabilities", ignorecase) != 0)
                    WmsException.ThrowWmsException("VERSION parameter not supplied");
            }


            if (String.Compare(context.Request.Params["REQUEST"], "GetCapabilities", ignorecase) == 0)
            {
                //Service parameter is mandatory for GetCapabilities request
                if (context.Request.Params["SERVICE"] == null)
                    WmsException.ThrowWmsException("Required parameter SERVICE not specified");

                if (String.Compare(context.Request.Params["SERVICE"], "WMS") != 0)
                    WmsException.ThrowWmsException("Invalid service for GetCapabilities Request. Service parameter must be 'WMS'");

                config.WmsMode = WmsMode.Capabilites;
                config.MimeType = "text/xml";
                config.Description = this.Description;
                return config;


            }

            if (String.Compare(context.Request.Params["REQUEST"], "GetMap", ignorecase) != 0)
                WmsException.ThrowWmsException("Invalid REQUEST parameter");

            config.WmsMode = WmsMode.Map;



            //Check for required parameters
            if (context.Request.Params["LAYERS"] == null)
                WmsException.ThrowWmsException("Required parameter LAYERS not specified");
            config.EnabledLayerNames = new List<string>(context.Request.Params["LAYERS"].Split(','));

            if (context.Request.Params["STYLES"] == null)
                WmsException.ThrowWmsException("Required parameter STYLES not specified");

            if (context.Request.Params["CRS"] == null)
                WmsException.ThrowWmsException("Required parameter CRS not specified");
            config.Crs = context.Request.Params["CRS"];

            if (context.Request.Params["BBOX"] == null)
                WmsException.ThrowWmsException(WmsExceptionCode.InvalidDimensionValue, "Required parameter BBOX not specified");

            if (context.Request.Params["WIDTH"] == null)
                WmsException.ThrowWmsException(WmsExceptionCode.InvalidDimensionValue, "Required parameter WIDTH not specified");

            if (context.Request.Params["HEIGHT"] == null)
                WmsException.ThrowWmsException(WmsExceptionCode.InvalidDimensionValue, "Required parameter HEIGHT not specified");

            if (context.Request.Params["FORMAT"] == null)
                WmsException.ThrowWmsException("Required parameter FORMAT not specified");

            Color bkgnd = Color.White;

            //Set background color of map
            if (String.Compare(context.Request.Params["TRANSPARENT"], "TRUE", ignorecase) == 0)
                bkgnd = Color.Transparent;
            else if (context.Request.Params["BGCOLOR"] != null)
            {
                try { bkgnd = ColorTranslator.FromHtml(context.Request.Params["BGCOLOR"]); }
                catch { WmsException.ThrowWmsException("Invalid parameter BGCOLOR"); };
            }


            config.BackgroundColor = bkgnd;
            config.MimeType = context.Request.Params["FORMAT"];

            //Parse map size
            int width = 0;
            int height = 0;

            if (!int.TryParse(context.Request.Params["WIDTH"], out width))
                WmsException.ThrowWmsException(WmsExceptionCode.InvalidDimensionValue, "Invalid parameter WIDTH");
            else if (Description.MaxWidth > 0 && width > Description.MaxWidth)
                WmsException.ThrowWmsException(WmsExceptionCode.OperationNotSupported, "Parameter WIDTH too large");

            if (!int.TryParse(context.Request.Params["HEIGHT"], out height))
                WmsException.ThrowWmsException(WmsExceptionCode.InvalidDimensionValue, "Invalid parameter HEIGHT");
            else if (Description.MaxHeight > 0 && height > Description.MaxHeight)
                WmsException.ThrowWmsException(WmsExceptionCode.OperationNotSupported, "Parameter HEIGHT too large");


            config.OutputSize = new System.Drawing.Size(width, height);

            if (context.Request.Params["BBOX"] == null)
            {
                WmsException.ThrowWmsException("Invalid parameter BBOX");
            }

            config.RealWorldBounds = SharpMap.Web.Wms.WmsServer.ParseBBOX(context.Request.Params["BBOX"]);
            config.Description = this.Description;

            return config;

        }


        IMapRequestConfig IMapRequestConfigFactory.CreateConfig(System.Web.HttpContext context)
        {
            return CreateConfig(context);
        }
    }
}
