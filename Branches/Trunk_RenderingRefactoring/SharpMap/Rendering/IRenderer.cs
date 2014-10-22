using System.Drawing;
using GeoAPI.Geometries;
using SharpMap.Styles;

namespace SharpMap.Rendering
{
    public interface IRenderer
    {
        void Draw(Map map, Graphics g, Label label);
        void Draw(Map map, Graphics g, IGeometry geom, IStyle style, bool clip);
        void DrawOutline(Map map, Graphics g, IGeometry geom, IStyle style);
    }
}   