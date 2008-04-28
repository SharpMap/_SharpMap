using System;
using System.Collections.Generic;
using System.Text;
using SharpMap.Layers;
using System.Drawing;

namespace SharpMap.Renderer
{
    public class DefaultGdiRasterRenderer
        : DefaultImageRenderer.ILayerRenderer<IGdiRasterLayer>
    {

        #region ILayerRenderer<IGdiRasterLayer> Members

        public void RenderLayer(IGdiRasterLayer layer, Map map, Graphics g)
        {
            layer.DrawToGraphics(map, map.Envelope, g);
        }

        #endregion

        #region ILayerRenderer Members

        public void RenderLayer(ILayer layer, Map map, Graphics g)
        {
            RenderLayer((IGdiRasterLayer)layer, map, g);
        }

        #endregion
    }
}
