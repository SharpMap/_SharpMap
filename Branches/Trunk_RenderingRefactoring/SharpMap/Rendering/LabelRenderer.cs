using System;
using System.Drawing;
using GeoAPI.Geometries;
using SharpMap.Styles;

namespace SharpMap.Rendering
{
    public class LabelRenderer : VectorRenderer, IRenderer
    {
        public void Draw(Map map, Graphics g, Label label)
        {
            if (map == null)
                throw new ArgumentNullException("map");
            if (g == null)
                throw new ArgumentNullException("g");
            if (label == null)

                throw new ArgumentNullException("label");
            DrawLabel(g, label.Location, label.Style.Offset,
                label.Style.Font, label.Style.ForeColor,
                label.Style.BackColor, label.Style.Halo, label.Rotation,
                label.Text, map, label.Style.HorizontalAlignment,
                label.LabelPoint);
        }

        public void Draw(Map map, Graphics g, IGeometry geom, IStyle style, bool clip)
        {
            throw new NotImplementedException();
        }

        public void DrawOutline(Map map, Graphics g, IGeometry geom, IStyle style)
        {
            throw new System.NotImplementedException();
        }
    }
}