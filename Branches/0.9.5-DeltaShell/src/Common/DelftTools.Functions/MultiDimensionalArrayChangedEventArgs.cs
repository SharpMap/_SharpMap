using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using DelftTools.Utils.Collections;

namespace DelftTools.Functions
{
    public class MultiDimensionalArrayChangedEventArgs : NotifyCollectionChangedEventArgs
    {
        public MultiDimensionalArrayChangedEventArgs(NotifyCollectionChangedAction action, object item, int index, int oldIndex, int[] stride)
        {
            Stride = stride; // TODO: maybe we should clone it here, will be slow
            Action = action;
            Item = item;
            Index = index;
            Length = 1;
            OldIndex = oldIndex;
        }

        /// <summary>
        /// Multidimensional index of the changed value
        /// </summary>
        public int[] MultiDimensionalIndex { get { return MultiDimensionalArrayHelper.GetIndex(Index, Stride); } }

        public int[] MultiDimensionalLength; // not used yet

        /// <summary>
        /// Current Stride of the array. Used to compute index.
        /// </summary>
        public int[] Stride; // for performance reasons we use field here as a property

        protected MultiDimensionalArrayChangedEventArgs()
        {
        }
    }
}