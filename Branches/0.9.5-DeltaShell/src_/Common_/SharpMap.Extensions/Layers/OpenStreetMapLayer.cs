using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Reflection;
using BruTile;
using BruTile.Cache;
using BruTile.Web;
using DelftTools.Utils.Reflection;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;
using log4net;
using SharpMap.Layers;

namespace SharpMap.Extensions.Layers
{
    public class OpenStreetMapLayer : Layer
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(OpenStreetMapLayer)); 
        private ITileSchema schema;
     
        private static ITileCache<byte[]> cache;

        public OpenStreetMapLayer()
        {
            // Here we use a tile schema that is defined in code. There are a few predefined 
            // tile schemas in the BruTile.dll. In the usual case the schema should be parsed
            // from a tile service description.
            schema = CreateTileSchema();

            if (cache == null)
            {
                var localSettingsDirectoryPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var assembly = Assembly.GetEntryAssembly();
                if (assembly == null)
                {
                    cache = new MemoryCache<byte[]>(1000, 100000);
                }
                else
                {
                    var assemblyInfo = AssemblyUtils.GetAssemblyInfo(assembly);
                    var cacheDirectoryPath = Path.Combine(Path.Combine(Path.Combine(
                        localSettingsDirectoryPath, assemblyInfo.Company), assemblyInfo.Product), "cache_open_street_map");
                    cache = new FileCache(cacheDirectoryPath, "png");
                }
            }
        }

        public override IEnvelope Envelope
        {
            get { return new Envelope(schema.Extent.MinX, schema.Extent.MaxX, schema.Extent.MinY, schema.Extent.MaxY); }
        }

        /// <summary>
        /// Renders the layer
        /// </summary>
        /// <param name="g">Graphics object reference</param>
        /// <param name="map">Map which is rendered</param>
        public override void OnRender(System.Drawing.Graphics g, Map map)
        {
            MapTransform mapTransform = new MapTransform(new PointF((float)Map.Center.X, (float)Map.Center.Y), (float)Map.PixelSize, Map.Image.Width, Map.Image.Height);

            int level = BruTile.Utilities.GetNearestLevel(schema.Resolutions, Map.PixelSize);
            
            IList<TileInfo> tileInfos = schema.GetTilesInView(mapTransform.Extent, level);

            IRequest requestBuilder = new TmsRequest(new Uri("http://a.tile.openstreetmap.org"), "png");

            var graphics = Graphics.FromImage(Image);
            //log.DebugFormat("Downloading tiles:");
            foreach (var tileInfo in tileInfos)
            {
                var bytes = cache.Find(tileInfo.Index);

                if (bytes == default(byte[]))
                {
                    try
                    {
                        //log.DebugFormat("row: {0}, column: {1}, level: {2}", tileInfo.Index.Row, tileInfo.Index.Col, tileInfo.Index.Level);
                        var url = requestBuilder.GetUri(tileInfo);
                        bytes = RequestHelper.FetchImage(url);
                        cache.Add(tileInfo.Index, bytes);
                    }
                    catch (WebException e)
                    {
                        log.Error("Can't fetch tiles from the server", e);
                    }
                }
                else
                {
                    log.DebugFormat("row: {0}, column: {1}, level: {2} (cached)", tileInfo.Index.Row, tileInfo.Index.Col, tileInfo.Index.Level);
                }

                if (bytes == null) continue;
                using (var bitmap = new Bitmap(new MemoryStream(bytes)))
                {
                    var rectangle = mapTransform.WorldToMap(tileInfo.Extent.MinX, tileInfo.Extent.MinY,
                                                            tileInfo.Extent.MaxX, tileInfo.Extent.MaxY);
                    DrawTile(schema, graphics, bitmap, rectangle);
                }
            }
        }

        private static RectangleF DrawTile(ITileSchema schema, Graphics graphics, Bitmap bitmap, RectangleF extent)
        {
            // For drawing on WinForms there are two things to take into account 
            // to prevent seams between tiles.
            // 1) The WrapMode should be set to TileFlipXY. This is related 
            //    to how pixels are rounded by GDI+
            ImageAttributes imageAttributes = new ImageAttributes();
            imageAttributes.SetWrapMode(WrapMode.TileFlipXY);
            // 2) The rectangle should be rounded to actual pixels. 
            Rectangle roundedExtent = RoundToPixel(extent);
            graphics.DrawImage(bitmap, roundedExtent, 0, 0, schema.Width, schema.Height, GraphicsUnit.Pixel, imageAttributes);
            return extent;
        }

        private static Rectangle RoundToPixel(RectangleF dest)
        {
            // To get seamless aligning you need to round the locations
            // not the width and height
            return new Rectangle(
                (int)Math.Round(dest.Left),
                (int)Math.Round(dest.Top),
                (int)(Math.Round(dest.Right) - Math.Round(dest.Left)),
                (int)(Math.Round(dest.Bottom) - Math.Round(dest.Top)));
        }

        private ITileSchema CreateTileSchema()
        {
            var resolutions = new[] { 
                156543.033900000, 78271.516950000, 39135.758475000, 19567.879237500, 9783.939618750, 
                4891.969809375, 2445.984904688, 1222.992452344, 611.496226172, 305.748113086, 
                152.874056543, 76.437028271, 38.218514136, 19.109257068, 9.554628534, 4.777314267,
                2.388657133, 1.194328567, 0.597164283};

            var tileSchema = new TileSchema {Name = "OpenStreetMap"};
            foreach (float resolution in resolutions)
            {
                tileSchema.Resolutions.Add(resolution);
            }

            tileSchema.OriginX = -20037508.342789;
            tileSchema.OriginY = 20037508.342789;
            tileSchema.Axis = AxisDirection.InvertedY;
            tileSchema.Extent = new Extent(-20037508.342789, -20037508.342789, 20037508.342789, 20037508.342789);
            tileSchema.Height = 256;
            tileSchema.Width = 256;
            tileSchema.Format = "png";
            tileSchema.Srs = "EPSG:900913";
            return tileSchema;
        }

    }
}
