using System;
using DelftTools.Utils.Collections;
using log4net;
using PostSharp.Laos;

namespace DelftTools.Utils.Aop.NotifyPropertyChange
{
    /// <summary>
    /// Subscribe to childobject in case it implements INotifyPropertyChange.
    /// </summary>
    [Serializable]
    public class BubblePropertyChangeFieldAspect : OnFieldAccessAspect
    {
        private static ILog log = LogManager.GetLogger(typeof (BubblePropertyChangeFieldAspect));

        public BubblePropertyChangeFieldAspect(string fieldName, NotifyPropertyChangeAttribute parent,
                                                 bool enableLogging)
        {
            AspectPriority = parent.AspectPriority;
        }

        public override void OnSetValue(FieldAccessEventArgs eventArgs)
        {
            var implementation =
                (IFirePropertyChange)
                ((IComposed<INotifyPropertyChange>) eventArgs.Instance).GetImplementation(eventArgs.InstanceCredentials);

            var oldValue = eventArgs.StoredFieldValue;
            var newValue = eventArgs.ExposedFieldValue;


            // manage subscription to childobjects that are notifiable
            if (oldValue is INotifyPropertyChange)
            {
                implementation.Unsubscribe(oldValue as INotifyPropertyChange);
            }

            if (newValue is INotifyPropertyChange)
            {
                implementation.Subscribe(newValue as INotifyPropertyChange);
            }


            var value = oldValue ?? newValue;

            if (value == null)
            {
                base.OnSetValue(eventArgs);
            }
            else
            {
                // prevent calling set values two times, call it only in the last aspect
                var valueImplements2Aspects = value is INotifyPropertyChange && value is INotifyCollectionChange;

                if (!valueImplements2Aspects || implementation.IsLastPropertyNotifier)
                {
                    base.OnSetValue(eventArgs);
                }
            }
        }
    }
}