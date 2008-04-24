using System.Collections.Generic;

namespace SharpMap.Renderers.ImageMap.Impl
{
    internal class ImageMapUtilities
    {
        /// <summary>
        /// Clips a polygon to the view.
        /// Based on UMN Mapserver renderer
        /// </summary>
        /// <param name="vertices">vertices in image coordinates</param>
        /// <param name="width">Width of map in image coordinates</param>
        /// <param name="height">Height of map in image coordinates</param>
        /// <returns>Clipped polygon</returns>
        internal static System.Drawing.PointF[] ClipPolygon(System.Drawing.PointF[] vertices, int width, int height)
        {
            float deltax, deltay, xin, xout, yin, yout;
            float tinx, tiny, toutx, touty, tin1, tin2, tout;
            float x1, y1, x2, y2;

            List<System.Drawing.PointF> line = new List<System.Drawing.PointF>();
            if (vertices.Length <= 1) /* nothing to clip */
                return vertices;
            /*
            ** Don't do any clip processing of shapes completely within the
            ** clip rectangle based on a comparison of bounds.   We could do 
            ** something similar for completely outside, but that rarely occurs
            ** since the spatial query at the layer read level has generally already
            ** discarded all shapes completely outside the rect.
            */

            // TODO
            //if (vertices.bounds.maxx <= width
            //		&& vertices.bounds.minx >= 0
            //		&& vertices.bounds.maxy <= height
            //		&& vertices.bounds.miny >= 0)
            //	{
            //		return vertices;
            //	}


            //line.point = (pointObj*)malloc(sizeof(pointObj) * 2 * shape->line[j].numpoints + 1); /* worst case scenario, +1 allows us to duplicate the 1st and last point */
            //line.numpoints = 0;

            for (int i = 0; i < vertices.Length - 1; i++)
            {
                x1 = vertices[i].X;
                y1 = vertices[i].Y;
                x2 = vertices[i + 1].X;
                y2 = vertices[i + 1].Y;

                deltax = x2 - x1;
                if (deltax == 0)
                {	// bump off of the vertical
                    deltax = (x1 > 0) ? -float.MinValue : float.MinValue;
                }
                deltay = y2 - y1;
                if (deltay == 0)
                {	// bump off of the horizontal
                    deltay = (y1 > 0) ? -float.MinValue : float.MinValue;
                }

                if (deltax > 0)
                {   //  points to right
                    xin = 0;
                    xout = width;
                }
                else
                {
                    xin = width;
                    xout = 0;
                }

                if (deltay > 0)
                {   //  points up
                    yin = 0;
                    yout = height;
                }
                else
                {
                    yin = height;
                    yout = 0;
                }

                tinx = (xin - x1) / deltax;
                tiny = (yin - y1) / deltay;

                if (tinx < tiny)
                {   // hits x first
                    tin1 = tinx;
                    tin2 = tiny;
                }
                else
                {   // hits y first
                    tin1 = tiny;
                    tin2 = tinx;
                }

                if (1 >= tin1)
                {
                    if (0 < tin1)
                        line.Add(new System.Drawing.PointF(xin, yin));

                    if (1 >= tin2)
                    {
                        toutx = (xout - x1) / deltax;
                        touty = (yout - y1) / deltay;

                        tout = (toutx < touty) ? toutx : touty;

                        if (0 < tin2 || 0 < tout)
                        {
                            if (tin2 <= tout)
                            {
                                if (0 < tin2)
                                {
                                    if (tinx > tiny)
                                        line.Add(new System.Drawing.PointF(xin, y1 + tinx * deltay));
                                    else
                                        line.Add(new System.Drawing.PointF(x1 + tiny * deltax, yin));
                                }

                                if (1 > tout)
                                {
                                    if (toutx < touty)
                                        line.Add(new System.Drawing.PointF(xout, y1 + toutx * deltay));
                                    else
                                        line.Add(new System.Drawing.PointF(x1 + touty * deltax, yout));
                                }
                                else
                                    line.Add(new System.Drawing.PointF(x2, y2));
                            }
                            else
                            {
                                if (tinx > tiny)
                                    line.Add(new System.Drawing.PointF(xin, yout));
                                else
                                    line.Add(new System.Drawing.PointF(xout, yin));
                            }
                        }
                    }
                }
            }
            if (line.Count > 0)
            {
                line.Add(new System.Drawing.PointF(line[0].Y, line[0].Y));
            }
            return line.ToArray();
        }
    }
}
