using System.Collections.Generic;

namespace DelftTools.Utils.UndoRedo
{
    /// <summary>
    /// Represents state and logic of the single Undo/Redo operation
    /// </summary>
    public interface IMemento
    {
        /// <summary>
        /// Restores changes captured in memento.
        /// </summary>
        void Restore();

        /// <summary>
        /// Nested mementos.
        /// </summary>
        IList<IMemento> ChildMementos { get; }

        /// <summary>
        /// Gets the current active (eg property changed or collection changed) memento (in case of compound mementos)
        /// </summary>
        IMemento CurrentSimpleMemento { get; }

        /// <summary>
        /// Gets stack trace where this memento was created.
        /// </summary>
        string StackTrace { get; }
    }
}