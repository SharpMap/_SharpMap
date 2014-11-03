using System;
using GeoAPI.Geometries;
using SharpMap.Layers;
using SharpMap.Styles;

namespace SharpMap.Rendering
{
    [Serializable]
    public class NullRenderer : IRenderer
    {
        public void Draw(Map map, IGraphics g, Label label)
        {
            throw new NotImplementedException();
        }

        public void Draw(Map map, IGraphics g, IGeometry geom, IStyle style, bool clip)
        {
            throw new NotImplementedException();
        }

        public void DrawOutline(Map map, IGraphics g, IGeometry geom, IStyle style)
        {
            throw new NotImplementedException();
        }
    }
}