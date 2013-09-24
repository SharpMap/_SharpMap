using System;
using System.ComponentModel;
using DelftTools.Utils.Reflection;

using log4net;

namespace DelftTools.Utils.UndoRedo.Mementos
{
    public class NotifyPropertyChangeMemento : CompoundMemento
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(NotifyPropertyChangeMemento));

        private object instance;
        private string propertyName;
        private object oldValue;
        private object newValue;

        public override void Restore()
        {
            FireBeforeEventCall(instance, true);

            base.Restore();

            if (propertyName == "IsEditing")
                return;

            SuppressNextBeforeEventCall();

            // restore property value
            if (!TypeUtils.TrySetValueAnyVisibility(instance, instance.GetType(), propertyName, oldValue))
            {
                throw new InvalidOperationException(String.Format("Cannot find setter for {0} on {1}", propertyName,
                                                                    instance.GetType()));
            }
        }
        
        public void RememberOldValue(object sender, PropertyChangingEventArgs e)
        {
            if(instance != null)
            {
                throw new InvalidOperationException("Old value is already initialized");
            }

            instance = sender;
            propertyName = e.PropertyName;
            oldValue = TypeUtils.GetPropertyValue(sender, e.PropertyName);
        }

        public void RememberNewValue(object sender, PropertyChangedEventArgs e)
        {
            if (instance != sender)
            {
                throw new InvalidOperationException("Instance of the new value is not the same as for the old value");
            }

            if (newValue != null)
            {
                throw new InvalidOperationException("New value is already initialized");
            }

            newValue = TypeUtils.GetPropertyValue(sender, e.PropertyName);
        }

        /// <summary>
        /// Converts memento to user-readable string.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (log.IsDebugEnabled)
            {
                return string.Format(
                    "Change {0} from \"{1}\" to \"{2}\" ({3})",
                    propertyName,
                    oldValue,
                    newValue,
                    instance.GetType().Name);
            }
            else
            {
                return string.Format(
                    "Change {0} from \"{1}\" to \"{2}\"",
                    propertyName,
                    oldValue,
                    newValue);
            }
        }

        public object Instance
        {
            get { return instance; }
            set { instance = value; }
        }

        public string PropertyName
        {
            get { return propertyName; }
            set { propertyName = value; }
        }

        public object OldValue
        {
            get { return oldValue; }
            set { oldValue = value; }
        }

        public object NewValue
        {
            get { return newValue; }
            set { newValue = value; }
        }

        /// <summary>
        ///  This is the last object where event comes from (used for debugging).
        /// </summary>
        public object LastEventSender { get; set; }

        public bool LastEventSenderIsDisconnected { get; set; }
    }
}