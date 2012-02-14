using System;
using DelftTools.Utils.Collections;
using log4net;
using PostSharp.Laos;

namespace DelftTools.Utils.Aop.NotifyCollectionChange
{
    /// <summary>
    /// Implementation of <see cref="CompositionAspect"/> that adds the <see cref="INotifyCollectionChange"/>
    /// interface to the type to which it is applied.
    /// </summary>
    [Serializable]
    public class AddNotifyCollectionChangeInterfaceAspect : CompositionAspect
    {
        private static readonly ILog log = LogManager.GetLogger(typeof (AddNotifyCollectionChangeInterfaceAspect));
        private bool logging;

        public AddNotifyCollectionChangeInterfaceAspect(bool logging)
        {
            this.logging = logging;
        }

        /// <summary>
        /// Called at runtime, creates the implementation of the <see cref="INotifyCollectionChange"/> interface.
        /// 
        /// NOTE: tries to get implementation of aspect from instance, othrewise create a new one
        /// this is critical for objects with multiple constructors (see generated code in Reflector)
        /// </summary>
        /// <param name="eventArgs">Execution context.</param>
        /// <returns>A new instance of <see cref="NotifyCollectionChangeImplementation"/>, which implements
        /// <see cref="INotifyCollectionChange"/>.</returns>
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
                ((IComposed<INotifyCollectionChange>) eventArgs.Instance).GetImplementation(
                    eventArgs.InstanceCredentials);

            if (implementation != null)
            {
                if (logging)
                {
/*
                    log.DebugFormat(
                        "Instance already has an implementation injected - returning it and updating credentials {0} > ({1})",
                        ((NotifyCollectionChangeImplementation) implementation).InstanceCredentials,
                        InstanceCredentialsConvertor.ToString(credentials));
*/
                }
                ((NotifyCollectionChangeImplementation) implementation).InstanceCredentials =
                    credentials.ToString();
            }

            return implementation ??
                   new NotifyCollectionChangeImplementation(eventArgs.Instance, eventArgs.InstanceCredentials, logging);
        }

        /// <summary>
        /// Called at compile-time, gets the interface that should be publicly exposed.
        /// </summary>
        /// <param name="containerType">Type on which the interface will be implemented.</param>
        /// <returns></returns>
        public override Type GetPublicInterface(Type containerType)
        {
            return typeof (INotifyCollectionChange);
        }

        public override Type[] GetProtectedInterfaces(Type containerType)
        {
            return new Type[] {typeof (IFireCollectionChange)};
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
}