using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DelftTools.Utils;

namespace DelftTools.Functions.Generic
{
    [Serializable]
    public class MultiDimensionalArray<T> : MultiDimensionalArray, IMultiDimensionalArray<T> where T : IComparable
    {
        private IList<T> values2;

        public MultiDimensionalArray()
            : this(new[] { 0 })
        {
        }

        public MultiDimensionalArray(params int[] shape) : base(false, false,default(T),shape)
        {
        }
        
        public MultiDimensionalArray(IList<T> values, params int[] shape)
            : this(false, false, default(T), values, GetShape((IList)values, shape))
        {
        }

        public MultiDimensionalArray(T[] values, params int[] shape)
            : this(false, false, default(T), values, GetShape((IList)values, shape))
        {
        }

        public MultiDimensionalArray(T[,] values, params int[] shape)
            : this(false, false, default(T), new List<T>(values.Cast<T>()), GetShape((IList)values, shape))
        {
        }

        public MultiDimensionalArray(T[,,] values, params int[] shape)
            : this(false, false, default(T), new List<T>(values.Cast<T>()), GetShape((IList)values, shape))
        {
        }

        public MultiDimensionalArray(bool isReadOnly, bool isFixedSize, IList<T> values, params int[] shape)
            : this(isReadOnly, isFixedSize, default(T), values, shape)
        {
        }

        public MultiDimensionalArray(bool isReadOnly, bool isFixedSize, T defaultValue, IList<T> values, params int[] shape)
            : base(isReadOnly, isFixedSize, defaultValue, (IList)values, shape)
        {
        }

        /// <summary>
        /// TODO : what is this??? Rename or refactor.
        private static int[] GetShape(IList list, int[] shape)
        {
            if (shape.Length > 0)
            {
                return shape;
            }

            if (list is IMultiDimensionalArray)
            {
                return ((IMultiDimensionalArray)list).Shape;
            }

            return new[] { list.Count };
        }

        public void AddRange(IEnumerable<T> enumerable)
        {
            foreach (var o in enumerable)
            {
                Add(o);
            }
        }

        public new IEnumerator<T> GetEnumerator()
        {
            if(values2 != values)
            {
                values2 = (IList<T>) values;
            }

            return values2.GetEnumerator();
            // return new MultiDimensionalArrayEnumerator<T>(this);
        }

        public void Add(T item)
        {
            base.Add(item);
        }

        public override int Add(object value)
        {
            if (value != null && !typeof(T).IsAssignableFrom(value.GetType()))
            {
                return base.Add(Convert.ChangeType(value, typeof (T)));
            }

            return base.Add(value);
        }

        object IList.this[int index]
        {
            get { return base[index]; }
            set
            {
                if (value != null && !typeof(T).IsAssignableFrom(value.GetType()))
                {
                    base[index] = Convert.ChangeType(value, typeof(T));
                    return;
                }

                base[index] = value;
            }
        }

        object IMultiDimensionalArray.this[params int[] index]
        {
            get { return base[index]; }
            set
            {
                if (value != null && !typeof(T).IsAssignableFrom(value.GetType()))
                {
                    base[index] = Convert.ChangeType(value, typeof(T));
                    return;
                }

                base[index] = value;
            }
        }

        public bool Contains(T item)
        {
            return base.Contains(item);
        }

        public void CopyTo(T[] array, int index)
        {
            base.CopyTo(array, index);
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
            get
            {
                return (T) ((IList)this)[index];
            }
            set
            {
                ((IList)this)[index] = value;
            }
        }

        T IMultiDimensionalArray<T>.this[params int[] indexes]
        {
            get
            {
                object value = base[indexes];
                
                if(value != null)
                {
                    return (T) value;
                }
                
                if (typeof(T).IsValueType)
                {
                    return (T) Activator.CreateInstance(typeof (T));
                }

                return (T)(object)null;
            }
            set
            {
                base[indexes] = value;
            }
        }

        #region IMultiDimensionalArray<T> Members

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

        public override object Clone()
        {
            return new MultiDimensionalArray<T>(IsReadOnly, IsFixedSize, this, Shape);
        }

        IMultiDimensionalArrayView<T> IMultiDimensionalArray<T>.Select(int[] start, int[] end)
        {
            return new MultiDimensionalArrayView<T>(this,start,end);
        }

        IMultiDimensionalArrayView<T> IMultiDimensionalArray<T>.Select(int dimension, int start, int end)
        {
            return new MultiDimensionalArrayView<T>(this,dimension, start, end);
        }

        IMultiDimensionalArrayView<T> IMultiDimensionalArray<T>.Select(int dimension, int[] indexes)
        {
            return new MultiDimensionalArrayView<T>(this, dimension, indexes);
        }

        #endregion

        public override object this[params int[] index]
        {
            get { return base[index]; }
            set 
            {
                //provide type checking when using non generic indexer for this array
                Type valueType = GetType().GetGenericArguments()[0];
                if (value.GetType() != valueType)
                {
                    try
                    {
                        base[index] = Convert.ChangeType(value, valueType);
                    }
                    catch (Exception ex)
                    {
                        throw new ArgumentException("Invalid value",ex);
                    }
                }
                else
                {
                    base[index] = value;
                }
            }
        }

        /// <summary>
        /// Don't remove it, used for NHibernate mapping before a better solution will be found
        /// </summary>
        private IList<T> Values
        {
            get
            {
                return values2;
            }
            set
            {
                values2 = value;
                values = (IList) values2;
            }
        }

        protected override void SetValues(IList values)
        {
            base.SetValues(values);
            values2 = (List<T>)values;
        }

        protected override IList CreateValuesList()
        {
            return new List<T>();
        }

        protected override IList CreateClone(IList values)
        {
            //todo this is too memory intensive. It causes out of memory exception all the
            //todo time please base it on array instead of list! 
            return new List<T>(values.Cast<T>());
        }

        public static implicit operator MultiDimensionalArray<T>(T[] values)
        {
            return new MultiDimensionalArray<T>(values);
        }

        public static implicit operator MultiDimensionalArray<T>(T[,] values)
        {
            return new MultiDimensionalArray<T>(values, values.GetLength(0), values.GetLength(1));
        }

        public new static IMultiDimensionalArray<T> Parse(string text)
        {
            if (string.IsNullOrEmpty(text) || text.Trim() == "")
            {
                throw new ArgumentException("Cant parse string: " + text, "text");
            }

            var values = text
                .Replace(" ", string.Empty).Replace("{", string.Empty).Replace("}", string.Empty)
                .Split(',')
                .Select(s => s.Parse<T>(CultureInfo.InvariantCulture));


            var shape = MultiDimensionalArrayHelper.DetectShapeFromString(text);

            return new MultiDimensionalArray<T>(values.ToArray(), shape);
        }

        public static implicit operator MultiDimensionalArray<T>(T[,,] values)
        {
            return new MultiDimensionalArray<T>(values, values.GetLength(0), values.GetLength(1), values.GetLength(2));
        }
    }
}