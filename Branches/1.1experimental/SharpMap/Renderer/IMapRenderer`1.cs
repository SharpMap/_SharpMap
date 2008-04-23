using System;
using System.Collections.Generic;
using System.Text;

namespace SharpMap.Renderer
{
    public interface IMapRenderer
    {
        event EventHandler RenderDone;
        event EventHandler<LayerRenderedEventArgs> LayerRendered;
        object Render(Map map);
    }

    public interface IMapRenderer<TOutputFormat> : IMapRenderer
    {
        new TOutputFormat Render(Map map);
    }
}
