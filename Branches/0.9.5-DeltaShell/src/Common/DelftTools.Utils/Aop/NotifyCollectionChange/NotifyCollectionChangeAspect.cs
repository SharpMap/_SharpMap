using System;
using System.Collections.Generic;
using System.Reflection;
using DelftTools.Utils.Collections;
using log4net;
using PostSharp.Laos;

namespace DelftTools.Utils.Aop.NotifyCollectionChange
{
    /// <summary>
    /// Implementation of <see cref="OnMethodBoundaryAspect"/> that raises the 
    /// <see cref="INotifyCollectionChange.CollectionChanged"/> event when a property set
    /// accessor completes successfully.
    /// </summary>
    [Serializable]
    public class NotifyCollectionChangeAspect : OnMethodBoundaryAspect
    {
        private static readonly ILog log = LogManager.GetLogger(typeof (NotifyCollectionChangeAspect));

        private readonly string propertyName;

        private readonly Dictionary<string, INotifyCollectionChange> notifiablePropertyValues = null;

        private IFireCollectionChange implementation;

        private object newValue;
        private object oldValue;

        [NonSerialized] private MethodExecutionEventArgs eventArgs;

        private bool logging;

        /// <summary>
        /// Initializes a new <see cref="NotifyCollectionChangeAspect"/>.
        /// </summary>
        /// <param name="propertyName">Name of the property to which this set accessor belong.</param>
        /// <param name="parent">Parent <see cref="NotifyCollectionChangeAttribute"/>.</param>
        public NotifyCollectionChangeAspect(string propertyName, NotifyCollectionChangeAttribute parent,
                                               bool logging)
        {
            AspectPriority = parent.AspectPriority;
            this.propertyName = propertyName;
            this.logging = logging;
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

        /// <summary>
        /// Executed when the set accessor successfully completes. Raises the 
        /// <see cref="INotifyCollectionChange.CollectionChanged"/> event.
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
                (IFireCollectionChange)
                ((IComposed<INotifyCollectionChange>) eventArgs.Instance).GetImplementation(
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
                //if (oldValue is INotifyCollectionChange)
                //{
                //    implementation.Unsubscribe(oldValue as INotifyCollectionChange);
                //}

                //if (arg is INotifyCollectionChange)
                //{
                //    implementation.Subscribe(arg as INotifyCollectionChange);
                //}

                //todo this should be moved to another onmethodboundary aspect for
                //adding and removing list items.
                //implementation.OnCollectionChanged((NotifyCollectionChangeAction) oldValue, newValue);
                // Raises the CollectionChanged event.
            }
        }

        public override void OnException(MethodExecutionEventArgs eventArgs)
        {
            base.OnException(eventArgs);
            log.Error("Exception occured during property set", eventArgs.Exception);
        }
    }
}