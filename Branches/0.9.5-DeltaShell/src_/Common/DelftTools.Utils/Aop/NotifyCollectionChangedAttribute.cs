using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using DelftTools.Utils.Aop.NotifyPropertyChanged;
using DelftTools.Utils.Collections;
using DelftTools.Utils.Reflection;
using log4net;
using PostSharp.Extensibility;
using PostSharp.Laos;

namespace DelftTools.Utils.Aop
{
    /// <summary>
    /// Custom attribute that, when applied on a type (designated <i>target type</i>), implements the interface
    /// <see cref="INotifyCollectionChanged"/> and raises the <see cref="INotifyCollectionChanged.CollectionChanged"/>
    /// event when any property of the target type is modified.
    /// </summary>
    /// <remarks>
    /// Event raising is implemented by appending logic to the <b>set</b> accessor of properties. The 
    /// currently only forwards events from items that implement <see cref="INotifyCollectionChanged"/>
    /// <see cref="INotifyCollectionChanged.CollectionChanged"/> is raised only when accessors successfully complete.
    /// </remarks>
    [Serializable, MulticastAttributeUsage(MulticastTargets.Class | MulticastTargets.Struct)]
    public sealed class NotifyCollectionChangedAttribute : CompoundAspect 
    {
        private bool enableLogging = true;
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

            // On the type, add a Composition aspect to implement the INotifyCollectionChanged interface.
            collection.AddAspect(targetType, new AddNotifyCollectionChangedInterfaceAspect(enableLogging));

            Console.WriteLine("Applicable to {0} fields.",
                              targetType.UnderlyingSystemType.GetFields(BindingFlags.Instance | BindingFlags.Public |
                                                                        BindingFlags.NonPublic).Length);
            foreach (
                FieldInfo fi in
                    targetType.UnderlyingSystemType.GetFields(BindingFlags.Instance | BindingFlags.Public |
                                                              BindingFlags.NonPublic))
            {
                Console.WriteLine("Analizing: " + fi.Name);
                // skip changes of selected fields marked with [NoNotifyPropertyChanged]
                if (IsBackingField(fi))
                {
                    Console.WriteLine("Analizing backing field: " + fi.Name);
                    PropertyInfo pi = GetProperty(targetType, fi);
                    if (pi.GetCustomAttributes(typeof(NoBubbling), true).Length != 0)
                    {
                        Console.WriteLine("Skipping field: " + fi.Name);
                        continue;
                    }
                    if (pi.GetCustomAttributes(typeof(NoNotifyCollectionChangedAttribute), true).Length != 0)
                    {
                        Console.WriteLine("Skipping field: " + fi.Name);
                        continue;
                    }
                }


                object[] attributes = fi.GetCustomAttributes(typeof (NoBubbling), true);
                if (attributes.Length != 0)
                {
                    Console.WriteLine("Skipping field: " + fi.Name);
                    continue;
                }
                attributes = fi.GetCustomAttributes(typeof(NoNotifyCollectionChangedAttribute), true);
                if (attributes.Length != 0)
                {
                    Console.WriteLine("Skipping field: " + fi.Name);
                    continue;
                }
                
                if (fi.DeclaringType == targetType && !fi.IsStatic && IsCollection(fi.FieldType) && !TypeUtils.IsDelegate(fi.FieldType))
                {
                    Console.WriteLine("Intercepting NotifyCollectionChanged aspect for field: " + fi.Name);
                    collection.AddAspect(fi, new BubbleNotifyCollectionChangedAspect(this, enableLogging));
                }
            }
        }

        public static PropertyInfo GetProperty(Type targetType,FieldInfo backingField)
        {
            string propertyName = backingField.Name.Substring(backingField.Name.IndexOf("<")+1,
                                                              backingField.Name.IndexOf(">")-1);
            Console.WriteLine("Looking for property {0}",propertyName);
            return targetType.GetProperty(propertyName);
            foreach (PropertyInfo pi in targetType.GetProperties())
            {
                if (pi.Name == propertyName && pi.PropertyType == backingField.FieldType)
                    return pi;
            }
            throw new Exception("Property for backingfield not found."+propertyName);
        }

        public static bool IsBackingField(FieldInfo fi)
        {
            return ((fi.GetCustomAttributes(typeof (CompilerGeneratedAttribute),true).Length != 0)
                    && fi.Name.EndsWith("k__BackingField"));
        }
        
        public static bool IsCollection(Type type)
        {
            if (type.IsValueType || type == typeof (string))
            {
                return false;
            }
            //why this generic IEnumerable<> requirement?
            /*if (type.IsGenericType)
            {
                return type.GetGenericTypeDefinition() == typeof (IEnumerable<>);
            }*/

            //does the field implement IEnumerable or INotifyCollectionChanged?
            return type.GetInterfaces().Any(
                i=> i ==  typeof (IEnumerable) 
                    || i == typeof(INotifyCollectionChanged));
            foreach (Type t in type.GetInterfaces())
            {
                Console.WriteLine("Interface: "+t);
                if (t == typeof (IEnumerable))
                {
                    return true;
                }
                else if (t == typeof (INotifyCollectionChanged))
                {
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>
    /// Implementation of <see cref="CompositionAspect"/> that adds the <see cref="INotifyCollectionChanged"/>
    /// interface to the type to which it is applied.
    /// </summary>
    [Serializable]
    internal class AddNotifyCollectionChangedInterfaceAspect : CompositionAspect
    {
        private static readonly ILog log = LogManager.GetLogger(typeof (AddNotifyCollectionChangedInterfaceAspect));
        private bool logging;

        public AddNotifyCollectionChangedInterfaceAspect(bool logging)
        {
            this.logging = logging;
        }

        /// <summary>
        /// Called at runtime, creates the implementation of the <see cref="INotifyCollectionChanged"/> interface.
        /// 
        /// NOTE: tries to get implementation of aspect from instance, othrewise create a new one
        /// this is critical for objects with multiple constructors (see generated code in Reflector)
        /// </summary>
        /// <param name="eventArgs">Execution context.</param>
        /// <returns>A new instance of <see cref="NotifyCollectionChangedImplementation"/>, which implements
        /// <see cref="INotifyCollectionChanged"/>.</returns>
        public override object CreateImplementationObject(InstanceBoundLaosEventArgs eventArgs)
        {
            InstanceCredentials credentials = eventArgs.InstanceCredentials;
            if (logging)
            {
                /*
                                log.DebugFormat("Creating new instance of interceptor for object {0}, credentials: {1}",
                                                eventArgs.Instance,
                                                InstanceCredentialsConvertor.ToString(eventArgs.InstanceCredentials));
                */
            }
            object implementation =
                ((IComposed<INotifyCollectionChanged>) eventArgs.Instance).GetImplementation(
                    eventArgs.InstanceCredentials);

            if (implementation != null)
            {
                if (logging)
                {
/*
                    log.DebugFormat(
                        "Instance already has an implementation injected - returning it and updating credentials {0} > ({1})",
                        ((NotifyCollectionChangedImplementation) implementation).InstanceCredentials,
                        InstanceCredentialsConvertor.ToString(credentials));
*/
                }
                ((NotifyCollectionChangedImplementation) implementation).InstanceCredentials =
                    credentials.ToString();
            }

            return implementation ??
                   new NotifyCollectionChangedImplementation(eventArgs.Instance, eventArgs.InstanceCredentials, logging);
        }

        /// <summary>
        /// Called at compile-time, gets the interface that should be publicly exposed.
        /// </summary>
        /// <param name="containerType">Type on which the interface will be implemented.</param>
        /// <returns></returns>
        public override Type GetPublicInterface(Type containerType)
        {
            return typeof (INotifyCollectionChanged);
        }

        public override Type[] GetProtectedInterfaces(Type containerType)
        {
            return new Type[] {typeof (IFireCollectionChanged)};
        }

        /// <summary>
        /// Gets weaving options.
        /// </summary>
        /// <returns>Weaving options specifying that the implementation accessor interface (<see cref="IComposed{T}"/>)
        /// should be exposed, and that the implementation of interfaces should be silently ignored if they are
        /// already implemented in the parent types.</returns>
        public override CompositionAspectOptions GetOptions()
        {
            return
                CompositionAspectOptions.GenerateImplementationAccessor |
                CompositionAspectOptions.IgnoreIfAlreadyImplemented;
        }
    }

    /// <summary>
    /// Implementation of the <see cref="INotifyCollectionChanged"/> interface.
    /// </summary>
    [Serializable]
    public class NotifyCollectionChangedImplementation : IFireCollectionChanged, INotifyCollectionChanged
    {
        private static readonly ILog log = LogManager.GetLogger(typeof (NotifyCollectionChangedImplementation));

        private readonly object instance;

        private string intanceCredentials;

        public string InstanceCredentials
        {
            get { return intanceCredentials; }
            set { intanceCredentials = value; }
        }

        private ArrayList observedObjects = new ArrayList(); // objects which are being observed by current object
        private ArrayList observersObjects = new ArrayList(); // objects observing current object
        private bool logging;

        // TODO: make it join several calls into 1
        //private static DateTime lastCallTime;

        /// <summary>
        /// Initializes a new <see cref="NotifyCollectionChangedImplementation"/> instance.
        /// </summary>
        /// <param name="instance">Instance of the object where aspect is applied.</param>
        public NotifyCollectionChangedImplementation(object instance, InstanceCredentials credentials, bool logging)
        {
            this.instance = instance;
            this.intanceCredentials = credentials.ToString();
            this.logging = logging;
        }

        public object Instance
        {
            get { return instance; }
        }

        #region IFireCollectionChanged Members

        #endregion

        #region INotifyCollectionChanged Members

        /// <summary>
        /// Event raised when a property is changed on the instance that
        /// exposes the current implementation.
        /// </summary>
        public virtual event NotifyCollectionChangedEventHandler CollectionChanged;

        public virtual event NotifyCollectionChangedEventHandler CollectionChanging;

        #endregion

        /// <summary>
        /// Used to bubble events
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (CollectionChanged != null)
            {
                if (logging)
                {
                    //log.DebugFormat("Forwarding '{0}' property changes: '{1}' > '{2}, {3}'", e.PropertyName, sender,
                    //                instance.GetType().Name, intanceCredentials);
                }
                lock (sender)
                {
                    CollectionChanged(sender, e);
                }
            }
        }

        public void Unsubscribe(INotifyCollectionChanged changed)
        {
            if (logging)
            {
                /*
                                log.DebugFormat("Unsubscribed '{0}, {1}' from changes of property '{2}'", instance.GetType().Name,
                                                intanceCredentials, changed);
                */
            }
            lock (changed)
            {
                // observedObjects.Remove(changed); // cleanup objects being observed
                // changed.ObserversObjects.Remove(this); // remember objects observing item
                changed.CollectionChanged -= argument_CollectionChanged;
                changed.CollectionChanging -= argument_CollectionChanging;
            }
        }

        public void Subscribe(INotifyCollectionChanged changed)
        {
            if (logging)
            {
                /*
                                log.DebugFormat("Subscribed '{0}, {1}' to changes of property '{2}'", instance.GetType().Name,
                                                intanceCredentials, changed);
                */
            }
            lock (changed)
            {
                // observedObjects.Add(changed); // remember objects being observed
                // changed.ObserversObjects.Add(this); // remember objects observing item
                changed.CollectionChanged += argument_CollectionChanged;
                changed.CollectionChanging += argument_CollectionChanging;
            }
        }

        private void argument_CollectionChanging(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnCollectionChanging(sender, e);
        }

        public void OnCollectionChanging(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (CollectionChanging != null)
            {
                if (logging)
                {
                    //log.DebugFormat("Forwarding '{0}' property changes: '{1}' > '{2}, {3}'", e.PropertyName, sender,
                    //                instance.GetType().Name, intanceCredentials);
                }
                lock (sender)
                {
                    CollectionChanging(sender, e);
                }
            }
        }

        public IList ObservedObjects
        {
            get { return observedObjects; }
        }

        public IList ObserversObjects
        {
            get { return observersObjects; }
        }

        private void argument_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnCollectionChanged(sender, e);
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

    /// <summary>
    /// Implementation of <see cref="OnMethodBoundaryAspect"/> that raises the 
    /// <see cref="INotifyCollectionChanged.CollectionChanged"/> event when a property set
    /// accessor completes successfully.
    /// </summary>
    [Serializable]
    internal class OnNotifyCollectionChangedAspect : OnMethodBoundaryAspect
    {
        private static readonly ILog log = LogManager.GetLogger(typeof (OnNotifyCollectionChangedAspect));

        private readonly string propertyName;

        private readonly Dictionary<string, INotifyCollectionChanged> notifiablePropertyValues = null;

        private IFireCollectionChanged implementation;

        private object newValue;
        private object oldValue;

        [NonSerialized] private MethodExecutionEventArgs eventArgs;

        private bool logging;

        /// <summary>
        /// Initializes a new <see cref="OnNotifyCollectionChangedAspect"/>.
        /// </summary>
        /// <param name="propertyName">Name of the property to which this set accessor belong.</param>
        /// <param name="parent">Parent <see cref="NotifyCollectionChangedAttribute"/>.</param>
        public OnNotifyCollectionChangedAspect(string propertyName, NotifyCollectionChangedAttribute parent,
                                               bool logging)
        {
            AspectPriority = parent.AspectPriority;
            this.propertyName = propertyName;
            this.logging = logging;
        }

        /// <summary>
        /// Executed when the set accessor successfully completes. Raises the 
        /// <see cref="INotifyCollectionChanged.CollectionChanged"/> event.
        /// </summary>
        /// <param name="eventArgs">Event arguments with information about the 
        /// current execution context.</param>
        public override void OnSuccess(MethodExecutionEventArgs eventArgs)
        {
            this.eventArgs = eventArgs;
/*
            string credentialsString = eventArgs.InstanceCredentials.ToString();
*/

            implementation =
                (IFireCollectionChanged)
                ((IComposed<INotifyCollectionChanged>) eventArgs.Instance).GetImplementation(
                    eventArgs.InstanceCredentials);

/*
            object arg = this.eventArgs.GetReadOnlyArgumentArray()[0];
*/

            if (logging)
            {
/*
                log.DebugFormat("Firing an event on property '{0}' set for: {1}, credentials {2}", propertyName,
                                eventArgs.Instance.GetType().Name, credentialsString);
*/
            }

            lock (implementation)
            {
                // manage subscription to childobjects that are notifiable
                //if (oldValue is INotifyCollectionChanged)
                //{
                //    implementation.Unsubscribe(oldValue as INotifyCollectionChanged);
                //}

                //if (arg is INotifyCollectionChanged)
                //{
                //    implementation.Subscribe(arg as INotifyCollectionChanged);
                //}

                //todo this should be moved to another onmethodboundary aspect for
                //adding and removing list items.
                //implementation.OnCollectionChanged((NotifyCollectionChangedAction) oldValue, newValue);
                // Raises the CollectionChanged event.
            }
        }

        //event bubbling
        public override void OnEntry(MethodExecutionEventArgs eventArgs)
        {
            PropertyInfo pi = eventArgs.Instance.GetType().GetProperty(propertyName,
                                                                       BindingFlags.Public | BindingFlags.NonPublic |
                                                                       BindingFlags.Instance);

            oldValue = pi.GetValue(eventArgs.Instance, null);
            newValue = eventArgs.GetReadOnlyArgumentArray()[0]; // will work only for simple properties!

            base.OnEntry(eventArgs);
        }

        public override void OnException(MethodExecutionEventArgs eventArgs)
        {
            base.OnException(eventArgs);
            log.Error("Exception occured during property set", eventArgs.Exception);
        }
    }

    public interface IFireCollectionChanged
    {
        void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e);
        void OnCollectionChanging(object sender, NotifyCollectionChangedEventArgs e);

        void Unsubscribe(INotifyCollectionChanged changed);

        void Subscribe(INotifyCollectionChanged changed);
        IList ObservedObjects { get; }
        IList ObserversObjects { get; }
    }

    /// <summary>
    /// Subscribe to childobject in case it implements INotifyCollectionChanged.
    /// </summary>
    [Serializable]
    internal class BubbleNotifyCollectionChangedAspect : OnFieldAccessAspect
    {
        private bool enableLogging;

        public BubbleNotifyCollectionChangedAspect(NotifyCollectionChangedAttribute parent,
                                                   bool enableLogging)
        {
            AspectPriority = parent.AspectPriority;
            this.enableLogging = enableLogging;
        }

        private static Dictionary<Type, bool> thisTypeIsLastComposedType = new Dictionary<Type, bool>();

        public override void OnSetValue(FieldAccessEventArgs eventArgs)
        {
            var implementation =
                (IFireCollectionChanged)
                ((IComposed<INotifyCollectionChanged>) eventArgs.Instance).GetImplementation(
                    eventArgs.InstanceCredentials);

            var oldValue = eventArgs.StoredFieldValue;
            var newValue = eventArgs.ExposedFieldValue;


            // manage subscription to childobjects that are notifiable
            if (oldValue is INotifyCollectionChanged)
            {
                implementation.Unsubscribe(oldValue as INotifyCollectionChanged);
            }

            if (newValue is INotifyCollectionChanged)
            {
                implementation.Subscribe(newValue as INotifyCollectionChanged);
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

                    thisTypeIsLastComposedType[eventArgs.DeclaringType] = typeof (IComposed<INotifyCollectionChanged>) == composedBaseTypes.Last();
                }

                // prevent calling set values two times, call it only in the last aspect
                var valueImplements2Aspects = value is INotifyPropertyChanged && value is INotifyCollectionChanged;

                if (!valueImplements2Aspects || thisTypeIsLastComposedType[eventArgs.DeclaringType])
                {
                    base.OnSetValue(eventArgs);
                }
            }
        }
    }
}
