using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ProjNet.CoordinateSystems.Transformations;
using SharpMap;
using SharpMap.Data;
using SharpMap.Data.Providers;
using SharpMap.Geometries;
using SharpMap.Rendering;
using SharpMap.Rendering.Thematics;
using SharpMap.Styles;
using Windows = System.Windows.Media;

namespace SilverlightRendering
{
    public class SilverlightRenderer : IRenderer
    {
        UIElementCollection elements;

        public SilverlightRenderer(UIElementCollection elements)
        {
            this.elements = elements;
        }

        public void Render(IProvider provider, Func<IFeature, IStyle> getStyle, ICoordinateTransformation coordinateTransformation, IMapTransform mapTransform)
        {
            BoundingBox envelope = mapTransform.Extent;
            provider.Open();
            IFeatures features = provider.GetFeaturesInView(envelope);
            provider.Close();

            foreach (var feature in features.Items)
            {
                if (feature.Geometry is Point)
                    elements.Add(RenderPoint(feature.Geometry as Point, getStyle(feature), mapTransform));
                else if (feature.Geometry is MultiPoint)
                    elements.Add(RenderMultiPoint(feature.Geometry as MultiPoint, getStyle(feature), mapTransform));
                else if (feature.Geometry is LineString)
                    elements.Add(RenderLineString(feature.Geometry as LineString, getStyle(feature), mapTransform));
                else if (feature.Geometry is MultiLineString)
                    elements.Add(RenderMultiLineString(feature.Geometry as MultiLineString, getStyle(feature), mapTransform));
                else if (feature.Geometry is SharpMap.Geometries.Polygon)
                    elements.Add(RenderPolygon(feature.Geometry as SharpMap.Geometries.Polygon, getStyle(feature), mapTransform));
                else if (feature.Geometry is MultiPolygon)
                    elements.Add(RenderMultiPolygon(feature.Geometry as MultiPolygon, getStyle(feature), mapTransform));
            }

        }

        private Path RenderPoint(SharpMap.Geometries.Point point, IStyle style, IMapTransform mapTransform)
        {
            Path path = CreatePointPath(style);
            path.Data = ConvertPoint(point, style, mapTransform);
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

        private static EllipseGeometry ConvertPoint(Point point, IStyle style, IMapTransform mapTransform)
        {
            var vectorStyle = style as VectorStyle;
            Point p = mapTransform.WorldToMap(point);
            EllipseGeometry ellipse = new EllipseGeometry();
            ellipse.Center = new System.Windows.Point(p.X, p.Y);
            ellipse.RadiusX = 10 * vectorStyle.SymbolScale;  //!!! todo: get actual width and height
            ellipse.RadiusY = 10 * vectorStyle.SymbolScale;  //!!! todo: get actual width and height
            return ellipse;
        }

        private static Path RenderMultiPoint(MultiPoint multiPoint, IStyle style, IMapTransform mapTransform)
        {
            Path path = CreatePointPath(style);
            path.Data = ConvertMultiPoint(multiPoint, style, mapTransform);
            return path;
        }

        private static GeometryGroup ConvertMultiPoint(MultiPoint multiPoint, IStyle style, IMapTransform mapTransform)
        {
            var group = new GeometryGroup();
            foreach (Point point in multiPoint)
                group.Children.Add(ConvertPoint(point, style, mapTransform));
            return group;
        }

        private static Path RenderLineString(LineString lineString, IStyle style, IMapTransform mapTransform)
        {
            Path path = CreateLineStringPath(style);
            path.Data = ConvertLineString(lineString, mapTransform);
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

        private static Windows.Geometry ConvertLineString(LineString lineString, IMapTransform mapTransform)
        {
            var pathGeometry = new PathGeometry();
            pathGeometry.Figures.Add(CreatePathFigure(lineString, mapTransform));
            return pathGeometry;
        }

        private static PathFigure CreatePathFigure(LineString linearRing, IMapTransform mapTransform)
        {
            var pathFigure = new PathFigure();
            pathFigure.StartPoint = ConvertPoint(linearRing.StartPoint.WorldToMap(mapTransform));

            foreach (Point point in linearRing.Vertices)
            {
                pathFigure.Segments.Add(
                    new LineSegment() { Point = ConvertPoint(point.WorldToMap(mapTransform)) });
            }
            return pathFigure;
        }

        public static System.Windows.Point ConvertPoint(Point point)
        {
            return new System.Windows.Point((float)point.X, (float)point.Y);
        }

        private static Path RenderMultiLineString(MultiLineString multiLineString, IStyle style, IMapTransform mapTransform)
        {
            Path path = CreateLineStringPath(style);
            path.Data = ConvertMultiLineString(multiLineString, mapTransform);
            return path;
        }

        private static System.Windows.Media.Geometry ConvertMultiLineString(MultiLineString multiLineString, IMapTransform mapTransform)
        {
            var group = new GeometryGroup();
            foreach (LineString lineString in multiLineString)
                group.Children.Add(ConvertLineString(lineString, mapTransform));
            return group;
        }

        private Path RenderPolygon(SharpMap.Geometries.Polygon polygon, IStyle style, IMapTransform mapTransform)
        {
            Path path = CreatePolygonPath(style);
            path.Data = ConvertPolygon(polygon, mapTransform);
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

        private static GeometryGroup ConvertPolygon(SharpMap.Geometries.Polygon polygon, IMapTransform mapTransform)
        {
            var group = new GeometryGroup();
            group.FillRule = FillRule.EvenOdd;
            group.Children.Add(ConvertLinearRing(polygon.ExteriorRing, mapTransform));
            group.Children.Add(ConvertLinearRings(polygon.InteriorRings, mapTransform));
            return group;
        }

        private static PathGeometry ConvertLinearRing(LinearRing linearRing, IMapTransform mapTransform)
        {
            var pathGeometry = new PathGeometry();
            pathGeometry.Figures.Add(CreatePathFigure(linearRing, mapTransform));
            return pathGeometry;
        }

        private static PathGeometry ConvertLinearRings(IList<LinearRing> linearRings, IMapTransform mapTransform)
        {
            var pathGeometry = new PathGeometry();
            foreach (var linearRing in linearRings)
                pathGeometry.Figures.Add(CreatePathFigure(linearRing, mapTransform));
            return pathGeometry;
        }

        private static Path RenderMultiPolygon(MultiPolygon geometry, IStyle style, IMapTransform mapTransform)
        {
            Path path = CreatePolygonPath(style);
            path.Data = ConvertMultiPolygon(geometry, mapTransform);
            return path;
        }

        private static GeometryGroup ConvertMultiPolygon(MultiPolygon geometry, IMapTransform mapTransform)
        {
            var group = new GeometryGroup();
            foreach (SharpMap.Geometries.Polygon polygon in geometry.Polygons)
                group.Children.Add(ConvertPolygon(polygon, mapTransform));
            return group;
        }
    }
}
