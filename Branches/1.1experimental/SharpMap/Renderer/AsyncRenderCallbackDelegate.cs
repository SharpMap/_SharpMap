using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace SharpMap.Renderer
{
    public delegate void AsyncRenderCallbackDelegate(Stream result, string mimeType);
    public delegate void AsyncRenderCallbackDelegate<TOutput>(TOutput result, string mimeType);

}
