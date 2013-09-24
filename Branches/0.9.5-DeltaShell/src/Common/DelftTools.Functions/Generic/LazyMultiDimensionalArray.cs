using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DelftTools.Utils;
using DelftTools.Utils.Data;

namespace DelftTools.Functions.Generic
{
    /// <summary>
    /// Class wrapping another array. Other array is read only when needed using sourceArrayFunc.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class LazyMultiDimensionalArray<T> : Unique<long>, IMultiDimensionalArray<T>, INotifyPropertyChange
    {
        private readonly Func<IMultiDimensionalArray<T>> sourceArrayFunc;
        private readonly Func<int> customCountDelegate;
        private IMultiDimensionalArray<T> source;

        private IMultiDimensionalArray<T> Source
        {
            get
            {
                if (source == null)
                {
                    source = sourceArrayFunc();
                }
                return source;
            }
        }
        public LazyMultiDimensionalArray(Func<IMultiDimensionalArray<T>> sourceArrayFunc,Func<int> customCountDelegate)
        {
            this.sourceArrayFunc = sourceArrayFunc;
            this.customCountDelegate = customCountDelegate;
        }

        public void AddRange(IEnumerable<T> enumerable)
        {
            if (enumerable is IList)
            {
                Source.AddRange((IList)enumerable);
            }
            else
            {
                Source.AddRange(enumerable.ToList());
            }
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return Source.GetEnumerator();
        }

        public IEnumerator GetEnumerator()
        {
            return Source.GetEnumerator();
        }

        public void CopyTo(Array array, int index)
        {
            Source.CopyTo(array, index);
        }

        public bool Remove(T item)
        {
            return Source.Remove(item);
        }

        IMultiDimensionalArrayView<T> IMultiDimensionalArray<T>.Select(int dimension, int[] indexes)
        {
            return Source.Select(dimension, indexes);
        }

        public int Count
        {
            get
            {
                //if a custom way of getting count is defined we should use it.
                if (customCountDelegate != null)
                {
                    return customCountDelegate();
                }
                //get the 'normal' count..should not get here.
                return Source.Count;
            }
        }

        public void Resize(params int[] newShape)
        {
            Source.Resize(newShape);
        }

        T IMultiDimensionalArray<T>.this[params int[] indexes]
        {
            get { return Source[indexes]; }
            set { Source[indexes] = value; }
        }

        object IMultiDimensionalArray.this[params int[] index]
        {
            get { return Source[index]; }
            set { Source[index] = (T) value; }
        }

        int ICollection.Count
        {
            get { return Count; }
        }

        public object SyncRoot
        {
            get { return Source.SyncRoot; }
        }

        public bool IsSynchronized
        {
            get { return Source.IsSynchronized; }
        }

        public int Add(object value)
        {
            return Source.Add(value);
        }

        public bool Contains(object value)
        {
            return Source.Contains(value);
        }

        public void Clear()
        {
            Source.Clear();
        }

        public void Add(T item)
        {
            Source.Add(item);
        }

        void ICollection<T>.Clear()
        {
             Clear();
        }

        public bool Contains(T item)
        {
            return Source.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            Source.CopyTo(array,arrayIndex);
        }

        void IMultiDimensionalArray.Clear()
        {
            Source.Clear();
        }

        public int IndexOf(T item)
        {
            return Source.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            Source.Insert(index,item);
        }

        public virtual void RemoveAt(int index)
        {
            Source.RemoveAt(index);
        }

        T IList<T>.this[int index]
        {
            get { return Source[index]; }
            set { Source[index] = value; }
        }

        public object DefaultValue
        {
            get { return Source.DefaultValue; }
            set
            {
                if (source != null)
                {
                    source.DefaultValue = value;
                }
            }
        }

        public int[] Shape
        {
            get { return Source.Shape; }
        }

        public int[] Stride
        {
            get { return Source.Stride; }
        }

        public int Rank
        {
            get { return Source.Rank; }
        }

        public IMultiDimensionalArrayView Select(int[] start, int[] end)
        {
            return Source.Select(start, end);
        }

        IMultiDimensionalArrayView<T> IMultiDimensionalArray<T>.Select(int dimension, int start, int end)
        {
            return Source.Select(dimension, start, end);
        }

        IMultiDimensionalArrayView<T> IMultiDimensionalArray<T>.Select(int[] start, int[] end)
        {
            return Source.Select(start, end);
        }

        public IMultiDimensionalArrayView Select(int dimension, int start, int end)
        {
            return Source.Select(dimension,start, end);
        }

        public IMultiDimensionalArrayView Select(int dimension, int[] indexes)
        {
            return Source.Select(dimension,  indexes);
        }

        public void RemoveAt(int dimension, int index)
        {
            Source.RemoveAt(dimension, index);
        }

        public void RemoveAt(int dimension, int index, int length)
        {
            Source.RemoveAt(dimension, index, length);
        }

        public void InsertAt(int dimension, int index)
        {
            Source.InsertAt(dimension, index);
        }

        public void InsertAt(int dimension, int index, int length)
        {
            Source.InsertAt(dimension, index,length);
        }

        public void Move(int dimension, int index, int length, int newIndex)
        {
            Source.Move(dimension, index,length,newIndex);
        }

        public bool FireEvents
        {
            get { return Source.FireEvents; }
            set { Source.FireEvents = value; }
        }

        public void AddRange(IList values)
        {
            Source.AddRange(values);
        }

        public object MaxValue
        {
            get { return Source.MaxValue; }
        }

        public object MinValue
        {
            get { return Source.MinValue; }
        }

        public bool IsAutoSorted
        {
            get { return source.IsAutoSorted; }
            set { throw new NotImplementedException(); }
        }

        void IList.Clear()
        {
            Source.Clear();
        }

        public int IndexOf(object value)
        {
            return Source.IndexOf(value);
        }

        public void Insert(int index, object value)
        {
            Source.Insert(index, value);
        }

        public void Remove(object value)
        {
            Source.Remove(value);
        }

        void IList.RemoveAt(int index)
        {
            RemoveAt(index);
        }

        object IList.this[int index]
        {
            get { return Source[index]; }
            set { Source[index] = (T) value; }
        }

        public bool IsReadOnly
        {
            get { return ((IMultiDimensionalArray) Source).IsReadOnly; }
            set { ((IMultiDimensionalArray) Source).IsReadOnly = value; }
        }

        public bool IsFixedSize
        {
            get { return Source.IsFixedSize; }
        }

        public object Clone()
        {
            return new LazyMultiDimensionalArray<T>(sourceArrayFunc,customCountDelegate);
        }

        //test this stuff
        public event EventHandler<MultiDimensionalArrayChangingEventArgs> CollectionChanging
        {
            add { Source.CollectionChanging += value; }
            remove { Source.CollectionChanging -= value; }
        }

        public event EventHandler<MultiDimensionalArrayChangingEventArgs> CollectionChanged
        {
            add { Source.CollectionChanged += value; }
            remove { Source.CollectionChanged -= value; }
        }

        public int InsertAt(int dimension, int index, int length, IList valuesToInsert)
        {
            throw new NotImplementedException();
        }

        public event PropertyChangedEventHandler PropertyChanged
        {
            add { ((INotifyPropertyChange)Source).PropertyChanged += value; }
            remove { ((INotifyPropertyChange)Source).PropertyChanged -= value; }
        }
        public event PropertyChangingEventHandler PropertyChanging
        {
            add { ((INotifyPropertyChange)Source).PropertyChanging += value; }
            remove { ((INotifyPropertyChange)Source).PropertyChanging -= value; }
        }
        public bool HasParent { get; set; }
    }
}