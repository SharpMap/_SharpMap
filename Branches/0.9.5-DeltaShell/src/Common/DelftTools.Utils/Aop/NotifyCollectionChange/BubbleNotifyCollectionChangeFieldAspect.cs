using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DelftTools.Utils.Collections;
using PostSharp.Laos;

namespace DelftTools.Utils.Aop.NotifyCollectionChange
{
    /// <summary>
    /// Subscribe to childobject in case it implements INotifyCollectionChange.
    /// </summary>
    [Serializable]
    public class BubbleNotifyCollectionChangeFieldAspect : OnFieldAccessAspect
    {
        private bool enableLogging;

        public BubbleNotifyCollectionChangeFieldAspect(NotifyCollectionChangeAttribute parent,
                                                   bool enableLogging)
        {
            AspectPriority = parent.AspectPriority;
            this.enableLogging = enableLogging;
        }

        private static Dictionary<Type, bool> thisTypeIsLastComposedType = new Dictionary<Type, bool>();

        public override void OnSetValue(FieldAccessEventArgs eventArgs)
        {
            var implementation =
                (IFireCollectionChange)
                ((IComposed<INotifyCollectionChange>) eventArgs.Instance).GetImplementation(
                    eventArgs.InstanceCredentials);

            var oldValue = eventArgs.StoredFieldValue;
            var newValue = eventArgs.ExposedFieldValue;

            // manage subscription to childobjects that are notifiable
            if (oldValue is INotifyCollectionChange)
            {
                implementation.Unsubscribe(oldValue as INotifyCollectionChange);
            }

            if (newValue is INotifyCollectionChange)
            {
                implementation.Subscribe(newValue as INotifyCollectionChange);
            }

            //base.OnSetValue(eventArgs);

            var value = oldValue ?? newValue;

            if (value == null)
            {
                base.OnSetValue(eventArgs);
            }
            else
            {
                if (!thisTypeIsLastComposedType.ContainsKey(eventArgs.DeclaringType))
                {
                    var composedBaseTypes = eventArgs.DeclaringType.GetInterfaces()
                        .Where(t => t.Name.Contains("IComposed") && t.IsGenericType);

                    thisTypeIsLastComposedType[eventArgs.DeclaringType] = typeof (IComposed<INotifyCollectionChange>) == composedBaseTypes.Last();
                }

                // prevent calling set values two times, call it only in the last aspect
                var valueImplements2Aspects = value is INotifyPropertyChanged && value is INotifyCollectionChange;

                if (!valueImplements2Aspects || thisTypeIsLastComposedType[eventArgs.DeclaringType])
                {
                    base.OnSetValue(eventArgs);
                }
            }
        }
    }
}