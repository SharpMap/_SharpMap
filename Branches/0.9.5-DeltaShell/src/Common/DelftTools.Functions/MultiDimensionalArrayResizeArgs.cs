using System;
using System.Collections.Generic;

namespace DelftTools.Functions
{
    /// <summary>
    /// Arguments specifying how the array was resized
    /// </summary>
    /// TODO: merge it with NotifyCollectionChanged
    public class MultiDimensionalArrayResizeArgs : EventArgs
    {
        public IList<MultiDimensionalArrayDimensionChangeAction> Actions { get; set; }

        public int[] OldShape { get; set; }
        public int[] NewShape { get; set; }
    }
}