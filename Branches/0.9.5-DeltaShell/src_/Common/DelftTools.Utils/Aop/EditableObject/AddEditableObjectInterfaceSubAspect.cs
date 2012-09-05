using System;
using PostSharp.Laos;

namespace DelftTools.Utils.Aop.EditableObject
{
    /// <summary>
    /// Implementation of <see cref="CompositionAspect"/> that adds the <see cref="IEditableObject"/>
    /// interface to the type to which it is applied.
    /// </summary>
    [Serializable]
    public class AddEditableObjectInterfaceSubAspect : CompositionAspect
    {
        /// <summary>
        /// Called at runtime, creates the implementation of the <see cref="IEditableObject"/> interface.
        /// </summary>
        /// <param name="eventArgs">Execution context.</param>
        /// <returns>A new instance of <see cref="EditableObjectImplementation"/>, which implements
        /// <see cref="IEditableObject"/>.</returns>
        public override object CreateImplementationObject(InstanceBoundLaosEventArgs eventArgs)
        {
            return new EditableObjectImplementation(eventArgs.Instance as IProtectedInterface<IEditableObject>,
                                                    eventArgs.InstanceCredentials);
        }

        /// <summary>
        /// Called at compile-time, gets the interface that should be publicly exposed.
        /// </summary>
        /// <param name="containerType">Type on which the interface will be implemented.</param>
        /// <returns></returns>
        public override Type GetPublicInterface(Type containerType)
        {
            return typeof (IEditableObject);
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