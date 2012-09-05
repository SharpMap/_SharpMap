using System;
using System.Collections;
using System.ComponentModel;

namespace DelftTools.Utils.Collections
{
    /// <summary>
    /// Action for changes to a collection such as add, remove.
    /// </summary>
    public enum NotifyCollectionChangedAction
    {
        Add,
        Remove,
        Replace
    }

    /// <summary>
    /// EventArgs for a collection that include the item and the action.
    /// 
    /// Note: for performance reasons we use fields here instead of properties.
    /// </summary>
    public class NotifyCollectionChangedEventArgs : CancelEventArgs
    {
        public NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, object item, int index, int oldIndex) 
        {
            Action = action;
            Item = item;
            Index = index;
            Length = 1;
            OldIndex = oldIndex;
        }

        public NotifyCollectionChangedEventArgs()
        {
        }

        /// <summary>
        /// Indicate what operation took place such as add, remove etc...
        /// </summary>
        public NotifyCollectionChangedAction Action;

        /// <summary>
        /// The item added, removed or changed (old value when changed).
        /// </summary>
        public object Item;

        /// <summary>
        /// When number of items added (or removed) is more than one.
        /// </summary>
        public IEnumerable Items;

        /// <summary>
        /// When inserting, this is the position where the item is inserted.
        /// </summary>
        public int Index;

        /// <summary>
        /// Previous index (if changed).
        /// </summary>
        public int OldIndex;

        /// <summary>
        /// Default is 1, more than one when more than one value are changed.
        /// </summary>
        public int Length;
    }
}