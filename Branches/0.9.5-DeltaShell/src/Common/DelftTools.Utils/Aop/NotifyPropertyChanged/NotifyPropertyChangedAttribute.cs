using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using DelftTools.Utils.Reflection;
using PostSharp.Extensibility;
using PostSharp.Laos;

namespace DelftTools.Utils.Aop.NotifyPropertyChanged
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
    public sealed class NotifyPropertyChangedAttribute : CompoundAspect
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
            collection.AddAspect(targetType, new AddNotifyPropertyChangedInterfaceAspect(enableLogging));

            // Add a OnMethodBoundaryAspect on each writable non-static property.
            foreach (PropertyInfo property in targetType.UnderlyingSystemType.GetProperties())
            {
                object[] attributes = property.GetCustomAttributes(typeof (NoNotifyPropertyChangedAttribute), true);
                // skip changes of selected properties marked with [NoNotifyPropertyChanged]
                if (attributes.Length != 0)
                {
                    Console.WriteLine("Skipping property: " + property.Name);
                    continue;
                }

                attributes = property.GetCustomAttributes(typeof(NoBubbling), true);
                if (attributes.Length != 0)
                {
                    Console.WriteLine("Skipping property: " + property.Name);
                    continue;
                }

                if (property.DeclaringType == targetType && property.CanWrite)
                {
                    MethodInfo method = property.GetSetMethod();

                    if (method != null && !method.IsStatic)
                    {
                        Console.WriteLine("Intercepting NotifyPropertyChanged aspect for property: " + property.Name);
                        collection.AddAspect(method,
                                             new OnNotifyPropertyChangedAspect(property.Name, this, enableLogging));
                    }
                }
            }

            //todo exclude: eventhandlers from subscription
            foreach (
                FieldInfo fi in
                    targetType.UnderlyingSystemType.GetFields(BindingFlags.Instance | BindingFlags.Public |
                                                              BindingFlags.NonPublic))
            {
                object[] attributes = fi.GetCustomAttributes(typeof(NoNotifyPropertyChangedAttribute), true);
                if (attributes.Length != 0)
                {
                    Console.WriteLine("Skipping field: " + fi.Name);
                    continue;
                }

                attributes = fi.GetCustomAttributes(typeof(NoBubbling), true);
                if (attributes.Length != 0)
                {
                    Console.WriteLine("Skipping field: " + fi.Name);
                    continue;
                }

                if (fi.DeclaringType == targetType && !fi.IsStatic && !fi.FieldType.IsValueType &&
                    fi.FieldType != typeof (string) && !TypeUtils.IsDelegate(fi.FieldType))
                {
                    //only apply to field setter, not to getter.
                    Console.WriteLine("Intercepting NotifyPropertyChanged aspect for field: " + fi.Name + " bubbling");
                    collection.AddAspect(fi, new BubbleNotifyPropertyChangedAspect(fi.Name, this, enableLogging));
                }
            }
        }



    }
}