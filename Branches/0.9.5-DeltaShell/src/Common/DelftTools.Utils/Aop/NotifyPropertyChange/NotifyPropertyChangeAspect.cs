using System;
using System.ComponentModel;
using System.Reflection;
using log4net;
using PostSharp.Laos;

namespace DelftTools.Utils.Aop.NotifyPropertyChange
{
    /// <summary>
    /// Implementation of <see cref="OnMethodBoundaryAspect"/> that raises the 
    /// <see cref="INotifyPropertyChanging.PropertyChanging" /> and <see cref="INotifyPropertyChanged.PropertyChanged"/> events 
    /// when a property set accessor completes successfully.
    /// </summary>
    [Serializable]
    public class NotifyPropertyChangeAspect : OnMethodBoundaryAspect
    {
        private static readonly ILog log = LogManager.GetLogger(typeof (NotifyPropertyChangeAspect));

        private readonly string propertyName;

        private NotifyPropertyChangeImplementation implementation;

        private bool logging;

        /// <summary>
        /// Initializes a new <see cref="NotifyPropertyChangeAspect"/>.
        /// </summary>
        /// <param name="propertyName">Name of the property to which this set accessor belong.</param>
        /// <param name="parent">Parent <see cref="NotifyPropertyChangeAttribute"/>.</param>
        public NotifyPropertyChangeAspect(string propertyName, NotifyPropertyChangeAttribute parent, bool logging)
        {
            AspectPriority = parent.AspectPriority;
            this.propertyName = propertyName;
            this.logging = logging;
        }

        public override void RuntimeInitialize(MethodBase method)
        {
        }

        public override void OnEntry(MethodExecutionEventArgs eventArgs)
        {
/*
            if (implementation == null)
            {
*/
                implementation =
                    (NotifyPropertyChangeImplementation)
                    ((IComposed<INotifyPropertyChange>)eventArgs.Instance).GetImplementation(
                        eventArgs.InstanceCredentials);
/*
            }
*/

            implementation.FirePropertyChanging(implementation.instance, new PropertyChangingEventArgs(propertyName));
        }

        public override void OnExit(MethodExecutionEventArgs eventArgs)
        {
            implementation = null;
        }

        /// <summary>
        /// Executed when the set accessor successfully completes. Raises the 
        /// <see cref="INotifyPropertyChanged.PropertyChanged"/> event.
        /// </summary>
        /// <param name="eventArgs">Event arguments with information about the 
        /// current execution context.</param>
        public override void OnSuccess(MethodExecutionEventArgs eventArgs)
        {
            if (implementation != null)
            {
                implementation.FirePropertyChanged(implementation.instance, new PropertyChangedEventArgs(propertyName));
            }
        }

        public override void OnException(MethodExecutionEventArgs eventArgs)
        {
            base.OnException(eventArgs);
            log.Error("Exception occured during property set", eventArgs.Exception);
            implementation = null;
        }
    }
}