using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using DelftTools.Utils.Reflection;
using PostSharp.Extensibility;
using PostSharp.Laos;

namespace DelftTools.Utils.Aop.NotifyPropertyChange
{
    /// <summary>
    /// Custom attribute that, when applied on a type (designated <i>target type</i>), implements the interface
    /// <see cref="INotifyPropertyChanged"/> and raises the <see cref="INotifyPropertyChanged.PropertyChanged"/>
    /// event when any property of the target type is modified.
    /// </summary>
    /// <remarks>
    /// Event raising is implemented by appending logic to the <b>set</b> accessor of properties. The 
    /// <see cref="INotifyPropertyChanged.PropertyChanged"/> is raised only when accessors successfully complete.
    /// </remarks>
    [Serializable, MulticastAttributeUsage(MulticastTargets.Class | MulticastTargets.Struct)]
    public sealed class NotifyPropertyChangeAttribute : CompoundAspect
    {
        private bool enableLogging;
        [NonSerialized] private int aspectPriority;

        /// <summary>
        /// Gets or sets the priority of the property-level aspect.
        /// </summary>
        /// <remarks>
        /// Give a large number to have the event raisen after any other
        /// on-success aspect on the properties of this type. The default value
        /// is 0.
        /// </remarks>
        public int AspectPriority
        {
            get { return aspectPriority; }
            set { aspectPriority = value; }
        }


        /// <summary>
        /// Log property changes
        /// </summary>
        public bool EnableLogging
        {
            get { return enableLogging; }
            set { enableLogging = value; }
        }

        /// <summary>
        /// Method called at compile time to get individual aspects required by the current compound
        /// aspect.
        /// </summary>
        /// <param name="targetElement">Metadata element (<see cref="Type"/> in our case) to which
        /// the current custom attribute instance is applied.</param>
        /// <param name="collection">Collection of aspects to which individual aspects should be
        /// added.</param>
        public override void ProvideAspects(object targetElement, LaosReflectionAspectCollection collection)
        {
            // Get the target type.
            Type targetType = (Type) targetElement;

            Console.WriteLine("Adding aspects to type: " + targetType);

            // On the type, add a Composition aspect to implement the INotifyPropertyChanged interface.
            collection.AddAspect(targetType, new AddNotifyPropertyChangeInterfaceAspect(enableLogging));

            var interceptedPropertyNames = new List<string>();

            // Add a OnMethodBoundaryAspect on each writable non-static property.
            foreach (var propertyInfo in targetType.UnderlyingSystemType.GetProperties())
            {
                var attributes = propertyInfo.GetCustomAttributes(typeof (NoNotifyPropertyChangeAttribute), true);
                // skip changes of selected properties marked with [NoNotifyPropertyChanged]
                if (attributes.Length != 0)
                {
                    Console.WriteLine("Skipping property: " + propertyInfo.Name);
                    continue;
                }

                attributes = propertyInfo.GetCustomAttributes(typeof(NoNotifyPropertyChangeAttribute), true);
                if (attributes.Length != 0)
                {
                    Console.WriteLine("Skipping property: " + propertyInfo.Name);
                    continue;
                }

                if (propertyInfo.DeclaringType == targetType && propertyInfo.CanWrite)
                {
                    //private set method is OK
                    MethodInfo setMethod = propertyInfo.GetSetMethod(true);//private setter is OK
                    MethodInfo getMethod = propertyInfo.GetGetMethod();//getter should be public

                    
                    if (getMethod != null && setMethod != null && !setMethod.IsStatic)
                    {
                        Console.WriteLine("Intercepting NotifyPropertyChanged aspect for property: " + propertyInfo.Name);

                        collection.AddAspect(setMethod, new NotifyPropertyChangeAspect(propertyInfo.Name, this, enableLogging));

                        interceptedPropertyNames.Add(propertyInfo.Name.ToLower());
                    }
                }
            }

            //todo exclude: eventhandlers from subscription
            foreach (var fieldInfo in targetType.UnderlyingSystemType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (!interceptedPropertyNames.Any(n => fieldInfo.Name.ToLower().Equals(n) || fieldInfo.Name.ToLower().Contains("<" + n)))
                {
                    Console.WriteLine("Skipping field: " + fieldInfo.Name + " (no property intercepted)");
                    continue;
                }

                // skip subscription if field is not of an interface type and not subscribable
                if(fieldInfo.FieldType.IsValueType)
                {
                    Console.WriteLine("Skipping field: " + fieldInfo.Name + " (does not implement INotifyPropertyChanged)");
                    continue;
                }

                var attributes = fieldInfo.GetCustomAttributes(typeof(NoNotifyPropertyChangeAttribute), true);
                if (attributes.Length != 0)
                {
                    Console.WriteLine("Skipping field: " + fieldInfo.Name);
                    continue;
                }

                attributes = fieldInfo.GetCustomAttributes(typeof(NoNotifyPropertyChangeAttribute), true);
                if (attributes.Length != 0)
                {
                    Console.WriteLine("Skipping field: " + fieldInfo.Name);
                    continue;
                }

                if (fieldInfo.DeclaringType == targetType && !fieldInfo.IsStatic && !fieldInfo.FieldType.IsValueType &&
                    fieldInfo.FieldType != typeof (string) && !TypeUtils.IsDelegate(fieldInfo.FieldType))
                {
                    //only apply to field setter, not to getter.
                    Console.WriteLine("Intercepting NotifyPropertyChanged aspect for field: " + fieldInfo.Name + " bubbling");
                    collection.AddAspect(fieldInfo, new BubblePropertyChangeFieldAspect(fieldInfo.Name, this, enableLogging));
                }
            }
        }



    }
}