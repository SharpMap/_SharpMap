//#define LOGGING_ENABLED

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Timers;
using DelftTools.Utils.Collections;
using Timer = System.Timers.Timer;

#if LOGGING_ENABLED
using System.Diagnostics;
using log4net;
#endif

namespace DelftTools.Utils.Threading
{
    /// <summary>
    /// Fires actions in a delayed way, in a separate thread. Action must contain logic which is thread-safe.
    /// 
    /// DelayedEventHandler watches DelayedEventHandlerController.FireEvents to see if events can be fired.
    /// </summary>
    /// <typeparam name="TEventArgs"></typeparam>
    public class DelayedEventHandler<TEventArgs> : IDisposable where TEventArgs : EventArgs
    {
#if LOGGING_ENABLED
        private static readonly ILog Log = LogManager.GetLogger("DelayedEventHandler<" + typeof(TEventArgs).Name + ">");
#endif
        private readonly EventHandler<TEventArgs> handler;

        private readonly Queue<EventInfo> events = new Queue<EventInfo>();
        
        private readonly Timer timer;

        private bool onlyFromAnotherThread;
        private int threadIdOnCreation;

        private struct EventInfo
        {
            public object Sender;
            public TEventArgs Arguments;
        }

        public DelayedEventHandler(EventHandler<TEventArgs> handler, bool onlyFromAnotherThread = false)
        {
            this.handler = handler;
            this.onlyFromAnotherThread = onlyFromAnotherThread;
            threadIdOnCreation = Thread.CurrentThread.ManagedThreadId;

            DelayedEventHandlerController.FireEventsChanged += DelayedEventHandlerController_FireEventsChanged;
            FireLastEventOnly = true;
            Delay = 1;
            FullRefreshDelay = 1;

            timer = new Timer { AutoReset = false };
            timer.Elapsed += timer_Elapsed;

            Enabled = true;
        }

        public ISynchronizeInvoke SynchronizingObject
        {
            get { return timer.SynchronizingObject; }
            set { timer.SynchronizingObject = value; }
        }

        void DelayedEventHandlerController_FireEventsChanged(object sender, EventArgs e)
        {
            Enabled = DelayedEventHandlerController.FireEvents;
        }

        public bool Enabled
        {
            get { return enabled; }
            set
            {
                enabled = value;

                if (!enabled) // stopped
                {
                    events.Clear();
                    timer.Stop();
                }

                if (IsRunning)
                {
                    return;
                }

                // start processing events
                if(events.Count != 0)
                {
                    // make sure that we don't call full refresh event handler too frequently if events are being processed already,
                    // this has to do with a duration of full event handler self.
                    var interval = (DateTime.Now - timeEndProcessingEvents).TotalMilliseconds < FullRefreshDelay ? FullRefreshDelay : Delay;

                    timer.Interval = interval;
                    timer.Start();
                }
            }
        }

        /// <summary>
        /// When is set to true - only last events will be processed.
        /// </summary>
        public bool FireLastEventOnly { get; set; }

        /// <summary>
        /// Number of milliseconds to wait before processing events.
        /// </summary>
        public int Delay { get; set; }

        /// <summary>
        /// When multiple events are stacked and number of events exceeds this amount - full refresh is called and stack is cleared.
        /// Works only if FullRefreshEventHandler is not null.
        /// </summary>
        public int FullRefreshEventsCount { get; set; }

        /// <summary>
        /// Event handler used to to full refresh.
        /// </summary>
        public EventHandler FullRefreshEventHandler { get; set; }

        /// <summary>
        /// Fire only events where Filter is evaluated to true.
        /// </summary>
        public Func<object, TEventArgs, bool> Filter { get; set; }

        public bool IsRunning { get; private set; }

        public bool HasEventsToProcess { get { return events.Count > 0; } }

        public int FullRefreshDelay { get; set; }

        public static implicit operator PropertyChangedEventHandler(DelayedEventHandler<TEventArgs> handler)
        {
            return handler.FirePropertyChangedEventHandler;
        }

        private void FirePropertyChangedEventHandler(object sender, PropertyChangedEventArgs e)
        {
            FireHandler(sender, (TEventArgs)(EventArgs)e);
        }

        public static implicit operator NotifyCollectionChangedEventHandler(DelayedEventHandler<TEventArgs> handler)
        {
            return handler.FireCollectionChangedHandler;
        }

        private void FireCollectionChangedHandler(object sender, NotifyCollectionChangingEventArgs e)
        {
            FireHandler(sender, (TEventArgs)(EventArgs)e);
        }

        public static implicit operator EventHandler<TEventArgs>(DelayedEventHandler<TEventArgs> handler)
        {
            return handler.FireHandler;
        }

        public void FireHandler(object sender, TEventArgs e)
        {
            if (Filter != null && !Filter(sender, e)) // filtered
            {
                return;
            }

            if (onlyFromAnotherThread)
            {
                if ((SynchronizingObject == null || !SynchronizingObject.InvokeRequired) && (threadIdOnCreation == 0 || threadIdOnCreation == Thread.CurrentThread.ManagedThreadId))
                {
                    OnHandler(sender, e);
                    return;
                }
            }

            lock (events)
            {
                if(events.Count == 0)
                {
                    totalProcessedEvents = 1;
                }
                else
                {
                    totalProcessedEvents++;
                }

                events.Enqueue(new EventInfo { Sender = sender, Arguments = e });
            }

            // schedule refresh
            if (IsRunning || !Enabled)
            {
                return;
            }

            IsRunning = true;

            var delay = Delay;

            if (FireLastEventOnly && FullRefreshEventHandler != null)
            {
                delay = FullRefreshDelay;
            }

            timer.Interval = delay;
            timer.Start();
        }

        private void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                if (FireLastEventOnly)
                {
                    // remove all event args and use the only fire timer one more time with the latest one
                    if (events.Count > 0)
                    {
#if LOGGING_ENABLED
                        Log.DebugFormat("Skipping {0} events and firing last event", events.Count);
                        var stopwatch = new Stopwatch();
                        stopwatch.Start();
#endif

                        EventInfo eventInfo;
                        lock(events)
                        {
                            eventInfo = events.Last();
                            events.Clear();
                        }

                        if (stopping)
                        {
                            IsRunning = false;
                            return;
                        }

                        OnHandler(eventInfo.Sender, eventInfo.Arguments);
                        
                        

#if LOGGING_ENABLED
                        stopwatch.Stop();
                        Log.DebugFormat("Firing event completed in {0} ms", stopwatch.ElapsedMilliseconds);
#endif
                    }
                }
                else
                {
                    // check if FullRefresh is configured
                    if (FullRefreshEventsCount != 0 && events.Count > FullRefreshEventsCount)
                    {
#if LOGGING_ENABLED
                        Log.DebugFormat("Number of events exceeded {0}, clearing all events and calling full refresh", FullRefreshEventsCount);
                        var stopwatch = new Stopwatch();
                        stopwatch.Start();

                        var eventsCount = events.Count;
#endif

                        events.Clear();

                        if (stopping)
                        {
                            IsRunning = false;
                            return;
                        }

                        FullRefreshEventHandler(this, null);

#if LOGGING_ENABLED
                        stopwatch.Stop();
                        Log.DebugFormat("Clearing {0} events and full refresh completed in {1} ms", eventsCount, stopwatch.ElapsedMilliseconds);
#endif
                    }
                    else
                    {
#if LOGGING_ENABLED
                        Log.DebugFormat("Number of events in the queue is {0}, processing ... ", events.Count);
#endif

                        if (events.Count > 0) // process event after event
                        {
                            var eventInfo = events.Dequeue();

                            if (stopping)
                            {
                                IsRunning = false;
                                return;
                            }

                            OnHandler(eventInfo.Sender, eventInfo.Arguments);
                        }
                    }
                }
            }
            finally
            {
                if(events.Count != 0) // reschedule
                {
                    IsRunning = true;

                    var delay = 1;

                    if (FireLastEventOnly && FullRefreshEventHandler != null)
                    {
                        delay = FullRefreshDelay;
                    }

                    if (stopping)
                    {
                        IsRunning = false;
                    }
                    else
                    {
                        timer.Interval = delay;
                        timer.Start();
                    }
                }
                else
                {
                    timeEndProcessingEvents = DateTime.Now;
#if LOGGING_ENABLED
                    Log.DebugFormat("Finished processing {0} events, target: {1} - {2}", 
                        totalProcessedEvents, handler.Target.GetType().Name, handler.Method.Name);
#endif
                }
                IsRunning = false;
            }
        }

        protected virtual void OnHandler(object sender, TEventArgs e)
        {
            if(!enabled)
            {
                return;
            }

            handler(sender, e);
        }

        private int totalProcessedEvents;
        private DateTime timeEndProcessingEvents;
        
        private bool enabled;

        private bool disposed;
        private bool stopping;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    DelayedEventHandlerController.FireEventsChanged -= DelayedEventHandlerController_FireEventsChanged;

                    timer.Stop();
                    timer.Close();
                }
            }
            disposed = true;
        }

        ~DelayedEventHandler()
        {
            Dispose(false);
        }
    }
}