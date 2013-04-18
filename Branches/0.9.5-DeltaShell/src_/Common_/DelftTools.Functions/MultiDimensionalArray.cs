using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using DelftTools.Utils.Collections;

namespace DelftTools.Functions
{
    [Serializable]
    public class MultiDimensionalArray : IMultiDimensionalArray
    {
        //private static readonly ILog log = LogManager.GetLogger(typeof(MultiDimensionalArray));

        protected IList values;

        private int[] shape;
        private int[] stride;
        private int rank;
        private int count;

        private bool fireEvents = true;

        private object defaultValue;
        
        public MultiDimensionalArray()
            : this(new[] { 0 })
        {
        }

        public MultiDimensionalArray(params int[] shape)
            : this(null, shape)
        {
        }

        public MultiDimensionalArray(object defaultValue, params int[] shape)
            : this(false, false, defaultValue, shape)
        {
        }

        public MultiDimensionalArray(bool isReadOnly, bool isFixedSize, object defaultValue, IMultiDimensionalArray array)
            : this(isReadOnly, isFixedSize, defaultValue, array.Shape)
        {
            int i = 0;
            foreach (object value in array)
            {
                values[i++] = value;
            }
        }

        /// <summary>
        /// Creates a new multidimensional array using provided values as 1D array. 
        /// Values in the provided list will used in a row-major order.
        /// </summary>
        /// <param name="isReadOnly"></param>
        /// <param name="isFixedSize"></param>
        /// <param name="defaultValue"></param>
        /// <param name="values"></param>
        /// <param name="shape"></param>
        public MultiDimensionalArray(bool isReadOnly, bool isFixedSize, object defaultValue, IList values, int[] shape)
        {
            if (values.Count != MultiDimensionalArrayHelper.GetTotalLength(shape))
                throw new ArgumentException("Copy constructor shape does not match values");
            IsReadOnly = isReadOnly;
            IsFixedSize = isFixedSize;

            DefaultValue = defaultValue;

            Shape = (int[])shape.Clone();

            SetValues(CreateClone(values));
        }

        public MultiDimensionalArray(bool isReadOnly, bool isFixedSize, object defaultValue, params int[] shape)
        {
            IsReadOnly = isReadOnly;
            IsFixedSize = isFixedSize;

            DefaultValue = defaultValue;

            Shape = new[] { 0 };

            SetValues(CreateValuesList());

            Resize(shape);
        }

        /// <summary>
        /// Gets or sets owner. For optimization purposes only.
        /// </summary>
        public IVariable Owner { get; set; }

        #region IMultiDimensionalArray Members

        public long Id { get; set; }

        public virtual int Count
        {
            get { return count; }
        }

        public object SyncRoot
        {
            get { return values.SyncRoot; }
        }

        public bool IsSynchronized
        {
            get { return values.IsSynchronized; }
        }

        public bool IsReadOnly { get; set; }
        public bool IsFixedSize { get; set; }

        public object DefaultValue
        {
            get { return defaultValue; } 
            set { defaultValue = value; }
        }

        public virtual int[] Shape
        {
            get { return shape; }
            set
            {
                shape = value;
                count = MultiDimensionalArrayHelper.GetTotalLength(shape);
                stride = MultiDimensionalArrayHelper.GetStride(shape);
                rank = shape.Length;

                singleValueLength = new int[rank];
                for (var i = 0; i < singleValueLength.Length; i++)
                {
                    singleValueLength[i] = 1;
                }
            }
        }

        public virtual int Rank
        {
            get { return rank; }
        }

        public virtual int[] Stride
        {
            get { return stride; }
        }

        object IList.this[int index]
        {
            get { return this[index]; }
            set { this[index] = value; }
        }

        public virtual object this[params int[] index]
        {
            get
            {
                if (index.Length != rank)
                {
                    if (index.Length == 1) // use 1D
                    {
                        return values[index[0]];
                    }

                    throw new ArgumentException("Invalid number of indexes");
                }

                if(index.Length == 1) // performance improvement
                {
                    return values[index[0]];
                }

                return values[MultiDimensionalArrayHelper.GetIndex1d(index, stride)];
            }
            set
            {
                //exception single dimensional access to More dimensional array used 1d array
                if (index.Length != rank)
                {
                    /*
                                        if (index.Length == 1) // use 1D
                                        {
                                            ((IList)this)[index[0]] = value;
                                            dirty = true;

                                            //todo: collectionchanged etc
                                            return;
                                        }
                    */

                    throw new ArgumentException("Invalid number of indexes");
                }

                var index1d = MultiDimensionalArrayHelper.GetIndex1d(index, stride);
                var newValue = value;
                var newIndex = index1d;
                var oldValue = values[index1d];

                if(values[index1d] == value)
                {
                    return; // do nothing, value is the same as old value
                }

                // log.DebugFormat("Value before Replace: {0}", this);
                
                if (FireEvents)
                {
                    var args = FireCollectionChanging(NotifyCollectionChangedAction.Replace, value, index1d, singleValueLength);
                    if (args != null)
                    {
                        if (args.Cancel)
                        {
                            return;
                        }
                        newValue = args.Item;
                        newIndex = args.Index;
                    }
                }

                values[index1d] = newValue;

                if (newIndex != index1d)
                {
                    if (Rank > 1)
                    {
                        throw new NotSupportedException("Replacing index in CollectionChanging event works only for 1D arrays for now.");
                    }

                    if(newIndex != index1d)
                    {
                        Move(0, index1d, 1, newIndex);
                    }
                }

                dirty = true;

                if (FireEvents)
                {
                    FireCollectionChanged(NotifyCollectionChangedAction.Replace, oldValue, index1d, newIndex, singleValueLength);
                }

                // log.DebugFormat("Value after Replace: {0}", this);
            }
        }

        public virtual IEnumerator GetEnumerator()
        {
            return values.GetEnumerator();
            // return new MultiDimensionalArrayEnumerator(this);
        }

        public virtual void CopyTo(Array array, int index)
        {
            values.CopyTo(array, index);
        }

        public virtual int Add(object value)
        {
            if (rank == 1)
            {
                var index = count;
                var newValue = value;
                if (FireEvents)
                {
                    var args = FireCollectionChanging(NotifyCollectionChangedAction.Add, newValue, index, singleValueLength);
                    if (args != null)
                    {
                        if (args.Cancel)
                        {
                            return -1;
                        }

                        newValue = args.Item;
                        index = args.Index; //index might be changed by changing listeners forcing a sort.
                    }
                }
                values.Insert(index,newValue);

                if(newValue is INotifyPropertyChanged)
                {
                    ((INotifyPropertyChanged)newValue).PropertyChanged += Item_PropertyChanged;
                }

                shape[0]++;
                count = MultiDimensionalArrayHelper.GetTotalLength(shape);

                if (FireEvents)
                {
                    FireCollectionChanged(NotifyCollectionChangedAction.Add, newValue, index, -1, singleValueLength);
                }

                return index;
            }

            throw new NotSupportedException("Use Resize() and this[] to work with array where number of dimensions > 1");
        }

        void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if(PropertyChanged != null)
            {
                PropertyChanged(sender, e);
            }
        }

        public virtual bool Contains(object value)
        {
            if (rank == 1)
            {
                return values.Contains(value);
            }
            throw new NotImplementedException();
        }

        public void Clear()
        {
            if (rank == 1)
            {
                while (Shape[0] > 0)
                {
                    //TODO : increase performance by deleting all. Now events dont match up. Get one changed event when the whole
                    //TODO : array is empty. Change events and batch delete.
                    RemoveAt(0, Shape[0] - 1);
                }
            }
            else
            {
                Resize(new int[rank]);
            }
        }

        public virtual int IndexOf(object value)
        {
            return values.IndexOf(value);
        }

        public void Insert(int index, object value)
        {
            if (rank != 1)
            {
                throw new NotSupportedException("Use SetValue to set values");
            }

            var newValue = value;
            if (FireEvents)
            {
                var args = FireCollectionChanging(NotifyCollectionChangedAction.Add, newValue, index, singleValueLength);
                if (args != null)
                {
                    if (args.Cancel)
                    {
                        return;
                    }
                    newValue = args.Item;
                }
            }

            values.Insert(index, newValue);

            shape[0]++;
            count = MultiDimensionalArrayHelper.GetTotalLength(shape);

            if (FireEvents)
            {
                FireCollectionChanged(NotifyCollectionChangedAction.Add, newValue, index, -1, singleValueLength);
            }
        }

        public virtual void Remove(object value)
        {
            if (rank == 1)
            {
                var index = values.IndexOf(value);

                var valueToRemove = value;
                if (FireEvents)
                {
                    var args = FireCollectionChanging(NotifyCollectionChangedAction.Remove, valueToRemove, index, singleValueLength);
                    if (args != null && args.Cancel)
                    {
                        return;
                    }
                }

                if (valueToRemove is INotifyPropertyChanged)
                {
                    ((INotifyPropertyChanged)valueToRemove).PropertyChanged -= Item_PropertyChanged;
                }

                values.Remove(valueToRemove);

                shape[0]--;
                count = MultiDimensionalArrayHelper.GetTotalLength(shape);

                if (FireEvents)
                {
                    FireCollectionChanged(NotifyCollectionChangedAction.Remove, valueToRemove, index, -1, singleValueLength);
                }
            }
            else
            {
                throw new NotSupportedException("Use Resize");
            }
        }

        public virtual void RemoveAt(int index)
        {
            if (rank == 1)
            {
                var valueToRemove = values[index];

                if (FireEvents)
                {
                    var args = FireCollectionChanging(NotifyCollectionChangedAction.Remove, valueToRemove, index, singleValueLength);

                    if (args != null && args.Cancel)
                    {
                        return;
                    }
                }

                if (values[index] is INotifyPropertyChanged)
                {
                    ((INotifyPropertyChanged)values[index]).PropertyChanged -= Item_PropertyChanged;
                }

                values.RemoveAt(index);

                shape[0]--;
                count = MultiDimensionalArrayHelper.GetTotalLength(shape);

                if (FireEvents)
                {
                    FireCollectionChanged(NotifyCollectionChangedAction.Remove, valueToRemove, index, -1, singleValueLength);
                }
            }
            else
            {
                throw new NotSupportedException("Use Resize");
            }
        }

        public virtual void RemoveAt(int dimension, int index)
        {
            RemoveAt(dimension, index, 1);
        }

        private int[] singleValueLength;

        public virtual void RemoveAt(int dimension, int index, int length)
        {
            if (rank == 1)
            {
                var elementsToRemove = length;
                while (elementsToRemove != 0)
                {
                    RemoveAt(index);
                    elementsToRemove--;
                }
            }
            else
            {
                //move items
                if (dimension > 0 || index != shape[dimension] - 1) // don't copy when 1st dimension and delete at the end (note: performance)
                {
                    for (var i = 0; i < count; i++) // TODO: optimize this, probably it is better to iterate in nD array instead of 1d
                    {
                        var currentIndex = MultiDimensionalArrayHelper.GetIndex(i, stride);

                        if ((currentIndex[dimension] < index) ||
                            (currentIndex[dimension] >= (shape[dimension] - length)))
                        {
                            continue;
                        }

                        // value is to be moved to a new spot in the array
                        var oldIndexes = (int[]) currentIndex.Clone();
                        oldIndexes[dimension] += length;
                        values[i] = values[MultiDimensionalArrayHelper.GetIndex1d(oldIndexes, stride)];
                    }
                }

                //trim the array
                var newShape = (int[])shape.Clone();
                if (newShape[dimension] > 0)
                {
                    newShape[dimension] -= length;
                }
                
                Resize(newShape);
            }
        }

        public virtual object Clone()
        {
            return new MultiDimensionalArray(IsReadOnly, IsFixedSize, DefaultValue, values, Shape);
        }

        /// <summary>
        /// Resizes array using new lengths of dimensions.
        /// </summary>
        /// <param name="newShape"></param>
        public virtual void Resize(params int[] newShape)
        {
            // special case when only the first dimension is altered
            if (MultiDimensionalArrayHelper.ShapesAreEqualExceptFirstDimension(shape, newShape))
            {
                // just in case, check if we really resizing
                if (shape[0] == newShape[0])
                {
                    return;
                }
                ResizeFirstDimension(newShape);
                return;
            }

            var oldShape = shape;
            var oldStride = stride;

            // TODO: optimize copy, currently it is very slow and unscalable
            // create a new defaultValue filled arrayList
            var newTotalLength = MultiDimensionalArrayHelper.GetTotalLength(newShape);
            var newValues = CreateValuesList();
            for (int i = 0; i < newTotalLength; i++)
            {
                newValues.Add(defaultValue);
            }
            //var newValues = Enumerable.Repeat(defaultValue, newTotalLength).ToList();
            var newStride = MultiDimensionalArrayHelper.GetStride(newShape);

            // copy old values to newValues if they are within a new shape, otherwise send Changing event for all *removed* values
            for (var i = 0; i < values.Count; i++)
            {
                var oldIndex = MultiDimensionalArrayHelper.GetIndex(i, stride);

                var isOldIndexWithinNewShape = MultiDimensionalArrayHelper.IsIndexWithinShape(oldIndex, newShape);
                if (!isOldIndexWithinNewShape)
                {
                    if (FireEvents)
                    {
                        var args = FireCollectionChanging(NotifyCollectionChangedAction.Remove, values[i], i, singleValueLength);
                        if (args != null && args.Cancel)
                        {
                            return;
                        }
                    }

                    continue;
                }

                var newValueIndex = MultiDimensionalArrayHelper.GetIndex1d(oldIndex, newStride);
                newValues[newValueIndex] = values[i];
            }

            // set a new value and send Changing event for all *newly added* values
            for (var i = 0; i < newValues.Count; i++)
            {
                var newIndex = MultiDimensionalArrayHelper.GetIndex(i, newStride);
                var isNewIndexWithinOldShape = MultiDimensionalArrayHelper.IsIndexWithinShape(newIndex, shape);

                if (isNewIndexWithinOldShape)
                {
                    continue;
                }

                var newValue = defaultValue;
                if (FireEvents)
                {
                    var args = FireCollectionChanging(NotifyCollectionChangedAction.Add, newValue, i, singleValueLength);
                    if (args != null)
                    {
                        if (args.Cancel)
                        {
                            return;
                        }
                        newValue = args.Item;
                    }
                }

                newValues[i] = newValue;
            }

            var oldValues = values;

            // replace old values by new values
            SetValues(newValues);

            Shape = newShape;

            if (FireEvents)
            {
                // send Changed even for all *removed* values
                for (var i = 0; i < oldValues.Count; i++)
                {
                    var oldIndex = MultiDimensionalArrayHelper.GetIndex(i, oldStride);
                    var isIndexWithinNewShape = MultiDimensionalArrayHelper.IsIndexWithinShape(oldIndex, newShape);
                    if (!isIndexWithinNewShape)
                    {
                        FireCollectionChanged(NotifyCollectionChangedAction.Remove, oldValues[i], i, -1, singleValueLength);
                    }
                }

                // send Changing event for all *newly added* values
                for (var i = 0; i < newValues.Count; i++)
                {
                    var newIndex = MultiDimensionalArrayHelper.GetIndex(i, oldStride);
                    var isNewIndexWithinOldShape = MultiDimensionalArrayHelper.IsIndexWithinShape(newIndex, oldShape);

                    if (isNewIndexWithinOldShape)
                    {
                        continue;
                    }

                    FireCollectionChanged(NotifyCollectionChangedAction.Add, newValues[i], i, -1, singleValueLength);
                }
            }
        }

        protected virtual void SetValues(IList values)
        {
            this.values = values;
        }

        private void ResizeFirstDimension(int[] newShape)
        {
            var valuesToAddCount = MultiDimensionalArrayHelper.GetTotalLength(newShape) - MultiDimensionalArrayHelper.GetTotalLength(shape);

            if (valuesToAddCount > 0)
            {
                bool generateUniqueValueForDefaultValue = false;
                if (null != Owner)
                {
                    generateUniqueValueForDefaultValue = Owner.GenerateUniqueValueForDefaultValue;
                    // newly added function should be unique
                    Owner.GenerateUniqueValueForDefaultValue = true;
                }
                AddValuesToFirstDimension(newShape, valuesToAddCount);
                if (null != Owner)
                {
                    Owner.GenerateUniqueValueForDefaultValue = generateUniqueValueForDefaultValue;
                }
            }
            else if (valuesToAddCount < 0)// remove values at the end. 
            {
                RemoveValuesFromFirstDimension(newShape, valuesToAddCount);
            }
            else
            {
                Shape = newShape;
            }
        }

        private void RemoveValuesFromFirstDimension(int[] newShape, int valuesToAddCount)
        {
            var valuesToRemoveCount = Math.Abs(valuesToAddCount);

            // remove all values
            if (valuesToRemoveCount == values.Count)
            {
                Clear();
            }
            else
            {
                if (FireEvents)
                {
                    // send Changing evenrs
                    for (var index1d = values.Count - 1; index1d < values.Count - 1 - valuesToRemoveCount; index1d--)
                    {
                        var args = FireCollectionChanging(NotifyCollectionChangedAction.Remove, values[index1d], index1d, singleValueLength);
                        if (args != null && args.Cancel)
                        {
                            return;
                        }
                    }
                }

                // remove values
                for (var i = 0; i < valuesToRemoveCount; i++)
                {
                    values.RemoveAt(values.Count - 1);
                }

                Shape = newShape;

                if (FireEvents)
                {
                    // send Changed events
                    for (var index1d = values.Count; index1d < values.Count + valuesToRemoveCount; index1d++)
                    {
                        var removedValue = defaultValue; // TODO: this should be real value
                        FireCollectionChanged(NotifyCollectionChangedAction.Remove, removedValue, index1d, -1, singleValueLength);
                    }
                }
            }
        }

        private void AddValuesToFirstDimension(int[] newShape, int valuesToAddCount)
        {
            var valuesToAdd = new object[valuesToAddCount];

            // send Changing events
            var index1d = values.Count - 1;
            for (var i = 0; i < valuesToAddCount; i++)
            {
                var newValue = defaultValue;

                if (FireEvents)
                {
                    index1d++;
                    var args = FireCollectionChanging(NotifyCollectionChangedAction.Add, newValue, index1d, singleValueLength);
                    if (args != null)
                    {
                        if (args.Cancel)
                        {
                            return;
                        }
                        newValue = args.Item;
                    }
                }

                valuesToAdd[i] = newValue;

                // specific case, for 1d we have to generate events one by one!
                if (rank == 1)
                {
                    values.Add(newValue);
                    shape[0]++;
                    count = MultiDimensionalArrayHelper.GetTotalLength(shape);
                    if (FireEvents)
                    {
                        FireCollectionChanged(NotifyCollectionChangedAction.Add, newValue, index1d, -1, singleValueLength);
                    }
                }
            }

            if (rank > 1)
            {
                // add new values
                foreach (var o in valuesToAdd)
                {
                    values.Add(o);
                }

                Shape = newShape;

                if (FireEvents)
                {
                    // send Changed events
                    index1d = values.Count - valuesToAddCount - 1;
                    for (var i = 0; i < valuesToAddCount; i++)
                    {
                        index1d++;
                        var valueToAdd = valuesToAdd[i];
                        FireCollectionChanged(NotifyCollectionChangedAction.Add, valueToAdd, index1d, -1, singleValueLength);
                    }
                }
            }
        }


        public virtual void InsertAt(int dimension, int index)
        {
            InsertAt(dimension, index, 1);
        }

        public virtual void InsertAt(int dimension, int index, int length)
        {
            if (rank == 1 && index == count && length == 1)
            {
                bool generateUniqueValueForDefaultValue = Owner.GenerateUniqueValueForDefaultValue;
                Owner.GenerateUniqueValueForDefaultValue = true;
                Add(defaultValue);
                Owner.GenerateUniqueValueForDefaultValue = generateUniqueValueForDefaultValue;
                return;
            }

            //resize the array. This can cause a change of stride
            //copy values etc. Refactor resize and copy data operation?
            //increment the correct dimensions
            var newShape = (int[])shape.Clone();
            newShape[dimension] += length;

            // THE REST IS VERY SLOW BECAUSE ALL VALUES ARE COPIED!

            // compute number of values to be added
            var valuesToAddShape = (int[])shape.Clone();
            valuesToAddShape[dimension] = length;
            var newValuesCount = MultiDimensionalArrayHelper.GetTotalLength(valuesToAddShape);

            var valuesToAdd = new ArrayList();
            for (var i = 0; i < newValuesCount; i++)
            {
                valuesToAdd.Add(defaultValue);
            }

            // send Changing events
            if (FireEvents && valuesToAdd.Count > 0)
            {
                var newStride = MultiDimensionalArrayHelper.GetStride(newShape);
                var valueToAddIndex = new int[rank];
                valueToAddIndex[dimension] = index;

                var i = 0;
                do
                {
                    int newIndex1d = MultiDimensionalArrayHelper.GetIndex1d(valueToAddIndex, newStride);
                    var args = FireCollectionChanging(NotifyCollectionChangedAction.Add, defaultValue, newIndex1d, singleValueLength);
                    if (args != null)
                    {
                        if (args.Cancel)
                        {
                            return;
                        }
                        valuesToAdd[i] = args.Item;
                    }
                    i++;
                } while (MultiDimensionalArrayHelper.IncrementIndex(valueToAddIndex, valuesToAddShape, rank - 1));
            }

            var eventsAreFired = fireEvents;
            fireEvents = false;

            Resize(newShape); // TODO: dangerous, Shape and Stride will already change here

            fireEvents = eventsAreFired;

            //walk down the values in the underlying array
            //copy the values to a new spot if the value is to be moved
            int addedValueIndex = 0;
            for (var i = count - 1; i >= 0; i--)
            {
                var newIndex = MultiDimensionalArrayHelper.GetIndex(i, stride);

                // getting index one more time is faster than Clone()
                var oldIndex = MultiDimensionalArrayHelper.GetIndex(i, stride); 
                oldIndex[dimension] -= length;

                // value is to be moved
                if (newIndex[dimension] >= index + length)
                {
                    values[MultiDimensionalArrayHelper.GetIndex1d(newIndex, stride)] = values[MultiDimensionalArrayHelper.GetIndex1d(oldIndex, stride)];
                }

                // new value added, send Changed event 
                if ((newIndex[dimension] >= index) && (newIndex[dimension] < index + length))
                {
                    var index1d = MultiDimensionalArrayHelper.GetIndex1d(newIndex, stride);
                    values[index1d] = valuesToAdd[addedValueIndex];

                    if (FireEvents)
                    {
                        FireCollectionChanged(NotifyCollectionChangedAction.Add, valuesToAdd[addedValueIndex], index1d, -1, singleValueLength);
                    }

                    addedValueIndex++;
                }
            }
        }

        /// <summary>
        /// In 1d case, move vales at index 1, length = 2 to index 2:
        /// 
        /// <code>
        /// 1 [2  3] 4  5 => 1  4 [2  3] 5
        /// </code>
        /// </summary>
        /// <param name="dimension"></param>
        /// <param name="index"></param>
        /// <param name="length"></param>
        /// <param name="newIndex"></param>
        public void Move(int dimension, int index, int length, int newIndex)
        {
            // 1 [2  3][4  5]
            //   view1  view2
            //1 store the value(s) to move in a tmp array
            var valuesToMoveView = Select(dimension, index, index + length - 1);
            var tmpValueToMove = new object[valuesToMoveView.Count];
            valuesToMoveView.CopyTo(tmpValueToMove, 0);
            
            bool eventsAreFired = FireEvents;
            FireEvents = false;
            // 2 Move values between index en newindex in the correct direction
            if (newIndex > index)
            {
                CopyLeft(dimension,index+length, newIndex - index, length);
            }
            else
            {
                CopyRight(dimension,newIndex, index - newIndex, length);
            }
            //3 Place the tmp values at the target

            var targetLocation1 = Select(dimension, newIndex, newIndex + length - 1);
            for (int i =0 ;i<targetLocation1.Count;i++)
            {
                targetLocation1[i] = tmpValueToMove[i];
            }
            FireEvents = true;
            fireEvents = eventsAreFired;

            // 4 Get replace events for everything between index and newindex 
            if(FireEvents)
            {
                var minIndex = Math.Min(index , newIndex );
                var maxIndex = Math.Max(index , newIndex )+length-1;
                for (int i= minIndex;i<=maxIndex;i++)
                {
                    FireCollectionChanged(NotifyCollectionChangedAction.Replace, this[i],i, -1, singleValueLength);                    
                }
            }
        }


        /// <summary>
        /// Copies a block of the array to the right
        /// </summary>
        /// <param name="startIndex">Source index for copy</param>
        /// <param name="length">Length of block to copy</param>
        /// <param name="positions">Number of positions to move the block</param>
        private void CopyRight(int dimension,int startIndex, int length, int positions)
        {
            var sourceView = Select(dimension, startIndex, startIndex + length - 1);
            var targetView = Select(dimension, startIndex + positions, startIndex + positions + length - 1);
            for (int i = sourceView.Count-1; i >= 0 ; i--)
            {
                targetView[i] = sourceView[i];
            }
        }

        /// <summary>
        /// Copies a block of the array to the left
        /// </summary>
        /// <param name="startIndex">Source index for copy</param>
        /// <param name="length">Length of block to copy</param>
        /// <param name="positions">Number of positions to move the block</param>
        private void CopyLeft(int dimension,int startIndex, int length, int positions)
        {
            var sourceView = Select(dimension, startIndex, startIndex + length - 1);
            var targetView = Select(dimension, startIndex - positions, startIndex - positions + length-1);
            for (int i=0;i<sourceView.Count;i++)
            {
                targetView[i] = sourceView[i];
            }
        }

        public bool FireEvents
        {
            get { return fireEvents; } 
            set { fireEvents = value; }
        }

        public void AddRange(IEnumerable values)
        {
            // TODO: disable events, fire changing event for range, add all values, fire changed event

            foreach (var value in values)
            {
                Add(value);
            }
        }


        public virtual IMultiDimensionalArrayView Select(int dimension, int start, int end)
        {
            return new MultiDimensionalArrayView(this, dimension, start, end);
        }

        public virtual IMultiDimensionalArrayView Select(int[] start, int[] end)
        {
            return new MultiDimensionalArrayView(this, start, end);
        }

        public virtual IMultiDimensionalArrayView Select(int dimension, int[] indexes)
        {
            return new MultiDimensionalArrayView(this, dimension, indexes);
        }

        // can be used only for 1D arrays

        public event EventHandler<MultiDimensionalArrayResizeArgs> Resized;


        public event NotifyCollectionChangedEventHandler CollectionChanging;
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        #endregion

        public override string ToString()
        {
            return MultiDimensionalArrayHelper.ToString(this);
        }
        
        private NotifyCollectionChangedEventArgs FireCollectionChanging(NotifyCollectionChangedAction action, object value, int index1d, int[] length)
        {
            if (CollectionChanging == null)
            {
                return null;
            }

            var eventArgs = new MultiDimensionalArrayChangedEventArgs(action, value, index1d, -1, stride);

            CollectionChanging(this, eventArgs);    

            return eventArgs;
        }

        private void FireCollectionChanged(NotifyCollectionChangedAction action, object value, int index1d, int oldIndex1d, int[] length)
        {
            if(CollectionChanged == null)
            {
                return;
            }

            CollectionChanged(this, new MultiDimensionalArrayChangedEventArgs(action, value, index1d, oldIndex1d, stride));
        }

        /// <summary>
        /// Return all values that should be ignored 
        /// TODO: move it to Variable<>?
        /// </summary>
        /// <returns></returns>
        private HashSet<object> GetIgnoreValues()
        {
            var ignoreValues = new HashSet<object>();
            if (null == Owner)
                return ignoreValues;
            foreach (object ignore in Owner.NoDataValues)
            {
                ignoreValues.Add(ignore);
            }
            return ignoreValues;
        }


        private bool dirty = true;
        private object minValue;
        private object maxValue;

        private void UpdateMinMax()
        {
            object maxValue = null;
            object minValue = null;
            HashSet<object> ignoreValues = GetIgnoreValues();

            // todo use linq? does not work on IList or ArrayList, List<object> and List<double> are not
            // interchangeable
            // object min = Where(v => !ignoreValues.Contains(v));

            foreach (object value in this)
            {
                if ((value as IComparable == null) || (ignoreValues.Contains(value)))
                    continue;
                if (((maxValue == null) || (((IComparable)value).CompareTo(maxValue) > 0)))
                {
                    maxValue = value;
                }
                if (((minValue == null) || (((IComparable)value).CompareTo(minValue) < 0)))
                {
                    minValue = value;
                }
            }
            this.maxValue = maxValue;
            this.minValue = minValue;

            dirty = false;
        }

        /// <summary>
        /// Gets the maximal value in the array or throws an exception if the array type is not supported
        /// TODO: move it to Variable<>?
        /// </summary>
        public object MaxValue
        {
            get
            {
                if (dirty)
                {
                    UpdateMinMax();
                }
                return maxValue;
            }
        }

        /// <summary>
        /// Gets the minimal value in the array or throws an exception if the array type is not supported
        /// TODO: move it to Variable<>?
        /// </summary>
        public object MinValue
        {
            get
            {
                if (dirty)
                {
                    UpdateMinMax();
                }
                return minValue;
            }
        }

        /// <summary>
        /// Returns a new instance for the internal 'flat' list.
        /// Returns a generic version if possible 
        /// </summary>
        /// <returns></returns>
        protected virtual IList CreateValuesList()
        {
            return new ArrayList();
        }

        /// <summary>
        /// Creates a new values list and copies the values in values.
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        protected virtual IList CreateClone(IList values)
        {
            return new ArrayList(values);
        }

        public virtual event PropertyChangedEventHandler PropertyChanged;

        public static IMultiDimensionalArray Parse(string text)
        {
            throw new NotImplementedException("");
        }
    }
}