using System;
using System.ComponentModel;
using System.Linq;
using DelftTools.Utils.Collections;
using log4net;
using PostSharp.Laos;

namespace DelftTools.Utils.Aop.NotifyPropertyChanged
{
    /// <summary>
    /// Subscribe to childobject in case it implements INotifyPropertyChanged.
    /// </summary>
    [Serializable]
    internal class BubbleNotifyPropertyChangedAspect : OnFieldAccessAspect
    {
        private static ILog log = LogManager.GetLogger(typeof (BubbleNotifyPropertyChangedAspect));

        public BubbleNotifyPropertyChangedAspect(string fieldName, NotifyPropertyChangedAttribute parent,
                                                 bool enableLogging)
        {
            AspectPriority = parent.AspectPriority;
        }

        public override void OnSetValue(FieldAccessEventArgs eventArgs)
        {
            IFirePropertyChanged implementation =
                (IFirePropertyChanged)
                ((IComposed<INotifyPropertyChanged>) eventArgs.Instance).GetImplementation(eventArgs.InstanceCredentials);

            //// no bubbling (performance)!
            // next optimization does not work; run unit tests
            //if (implementation.ObserversObjects.Count == 0)
            //{
            //    base.OnSetValue(eventArgs);
            //    return;
            //}

            object oldValue = eventArgs.StoredFieldValue;
            object newValue = eventArgs.ExposedFieldValue;


            // manage subscription to childobjects that are notifiable
            if (oldValue is INotifyPropertyChanged)
            {
                implementation.Unsubscribe(oldValue as INotifyPropertyChanged);
            }

            if (newValue is INotifyPropertyChanged)
            {
                implementation.Subscribe(newValue as INotifyPropertyChanged);
            }

/*
            base.OnSetValue(eventArgs);
*/


            var value = oldValue ?? newValue;

            if (value == null)
            {
                base.OnSetValue(eventArgs);
            }
            else
            {
                // prevent calling set values two times, call it only in the last aspect
                var valueImplements2Aspects = value is INotifyPropertyChanged && value is INotifyCollectionChanged;

                if (!valueImplements2Aspects || implementation.IsLastPropertyNotifier)
                {
                    base.OnSetValue(eventArgs);
                }
            }
        }
    }
}