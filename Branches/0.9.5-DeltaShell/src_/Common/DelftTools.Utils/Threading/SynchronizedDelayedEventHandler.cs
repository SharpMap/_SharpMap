using System;
using System.ComponentModel;
using DelftTools.Utils.Collections;
using log4net;

namespace DelftTools.Utils.Threading
{
    public class SynchronizedDelayedEventHandler<TEventArgs> : DelayedEventHandler<TEventArgs> where TEventArgs : EventArgs
    {
        private static readonly ILog log = LogManager.GetLogger("SynchronizedDelayedEventHandler<" + typeof(TEventArgs).Name + ">");

        public SynchronizedDelayedEventHandler(EventHandler<TEventArgs> handler)
            : base(handler)
        {
        }

        public ISynchronizeInvoke SynchronizeInvoke { get; set; }

        private delegate void Handler(object sender, TEventArgs e);

        protected override void OnHandler(object sender, TEventArgs e)
        {
            if (SynchronizeInvoke != null && SynchronizeInvoke.InvokeRequired)
            {
                try
                {
                    SynchronizeInvoke.Invoke(new Handler(base.OnHandler), new[] { sender, e });
                }
                catch (InvalidOperationException exception)
                {
                    if (exception.Message.Contains("Invoke"))
                    {
                        // somtimes worker thread continues to run when control already has handle destroyed, then we end up here
                        log.DebugFormat("Error during invoke call: {0}", exception.Message);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            else
            {
                base.OnHandler(sender, e);
            }
        }
    }
}