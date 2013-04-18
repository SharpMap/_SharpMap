using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using log4net;
using PostSharp.Laos;

namespace DelftTools.Utils.Aop.NotifyPropertyChange
{
    /// <summary>
    /// Implementation of the <see cref="INotifyPropertyChanged"/> interface.
    /// </summary>
    [Serializable]
    public class NotifyPropertyChangeImplementation : IFirePropertyChange, INotifyPropertyChange
    {
        private static readonly ILog log = LogManager.GetLogger(typeof (NotifyPropertyChangeImplementation));

        public object instance;

        private string intanceCredentials;

        public virtual object Instance { get { return instance; } }

        public virtual string InstanceCredentials
        {
            get { return intanceCredentials; }
            set { intanceCredentials = value; }
        }

        private ArrayList observedObjects = new ArrayList(); // objects which are being observed by current object
        private ArrayList observersObjects = new ArrayList(); // objects observing current object
        private bool logging;
        private bool isLastPropertyNotifier;

        // performance optimization
        private static Dictionary<Type, bool> cachedIsLastPropertyNotifier = new Dictionary<Type, bool>();

        /// <summary>
        /// Initializes a new <see cref="NotifyPropertyChangeImplementation"/> instance.
        /// </summary>
        /// <param name="instance">Instance of the object where aspect is applied.</param>
        public NotifyPropertyChangeImplementation(object instance, InstanceCredentials credentials, bool logging)
        {
            this.instance = instance;
            this.logging = logging;

            var type = instance.GetType();
            if (!cachedIsLastPropertyNotifier.ContainsKey(type))
            {
                var composedBaseTypes = type.GetInterfaces()
                    .Where(t => t.Name.Contains("IComposed") && t.IsGenericType);

                var thisType = typeof (IComposed<INotifyPropertyChange>);

                isLastPropertyNotifier = thisType == composedBaseTypes.Last();

                cachedIsLastPropertyNotifier[type] = isLastPropertyNotifier;
            }
            else
            {
                isLastPropertyNotifier = cachedIsLastPropertyNotifier[type];
            }
        }

        /// <summary>
        /// Event raised when a property is changed on the instance that
        /// exposes the current implementation.
        /// </summary>
        public virtual event PropertyChangingEventHandler PropertyChanging;

        /// <summary>
        /// Event raised when a property is changed on the instance that
        /// exposes the current implementation.
        /// </summary>
        public virtual event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Used to bubble events
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public virtual void FirePropertyChanging(object sender, PropertyChangingEventArgs e)
        {
            if (!BubblingEnabled)
                return;

            if (PropertyChanging != null)
            {
                PropertyChanging(sender, e);
            }
        }

        public static bool BubblingEnabled = true;

        /// <summary>
        /// Used to bubble events
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public virtual void FirePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!BubblingEnabled)
                return;

            if (PropertyChanged != null)
            {
                PropertyChanged(sender, e);
            }
        }

        public virtual void Unsubscribe(INotifyPropertyChange change)
        {
            change.PropertyChanged -= FirePropertyChanged;

            change.PropertyChanging -= FirePropertyChanging;
        }

        public virtual void Subscribe(INotifyPropertyChange changed)
        {
            changed.PropertyChanged -= FirePropertyChanged;
            changed.PropertyChanged += FirePropertyChanged;

            changed.PropertyChanging -= FirePropertyChanging;
            changed.PropertyChanging += FirePropertyChanging;
        }

        public virtual IList ObservedObjects
        {
            get { return observedObjects; }
        }

        public virtual IList ObserversObjects
        {
            get { return observersObjects; }
        }

        public bool IsLastPropertyNotifier
        {
            get 
            {
                return isLastPropertyNotifier;
            }
        }
    }
}