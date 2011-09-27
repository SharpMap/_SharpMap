namespace SharpMap.Web.Wms.Handlers
{
    using System;
    using System.Drawing.Imaging;
    using System.Globalization;
    using System.Linq;
    using System.Web;
    using Geometries;

    internal abstract class AbstractHandler : IWmsHandler
    {
        protected readonly HttpContext context;

        protected readonly Map map;

        protected readonly Capabilities.WmsServiceDescription description;

        private readonly bool ignorecase;

        protected AbstractHandler(HandlerParams @params)
        {
            if (@params == null)
                throw new ArgumentNullException("params");

            this.context = @params.Context;
            this.map = @params.Map;
            this.description = @params.Description;
            this.ignorecase = @params.IgnoreCase;
        }

        protected bool Check(string expected, string actual)
        {
            return String.Compare(actual, expected, this.ignorecase) == 0;
        }

        /// <summary>
        /// Used for setting up output format of image file
        /// </summary>
        protected ImageCodecInfo GetEncoderInfo(String mimeType)
        {
            return ImageCodecInfo.GetImageEncoders().FirstOrDefault(e => e.MimeType == mimeType);
        }

        /// <summary>
        /// Parses a boundingbox string to a boundingbox geometry from the format minx,miny,maxx,maxy. Returns null if the format is invalid
        /// </summary>
        /// <param name="strBBOX">string representation of a boundingbox</param>
        /// <returns>Boundingbox or null if invalid parameter</returns>
        protected BoundingBox ParseBBOX(string strBBOX)
        {
            string[] strVals = strBBOX.Split(new[] { ',' });
            if (strVals.Length != 4)
                return null;

            double minx;
            double miny;
            double maxx;
            double maxy;
            if (!Double.TryParse(strVals[0], NumberStyles.Float, Map.NumberFormatEnUs, out minx))
                return null;
            if (!Double.TryParse(strVals[2], NumberStyles.Float, Map.NumberFormatEnUs, out maxx))
                return null;
            if (maxx < minx)
                return null;

            if (!Double.TryParse(strVals[1], NumberStyles.Float, Map.NumberFormatEnUs, out miny))
                return null;
            if (!Double.TryParse(strVals[3], NumberStyles.Float, Map.NumberFormatEnUs, out maxy))
                return null;
            if (maxy < miny)
                return null;

            return new BoundingBox(minx, miny, maxx, maxy);
        }

        public abstract void Handle();        
    }
}
