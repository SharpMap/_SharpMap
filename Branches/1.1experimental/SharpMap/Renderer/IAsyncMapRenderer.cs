using System;

namespace SharpMap.Renderer
{
    public interface IAsyncMapRenderer
    {
        IAsyncResult RenderAsync(Map map, AsyncRenderCallbackDelegate callback);
    }

    public interface IAsyncMapRenderer<TOutput> : IAsyncMapRenderer
    {
        IAsyncResult RenderAsync(Map map, AsyncRenderCallbackDelegate<TOutput> callback);
    }
}
