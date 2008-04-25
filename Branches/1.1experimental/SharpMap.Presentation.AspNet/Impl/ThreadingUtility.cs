using System;
using System.Threading;

namespace SharpMap.Presentation.AspNet.Impl
{
    internal static class ThreadingUtility
    {
#warning arrange a better threadpool for AsyncHttpMapHandler
        /// <remarks>
        /// by default ThreadPool.QueueUserWorkItem is using the same threads as the IIS worker thread pool 
        /// -  hence multithreading uses up the same thread pool faster.
        /// 
        /// TODO: use a different mechanism for thread pooling.
        /// </remarks>
        public static void QueueWorkItem(Action<Object> action, object value)
        {
            ///uncomment the following 3 lines and comment the ThreadPool.QueueUserWorkItem(new WaitCallback(action), value); line to 
            ///see that the request is being processed on a different thread. 
            ///This will *not* scale well so use it for *investigative* purposes only


            //ParameterizedThreadStart ts = new ParameterizedThreadStart(action);
            //Thread t = new Thread(ts);
            //t.Start(value);

            ThreadPool.QueueUserWorkItem(new WaitCallback(action), value);
        }
    }
}
