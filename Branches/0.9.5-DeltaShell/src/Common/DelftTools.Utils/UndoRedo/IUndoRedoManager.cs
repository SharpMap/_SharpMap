using System;
using System.Collections.Generic;

namespace DelftTools.Utils.UndoRedo
{
    public interface IUndoRedoManager : IDisposable
    {
        int MaxUndoLevel { get; set; }

        bool CanUndo { get; }
        bool CanRedo { get; }

        IEnumerable<IMemento> UndoStack { get; }
        IEnumerable<IMemento> RedoStack { get; }

        void Undo();
        void Redo();

        event EventHandler<UndoRedoEventArgs> BeforeUndo;
        event EventHandler<UndoRedoEventArgs> AfterUndo;
        
        event EventHandler<UndoRedoEventArgs> BeforeRedo;
        event EventHandler<UndoRedoEventArgs> AfterRedo;

        event EventHandler<UndoRedoEventArgs> AfterDo;

        event EventHandler<UndoRedoEventArgs> AfterRemoveRedo;

        event EventHandler<UndoRedoEventArgs> AfterRemoveUndo;

        event EventHandler ObservableChanged;

        /// <summary>
        /// Object being monitored by undo / redo manager.
        /// </summary>
        object Observable { get; set; }

        /// <summary>
        /// Enables or disables change tracking.
        /// </summary>
        bool TrackChanges { get; set; }

        /// <summary>
        /// Strategy used to track changes in the observable.
        /// </summary>
        IUndoRedoChangeTracker ChangeTracker { get; set; }
    }
}