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
using System.Collections.ObjectModel;
using SharpMap.CoordinateSystems.Transformations;
using SharpMap.Data;
using SharpMap.Geometries;
using SharpMap.Layers;
using SharpMap.Styles;

namespace SharpMap.Renderer.DefaultImage
{
    internal class DefaultVectorRenderer
        : DefaultImageRenderer.ILayerRenderer<IVectorLayer>
    {
        #region RenderingHelper<VectorLayer> Members

        public void RenderLayer(ILayer layer, Map map, System.Drawing.Graphics g)
        {
            RenderLayer((IVectorLayer)layer, map, g);
        }

        public void RenderLayer(IVectorLayer layer, Map map, System.Drawing.Graphics g)
        {
            if (map.Center == null)
                throw (new ApplicationException("Cannot render map. View center not specified"));

            g.SmoothingMode = layer.SmoothingMode;
            SharpMap.Geometries.BoundingBox envelope = map.Envelope; //View to render
            if (layer.CoordinateTransformation != null)
                envelope = GeometryTransform.TransformBox(
                    envelope,
                    layer.CoordinateTransformation.MathTransform.Inverse());

            //List<SharpMap.Geometries.Geometry> features = this.DataSource.GetGeometriesInView(map.Envelope);

            if (layer.DataSource == null)
                throw (new ApplicationException("DataSource property not set on layer '" + layer.LayerName + "'"));

            //If thematics is enabled, we use a slighty different rendering approach
            if (layer.Theme != null)
            {
                SharpMap.Data.FeatureDataSet ds = new SharpMap.Data.FeatureDataSet();
                layer.DataSource.Open();
                layer.DataSource.ExecuteIntersectionQuery(envelope, ds);
                layer.DataSource.Close();

                FeatureDataTable features = (FeatureDataTable)ds.Tables[0];

                if (layer.CoordinateTransformation != null)
                    for (int i = 0; i < features.Count; i++)
                        features[i].Geometry = GeometryTransform.TransformGeometry(features[i].Geometry, layer.CoordinateTransformation.MathTransform);

                //Linestring outlines is drawn by drawing the layer once with a thicker line
                //before drawing the "inline" on top.
                if (layer.Style.EnableOutline)
                {
                    //foreach (SharpMap.Geometries.Geometry feature in features)
                    for (int i = 0; i < features.Count; i++)
                    {
                        SharpMap.Data.FeatureDataRow feature = features[i];
                        //Draw background of all line-outlines first
                        if (feature.Geometry is SharpMap.Geometries.LineString)
                        {
                            IVectorStyle outlinestyle1 = layer.Theme.GetStyle(feature);
                            if (outlinestyle1.Enabled && outlinestyle1.EnableOutline)
                                SharpMap.Rendering.VectorRenderer.DrawLineString(g, feature.Geometry as LineString, outlinestyle1.Outline, map);
                        }
                        else if (feature.Geometry is SharpMap.Geometries.MultiLineString)
                        {
                            IVectorStyle outlinestyle2 = layer.Theme.GetStyle(feature);
                            if (outlinestyle2.Enabled && outlinestyle2.EnableOutline)
                                SharpMap.Rendering.VectorRenderer.DrawMultiLineString(g, feature.Geometry as MultiLineString, outlinestyle2.Outline, map);
                        }
                    }
                }

                for (int i = 0; i < features.Count; i++)
                {
                    SharpMap.Data.FeatureDataRow feature = features[i];
                    IVectorStyle style = layer.Theme.GetStyle(feature);
                    RenderGeometry(g, map, layer.ClippingEnabled, feature.Geometry, style);
                }
            }
            else
            {
                layer.DataSource.Open();

                Collection<Geometry> geoms = layer.DataSource.GetGeometriesInView(envelope);
                layer.DataSource.Close();

                if (layer.CoordinateTransformation != null)
                    for (int i = 0; i < geoms.Count; i++)
                        geoms[i] = GeometryTransform.TransformGeometry(geoms[i], layer.CoordinateTransformation.MathTransform);

                //Linestring outlines is drawn by drawing the layer once with a thicker line
                //before drawing the "inline" on top.
                if (layer.Style.EnableOutline)
                {
                    foreach (SharpMap.Geometries.Geometry geom in geoms)
                    {
                        if (geom != null)
                        {
                            //Draw background of all line-outlines first
                            switch (geom.GetType().FullName)
                            {
                                case "SharpMap.Geometries.LineString":
                                    SharpMap.Rendering.VectorRenderer.DrawLineString(g, geom as LineString, layer.Style.Outline, map);
                                    break;
                                case "SharpMap.Geometries.MultiLineString":
                                    SharpMap.Rendering.VectorRenderer.DrawMultiLineString(g, geom as MultiLineString, layer.Style.Outline, map);
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }

                for (int i = 0; i < geoms.Count; i++)
                {
                    if (geoms[i] != null)
                        RenderGeometry(g, map, layer.ClippingEnabled, geoms[i], layer.Style);
                }
            }


            //base.Render(g, map);
        }

        private void RenderGeometry(System.Drawing.Graphics g, Map map, bool clipLayer, Geometry feature, IVectorStyle style)
        {
            switch (feature.GetType().FullName)
            {
                case "SharpMap.Geometries.Polygon":
                    if (style.EnableOutline)
                        SharpMap.Rendering.VectorRenderer.DrawPolygon(g, (Polygon)feature, style.Fill, style.Outline, clipLayer, map);
                    else
                        SharpMap.Rendering.VectorRenderer.DrawPolygon(g, (Polygon)feature, style.Fill, null, clipLayer, map);
                    break;
                case "SharpMap.Geometries.MultiPolygon":
                    if (style.EnableOutline)
                        SharpMap.Rendering.VectorRenderer.DrawMultiPolygon(g, (MultiPolygon)feature, style.Fill, style.Outline, clipLayer, map);
                    else
                        SharpMap.Rendering.VectorRenderer.DrawMultiPolygon(g, (MultiPolygon)feature, style.Fill, null, clipLayer, map);
                    break;
                case "SharpMap.Geometries.LineString":
                    SharpMap.Rendering.VectorRenderer.DrawLineString(g, (LineString)feature, style.Line, map);
                    break;
                case "SharpMap.Geometries.MultiLineString":
                    SharpMap.Rendering.VectorRenderer.DrawMultiLineString(g, (MultiLineString)feature, style.Line, map);
                    break;
                case "SharpMap.Geometries.Point":
                    SharpMap.Rendering.VectorRenderer.DrawPoint(g, (Point)feature, style.Symbol, style.SymbolScale, style.SymbolOffset, style.SymbolRotation, map);
                    break;
                case "SharpMap.Geometries.MultiPoint":
                    SharpMap.Rendering.VectorRenderer.DrawMultiPoint(g, (MultiPoint)feature, style.Symbol, style.SymbolScale, style.SymbolOffset, style.SymbolRotation, map);
                    break;
                case "SharpMap.Geometries.GeometryCollection":
                    foreach (Geometries.Geometry geom in (GeometryCollection)feature)
                        RenderGeometry(g, map, clipLayer, geom, style);
                    break;
                default:
                    break;
            }
        }


        #endregion
    }
}
