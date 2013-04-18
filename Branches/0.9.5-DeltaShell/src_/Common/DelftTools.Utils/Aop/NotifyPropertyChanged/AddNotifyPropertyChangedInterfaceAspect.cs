using System;
using System.ComponentModel;
using log4net;
using PostSharp.Laos;

namespace DelftTools.Utils.Aop.NotifyPropertyChanged
{
    /// <summary>
    /// Implementation of <see cref="CompositionAspect"/> that adds the <see cref="INotifyPropertyChanged"/>
    /// interface to the type to which it is applied.
    /// </summary>
    [Serializable]
    internal class AddNotifyPropertyChangedInterfaceAspect : CompositionAspect
    {
        private static readonly ILog log = LogManager.GetLogger(typeof (AddNotifyPropertyChangedInterfaceAspect));
        private bool logging;

        public AddNotifyPropertyChangedInterfaceAspect(bool logging)
        {
            this.logging = logging;
        }

        public override void RuntimeInitialize(Type type)
        {
        }

        /// <summary>
        /// Called at runtime, creates the implementation of the <see cref="INotifyPropertyChanged"/> interface.
        /// 
        /// NOTE: tries to get implementation of aspect from instance, othrewise create a new one
        /// this is critical for objects with multiple constructors (see generated code in Reflector)
        /// </summary>
        /// <param name="eventArgs">Execution context.</param>
        /// <returns>A new instance of <see cref="NotifyPropertyChangedImplementation"/>, which implements
        /// <see cref="INotifyPropertyChanged"/>.</returns>
        public override object CreateImplementationObject(InstanceBoundLaosEventArgs eventArgs)
        {
            var instanceCredentials = eventArgs.InstanceCredentials;
            object implementation = ((IComposed<INotifyPropertyChanged>) eventArgs.Instance).GetImplementation(instanceCredentials);

            return implementation ??
                   new NotifyPropertyChangedImplementation(eventArgs.Instance, eventArgs.InstanceCredentials, logging);
        }

        /// <summary>
        /// Called at compile-time, gets the interface that should be publicly exposed.
        /// </summary>
        /// <param name="containerType">Type on which the interface will be implemented.</param>
        /// <returns></returns>
        public override Type GetPublicInterface(Type containerType)
        {
            return typeof (INotifyPropertyChanged);
        }

        public override Type[] GetProtectedInterfaces(Type containerType)
        {
            return new Type[] {typeof (IFirePropertyChanged)};
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