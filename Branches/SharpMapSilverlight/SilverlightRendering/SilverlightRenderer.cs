using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ProjNet.CoordinateSystems.Transformations;
using SharpMap;
using SharpMap.Data.Providers;
using SharpMap.Geometries;
using SharpMap.Layers;
using SharpMap.Rendering;
using SharpMap.Rendering.Thematics;
using SharpMap.Styles;
using Windows = System.Windows.Media;
using SharpMap.Data;
using SharpMap.Projection;

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
                    canvas.Children.Add(RenderPoint(feature.Geometry as Point, getStyle(feature), view));
                else if (feature.Geometry is MultiPoint)
                    canvas.Children.Add(RenderMultiPoint(feature.Geometry as MultiPoint, getStyle(feature), view));
                else if (feature.Geometry is LineString)
                    canvas.Children.Add(RenderLineString(feature.Geometry as LineString, getStyle(feature), view));
                else if (feature.Geometry is MultiLineString)
                    canvas.Children.Add(RenderMultiLineString(feature.Geometry as MultiLineString, getStyle(feature), view));
                else if (feature.Geometry is SharpMap.Geometries.Polygon)
                    canvas.Children.Add(RenderPolygon(feature.Geometry as SharpMap.Geometries.Polygon, getStyle(feature), view));
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

        private Path RenderPoint(SharpMap.Geometries.Point point, IStyle style, IViewTransform viewTransform)
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

        #if !SILVERLIGHT
        public System.IO.Stream ToBitmapStream(double width, double height)
        {            
            canvas.Arrange(new System.Windows.Rect(0, 0, width, height));
            var renderTargetBitmap = new RenderTargetBitmap((int)width, (int)height, 96, 96, new PixelFormat());
            renderTargetBitmap.Render(canvas);
            var bitmap = new PngBitmapEncoder();
            bitmap.Frames.Add(BitmapFrame.Create(renderTargetBitmap));
            var bitmapStream = new System.IO.MemoryStream();
            bitmap.Save(bitmapStream);
            return bitmapStream;
        }
        #endif
    }
}
