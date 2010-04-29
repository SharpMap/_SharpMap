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

namespace SilverlightRendering
{
    public class SilverlightRenderer : IRenderer
    {
        UIElementCollection elements;

        public SilverlightRenderer(UIElementCollection elements)
        {
            this.elements = elements;
        }

        public void RenderLayer(IView view, IProvider provider, Func<IFeature, IStyle> getStyle, ICoordinateTransformation coordinateTransformation)
        {
            BoundingBox envelope = view.Extent;
            provider.Open();
            IFeatures features = provider.GetFeaturesInView(view);
            provider.Close();

            foreach (var feature in features)
            {
                if (feature.Geometry is Point)
                    elements.Add(RenderPoint(feature.Geometry as Point, getStyle(feature), view));
                else if (feature.Geometry is MultiPoint)
                    elements.Add(RenderMultiPoint(feature.Geometry as MultiPoint, getStyle(feature), view));
                else if (feature.Geometry is LineString)
                    elements.Add(RenderLineString(feature.Geometry as LineString, getStyle(feature), view));
                else if (feature.Geometry is MultiLineString)
                    elements.Add(RenderMultiLineString(feature.Geometry as MultiLineString, getStyle(feature), view));
                else if (feature.Geometry is SharpMap.Geometries.Polygon)
                    elements.Add(RenderPolygon(feature.Geometry as SharpMap.Geometries.Polygon, getStyle(feature), view));
                else if (feature.Geometry is MultiPolygon)
                    elements.Add(RenderMultiPolygon(feature.Geometry as MultiPolygon, getStyle(feature), view));
            }

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

            BitmapImage bitmapImage = new BitmapImage();
#if !SILVERLIGHT
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = vectorStyle.Symbol.data;
            bitmapImage.EndInit();
#else
            bitmapImage.SetSource(vectorStyle.Symbol.data);
#endif

            Path path = new Path();
            path.Fill = new ImageBrush() { ImageSource = bitmapImage };
            if (vectorStyle.EnableOutline)
            {
                path.Stroke = new SolidColorBrush(vectorStyle.Outline.Color.Convert());
                path.StrokeThickness = vectorStyle.Outline.Width;
            }
            return path;
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

        #region IRenderer Members


        public void RenderLabelLayer(IView view, IProvider dataSource, LabelLayer labelLayer)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
