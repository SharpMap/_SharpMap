namespace SharpMap.Web.Wms.Handlers
{
    using System;

    internal class GetFeatureInfoParams 
    {                
        public GetFeatureInfoParams(int pixelSensitivity, WmsServer.InterSectDelegate intersectDelegate)
        {
            if (intersectDelegate == null) { /* can be null */}

            this.pixelSensitivity = Math.Max(0, pixelSensitivity);
            this.intersectDelegate = intersectDelegate;
        }

        private readonly int pixelSensitivity;

        public int PixelSensitivity
        {
            get { return this.pixelSensitivity; }
        }

        private readonly WmsServer.InterSectDelegate intersectDelegate;

        public WmsServer.InterSectDelegate IntersectDelegate
        {
            get { return this.intersectDelegate; }
        }
    }
}