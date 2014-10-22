// Copyright 2005, 2006 - Morten Nielsen (www.iter.dk)
//
// This file is part of SharpMap.
// SharpMap is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
// 
// SharpMap is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.

// You should have received a copy of the GNU Lesser General Public License
// along with SharpMap; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA 

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;
using GeoAPI.Geometries;
using SharpMap.Rendering.Symbolizer;
using SharpMap.Styles;
using SharpMap.Utilities;
using Point=GeoAPI.Geometries.Coordinate;
using System.Runtime.CompilerServices;

namespace SharpMap.Rendering
{
    /// <summary>
    /// This class renders individual geometry features to a graphics object using the settings of a map object.
    /// </summary>
    public class VectorRenderer
    {
        internal const float ExtremeValueLimit = 1E+8f;
        internal const float NearZero = 1E-30f; // 1/Infinity

        private readonly Bitmap Defaultsymbol =
            (Bitmap)
            Image.FromStream(
                Assembly.GetExecutingAssembly().GetManifestResourceStream("SharpMap.Styles.DefaultSymbol.png"));

        /// <summary>
        /// Renders a MultiLineString to the map.
        /// </summary>
        /// <param name="g">Graphics reference</param>
        /// <param name="lines">MultiLineString to be rendered</param>
        /// <param name="pen">Pen style used for rendering</param>
        /// <param name="map">Map reference</param>
        /// <param name="offset">Offset by which line will be moved to right</param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void DrawMultiLineString(Graphics g, IMultiLineString lines, Pen pen, Map map, float offset)
        {
            for(var i = 0; i < lines.NumGeometries; i++)
            {
                var line = (ILineString) lines[i];
                DrawLineString(g, line, pen, map, offset);
            }
        }

        /// <summary>
        /// Renders a LineString to the map.
        /// </summary>
        /// <param name="g">Graphics reference</param>
        /// <param name="line">LineString to render</param>
        /// <param name="pen">Pen style used for rendering</param>
        /// <param name="map">Map reference</param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void DrawLineString(Graphics g, ILineString line, Pen pen, Map map)
        {
            DrawLineString(g, line, pen, map, 0);
        }
        /// <summary>
        /// Renders a LineString to the map.
        /// </summary>
        /// <param name="g">Graphics reference</param>
        /// <param name="line">LineString to render</param>
        /// <param name="pen">Pen style used for rendering</param>
        /// <param name="map">Map reference</param>
        /// <param name="offset">Offset by which line will be moved to right</param>
        public void DrawLineString(Graphics g, ILineString line, Pen pen, Map map, float offset)
        {
            var points = line.TransformToImage(map);
            if (points.Length > 1)
            {
                var gp = new GraphicsPath();
                if (offset != 0d)
                    points = RendererHelper.OffsetRight(points, offset);
                gp.AddLines(/*LimitValues(*/points/*, ExtremeValueLimit)*/);

                g.DrawPath(pen, gp);
            }
        }

        /// <summary>
        /// Renders a multipolygon byt rendering each polygon in the collection by calling DrawPolygon.
        /// </summary>
        /// <param name="g">Graphics reference</param>
        /// <param name="pols">MultiPolygon to render</param>
        /// <param name="brush">Brush used for filling (null or transparent for no filling)</param>
        /// <param name="pen">Outline pen style (null if no outline)</param>
        /// <param name="clip">Specifies whether polygon clipping should be applied</param>
        /// <param name="map">Map reference</param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void DrawMultiPolygon(Graphics g, IMultiPolygon pols, Brush brush, Pen pen, bool clip, Map map)
        {
            for (var i = 0; i < pols.NumGeometries;i++ )
            {
                var p = (IPolygon) pols[i];
                DrawPolygon(g, p, brush, pen, clip, map);
            }
        }

        /// <summary>
        /// Renders a polygon to the map.
        /// </summary>
        /// <param name="g">Graphics reference</param>
        /// <param name="pol">Polygon to render</param>
        /// <param name="brush">Brush used for filling (null or transparent for no filling)</param>
        /// <param name="pen">Outline pen style (null if no outline)</param>
        /// <param name="clip">Specifies whether polygon clipping should be applied</param>
        /// <param name="map">Map reference</param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void DrawPolygon(Graphics g, IPolygon pol, Brush brush, Pen pen, bool clip, Map map)
        {
            if (pol.ExteriorRing == null)
                return;

            var points = pol.ExteriorRing.TransformToImage(map);
            if (points.Length > 2)
            {
                //Use a graphics path instead of DrawPolygon. DrawPolygon has a problem with several interior holes
                var gp = new GraphicsPath();

                //Add the exterior polygon
                if (!clip)
                    gp.AddPolygon(/*LimitValues(*/points/*, ExtremeValueLimit)*/);
                else
                    DrawPolygonClipped(gp, /*LimitValues(*/points/*, ExtremeValueLimit)*/, map.Size.Width, map.Size.Height);

                //Add the interior polygons (holes)
                if (pol.NumInteriorRings > 0)
                {
                    foreach (ILinearRing ring in pol.InteriorRings)
                    {
                        points = ring.TransformToImage(map);
                        if (!clip)
                            gp.AddPolygon(/*LimitValues(*/points/*, ExtremeValueLimit)*/);
                        else
                            DrawPolygonClipped(gp, /*LimitValues(*/points/*, ExtremeValueLimit)*/, map.Size.Width,
                                               map.Size.Height);
                    }
                }

                
                // Only render inside of polygon if the brush isn't null or isn't transparent
                if (brush != null && brush != Brushes.Transparent)
                    g.FillPath(brush, gp);

                // Create an outline if a pen style is available
                if (pen != null)
                    g.DrawPath(pen, gp);
            }
        }

        private void DrawPolygonClipped(GraphicsPath gp, PointF[] polygon, int width, int height)
        {
            var clipState = DetermineClipState(polygon, width, height);
            if (clipState == ClipState.Within)
            {
                gp.AddPolygon(polygon);
            }
            else if (clipState == ClipState.Intersecting)
            {
                var clippedPolygon = RendererHelper.ClipPolygon(polygon, width, height);
                if (clippedPolygon.Length > 2)
                    gp.AddPolygon(clippedPolygon);
            }
        }

        /// <summary>
        /// Purpose of this method is to prevent the 'overflow error' exception in the FillPath method. 
        /// This Exception is thrown when the coordinate values become too big (values over -2E+9f always 
        /// throw an exception, values under 1E+8f seem to be okay). This method limits the coordinates to 
        /// the values given by the second parameter (plus an minus). Theoretically the lines to and from 
        /// these limited points are not correct but GDI+ paints incorrect even before that limit is reached. 
        /// </summary>
        /// <param name="vertices">The vertices that need to be limited</param>
        /// <param name="limit">The limit at which coordinate values will be cutoff</param>
        /// <returns>The limited vertices</returns>
        private PointF[] LimitValues(PointF[] vertices, float limit)
        {
            for (var i = 0; i < vertices.Length; i++)
            {
                vertices[i].X = Math.Max(-limit, Math.Min(limit, vertices[i].X));
                vertices[i].Y = Math.Max(-limit, Math.Min(limit, vertices[i].Y));
            }
            return vertices;
        }

        /// <summary>
        /// Signature for a function that evaluates the length of a string when rendered on a Graphics object with a given font
        /// </summary>
        /// <param name="g"><see cref="Graphics"/> object</param>
        /// <param name="text">the text to render</param>
        /// <param name="font">the font to use</param>
        /// <returns>the size</returns>
        public delegate SizeF SizeOfStringDelegate(Graphics g, string text, Font font);

        /// <summary>
        /// Function to get the <see cref="SizeF"/> of a string when rendered with the given font.
        /// </summary>
        /// <param name="g"><see cref="Graphics"/> object</param>
        /// <param name="text">the text to render</param>
        /// <param name="font">the font to use</param>
        /// <returns>the size</returns>
        public SizeF SizeOfStringBase(Graphics g, string text, Font font)
        {
            return g.MeasureString(text, font);
        }
        
        /// <summary>
        /// Function to get the <see cref="SizeF"/> of a string when rendered with the given font.
        /// </summary>
        /// <param name="g"><see cref="Graphics"/> object</param>
        /// <param name="text">the text to render</param>
        /// <param name="font">the font to use</param>
        /// <returns>the size</returns>
        public SizeF SizeOfStringCeiling(Graphics g, string text, Font font)
        {
            SizeF f = g.MeasureString(text, font);
            return new SizeF((float)Math.Ceiling(f.Width), (float)Math.Ceiling(f.Height));
        }


        /// <summary>
        /// Renders a label to the map.
        /// </summary>
        /// <param name="g">Graphics reference</param>
        /// <param name="labelPoint">Label placement</param>
        /// <param name="offset">Offset of label in screen coordinates</param>
        /// <param name="font">Font used for rendering</param>
        /// <param name="forecolor">Font forecolor</param>
        /// <param name="backcolor">Background color</param>
        /// <param name="halo">Color of halo</param>
        /// <param name="rotation">Text rotation in degrees</param>
        /// <param name="text">Text to render</param>
        /// <param name="map">Map reference</param>
        /// <param name="alignment">Horizontal alignment for multi line labels. If not set <see cref="StringAlignment.Near"/> is used</param>
        /// <param name="rotationPoint">Point where the rotation should take place</param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void DrawLabel(Graphics g, PointF labelPoint, PointF offset, Font font, Color forecolor,
                                     Brush backcolor, Pen halo, float rotation, string text, Map map,
                                     LabelStyle.HorizontalAlignmentEnum alignment = LabelStyle.HorizontalAlignmentEnum.Left,
                                     PointF? rotationPoint = null)

        {
            //Calculate the size of the text
            var labelSize = RendererHelper.SizeOfString(g, text, font);
            
            //Add label offset
            labelPoint.X += offset.X;
            labelPoint.Y += offset.Y;

            //Translate alignment to stringalignment
            StringAlignment salign;
            switch (alignment)
            {
                case LabelStyle.HorizontalAlignmentEnum.Left:
                    salign = StringAlignment.Near;
                    break;
                case LabelStyle.HorizontalAlignmentEnum.Center:
                    salign = StringAlignment.Center;
                    break;
                default:
                    salign = StringAlignment.Far;
                    break;
            }

            if (rotation != 0 && !float.IsNaN(rotation))
            {
                rotationPoint = rotationPoint ?? labelPoint;

                g.FillEllipse(Brushes.LawnGreen, rotationPoint.Value.X - 1, rotationPoint.Value.Y - 1, 2, 2);
                
                var t = g.Transform.Clone();
                g.TranslateTransform(rotationPoint.Value.X, rotationPoint.Value.Y);
                g.RotateTransform(rotation);
                //g.TranslateTransform(-labelSize.Width/2, -labelSize.Height/2);

                labelPoint = new PointF(labelPoint.X - rotationPoint.Value.X,
                                        labelPoint.Y - rotationPoint.Value.Y);

                //labelSize = new SizeF(labelSize.Width*0.74f + 1f, labelSize.Height*0.74f);
                if (backcolor != null && backcolor != Brushes.Transparent)
                    g.FillRectangle(backcolor, labelPoint.X, labelPoint.Y, labelSize.Width, labelSize.Height);

                var path = new GraphicsPath();
                path.AddString(text, font.FontFamily, (int) font.Style, font.Size,
                               new RectangleF(labelPoint, labelSize) /* labelPoint*/, 
                               new StringFormat { Alignment = salign } /*null*/);
                if (halo != null)
                    g.DrawPath(halo, path);

                g.FillPath(new SolidBrush(forecolor), path);
                //g.DrawString(text, font, new System.Drawing.SolidBrush(forecolor), 0, 0);        
                g.Transform = t;
            }
            else
            {
                if (backcolor != null && backcolor != Brushes.Transparent)
                    g.FillRectangle(backcolor, labelPoint.X, labelPoint.Y, labelSize.Width,
                                    labelSize.Height);

                var path = new GraphicsPath();
                path.AddString(text, font.FontFamily, (int) font.Style, font.Size, 
                               new RectangleF(labelPoint, labelSize) /* labelPoint*/,
                               new StringFormat { Alignment = salign } /*null*/);
                if (halo != null)
                    g.DrawPath(halo, path);
                g.FillPath(new SolidBrush(forecolor), path);
                //g.DrawString(text, font, new System.Drawing.SolidBrush(forecolor), LabelPoint.X, LabelPoint.Y);
            }
        }

        private ClipState DetermineClipState(PointF[] vertices, int width, int height)
        {
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;

            for (int i = 0; i < vertices.Length; i++)
            {
                minX = Math.Min(minX, vertices[i].X);
                minY = Math.Min(minY, vertices[i].Y);
                maxX = Math.Max(maxX, vertices[i].X);
                maxY = Math.Max(maxY, vertices[i].Y);
            }

            if (maxX < 0) return ClipState.Outside;
            if (maxY < 0) return ClipState.Outside;
            if (minX > width) return ClipState.Outside;
            if (minY > height) return ClipState.Outside;
            if (minX > 0 && maxX < width && minY > 0 && maxY < height) return ClipState.Within;
            return ClipState.Intersecting;
        }

        /// <summary>
        /// Renders a point to the map.
        /// </summary>
        /// <param name="g">Graphics reference</param>
        /// <param name="point">Point to render</param>
        /// <param name="b">Brush reference</param>
        /// <param name="size">Size of drawn Point</param>
        /// <param name="offset">Symbol offset af scale=1</param>
        /// <param name="map">Map reference</param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void DrawPoint(Graphics g, IPoint point, Brush b, float size, PointF offset, Map map)
        {
            if (point == null)
                return;

            var pp = Transform.WorldtoMap(point.Coordinate, map);
            //var startingTransform = g.Transform;

            var width = size;
            var height = size;

            g.FillEllipse(b, (int)pp.X - width / 2 + offset.X ,
                        (int)pp.Y - height / 2 + offset.Y , width, height);
        }

        /// <summary>
        /// Renders a point to the map.
        /// </summary>
        /// <param name="g">Graphics reference</param>
        /// <param name="point">Point to render</param>
        /// <param name="symbolizer">Symbolizer to decorate point</param>
        /// <param name="map">Map reference</param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void DrawPoint(IPointSymbolizer symbolizer, Graphics g, IPoint point, Map map)
        {
            if (point == null)
                return; 

            symbolizer.Render(map, point, g);
        }

        /// <summary>
        /// Renders a point to the map.
        /// </summary>
        /// <param name="g">Graphics reference</param>
        /// <param name="point">Point to render</param>
        /// <param name="symbol">Symbol to place over point</param>
        /// <param name="symbolscale">The amount that the symbol should be scaled. A scale of '1' equals to no scaling</param>
        /// <param name="offset">Symbol offset af scale=1</param>
        /// <param name="rotation">Symbol rotation in degrees</param>
        /// <param name="map">Map reference</param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void DrawPoint(Graphics g, IPoint point, Image symbol, float symbolscale, PointF offset,
                                     float rotation, Map map)
        {
            if (point == null)
                return;
            
            if (symbol == null) //We have no point style - Use a default symbol
                symbol = Defaultsymbol;


            var pp = Transform.WorldtoMap(point.Coordinate, map);

            lock (symbol)
            {
                if (rotation != 0 && !Single.IsNaN(rotation))
                {
                    var startingTransform = g.Transform.Clone();

                    var transform = g.Transform;
                    var rotationCenter = pp;
                    transform.RotateAt(rotation, rotationCenter);
                    g.Transform = transform;

                    //if (symbolscale == 1f)
                    //{
                    //    g.DrawImage(symbol,  (pp.X - symbol.Width/2f + offset.X),
                    //                                (pp.Y - symbol.Height/2f + offset.Y));
                    //}
                    //else
                    //{
                    //    var width = symbol.Width*symbolscale;
                    //    var height = symbol.Height*symbolscale;
                    //    g.DrawImage(symbol, (int) pp.X - width/2 + offset.X*symbolscale,
                    //                        (int) pp.Y - height/2 + offset.Y*symbolscale, width, height);
                    //}
                    var width = symbol.Width * symbolscale;
                    var height = symbol.Height * symbolscale;
                    g.DrawImage(symbol, pp.X - width / 2 + offset.X * symbolscale,
                                        pp.Y - height / 2 + offset.Y * symbolscale, width, height);
                    g.Transform = startingTransform;
                }
                else
                {
                    //if (symbolscale == 1f)
                    //{
                    //    g.DrawImageUnscaled(symbol, (int) (pp.X - symbol.Width/2f + offset.X),
                    //                                (int) (pp.Y - symbol.Height/2f + offset.Y));
                    //}
                    //else
                    //{
                    //    var width = symbol.Width*symbolscale;
                    //    var height = symbol.Height*symbolscale;
                    //    g.DrawImage(symbol, (int) pp.X - width/2 + offset.X*symbolscale,
                    //                        (int) pp.Y - height/2 + offset.Y*symbolscale, width, height);
                    //}
                    var width = symbol.Width * symbolscale;
                    var height = symbol.Height * symbolscale;
                    g.DrawImage(symbol, pp.X - width / 2 + offset.X * symbolscale,
                                        pp.Y - height / 2 + offset.Y * symbolscale, width, height);
                }
            }
        }

        /// <summary>
        /// Renders a <see cref="GeoAPI.Geometries.IMultiPoint"/> to the map.
        /// </summary>
        /// <param name="g">Graphics reference</param>
        /// <param name="points">MultiPoint to render</param>
        /// <param name="symbol">Symbol to place over point</param>
        /// <param name="symbolscale">The amount that the symbol should be scaled. A scale of '1' equals to no scaling</param>
        /// <param name="offset">Symbol offset af scale=1</param>
        /// <param name="rotation">Symbol rotation in degrees</param>
        /// <param name="map">Map reference</param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void DrawMultiPoint(Graphics g, IMultiPoint points, Image symbol, float symbolscale, PointF offset,
                                          float rotation, Map map)
        {
            for (var i = 0; i < points.NumGeometries; i++)
            {
                var point = (IPoint) points[i];
                DrawPoint(g, point, symbol, symbolscale, offset, rotation, map);
            }
        }

        /// <summary>
        /// Renders a <see cref="GeoAPI.Geometries.IMultiPoint"/> to the map.
        /// </summary>
        /// <param name="g">Graphics reference</param>
        /// <param name="points">MultiPoint to render</param>
        /// <param name="symbolizer">Symbolizer to decorate point</param>
        /// <param name="map">Map reference</param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void DrawMultiPoint(IPointSymbolizer symbolizer, Graphics g, IMultiPoint points, Map map)
        {
            symbolizer.Render(map, points, g);
        }

        /// <summary>
        /// Renders a <see cref="GeoAPI.Geometries.IMultiPoint"/> to the map.
        /// </summary>
        /// <param name="g">Graphics reference</param>
        /// <param name="points">MultiPoint to render</param>
        /// <param name="brush">Brush reference</param>
        /// <param name="size">Size of drawn Point</param>
        /// <param name="offset">Symbol offset af scale=1</param>
        /// <param name="map">Map reference</param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void DrawMultiPoint(Graphics g, IMultiPoint points, Brush brush, float size, PointF offset, Map map)
        {
            for (var i = 0; i < points.NumGeometries; i++)
            {
                var point = (IPoint) points[i];
                DrawPoint(g, point, brush, size, offset, map);
            }
        }

        #region Nested type: ClipState

        private enum ClipState
        {
            Within,
            Outside,
            Intersecting
        } ;

        #endregion        
    }
}