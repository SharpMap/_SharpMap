using System;
using System.Collections.Generic;
using System.Drawing;

namespace SharpMap.Rendering
{
    internal static class RendererHelper
    {
        /// <summary>
        /// Function to get the <see cref="SizeF"/> of a string when rendered with the given font.
        /// </summary>
        /// <param name="g"><see cref="Graphics"/> object</param>
        /// <param name="text">the text to render</param>
        /// <param name="font">the font to use</param>
        /// <returns>the size</returns>
        internal static SizeF SizeOfString74(Graphics g, string text, Font font)
        {
            var s = g.MeasureString(text, font);
            return new SizeF(s.Width * 0.74f + 1f, s.Height * 0.74f);
        }

        private static VectorRenderer.SizeOfStringDelegate _sizeOfString;

        /// <summary>
        /// Delegate used to determine the <see cref="SizeF"/> of a given string.
        /// </summary>
        internal static VectorRenderer.SizeOfStringDelegate SizeOfString
        {
            get { return _sizeOfString ?? (_sizeOfString = SizeOfString74); }
            set
            {
                if (value != null)
                    _sizeOfString = value;
            }
        }

        static RendererHelper()
        {
            SizeOfString = SizeOfString74;
        }


        /// <summary>
        /// Offset drawn linestring by given pixel width
        /// </summary>
        /// <param name="points"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        internal static PointF[] OffsetRight(PointF[] points, float offset)
        {
            int length = points.Length;
            PointF[] newPoints = new PointF[(length - 1) * 2];

            float space = (offset * offset / 4) + 1;
            
            if (length < 2) 
                return points;

            //if there are two or more points
            int counter = 0;
            float x = 0, y = 0;
            for (int i = 0; i < length - 1; i++)
            {
                float b = -(points[i + 1].X - points[i].X);
                if (b != 0)
                {
                    float a = points[i + 1].Y - points[i].Y;
                    float c = a / b;
                    y = 2 * (float)Math.Sqrt(space / (c * c + 1));
                    y = b < 0 ? y : -y;
                    x = c * y;

                    if (offset < 0)
                    {
                        y = -y;
                        x = -x;
                    }

                    newPoints[counter] = new PointF(points[i].X + x, points[i].Y + y);
                    newPoints[counter + 1] = new PointF(points[i + 1].X + x, points[i + 1].Y + y);
                }
                else
                {
                    newPoints[counter] = new PointF(points[i].X + x, points[i].Y + y);
                    newPoints[counter + 1] = new PointF(points[i + 1].X + x, points[i + 1].Y + y);
                }
                counter += 2;
            }

            return newPoints;
        }

        /// <summary>
        /// Clips a polygon to the view.
        /// Based on UMN Mapserver renderer 
        /// </summary>
        /// <param name="vertices">vertices in image coordinates</param>
        /// <param name="width">Width of map in image coordinates</param>
        /// <param name="height">Height of map in image coordinates</param>
        /// <returns>Clipped polygon</returns>
        internal static PointF[] ClipPolygon(PointF[] vertices, int width, int height)
        {
            List<PointF> line = new List<PointF>();
            if (vertices.Length <= 1) /* nothing to clip */
                return vertices;

            for (int i = 0; i < vertices.Length - 1; i++)
            {
                float x1 = vertices[i].X;
                float y1 = vertices[i].Y;
                float x2 = vertices[i + 1].X;
                float y2 = vertices[i + 1].Y;

                float deltax = x2 - x1;
                if (deltax == 0f)
                {
                    // bump off of the vertical
                    deltax = (x1 > 0) ? -VectorRenderer.NearZero : VectorRenderer.NearZero;
                }
                float deltay = y2 - y1;
                if (deltay == 0f)
                {
                    // bump off of the horizontal
                    deltay = (y1 > 0) ? -VectorRenderer.NearZero : VectorRenderer.NearZero;
                }

                float xin;
                float xout;
                if (deltax > 0)
                {
                    //  points to right
                    xin = 0;
                    xout = width;
                }
                else
                {
                    xin = width;
                    xout = 0;
                }

                float yin;
                float yout;
                if (deltay > 0)
                {
                    //  points up
                    yin = 0;
                    yout = height;
                }
                else
                {
                    yin = height;
                    yout = 0;
                }

                float tinx = (xin - x1)/deltax;
                float tiny = (yin - y1)/deltay;

                float tin1;
                float tin2;
                if (tinx < tiny)
                {
                    // hits x first
                    tin1 = tinx;
                    tin2 = tiny;
                }
                else
                {
                    // hits y first
                    tin1 = tiny;
                    tin2 = tinx;
                }

                if (1 >= tin1)
                {
                    if (0 < tin1)
                        line.Add(new PointF(xin, yin));

                    if (1 >= tin2)
                    {
                        float toutx = (xout - x1)/deltax;
                        float touty = (yout - y1)/deltay;

                        float tout = (toutx < touty) ? toutx : touty;

                        if (0 < tin2 || 0 < tout)
                        {
                            if (tin2 <= tout)
                            {
                                if (0 < tin2)
                                {
                                    line.Add(tinx > tiny
                                        ? new PointF(xin, y1 + tinx*deltay)
                                        : new PointF(x1 + tiny*deltax, yin));
                                }

                                if (1 > tout)
                                {
                                    line.Add(toutx < touty
                                        ? new PointF(xout, y1 + toutx*deltay)
                                        : new PointF(x1 + touty*deltax, yout));
                                }
                                else
                                    line.Add(new PointF(x2, y2));
                            }
                            else
                            {
                                line.Add(tinx > tiny ? new PointF(xin, yout) : new PointF(xout, yin));
                            }
                        }
                    }
                }
            }
            if (line.Count > 0)
                line.Add(new PointF(line[0].X, line[0].Y));

            return line.ToArray();
        }
    }
}
