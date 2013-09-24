using System;
using System.Collections.Generic;
using DelftTools.Utils.Aop;

namespace DelftTools.Utils.UndoRedo
{
    public class UndoRedoManager : IUndoRedoManager
    {
        private readonly Stack<IMemento> undoStack = new Stack<IMemento>();
        private readonly Stack<IMemento> redoStack = new Stack<IMemento>();

        public event EventHandler ObservableChanged;
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
                if(changeTracker != null)
                {
                    changeTracker.Dispose();
                }
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

                if (ObservableChanged != null)
                {
                    ObservableChanged(this, EventArgs.Empty);
                }
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

            changeTracker.AddNewMementoCallback = AddRedo;

            if (BeforeUndo != null)
            {
                BeforeUndo(this, new UndoRedoEventArgs { Memento = memento });
            }

            changeTracker.OnBeforeUndo(memento);
            
            try
            {
                EditActionSettings.Disabled = true;
                memento.Restore();
            }
            finally
            {
                EditActionSettings.Disabled = false;
            }

            changeTracker.OnAfterUndo(memento);
            
            if (AfterUndo != null)
            {
                AfterUndo(this, new UndoRedoEventArgs { Memento = memento });
            }

            changeTracker.AddNewMementoCallback = AddUndo;
        }

        public void Redo()
        {
            if (!CanRedo)
            {
                throw new InvalidOperationException("Redo stack is empty");
            }

            var memento = redoStack.Pop();

            changeTracker.AddNewMementoCallback = AddUndoForRedo;

            if (BeforeRedo != null)
            {
                BeforeRedo(this, new UndoRedoEventArgs { Memento = memento });
            }

            changeTracker.OnBeforeRedo(memento);

            try
            {
                EditActionSettings.Disabled = true;
                memento.Restore();
            }
            finally
            {
                EditActionSettings.Disabled = false;
            }

            changeTracker.OnAfterRedo(memento);
            
            if (AfterRedo != null)
            {
                AfterRedo(this, new UndoRedoEventArgs { Memento = memento });
            }

            changeTracker.AddNewMementoCallback = AddUndo;
        }

        public bool CanUndo { get { return undoStack.Count > 0; } }

        public bool CanRedo { get { return redoStack.Count > 0; } }

        public event EventHandler<UndoRedoEventArgs> BeforeUndo;
        
        public event EventHandler<UndoRedoEventArgs> AfterUndo;

        public event EventHandler<UndoRedoEventArgs> BeforeRedo;

        public event EventHandler<UndoRedoEventArgs> AfterRedo;

        public event EventHandler<UndoRedoEventArgs> AfterDo;
        
        public event EventHandler<UndoRedoEventArgs> AfterRemoveRedo;

        public event EventHandler<UndoRedoEventArgs> AfterRemoveUndo;

        public void AddUndo(IMemento memento)
        {
            ClearRedoStack();

            undoStack.Push(memento);
           
            if(AfterDo != null)
            {
                AfterDo(this, new UndoRedoEventArgs { Memento = memento });
            }
        }

        private void AddUndoForRedo(IMemento memento)
        {
            undoStack.Push(memento);
        }

        public void AddRedo(IMemento memento)
        {
            redoStack.Push(memento);
        }

        public void ClearRedoStack()
        {
            while(redoStack.Count != 0)
            {
                var memento = redoStack.Pop();
                
                changeTracker.OnBeforeRemoveRedo(memento);
                
                if (AfterRemoveRedo != null)
                {
                    AfterRemoveRedo(this, new UndoRedoEventArgs { Memento = memento });
                }
            }
        }

        public void ClearUndoStack()
        {
            while (undoStack.Count != 0)
            {
                var memento = undoStack.Pop();

                changeTracker.OnBeforeRemoveUndo(memento);
                
                if (AfterRemoveUndo != null)
                {
                    AfterRemoveUndo(this, new UndoRedoEventArgs { Memento = memento });
                }
            }
        }

        public void Dispose()
        {
            Observable = null;
            changeTracker.Dispose();
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