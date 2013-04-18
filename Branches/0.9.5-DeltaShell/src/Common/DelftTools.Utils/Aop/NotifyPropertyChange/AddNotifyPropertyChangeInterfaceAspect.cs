using System;
using log4net;
using PostSharp.Laos;

namespace DelftTools.Utils.Aop.NotifyPropertyChange
{
    /// <summary>
    /// Implementation of <see cref="CompositionAspect"/> that adds the <see cref="INotifyPropertyChange"/>
    /// interface to the type to which it is applied.
    /// </summary>
    [Serializable]
    public class AddNotifyPropertyChangeInterfaceAspect : CompositionAspect
    {
        private static readonly ILog log = LogManager.GetLogger(typeof (AddNotifyPropertyChangeInterfaceAspect));
        private bool logging;

        public AddNotifyPropertyChangeInterfaceAspect(bool logging)
        {
            this.logging = logging;
        }

        public override void RuntimeInitialize(Type type)
        {
        }

        /// <summary>
        /// Called at runtime, creates the implementation of the <see cref="INotifyPropertyChange"/> interface.
        /// 
        /// NOTE: tries to get implementation of aspect from instance, othrewise create a new one
        /// this is critical for objects with multiple constructors (see generated code in Reflector)
        /// </summary>
        /// <param name="eventArgs">Execution context.</param>
        /// <returns>A new instance of <see cref="NotifyPropertyChangeImplementation"/>, which implements
        /// <see cref="INotifyPropertyChange"/>.</returns>
        public override object CreateImplementationObject(InstanceBoundLaosEventArgs eventArgs)
        {
            var instanceCredentials = eventArgs.InstanceCredentials;
            object implementation = ((IComposed<INotifyPropertyChange>) eventArgs.Instance).GetImplementation(instanceCredentials);

            return implementation ??
                   new NotifyPropertyChangeImplementation(eventArgs.Instance, eventArgs.InstanceCredentials, logging);
        }

        /// <summary>
        /// Called at compile-time, gets the interface that should be publicly exposed.
        /// </summary>
        /// <param name="containerType">Type on which the interface will be implemented.</param>
        /// <returns></returns>
        public override Type GetPublicInterface(Type containerType)
        {
            return typeof (INotifyPropertyChange);
        }

        public override Type[] GetProtectedInterfaces(Type containerType)
        {
            return new Type[] {typeof (IFirePropertyChange)};
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