using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DelftTools.Utils.Collections.Generic;

namespace DelftTools.Functions.Generic
{
    [Serializable]
    public class MultiDimensionalArrayView<T> : MultiDimensionalArrayView, IMultiDimensionalArrayView<T> where T : IComparable
    {
        public MultiDimensionalArrayView(IMultiDimensionalArray parent, int dimension,int start, int end):base(parent,dimension,start,end)
        {
        }

        public MultiDimensionalArrayView(IMultiDimensionalArray parent, IList<int> start, IList<int> end):base(parent,start,end)
        {
        }
        
        public MultiDimensionalArrayView(IMultiDimensionalArray parent, int dimension, int[] indexes):base(parent, dimension, indexes)
        {
        }

        public MultiDimensionalArrayView(MultiDimensionalArray parent): base(parent)
        {
        }

        public MultiDimensionalArrayView()
        {
        }

        public void AddRange(IEnumerable<T> enumerable)
        {
            base.Add(enumerable);
        }

        public new IEnumerator<T> GetEnumerator()
        {
            return new MultiDimensionalArrayEnumerator<T>(this);
/*
            foreach (var o in ((IList)this))
            {
                yield return (T)o;
            }
*/
        }

        public void Add(T item)
        {
            base.Add(item);
        }

        public bool Contains(T item)
        {
            return base.Contains(item);
        }

            
        public void CopyTo(T[] array, int index)
        {   
            base.CopyTo(array,index);
        }

        public bool Remove(T item)
        {
            base.Remove(item);
            return true;
        }

        public int IndexOf(T item)
        {
            return base.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            base.Insert(index, item);
        }

        T IList<T>.this[int index]
        {
            get { return (T)((IList)this)[index]; }
            set { ((IList)this)[index] = value; }
        }

        public new T this[params int[] indexes]
        {
            get { return (T)base[indexes]; }
            set { base[indexes] = value; }
        }

        public void Add(params T[] values)
        {
            base.Add(values);
        }

        public override object Clone()
        {
            var clone = new MultiDimensionalArrayView<T>
                            {
                                Parent = Parent,
                                OffsetStart = OffsetStart.ToArray(),
                                OffsetEnd = OffsetEnd.ToArray(),
                                Reduce = new EventedList<bool>(Reduce),
                                SelectedIndexes = new int[Rank][],
                            };


            for (var i = 0; i < Rank; i++)
            {
                if(SelectedIndexes[i] != null )
                {
                    clone.SelectedIndexes[i] = (int[])SelectedIndexes[i].Clone();
                }
            }

            return clone;
        }

        public void Move(int dimension, int index, int length, int newIndex)
        {
            throw new NotImplementedException();
        }

        IMultiDimensionalArrayView<T> IMultiDimensionalArray<T>.Select(int[] start, int[] end)
        {
            return (IMultiDimensionalArrayView<T>) Select(start, end);
        }

        IMultiDimensionalArrayView<T> IMultiDimensionalArray<T>.Select(int dimension, int start, int end)
        {
            return (IMultiDimensionalArrayView<T>)Select(dimension, start, end);
        }

        IMultiDimensionalArrayView<T> IMultiDimensionalArray<T>.Select(int dimension, int[] indexes)
        {
            return (IMultiDimensionalArrayView<T>)Select(dimension, indexes);
        }

        public override IMultiDimensionalArrayView Select(int dimension, int start, int end)
        {
            return new MultiDimensionalArrayView<T>(this, dimension, start, end);
        }

        public override IMultiDimensionalArrayView Select(int[] start, int[] end)
        {
            return new MultiDimensionalArrayView<T>(this, start, end);
        }

        public override IMultiDimensionalArrayView Select(int dimension, int[] indexes)
        {
            return new MultiDimensionalArrayView<T>(this, dimension, indexes);
        }

        #region IList Members

        public object this[int index]
        {
            get { return base[index]; }
            set { base[index] = value; }
        }

        #endregion

        #region IMultiDimensionalArray<T> Members


        public void SetValues(T[] values)
        {
            throw new NotSupportedException("Not supported for views");
        }

        #endregion
    }
}