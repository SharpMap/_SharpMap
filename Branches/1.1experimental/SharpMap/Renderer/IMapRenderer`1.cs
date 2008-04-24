using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace SharpMap.Renderer
{
    public interface IMapRenderer
    {
        event EventHandler RenderDone;
        event EventHandler<LayerRenderedEventArgs> LayerRendered;
        Stream Render(Map map, out string mimeType);
    }

    public interface IMapRenderer<TOutputFormat> : IMapRenderer
    {
        new TOutputFormat Render(Map map);
    }
}
