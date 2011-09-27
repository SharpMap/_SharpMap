namespace SharpMap.Web.Wms.Handlers
{
    using System;
    using System.Drawing;
    using Data;
    using Geometries;
    using Layers;
    using Point = Geometries.Point;

    internal class GetFeatureInfo : AbstractHandler
    {
        private readonly int pixelSensitivity;
        private readonly WmsServer.InterSectDelegate intersectDelegate;

        public GetFeatureInfo(HandlerParams @params, GetFeatureInfoParams infoParams) : base(@params)
        {
            this.pixelSensitivity = infoParams.PixelSensitivity;
            this.intersectDelegate = infoParams.IntersectDelegate;
        }

        public override void Handle()
        {
            string layers = this.context.Request.Params["LAYERS"];
            string styles = this.context.Request.Params["STYLES"];
            string crs = this.context.Request.Params["CRS"];
            string queryBBOX = this.context.Request.Params["BBOX"];
            string queryWidth = this.context.Request.Params["WIDTH"];
            string queryHeight = this.context.Request.Params["HEIGHT"];
            string format = this.context.Request.Params["FORMAT"];
            string queryLayers = this.context.Request.Params["QUERY_LAYERS"];
            string infoFormat = this.context.Request.Params["INFO_FORMAT"];
            string xAxis = this.context.Request.Params["X"];
            string yAxis = this.context.Request.Params["Y"];
            string iParam = this.context.Request.Params["I"];
            string jParam = this.context.Request.Params["J"];

            if (layers == null)
            {
                WmsException.ThrowWmsException("Required parameter LAYERS not specified");
                return;
            }

            if (styles == null)
            {
                WmsException.ThrowWmsException("Required parameter STYLES not specified");
                return;
            }

            if (crs == null)
            {
                WmsException.ThrowWmsException("Required parameter CRS not specified");
                return;
            }

            if (!this.Check(String.Format("EPSG:{0}", this.map.Layers[0].SRID), crs))
            {
                WmsException.ThrowWmsException(WmsException.WmsExceptionCode.InvalidCRS, 
                    "CRS not supported");
                return;
            }

            if (queryBBOX == null)
            {
                WmsException.ThrowWmsException(WmsException.WmsExceptionCode.InvalidDimensionValue,
                    "Required parameter BBOX not specified");
                return;
            }

            if (queryWidth == null)
            {
                WmsException.ThrowWmsException(WmsException.WmsExceptionCode.InvalidDimensionValue,
                    "Required parameter WIDTH not specified");
                return;
            }

            if (queryHeight == null)
            {
                WmsException.ThrowWmsException(WmsException.WmsExceptionCode.InvalidDimensionValue,
                    "Required parameter HEIGHT not specified");
                return;
            }

            if (format == null)
            {
                WmsException.ThrowWmsException("Required parameter FORMAT not specified");
                return;
            }

            if (queryLayers == null)
            {
                WmsException.ThrowWmsException("Required parameter QUERY_LAYERS not specified");
                return;
            }

            if (infoFormat == null)
            {
                WmsException.ThrowWmsException("Required parameter INFO_FORMAT not specified");
                return;
            }

            //parameters X&Y are not part of the 1.3.0 specification, but are included for backwards compatability with 1.1.1 
            // (OpenLayers likes it when used together with 1.1.1 services)            
            if (xAxis == null && iParam == null)
            {
                WmsException.ThrowWmsException("Required parameter I not specified");
                return;
            }

            if (yAxis == null && jParam == null)
            {
                WmsException.ThrowWmsException("Required parameter J not specified");
                return;
            }

            try
            {
                //sets the map size to the size of the client in order to calculate the coordinates of the projection of the client
                this.map.Size = new Size(Convert.ToInt16(queryWidth), Convert.ToInt16(queryHeight));
            }
            catch
            {
                WmsException.ThrowWmsException("Invalid parameters for HEIGHT or WITDH");
                return;
            }

            //sets the boundingbox to the boundingbox of the client in order to calculate the coordinates of the projection of the client
            BoundingBox bbox = this.ParseBBOX(queryBBOX);
            if (bbox == null)
            {
                WmsException.ThrowWmsException("Invalid parameter BBOX");
                return;
            }

            this.map.ZoomToBox(bbox);
            //sets the point clicked by the client
            Single x = 0;
            Single y = 0;
            //tries to set the x to the Param I, if the client send an X, it will try the X, if both fail, exception is thrown
            if (xAxis != null)
            {
                try { x = Convert.ToSingle(xAxis); }
                catch { WmsException.ThrowWmsException("Invalid parameters for I"); }
            }
            if (iParam != null)
            {
                try { x = Convert.ToSingle(iParam); }
                catch { WmsException.ThrowWmsException("Invalid parameters for I"); }
            }
            //same procedure for J (Y)
            if (yAxis != null)
            {
                try { y = Convert.ToSingle(yAxis); }
                catch { WmsException.ThrowWmsException("Invalid parameters for J"); }
            }
            if (jParam != null)
            {
                try { y = Convert.ToSingle(jParam); }
                catch { WmsException.ThrowWmsException("Invalid parameters for J"); }
            }

            Point p = this.map.ImageToWorld(new PointF(x, y));
            int fc;
            try
            {
                fc = Convert.ToInt16(this.context.Request.Params["FEATURE_COUNT"]);
                if (fc < 1)
                    fc = 1;
            }
            catch { fc = 1; }

            String vstr = "GetFeatureInfo results: \n";
            string[] requestLayers = queryLayers.Split(new[] { ',' });
            foreach (string item in requestLayers)
            {
                bool found = false;

                foreach (ILayer mapLayer in this.map.Layers)
                {
                    if (!String.Equals(mapLayer.LayerName, item,
                         StringComparison.InvariantCultureIgnoreCase))
                        continue;

                    found = true;

                    if (!(mapLayer is ICanQueryLayer))
                        continue;

                    ICanQueryLayer layer = mapLayer as ICanQueryLayer;
                    bool enabled = layer.Enabled && layer.IsQueryEnabled;
                    if (!enabled)
                        continue;

                    Single queryBoxMinX = x - (this.pixelSensitivity);
                    Single queryBoxMinY = y - (this.pixelSensitivity);
                    Single queryBoxMaxX = x + (this.pixelSensitivity);
                    Single queryBoxMaxY = y + (this.pixelSensitivity);
                    Point minXY = this.map.ImageToWorld(new PointF(queryBoxMinX, queryBoxMinY));
                    Point maxXY = this.map.ImageToWorld(new PointF(queryBoxMaxX, queryBoxMaxY));
                    BoundingBox queryBox = new BoundingBox(minXY, maxXY);
                    FeatureDataSet fds = new FeatureDataSet();
                    layer.ExecuteIntersectionQuery(queryBox, fds);
                    if (this.intersectDelegate != null)
                        fds.Tables[0] = this.intersectDelegate(fds.Tables[0], queryBox);

                    if (fds.Tables.Count != 0)
                    {
                        if (fds.Tables[0].Rows.Count != 0)
                        {
                            //if featurecount < fds...count, select smallest bbox, because most likely to be clicked
                            vstr = String.Format("{0}\n Layer: '{1}'\n Featureinfo:\n", vstr, item);
                            int[] keys = new int[fds.Tables[0].Rows.Count];
                            double[] area = new double[fds.Tables[0].Rows.Count];
                            for (int l = 0; l < fds.Tables[0].Rows.Count; l++)
                            {
                                FeatureDataRow fdr = fds.Tables[0].Rows[l] as FeatureDataRow;
                                area[l] = fdr.Geometry.GetBoundingBox().GetArea();
                                keys[l] = l;
                            }
                            Array.Sort(area, keys);
                            if (fds.Tables[0].Rows.Count < fc)
                                fc = fds.Tables[0].Rows.Count;
                            for (int k = 0; k < fc; k++)
                            {
                                for (int j = 0; j < fds.Tables[0].Rows[keys[k]].ItemArray.Length; j++)
                                    vstr = String.Format("{0} '{1}'", vstr, fds.Tables[0].Rows[keys[k]].ItemArray[j].ToString());
                                if ((k + 1) < fc)
                                    vstr = String.Format("{0},\n", vstr);
                            }
                        }
                        else vstr = String.Format("{0}\nSearch returned no results on layer: {1} ", vstr, item);
                    }
                    else vstr = String.Format("{0}\nSearch returned no results on layer: {1}", vstr, item);
                }
                if (found)
                    continue;

                WmsException.ThrowWmsException(WmsException.WmsExceptionCode.LayerNotDefined,
                    String.Format("Unknown layer '{0}'", item));
                return;
            }

            this.context.Response.Clear();
            this.context.Response.ContentType = "text/plain";
            this.context.Response.Charset = "windows-1252";
            this.context.Response.Write(vstr);
            this.context.Response.Flush();
            this.context.Response.End();
        }
    }
}
