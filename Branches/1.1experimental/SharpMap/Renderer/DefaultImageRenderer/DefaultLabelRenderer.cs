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
using System.Globalization;
using SharpMap.CoordinateSystems.Transformations;
using SharpMap.Data;
using SharpMap.Geometries;
using SharpMap.Layers;
using SharpMap.Rendering;
using SharpMap.Styles;

namespace SharpMap.Renderer.DefaultImage
{
    internal class DefaultLabelRenderer
        : DefaultImageRenderer.ILayerRenderer<LabelLayer>
    {
        #region RenderingHelper<LabelLayer> Members
        public void RenderLayer(ILayer layer, Map map, Graphics g)
        {
            RenderLayer((LabelLayer)layer, map, g);
        }
        public void RenderLayer(LabelLayer layer, Map map, Graphics g)
        {
            if (layer.Style.Enabled
                && layer.Style.MaxVisible >= map.Zoom
                && layer.Style.MinVisible < map.Zoom)
            {
                if (layer.DataSource == null)
                    throw (new ApplicationException("DataSource property not set on layer '" + layer.LayerName + "'"));

                g.TextRenderingHint = layer.TextRenderingHint;
                g.SmoothingMode = layer.SmoothingMode;

                SharpMap.Geometries.BoundingBox envelope = map.Envelope; //View to render
                if (layer.CoordinateTransformation != null)
                    envelope = GeometryTransform.TransformBox(envelope, layer.CoordinateTransformation.MathTransform.Inverse());

                FeatureDataSet ds = new FeatureDataSet();
                layer.DataSource.Open();
                layer.DataSource.ExecuteIntersectionQuery(envelope, ds);
                layer.DataSource.Close();
                if (ds.Tables.Count == 0)
                {
                    //base.Render(g, map);
                    return;
                }
                FeatureDataTable features = (FeatureDataTable)ds.Tables[0];

                //Initialize label collection
                List<Label> labels = new List<Label>();
                LabelLayer.GetLabelMethod lblDelegate = layer.LabelStringDelegate;

                //List<System.Drawing.Rectangle> LabelBoxes; //Used for collision detection
                //Render labels
                for (int i = 0; i < features.Count; i++)
                {
                    FeatureDataRow feature = features[i];
                    if (layer.CoordinateTransformation != null)
                        features[i].Geometry = GeometryTransform.TransformGeometry(features[i].Geometry, layer.CoordinateTransformation.MathTransform);

                    ILabelStyle style = null;
                    if (layer.Theme != null) //If thematics is enabled, lets override the style
                        style = layer.Theme.GetStyle(feature);
                    else
                        style = layer.Style;

                    float rotation = 0;
                    if (!String.IsNullOrEmpty(layer.RotationColumn))
                        float.TryParse(feature[layer.RotationColumn].ToString(), NumberStyles.Any, Map.numberFormat_EnUS, out rotation);

                    string text;
                    if (lblDelegate != null)
                        text = lblDelegate(feature);
                    else
                        text = feature[layer.LabelColumn].ToString();

                    if (text != null && text != String.Empty)
                    {
                        if (feature.Geometry is GeometryCollection)
                        {
                            if (layer.MultipartGeometryBehaviour == SharpMap.Layers.LabelLayer.MultipartGeometryBehaviourEnum.All)
                            {
                                foreach (SharpMap.Geometries.Geometry geom in (feature.Geometry as GeometryCollection))
                                {
                                    SharpMap.Rendering.Label lbl = CreateLabel(layer, geom, text, rotation, style, map, g);
                                    if (lbl != null)
                                        labels.Add(lbl);
                                }
                            }
                            else if (layer.MultipartGeometryBehaviour == SharpMap.Layers.LabelLayer.MultipartGeometryBehaviourEnum.CommonCenter)
                            {
                                Label lbl = CreateLabel(layer, feature.Geometry, text, rotation, style, map, g);
                                if (lbl != null)
                                    labels.Add(lbl);
                            }
                            else if (layer.MultipartGeometryBehaviour == SharpMap.Layers.LabelLayer.MultipartGeometryBehaviourEnum.First)
                            {
                                if ((feature.Geometry as GeometryCollection).Collection.Count > 0)
                                {
                                    SharpMap.Rendering.Label lbl = CreateLabel(layer, (feature.Geometry as GeometryCollection).Collection[0], text, rotation, style, map, g);
                                    if (lbl != null)
                                        labels.Add(lbl);
                                }
                            }
                            else if (layer.MultipartGeometryBehaviour == SharpMap.Layers.LabelLayer.MultipartGeometryBehaviourEnum.Largest)
                            {
                                GeometryCollection coll = (feature.Geometry as GeometryCollection);
                                if (coll.NumGeometries > 0)
                                {
                                    double largestVal = 0;
                                    int idxOfLargest = 0;
                                    for (int j = 0; j < coll.NumGeometries; j++)
                                    {
                                        SharpMap.Geometries.Geometry geom = coll.Geometry(j);
                                        if (geom is Geometries.LineString && ((Geometries.LineString)geom).Length > largestVal)
                                        {
                                            largestVal = ((Geometries.LineString)geom).Length;
                                            idxOfLargest = j;
                                        }
                                        if (geom is Geometries.MultiLineString && ((Geometries.MultiLineString)geom).Length > largestVal)
                                        {
                                            largestVal = ((Geometries.LineString)geom).Length;
                                            idxOfLargest = j;
                                        }
                                        if (geom is Geometries.Polygon && ((Geometries.Polygon)geom).Area > largestVal)
                                        {
                                            largestVal = ((Geometries.Polygon)geom).Area;
                                            idxOfLargest = j;
                                        }
                                        if (geom is Geometries.MultiPolygon && ((Geometries.MultiPolygon)geom).Area > largestVal)
                                        {
                                            largestVal = ((Geometries.MultiPolygon)geom).Area;
                                            idxOfLargest = j;
                                        }
                                    }

                                    SharpMap.Rendering.Label lbl = CreateLabel(layer, coll.Geometry(idxOfLargest), text, rotation, style, map, g);
                                    if (lbl != null)
                                        labels.Add(lbl);
                                }
                            }
                        }
                        else
                        {
                            SharpMap.Rendering.Label lbl = CreateLabel(layer, feature.Geometry, text, rotation, style, map, g);
                            if (lbl != null)
                                labels.Add(lbl);
                        }
                    }
                }
                if (labels.Count > 0) //We have labels to render...
                {
                    if (layer.Style.CollisionDetection && layer.LabelFilter != null)
                        layer.LabelFilter(labels);
                    for (int i = 0; i < labels.Count; i++)
                        VectorRenderer.DrawLabel(g, labels[i].LabelPoint, labels[i].Style.Offset, labels[i].Style.Font, labels[i].Style.ForeColor, labels[i].Style.BackColor, layer.Style.Halo, labels[i].Rotation, labels[i].Text, map);
                }
                labels = null;
            }
            //base.Render(g, map);
        }

        private Label CreateLabel(ILabelLayer layer, Geometry feature, string text, float rotation, ILabelStyle style, Map map, Graphics g)
        {
            System.Drawing.SizeF size = g.MeasureString(text, style.Font);

            System.Drawing.PointF position = map.WorldToImage(feature.GetBoundingBox().GetCentroid());
            position.X = position.X - size.Width * (short)style.HorizontalAlignment * 0.5f;
            position.Y = position.Y - size.Height * (short)style.VerticalAlignment * 0.5f;
            if (position.X - size.Width > map.Size.Width || position.X + size.Width < 0 ||
                position.Y - size.Height > map.Size.Height || position.Y + size.Height < 0)
                return null;
            else
            {
                SharpMap.Rendering.Label lbl;

                if (!style.CollisionDetection)
                    lbl = new Label(text, position, rotation, layer.Priority, null, style);
                else
                {
                    //Collision detection is enabled so we need to measure the size of the string
                    lbl = new Label(text, position, rotation, layer.Priority,
                        new LabelBox(position.X - size.Width * 0.5f - style.CollisionBuffer.Width, position.Y + size.Height * 0.5f + style.CollisionBuffer.Height,
                        size.Width + 2f * style.CollisionBuffer.Width, size.Height + style.CollisionBuffer.Height * 2f), style);
                }
                if (feature.GetType() == typeof(SharpMap.Geometries.LineString))
                {
                    SharpMap.Geometries.LineString line = feature as SharpMap.Geometries.LineString;
                    if (line.Length / map.PixelSize > size.Width) //Only label feature if it is long enough
                        CalculateLabelOnLinestring(line, ref lbl, map);
                    else
                        return null;
                }

                return lbl;
            }
        }

        private void CalculateLabelOnLinestring(SharpMap.Geometries.LineString line, ref Label label, Map map)
        {
            double dx, dy;
            double tmpx, tmpy;
            double angle = 0.0;

            // first find the middle segment of the line
            int midPoint = (line.Vertices.Count - 1) / 2;
            if (line.Vertices.Count > 2)
            {
                dx = line.Vertices[midPoint + 1].X - line.Vertices[midPoint].X;
                dy = line.Vertices[midPoint + 1].Y - line.Vertices[midPoint].Y;
            }
            else
            {
                midPoint = 0;
                dx = line.Vertices[1].X - line.Vertices[0].X;
                dy = line.Vertices[1].Y - line.Vertices[0].Y;
            }
            if (dy == 0)
                label.Rotation = 0;
            else if (dx == 0)
                label.Rotation = 90;
            else
            {
                // calculate angle of line					
                angle = -Math.Atan(dy / dx) + Math.PI * 0.5;
                angle *= (180d / Math.PI); // convert radians to degrees
                label.Rotation = (float)angle - 90; // -90 text orientation
            }
            tmpx = line.Vertices[midPoint].X + (dx * 0.5);
            tmpy = line.Vertices[midPoint].Y + (dy * 0.5);
            label.LabelPoint = map.WorldToImage(new SharpMap.Geometries.Point(tmpx, tmpy));
        }


        #endregion
    }
}
