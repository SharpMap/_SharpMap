using System;
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
        TOutputFormat Render(Map map);
    }
}
