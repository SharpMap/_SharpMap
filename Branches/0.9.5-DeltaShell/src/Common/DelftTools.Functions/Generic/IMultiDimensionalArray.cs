using System.Collections.Generic;

namespace DelftTools.Functions.Generic
{
    public interface IMultiDimensionalArray<T> : IMultiDimensionalArray, IList<T>
    {
        /// <summary>
        /// Gets value at the specified dimension indexes.
        /// </summary>
        /// <param name="indexes"></param>
        /// <returns></returns>
        new T this[params int[] indexes] { get; set; }
		
#if MONO
		new T this[int index] { get; set; }
#endif

        /// <summary>
        /// Selects subset of an original array. Keeps reference to the parent array.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns>View of the original array where reference to the parent array is in <see cref="IMultiDimensionalArrayView.Parent"/></returns>
        new IMultiDimensionalArrayView<T> Select(int[] start, int[] end);

        /// <summary>
        /// Allows to select only specific part of the parent array.
        /// </summary>
        /// <param name="dimension"></param>
        /// <param name="start">Start offset in the parent or <seealso cref="int.MinValue"/> if it is equal to the parent</param>
        /// <param name="end">Start offset in the parent or <seealso cref="int.MaxValue"/> if it is equal to the parent</param>
        /// <returns>View of the original array where reference to the parent array is in <see cref="IMultiDimensionalArrayView.Parent"/></returns>
        new IMultiDimensionalArrayView<T> Select(int dimension, int start, int end);

        /// <summary>
        /// Selects subset of an original array based on dimension and indexes for that dimenion. Keeps reference to the parent array.
        /// </summary>
        /// <param name="dimension">Dimension to filter</param>
        /// <param name="indexes">Indexes which will be selected</param>
        /// <returns>View of the original array where reference to the parent array is in <see cref="IMultiDimensionalArrayView.Parent"/></returns>
        new IMultiDimensionalArrayView<T> Select(int dimension, int[] indexes);

        /// <summary>
        /// Total number of elements in array.
        /// </summary>
        new int Count { get; }

        new void Clear();

        new void RemoveAt(int index);
    }
}