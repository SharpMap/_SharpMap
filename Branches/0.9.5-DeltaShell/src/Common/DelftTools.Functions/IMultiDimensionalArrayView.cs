using System.Collections.Generic;
using DelftTools.Utils.Collections.Generic;

namespace DelftTools.Functions
{
    public interface IMultiDimensionalArrayView : IMultiDimensionalArray
    {
        /// <summary>
        /// Parent array of the current array.
        /// </summary>
        IMultiDimensionalArray Parent { get; set; }

        /// <summary>
        /// Start offset in the parent array for all dimensions.
        /// </summary>
        IList<int> OffsetStart { get; set; }

        /// <summary>
        /// End offset in the parent array for all dimensions.
        /// </summary>
        IList<int> OffsetEnd { get; set; }

        /// <summary>
        /// Gets true if dimension is reduced, array of flags for all dimensions.
        /// </summary>
        IEventedList<bool> Reduce { get; set; }

        /// <summary>
        /// Indexes for each dimension. Provides a selection of the Parent for the given dimension.
        /// </summary>
        int[][] SelectedIndexes { get; set; }
    }
}