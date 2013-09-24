using System;
using System.Collections.Generic;

namespace DelftTools.Utils.UndoRedo
{
    /// <summary>
    /// Used by <see cref="IUndoRedoManager"/> implementations to track changes occuring in <see cref="Observable"/>
    /// Transforms changes in the <see cref="Observable"/> into mementos and adds them using <see cref="AddNewMementoCallback"/> action.
    /// </summary>
    public interface IUndoRedoChangeTracker : IDisposable
    {
        object Observable { get; set; }

        bool TrackChanges { get; set; }

        void OnBeforeUndo(IMemento memento);

        void OnAfterUndo(IMemento memento);

        void OnBeforeRedo(IMemento memento);

        void OnAfterRedo(IMemento memento);

        void OnBeforeRemoveUndo(IMemento memento);

        void OnBeforeRemoveRedo(IMemento memento);

        Action<IMemento> AddNewMementoCallback { get; set; }

        IList<Type> ExcludedTypes { get; }
    }
}