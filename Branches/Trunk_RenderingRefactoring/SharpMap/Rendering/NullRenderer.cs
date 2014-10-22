using System.Drawing;
using GeoAPI.Geometries;
using SharpMap.Styles;

namespace SharpMap.Rendering
{
    public class NullRenderer : IRenderer
    {
        public void Draw(Map map, Graphics g, Label label)
        {
            throw new System.NotImplementedException();
        }

        public void Draw(Map map, Graphics g, IGeometry geom, IStyle style, bool clip)
        {
            throw new System.NotImplementedException();
        }

        public void DrawOutline(Map map, Graphics g, IGeometry geom, IStyle style)
        {
            throw new System.NotImplementedException();
        }
    }
}