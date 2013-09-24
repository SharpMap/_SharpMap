using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DelftTools.Utils;
using DelftTools.Utils.Aop;

namespace DelftTools.Functions.Generic
{
    public class MultiDimensionalArray<T> : MultiDimensionalArray, IMultiDimensionalArray<T>
    {
        private IList<T> values2;

        public MultiDimensionalArray()
            : this(new[] { 0 })
        {
        }

        public MultiDimensionalArray(bool isReadOnly)
            : base(isReadOnly, false, default(T), new[] { 0 })
        {
            SetInternalType();
        }
        
        public MultiDimensionalArray(params int[] shape) : base(false, false,default(T),shape)
        {
            SetInternalType();
        }

        public MultiDimensionalArray(bool isReadOnly, bool isFixedSize, T defaultValue, IList<T> values, params int[] shape)
            : base(isReadOnly, isFixedSize, defaultValue, (IList)values, shape)
        {
            SetInternalType();
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

        private void SetInternalType()
        {
            IsReferenceTyped = !typeof(T).IsValueType;
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

        public virtual new IEnumerator<T> GetEnumerator()
        {
            if(values2 != values)
            {
                values2 = (IList<T>) values;
            }

            return values2.GetEnumerator();
            // return new MultiDimensionalArrayEnumerator<T>(this);
        }

        public virtual void Add(T item)
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
		
#if MONO
		object IMultiDimensionalArray.this[int index]
		{
			get { return this[new [] {index}]; }
			set { this[new [] {index}] = value; }
		}

		T IMultiDimensionalArray<T>.this[int index]
		{
			get { return (T)this[new [] {index}]; }
			set { this[new [] {index}] = (T)value; }
		}
#endif

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

        public virtual bool Contains(T item)
        {
            return base.Contains(item);
        }

        public virtual void CopyTo(T[] array, int index)
        {
            base.CopyTo(array, index);
        }

        public virtual bool Remove(T item)
        {
            base.Remove(item);
            return true;
        }

        public virtual int IndexOf(T item)
        {
            return base.IndexOf(item);
        }

        public virtual void Insert(int index, T item)
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
                if (!valueType.IsInstanceOfType(value))
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
        [Aggregation]
        private IList<T> Values
        {
            get
            {
                return values2;
            }
            set
            {
                values2 = value;
                base.SetValues((IList) values2);
            }
        }

        public override void SetValues(IList values)
        {
            if (!(values is IList<T>))
            {
                values2 = (IList<T>) CreateClone(values);

                base.SetValues((IList) values2);
            }
            else
            {
                base.SetValues(values);

                values2 = (IList<T>) values;
            }
        }

        protected override IList CreateValuesList()
        {
            return new List<T>();
        }

        protected override IList CreateClone(IList values)
        {
            //todo this is too memory intensive. It causes out of memory exception all the
            //todo time please base it on array instead of list! 

/*
            var resultValues = new List<T>(values.Count);

            for (int i = 0; i < values.Count; i++)
            {
                resultValues.Add((T) values[i]);
            }

            return resultValues;
*/

            return new List<T>(values.Cast<T>());
        }

        protected override int GetInsertionStartIndex(IList valuesToInsert)
        {
            return MultiDimensionalArrayHelper.GetInsertionIndex(valuesToInsert[0], (IList)values2);
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