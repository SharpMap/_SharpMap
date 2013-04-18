using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using log4net;

namespace DelftTools.Functions.Binding
{
    /// <summary>
    /// Represents a data <c>bindable</c>, customized view of two dimensional data
    /// </summary>
    public class MultiDimensionalArrayBindingList : IMultiDimensionalArrayBindingList
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(MultiDimensionalArrayBindingList));

        private IMultiDimensionalArray array;

        private IList<MultiDimensionalArrayBindingListRow> rows;

        private bool allowEdit = true;
        private bool allowNew = true;
        private bool allowRemove = true;

        private string[] columnNames;
        
        /// <summary>
        /// Initializes a new MultiDimensionalArrayBindingList from data.
        /// </summary>
        /// <param name="array">data of data.</param>
        public MultiDimensionalArrayBindingList(IMultiDimensionalArray array)
        {
            if (array.Rank != 2)
            {
                throw new ArgumentException("Supports only two dimensional arrays", "array");
            }

            Array = array;
            ColumnDimension = 0;
            RowDimension = 1;
        }

        public IMultiDimensionalArray Array
        {
            get { return array; } 
            set
            {
                array = value;

                int rowsCount = Array.Shape[ColumnDimension];
                rows = new List<MultiDimensionalArrayBindingListRow>(rowsCount);

                for (int i = 0; i < rowsCount; i++)
                {
                    rows.Add(new MultiDimensionalArrayBindingListRow(this, i, false));
                }
            }
        }

        /// <summary>
        /// Initializes a new MultiDimensionalArrayBindingList from data with custom column names.
        /// </summary>
        /// <param name="array">data of data.</param>
        /// <param name="columnNames">collection of column names.</param>
        public MultiDimensionalArrayBindingList(IMultiDimensionalArray array, object[] columnNames) : this(array)
        {
            if (columnNames.Length != Array.Shape[ColumnDimension])
            {
                throw new ArgumentException("column names must correspond to data columns.", "columnNames");
            }

            this.columnNames = new string[columnNames.Length];
            for (int i = 0; i < columnNames.Length; i++)
            {
                this.columnNames[i] = columnNames[i].ToString();
            }
        }

        public string[] ColumnNames
        {
            get
            {
                if (columnNames == null)
                {
                    columnNames = new string[Array.Shape[ColumnDimension]];
                    for (int i = 0; i < columnNames.Length; i++)
                    {
                        columnNames[i] = i.ToString();
                    }
                }
                return columnNames;
            }
        
            set { throw new NotImplementedException(); }
        }

        #region IBindingList Members

        public void AddIndex(PropertyDescriptor property)
        {
            // TODO:  Add MultiDimensionalArrayBindingList.AddIndex implementation
        }

        public bool AllowNew
        {
            get { return allowNew; }
        }

        public void ApplySort(PropertyDescriptor property, ListSortDirection direction)
        {
            // TODO:  Add MultiDimensionalArrayBindingList.ApplySort implementation
        }

        public PropertyDescriptor SortProperty
        {
            get
            {
                // TODO:  Add MultiDimensionalArrayBindingList.SortProperty getter implementation
                return null;
            }
        }

        public int Find(PropertyDescriptor property, object key)
        {
            // TODO:  Add MultiDimensionalArrayBindingList.Find implementation
            return 0;
        }

        public bool SupportsSorting
        {
            get
            {
                // TODO:  Add MultiDimensionalArrayBindingList.SupportsSorting getter implementation
                return false;
            }
        }

        public bool IsSorted
        {
            get
            {
                // TODO:  Add MultiDimensionalArrayBindingList.IsSorted getter implementation
                return false;
            }
        }

        public bool AllowRemove
        {
            get { return allowRemove; }
        }

        public bool SupportsSearching
        {
            get
            {
                // TODO:  Add MultiDimensionalArrayBindingList.SupportsSearching getter implementation
                return false;
            }
        }

        public ListSortDirection SortDirection
        {
            get
            {
                // TODO:  Add MultiDimensionalArrayBindingList.SortDirection getter implementation
                return new ListSortDirection();
            }
        }

        public event ListChangedEventHandler ListChanged;

        public bool SupportsChangeNotification
        {
            get { return true; }
        }

        public void RemoveSort()
        {
            // TODO:  Add MultiDimensionalArrayBindingList.RemoveSort implementation
        }

        public object AddNew()
        {
            int newRowIndex = Array.Shape[RowDimension];
            Array.InsertAt(RowDimension, newRowIndex);

            MultiDimensionalArrayBindingListRow newRow = new MultiDimensionalArrayBindingListRow(this, newRowIndex, true);
            rows.Add(newRow);

            return newRow;
        }

        public bool AllowEdit
        {
            get { return allowEdit; }
        }

        public void RemoveIndex(PropertyDescriptor property)
        {
            log.Warn("TODO: RemoveIndex is not implemented in the binding list");
        }

        public bool IsReadOnly
        {
            get { return allowRemove && AllowNew && AllowEdit; }
        }

        public object this[int index]
        {
            get { return rows[index]; }
            set { throw new NotImplementedException(); }
        }

        public void RemoveAt(int index)
        {
            rows.RemoveAt(index);
            Array.RemoveAt(RowDimension, index);
            OnListChanged(new ListChangedEventArgs(ListChangedType.ItemDeleted, index));
        }

        public void Insert(int index, object value)
        {
            log.Warn("TODO: Insert is not implemented in the binding list");
        }

        public void Remove(object value)
        {
            log.Warn("TODO: Remove is not implemented in the binding list");
        }

        public bool Contains(object value)
        {
            log.Warn("TODO: Contains is not implemented in the binding list");

            return false;
        }

        public void Clear()
        {
            log.Warn("TODO: Clear is not implemented in the binding list");
        }

        public int IndexOf(object value)
        {
            log.Warn("TODO: IndexOf is not implemented in the binding list");

            return 0;
        }

        public int Add(object value)
        {
            log.Warn("TODO: Add is not implemented in the binding list");

            return 0;
        }

        public bool IsFixedSize
        {
            get { return true; }
        }

        public bool IsSynchronized
        {
            get { return false; }
        }

        public int Count
        {
            get { return rows.Count; }
        }

        public void CopyTo(Array array, int index)
        {
            array.CopyTo(array, index);
        }

        public object SyncRoot
        {
            get { return array.SyncRoot; }
        }

        public int RowDimension { get; set; }

        public int ColumnDimension { get; set; }

        public IEnumerator GetEnumerator()
        {
            return rows.GetEnumerator();
        }

        #endregion

        public void Reset()
        {
            OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
        }

        internal void OnListChanged(ListChangedEventArgs e)
        {
            if (ListChanged != null)
            {
                ListChanged(this, e);
            }
        }

        public void FireChangedEvent(ListChangedType changeType, int index)
        {
            OnListChanged(new ListChangedEventArgs(changeType, index));
        }
    }
}