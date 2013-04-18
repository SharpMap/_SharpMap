using System;
using System.ComponentModel;
using DelftTools.Utils.Reflection;

namespace DelftTools.Utils.UndoRedo.Mementos
{
    public class NotifyPropertyChangeMemento : IMemento
    {
        private object instance;
        private string propertyName;
        private object oldValue;
        private object newValue;

        public IMemento Restore()
        {
            var memento = new NotifyPropertyChangeMemento()
                              {
                                  instance = instance,
                                  propertyName = propertyName,
                                  newValue = oldValue,
                                  oldValue = newValue
                              };

            // restore property value
            instance.GetType().GetProperty(propertyName).SetValue(instance, oldValue, null);

            return memento;
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
            return string.Format("Change {0} from \"{1}\" to \"{2}\" ({3})", propertyName, oldValue, newValue, instance.GetType().Name); 
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
    }
}