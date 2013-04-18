using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DelftTools.Functions.Conversion;
using DelftTools.Utils.Collections;

namespace DelftTools.Functions.Generic
{
    public class ConvertedArray<TTarget, TSource> : IMultiDimensionalArray<TTarget>
        where TTarget : IComparable
        where TSource : IComparable
    {
        private readonly IMultiDimensionalArray<TSource> source;
        private readonly Func<TSource, TTarget> toTarget;
        private readonly Func<TTarget, TSource> toSource;

        public ConvertedArray(IMultiDimensionalArray<TSource> source, Func<TTarget, TSource> toSource, Func<TSource, TTarget> toTarget)
        {
            this.toSource = toSource;
            this.toTarget = toTarget;
            this.source = source;
        }
        public void AddRange(IEnumerable<TTarget> enumerable)
        {
            source.AddRange(enumerable.Select(t => toSource(t)));
        }

        public IEnumerator<TTarget> GetEnumerator()
        {
            return new ConvertedEnumerator<TTarget, TSource>(source.GetEnumerator(), toTarget);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }

        public bool Remove(TTarget item)
        {
            return source.Remove(toSource(item));
        }

        public int Count
        {
            get { return source.Count; }
        }

        public bool IsReadOnly
        {
            get { return ((IList)source).IsReadOnly; }
        }


        IMultiDimensionalArrayView IMultiDimensionalArray.Select(int[] start, int[] end)
        {
            return Select(start, end);
        }

        IMultiDimensionalArrayView IMultiDimensionalArray.Select(int dimension, int start, int end)
        {
            return Select(dimension, start, end);
        }

        IMultiDimensionalArrayView IMultiDimensionalArray.Select(int dimension, int[] indexes)
        {
            return Select(dimension, indexes);
        }

        public IMultiDimensionalArrayView<TTarget> Select(int dimension, int[] indexes)
        {
            return new ConvertedArray<TTarget, TSource>(source, toSource, toTarget).Select(dimension, indexes);
        }

        public IMultiDimensionalArrayView<TTarget> Select(int[] start, int[] end)
        {
            return new ConvertedArray<TTarget, TSource>(source, toSource, toTarget).Select(start,end);
        }

        int IMultiDimensionalArray.Count
        {
            get { return Count; }
        }

        public void Resize(params int[] newShape)
        {
            source.Resize(newShape);
        }

        public TTarget this[params int[] indexes]
        {
            get 
            {
                return toTarget(source[indexes]); 
            }
            set
            {
                source[indexes] = toSource(value);
            }
        }

        object IMultiDimensionalArray.this[params int[] index]
        {
            get
            {
                return this[index];
            }
            set
            {
                this[index]= (TTarget) value;
            }
        }

        int ICollection.Count
        {
            get { return Count; }
        }

        public object SyncRoot
        {
            get { return source.SyncRoot; }
        }

        public bool IsSynchronized
        {
            get { return source.IsSynchronized; }
        }

        public int Add(object value)
        {
            return source.Add((object)toSource((TTarget) value));
        }

        public bool Contains(object value)
        {
            return source.Contains(toSource((TTarget) value));
        }

        public void Add(TTarget item)
        {
            source.Add(toSource(item));
        }

        public void Clear()
        {
            source.Clear();
        }

        public bool Contains(TTarget item)
        {
            return source.Contains(toSource(item));
        }

        public void CopyTo(TTarget[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public int IndexOf(TTarget item)
        {
            return source.IndexOf(toSource(item));
        }

        public void Insert(int index, TTarget item)
        {
            source.Insert(index,toSource(item));
        }

        public void RemoveAt(int index)
        {
            source.RemoveAt(index);
        }

        TTarget IList<TTarget>.this[int index]
        {
            get
            {
                return this[index];
            }
            set
            {
                this[index] = value;
            }
        }

        public IMultiDimensionalArray<TTarget> Clone()
        {
            throw new NotImplementedException();
        }

        void IMultiDimensionalArray.Clear()
        {
            Clear();
        }

        void IMultiDimensionalArray.RemoveAt(int index)
        {
            RemoveAt(index);
        }

        public object DefaultValue
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public int[] Shape
        {
            get {return source.Shape; }
        }

        public int[] Stride
        {
            get { return source.Stride; }
        }

        public int Rank
        {
            get { return source.Rank; }
        }
      
        public IMultiDimensionalArrayView<TTarget> Select(int dimension, int start, int end)
        {
            return new ConvertedArray<TTarget, TSource>(source, toSource, toTarget).Select(dimension, start, end);
        }


        public void RemoveAt(int dimension, int index)
        {
            source.RemoveAt(dimension,index);
        }

        public void RemoveAt(int dimension, int index, int length)
        {
            source.RemoveAt(dimension,index,length);
        }

        public void InsertAt(int dimension, int index)
        {
            source.InsertAt(dimension,index);
        }

        public void InsertAt(int dimension, int index, int length)
        {
            source.InsertAt(dimension,index,length);
        }

        public void Move(int dimension, int index, int length, int newIndex)
        {
            source.Move(dimension,index,length,newIndex);
        }

        public bool FireEvents
        {
            get { return source.FireEvents; }
            set { source.FireEvents = value; }
        }

        public void AddRange(IEnumerable values)
        {
            //foreach??
            //source.AddRange(values);
            throw new NotImplementedException();
        }

        public object MaxValue
        {
            get
            {
                return toTarget((TSource) source.MaxValue); 
            }
        }

        public object MinValue
        {
            get
            {
                return toTarget((TSource)source.MinValue); 
            }
        }

        public IVariable Owner { get; set; }

        void IList.Clear()
        {
            Clear();
        }

        public int IndexOf(object value)
        {
            return source.IndexOf(toSource((TTarget) value));
        }

        public void Insert(int index, object value)
        {
            source.Insert(index,toSource((TTarget) value));
        }

        public void Remove(object value)
        {
            source.Remove(toSource((TTarget) value));
        }

        void IList.RemoveAt(int index)
        {
            RemoveAt(index);
        }

        object IList.this[int index]
        {
            get { return this[index]; }
            set { this[index] = (TTarget) value; }
        }

        bool IList.IsReadOnly
        {
            get { return IsReadOnly; }
        }

        public bool IsFixedSize
        {
            get { return source.IsFixedSize; }
        }

        object ICloneable.Clone()
        {
            throw new NotImplementedException();
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public event NotifyCollectionChangedEventHandler CollectionChanging;
        public event PropertyChangedEventHandler PropertyChanged;
        public long Id
        {
            get; set;
        }

        public override string ToString()
        {
            return MultiDimensionalArrayHelper.ToString(this);
        }
    }
}