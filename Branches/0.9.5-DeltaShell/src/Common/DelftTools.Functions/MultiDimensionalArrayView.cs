using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DelftTools.Utils.Collections;
using DelftTools.Utils.Collections.Generic;


namespace DelftTools.Functions
{
    public class MultiDimensionalArrayView : MultiDimensionalArray, IMultiDimensionalArrayView, IDisposable
    {
        private IMultiDimensionalArray parent;

        public MultiDimensionalArrayView()
        {
        }

        public MultiDimensionalArrayView(IMultiDimensionalArray parent, int dimension, int start, int end)
        {
            Parent = parent;

            OffsetStart[dimension] = start;
            OffsetEnd[dimension] = end;

            SelectedIndexes = new int[parent.Rank][];

            if (parent is ICachedMultiDimensionalArray)
            {
                ((ICachedMultiDimensionalArray)parent).Cache(this);
            }
        }

        public MultiDimensionalArrayView(IMultiDimensionalArray parent, int dimension, int[] indexes)
        {
            Parent = parent;

            SelectedIndexes = new int[parent.Rank][];
            SelectedIndexes[dimension] = SelectedIndexes[dimension] != null ? indexes.Concat(SelectedIndexes[dimension]).ToArray() : indexes;

            if (parent is ICachedMultiDimensionalArray)
            {
                ((ICachedMultiDimensionalArray) parent).Cache(this);
            }
        }

        /// <summary>
        /// Create a attached copy of the parent array
        /// </summary>
        /// <param name="parent">Parent array</param>
        /// <param name="start">Start for dimension</param>
        /// <param name="end">End for dimension</param>
        public MultiDimensionalArrayView(IMultiDimensionalArray parent,IList<int> start,IList<int> end)
        {
            if (start.Count != parent.Rank || end.Count != parent.Rank)
            {
                throw new ArgumentOutOfRangeException("Rank of array and number of elements in the arguments are not equal");
            }

            Parent = parent;

            OffsetStart = start;
            OffsetEnd = end;

            SelectedIndexes = new int[parent.Rank][];

            if (parent is ICachedMultiDimensionalArray)
            {
                ((ICachedMultiDimensionalArray)parent).Cache(this);
            }
        }

        public MultiDimensionalArrayView(IMultiDimensionalArray parent)
        {
            Parent = parent;
            SelectedIndexes = new int[parent.Rank][];

            if (parent is ICachedMultiDimensionalArray)
            {
                ((ICachedMultiDimensionalArray)parent).Cache(this);
            }
        }

        #region IMultiDimensionalArrayRangeView Members
        
        // TODO: shouldn't OffsetStart, OffsetEnd be IEventedList<>?
        
        public IList<int> OffsetStart { get; set; }

        public IList<int> OffsetEnd { get; set; }

        /// <summary>
        /// Defines which indexes should be selected for each dimension? Is null if all indexes are to be selected
        /// </summary>
        public int[][] SelectedIndexes { get; set; }

        public override object Clone()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// The array of which this array is 'derived'
        /// </summary>
        public IMultiDimensionalArray Parent
        {
            get { return parent; }
            set
            {
                parent = value;
                var parentRank = parent.Rank;
                OffsetStart = Enumerable.Repeat(int.MinValue, parentRank).ToArray();
                OffsetEnd = Enumerable.Repeat(int.MaxValue, parentRank).ToArray();

                if (reduce != null)
                {
                    reduce.CollectionChanged -= ReduceCollectionChanged;
                }

                reduce = new EventedList<bool>();
                
                for (int i=0;i<parentRank ;i++)
                {
                    reduce.Add(false);
                }

                Reduce = reduce; //updates rank
                reduce.CollectionChanged += ReduceCollectionChanged;

                SelectedIndexes = new int[parentRank][];
            }
        }

        #endregion

        /// <summary>
        /// Listens for changes in reduce array and throws exception if reduction is 
        /// not possible for the given dimension
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ReduceCollectionChanged(object sender, NotifyCollectionChangingEventArgs e)
        {
            UpdateRank();

            //check whether reduced dimensions are bound by offsets
            for (int i = 0; i < parent.Rank; i++)
            {
                if (Reduce[i])
                {
                    bool isLimitedToOneIndex = OffsetEnd[i] == OffsetStart[i] || (SelectedIndexes[i] != null && SelectedIndexes[i].Length <= 1);
                    if (!isLimitedToOneIndex)
                        throw new InvalidOperationException("Reduction not possible because dimension " + i +
                                                            " is not bound to a single index");
                }
            }
        }

        private void UpdateRank()
        {
            rank = Reduce.Count(r => r == false); //the sum of non reduced dimensions
        }

        public override int Add(object value)
        {
            return parent.Add(value);
        }

        public override void Remove(object value)
        {
            RemoveAt(IndexOf(value));
        }

        public override void RemoveAt(int index)
        {
            RemoveAt(0,index);
        }

        public override void RemoveAt(int dimension, int index)
        {
            if (Rank != 1)
                throw new NotImplementedException("Should redirect call to parent with proper index");

            parent.RemoveAt(0, GetIndexInParent(new[] {index})[0]);
            
            OffsetEnd[0]--;
        }
        
        public override void RemoveAt(int dimension, int index, int length)
        {
            throw new NotImplementedException("Should redirect call to parent with proper index");
        }

        public override void InsertAt(int dimension, int index)
        {
            if (Rank != 1)
                throw new NotImplementedException("Should redirect call to parent with proper index");

            //the implementation is a bit ambiguous, since we cannot 

            if (index == Count) //add new value at end of array
            {
                var parentIndex = GetIndexInParent(new []{index - 1})[0] + 1;
                parent.InsertAt(0, parentIndex);
            }
            else
            {
                parent.InsertAt(0, GetIndexInParent(new[] {index})[0]);
            }
        }

        public override void InsertAt(int dimension, int index, int length)
        {
            throw new NotImplementedException("Should redirect call to parent with proper index");
        }

        #region MultidimensionalArray overrides

        
        public override int[] Shape
        {
            get
            {
                if(Parent == null)
                {
                    throw new InvalidOperationException("Parent array must be selected in the array view first");
                }

                // 1. filter indexes
                int[] filteredShape = new int[Parent.Rank];

                if (SelectedIndexes == null)
                {
                    filteredShape = Parent.Shape.ToArray();
                }

                for (int i = 0; i < parent.Rank; i++)
                {
                    filteredShape[i] = SelectedIndexes[i] != null ? NumberOfValidIndices(i, SelectedIndexes[i]) : Parent.Shape[i];
                }
                
                if (MultiDimensionalArrayHelper.GetTotalLength(filteredShape) == 0) //empty because of selected indices!
                    return new []{0}; //empty selection, so return empty shape (skip below steps)

                // 2. create a new shape with reduced dimensions
                var shape = new List<int>();
                for (int i = 0; i < Parent.Rank; i++)
                {
                    //skip reduced dimension
                    if (Reduce[i])
                    {
                        continue;
                    }

                    //take shape of parent and modify by offsets
                    int dimensionLength = filteredShape[i];
                    if (OffsetStart[i] != int.MinValue)
                    {
                        dimensionLength -= OffsetStart[i];
                    }

                    if (OffsetEnd[i] != int.MaxValue)
                    {
                        dimensionLength -= ((filteredShape[i] - 1) - OffsetEnd[i]);
                    }

                    shape.Add(dimensionLength);
                }

                return shape.ToArray();
            }
        }

        //Returns the number of indices that lie in the valid range for the parent's shape.
        private int NumberOfValidIndices(int dimension, int[] indicesForDimension)
        {
            int numberOfValidIndices = 0;

            foreach (int index in indicesForDimension)
            { 
                if (index >= 0 && index < parent.Shape[dimension])
                    numberOfValidIndices++;
            }

            return numberOfValidIndices;
        }

        public override int Count
        {
            get { return MultiDimensionalArrayHelper.GetTotalLength(Shape); }
        }

        public override int[] Stride
        {
            get { return MultiDimensionalArrayHelper.GetStride(Shape); }
        }

        int[] IMultiDimensionalArray.Stride
        {
            get { return MultiDimensionalArrayHelper.GetStride(Shape); }
        }

        public override void Resize(params int[] newShape)
        {
            //throw not supported exception.
        }

        /// <summary>
        /// Returns index to parent. For performance reasons does not check upperbounds
        /// </summary>
        /// <param name="index">Index in the child array</param>
        /// <returns>Index in the parent array</returns>
        private int[] GetIndexInParent(params int[] index)
        {
            var childIndexIndex = 0;
            var parentRank = parent.Rank;
            var parentIndex = new int[parentRank];

            //var shape = Shape;

            for (int i = 0; i < parentRank; i++)
            {
                if (Reduce[i])
                {
                    if (OffsetStart[i] != int.MinValue)
                    {
                        parentIndex[i] = OffsetStart[i];
                    }
                    else
                    {
                        parentIndex[i] = SelectedIndexes[i][0];
                    }
                    continue;
                }

                if (SelectedIndexes[i] != null)
                {
                    // transform filtered indexes into parent array indexes
                    //
                    // 0 1 2 3 4 5 6 7 8 9  - parent array indexes
                    //   * *     *
                    //   0 1     2          - filtered indexes, index[]

                    if (OffsetStart[i] != int.MinValue || OffsetEnd[i] != int.MaxValue)
                    {
                        throw new InvalidOperationException("Can't use dimension index filters and offsets at the same time");
                    }

                    parentIndex[i] = SelectedIndexes[i][index[childIndexIndex]]; // select index only within filtered indexes
                }
                else
                {
                    // transform index into parent index using range
                    //
                    // 0 1 2 3 4 5 6 7 8 9  - parent array indexes
                    //  [* * * *]
                    //   0 1 2 3            - selected range, index[]

                    //TODO: find some better names for all the indexes
                    int dimensionIndex = index[childIndexIndex];

                    //if (dimensionIndex >= shape[childIndexIndex]) 
                    //{
                    //    //very expensive check..to throw an exception which will be thrown anyway?
                    //    throw new IndexOutOfRangeException();
                    //}

                    if (OffsetStart[i] != int.MinValue)
                    {
                        parentIndex[i] = dimensionIndex + OffsetStart[i];
                    }
                    else
                    {
                        parentIndex[i] = dimensionIndex;
                    }
                }

                childIndexIndex++; //we processed one dimension of our childindex
            }

            return parentIndex;
        }

        public override object this[params int[] index]
        {
            get
            {
                if(index.Length == 1 && Rank != 1) // use 1D access
                {
                    int[] stride = MultiDimensionalArrayHelper.GetStride(Shape);
                    return parent[GetIndexInParent(MultiDimensionalArrayHelper.GetIndex(index[0], stride))];
                }

                return parent[GetIndexInParent(index)];
            }
            set
            {
                if (index.Length == 1 && Rank != 1) // use 1D access
                {
                    int[] stride = MultiDimensionalArrayHelper.GetStride(Shape);
                    parent[GetIndexInParent(MultiDimensionalArrayHelper.GetIndex(index[0], stride))] = value;
                    return;
                }

                parent[GetIndexInParent(index)] = value;
            }
        }

        object IList.this[int index]
        {
            get { return this[index]; }
            set { this[index] = value; }
        }

#if MONO
        object IMultiDimensionalArray.this[int index]
        {
            get { return this[index]; }
            set { this[index] = value; }
        }
#endif

        private IEventedList<bool> reduce;
        public IEventedList<bool> Reduce
        {
            get { return reduce; }
            protected set
            {
                reduce = value;
                UpdateRank();
            }
        }

        private int rank;
        public override int Rank
        {
            get { return rank; }
        }
        #endregion

        public void Dispose()
        {
            if (parent is ICachedMultiDimensionalArray)
            {
                ((ICachedMultiDimensionalArray) parent).Free(this);
            }
        }

        public override IEnumerator GetEnumerator()
        {
            return new MultiDimensionalArrayEnumerator(this);
        }

        public override void CopyTo(Array array, int index)
        {
            //Cannot use base copy because this uses the values array directly. This array is unfilled in a view.
            IEnumerator enumerator = GetEnumerator();
            enumerator.MoveNext();
            
            for (int i = index; i < array.Length; i++)
            {
                array.SetValue(enumerator.Current, i);
                enumerator.MoveNext();
            }
        }

        public override bool Contains(object value)
        {
            foreach (var value2 in this)
            {
                if (value != null && value.Equals(value2))
                {
                    return true;
                }
            }
            return false;
        }

        public override int IndexOf(object value)
        {
            //todo: do some algebra to find the index..
            int index = 0;
            foreach (object o in this)
            {
                if (o.Equals(value))
                {
                    return index;
                }
                index++;
            }
            return -1;
        }

    }
}