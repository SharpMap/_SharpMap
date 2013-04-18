using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;
using log4net;
using OSGeo.GDAL;
using SharpMap.Extensions.Layers;
using SharpMap.Layers;
using SharpMap.Rendering;
using SharpMap.Utilities;

namespace SharpMap.Extensions.Data.Providers
{
    // TODO: move general logic to GdalHelper, GdalFunctionStore and bitmap-based rendering logic should go to RegularGridCoverageRenderer
    public class GdalRenderer : RegularGridCoverageRenderer
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(GdalRenderer));

        private GdalFeatureProvider gdalFeatureFeatureProvider;

        private ILayer layer;

        private Map map;

        public GdalRenderer(IRegularGridCoverageLayer layer) : base(layer)
        {
        }

        public override bool Render(IFeature feature, Graphics g, ILayer layer)
        {
            gdalFeatureFeatureProvider = (GdalFeatureProvider)layer.DataSource;

            if(!gdalFeatureFeatureProvider.IsOpen)
            {
                log.WarnFormat("Can not render raster layer, file is not opened");
                return false;
            }

            map = layer.Map;
            this.layer = layer;
            Draw(gdalFeatureFeatureProvider.GdalDataset, map.Size, g, map.Envelope, gdalFeatureFeatureProvider.GetExtents());

            return true;
        }

        private void Draw(Dataset dataset, Size size, Graphics g, IEnvelope mapExtents, IEnvelope gridExtents)
        {
            var geoTrans = new double[6];
            dataset.GetGeoTransform(geoTrans);
            var geoTransform = new RegularGridGeoTransform(geoTrans);

            double left = Math.Max(mapExtents.MinX, gridExtents.MinX);
            double top = Math.Min(mapExtents.MaxY, gridExtents.MaxY);
            double right = Math.Min(mapExtents.MaxX, gridExtents.MaxX);
            double bottom = Math.Max(mapExtents.MinY, gridExtents.MinY);

            int x1 = (int)geoTransform.PixelX(left);
            int y1 = (int)geoTransform.PixelY(top);
            int x1width = (int)geoTransform.PixelXwidth(right - left);
            int y1height = (int)geoTransform.PixelYwidth(top - bottom);

            int bitmapOffsetX = (int)Math.Max((gridExtents.MinX - mapExtents.MinX) / map.PixelWidth, 0);
            int bitmapOffsetY = (int)Math.Max((mapExtents.MaxY - gridExtents.MaxY) / map.PixelHeight, 0);
            int bitmapWidth = (int)Math.Max((right - left) / map.PixelWidth, 0);
            int bitmapHeight = (int)Math.Max((top - bottom) / map.PixelHeight, 0);

            if (bitmapWidth > 0 && bitmapHeight > 0 && x1width > 0 && y1height > 0)
            {
                Bitmap bitmap = GetBitmapDirect(dataset, x1, y1, x1width, y1height, bitmapWidth, bitmapHeight);
                g.DrawImage(bitmap, new Point(bitmapOffsetX, bitmapOffsetY));
            }
        }

        // todo cleanup and add support for adding external palettes.
        private Bitmap GetBitmapDirect(Dataset dataset, int xOff, int yOff, int width, int height, int imageWidth, int imageHeight)
        {
            if (dataset.RasterCount == 0)
            {
                return null;
            }

            ImageSettings settings = CreateSettings(PixelFormat.Undefined, 0);
            DataType dataType = dataset.GetRasterBand(1).DataType;
            
            if (dataset.GetDriver().ShortName.Equals("EHdr") && dataset.RasterCount == 1) // 1-band ESRI BIL/BIP/BSQ files from Habitat
            {
                settings = GetDefaultImageSettings(dataset, true);
            }
            else // default for all other GDAL supported formats
            {
                settings = GetDefaultImageSettings(dataset, false);
            }

            PixelFormat pixelFormat = settings.pixelFormat;
            int targetPixelSpace = settings.targetPixelSpace;

            //// Create a Bitmap to store the GDAL image in
            var bitmap = new Bitmap(imageWidth, imageHeight, pixelFormat);


            // Use GDAL raster reading methods to read the image data directly into the Bitmap
            var bitmapData = bitmap.LockBits(new Rectangle(0, 0, imageWidth, imageHeight), ImageLockMode.ReadWrite, pixelFormat);

            try
            {
                log.InfoFormat("lineSpace: {0} pixelSpace: {1}", bitmapData.Stride, targetPixelSpace);

                //special rendering for bil file: this is ugly
                if (dataset.GetDriver().ShortName.Equals("EHdr") && dataset.RasterCount == 1)
                {

                    string legendFilename = gdalFeatureFeatureProvider.Path.Substring(0, gdalFeatureFeatureProvider.Path.LastIndexOf('.')) + ".leg";
                    if (HasFloatingDataType(dataset))
                        FillBitmap<float>(bitmap, bitmapData, dataset, xOff, yOff, width, height, imageWidth, imageHeight,
                            targetPixelSpace, bitmapData.Stride, legendFilename);
                    else
                        FillBitmap<int>(bitmap, bitmapData, dataset, xOff, yOff, width, height, imageWidth, imageHeight,
                            targetPixelSpace, bitmapData.Stride, legendFilename);
                }
                else
                {

                    int[] bandMap = new int[4] { 1, 1, 1, 1 };
                    int bandCount = 1;
                    bool hasAlpha = false;
                    bool isIndexed = false;
                    int channelSize = 8;
                    ColorTable ct = null;
                    // Evaluate the bands and find out a proper image transfer format
                    for (int i = 0; i < dataset.RasterCount; i++)
                    {
                        Band band = dataset.GetRasterBand(i + 1);
                        if (Gdal.GetDataTypeSize(band.DataType) > 8)
                            channelSize = 16;
                        switch (band.GetRasterColorInterpretation())
                        {
                            case ColorInterp.GCI_AlphaBand:
                                bandCount = 4;
                                hasAlpha = true;
                                bandMap[3] = i + 1;
                                break;
                            case ColorInterp.GCI_BlueBand:
                                if (bandCount < 3)
                                    bandCount = 3;
                                bandMap[0] = i + 1;
                                break;
                            case ColorInterp.GCI_RedBand:
                                if (bandCount < 3)
                                    bandCount = 3;
                                bandMap[2] = i + 1;
                                break;
                            case ColorInterp.GCI_GreenBand:
                                if (bandCount < 3)
                                    bandCount = 3;
                                bandMap[1] = i + 1;
                                break;
                            case ColorInterp.GCI_PaletteIndex:
                                ct = band.GetRasterColorTable();
                                isIndexed = true;
                                bandMap[0] = i + 1;
                                break;
                            case ColorInterp.GCI_GrayIndex:
                                isIndexed = true;
                                bandMap[0] = i + 1;
                                break;
                            default:
                                // we create the bandmap using the dataset ordering by default
                                if (i < 4 && bandMap[i] == 0)
                                {
                                    if (bandCount < i)
                                        bandCount = i;
                                    bandMap[i] = i + 1;
                                }
                                break;
                        }
                    }




                    // find out the pixel format based on the gathered information
                    //PixelFormat pixelFormat;
                    //DataType dataType;
                    int pixelSpace;

                    if (isIndexed)
                    {
                        //pixelFormat = PixelFormat.Format8bppIndexed;
                        dataType = DataType.GDT_Byte;
                        pixelSpace = 1;
                    }
                    else
                    {
                        if (bandCount == 1)
                        {
                            if (channelSize > 8)
                            {
                          //      pixelFormat = PixelFormat.Format16bppGrayScale;
                                dataType = DataType.GDT_Int16;
                                pixelSpace = 2;
                            }
                            else
                            {
                            //    pixelFormat = PixelFormat.Format24bppRgb;
                                bandCount = 3;
                                dataType = DataType.GDT_Byte;
                                pixelSpace = 3;
                            }
                        }
                        else
                        {
                            if (hasAlpha)
                            {
                                if (channelSize > 8)
                                {
                              //      pixelFormat = PixelFormat.Format64bppArgb;
                                    dataType = DataType.GDT_UInt16;
                                    pixelSpace = 8;
                                }
                                else
                                {
                                //    pixelFormat = PixelFormat.Format32bppArgb;
                                    dataType = DataType.GDT_Byte;
                                    pixelSpace = 4;
                                }
                                bandCount = 4;
                            }
                            else
                            {
                                if (channelSize > 8)
                                {
                                  //  pixelFormat = PixelFormat.Format48bppRgb;
                                    dataType = DataType.GDT_UInt16;
                                    pixelSpace = 6;
                                }
                                else
                                {
                                   // pixelFormat = PixelFormat.Format24bppRgb;
                                    dataType = DataType.GDT_Byte;
                                    pixelSpace = 4;
                                }
                                bandCount = 3;
                            }
                        }
                    }


                    SetColorPalette(dataset, bitmap);

                    int stride = bitmapData.Stride;
                    IntPtr buf = bitmapData.Scan0;

                    var bandSpace = 1;
                    dataset.ReadRaster(xOff, yOff, width, height, buf, imageWidth, imageHeight, dataType, bandCount, bandMap, pixelSpace, stride, bandSpace);
                }
            }
            finally
            {
                if (bitmapData != null)
                    bitmap.UnlockBits(bitmapData);
            }


            return bitmap;


        }


        private static void SetColorPalette(Dataset ds, Bitmap bitmap)
        {
            List<OSGeo.GDAL.ColorInterp> interps = GetRasterColorInterps(ds);
            int paletteIndex = interps.IndexOf(OSGeo.GDAL.ColorInterp.GCI_PaletteIndex);
            int grayIndex = interps.IndexOf(OSGeo.GDAL.ColorInterp.GCI_GrayIndex);

            if (paletteIndex > -1)
            {
                ColorTable ct = ds.GetRasterBand(paletteIndex + 1).GetRasterColorTable();
                int iCol = ct.GetCount();

                ColorPalette pal = bitmap.Palette;
                for (int i = 0; i < iCol; i++)
                {
                    ColorEntry ce = ct.GetColorEntry(i);
                    pal.Entries[i] = Color.FromArgb(ce.c4, ce.c1, ce.c2, ce.c3);
                }
                bitmap.Palette = pal;
            }
            else if (grayIndex > -1)
            {
                ColorPalette pal = bitmap.Palette;
                for (int i = 0; i < 256; i++)
                    pal.Entries[i] = Color.FromArgb(255, i, i, i);
                bitmap.Palette = pal;
            }
        }

        private static ImageSettings GetDefaultImageSettings(Dataset ds, bool dealingWithValuesInsteadOfColors /* why this would be required? */)
        {
            List<OSGeo.GDAL.ColorInterp> interps = GetRasterColorInterps(ds);
            bool hasAlpha = interps.Contains(OSGeo.GDAL.ColorInterp.GCI_AlphaBand);
            bool isIndexed = (interps.Contains(OSGeo.GDAL.ColorInterp.GCI_PaletteIndex) ||
                interps.Contains(OSGeo.GDAL.ColorInterp.GCI_GrayIndex)) &&
                !dealingWithValuesInsteadOfColors;
            OSGeo.GDAL.DataType dataType = ds.GetRasterBand(1).DataType;

            ImageSettings settings = CreateSettings(PixelFormat.Undefined, 0);

            if (isIndexed)
            {
                settings = CreateSettings(PixelFormat.Format8bppIndexed, 1);
            }
            else if (ds.RasterCount == 1)
            {
                settings = dealingWithValuesInsteadOfColors ? CreateSettings(PixelFormat.Format32bppArgb, 4) : CreateSettings(PixelFormat.Format16bppRgb555, 2);
            }
            else
            {
                var d = new Dictionary<OSGeo.GDAL.DataType, ImageSettings[]>();

                // Mapping from dataType to PixelFormat, first item in PixelFormat array is the non-alpha variant (if available),
                // the second is the alpha variant
                d.Add(OSGeo.GDAL.DataType.GDT_CFloat32, new ImageSettings[] { CreateSettings(PixelFormat.Format32bppRgb, 4), CreateSettings(PixelFormat.Format32bppArgb, 4) });
                d.Add(OSGeo.GDAL.DataType.GDT_CFloat64, new ImageSettings[] { CreateSettings(PixelFormat.Format64bppArgb, 8), CreateSettings(PixelFormat.Format64bppArgb, 8) });
                d.Add(OSGeo.GDAL.DataType.GDT_CInt16, new ImageSettings[] { CreateSettings(PixelFormat.Format16bppRgb555, 2), CreateSettings(PixelFormat.Format16bppArgb1555, 2) });
                d.Add(OSGeo.GDAL.DataType.GDT_CInt32, new ImageSettings[] { CreateSettings(PixelFormat.Format32bppRgb, 4), CreateSettings(PixelFormat.Format32bppArgb, 4) });
                d.Add(OSGeo.GDAL.DataType.GDT_Float32, new ImageSettings[] { CreateSettings(PixelFormat.Format32bppRgb, 4), CreateSettings(PixelFormat.Format32bppArgb, 4) });
                d.Add(OSGeo.GDAL.DataType.GDT_Float64, new ImageSettings[] { CreateSettings(PixelFormat.Format64bppArgb, 8), CreateSettings(PixelFormat.Format64bppArgb, 8) });
                d.Add(OSGeo.GDAL.DataType.GDT_Int16, new ImageSettings[] { CreateSettings(PixelFormat.Format16bppRgb555, 2), CreateSettings(PixelFormat.Format16bppArgb1555, 2) });
                d.Add(OSGeo.GDAL.DataType.GDT_Int32, new ImageSettings[] { CreateSettings(PixelFormat.Format32bppRgb, 4), CreateSettings(PixelFormat.Format32bppArgb, 4) });
                d.Add(OSGeo.GDAL.DataType.GDT_UInt16, new ImageSettings[] { CreateSettings(PixelFormat.Format16bppRgb555, 2), CreateSettings(PixelFormat.Format16bppArgb1555, 2) });
                d.Add(OSGeo.GDAL.DataType.GDT_UInt32, new ImageSettings[] { CreateSettings(PixelFormat.Format32bppRgb, 4), CreateSettings(PixelFormat.Format32bppArgb, 4) });
                d.Add(OSGeo.GDAL.DataType.GDT_Byte, new ImageSettings[] { CreateSettings(PixelFormat.Format32bppRgb, 1), CreateSettings(PixelFormat.Format32bppArgb, 1) });

                if (d.ContainsKey(dataType))
                    settings = d[dataType][hasAlpha ? 1 : 0];
            }

            return settings;
        }

        private struct ImageSettings
        {
            public PixelFormat pixelFormat;
            public int targetPixelSpace;
        }

        private static ImageSettings CreateSettings(PixelFormat pixelFormat, int pixelSpace)
        {
            var settings = new ImageSettings {pixelFormat = pixelFormat, targetPixelSpace = pixelSpace};
            return settings;
        }

        private static bool HasFloatingDataType(Dataset dataset)
        {
            OSGeo.GDAL.DataType dataType = dataset.GetRasterBand(1).DataType;
            bool isFloat = new List<OSGeo.GDAL.DataType>(new OSGeo.GDAL.DataType[]
                        {
                            OSGeo.GDAL.DataType.GDT_CFloat32,
                            OSGeo.GDAL.DataType.GDT_CFloat64,
                            OSGeo.GDAL.DataType.GDT_Float32,
                            OSGeo.GDAL.DataType.GDT_Float64
                        }).Contains(dataType);
            return isFloat;
        }

        private static List<OSGeo.GDAL.ColorInterp> GetRasterColorInterps(Dataset ds)
        {
            List<OSGeo.GDAL.ColorInterp> interps = new List<OSGeo.GDAL.ColorInterp>();

            for (int i = 1; i <= ds.RasterCount; i++)
            {
                interps.Add(ds.GetRasterBand(i).GetRasterColorInterpretation());
            }

            return interps;
        }

        // This fills the bitmap columns first, then wrapping by row (i.e.:
        // (0,0), (1,0), (2,0), (0,1), (1,1), (2,1), (0,2), (1,2), (2,2)
        private void FillBitmapPixels<T>(IList<T> values, BitmapData bitmapData, int targetPixelSpace, int imageWidth, int imageHeight)
        {
            if (layer.Theme == null)
            {
                // HACK: get noDataValue from grid in the future
                int hasNoDataValue;
                double noDataValue;
                object noDataValueObject = null;

                Band band = gdalFeatureFeatureProvider.GdalDataset.GetRasterBand(1);
                band.GetNoDataValue(out noDataValue, out hasNoDataValue);
                if (hasNoDataValue > 0)
                {
                    noDataValueObject = noDataValue;
                }


                var minValue= values.Min();
                var maxValue = values.Max();

                layer.Theme = GenerateDefaultTheme(layer.Name, new[] { noDataValueObject }, minValue, maxValue);
            }

            IntPtr pixels = bitmapData.Scan0;
            unsafe
            {
                byte* pBits = (byte*)pixels.ToPointer();

                for (int row = 0; row < imageHeight; row++)
                {
                    for (int col = 0; col < imageWidth; col++)
                    {
                        double v = Convert.ToDouble(values[row * imageWidth + col]);
                        Color color = layer.Theme.GetFillColor(v);

                        byte* bluePixel = pBits + row * bitmapData.Stride + col * targetPixelSpace;
                        byte* greenPixel = pBits + row * bitmapData.Stride + col * targetPixelSpace + 1;
                        byte* redPixel = pBits + row * bitmapData.Stride + col * targetPixelSpace + 2;
                        byte* alphaPixel = pBits + row * bitmapData.Stride + col * targetPixelSpace + 3;

                        *bluePixel = color.B;
                        *greenPixel = color.G;
                        *redPixel = color.R;
                        *alphaPixel = color.A;
                    }
                }
            }
        }

        private void FillBitmap<T>(Bitmap bitmap, BitmapData bitmapData, Dataset dataset, int xOff, int yOff, int width, int height,
            int imageWidth, int imageHeight, int targetPixelSpace, int lineSpace, string legendFilename)
        {
            T[] values = GdalHelper.GetValuesForBand<T>(dataset, 1, xOff, yOff, width, height, imageWidth, imageHeight);

            var gdalRasterLayer = layer as GdalRasterLayer;
            if (gdalRasterLayer == null)
                throw new Exception("Trying to draw a GdalRasterLayer, while we got another type of layer!");

            FillBitmapPixels<T>(values, bitmapData, targetPixelSpace, imageWidth, imageHeight);
        }

    }
}
