using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using DelftTools.Utils.Aop.NotifyPropertyChange;
using DelftTools.Utils.Collections;
using DelftTools.Utils.Reflection;
using PostSharp.Extensibility;
using PostSharp.Laos;

namespace DelftTools.Utils.Aop.NotifyCollectionChange
{
    /// <summary>
    /// Custom attribute that, when applied on a type (designated <i>target type</i>), implements the interface
    /// <see cref="INotifyCollectionChange"/> and raises the <see cref="INotifyCollectionChange.CollectionChanged"/>
    /// event when any property of the target type is modified.
    /// </summary>
    /// <remarks>
    /// Event raising is implemented by appending logic to the <b>set</b> accessor of properties. The 
    /// currently only forwards events from items that implement <see cref="INotifyCollectionChange"/>
    /// <see cref="INotifyCollectionChange.CollectionChanged"/> is raised only when accessors successfully complete.
    /// </remarks>
    [Serializable, MulticastAttributeUsage(MulticastTargets.Class | MulticastTargets.Struct)]
    public sealed class NotifyCollectionChangeAttribute : CompoundAspect
    {
        private bool enableLogging = true;
        [NonSerialized]
        private int aspectPriority;

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
            Type targetType = (Type)targetElement;

            Console.WriteLine("Adding aspects to type: " + targetType);

            // On the type, add a Composition aspect to implement the INotifyCollectionChange interface.
            collection.AddAspect(targetType, new AddNotifyCollectionChangeInterfaceAspect(enableLogging));

            Console.WriteLine("Applicable to {0} fields.",
                              targetType.UnderlyingSystemType.GetFields(BindingFlags.Instance | BindingFlags.Public |
                                                                        BindingFlags.NonPublic).Length);
            foreach (var fieldInfo in targetType.UnderlyingSystemType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                Console.WriteLine("Analizing: " + fieldInfo.Name);
                // skip changes of selected fields marked with [NoNotifyPropertyChanged]
                if (IsBackingField(fieldInfo))
                {
                    Console.WriteLine("Analizing backing field: " + fieldInfo.Name);
                    PropertyInfo pi = GetPublicProperty(targetType, fieldInfo);
                    if (pi == null)
                    {
                        Console.WriteLine("Skipping field: {0}, no public property found." + fieldInfo.Name);
                        continue;
                    }
                    if (pi.GetCustomAttributes(typeof(NoNotifyPropertyChangeAttribute), true).Length != 0)
                    {
                        Console.WriteLine("Skipping field: " + fieldInfo.Name);
                        continue;
                    }
                    if (pi.GetCustomAttributes(typeof(NoNotifyCollectionChangedAttribute), true).Length != 0)
                    {
                        Console.WriteLine("Skipping field: " + fieldInfo.Name);
                        continue;
                    }
                }


                object[] attributes = fieldInfo.GetCustomAttributes(typeof(NoNotifyPropertyChangeAttribute), true);
                if (attributes.Length != 0)
                {
                    Console.WriteLine("Skipping field: " + fieldInfo.Name);
                    continue;
                }
                attributes = fieldInfo.GetCustomAttributes(typeof(NoNotifyCollectionChangedAttribute), true);
                if (attributes.Length != 0)
                {
                    Console.WriteLine("Skipping field: " + fieldInfo.Name);
                    continue;
                }

                if (fieldInfo.DeclaringType == targetType && !fieldInfo.IsStatic && fieldInfo.FieldType != typeof(string) && !fieldInfo.FieldType.IsValueType && !TypeUtils.IsDelegate(fieldInfo.FieldType))
                {
                    Console.WriteLine("Intercepting NotifyCollectionChanged aspect for field: " + fieldInfo.Name);
                    collection.AddAspect(fieldInfo, new BubbleNotifyCollectionChangeFieldAspect(this, enableLogging));
                }
            }
        }

        public static PropertyInfo GetPublicProperty(Type targetType, FieldInfo backingField)
        {
            string propertyName = backingField.Name.Substring(backingField.Name.IndexOf("<") + 1,
                                                              backingField.Name.IndexOf(">") - 1);
            Console.WriteLine("Looking for property {0}", propertyName);
            return targetType.GetProperty(propertyName);
        }

        public static bool IsBackingField(FieldInfo fi)
        {
            return ((fi.GetCustomAttributes(typeof(CompilerGeneratedAttribute), true).Length != 0)
                    && fi.Name.EndsWith("k__BackingField"));
        }

        public static bool IsCollection(Type type)
        {
            if (type.IsValueType || type == typeof(string))
            {
                return false;
            }
            //why this generic IEnumerable<> requirement?
            /*if (type.IsGenericType)
            {
                return type.GetGenericTypeDefinition() == typeof (IEnumerable<>);
            }*/

            //does the field implement IEnumerable or INotifyCollectionChange?
            return type.GetInterfaces().Any(
                i => i == typeof(IEnumerable)
                    || i == typeof(INotifyCollectionChange));
        }
    }

    ///// <summary>
    ///// HelperClass to manipulate object of type InstanceCredentials.
    ///// </summary>
    //public class InstanceCredentialsConvertor
    //{
    //    public static string ToString(InstanceCredentials credentials)
    //    {
    //        return credentials.GetType().InvokeMember("value",
    //            BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance, null, credentials, null).ToString();
    //    }
    //}
}
