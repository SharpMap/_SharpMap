using System;
using PostSharp.Extensibility;
using PostSharp.Laos;

namespace DelftTools.Utils.Aop.EditableObject
{
    [MulticastAttributeUsage(MulticastTargets.Class)]
    public sealed class EditableObjectAttribute : CompoundAspect
    {
        /// <summary>
        /// Method called at compile time to get individual aspects required by the current compound
        /// aspect.
        /// </summary>
        /// <param name="element">Metadata element (<see cref="Type"/> in our case) to which
        /// the current custom attribute instance is applied.</param>
        /// <param name="collection">Collection of aspects to which individual aspects should be
        /// added.</param>
        public override void ProvideAspects(object element, LaosReflectionAspectCollection collection)
        {
            // Get the target type.
            var targetType = (Type) element;

            // On the type, add a Composition aspect to implement the INotifyPropertyChanged interface.
            collection.AddAspect(targetType, new AddEditableObjectInterfaceSubAspect());
        }

        #region Nested type: AddEditableObjectInterfaceSubAspect

        #endregion
    }
}