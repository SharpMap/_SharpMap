using System;
using System.Collections.Generic;
using System.Text;
using SharpMap.Renderer;
using System.IO;
using SharpMap.Layers;
using SharpMap.Data;
using SharpMap.Styles;
using SharpMap.Geometries;

namespace SharpMap.Renderers.GeoJson
{
    public class GeoJsonRenderer
        : IMapRenderer<string>
    {
        private readonly Dictionary<string, Func<ILayer, FeatureDataRow, string>> _attributeProviders
            = new Dictionary<string, Func<ILayer, FeatureDataRow, string>>();

        public Dictionary<string, Func<ILayer, FeatureDataRow, string>> AttributeProviders
        {
            get
            {
                return _attributeProviders;
            }
        }

        public string Render(Map map, out string mimeType)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{");

            foreach (ILayer l in map.Layers)
            {
                RenderLayer(sb, map, l);
                sb.Append(",");
            }
            sb.Remove(sb.Length - 2, 1);

            sb.Append("}");
            OnRenderDone();
            mimeType = "application/json";
            return sb.ToString();
        }

        private void RenderLayer(StringBuilder sb, Map map, ILayer l)
        {
            if (!l.Enabled)
                return;


            if (l is VectorLayer)
            {
                RenderVectorLayer(sb, map, (VectorLayer)l);
                return;
            }
            throw new NotImplementedException(string.Format("No Json renderer set for layers of type {0}", l.GetType()));
        }

        private void RenderVectorLayer(StringBuilder sb, Map map, VectorLayer vectorLayer)
        {
            if (map.Zoom > vectorLayer.MaxVisible)
                return;
            if (map.Zoom < vectorLayer.MinVisible)
                return;


            sb.Append("{");
            sb.Append(FormatJsonAttribute("type", "FeatureCollection"));
            sb.Append(",");
            sb.Append(FormatJsonAttribute("name", vectorLayer.LayerName));
            sb.Append(",");
            sb.Append("\"features\":");
            sb.Append("[");
            FeatureDataSet fds = new FeatureDataSet();
            bool leaveOpen = true;
            if (!vectorLayer.DataSource.IsOpen)
            {
                vectorLayer.DataSource.Open();
                leaveOpen = false;
            }
            vectorLayer.DataSource.ExecuteIntersectionQuery(map.Envelope, fds);
            if (!leaveOpen)
                vectorLayer.DataSource.Close();

            FeatureDataTable fdt = fds.Tables[0];
            bool itemRendered = false;
            if (vectorLayer.Theme == null)
            {
                if (!vectorLayer.Style.Enabled)
                    return;
                if (map.Zoom > vectorLayer.Style.MaxVisible)
                    return;
                if (map.Zoom < vectorLayer.Style.MinVisible)
                    return;

                foreach (FeatureDataRow fdr in fdt)
                {
                    RenderVectorFeature(sb, vectorLayer, fdr);
                    sb.Append(",");
                    itemRendered = true;
                }
            }

            else
            {

                foreach (FeatureDataRow fdr in fdt)
                {
                    IStyle style = vectorLayer.Theme.GetStyle(fdr);
                    if (!style.Enabled)
                        continue;
                    if (map.Zoom > style.MaxVisible)
                        continue;
                    if (map.Zoom < style.MinVisible)
                        continue;

                    RenderVectorFeature(sb, vectorLayer, fdr);
                    sb.Append(",");
                    itemRendered = true;
                }
            }

            if (itemRendered)
                sb.Remove(sb.Length - 2, 1);
            sb.Append("]");
            sb.Append("}");

            OnLayerRendered(vectorLayer);

        }

        private void RenderVectorFeature(StringBuilder sb, ILayer layer, FeatureDataRow fdr)
        {
            sb.Append("{");
            Geometry g = fdr.Geometry;
            sb.Append(FormatJsonAttribute("type", "Feature"));
            sb.Append(",");
            sb.Append("\"properties\":");
            RenderFeatureProperties(sb, layer, fdr);
            sb.Append(",");
            sb.Append("\"geometry\":");
            RenderGeometry(sb, g);
            sb.Append("}");
        }

        private void RenderGeometry(StringBuilder sb, Geometry g)
        {
            sb.Append("{");
            sb.Append(FormatJsonAttribute("type", g.GetType().Name));
            sb.Append(",");
            sb.Append("\"coordinates\":");

            if (g is Point)
                RenderPointCoordinates(sb, (Point)g);
            else if (g is LineString)
                RenderLineStringCoordinates(sb, (LineString)g);
            else if (g is Polygon)
                RenderPolygonCoordinates(sb, (Polygon)g);
            else if (g is MultiPoint)
                RenderMultiPointCoordinates(sb, (MultiPoint)g);
            else if (g is MultiLineString)
                RenderMultiLineStringCoordinates(sb, (MultiLineString)g);
            else if (g is MultiPolygon)
                RenderMultiPolygonCoordinates(sb, (MultiPolygon)g);
            else if (g is GeometryCollection)
            {
                foreach (Geometry g2 in ((GeometryCollection)g).Collection)
                {
                    RenderGeometry(sb, g2);
                    sb.Append(",");
                }
                sb.Remove(sb.Length - 2, 1);
            }

            sb.Append("}");
        }

        private void RenderMultiPolygonCoordinates(StringBuilder sb, MultiPolygon multiPolygon)
        {
            sb.Append("[");
            foreach (Polygon p in multiPolygon)
            {
                RenderPolygonCoordinates(sb, p);
                sb.Append(",");
            }
            sb.Remove(sb.Length - 2, 1);
            sb.Append("]");
        }

        private void RenderMultiLineStringCoordinates(StringBuilder sb, MultiLineString multiLineString)
        {
            sb.Append("[");
            foreach (LineString ls in multiLineString)
            {
                RenderLineStringCoordinates(sb, ls);
                sb.Append(",");
            }
            sb.Remove(sb.Length - 2, 1);
            sb.Append("]");
        }

        private void RenderMultiPointCoordinates(StringBuilder sb, MultiPoint multiPoint)
        {
            sb.Append("[");
            foreach (Point p in multiPoint.Points)
            {
                RenderPointCoordinates(sb, p);
                sb.Append(",");
            }
            sb.Remove(sb.Length - 2, 1);
            sb.Append("]");
        }

        private void RenderPolygonCoordinates(StringBuilder sb, Polygon polygon)
        {
            sb.Append("[");
            RenderLinearRing(sb, polygon.ExteriorRing);

            foreach (LinearRing lr in polygon.InteriorRings)
            {
                sb.Append(",");
                RenderLinearRing(sb, lr);
            }
            sb.Append("]");
        }

        private void RenderLinearRing(StringBuilder sb, LinearRing linearRing)
        {
            sb.Append("[");
            foreach (Point p in linearRing.Vertices)
            {
                RenderPointCoordinates(sb, p);
                sb.Append(",");
            }
            sb.Remove(sb.Length - 2, 1);

            sb.Append("]");
        }

        private void RenderLineStringCoordinates(StringBuilder sb, LineString lineString)
        {
            sb.Append("[");
            foreach (Point p in lineString.Vertices)
            {
                RenderPointCoordinates(sb, p);
                sb.Append(",");
            }
            sb.Remove(sb.Length - 2, 1);
            sb.Append("]");
        }

        //TODO: add string format options.
        private void RenderPointCoordinates(StringBuilder sb, Point g)
        {
            sb.AppendFormat("[{0},{1}]", g.X, g.Y);
        }

        private void RenderFeatureProperties(StringBuilder sb, ILayer layer, FeatureDataRow fdr)
        {
            sb.Append("{");
            if (this.AttributeProviders.Count > 0)
            {
                foreach (KeyValuePair<string, Func<ILayer, FeatureDataRow, string>> v in this.AttributeProviders)
                {
                    sb.Append(FormatJsonAttribute(v.Key, v.Value(layer, fdr)));
                    sb.Append(",");
                }
                sb.Remove(sb.Length - 1, 1);
            }

            sb.Append("}");
        }


        private string FormatJsonAttribute(string name, string value)
        {
            return string.Format("\"{0}\":\"{1}\"", name, value);
        }

        private void OnLayerRendered(ILayer l)
        {
            if (this.LayerRendered != null)
                this.LayerRendered(this, new LayerRenderedEventArgs(l));
        }

        private void OnRenderDone()
        {
            if (this.RenderDone != null)
                this.RenderDone(this, EventArgs.Empty);
        }

        public event EventHandler RenderDone;

        public event EventHandler<LayerRenderedEventArgs> LayerRendered;

        Stream IMapRenderer.Render(Map map, out string mimeType)
        {
            MemoryStream ms = new MemoryStream();
            StreamWriter sw = new StreamWriter(ms);
            sw.Write(Render(map, out mimeType));
            sw.Flush();
            ms.Position = 0;
            return ms;
        }
    }
}
