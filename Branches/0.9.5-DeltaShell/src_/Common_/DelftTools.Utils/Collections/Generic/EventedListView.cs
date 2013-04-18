using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DelftTools.Utils.Collections.Generic
{
    public interface IEnumerableListEditor
    {
        void OnAdd(object o);
        void OnRemove(object o);
        void OnInsert(int index, object value);
        void OnRemoveAt(int index);
        void OnReplace(int index, object o);
        void OnClear();
    }

    public interface IEnumerableList<T> : IList<T>, IList
    {
        IEnumerable<T> Enumerable { get; set; }

        IEnumerableListEditor Editor { get; set; }
    }

    public class EnumerableList<T> : IEnumerableList<T>
    {
        public EnumerableList()
        {
        }

        public EnumerableList(IEnumerable<T> enumerable)
        {
            Enumerable = enumerable;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return Enumerable.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(T o)
        {
            Editor.OnAdd(o);
        }

        public int Add(object value)
        {
            Editor.OnAdd((T)value);
            return Count - 1;
        }

        public bool Contains(object value)
        {
            return Enumerable.Contains((T) value);
        }

        public void Clear()
        {
            Editor.OnClear();
        }

        public int IndexOf(object value)
        {
            return Enumerable.ToList().IndexOf((T) value);
        }

        public void Insert(int index, object value)
        {
            Editor.OnInsert(index, value);
        }

        public void Remove(object value)
        {
            Editor.OnRemove(value);
        }

        public bool Contains(T item)
        {
            return Enumerable.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            Enumerable.ToArray().CopyTo(array, arrayIndex);
        }

        public bool Remove(T o)
        {
            Editor.OnRemove(o);
            return true;
        }

        public void CopyTo(Array array, int index)
        {
            Enumerable.ToArray().CopyTo(array, index);
        }

        public int Count
        {
            get
            {
                lock (Enumerable)
                {
                    return Enumerable.Count();
                }
            }
        }

        public object SyncRoot
        {
            get { return Enumerable; }
        }

        public bool IsSynchronized
        {
            get
            {
                if (Enumerable is ICollection)
                {
                    return ((ICollection) Enumerable).IsSynchronized;
                }

                return false;
            }
        }

        public bool IsReadOnly
        {
            get { return Editor == null; }
        }

        public bool IsFixedSize
        {
            get { return Editor == null; }
        }

        public int IndexOf(T item)
        {
            var i = 0;
            foreach (var o in Enumerable)
            {
                if(Equals(o, item))
                {
                    return i;
                }
                i++;
            }

            return -1;
        }

        public void Insert(int index, T item)
        {
            Editor.OnInsert(index, item);
        }

        public void RemoveAt(int index)
        {
            Editor.OnRemoveAt(index);
        }

        object IList.this[int index]
        {
            get { return Enumerable.ElementAt(index); }
            set { Editor.OnReplace(index, value); }
        }

        public T this[int index]
        {
            get { return Enumerable.ElementAt(index); }
            set { Editor.OnReplace(index, value); }
        }

        public IEnumerable<T> Enumerable { get; set; }

        public IEnumerableListEditor Editor { get; set; }
    }
}