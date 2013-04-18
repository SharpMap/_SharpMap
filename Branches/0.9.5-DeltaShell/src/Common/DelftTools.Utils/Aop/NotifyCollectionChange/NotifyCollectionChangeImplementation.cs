using System;
using System.Collections;
using DelftTools.Utils.Collections;
using log4net;
using PostSharp.Laos;

namespace DelftTools.Utils.Aop.NotifyCollectionChange
{
    /// <summary>
    /// Implementation of the <see cref="INotifyCollectionChange"/> interface.
    /// </summary>
    [Serializable]
    public class NotifyCollectionChangeImplementation : IFireCollectionChange, INotifyCollectionChange
    {
        private static readonly ILog log = LogManager.GetLogger(typeof (NotifyCollectionChangeImplementation));

        private readonly object instance;

        private string intanceCredentials;

        public string InstanceCredentials
        {
            get { return intanceCredentials; }
            set { intanceCredentials = value; }
        }

        private ArrayList observedObjects = new ArrayList(); // objects which are being observed by current object
        private ArrayList observersObjects = new ArrayList(); // objects observing current object
        private bool logging;

        // TODO: make it join several calls into 1
        //private static DateTime lastCallTime;

        /// <summary>
        /// Initializes a new <see cref="NotifyCollectionChangeImplementation"/> instance.
        /// </summary>
        /// <param name="instance">Instance of the object where aspect is applied.</param>
        public NotifyCollectionChangeImplementation(object instance, InstanceCredentials credentials, bool logging)
        {
            this.instance = instance;
            this.intanceCredentials = credentials.ToString();
            this.logging = logging;
        }

        public object Instance
        {
            get { return instance; }
        }

        #region IFireCollectionChange Members

        #endregion

        #region INotifyCollectionChange Members

        /// <summary>
        /// Event raised when a property is change on the instance that
        /// exposes the current implementation.
        /// </summary>
        public virtual event NotifyCollectionChangedEventHandler CollectionChanged;

        public virtual event NotifyCollectionChangingEventHandler CollectionChanging;

        #endregion

        /// <summary>
        /// Used to bubble events
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnCollectionChanged(object sender, NotifyCollectionChangingEventArgs e)
        {
            if (!BubblingEnabled)
                return;

            if (CollectionChanged != null)
            {
                if (logging)
                {
                    //log.DebugFormat("Forwarding '{0}' property changes: '{1}' > '{2}, {3}'", e.PropertyName, sender,
                    //                instance.GetType().Name, intanceCredentials);
                }
                lock (sender)
                {
                    CollectionChanged(sender, e);
                }
            }
        }

        public void Unsubscribe(INotifyCollectionChange observed)
        {
            if (logging)
            {
                /*
                                log.DebugFormat("Unsubscribed '{0}, {1}' from changes of property '{2}'", instance.GetType().Name,
                                                intanceCredentials, change);
                */
            }
            lock (observed)
            {
                // observedObjects.Remove(change); // cleanup objects being observed
                // change.ObserversObjects.Remove(this); // remember objects observing item
                observed.CollectionChanged -= observed_CollectionChanged;
                observed.CollectionChanging -= observed_CollectionChanging;
            }
        }

        public void Subscribe(INotifyCollectionChange change)
        {
            if (logging)
            {
                /*
                                log.DebugFormat("Subscribed '{0}, {1}' to changes of property '{2}'", instance.GetType().Name,
                                                intanceCredentials, change);
                */
            }
            lock (change)
            {
                // observedObjects.Add(change); // remember objects being observed
                // change.ObserversObjects.Add(this); // remember objects observing item
                change.CollectionChanged += observed_CollectionChanged;
                change.CollectionChanging += observed_CollectionChanging;
            }
        }

        private void observed_CollectionChanging(object sender, NotifyCollectionChangingEventArgs e)
        {
            OnCollectionChanging(sender, e);
        }

        public static bool BubblingEnabled = true;

        public void OnCollectionChanging(object sender, NotifyCollectionChangingEventArgs e)
        {
            if (!BubblingEnabled)
                return;

            if (CollectionChanging != null)
            {
                if (logging)
                {
                    //log.DebugFormat("Forwarding '{0}' property changes: '{1}' > '{2}, {3}'", e.PropertyName, sender,
                    //                instance.GetType().Name, intanceCredentials);
                }
                lock (sender)
                {
                    CollectionChanging(sender, e);
                }
            }
        }

        public IList ObservedObjects
        {
            get { return observedObjects; }
        }

        public IList ObserversObjects
        {
            get { return observersObjects; }
        }

        private void observed_CollectionChanged(object sender, NotifyCollectionChangingEventArgs e)
        {
            OnCollectionChanged(sender, e);
        }
    }
}