namespace SharpMap.Web.Wms.Handlers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Linq;
    using Data;
    using Geometries;
    using Layers;
    using ProjNet.CoordinateSystems.Transformations;
    using Rendering.GeoJSON;

    internal class GetMap : AbstractHandler
    {
        public GetMap(HandlerParams @params) : base(@params) { }

        public override void Handle()
        {
            string queryLayers = this.context.Request.Params["LAYERS"];
            string styles = this.context.Request.Params["STYLES"];
            string crs = this.context.Request.Params["CRS"];
            string queryBBOX = this.context.Request.Params["BBOX"];
            string queryWidth = this.context.Request.Params["WIDTH"];
            string queryHeight = this.context.Request.Params["HEIGHT"];
            string format = this.context.Request.Params["FORMAT"];
            string transparent = this.context.Request.Params["TRANSPARENT"];
            string background = this.context.Request.Params["BGCOLOR"];

            if (queryLayers == null)
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
                WmsException.ThrowWmsException(WmsException.WmsExceptionCode.InvalidCRS, "CRS not supported");
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

            //Set background color of map
            if (!this.Check("TRUE", transparent))
            {
                if (background != null)
                {
                    try { this.map.BackColor = ColorTranslator.FromHtml(background); }
                    catch
                    {
                        WmsException.ThrowWmsException("Invalid parameter BGCOLOR");
                        return;
                    }
                }
                else this.map.BackColor = Color.White;
            }
            else this.map.BackColor = Color.Transparent;

            //Parse map size
            int width;            
            if (!Int32.TryParse(queryWidth, out width))
            {
                WmsException.ThrowWmsException(WmsException.WmsExceptionCode.InvalidDimensionValue,
                    "Invalid parameter WIDTH");
                return;
            }
            if (this.description.MaxWidth > 0 && width > this.description.MaxWidth)
            {
                WmsException.ThrowWmsException(WmsException.WmsExceptionCode.OperationNotSupported,
                    "Parameter WIDTH too large");
                return;
            }
            int height;
            if (!Int32.TryParse(queryHeight, out height))
            {
                WmsException.ThrowWmsException(WmsException.WmsExceptionCode.InvalidDimensionValue,
                    "Invalid parameter HEIGHT");
                return;
            }
            if (this.description.MaxHeight > 0 && height > this.description.MaxHeight)
            {
                WmsException.ThrowWmsException(WmsException.WmsExceptionCode.OperationNotSupported,
                    "Parameter HEIGHT too large");
                return;
            }
            this.map.Size = new Size(width, height);

            BoundingBox bbox = this.ParseBBOX(queryBBOX);
            if (bbox == null)
            {
                WmsException.ThrowWmsException("Invalid parameter BBOX");
                return;
            }

            this.map.PixelAspectRatio = (width / (double)height) / (bbox.Width / bbox.Height);
            this.map.Center = bbox.GetCentroid();
            this.map.Zoom = bbox.Width;

            //Set layers on/off
            if (!String.IsNullOrEmpty(queryLayers))
            //If LAYERS is empty, use default layer on/off settings
            {
                string[] layers = queryLayers.Split(new[] { ',' });
                if (this.description.LayerLimit > 0)
                {
                    if (layers.Length == 0 && 
                        this.map.Layers.Count > this.description.LayerLimit ||
                        layers.Length > this.description.LayerLimit)
                    {
                        WmsException.ThrowWmsException(WmsException.WmsExceptionCode.OperationNotSupported,
                            "Too many layers requested");
                        return;
                    }
                }
                foreach (ILayer layer in this.map.Layers)
                    layer.Enabled = false;
                foreach (string layer in layers)
                {
                    ILayer lay = map.GetLayerByName(layer);                    
                    if (lay == null)
                    {
                        WmsException.ThrowWmsException(WmsException.WmsExceptionCode.LayerNotDefined,
                            String.Format("Unknown layer '{0}'", layer));
                        return;
                    }
                    lay.Enabled = true;
                }
            }
            
            bool json = this.Check("text/json", format);
            if (json)
            {
                //Only queryable data!
                IQueryable<ICanQueryLayer> collection = this.map.Layers.AsQueryable()
                    .OfType<ICanQueryLayer>().Where(l => l.Enabled && l.IsQueryEnabled);
                foreach (ICanQueryLayer layer in collection)
                {
                    //Query for data
                    FeatureDataSet ds = new FeatureDataSet();
                    layer.ExecuteIntersectionQuery(bbox, ds);
                    IEnumerable<GeoJSON> data = GeoJSONHelper.GetData(ds);
                    Debug.WriteLine(String.Format("BBOX: {0}, QUERY ITEMS: {1}", bbox, data.Count()));

                    //Reproject geometries if needed
                    IMathTransform transform = null;
                    if (layer is VectorLayer)
                    {
                        ICoordinateTransformation transformation = (layer as VectorLayer).CoordinateTransformation;
                        transform = transformation == null ? null : transformation.MathTransform;
                    }
                    if (transform != null)
                    {
                        data = data.Select(d =>
                        {
                            Geometry converted = GeometryTransform.TransformGeometry(d.Geometry, transform);
                            d.SetGeometry(converted);
                            return d;
                        });
                    }

                    StringWriter writer = new StringWriter();
                    GeoJSONWriter.Write(data, writer);
                    string buffer = writer.ToString();

                    this.context.Response.Clear();
                    this.context.Response.ContentType = "text/json";
                    this.context.Response.BufferOutput = true;
                    this.context.Response.Write(buffer);
                    this.context.Response.End();
                }
            }
            else
            {
                //Get the image format requested
                ImageCodecInfo imageEncoder = this.GetEncoderInfo(format);
                if (imageEncoder == null)
                {
                    WmsException.ThrowWmsException("Invalid MimeType specified in FORMAT parameter");
                    return;
                }

                //Render map
                Image img = this.map.GetMap();

                //Png can't stream directy. Going through a memorystream instead
                MemoryStream ms = new MemoryStream();
                img.Save(ms, imageEncoder, null);
                img.Dispose();
                byte[] buffer = ms.ToArray();

                this.context.Response.Clear();
                this.context.Response.ContentType = imageEncoder.MimeType;
                this.context.Response.BufferOutput = true;
                this.context.Response.BinaryWrite(buffer);
                this.context.Response.End();
            }
        }
    }
}
