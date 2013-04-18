using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using log4net;
using PostSharp.Laos;

namespace DelftTools.Utils.Aop.NotifyPropertyChanged
{
    /// <summary>
    /// Implementation of the <see cref="INotifyPropertyChanged"/> interface.
    /// </summary>
    [Serializable]
    public class NotifyPropertyChangedImplementation : IFirePropertyChanged, INotifyPropertyChanged
    {
        private static readonly ILog log = LogManager.GetLogger(typeof (NotifyPropertyChangedImplementation));

        private object instance;

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
        /// Initializes a new <see cref="NotifyPropertyChangedImplementation"/> instance.
        /// </summary>
        /// <param name="instance">Instance of the object where aspect is applied.</param>
        public NotifyPropertyChangedImplementation(object instance, InstanceCredentials credentials, bool logging)
        {
            this.instance = instance;
            this.logging = logging;

            var type = instance.GetType();
            if (!cachedIsLastPropertyNotifier.ContainsKey(type))
            {
                var composedBaseTypes = type.GetInterfaces()
                    .Where(t => t.Name.Contains("IComposed") && t.IsGenericType);

                var thisType = typeof (IComposed<INotifyPropertyChanged>);

                isLastPropertyNotifier = thisType == composedBaseTypes.Last();

                cachedIsLastPropertyNotifier[type] = isLastPropertyNotifier;
            }
            else
            {
                isLastPropertyNotifier = cachedIsLastPropertyNotifier[type];
            }
        }

        #region IFirePropertyChanged Members

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event. Called by the
        /// property-level aspect (<see cref="AddNotifyPropertyChangedInterfaceAspect"/>)
        /// at the end of property set accessors.
        /// </summary>
        /// <param name="propertyName">Name of the changed property.</param>
        /// <param name="newValue">New value of the property.</param>
        public virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(instance, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

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
        public virtual void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(sender, e);
            }
        }

        public virtual void Unsubscribe(INotifyPropertyChanged changed)
        {
            changed.PropertyChanged -= item_PropertyChanged;
        }

        public virtual void Subscribe(INotifyPropertyChanged changed)
        {
            changed.PropertyChanged -= item_PropertyChanged;
            changed.PropertyChanged += item_PropertyChanged;
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

        private void item_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            OnPropertyChanged(sender, e);
        }
    }
}