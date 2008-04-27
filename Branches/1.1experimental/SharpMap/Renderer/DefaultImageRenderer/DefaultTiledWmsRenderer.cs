/*
 *	This file is part of SharpMap
 *  SharpMap is free software. This file © 2008 Newgrove Consultants Limited, 
 *  http://www.newgrove.com; you can redistribute it and/or modify it under the terms 
 *  of the current GNU Lesser General Public License (LGPL) as published by and 
 *  available from the Free Software Foundation, Inc., 
 *  59 Temple Place, Suite 330, Boston, MA 02111-1307 USA: http://fsf.org/    
 *  This program is distributed without any warranty; 
 *  without even the implied warranty of merchantability or fitness for purpose.  
 *  See the GNU Lesser General Public License for the full details. 
 *  
 *  Author: John Diss 2008
 *  
 *  Portions based on earlier work.
 * 
 */
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using SharpMap.Geometries;
using SharpMap.Layers;
using SharpMap.Web.Wms.Tiling;

namespace SharpMap.Renderer.DefaultImage
{
    public class DefaultTiledWmsRenderer : DefaultImageRenderer.ILayerRenderer<TiledWmsLayer>
    {
        #region ILayerRenderer<TiledWmsLayer> Members

        public void RenderLayer(TiledWmsLayer layer, Map map, System.Drawing.Graphics g)
        {
            System.Drawing.Bitmap bitmap = null;

            try
            {
                foreach (string key in layer.TileSetsActive)
                {
                    TileSet tileSet = layer.TileSets[key];

                    tileSet.Verify();

                    List<BoundingBox> tileExtents = TileExtents.GetTileExtents(tileSet, map.Envelope, map.PixelSize);

                    //TODO: Retrieve several tiles at the same time asynchronously to improve performance. PDD.
                    foreach (BoundingBox tileExtent in tileExtents)
                    {
                        if (bitmap != null) { bitmap.Dispose(); }

                        if ((tileSet.TileCache != null) && (tileSet.TileCache.ContainsTile(tileExtent)))
                        {
                            bitmap = tileSet.TileCache.GetTile(tileExtent);
                        }
                        else
                        {
                            bitmap = layer.WmsGetMap(tileExtent, tileSet);
                            if ((tileSet.TileCache != null) && (bitmap != null))
                            {
                                tileSet.TileCache.AddTile(tileExtent, bitmap);
                            }
                        }

                        if (bitmap != null)
                        {
                            PointF destMin = SharpMap.Utilities.Transform.WorldtoMap(tileExtent.Min, map);
                            PointF destMax = SharpMap.Utilities.Transform.WorldtoMap(tileExtent.Max, map);

                            #region Comment on BorderBug correction
                            // Even when tiles border to one another without any space between them there are 
                            // seams visible between the tiles in the map image.
                            // This problem could be resolved with the solution suggested here:
                            // http://www.codeproject.com/csharp/BorderBug.asp
                            // The suggested correction value of 0.5f still results in seams in some cases, not so with 0.4999f.
                            // Also it was necessary to apply Math.Round and Math.Ceiling on the destination rectangle.
                            // PDD.
                            #endregion

                            float correction = 0.4999f;
                            RectangleF srcRect = new RectangleF(0 - correction, 0 - correction, tileSet.Width, tileSet.Height);

                            InterpolationMode tempInterpolationMode = g.InterpolationMode;
                            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;

                            //TODO: Allow custom image attributes for each TileSet.

                            int x = (int)Math.Round(destMin.X);
                            int y = (int)Math.Round(destMax.Y);
                            int width = (int)Math.Round(destMax.X - x);
                            int height = (int)Math.Round(destMin.Y - y);

                            g.DrawImage(bitmap, new Rectangle(x, y, width, height),
                              srcRect.Left, srcRect.Top, srcRect.Width, srcRect.Height,
                              GraphicsUnit.Pixel, layer.ImageAttributes);

                            g.InterpolationMode = tempInterpolationMode; //Put InterpolationMode back so drawing of other layers is not affected.
                        }
                    }
                }
            }
            finally
            {
                if (bitmap != null)
                {
                    bitmap.Dispose();
                }
            }
        }

        #endregion

        #region ILayerRenderer Members

        public void RenderLayer(SharpMap.Layers.ILayer layer, Map map, System.Drawing.Graphics g)
        {
            RenderLayer((TiledWmsLayer)layer, map, g);
        }

        #endregion
    }
}
