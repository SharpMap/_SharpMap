using System;
using System.Drawing;
using GeoAPI.Geometries;
using SharpMap.Layers;
using SharpMap.Styles;

namespace SharpMap.Rendering
{
    public class VectorRendererAdapter : VectorRenderer, IRenderer
    {
        public void Draw(Map map, IGraphics g, Label label)
        {
            throw new NotImplementedException();
        }

        public void Draw(Map map, IGraphics g, IGeometry geom, IStyle style, bool clip)
        {
            Validate(map, g, geom, style);

            VectorStyle s = (VectorStyle)style;
            OgcGeometryType type = geom.OgcGeometryType;
            switch (type)
            {
                case OgcGeometryType.Point:
                    if (s.PointSymbolizer != null)
                    {
                        DrawPoint(s.PointSymbolizer, g, (IPoint)geom, map);
                        return;
                    }
                    if (s.Symbol != null || s.PointColor == null)
                    {
                        DrawPoint(g, (IPoint)geom, s.Symbol, s.SymbolScale, s.SymbolOffset,
                                                 s.SymbolRotation, map);
                        return;
                    }
                    DrawPoint(g, (IPoint)geom, s.PointColor, s.PointSize, s.SymbolOffset, map);
                    break;

                case OgcGeometryType.MultiPoint:
                    if (s.PointSymbolizer != null)
                        DrawMultiPoint(s.PointSymbolizer, g, (IMultiPoint)geom, map);
                    if (s.Symbol != null || s.PointColor == null)
                        DrawMultiPoint(g, (IMultiPoint)geom, s.Symbol, s.SymbolScale, s.SymbolOffset, s.SymbolRotation, map);
                    else DrawMultiPoint(g, (IMultiPoint)geom, s.PointColor, s.PointSize, s.SymbolOffset, map);
                    break;

                case OgcGeometryType.LineString:
                    if (s.LineSymbolizer != null)
                    {
                        s.LineSymbolizer.Render(map, (ILineString)geom, g);
                        return;
                    }
                    DrawLineString(g, (ILineString)geom, s.Line, map, s.LineOffset);
                    return;

                case OgcGeometryType.MultiLineString:
                    if (s.LineSymbolizer != null)
                    {
                        s.LineSymbolizer.Render(map, (IMultiLineString)geom, g);
                        return;
                    }
                    DrawMultiLineString(g, (IMultiLineString)geom, s.Line, map, s.LineOffset);
                    break;

                case OgcGeometryType.Polygon:
                    if (s.EnableOutline)
                        DrawPolygon(g, (IPolygon)geom, s.Fill, s.Outline, clip, map);
                    else DrawPolygon(g, (IPolygon)geom, s.Fill, null, clip, map);
                    break;

                case OgcGeometryType.MultiPolygon:
                    if (s.EnableOutline)
                        DrawMultiPolygon(g, (IMultiPolygon)geom, s.Fill, s.Outline, clip, map);
                    else DrawMultiPolygon(g, (IMultiPolygon)geom, s.Fill, null, clip, map);
                    break;

                case OgcGeometryType.GeometryCollection:
                    IGeometryCollection coll = (IGeometryCollection)geom;
                    for (int i = 0; i < coll.NumGeometries; i++)
                        Draw(map, g, coll[i], s, clip);
                    break;
            }
        }

        public void DrawOutline(Map map, IGraphics g, IGeometry geom, IStyle style)
        {
            Validate(map, g, geom, style);
            VectorStyle s = (VectorStyle)style;
            OgcGeometryType type = geom.OgcGeometryType;
            switch (type)
            {
                case OgcGeometryType.LineString:
                    DrawLineString(g, (ILineString)geom, s.Outline, map, s.LineOffset);
                    break;
                case OgcGeometryType.MultiLineString:
                    DrawMultiLineString(g, (IMultiLineString)geom, s.Outline, map, s.LineOffset);
                    break;
            }                
        }

        private static void Validate(Map map, IGraphics g, IGeometry geom, IStyle style)
        {
            if (map == null)
                throw new ArgumentNullException("map");
            if (g == null)
                throw new ArgumentNullException("g");
            if (geom == null)
                throw new ArgumentNullException("geom");
            if (style == null)
                throw new ArgumentNullException("style");
            if (!(style is VectorStyle))
            {
                string s = String.Format("VectorStyle expected but was {0}", style.GetType().Name);
                throw new ArgumentException(s);
            }
        }
    }
}