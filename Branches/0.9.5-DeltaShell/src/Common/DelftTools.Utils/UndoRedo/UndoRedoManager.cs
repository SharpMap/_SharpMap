using System;
using System.Collections.Generic;

namespace DelftTools.Utils.UndoRedo
{
    public class UndoRedoManager : IUndoRedoManager
    {
        private readonly Stack<IMemento> undoStack = new Stack<IMemento>();
        private readonly Stack<IMemento> redoStack = new Stack<IMemento>();
        
        private IUndoRedoChangeTracker changeTracker;
        
        private bool trackChanges = true;

        public UndoRedoManager(object observable)
        {
            // default change tracking strategy
            changeTracker = new EventBasedChangeTracker(AddUndo, observable);

            Observable = observable;
        }

        public IUndoRedoChangeTracker ChangeTracker
        {
            get { return changeTracker; }
            set
            {
                changeTracker = value;
                changeTracker.AddNewMementoCallback = AddUndo;
            }
        }

        public object Observable
        {
            get { return changeTracker.Observable; } 
            set
            {
                // clears previous undo/redo stack
                ClearRedoStack();
                ClearUndoStack();

                changeTracker.Observable = value;
            }
        }

        public bool TrackChanges
        {
            get { return trackChanges; }
            set
            {
                trackChanges = value;
                changeTracker.TrackChanges = value;
            }
        }

        public int MaxUndoLevel { get; set; }

        public IEnumerable<IMemento> UndoStack
        {
            get { return undoStack; }
        }

        public IEnumerable<IMemento> RedoStack
        {
            get { return redoStack; }
        }

        public void Undo()
        {
            if (!CanUndo)
            {
                throw new InvalidOperationException("Undo stack is empty");
            }

            var memento = undoStack.Pop();

            changeTracker.TrackChanges = false;

            changeTracker.OnBeforeUndo(memento);

            if (BeforeUndo != null)
            {
                BeforeUndo(this, new UndoRedoEventArgs { Memento = memento });
            }

            memento = memento.Restore();

            redoStack.Push(memento);

            if (AfterUndo != null)
            {
                AfterUndo(this, new UndoRedoEventArgs { Memento = memento });
            }

            changeTracker.TrackChanges = trackChanges;
        }

        public void Redo()
        {
            if (!CanRedo)
            {
                throw new InvalidOperationException("Redo stack is empty");
            }

            var memento = redoStack.Pop();

            changeTracker.TrackChanges = false;

            changeTracker.OnBeforeRedo(memento);

            if (BeforeRedo != null)
            {
                BeforeRedo(this, new UndoRedoEventArgs { Memento = memento });
            }

            memento = memento.Restore();

            undoStack.Push(memento);

            if (AfterRedo != null)
            {
                AfterRedo(this, new UndoRedoEventArgs { Memento = memento });
            }

            changeTracker.TrackChanges = trackChanges;
        }

        public bool CanUndo { get { return undoStack.Count > 0; } }

        public bool CanRedo { get { return redoStack.Count > 0; } }

        public event EventHandler<UndoRedoEventArgs> BeforeUndo;
        
        public event EventHandler<UndoRedoEventArgs> AfterUndo;

        public event EventHandler<UndoRedoEventArgs> BeforeRedo;

        public event EventHandler<UndoRedoEventArgs> AfterRedo;

        public event EventHandler<UndoRedoEventArgs> AfterDo;
        
        public event EventHandler<UndoRedoEventArgs> AfterRemove;

        public void AddUndo(IMemento memento)
        {
            ClearRedoStack();

            undoStack.Push(memento);
           
            if(AfterDo != null)
            {
                AfterDo(this, new UndoRedoEventArgs { Memento = memento });
            }
        }

        public void ClearRedoStack()
        {
            while(redoStack.Count != 0)
            {
                var memento = redoStack.Pop();
                if(AfterRemove != null)
                {
                    changeTracker.OnBeforeRemove(memento);
                    AfterRemove(this, new UndoRedoEventArgs { Memento = memento });
                }
            }
        }

        public void ClearUndoStack()
        {
            while (undoStack.Count != 0)
            {
                var memento = undoStack.Pop();
                if (AfterRemove != null)
                {
                    changeTracker.OnBeforeRemove(memento);
                    AfterRemove(this, new UndoRedoEventArgs { Memento = memento });
                }
            }
        }
    }

    // TODO: add Windows.Forms timer-based handling of undo/redo in tree view
    // TODO: check performance
    // TODO: test using save/load (NHibernate)
    // TODO: threading
    // TODO: functions
    // TODO: IFileBased?
    // TODO: running model   
}