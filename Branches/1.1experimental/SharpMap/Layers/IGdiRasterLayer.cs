using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using SharpMap.Geometries;

namespace SharpMap.Layers
{
    public interface IGdiRasterLayer
        : IRasterLayer
    {
        void DrawToGraphics(Map m, BoundingBox e, Graphics g);

    }
}
