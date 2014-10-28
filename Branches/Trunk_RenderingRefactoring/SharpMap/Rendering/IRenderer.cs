using GeoAPI.Geometries;
using SharpMap.Layers;
using SharpMap.Styles;

namespace SharpMap.Rendering
{
    public interface IRenderer
    {
        void Draw(Map map, IGraphics g, Label label);
        void Draw(Map map, IGraphics g, IGeometry geom, IStyle style, bool clip);
        void DrawOutline(Map map, IGraphics g, IGeometry geom, IStyle style);
    }
}   