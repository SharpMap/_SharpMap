using System;
using System.ComponentModel;

namespace DelftTools.Functions.Binding
{
    /// <summary>
    /// Represents a row from data view.
    /// </summary>
    public class MultiDimensionalArrayBindingListRow : ICustomTypeDescriptor, IEditableObject, IDataErrorInfo
    {
        private readonly int index;
        private readonly MultiDimensionalArrayBindingList owner;

        private bool addingNew;
        private bool addingNewCancelled;

        internal MultiDimensionalArrayBindingListRow(MultiDimensionalArrayBindingList owner, int index, bool addingNew)
        {
            this.owner = owner;
            this.index = index;
            this.addingNew = addingNew;
        }

        #region ICustomTypeDescriptor Members

        public TypeConverter GetConverter()
        {
            // TODO:  Add ArrayColumn.GetConverter implementation
            return null;
        }

        public EventDescriptorCollection GetEvents(Attribute[] attributes)
        {
            return EventDescriptorCollection.Empty;
        }

        EventDescriptorCollection ICustomTypeDescriptor.GetEvents()
        {
            return EventDescriptorCollection.Empty;
        }

        public string GetComponentName()
        {
            return null;
        }

        public object GetPropertyOwner(PropertyDescriptor pd)
        {
            return owner;
        }

        public AttributeCollection GetAttributes()
        {
            return AttributeCollection.Empty;
        }

        public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {
            int column = owner.Array.Shape[owner.ColumnDimension];
            Type type = typeof(object);

            if(owner.Array.GetType().IsGenericType) // IMultiDimensionalArray<T>
            {
                type = owner.Array.GetType().GetGenericArguments()[0];
            }

            var propertyDescriptors = new PropertyDescriptor[column];
            for (int i = 0; i < column; i++)
            {
                propertyDescriptors[i] = new MultiDimensionaArrayPropertyDescriptor(owner.ColumnNames[i], type, i);
            }
        
            return new PropertyDescriptorCollection(propertyDescriptors);
        }

        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties()
        {
            return GetProperties(null);
        }

        public object GetEditor(Type editorBaseType)
        {
            return null;
        }

        public PropertyDescriptor GetDefaultProperty()
        {
            return null;
        }

        public EventDescriptor GetDefaultEvent()
        {
            return null;
        }

        public string GetClassName()
        {
            return GetType().Name;
        }

        #endregion

        #region IDataErrorInfo Members

        public string this[string columnName]
        {
            get
            {
                // TODO:  Add MultiDimensionalArrayBindingListRow.this getter implementation
                return null;
            }
        }

        public string Error
        {
            get
            {
                // TODO:  Add MultiDimensionalArrayBindingListRow.Error getter implementation
                return null;
            }
        }

        #endregion

        #region IEditableObject Members

        public void EndEdit()
        {
        }

        public void CancelEdit()
        {
            if(addingNew)
            {
                addingNewCancelled = true;
                owner.RemoveAt(index);
            }
        }

        public void BeginEdit()
        {
        }

        #endregion

        internal object GetColumn(int index)
        {
            if(addingNewCancelled)
            {
                return null;
            }

            int[] arrayIndex = new int[owner.Array.Rank];
            arrayIndex[owner.RowDimension] = this.index;
            arrayIndex[owner.ColumnDimension] = index;

            return owner.Array[arrayIndex];
        }

        internal void SetColumnValue(int index, object value)
        {
            if (addingNewCancelled)
            {
                return;
            }

            int[] arrayIndex = new int[owner.Array.Rank];
            arrayIndex[owner.RowDimension] = this.index;
            arrayIndex[owner.ColumnDimension] = index;
            
            owner.Array[arrayIndex] = value;
        }
    }
}