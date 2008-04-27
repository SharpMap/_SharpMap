/*
 *  The attached / following is part of SharpMap.Presentation.AspNet
 *  SharpMap.Presentation.AspNet is free software © 2008 Newgrove Consultants Limited, 
 *  www.newgrove.com; you can redistribute it and/or modify it under the terms 
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
using System.Web;

namespace SharpMap.Presentation.AspNet.Impl
{
    public abstract class AsyncMapHandlerBase
        : MapHandlerBase, IHttpAsyncHandler
    {
        #region IHttpAsyncHandler Members

        public IAsyncResult BeginProcessRequest(HttpContext context, AsyncCallback cb, object extraData)
        {
            Debug.WriteLine(string.Format("Request recieved on thread {0}", Thread.CurrentThread.ManagedThreadId));

            SetCacheability(context.Response);
            AsyncWorkItem workItem = new AsyncWorkItem(this.WebMap, context, cb, extraData);
            workItem.DoWork();
            return workItem;
        }

        public void EndProcessRequest(IAsyncResult result)
        {

        }

        #endregion

        public override void ProcessRequest(HttpContext context)
        {
            throw new InvalidOperationException();
        }


        internal class AsyncWorkItem
            : IAsyncResult
        {
            private IWebMap _webMap;

            private bool _completed;
            private Object _state;
            private AsyncCallback _callback;
            private HttpContext _context;

            bool IAsyncResult.IsCompleted { get { return _completed; } }
            WaitHandle IAsyncResult.AsyncWaitHandle { get { return null; } }
            Object IAsyncResult.AsyncState { get { return _state; } }
            bool IAsyncResult.CompletedSynchronously { get { return false; } }

            public AsyncWorkItem(IWebMap webMap, HttpContext context, AsyncCallback callback, object state)
            {
                this._context = context;
                this._callback = callback;
                this._webMap = webMap;
                this._state = state;

            }

            public void DoWork()
            {
                ThreadingUtility.QueueWorkItem(new Action<object>(DoRealWork), this);
            }

            private void DoRealWork(object state)
            {

                AsyncWorkItem wi = (AsyncWorkItem)state;
                Debug.WriteLine(string.Format("Proccessing carried out on thread {0}", Thread.CurrentThread.ManagedThreadId));
                wi._webMap.Context = wi._context;

                wi._context.Response.Clear();
                string mime;

                using (Stream s = wi._webMap.Render(out mime))
                {
                    wi._context.Response.ContentType = mime;
                    s.Position = 0;
                    if (s.Length > 0)
                    {
                        using (BinaryReader br = new BinaryReader(s))
                        {
                            using (Stream outStream = wi._context.Response.OutputStream)
                            {
                                outStream.Write(br.ReadBytes((int)s.Length), 0, (int)s.Length);
                            }
                        }
                    }
                }
                wi._completed = true;
                wi._callback(this);
            }
        }

    }
}
