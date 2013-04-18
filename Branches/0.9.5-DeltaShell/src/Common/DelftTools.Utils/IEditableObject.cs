using System;

namespace DelftTools.Utils
{
    public interface IEditableObject
    {
        /// <summary>
        /// True if object is being edited (potentially in invalid state).
        /// </summary>
        bool IsEditing { get; }

        /// <summary>
        /// Is set to true if the last edit action was cancelled.
        /// </summary>
        bool EditWasCancelled { get; }

        /// <summary>
        /// Current edit action. 
        /// </summary>
        IEditAction CurrentEditAction { get; }

        /// <summary>
        /// Start editing object with the named action. Object must assign <see cref="action"/> to <see cref="CurrentEditActionName"/> before <see cref="IsEditing"/> is changed.
        /// </summary>
        /// <param name="action"></param>
        void BeginEdit(IEditAction action);

        /// <summary>
        /// Submit changes to the datasource.
        /// </summary>
        void EndEdit();

        /// <summary>
        /// Revert the changes.
        /// </summary>
        void CancelEdit();
    }
}