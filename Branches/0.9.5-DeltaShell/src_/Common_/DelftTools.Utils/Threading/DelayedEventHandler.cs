//#define LOGGING_ENABLED

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using DelftTools.Utils.Collections;
using log4net;

namespace DelftTools.Utils.Threading
{
    /// <summary>
    /// Fires actions in a delayed way, in a separate thread. Action must contain logic which is thread-safe.
    /// </summary>
    /// <typeparam name="TEventArgs"></typeparam>
    public class DelayedEventHandler<TEventArgs> where TEventArgs : EventArgs
    {
        private static readonly ILog log = LogManager.GetLogger("DelayedEventHandler<" + typeof(TEventArgs).Name + ">");

        private readonly EventHandler<TEventArgs> handler;

        private readonly Queue<EventInfo> events = new Queue<EventInfo>();
        
        private readonly Timer timer;
        private bool running;

        private struct EventInfo
        {
            public object Sender;
            public TEventArgs Arguments;
        }

        public DelayedEventHandler(EventHandler<TEventArgs> handler)
        {
            this.handler = handler;

            FireLastEventOnly = true;
            Delay = 1;
            Delay2 = 1;

            timer = new Timer(timer_Tick);

            Enabled = true;
        }

        public bool Enabled
        {
            get { return enabled; }
            set
            {
                enabled = value;

                if (running)
                {
                    return;
                }

                // start processing events
                if(events.Count != 0)
                {
                    var interval = (DateTime.Now - timeEndProcessingEvents).TotalMilliseconds < Delay2 ? Delay2 : Delay;
                    timer.Change(interval, Timeout.Infinite);
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

        public Func<object, TEventArgs, bool> Filter { get; set; }

        public bool IsRunning { get { return running; } }

        public bool HasEventsToProcess { get { return events.Count > 0; } }

        public int Delay2 { get; set; }

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

        private void FireCollectionChangedHandler(object sender, NotifyCollectionChangedEventArgs e)
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
            if (running || !Enabled)
            {
                return;
            }

            running = true;

            var interval = (DateTime.Now - timeEndProcessingEvents).TotalMilliseconds < Delay2 ? Delay2 : Delay;
            timer.Change(interval, Timeout.Infinite);
        }

        private void timer_Tick(object timerSender)
        {
            try
            {
                if (FireLastEventOnly)
                {
                    // remove all event args and use the only fire timer one more time with the latest one
                    if (events.Count > 0)
                    {
#if LOGGING_ENABLED
                        log.DebugFormat("Skipping {0} events and firing last event", events.Count);
                        var stopwatch = new Stopwatch();
                        stopwatch.Start();
#endif
                        TEventArgs lastArg;

                        EventInfo eventInfo;
                        lock(events)
                        {
                            eventInfo = events.Last();
                            events.Clear();
                        }

                        OnHandler(eventInfo.Sender, eventInfo.Arguments);
                        
                        

#if LOGGING_ENABLED
                        stopwatch.Stop();
                        log.DebugFormat("Firing event completed in {0} ms", stopwatch.ElapsedMilliseconds);
#endif
                    }
                }
                else
                {
                    // check if FullRefresh is configured
                    if (FullRefreshEventsCount != 0 && events.Count > FullRefreshEventsCount)
                    {
#if LOGGING_ENABLED
                        log.DebugFormat("Number of events exceeded {0}, clearing all events and calling full refresh", FullRefreshEventsCount);
                        var stopwatch = new Stopwatch();
                        stopwatch.Start();

                        var eventsCount = events.Count;
#endif

                        events.Clear();
                        FullRefreshEventHandler(this, null);

#if LOGGING_ENABLED
                        stopwatch.Stop();
                        log.DebugFormat("Clearing {0} events and full refresh completed in {1} ms", eventsCount, stopwatch.ElapsedMilliseconds);
#endif
                    }
                    else
                    {
#if LOGGING_ENABLED
                        log.DebugFormat("Number of events in the queue is {0}, processing ... ", events.Count);
#endif

                        if (events.Count > 0)
                        {
                            var eventInfo = events.Dequeue();
                            OnHandler(eventInfo.Sender, eventInfo.Arguments);

                            timer.Change(Delay2, Timeout.Infinite);
                        }
                    }
                }
            }
            finally
            {
                if(events.Count != 0) // reschedule
                {
                    running = true;
                    timer.Change(Delay2, Timeout.Infinite);
                }
                else
                {
                    timeEndProcessingEvents = DateTime.Now;
#if LOGGING_ENABLED
                    log.DebugFormat("Finished processing {0} events, target: {1} - {2}", 
                        totalProcessedEvents, handler.Target.GetType().Name, handler.Method.Name);
#endif
                }
                running = false;
            }
        }

        protected virtual void OnHandler(object sender, TEventArgs e)
        {
            handler(sender, e);
        }

        private int totalProcessedEvents;
        private DateTime timeEndProcessingEvents;
        private bool enabled;
    }
}