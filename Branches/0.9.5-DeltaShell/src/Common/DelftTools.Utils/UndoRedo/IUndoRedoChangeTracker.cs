using System;
using System.Collections.Generic;

namespace DelftTools.Utils.UndoRedo
{
    /// <summary>
    /// Used by <see cref="IUndoRedoManager"/> implementations to track changes occuring in <see cref="Observable"/>
    /// Transforms changes in the <see cref="Observable"/> into mementos and adds them using <see cref="AddNewMementoCallback"/> action.
    /// </summary>
    public interface IUndoRedoChangeTracker 
    {
        object Observable { get; set; }

        bool TrackChanges { get; set; }

        void OnBeforeUndo(IMemento memento);
        
        void OnBeforeRedo(IMemento memento);
        
        void OnBeforeRemove(IMemento memento);

        Action<IMemento> AddNewMementoCallback { get; set; }
        
        IList<Type> ExcludedTypes { get; }
    }
}