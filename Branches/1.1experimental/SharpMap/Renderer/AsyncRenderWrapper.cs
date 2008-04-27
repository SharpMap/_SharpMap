/*
 *	This file is part of SharpMap
 *  SharpMap is free software. This file © 2008 Newgrove Consultants Limited, 
 *  http://www.newgrove.com; you can redistribute it and/or modify it under the terms 
 *  of the current GNU Lesser General Public License (LGPL) as published by and 
 *  available from the Free Software Foundation, Inc., 
 *  59 Temple Place, Suite 330, Boston, MA 02111-1307 USA: http://fsf.org/    
 *  This program is distributed without any warranty; 
 *  without even the implied warranty of merchantability or fitness for purpose.  
 *  See the GNU Lesser General Public License for the full details. 
 *  
 *  Author: John Diss 2008
 * 
 */

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;


namespace SharpMap.Renderer
{
    /// <summary>
    /// Utility class to wrap a non async IMapRenderer and use it asyncronously as if it were an IAsyncMapRenderer
    /// </summary>
    public class AsyncRenderWrapper
        : IAsyncMapRenderer
    {
        protected IMapRenderer _actualRenderer;
        public IMapRenderer ActualRenderer
        {
            get
            {
                return _actualRenderer;
            }
        }


        public AsyncRenderWrapper(IMapRenderer renderer)
        {
            _actualRenderer = renderer;
        }


        #region IAsyncMapRenderer Members

        public IAsyncResult Render(Map map, AsyncRenderCallbackDelegate callback)
        {
            Debug.WriteLine(string.Format("Calling Thread is {0}", Thread.CurrentThread.ManagedThreadId));
            InternalAsyncRenderDelegate dlgt = new InternalAsyncRenderDelegate(
                delegate(Map m, AsyncRenderCallbackDelegate call)
                {
                    string mime;
                    Stream s = ((IMapRenderer)this).Render(map, out mime);
                    callback(s, mime);

                });
            return dlgt.BeginInvoke(map, callback, null, null);
        }

        #endregion
    }

    /// <summary>
    /// Utility class to wrap a non async IMapRenderer[TRenderFormat] and use it asyncronously as if it were an IAsyncMapRenderer[TRenderFormat]
    /// </summary>
    public class AsyncRenderWrapper<TRenderFormat>
        : AsyncRenderWrapper, IAsyncMapRenderer<TRenderFormat>
    {
        public new IMapRenderer<TRenderFormat> ActualRenderer
        {
            get
            {
                return (IMapRenderer<TRenderFormat>)_actualRenderer;
            }
        }

        public AsyncRenderWrapper(IMapRenderer<TRenderFormat> renderer)
            : base(renderer)
        {
        }

        #region IAsyncMapRenderer<TRenderFormat> Members

        public IAsyncResult Render(Map map, AsyncRenderCallbackDelegate<TRenderFormat> callback)
        {
            Debug.WriteLine(string.Format("Calling Thread is {0}", Thread.CurrentThread.ManagedThreadId));
            InternalAsyncRenderDelegate<TRenderFormat> dlgt = new InternalAsyncRenderDelegate<TRenderFormat>(
                delegate(Map m, AsyncRenderCallbackDelegate<TRenderFormat> call)
                {
                    string mime;
                    TRenderFormat output = ActualRenderer.Render(map, out mime);
                    callback(output, mime);

                });
            return dlgt.BeginInvoke(map, callback, null, null);
        }

        #endregion
    }
}
