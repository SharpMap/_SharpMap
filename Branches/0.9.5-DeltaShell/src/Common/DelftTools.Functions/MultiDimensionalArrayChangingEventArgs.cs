using System.Collections;
using System.ComponentModel;
using DelftTools.Utils.Collections;

namespace DelftTools.Functions
{
    public class MultiDimensionalArrayChangingEventArgs : CancelEventArgs
    {
        public MultiDimensionalArrayChangingEventArgs(NotifyCollectionChangeAction action, IList items, int index, int oldIndex, int[] stride, int[] shape)
        {
            Stride = stride; // TODO: maybe we should clone it here, will be slow
            Action = action;
            Items = items;
            Shape = shape;
            Index = index;
            OldIndex = oldIndex;
        }

        protected MultiDimensionalArrayChangingEventArgs()
        {
        }

        public NotifyCollectionChangeAction Action;

        /// <summary>
        /// When inserting, this is the position where the item is inserted / being inserted.
        /// </summary>
        public int Index;

        public int OldIndex;

        /// <summary>
        /// Multidimensional index of the changed value
        /// </summary>
        public int[] MultiDimensionalIndex { get { return MultiDimensionalArrayHelper.GetIndex(Index, Stride); } }

        /// <summary>
        /// shape of the items changed/added/removed
        /// </summary>
        public int[] Shape; // not used yet

        /// <summary>
        /// Current Stride of the array. Used to compute index.
        /// </summary>
        public int[] Stride; // for performance reasons we use field here as a property

        /// <summary>
        /// When number of items added (or removed) is more than one.
        /// </summary>
        public IList Items;
    }
}