using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;

namespace DelftTools.Utils.Collections.Generic
{
    /// <summary>
    /// A list that supports IEventedList so the outside world
    /// can tell when changes occur
    /// </summary>
    /// <typeparam name="T">The type of element being stored in the list</typeparam>
    [Serializable]
    public class EventedList<T> : IEventedList<T>, IList, INotifyPropertyChanged // TODO: move INotifyPropertyChanged to interface and fix aspect
    {
        // WARNING: any change here should be mirrored in >>>>>>>>> PersistentEventedList <<<<<<<<<< !!!!!!!
        // TODO: make PersistentEventedList<T> and EventedList<T> use the same impl
        // TODO: make it work on Ranges and send events on ranges

        /// <summary>
        /// The underlying storage
        /// </summary>
        private readonly List<T> list;

        #region Constructors

        /// <summary>
        /// Construct me
        /// </summary>
        public EventedList(): this(null)
        {
        }

        /// <summary>
        /// Construct me
        /// </summary>
        /// <param name="initialData">The initialization data</param>
        public EventedList(IEnumerable<T> initialData)
        {
            list = initialData == null ? new List<T>() : new List<T>(initialData);

            // subscribe to existing items
            foreach (var o in list)
            {
                SubscribeEvents(o);
            }
        }

        private void InitializeDelegates()
        {
            Item_PropertyChangedDelegate = new PropertyChangedEventHandler(Item_PropertyChanged);
            Item_CollectionChangingDelegate = new NotifyCollectionChangedEventHandler(Item_CollectionChanging);
            Item_CollectionChangedDelegate = new NotifyCollectionChangedEventHandler(Item_CollectionChanged);
        }

        #endregion

        // re-use event delegates, for performance reasons (10x speedup at add/remove)
        PropertyChangedEventHandler Item_PropertyChangedDelegate;
        NotifyCollectionChangedEventHandler Item_CollectionChangedDelegate;
        NotifyCollectionChangedEventHandler Item_CollectionChangingDelegate;

        #region IList<T> Members

        public int IndexOf(T item)
        {
            return list.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            CheckReadOnly();
            if(!OnAdding(item, index))
            {
                return;
            }
            list.Insert(index, item);
            OnAdded(item, index);
        }

        [DebuggerStepThrough]
        private void CheckReadOnly()
        {
            if (IsReadOnly)
            {
                throw new ReadOnlyException("Collection is read-only");
            }
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the <see cref="T:System.Collections.IList" />.
        /// </summary>
        /// <param name="value">The <see cref="T:System.Object" /> to remove from the <see cref="T:System.Collections.IList" />. </param>
        /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.IList" /> is read-only.-or- The <see cref="T:System.Collections.IList" /> has a fixed size. </exception><filterpriority>2</filterpriority>
        public void Remove(object value)
        {
            Remove((T) value);
        }

        public void RemoveAt(int index)
        {
            CheckReadOnly();
            var item = this[index];
            if(!OnRemoving(item, index))
            {
                return;
            }
            list.RemoveAt(index);
            OnRemoved(item, index);
        }

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        /// <returns>
        /// The element at the specified index.
        /// </returns>
        /// <param name="index">The zero-based index of the element to get or set. </param>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index" /> is not a valid index in the <see cref="T:System.Collections.IList" />. </exception>
        /// <exception cref="T:System.NotSupportedException">The property is set and the <see cref="T:System.Collections.IList" /> is read-only. </exception><filterpriority>2</filterpriority>
        object IList.this[int index]
        {
            get { return this[index]; }
            set { this[index] = (T)value; }
        }

        public T this[int index]
        {
            get { return list[index]; }
            set
            {
                CheckReadOnly();
                var old = this[index];
                if(!OnReplacing(old, index))
                {
                    return;
                }
                list[index] = value;
                OnReplaced(value, old, index);
            }
        }

        #endregion

        #region ICollection<T> Members
        
        [DebuggerStepThrough]
        public void Add(T item)
        {
            CheckReadOnly();
            if(!OnAdding(item, Count))
            {
                return;
            }
            list.Add(item);
            OnAdded(item,list.Count - 1);
        }

        /// <summary>
        /// Adds an item to the <see cref="T:System.Collections.IList" />.
        /// </summary>
        /// <returns>
        /// The position into which the new element was inserted.
        /// </returns>
        /// <param name="value">The <see cref="T:System.Object" /> to add to the <see cref="T:System.Collections.IList" />. </param>
        /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.IList" /> is read-only.-or- The <see cref="T:System.Collections.IList" /> has a fixed size. </exception><filterpriority>2</filterpriority>
        int IList.Add(object value)
        {
            Add((T) value);
            return Count - 1;
        }

        /// <summary>
        /// Determines whether the <see cref="T:System.Collections.IList" /> contains a specific value.
        /// </summary>
        /// <returns>
        /// true if the <see cref="T:System.Object" /> is found in the <see cref="T:System.Collections.IList" />; otherwise, false.
        /// </returns>
        /// <param name="value">The <see cref="T:System.Object" /> to locate in the <see cref="T:System.Collections.IList" />. </param><filterpriority>2</filterpriority>
        public bool Contains(object value)
        {
            return Contains((T) value);
        }

        public void Clear()
        {
            CheckReadOnly();

            while(list.Count != 0)
            {
                RemoveAt(list.Count - 1);
            }
        }

        /// <summary>
        /// Determines the index of a specific item in the <see cref="T:System.Collections.IList" />.
        /// </summary>
        /// <returns>
        /// The index of <paramref name="value" /> if found in the list; otherwise, -1.
        /// </returns>
        /// <param name="value">The <see cref="T:System.Object" /> to locate in the <see cref="T:System.Collections.IList" />. </param><filterpriority>2</filterpriority>
        public int IndexOf(object value)
        {
            return IndexOf((T) value);
        }

        /// <summary>
        /// Inserts an item to the <see cref="T:System.Collections.IList" /> at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="value" /> should be inserted. </param>
        /// <param name="value">The <see cref="T:System.Object" /> to insert into the <see cref="T:System.Collections.IList" />. </param>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index" /> is not a valid index in the <see cref="T:System.Collections.IList" />. </exception>
        /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.IList" /> is read-only.-or- The <see cref="T:System.Collections.IList" /> has a fixed size. </exception>
        /// <exception cref="T:System.NullReferenceException"><paramref name="value" /> is null reference in the <see cref="T:System.Collections.IList" />.</exception><filterpriority>2</filterpriority>
        public void Insert(int index, object value)
        {
            Insert(index, (T)value);
        }

        public bool Contains(T item)
        {
            return list.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            list.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Copies the elements of the <see cref="T:System.Collections.ICollection" /> to an <see cref="T:System.Array" />, starting at a particular <see cref="T:System.Array" /> index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="T:System.Array" /> that is the destination of the elements copied from <see cref="T:System.Collections.ICollection" />. The <see cref="T:System.Array" /> must have zero-based indexing. </param>
        /// <param name="index">The zero-based index in <paramref name="array" /> at which copying begins. </param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="array" /> is null. </exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index" /> is less than zero. </exception>
        /// <exception cref="T:System.ArgumentException"><paramref name="array" /> is multidimensional.-or- <paramref name="index" /> is equal to or greater than the length of <paramref name="array" />.-or- The number of elements in the source <see cref="T:System.Collections.ICollection" /> is greater than the available space from <paramref name="index" /> to the end of the destination <paramref name="array" />. </exception>
        /// <exception cref="T:System.ArgumentException">The type of the source <see cref="T:System.Collections.ICollection" /> cannot be cast automatically to the type of the destination <paramref name="array" />. </exception><filterpriority>2</filterpriority>
        public void CopyTo(Array array, int index)
        {
            var i = 0;
            foreach (var o in list)
            {
                if (i >= index)
                {
                    array.SetValue(o, i);
                }

                i++;
            }
        }

        public virtual int Count
        {
            get { return list.Count; }
        }

        public object SyncRoot
        {
            get { return syncRoot; }
        }

        public bool IsSynchronized
        {
            get { return isSynchronized; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="T:System.Collections.IList" /> has a fixed size.
        /// </summary>
        /// <returns>
        /// true if the <see cref="T:System.Collections.IList" /> has a fixed size; otherwise, false.
        /// </returns>
        public bool IsFixedSize
        {
            get { return false; }
        }

        private object syncRoot;
        private bool isSynchronized;

        public bool Remove(T item)
        {
            CheckReadOnly();
            var index = list.IndexOf(item);
            if(!OnRemoving(item, index))
            {
                return false;
            }
            if (list.Remove(item))
            {
                OnRemoved(item, index);
                return true;
            }
            return false;
        }

        #endregion

        #region IEnumerable<T> Members

        public void AddRange(IEnumerable<T> enumerable)
        {
            foreach (var o in enumerable)
            {
                Add(o);
            }
        }

        [DebuggerStepThrough]
        public IEnumerator<T> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        [DebuggerStepThrough]
        IEnumerator IEnumerable.GetEnumerator()
        {
            return list.GetEnumerator();
        }

        #endregion

        #region IEventedList Helpers

        private bool OnRemoving(T item, int index)
        {
            return FireCollectionChangingEvent(NotifyCollectionChangedAction.Remove, item, index);
        }

        private bool OnReplacing(T item, int index)
        {
            return FireCollectionChangingEvent(NotifyCollectionChangedAction.Replace, item, index);
        }

        private bool OnAdding(T item, int index)
        {
            return FireCollectionChangingEvent(NotifyCollectionChangedAction.Add, item, index);
        }

        private void OnReplaced(object item, object oldItem, int index)
        {
            UnsubscribeEvents(oldItem);
            SubscribeEvents(item);
            FireCollectionChangedEvent(NotifyCollectionChangedAction.Replace, item, index);
        }

        //[DebuggerStepThrough]
        private void OnAdded(object item, int index)
        {
            SubscribeEvents(item);
            FireCollectionChangedEvent(NotifyCollectionChangedAction.Add, item, index);
        }
        private void OnRemoved(T item, int index)
        {
            UnsubscribeEvents(item);
            FireCollectionChangedEvent(NotifyCollectionChangedAction.Remove, item, index);
        }

        private bool FireCollectionChangingEvent(NotifyCollectionChangedAction action, T item, int index)
        {
            if (CollectionChanging != null)
            {
                var args = new NotifyCollectionChangedEventArgs(action, item, index, -1);
                CollectionChanging(this, args);
                return !args.Cancel;
            }

            return true;
        }

        [DebuggerStepThrough]
        private void FireCollectionChangedEvent(NotifyCollectionChangedAction action, object item, int index)
        {
            if (CollectionChanged != null)
            {
                var args = new NotifyCollectionChangedEventArgs(action, item, index, -1);
                CollectionChanged(this, args);
            }
        }

        void Item_CollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            // forwards event to subscribers of the list
            if (CollectionChanged != null)
            {
                CollectionChanged(sender, args);
            }
        }

        void Item_CollectionChanging(object sender, NotifyCollectionChangedEventArgs args)
        {
            // forwards event to subscribers of the list
            if (CollectionChanging != null)
            {
                CollectionChanging(sender, args);
            }
        }

        void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // forwards event to subscribers of the list
            if (PropertyChanged != null)
            {
                PropertyChanged(sender, e);
            }
        }

        /// <summary>
        /// Unsubscribes PropertyChanged and CollectionChanged handlers it item supports them.
        /// </summary>
        /// <param name="item">The item to detach the handlers from</param>
        private void UnsubscribeEvents(object item)
        {
            var propChangedPublisher = item as INotifyPropertyChanged;
            if (propChangedPublisher != null)
            {
                propChangedPublisher.PropertyChanged -= Item_PropertyChangedDelegate;
            }

            var collChangedPublisher = item as INotifyCollectionChanged;
            if (collChangedPublisher != null)
            {
                collChangedPublisher.CollectionChanged -= Item_CollectionChangedDelegate;
                collChangedPublisher.CollectionChanging -= Item_CollectionChangingDelegate;
            }
        }

        /// <summary>
        /// Subscribes PropertyChanged and CollectionChanged it item supports them
        /// </summary>
        /// <param name="item">The item to detach the handlers from</param>
        private void SubscribeEvents(object item)
        {
            INotifyPropertyChanged propChangedPublisher = item as INotifyPropertyChanged;
            if (propChangedPublisher != null)
            {
                if (Item_PropertyChangedDelegate == null)
                {
                    InitializeDelegates();
                }
                propChangedPublisher.PropertyChanged += Item_PropertyChangedDelegate;
            }

            INotifyCollectionChanged collChangedPublisher = item as INotifyCollectionChanged;
            if (collChangedPublisher != null)
            {
                if (Item_PropertyChangedDelegate == null)
                {
                    InitializeDelegates();
                }
                collChangedPublisher.CollectionChanged += Item_CollectionChangedDelegate;
                collChangedPublisher.CollectionChanging += Item_CollectionChangingDelegate;
            }
        }

        #endregion

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public event NotifyCollectionChangedEventHandler CollectionChanging;
        public event PropertyChangedEventHandler PropertyChanged;
    }
}