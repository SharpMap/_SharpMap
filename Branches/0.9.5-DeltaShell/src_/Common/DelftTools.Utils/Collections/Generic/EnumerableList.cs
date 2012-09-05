using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DelftTools.Utils.Collections.Generic
{
    /// <summary>
    /// Implement the IEnumerableList<T> list and allows chaching of the Count to speeds up performace.
    /// If _collectionChangeSource is not set Count will always be processed by Enumerable.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class EnumerableList<T> : IEnumerableList<T>, IEnumerableListCache
    {
        private int cachedCount;
        private bool Dirty { get; set; }
        private INotifyCollectionChange _collectionChangeSource;
        public INotifyCollectionChange CollectionChangeSource
        {
            get { return _collectionChangeSource; }
            set
            {
                if (_collectionChangeSource != null)
                {
                    _collectionChangeSource.CollectionChanged -= NotifyCollectionChangeCollectionChange;
                }
                _collectionChangeSource = value;
                if (_collectionChangeSource != null)
                {
                    _collectionChangeSource.CollectionChanged += NotifyCollectionChangeCollectionChange;
                }
            }
        }

        void NotifyCollectionChangeCollectionChange(object sender, NotifyCollectionChangingEventArgs e)
        {
            Dirty = true;
        }

        public EnumerableList()
        {
            Dirty = true;
        }

        public EnumerableList(IEnumerable<T> enumerable)
        {
            Dirty = true;
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
                //return Enumerable.Count();
                if (Dirty)
                {
                    lock (Enumerable)
                    {
                        cachedCount = Enumerable.Count();
                        if (_collectionChangeSource != null)
                        {
                            Dirty = false;
                        }
                        return cachedCount;
                    }
                }
                return cachedCount;
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