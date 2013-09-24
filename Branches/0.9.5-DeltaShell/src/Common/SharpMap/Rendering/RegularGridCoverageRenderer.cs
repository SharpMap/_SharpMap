using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using DelftTools.Functions.Filters;
using DelftTools.Functions.Generic;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;
using log4net;
using SharpMap.Api;
using SharpMap.Layers;
using SharpMap.Rendering.Thematics;

namespace SharpMap.Rendering
{
    public class RegularGridCoverageRenderer : IFeatureRenderer
    {
        // local store the 'classified' bitmap.
        // todo move to temporary diskfile
        private Bitmap bitmapCache;
        private static readonly ILog log = LogManager.GetLogger(typeof(RegularGridCoverageRenderer));

        private IRegularGridCoverageLayer regularGridCoverageLayer;

        private IRegularGridCoverageLayer RegularGridCoverageLayer
        {
            get { return regularGridCoverageLayer; }
            set
            {
                if (null != regularGridCoverageLayer)
                {
                    ((INotifyPropertyChanged)regularGridCoverageLayer).PropertyChanged -=
                        RegularGridCoverageRenderer_PropertyChanged;
                }
                regularGridCoverageLayer = value;
                if (null != regularGridCoverageLayer)
                {
                    ((INotifyPropertyChanged)regularGridCoverageLayer).PropertyChanged +=
                        RegularGridCoverageRenderer_PropertyChanged;
                }
            }
        }

        public RegularGridCoverageRenderer(IRegularGridCoverageLayer layer)
        {
            RegularGridCoverageLayer = layer;
        }


        private static int FindNearest(IMultiDimensionalArray<double> values, double first)
        {
            if (first <= values[0] || values.Count == 1)
            {
                return 0;
            }
            for (int i = 1; i < values.Count; i++)
            {
                if (values[i] > first)
                {
                    return i;
                }
            }
            return values.Count - 1;
        }

        /// <summary>
        /// Renders feature on a given map.
        /// </summary>
        /// <param name="feature">Feature to render</param>
        /// <param name="g">Graphics object to be used as a target for rendering</param>
        /// <param name="layer">Layer where feature belongs to</param>
        /// <returns>When rendering succeds - returns true, otherwise false</returns>
        public virtual bool Render(IFeature feature, Graphics g, ILayer layer)
        {
            if (null == RegularGridCoverageLayer)
            {
                throw new Exception("RegularGridCoverageRenderer needs reference to RegularGridCoverageLayer");
            }
            var gridLayer = RegularGridCoverageLayer;
            var grid = gridLayer.RenderedCoverage;

            IEnvelope mapExtents = layer.Map.Envelope;
            //  IEnvelope gridExtents = grid.Geometry.EnvelopeInternal;

            //do not render invalid grid
            if (grid.SizeX == 0 || grid.SizeY == 0)
            {
                return false;
            }

            //create filters for x, y to only obtain necessary data from functionstore.


            //  var xFilter = new VariableValueRangesFilter<double>(grid.X, new[] { new Pair<double, double>(layer.Map.Envelope.MinX,layer.Map.Envelope.MaxX) });
            //  var yFilter = new VariableValueRangesFilter<double>(grid.Y, new[] { new Pair<double, double>(layer.Map.Envelope.MinY, layer.Map.Envelope.MaxY) });

            IMultiDimensionalArray<double> xValues = grid.X.Values;
            var indexMinX = FindNearest(xValues, layer.Map.Envelope.MinX);
            var indexMaxX = FindNearest(xValues, layer.Map.Envelope.MaxX);
            IMultiDimensionalArray<double> yValues = grid.Y.Values;
            var indexMinY = FindNearest(yValues, layer.Map.Envelope.MinY);
            var indexMaxY = FindNearest(yValues, layer.Map.Envelope.MaxY);

            if (indexMaxX == indexMinX || indexMaxY == indexMinY) return true; //nothing to render.


            /*
                        var xWidth = xFilter.IndexRanges[0].Second - xFilter.IndexRanges[0].First + 1;
                        var yWidth = yFilter.IndexRanges[0].Second - yFilter.IndexRanges[0].First + 1;
            */

            const PixelFormat pixelFormat = PixelFormat.Format32bppArgb;


            var gridMinX = xValues[indexMinX];
            var gridMaxX = xValues[indexMaxX] + grid.DeltaX;
            var gridMinY = yValues[indexMinY];
            var gridMaxY = yValues[indexMaxY] + grid.DeltaY;

            //height and offset for the rasterlayer
            var targetOffsetX = (int)((gridMinX - mapExtents.MinX) / layer.Map.PixelWidth);
            var targetOffsetY = (int)((mapExtents.MaxY - gridMaxY) / layer.Map.PixelHeight);
            var targetWidth = (int)((gridMaxX - gridMinX) / layer.Map.PixelWidth);
            var targetHeight = (int)((gridMaxY - gridMinY) / layer.Map.PixelHeight);

            if (targetHeight == 0 || targetWidth == 0) return true;

            //in case there is just one pixel stepsize should be 1
            var stepsizeX = Math.Max((indexMaxX - indexMinX) / targetWidth, 1);
            var stepsizeY = Math.Max((indexMaxY - indexMinY) / targetHeight, 1);

            /*
                          var xFilter = new VariableAggregationFilter(grid.Components[0].Arguments.Where(a=>a.Name==grid.X.Name).FirstOrDefault(), stepsizeX, indexMinX, indexMaxX);
                    var yFilter = new VariableAggregationFilter(grid.Components[0].Arguments.Where(a=>a.Name==grid.Y.Name).FirstOrDefault(), stepsizeY, indexMinY, indexMaxY);

            */
            var xFilter = new VariableAggregationFilter(grid.X, stepsizeX, indexMinX, indexMaxX);
            var yFilter = new VariableAggregationFilter(grid.Y, stepsizeY, indexMinY, indexMaxY);

            log.DebugFormat("x: {0},{1},{2}; y: {3},{4},{5}", stepsizeX, indexMinX, indexMaxX, stepsizeY, indexMinY, indexMaxY);

            // TODO: Commented out as temporary fix. Bitmap is not be cleared any more, results in as of revision 19658 by Genna
            // Fix ensures coverage is updated properly and no strange rescaling effects when zooming or dragging a part of the coverage outside the viewport
            // occur because bitmapCache is never cleared.
            // However, the fix might incur performance issues because bitmapCache is always refreshed on Render call
            //if (null == bitmapCache) 
            //{
                var filters = grid.Filters.ToList();

                filters.Add(xFilter);
                filters.Add(yFilter);

                bitmapCache = GenerateBitmap(gridLayer, grid, pixelFormat, filters.ToArray(),
                                             xFilter.Count, yFilter.Count);
            //}


            /*
                        int targetOffsetX = (int)((gridExtents.MinX - mapExtents.MinX) / layer.Map.PixelWidth);
                        int targetOffsetY = (int)((mapExtents.MaxY - gridExtents.MaxY) / layer.Map.PixelHeight);

                        int targetWidth = (int)(gridExtents.Width / layer.Map.PixelWidth);
                        int targetHeight = (int)(gridExtents.Height / layer.Map.PixelHeight);
            */


            var srcRectangle = new Rectangle(0, 0, bitmapCache.Width, bitmapCache.Height);
            var targetRectangle = new Rectangle(targetOffsetX, targetOffsetY, targetWidth, targetHeight);

            g.SetClip(targetRectangle);
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.DrawImage(bitmapCache, targetRectangle, srcRectangle, GraphicsUnit.Pixel);
            g.ResetClip();

            return false;
        }

        private void RegularGridCoverageRenderer_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "Theme" || e.PropertyName != "Fill" || !e.PropertyName.Equals("Line"))
                return;

            RegularGridCoverageLayer.RenderRequired = true;
        }

        private static Bitmap GenerateBitmap(IRegularGridCoverageLayer gridLayer, IRegularGridCoverage grid,
                                             PixelFormat pixelFormat, IVariableFilter[] filters, int xWidth, int yWidth)
        {
            /*var xWidth =
                ((VariableIndexRangesFilter) filters[0]).IndexRanges[0].Second-((VariableIndexRangesFilter) filters[0]).
                    IndexRanges[0].First +1;
            var yWidth = ((VariableIndexRangesFilter)filters[1]).IndexRanges[0].Second - ((VariableIndexRangesFilter)filters[1]).
                    IndexRanges[0].First+1;*/


            var bitmap = new Bitmap(xWidth, yWidth, pixelFormat);


            if (grid.Components[0].ValueType == typeof(double))
            {
                FillBitmap<double>(gridLayer, grid, bitmap, pixelFormat, filters);
            }
            else if (grid.Components[0].ValueType == typeof(float))
            {
                FillBitmap<float>(gridLayer, grid, bitmap, pixelFormat, filters);
            }
            else if (grid.Components[0].ValueType == typeof(int))
            {
                FillBitmap<int>(gridLayer, grid, bitmap, pixelFormat, filters);
            }
            else if (grid.Components[0].ValueType == typeof(short))
            {
                FillBitmap<short>(gridLayer, grid, bitmap, pixelFormat, filters);
            }
            else if (grid.Components[0].ValueType == typeof(UInt32))
            {
                FillBitmap<UInt32>(gridLayer, grid, bitmap, pixelFormat, filters);
            }
            else if (grid.Components[0].ValueType == typeof(Byte))
            {
                //todo hack: in case source file is tiff it has its own color
                //return bitmap;
                FillBitmap<Byte>(gridLayer, grid, bitmap, pixelFormat, filters);
            }
            else
            {
                log.WarnFormat("Cant render type: " + grid.Components[0].ValueType);
            }
            return bitmap;
        }

        protected static void FillBitmap<T>(IRegularGridCoverageLayer gridLayer, IRegularGridCoverage gridCoverage,
                                            Bitmap bitmap, PixelFormat pixelFormat, IVariableFilter[] filters)
            where T : IComparable, IComparable<T>
        {
            // HACK: free memory before getting large block
            GC.Collect(1, GCCollectionMode.Optimized);

            T[] gridValues = gridCoverage.Components[0].GetValues<T>(filters).ToArray();

            var imageWidth = bitmap.Width;
            var imageHeight = bitmap.Height;
            Trace.Assert(imageHeight * imageWidth == gridValues.Count());
            //flip upside down
            for (int k = 0; k < imageWidth; k++)
            {
                for (int j = 0; j < imageHeight / 2; j++)
                {
                    T swap1 = gridValues[k + j * imageWidth];
                    T swap2 = gridValues[(imageHeight - 1 - j) * imageWidth + k];
                    gridValues[k + j * imageWidth] = swap2;
                    gridValues[(imageHeight - 1 - j) * imageWidth + k] = swap1;
                }
            }

            if (gridLayer.Theme == null)
            {
                object minValue = gridCoverage.Components[0].MinValue;
                object maxValue = gridCoverage.Components[0].MaxValue;

                gridLayer.Theme = GenerateDefaultTheme(gridCoverage.Components[0].Name,
                                                       gridCoverage.Components[0].NoDataValues, minValue, maxValue);
            }

            // Add NoDataValues from grid coverage if it was unspecified:
            var theme = gridLayer.Theme as Theme;
            if (theme != null && theme.NoDataValues == null)
            {
                theme.NoDataValues = gridCoverage.Components[0].NoDataValues;
            }

            BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, imageWidth, imageHeight),
                                                    ImageLockMode.ReadWrite, pixelFormat);
            try
            {
                unsafe
                {
                    // use a pointer to get direct access to the bits in the bitmap.
                    // this is faster and removes the need to copy values from
                    // unmanaged to managed memory and vice versa.
                    IntPtr array = bitmapData.Scan0;
                    int* pByte = (int*)array.ToPointer();
                    int bytes = bitmapData.Width * bitmapData.Height;
                    ((Theme)gridLayer.Theme).GetFillColors(pByte, bytes, (T[])gridValues);
                }
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                if (bitmapData != null)
                {
                    bitmap.UnlockBits(bitmapData);

                    // force a collect to reclaim large allocated arrays that have survived the first generations
                    GC.Collect(1, GCCollectionMode.Optimized);
                }
            }
        }

        protected static ITheme GenerateDefaultTheme(string name, IList noDataValues, object minValue, object maxValue)
        {
            double min = Convert.ToDouble(minValue);
            double max = Convert.ToDouble(maxValue);

            if (double.IsInfinity(min) || double.IsNaN(min))
            {
                log.Debug("Nan or infinity is invalid as minvalue for theme. Changed it to lowest possible value");
                min = double.MinValue;
            }
            if (double.IsInfinity(max) || double.IsNaN(max))
            {
                log.Debug("Nan or infinity is invalid as maxvalue for theme. Changed it to highest possible value");
                max = double.MaxValue;
            }

            var theme = ThemeFactory.CreateGradientTheme(name, null, Thematics.ColorBlend.GreenToBlue, min, max, 1, 1, false, true);
            theme.NoDataValues = noDataValues;

            return theme;
        }

        #region IFeatureRenderer Members

        public bool Render(int index, IGeometry featureGeometry, Graphics g, ILayer layer)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public IGeometry GetRenderedFeatureGeometry(IFeature feature, ILayer layer)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public bool UpdateRenderedFeatureGeometry(IFeature feature, ILayer layer)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public IEnumerable<IFeature> GetFeatures(IGeometry geometry, ILayer layer)
        {
            return layer.GetFeatures(geometry, false);
        }

        public IEnumerable<IFeature> GetFeatures(IEnvelope box, ILayer layer)
        {
            return layer.GetFeatures(new GeometryFactory().ToGeometry(box), false);
        }

        #endregion
    }
}