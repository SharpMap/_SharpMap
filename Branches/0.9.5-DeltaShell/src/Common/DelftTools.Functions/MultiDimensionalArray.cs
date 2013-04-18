using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using DelftTools.Utils;
using DelftTools.Utils.Collections;
using DelftTools.Utils.Data;
using log4net;

namespace DelftTools.Functions
{
    [Serializable]
    public class MultiDimensionalArray : Unique<long>, IMultiDimensionalArray
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(MultiDimensionalArray));

        protected IList values;
        
        protected virtual bool IsReferenceTyped
        {
            get { return true; }
        }
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
            IsFixedSize = isFixedSize;

            DefaultValue = defaultValue;

            Shape = new[] { 0 };

            SetValues(CreateValuesList());

            Resize(shape);
            //set readonly at last
            IsReadOnly = isReadOnly;
        }

        /// <summary>
        /// Gets or sets owner. For optimization purposes only.
        /// This should go! Sound like a bad thing MDA knows about IVariable
        /// </summary>
        //public IVariable Owner { get; set; }

        #region IMultiDimensionalArray Members
        
        public virtual int Count
        {
            get { return count; }
        }

        public virtual object SyncRoot
        {
            get { return values.SyncRoot; }
        }

        public virtual bool IsSynchronized
        {
            get { return values.IsSynchronized; }
        }

        /// <summary>
        /// Determines whether the array maintains a sort by modifying inserts and updates to values.
        /// Only works in 1D situations for now
        /// </summary>
        public virtual bool IsAutoSorted
        {
            get; set;
        }

        public virtual bool IsReadOnly { get; set; }

        public virtual bool IsFixedSize { get; set; }

        public virtual object DefaultValue
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
            set
            {
                this[index] = value;
            }
        }

#if MONO
        object IMultiDimensionalArray.this[int index]
        {
            get { return this[index]; }
            set { this[index] = value; }
        }
#endif

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
                VerifyIsNotReadOnly();
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

                if(values[index1d] == value)
                {
                    return; // do nothing, value is the same as old value
                }

                // log.DebugFormat("Value before Replace: {0}", this);
                
                if (FireEvents)
                {
                    var args = FireCollectionChanging(NotifyCollectionChangeAction.Replace, value, index1d);
                    if (args != null)
                    {
                        if (args.Cancel)
                        {
                            return;
                        }
                        newValue = args.Items[0];
                        newIndex = args.Index;
                    }
                }

                
                if (IsAutoSorted)
                {
                    if(newIndex != index1d)
                    {
                        throw new InvalidOperationException("Updating indexes in collectionChanging is not supported for AutoSortedArrays");
                    }

                    //value or newValue (possible changed by changingEvent...)
                    var oldIndex = index1d;
                    newIndex = MultiDimensionalArrayHelper.GetInsertionIndex((IComparable)value, values.Cast<IComparable>().ToList());
                    if (newIndex > oldIndex)
                    {
                        newIndex--; //if value is currently left of the new index so our insert should be on index one less..
                    }
                }

                //actual set values...
                SetValue1D(newValue,index1d);
                
                //move values to correct location if necessary
                if (newIndex != index1d)
                {
                    Move(0, index1d, 1, newIndex, false);
                }


                dirty = true;

                if (FireEvents)
                {
                    FireCollectionChanged(NotifyCollectionChangeAction.Replace, value, newIndex, index1d );
                }

                // log.DebugFormat("Value after Replace: {0}", this);
            }
        }

        /*private void UpdateInsertionIndex(FunctionValuesChangingEventArgs e, IList<T> values)
        {
            if ((typeof(T) == typeof(string)))
            {
                return; // doesn't matter, it is always unique + we don't care about objects
            }

            var oldIndex = e.Index;

            e.Index = MultiDimensionalArrayHelper.GetInsertionIndex((T)e.Item, values);

            if (e.Action == NotifyCollectionChangeAction.Replace)
            {
                if (e.Index > oldIndex) // !@#@#??????
                {
                    e.Index--;
                }
            }
        }
*/
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
            //add is and insert at dim 0,count
            if (Rank != 1)
            {
                throw new NotSupportedException("Use Resize() and this[] to work with array where number of dimensions > 1");
            }
            return InsertAt(0, count, 1, new[] { value });
        }

        private void VerifyIsNotReadOnly()
        {
            if (IsReadOnly)
                throw new InvalidOperationException("Illegal attempt to modify readonly array");
        }

        void ItemPropertyChanging(object sender, PropertyChangingEventArgs e)
        {
            if (PropertyChanging != null)
            {
                PropertyChanging(sender, e);
            }
        }

        void ItemPropertyChanged(object sender, PropertyChangedEventArgs e)
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
                // performance optimization, edge case
                if (IsAutoSorted && count != 0 && (value as IComparable).IsBigger(MaxValue as IComparable))
                {
                    return false;
                }

                return values.Contains(value);
            }
            throw new NotImplementedException();
        }

        public virtual void Clear()
        {
            VerifyIsNotReadOnly();
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


        public virtual void Remove(object value)
        {
            VerifyIsNotReadOnly();
            if (rank == 1)
            {
                var index = values.IndexOf(value);
                if (index == -1)
                    return;

                var valueToRemove = value;
                if (FireEvents)
                {
                    var args = FireCollectionChanging(NotifyCollectionChangeAction.Remove, valueToRemove, index);
                    if (args != null && args.Cancel)
                    {
                        return;
                    }
                }

                RemoveValue1D(valueToRemove);

                shape[0]--;
                count = MultiDimensionalArrayHelper.GetTotalLength(shape);

                if (FireEvents)
                {
                    FireCollectionChanged(NotifyCollectionChangeAction.Remove, valueToRemove, index, -1);
                }
            }
            else
            {
                throw new NotSupportedException("Use Resize");
            }
        }

        
        //TODO: rewrite this to use RemoveAt(index,dimension,length)
        public virtual void RemoveAt(int index)
        {
            VerifyIsNotReadOnly();
            if (rank == 1)
            {
                var valueToRemove = values[index];

                if (FireEvents)
                {
                    var args = FireCollectionChanging(NotifyCollectionChangeAction.Remove, valueToRemove, index);

                    if (args != null && args.Cancel)
                    {
                        return;
                    }
                }

                RemoveAt1D(index);
                
                

                shape[0]--;
                count = MultiDimensionalArrayHelper.GetTotalLength(shape);

                if (FireEvents)
                {
                    FireCollectionChanged(NotifyCollectionChangeAction.Remove, valueToRemove, index, -1);
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
            VerifyIsNotReadOnly();
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
            VerifyIsNotReadOnly();
            //speed using inserts and remove at the bounds of dimensions...
            /*if (newShape.Length == Rank)
            {
                for (int i =0;i<Rank;i++)
                {
                    var delta = newShape[i] - shape[i];
                    if (delta > 0)
                    {
                        InsertAt(i, shape[i], delta);
                    }
                    else if (delta < 0)
                    {
                        RemoveAt(i,shape[i]+delta);
                    }
                }
            }
*/
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
                        var args = FireCollectionChanging(NotifyCollectionChangeAction.Remove, values[i], i);
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
                    var args = FireCollectionChanging(NotifyCollectionChangeAction.Add, newValue, i);
                    if (args != null)
                    {
                        if (args.Cancel)
                        {
                            return;
                        }
                        newValue = args.Items[0];
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
                        FireCollectionChanged(NotifyCollectionChangeAction.Remove, oldValues[i], i, -1);
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

                    FireCollectionChanged(NotifyCollectionChangeAction.Add, newValues[i], i, -1);
                }
            }
        }

        /// <summary>
        /// Sets underlying storage to a given array. No events are sent! Backdoor to internal storage.
        /// </summary>
        /// <param name="values"></param>
        public virtual void SetValues(IList values)
        {
            if (rank == 1) // Only for 1d
            {
                foreach (var o in values)
                {
                    Subscribe(o);
                }
            }

            this.values = values;
        }

        private void ResizeFirstDimension(int[] newShape)
        {
            var valuesToAddCount = MultiDimensionalArrayHelper.GetTotalLength(newShape) - MultiDimensionalArrayHelper.GetTotalLength(shape);

            if (valuesToAddCount > 0)
            {
                /*
                bool generateUniqueValueForDefaultValue = false;
                if (null != Owner)
                {
                    generateUniqueValueForDefaultValue = Owner.GenerateUniqueValueForDefaultValue;
                    // newly added function should be unique
                    Owner.GenerateUniqueValueForDefaultValue = true;
                }s*/
                AddValuesToFirstDimension(newShape, valuesToAddCount);
                /*if (null != Owner)
                {
                    Owner.GenerateUniqueValueForDefaultValue = generateUniqueValueForDefaultValue;
                }*/
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
                //Set the shape because clear makes it 0,0...other dimensions might not be 0
                Shape = newShape;
            }
            else
            {
                if (FireEvents)
                {
                    // send Changing evenrs
                    for (var index1d = values.Count - 1; index1d < values.Count - 1 - valuesToRemoveCount; index1d--)
                    {
                        var args = FireCollectionChanging(NotifyCollectionChangeAction.Remove, values[index1d], index1d);
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
                        FireCollectionChanged(NotifyCollectionChangeAction.Remove, removedValue, index1d, -1);
                    }
                }
            }
        }

        private void AddValuesToFirstDimension(int[] newShape, int valuesToAddCount)
        {
            var valuesToInsertShape = (int[])newShape.Clone(); //we can copy everything but the first dimension
            valuesToInsertShape[0] = newShape[0] - shape[0]; //take the delta for first dimension

            var valuesToAdd = new object[valuesToAddCount];
            int insertIndex = values.Count;

            for (var i = 0; i < valuesToAddCount; i++)
            {
                valuesToAdd[i] = defaultValue;
            }

            if (FireEvents)
            {
                var args = FireCollectionChanging(NotifyCollectionChangeAction.Add, valuesToAdd, insertIndex, valuesToInsertShape);
                if (args != null && args.Cancel)
                {
                    return;
                }
            }

            foreach (var o in valuesToAdd)
            {
                values.Add(o);
            }

            Shape = newShape;

            if (FireEvents)
            {
                FireCollectionChanged(NotifyCollectionChangeAction.Add, valuesToAdd, insertIndex, -1, valuesToInsertShape);
            }
        }

        public virtual void Insert(int index, object value)
        {
            VerifyIsNotReadOnly();
            if (rank != 1)
            {
                throw new NotSupportedException("Use SetValue to set values");
            }

            var newValue = value;
            if (FireEvents)
            {
                var args = FireCollectionChanging(NotifyCollectionChangeAction.Add, newValue, index);
                if (args != null)
                {
                    if (args.Cancel)
                    {
                        return;
                    }
                    newValue = args.Items[0];
                }
            }
            //InsertValues1D(index, newValue);
            InsertValues1D(newValue, index);

            shape[0]++;
            count = MultiDimensionalArrayHelper.GetTotalLength(shape);
            dirty = true;//update min/max after insert..maybe check if the value we inserted is > max or < min

            if (FireEvents)
            {
                FireCollectionChanged(NotifyCollectionChangeAction.Add, newValue, index, -1);
            }

        }

        
        public virtual void InsertAt(int dimension, int index)
        {
            InsertAt(dimension, index, 1);
        }
        public virtual void InsertAt(int dimension, int index, int length)
        {
            // THE REST IS VERY SLOW BECAUSE ALL VALUES ARE COPIED!

            // compute number of values to be added
            object[] valuesToAdd = GetDefaultValuesToAdd(dimension, length);
            //no values are added ..just a resize with some dimensions at 0
            if (valuesToAdd.Length == 0)
            {
                var newShape = (int[])shape.Clone();
                newShape[dimension] += length;
                Resize(newShape);
            }
            else
            {
                InsertAt(dimension, index, length, valuesToAdd);    
            }
        }

        private object[] GetDefaultValuesToAdd(int dimension, int length)
        {
            var valuesToAddShape = (int[])shape.Clone();
            valuesToAddShape[dimension] = length;
            var newValuesCount = MultiDimensionalArrayHelper.GetTotalLength(valuesToAddShape);

            var valuesToAdd = new object[newValuesCount];
            for (var i = 0; i < newValuesCount; i++)
            {
                valuesToAdd[i]= defaultValue;
            }
            return valuesToAdd;
        }

        //TODO: change this signature of values object[] this will cause boxing and bad performance probably..
        //push the functionality down to the generic subclass.
        /// <summary>
        /// Insert a slices of value(s) for a given dimension
        /// </summary>
        /// <param name="dimension">Dimensen at which to insert</param>
        /// <param name="index">Index of insert for dimension</param>
        /// <param name="length">Length (in the dimensions). In 1D this is ValuesToInsert.Count but not in n-D</param>
        /// <param name="valuesToInsert">Total values</param>
        /// <returns></returns>
        public virtual int InsertAt(int dimension, int index, int length, IList valuesToInsert)
        {
            if (length == 0)
                return -1;


            //resize the array. This can cause a change of stride
            //copy values etc. Refactor resize and copy data operation?
            //increment the correct dimensions
            var newShape = (int[])shape.Clone();
            newShape[dimension] += length;

            // THE REST IS VERY SLOW BECAUSE ALL VALUES ARE COPIED!

            // compute number of values to be added
            var valuesToInsertShape = (int[])shape.Clone();
            valuesToInsertShape[dimension] = length;

            
            MultiDimensionalArrayHelper.VerifyValuesCountMatchesShape(valuesToInsertShape, valuesToInsert);

            var newStride = MultiDimensionalArrayHelper.GetStride(newShape);
            var valueToAddIndex = new int[rank];
            valueToAddIndex[dimension] = index;
            int insertionStartIndex = MultiDimensionalArrayHelper.GetIndex1d(valueToAddIndex, newStride);
                
            // send Changing events
            if (FireEvents)
            {
                var args = FireCollectionChanging(NotifyCollectionChangeAction.Add, valuesToInsert, insertionStartIndex,
                                                  valuesToInsertShape);

                //TODO: handle changes from changing event..allows for sorting (a little bit)
                if (args != null)
                {
                    if (args.Cancel)
                    {
                        return -1;
                    }
                    if (args.Index != insertionStartIndex && IsAutoSorted)
                    {
                        throw new InvalidOperationException("Sorted array does not allow update of Indexes in CollectionChanging");
                    }
                }
            }
            if (IsAutoSorted) 
            {
                //values values to insert have values smaller than MaxValue throw
                var comparables = valuesToInsert.Cast<IComparable>();
                var monotonous =comparables.IsMonotonousAscending();
                // first is the smallest value since comparables is monotonous ascending :)
                var allBiggerThanMaxValue = ((MaxValue == null) || ((MaxValue as IComparable).IsSmaller(comparables.First())));
                if (comparables.Count()>1 && 
                    (!allBiggerThanMaxValue 
                    || !monotonous))
                {
                    throw new InvalidOperationException(
                        "Adding range of values for sorted array is only possible if these values are all bigger than the current max and sorted");
                }
                
                //get the 'real' insertion indexes..first for 1 value
                //omg this must be slow...get this faster by 
                //A : using the knowledge that the array was sorted (binarysort)
                //B : using the type of T by moving this into a virtual method and push it down to a subclass

                insertionStartIndex = GetInsertionStartIndex(valuesToInsert);
                
                
                index = insertionStartIndex;//review and fix all these indexes..it is getting unclear..work in MDA style..
                
            }

            //performance increase when adding to the first dimension...no stuff will be moved so not need to go throught the whole array later on
            bool insertIsAtBoundsOfFirstDimension = dimension == 0 && index == shape[0];

            var eventsAreFired = fireEvents;
            fireEvents = false;

            Resize(newShape); // TODO: dangerous, Shape and Stride will already change here
            
            
            fireEvents = eventsAreFired;
            dirty = true;//make sure min/max are set dirty


            //simple insert...at bounds of first index
            if (insertIsAtBoundsOfFirstDimension)
            {
                var addedValueIndex = 0;
                do
                {
                    int newIndex1d = MultiDimensionalArrayHelper.GetIndex1d(valueToAddIndex, newStride);
                    var newValue = valuesToInsert[addedValueIndex];

                    SetValue1D(newValue, newIndex1d);

                    addedValueIndex++;
                } while (MultiDimensionalArrayHelper.IncrementIndex(valueToAddIndex, newShape, rank - 1));

                //fill it up until the whole new shape is filled..because we are are at the bounds of the first dimension
                if (FireEvents)
                {
                    FireCollectionChanged(NotifyCollectionChangeAction.Add, valuesToInsert, insertionStartIndex, -1, valuesToInsertShape);
                }

            }
            else  //complex...insert could be everywhere and might have to move stuff in the underlying array..
            {
                //walk down the values in the underlying array
                //copy the values to a new spot if the value is to be moved
                int addedValueIndex = valuesToInsert.Count - 1;

                var newIndex = MultiDimensionalArrayHelper.GetIndex(count - 1, stride); //start at end

                for (var i = count - 1; i >= 0; i--)
                {
                    // value is to be moved
                    if (newIndex[dimension] >= index + length)
                    {
                        var oldIndex = (int[])newIndex.Clone();
                        oldIndex[dimension] -= length;

                        values[MultiDimensionalArrayHelper.GetIndex1d(newIndex, stride)] =
                            values[MultiDimensionalArrayHelper.GetIndex1d(oldIndex, stride)];
                        //set the 'old' copy to 'null' to prevent unsubscribtion when we replace it by the new value
                        values[MultiDimensionalArrayHelper.GetIndex1d(oldIndex, stride)] = defaultValue;
                    }

                    // new value added
                    if ((newIndex[dimension] >= index) && (newIndex[dimension] < index + length))
                    {
                        var index1d = MultiDimensionalArrayHelper.GetIndex1d(newIndex, stride);
                        var newValue = valuesToInsert[addedValueIndex];

                        SetValue1D(newValue,index1d);

                        addedValueIndex--;//walk down because we start at the end
                    }

                    if (i > 0) //decrementing last time won't work
                    {
                        MultiDimensionalArrayHelper.DecrementIndexForShape(newIndex,newShape);
                    }
                }
                if (FireEvents)
                {
                    FireCollectionChanged(NotifyCollectionChangeAction.Add, valuesToInsert,insertionStartIndex,-1,valuesToInsertShape);
                }
            }
            return index;//this might be wrong..the index might have changed

        }
       
        /// <summary>
        /// Removes value and does unsubscribe
        /// </summary>
        /// <param name="valueToRemove"></param>
        private void RemoveValue1D(object valueToRemove)
        {
            Unsubscribe(valueToRemove);
            values.Remove(valueToRemove);
        }

        private void Subscribe(object item)
        {
            if (!IsReferenceTyped)
                return;
            if (item is INotifyPropertyChanged)
            {
                ((INotifyPropertyChanged)item).PropertyChanged += ItemPropertyChanged;
            }
            if (item is INotifyPropertyChanging)
            {
                ((INotifyPropertyChanging)item).PropertyChanging += ItemPropertyChanging;
            }

        }

        private void Unsubscribe(object item)
        {
            if (!IsReferenceTyped)
                return;
            if (item is INotifyPropertyChanged)
            {
                ((INotifyPropertyChanged)item).PropertyChanged -= ItemPropertyChanged;
            }
            if (item is INotifyPropertyChanging)
            {
                ((INotifyPropertyChanging)item).PropertyChanging -= ItemPropertyChanging;
            }
        }

        /// <summary>
        /// Sets value in underlying array and subscribes to changes if possible
        /// </summary>
        /// <param name="newValue"></param>
        /// <param name="newIndex1D"></param>
        private void SetValue1D(object newValue, int newIndex1D)
        {
            if (IsReferenceTyped)
            {
                var oldValue = values[newIndex1D];
                Unsubscribe(oldValue);
                
                Subscribe(newValue);
            }

            values[newIndex1D] = newValue;
        }

        
        private void InsertValues1D(object newValue, int index)
        {
            Subscribe(newValue);
            values.Insert(index, newValue);
        }
        
        private void RemoveAt1D(int index1D)
        {
            var oldValue = values[index1D];
            RemoveValue1D(oldValue);
        }



        protected virtual int GetInsertionStartIndex(IList valuesToInsert)
        {
            return MultiDimensionalArrayHelper.GetInsertionIndex((IComparable)valuesToInsert[0], values.Cast<IComparable>().ToList());
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
        public virtual void Move(int dimension, int index, int length, int newIndex)
        {
            Move(dimension, index, length, newIndex, true);
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
        /// <param name="fireEvents"></param>
        private void Move(int dimension, int index, int length, int newIndex, bool fireEvents)
        {
            //where is the changing event in this case??? only changed is made and fired?
            VerifyIsNotReadOnly();

            // 1 [2  3][4  5]
            //   view1  view2
            //1 store the value(s) to move in a tmp array
            var valuesToMoveView = Select(dimension, index, index + length - 1);
            var tmpValueToMove = new object[valuesToMoveView.Count];
            valuesToMoveView.CopyTo(tmpValueToMove, 0);
            
            bool eventsAreToBeFired = FireEvents && fireEvents;

            if (eventsAreToBeFired)
            {
                var minIndex = Math.Min(index, newIndex);
                var maxIndex = Math.Max(index, newIndex) + length - 1;
                for (int i = minIndex; i <= maxIndex; i++)
                {
                    //TODO: add value..it is needed in UNDO?
                    FireCollectionChanging(NotifyCollectionChangeAction.Replace, null, i);
                }                
            }

            bool oldIsSorted = IsAutoSorted;
            var oldFireEvents = FireEvents;
            FireEvents = false;
            IsAutoSorted = false;
            // 2 Move values between index en newindex in the correct direction
            if (newIndex > index)
            {
                CopyLeft(dimension, index + length, newIndex - index, length);
            }
            else
            {
                CopyRight(dimension, newIndex, index - newIndex, length);
            }

            //3 Place the tmp values at the target
            var targetLocation1 = Select(dimension, newIndex, newIndex + length - 1);
            for (int i =0 ;i<targetLocation1.Count;i++)
            {
                targetLocation1[i] = tmpValueToMove[i];
            }
            
            //FireEvents = true;
            FireEvents = oldFireEvents;
            IsAutoSorted = oldIsSorted;

            // 4 Get replace events for everything between index and newindex 
            if(eventsAreToBeFired)
            {
                var minIndex = Math.Min(index , newIndex );
                var maxIndex = Math.Max(index, newIndex) + length - 1;
                for (int i = minIndex; i <= maxIndex; i++)
                {
                    FireCollectionChanged(NotifyCollectionChangeAction.Replace, this[i], i, -1);
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

        public virtual bool FireEvents
        {
            get { return fireEvents; } 
            set { fireEvents = value; }
        }

        public virtual void AddRange(IList values)
        {
            InsertAt(0, Count, values.Count, values);
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

        public virtual event EventHandler<MultiDimensionalArrayChangingEventArgs> CollectionChanging;
        public virtual event EventHandler<MultiDimensionalArrayChangingEventArgs> CollectionChanged;

        #endregion

        public override string ToString()
        {
            return MultiDimensionalArrayHelper.ToString(this);
        }

        private MultiDimensionalArrayChangingEventArgs FireCollectionChanging(NotifyCollectionChangeAction action, IList values, int index1d, int[] itemsShape)
        {
            if (CollectionChanging == null)
            {
                return null;
            }

            var eventArgs = new MultiDimensionalArrayChangingEventArgs(action, values, index1d, -1, stride,itemsShape);
            CollectionChanging(this, eventArgs);
            return eventArgs;
        }

        private MultiDimensionalArrayChangingEventArgs FireCollectionChanging(NotifyCollectionChangeAction action, object value, int index1d)
        {
            return FireCollectionChanging(action, new[] {value}, index1d, new[] {1});
        }

        private void FireCollectionChanged(NotifyCollectionChangeAction action, IList values, int index1d,int oldIndex1d, int[] shape)
        {
            if (CollectionChanged == null)
            {
                return;
            }

            var eventArgs = new MultiDimensionalArrayChangingEventArgs(action, values, index1d, oldIndex1d, stride, shape);

            CollectionChanged(this,eventArgs);
        }

        private void FireCollectionChanged(NotifyCollectionChangeAction action, object value, int index1d, int oldIndex1d)
        {
            FireCollectionChanged(action, new[] { value }, index1d, oldIndex1d, new[] { 1 });
            /*if(CollectionChanged == null)
            {
                return;
            }
            
            CollectionChanged(this, new MultiDimensionalArrayChangingEventArgs(action, value, index1d, oldIndex1d, stride));*/
        }

        

        private bool dirty = true;
        private object minValue;
        private object maxValue;

        private void UpdateMinMax()
        {
            object maxValue = null;
            object minValue = null;
        
            // todo use linq? does not work on IList or ArrayList, List<object> and List<double> are not
            // interchangeable
            // object min = Where(v => !ignoreValues.Contains(v));
            
            foreach (object value in this)
            {
                if (value as IComparable == null) 
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
        public virtual object MaxValue
        {
            get
            {
                if (dirty) 
                {
                    if ((IsAutoSorted) && (values.Count != 0))
                    {
                        return values[values.Count-1];
                    }
                    UpdateMinMax();
                }
                return maxValue;
            }
        }

        /// <summary>
        /// Gets the minimal value in the array or throws an exception if the array type is not supported
        /// TODO: move it to Variable<>?
        /// </summary>
        public virtual object MinValue
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
            return new ArrayList {};
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

        public virtual event PropertyChangingEventHandler PropertyChanging; 
        public virtual event PropertyChangedEventHandler PropertyChanged;

        public static IMultiDimensionalArray Parse(string text)
        {
            throw new NotImplementedException("");
        }
    }
}