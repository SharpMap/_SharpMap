using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SharpMap;
using SharpMap.Geometries;
using SharpMap.Layers;
using SharpMap.Rendering;
using SharpMap.Rendering.Thematics;
using SharpMap.Styles;
using Windows = System.Windows.Media;
using SharpMap.Data;
using Path = System.Windows.Shapes.Path;

namespace SilverlightRendering
{
    public class SilverlightRenderer : IRenderer
    {
        Canvas canvas;
        
        public SilverlightRenderer()
        {
           canvas = new Canvas();
        }
        
        public SilverlightRenderer(Canvas canvas)
        {
            this.canvas = canvas;
        }

        public void Render(IView view, Map map)
        {
            foreach (var layer in map.Layers)
            {
                if (layer.Enabled &&
                    layer.MinVisible <= view.Resolution &&
                    layer.MaxVisible >= view.Resolution)
                {
                    RenderLayer(view, layer);
                }
            }
        }

        private void RenderLayer(IView view, ILayer layer)
        {
            if (layer is LabelLayer)
            {
                //
            }
            else if (layer is Layer) RenderVectorLayer(view, layer);
            //!!!else if (layer is LabelLayer) RenderLabelLayer(view, layer);
            //!!!else if (layer is GroupLayer) RenderVectorLayer(view, layer);
        }

        private void RenderVectorLayer(IView view, ILayer layer)
        {
            var vectorLayer = layer as Layer;
            var getStyle = CreateStyleMethod(layer.Style, layer.Theme);

            vectorLayer.DataSource.Open();
            IFeatures features = vectorLayer.DataSource.GetFeaturesInView(view.Extent, view.Resolution);
            vectorLayer.DataSource.Close();

            foreach (var feature in features)
            {
                if (feature.Geometry is Point)
                    RenderPoint(feature.Geometry as Point, getStyle(feature), view);
                else if (feature.Geometry is MultiPoint)
                    RenderMultiPoint(feature.Geometry as MultiPoint, getStyle(feature), view);
                else if (feature.Geometry is LineString)
                    canvas.Children.Add(RenderLineString(feature.Geometry as LineString, getStyle(feature), view));
                else if (feature.Geometry is MultiLineString)
                    canvas.Children.Add(RenderMultiLineString(feature.Geometry as MultiLineString, getStyle(feature), view));
                else if (feature.Geometry is SharpMap.Geometries.Polygon)
                    RenderPolygon(feature.Geometry as Polygon, getStyle(feature), view);
                else if (feature.Geometry is MultiPolygon)
                    canvas.Children.Add(RenderMultiPolygon(feature.Geometry as MultiPolygon, getStyle(feature), view));
                else if (feature.Geometry is IRaster)
                    canvas.Children.Add(RenderRaster(feature.Geometry as IRaster, getStyle(feature), view));
            }
        }

        private static Func<IFeature, IStyle> CreateStyleMethod(IStyle style, ITheme theme)
        {
            if (theme == null) return (row) => style;

            return (row) => theme.GetStyle(row);
        }

        private Path RenderPoint(Point point, IStyle style, IViewTransform viewTransform)
        {
            Path path = CreatePointPath(style);
            path.Data = ConvertPoint(point, style, viewTransform);
            return path;
        }

        private static Path CreatePointPath(IStyle style)
        {
            var vectorStyle = style as VectorStyle;

            //todo: use this:
            //vectorStyle.Symbol.Convert();
            //vectorStyle.SymbolScale;
            //vectorStyle.SymbolOffset.Convert();
            //vectorStyle.SymbolRotation;

            BitmapImage bitmapImage = CreateBitmapImage(vectorStyle.Symbol.data);

            Path path = new Path();
            path.Fill = new ImageBrush() { ImageSource = bitmapImage };
            if (vectorStyle.EnableOutline)
            {
                path.Stroke = new SolidColorBrush(vectorStyle.Outline.Color.Convert());
                path.StrokeThickness = vectorStyle.Outline.Width;
            }
            return path;
        }

        private static BitmapImage CreateBitmapImage(System.IO.Stream imageData)
        {
            BitmapImage bitmapImage = new BitmapImage();
#if !SILVERLIGHT
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = imageData;
            bitmapImage.EndInit();
#else
            bitmapImage.SetSource(imageData);
#endif
            return bitmapImage;
        }

        private static EllipseGeometry ConvertPoint(Point point, IStyle style, IViewTransform viewTransform)
        {
            var vectorStyle = style as VectorStyle;
            Point p = viewTransform.WorldToView(point);
            EllipseGeometry ellipse = new EllipseGeometry();
            ellipse.Center = new System.Windows.Point(p.X, p.Y);
            ellipse.RadiusX = 10 * vectorStyle.SymbolScale;  //!!! todo: get actual width and height
            ellipse.RadiusY = 10 * vectorStyle.SymbolScale;  //!!! todo: get actual width and height
            return ellipse;
        }

        private static Path RenderMultiPoint(MultiPoint multiPoint, IStyle style, IViewTransform viewTransform)
        {
            Path path = CreatePointPath(style);
            path.Data = ConvertMultiPoint(multiPoint, style, viewTransform);
            return path;
        }

        private static GeometryGroup ConvertMultiPoint(MultiPoint multiPoint, IStyle style, IViewTransform viewTransform)
        {
            var group = new GeometryGroup();
            foreach (Point point in multiPoint)
                group.Children.Add(ConvertPoint(point, style, viewTransform));
            return group;
        }

        private static Path RenderLineString(LineString lineString, IStyle style, IViewTransform viewTransform)
        {
            Path path = CreateLineStringPath(style);
            path.Data = ConvertLineString(lineString, viewTransform);
            return path;
        }

        private static Path CreateLineStringPath(IStyle style)
        {
            var vectorStyle = style as VectorStyle;

            Path path = new Path();
            if (vectorStyle.EnableOutline)
            {
                //todo: render an outline around the line. 
            }
            path.Stroke = new SolidColorBrush(vectorStyle.Line.Color.Convert());
            path.StrokeThickness = vectorStyle.Line.Width;
            path.Fill = vectorStyle.Fill.Convert();
            return path;
        }

        private static Windows.Geometry ConvertLineString(LineString lineString, IViewTransform viewTransform)
        {
            var pathGeometry = new PathGeometry();
            pathGeometry.Figures.Add(CreatePathFigure(lineString, viewTransform));
            return pathGeometry;
        }

        private static PathFigure CreatePathFigure(LineString linearRing, IViewTransform viewTransform)
        {
            var pathFigure = new PathFigure();
            pathFigure.StartPoint = ConvertPoint(linearRing.StartPoint.WorldToMap(viewTransform));

            foreach (Point point in linearRing.Vertices)
            {
                pathFigure.Segments.Add(
                    new LineSegment() { Point = ConvertPoint(point.WorldToMap(viewTransform)) });
            }
            return pathFigure;
        }

        public static System.Windows.Point ConvertPoint(Point point)
        {
            return new System.Windows.Point((float)point.X, (float)point.Y);
        }

        private static Path RenderMultiLineString(MultiLineString multiLineString, IStyle style, IViewTransform viewTransform)
        {
            Path path = CreateLineStringPath(style);
            path.Data = ConvertMultiLineString(multiLineString, viewTransform);
            return path;
        }

        private static System.Windows.Media.Geometry ConvertMultiLineString(MultiLineString multiLineString, IViewTransform viewTransform)
        {
            var group = new GeometryGroup();
            foreach (LineString lineString in multiLineString)
                group.Children.Add(ConvertLineString(lineString, viewTransform));
            return group;
        }

        private Path RenderPolygon(SharpMap.Geometries.Polygon polygon, IStyle style, IViewTransform viewTransform)
        {
            Path path = CreatePolygonPath(style);
            path.Data = ConvertPolygon(polygon, viewTransform);
            return path;
        }

        private static Path CreatePolygonPath(IStyle style)
        {
            var vectorStyle = style as VectorStyle;

            Path path = new Path();
            if (vectorStyle.EnableOutline)
            {
                path.Stroke = new SolidColorBrush(vectorStyle.Outline.Color.Convert());
                path.StrokeThickness = vectorStyle.Outline.Width;
            }

            path.Fill = vectorStyle.Fill.Convert();
            return path;
        }

        private static GeometryGroup ConvertPolygon(SharpMap.Geometries.Polygon polygon, IViewTransform viewTransform)
        {
            var group = new GeometryGroup();
            group.FillRule = FillRule.EvenOdd;
            group.Children.Add(ConvertLinearRing(polygon.ExteriorRing, viewTransform));
            group.Children.Add(ConvertLinearRings(polygon.InteriorRings, viewTransform));
            return group;
        }

        private static PathGeometry ConvertLinearRing(LinearRing linearRing, IViewTransform viewTransform)
        {
            var pathGeometry = new PathGeometry();
            pathGeometry.Figures.Add(CreatePathFigure(linearRing, viewTransform));
            return pathGeometry;
        }

        private static PathGeometry ConvertLinearRings(IList<LinearRing> linearRings, IViewTransform viewTransform)
        {
            var pathGeometry = new PathGeometry();
            foreach (var linearRing in linearRings)
                pathGeometry.Figures.Add(CreatePathFigure(linearRing, viewTransform));
            return pathGeometry;
        }

        private static Path RenderMultiPolygon(MultiPolygon geometry, IStyle style, IViewTransform viewTransform)
        {
            Path path = CreatePolygonPath(style);
            path.Data = ConvertMultiPolygon(geometry, viewTransform);
            return path;
        }

        private static GeometryGroup ConvertMultiPolygon(MultiPolygon geometry, IViewTransform viewTransform)
        {
            var group = new GeometryGroup();
            foreach (SharpMap.Geometries.Polygon polygon in geometry.Polygons)
                group.Children.Add(ConvertPolygon(polygon, viewTransform));
            return group;
        }

        private Path RenderRaster(IRaster raster, IStyle style, IView view)
        {
            Path path = CreateRasterPath(style, raster);
            path.Data = ConvertRaster(raster.GetBoundingBox(), view);
            return path;
        }

        private Path CreateRasterPath(IStyle style, IRaster raster)
        {
            //todo: use this:
            //vectorStyle.Symbol.Convert();
            //vectorStyle.SymbolScale;
            //vectorStyle.SymbolOffset.Convert();
            //vectorStyle.SymbolRotation;

            BitmapImage bitmapImage = new BitmapImage();
#if !SILVERLIGHT
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = new System.IO.MemoryStream(raster.Data);
            bitmapImage.EndInit();
#else
            bitmapImage.SetSource(new System.IO.MemoryStream(raster.Data));
#endif
            Path path = new Path();
            path.Fill = new ImageBrush() { ImageSource = bitmapImage };
            return path;
        }

        private System.Windows.Media.Geometry ConvertRaster(BoundingBox boundingBox, IViewTransform viewTransform)
        {
            return new RectangleGeometry
            {
                Rect = new System.Windows.Rect(
                    ConvertPoint(viewTransform.WorldToView(boundingBox.Min)),
                    ConvertPoint(viewTransform.WorldToView(boundingBox.Max)))
            };
        }

        #region IRenderer Members

        private void RenderLabelLayer(IView view, LabelLayer labelLayer)
        {
            //!!!throw new NotImplementedException();
        }

        #endregion

        public System.IO.Stream ToBitmapStream(double width, double height)
        {            
            canvas.Arrange(new System.Windows.Rect(0, 0, width, height));

            #if !SILVERLIGHT
        
            var renderTargetBitmap = new RenderTargetBitmap((int)width, (int)height, 96, 96, new PixelFormat());
            renderTargetBitmap.Render(canvas);
            var bitmap = new PngBitmapEncoder();
            bitmap.Frames.Add(BitmapFrame.Create(renderTargetBitmap));
            var bitmapStream = new System.IO.MemoryStream();
            bitmap.Save(bitmapStream);
            
            #else

            var writeableBitmap = new WriteableBitmap(256, 256);
            writeableBitmap.Render(canvas, null);
            var bitmapStream = ConverToBitmapStream(writeableBitmap);

            #endif
            
            return bitmapStream;
        }

#if SILVERLIGHT
        
        private Stream ConverToBitmapStream(WriteableBitmap writeableBitmap)
        {
            return new MemoryStream(GetBuffer(writeableBitmap));
        }

        private static byte[] GetBuffer(WriteableBitmap bitmap)
        {
            //This method was copied from msdn forums
            //Thanks Eiji
            //http://forums.silverlight.net/forums/p/114691/446894.aspx

            int width = bitmap.PixelWidth;
            int height = bitmap.PixelHeight;

            MemoryStream ms = new MemoryStream();

            #region BMP File Header(14 bytes)
            //the magic number(2 bytes):BM
            ms.WriteByte(0x42);
            ms.WriteByte(0x4D);

            //the size of the BMP file in bytes(4 bytes)
            long len = bitmap.Pixels.Length * 4 + 0x36;

            ms.WriteByte((byte)len);
            ms.WriteByte((byte)(len >> 8));
            ms.WriteByte((byte)(len >> 16));
            ms.WriteByte((byte)(len >> 24));

            //reserved(2 bytes)
            ms.WriteByte(0x00);
            ms.WriteByte(0x00);

            //reserved(2 bytes)
            ms.WriteByte(0x00);
            ms.WriteByte(0x00);

            //the offset(4 bytes)
            ms.WriteByte(0x36);
            ms.WriteByte(0x00);
            ms.WriteByte(0x00);
            ms.WriteByte(0x00);
            #endregion

            #region Bitmap Information(40 bytes:Windows V3)
            //the size of this header(4 bytes)
            ms.WriteByte(0x28);
            ms.WriteByte(0x00);
            ms.WriteByte(0x00);
            ms.WriteByte(0x00);

            //the bitmap width in pixels(4 bytes)
            ms.WriteByte((byte)width);
            ms.WriteByte((byte)(width >> 8));
            ms.WriteByte((byte)(width >> 16));
            ms.WriteByte((byte)(width >> 24));

            //the bitmap height in pixels(4 bytes)
            ms.WriteByte((byte)height);
            ms.WriteByte((byte)(height >> 8));
            ms.WriteByte((byte)(height >> 16));
            ms.WriteByte((byte)(height >> 24));

            //the number of color planes(2 bytes)
            ms.WriteByte(0x01);
            ms.WriteByte(0x00);

            //the number of bits per pixel(2 bytes)
            ms.WriteByte(0x20);
            ms.WriteByte(0x00);

            //the compression method(4 bytes)
            ms.WriteByte(0x00);
            ms.WriteByte(0x00);
            ms.WriteByte(0x00);
            ms.WriteByte(0x00);

            //the image size(4 bytes)
            ms.WriteByte(0x00);
            ms.WriteByte(0x00);
            ms.WriteByte(0x00);
            ms.WriteByte(0x00);

            //the horizontal resolution of the image(4 bytes)
            ms.WriteByte(0x00);
            ms.WriteByte(0x00);
            ms.WriteByte(0x00);
            ms.WriteByte(0x00);

            //the vertical resolution of the image(4 bytes)
            ms.WriteByte(0x00);
            ms.WriteByte(0x00);
            ms.WriteByte(0x00);
            ms.WriteByte(0x00);

            //the number of colors in the color palette(4 bytes)
            ms.WriteByte(0x00);
            ms.WriteByte(0x00);
            ms.WriteByte(0x00);
            ms.WriteByte(0x00);

            //the number of important colors(4 bytes)
            ms.WriteByte(0x00);
            ms.WriteByte(0x00);
            ms.WriteByte(0x00);
            ms.WriteByte(0x00);
            #endregion

            #region Bitmap data
            for (int y = height - 1; y >= 0; y--)
            {
                for (int x = 0; x < width; x++)
                {
                    int pixel = bitmap.Pixels[width * y + x];

                    ms.WriteByte((byte)(pixel & 0xff)); //B
                    ms.WriteByte((byte)((pixel >> 8) & 0xff)); //G
                    ms.WriteByte((byte)((pixel >> 0x10) & 0xff)); //R
                    ms.WriteByte(0x00); //reserved
                }
            }
            #endregion


            return ms.GetBuffer();
        }

#endif
        
    }
}
