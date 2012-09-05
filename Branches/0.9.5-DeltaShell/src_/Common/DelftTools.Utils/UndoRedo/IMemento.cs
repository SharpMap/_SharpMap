namespace DelftTools.Utils.UndoRedo
{
    /// <summary>
    /// Represents state and logic of the single Undo/Redo operation
    /// </summary>
    public interface IMemento
    {
        /// <summary>
        /// Restores state contained in memento and returns memento to get back.
        /// </summary>
        /// <returns></returns>
        IMemento Restore();
    }
}