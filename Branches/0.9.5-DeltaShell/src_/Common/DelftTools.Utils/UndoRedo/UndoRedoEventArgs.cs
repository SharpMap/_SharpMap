using System;

namespace DelftTools.Utils.UndoRedo
{
    public class UndoRedoEventArgs : EventArgs
    {
        public IMemento Memento { get; set; }
    }
}