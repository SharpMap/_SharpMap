using System;
using System.Collections;
using System.ComponentModel;
using DelftTools.Utils.Collections;
using DelftTools.Utils.Data;

namespace DelftTools.Functions
{
    ///<summary>
    /// Multi-dimensional array.
    /// 
    /// <remarks>
    /// When values are accessed using <see cref="IList"/> index of the element is computed in the following way:
    /// <c>index = sum { length(dimension(i)) * indexOf(dimension(i)) }</c>
    /// </remarks>
    ///</summary>
    /// TODO: split INotifyCollectionChanged into INotifyMultiDimensionalArrayChanged and INotifyCollectionChanged
    public interface IMultiDimensionalArray : IList, ICloneable, INotifyCollectionChanged, INotifyPropertyChanged, IUnique<long>
    {
        /// <summary>
        /// Changes lenghts of the dimensions.
        /// </summary>
        /// <returns></returns>
        void Resize(params int[] newShape);

        /// <summary>
        /// Gets value at the specified dimension index.
        /// </summary>
        object this[params int[] index] { get; set; }

        /// <summary>
        /// Total number of elements in array.
        /// </summary>
        new int Count { get; }

        new void Clear();

        new void RemoveAt(int index);

        /// <summary>
        /// Default value to be used when array is resized.
        /// </summary>
        object DefaultValue { get; set; }

        /// <summary>
        /// Gets lengths of the dimensions. Use Count to check total number of elements.
        /// </summary>
        /// <returns></returns>
        int[] Shape { get; }

        int[] Stride { get; }

        /// <summary>
        /// Gets rank (number of dimensions).
        /// </summary>
        int Rank { get; }

        /// <summary>
        /// Selects subset of an original array. Keeps reference to the parent array.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns>View of the original array where reference to the parent array is in <see cref="IMultiDimensionalArrayView.Parent"/></returns>
        IMultiDimensionalArrayView Select(int[] start, int[] end);

        /// <summary>
        /// Allows to select only specific part of the parent array.
        /// </summary>
        /// <param name="dimension"></param>
        /// <param name="start">Start offset in the parent or <seealso cref="int.MinValue"/> if it is equal to the parent</param>
        /// <param name="end">Start offset in the parent or <seealso cref="int.MaxValue"/> if it is equal to the parent</param>
        /// <returns>View of the original array where reference to the parent array is in <see cref="IMultiDimensionalArrayView.Parent"/></returns>
        IMultiDimensionalArrayView Select(int dimension, int start, int end);

        /// <summary>
        /// Selects subset of an original array based on dimension and indexes for that dimenion. Keeps reference to the parent array.
        /// </summary>
        /// <param name="dimension">Dimension to filter</param>
        /// <param name="indexes">Indexes which will be selected</param>
        /// <returns>View of the original array where reference to the parent array is in <see cref="IMultiDimensionalArrayView.Parent"/></returns>
        IMultiDimensionalArrayView Select(int dimension, int[] indexes);

        /// <summary>
        /// Removes one slice on a given dimension, <see cref="index"/>.
        /// </summary>
        /// <param name="dimension">Dimension index</param>
        /// <param name="index">Starting index</param>
        void RemoveAt(int dimension, int index);

        /// <summary>
        /// Removes <see cref="length"/> slices on a given dimension, <see cref="index"/>.
        /// </summary>
        /// <param name="dimension">Dimension index</param>
        /// <param name="index">Starting index</param>
        /// <param name="length">Number of indexes to remove</param>
        void RemoveAt(int dimension, int index, int length);

        /// <summary>
        /// Inserts one slice on a given dimension, at the <see cref="index"/>.
        /// </summary>
        /// <param name="dimension">Dimension index</param>
        /// <param name="index">Index on the specified dimension</param>
        void InsertAt(int dimension, int index);

        /// <summary>
        /// Inserts <see cref="length"/> slices on a given dimension starting at <see cref="index"/>.
        /// </summary>
        /// <param name="dimension">Dimension index</param>
        /// <param name="index">Starting index</param>
        /// <param name="length">Number of indexes to remove</param>
        void InsertAt(int dimension, int index, int length);

        /// <summary>
        /// Moves <see cref="length"/> elements at the given <see cref="dimension"/> and <see cref="index"/> to a new index: <see cref="newIndex"/>
        /// </summary>
        /// <param name="dimension"></param>
        /// <param name="index"></param>
        /// <param name="length"></param>
        /// <param name="newIndex"></param>
        void Move(int dimension, int index, int length, int newIndex);

        /// <summary>
        /// CollectionChanging and CollectionChanged events are not fired when this property is false. 
        /// Default value is true - events are fired.
        /// </summary>
        bool FireEvents { get; set; }

        /// <summary>
        /// Adds elements to the end of array.
        /// </summary>
        /// <param name="values"></param>
        void AddRange(IEnumerable values);

        /// <summary>
        /// Gets the maximal value in the array or throws an exception if the array type is not support
        /// </summary>
        object MaxValue { get; }

        /// <summary>
        /// Gets the minimal value in the array or throws an exception if the array type is not support
        /// </summary>
        object MinValue { get; }

        IVariable Owner { get; set; } // TODO: remove it, code smell!!!! BUG
    }
}