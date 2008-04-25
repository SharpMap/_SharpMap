using System;
using System.Collections.Generic;
using System.Text;
using SharpMap.Renderer;

namespace SharpMap.Presentation.AspNet
{
    public interface IMapRendererConfig
    {
        void ConfigureRenderer(IMapRequestConfig requestConfig, IMapRenderer renderer);

    }

    public interface IMapRendererConfig<TMapRequestConfig, TMapRenderer>
        : IMapRendererConfig
        where TMapRequestConfig : IMapRequestConfig
        where TMapRenderer : IMapRenderer
    {
        void ConfigureRenderer(TMapRequestConfig requestConfig, TMapRenderer renderer);
    }
}
