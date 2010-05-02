﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using SharpMap.Styles;
using SharpMap.Geometries;
using SharpMap.Rendering;
using SharpMap;
using SharpMap.Data;
using SharpMap.Data.Providers;
using System.Drawing.Drawing2D;
using SharpMap.Layers;
using System.Globalization;

namespace GdiRendering
{
    public static class LabelRenderer
    {
        /// <summary>
        /// Renders the layer
        /// </summary>
        /// <param name="g">Graphics object reference</param>
        /// <param name="map">Map which is rendered</param>
        public static void Render(Graphics g, IView map, IProvider DataSource, LabelLayer labelLayer)
        {
            if (labelLayer.Style.Enabled && labelLayer.MaxVisible >= map.Resolution && labelLayer.MinVisible < map.Resolution)
            {
                if (DataSource == null)
                    throw (new ApplicationException("DataSource property not set"));
                g.SmoothingMode = SmoothingMode.AntiAlias;

                //!!!BoundingBox envelope = map.Envelope; //View to render
                //!!!if (CoordinateTransformation != null)
                //!!!    envelope = GeometryTransform.TransformBox(envelope, CoordinateTransformation.MathTransform.Inverse());

                IFeatures features = new Features();
                DataSource.Open();
                features = DataSource.GetFeaturesInView(map);
                DataSource.Close();
                
                //Initialize label collection
                List<Label> labels = new List<Label>();

                //List<System.Drawing.Rectangle> LabelBoxes; //Used for collision detection
                //Render labels
                foreach (IFeature feature in features)
                {
                    //!!!if (CoordinateTransformation != null)
                    //!!!    features[i].Geometry = GeometryTransform.TransformGeometry(
                    //!!!   features[i].Geometry, CoordinateTransformation. MathTransform);

                    LabelStyle style = null;
                    if (labelLayer.Theme != null) //If thematics is enabled, lets override the style
                        style = labelLayer.Theme.GetStyle(feature) as LabelStyle;
                    else
                        style = (LabelStyle)labelLayer.Style;

                    float rotation = 0;
                    if (!String.IsNullOrEmpty(labelLayer.RotationColumn))
                        float.TryParse(feature[labelLayer.RotationColumn].ToString(), NumberStyles.Any, CultureInfo.InvariantCulture,
                                       out rotation);

                    int priority = labelLayer.Priority;
                    if (labelLayer.PriorityDelegate != null)
                        priority = labelLayer.PriorityDelegate(feature);
                    else if (!String.IsNullOrEmpty(labelLayer.PriorityColumn))
                        int.TryParse(feature[labelLayer.PriorityColumn].ToString(), NumberStyles.Any, CultureInfo.InvariantCulture,
                                     out priority);

                    string text;
                    if (labelLayer.LabelStringDelegate != null)
                        text = labelLayer.LabelStringDelegate(feature);
                    else
                        text = feature[labelLayer.LabelColumn].ToString();

                    if (text != null && text != String.Empty)
                    {
                        if (feature.Geometry is GeometryCollection)
                        {
                            if (labelLayer.MultipartGeometryBehaviour == SharpMap.Layers.LabelLayer.MultipartGeometryBehaviourEnum.All)
                            {
                                foreach (Geometry geom in (feature.Geometry as GeometryCollection))
                                {
                                    Label lbl = CreateLabel(geom, text, rotation, priority, style, map, g, labelLayer);
                                    if (lbl != null)
                                        labels.Add(lbl);
                                }
                            }
                            else if (labelLayer.MultipartGeometryBehaviour == SharpMap.Layers.LabelLayer.MultipartGeometryBehaviourEnum.CommonCenter)
                            {
                                Label lbl = CreateLabel(feature.Geometry, text, (float)rotation, priority, style, map, g, labelLayer);
                                if (lbl != null)
                                    labels.Add(lbl);
                            }
                            else if (labelLayer.MultipartGeometryBehaviour == SharpMap.Layers.LabelLayer.MultipartGeometryBehaviourEnum.First)
                            {
                                //!!!
                                //if ((feature.Geometry as GeometryCollection).Collection.Count > 0)
                                //{
                                //    Label lbl = CreateLabel((feature.Geometry as GeometryCollection).Collection[0], text,
                                //                            rotation, style, map, g, labelTheme);
                                //    if (lbl != null)
                                //        labels.Add(lbl);
                                //}
                            }
                            else if (labelLayer.MultipartGeometryBehaviour == SharpMap.Layers.LabelLayer.MultipartGeometryBehaviourEnum.Largest)
                            {
                                GeometryCollection coll = (feature.Geometry as GeometryCollection);
                                if (coll.NumGeometries > 0)
                                {
                                    double largestVal = 0;
                                    int idxOfLargest = 0;
                                    for (int j = 0; j < coll.NumGeometries; j++)
                                    {
                                        Geometry geom = coll.Geometry(j);
                                        if (geom is LineString && ((LineString)geom).Length > largestVal)
                                        {
                                            largestVal = ((LineString)geom).Length;
                                            idxOfLargest = j;
                                        }
                                        if (geom is MultiLineString && ((MultiLineString)geom).Length > largestVal)
                                        {
                                            largestVal = ((LineString)geom).Length;
                                            idxOfLargest = j;
                                        }
                                        if (geom is Polygon && ((Polygon)geom).Area > largestVal)
                                        {
                                            largestVal = ((Polygon)geom).Area;
                                            idxOfLargest = j;
                                        }
                                        if (geom is MultiPolygon && ((MultiPolygon)geom).Area > largestVal)
                                        {
                                            largestVal = ((MultiPolygon)geom).Area;
                                            idxOfLargest = j;
                                        }
                                    }

                                    Label lbl = CreateLabel(coll.Geometry(idxOfLargest), text, rotation, priority, style,
                                                            map, g, labelLayer);
                                    if (lbl != null)
                                        labels.Add(lbl);
                                }
                            }
                        }
                        else
                        {
                            Label lbl = CreateLabel(feature.Geometry, text, rotation, priority, style, map, g, labelLayer);
                            if (lbl != null)
                                labels.Add(lbl);
                        }
                    }
                }
                if (labels.Count > 0) //We have labels to render...
                {
                    if ((labelLayer.Style as LabelStyle).CollisionDetection && labelLayer.LabelFilter != null)
                        labelLayer.LabelFilter(labels);
                    for (int i = 0; i < labels.Count; i++)
                        if (labels[i].Show)
                            RendererHelper.DrawLabel(g, labels[i].LabelPoint, labels[i].Style.Offset,
                                                     labels[i].Style.Font, labels[i].Style.ForeColor,
                                                     labels[i].Style.BackColor, (labelLayer.Style as LabelStyle).Halo, labels[i].Rotation,
                                                     labels[i].Text, map);
                }
                labels = null;
            }
        }

        private static Label CreateLabel(IGeometry feature, string text, float rotation, LabelStyle style, IView map, Graphics g, LabelLayer labelTheme)
        {
            return CreateLabel(feature, text, rotation, labelTheme.Priority, style, map, g, labelTheme);
        }

        private static Label CreateLabel(IGeometry feature, string text, float rotation, int priority, LabelStyle style, IView map,
                                  Graphics g, LabelLayer labelTheme)
        {
            System.Drawing.SizeF gdiSize = g.MeasureString(text, style.Font.Convert());
            SharpMap.Styles.Size size = new SharpMap.Styles.Size() { Width = gdiSize.Width, Height = gdiSize.Height };

            SharpMap.Geometries.Point position = map.WorldToView(feature.GetBoundingBox().GetCentroid());
            position.X = position.X - size.Width * (short)style.HorizontalAlignment * 0.5f;
            position.Y = position.Y - size.Height * (short)style.VerticalAlignment * 0.5f;
            if (position.X - size.Width > map.Width || position.X + size.Width < 0 ||
                position.Y - size.Height > map.Height || position.Y + size.Height < 0)
                return null;
            else
            {
                Label lbl;

                if (!style.CollisionDetection)
                    lbl = new Label(text, position, rotation, priority, null, style);
                else
                {
                    //Collision detection is enabled so we need to measure the size of the string
                    lbl = new Label(text, position, rotation, priority,
                                    new LabelBox(position.X - size.Width * 0.5f - style.CollisionBuffer.Width,
                                                 position.Y + size.Height * 0.5f + style.CollisionBuffer.Height,
                                                 size.Width + 2f * style.CollisionBuffer.Width,
                                                 size.Height + style.CollisionBuffer.Height * 2f), style);
                }
                if (feature.GetType() == typeof(LineString))
                {
                    LineString line = feature as LineString;
                    if (line.Length / map.Resolution > size.Width) //Only label feature if it is long enough
                        CalculateLabelOnLinestring(line, ref lbl, map);
                    else
                        return null;
                }

                return lbl;
            }
        }

        private static void CalculateLabelOnLinestring(LineString line, ref Label label, IViewTransform viewTransform)
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
            label.LabelPoint = viewTransform.WorldToView(new SharpMap.Geometries.Point(tmpx, tmpy));
        }


    }
}
