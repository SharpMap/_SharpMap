using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DelftTools.Utils.Collections;
using DelftTools.Utils.Collections.Generic;


namespace DelftTools.Functions
{
    [Serializable]
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

        public IEventedList<bool> Reduce { get; set; }

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
                OffsetStart = Enumerable.Repeat(int.MinValue, parent.Rank).ToArray();
                OffsetEnd = Enumerable.Repeat(int.MaxValue, parent.Rank).ToArray();

                if (Reduce != null)
                {
                    Reduce.CollectionChanged -= Reduce_CollectionChanged;
                }

                Reduce = new EventedList<bool>();
                
                for (int i=0;i<parent.Rank ;i++)
                {
                    Reduce.Add(false);
                }

                Reduce.CollectionChanged += Reduce_CollectionChanged;

                SelectedIndexes = new int[parent.Rank][];
            }
        }

        #endregion

        /// <summary>
        /// Listens for changes in reduce array and throws exception if reduction is 
        /// not possible for the given dimension
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Reduce_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            //check whether reduced dimensions are bound by offsets
            for (int i = 0; i < parent.Rank; i++)
            {
                if (Reduce[i])
                {
                    bool isLimitedToOneIndex = OffsetEnd[i] == OffsetStart[i] || (SelectedIndexes[i] != null && SelectedIndexes[i].Length == 1);
                    if (!isLimitedToOneIndex)
                        throw new InvalidOperationException("Reduction not possible because dimension " + i +
                                                            " is not bound to a single index");
                }
            }
        }

        public override int Add(object value)
        {
            return parent.Add(value);
        }

        public override void Remove(object value)
        {
            throw new NotImplementedException("Should redirect call to parent with proper index");
        }

        public override void RemoveAt(int index)
        {
            RemoveAt(0,index);
        }

        public override void RemoveAt(int dimension, int index)
        {
            if (Rank != 1)
                throw new NotImplementedException("Should redirect call to parent with proper index");
            parent.RemoveAt(0,GetIndexInParent(new[]{index})[0]);
            
            OffsetEnd[0]--;
        }
        
        public override void RemoveAt(int dimension, int index, int length)
        {
            throw new NotImplementedException("Should redirect call to parent with proper index");
        }

        public override void InsertAt(int dimension, int index)
        {
            throw new NotImplementedException("Should redirect call to parent with proper index");
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
                    filteredShape[i] = SelectedIndexes[i] != null ? SelectedIndexes[i].Length : Parent.Shape[i];
                }

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
        /// Checks for upperboundary and returns index to parent. 
        /// </summary>
        /// <param name="index">Index in the child array</param>
        /// <returns>Index in the parent array</returns>
        public int[] GetIndexInParent(params int[] index)
        {
            int childIndexIndex = 0;
            int[] parentIndex = new int[Parent.Rank];

            for (int i = 0; i < parent.Rank; i++)
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

                    parentIndex[i] = SelectedIndexes[i][index[i]]; // select index only within filtered indexes
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
                    if (dimensionIndex >= Shape[childIndexIndex])
                    {
                        throw new IndexOutOfRangeException();
                    }

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

        public override int Rank
        {
            get
            {
                return Reduce.Count(r => r == false); //the sum of non reduced dimensions
            }
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
            if(Rank == 1)
            {
                foreach (var value2 in this)
                {
                    if (value != null && value.Equals(value2))
                    {
                        return true;
                    }
                }

            }

            throw new NotImplementedException();
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