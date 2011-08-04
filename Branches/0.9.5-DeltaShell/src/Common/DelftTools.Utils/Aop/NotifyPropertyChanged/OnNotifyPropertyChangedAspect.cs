using System;
using System.ComponentModel;
using System.Reflection;
using log4net;
using PostSharp.Laos;

namespace DelftTools.Utils.Aop.NotifyPropertyChanged
{
    /// <summary>
    /// Implementation of <see cref="OnMethodBoundaryAspect"/> that raises the 
    /// <see cref="INotifyPropertyChanged.PropertyChanged"/> event when a property set
    /// accessor completes successfully.
    /// </summary>
    [Serializable]
    internal class OnNotifyPropertyChangedAspect : OnMethodBoundaryAspect
    {
        private static readonly ILog log = LogManager.GetLogger(typeof (OnNotifyPropertyChangedAspect));

        private readonly string propertyName;

        private IFirePropertyChanged implementation;

        private bool logging;

        /// <summary>
        /// Initializes a new <see cref="OnNotifyPropertyChangedAspect"/>.
        /// </summary>
        /// <param name="propertyName">Name of the property to which this set accessor belong.</param>
        /// <param name="parent">Parent <see cref="NotifyPropertyChangedAttribute"/>.</param>
        public OnNotifyPropertyChangedAspect(string propertyName, NotifyPropertyChangedAttribute parent, bool logging)
        {
            AspectPriority = parent.AspectPriority;
            this.propertyName = propertyName;
            this.logging = logging;
        }

        public override void RuntimeInitialize(MethodBase method)
        {
        }

        /// <summary>
        /// Executed when the set accessor successfully completes. Raises the 
        /// <see cref="INotifyPropertyChanged.PropertyChanged"/> event.
        /// </summary>
        /// <param name="eventArgs">Event arguments with information about the 
        /// current execution context.</param>
        public override void OnSuccess(MethodExecutionEventArgs eventArgs)
        {
            implementation =
                (IFirePropertyChanged)
                ((IComposed<INotifyPropertyChanged>) eventArgs.Instance).GetImplementation(eventArgs.InstanceCredentials);

            implementation.OnPropertyChanged(propertyName); // Raises the PropertyChanged event.
        }

        public override void OnException(MethodExecutionEventArgs eventArgs)
        {
            base.OnException(eventArgs);
            log.Error("Exception occured during property set", eventArgs.Exception);
        }
    }
}