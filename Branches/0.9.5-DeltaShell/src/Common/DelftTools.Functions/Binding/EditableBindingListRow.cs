using System;
using IEditableObject = System.ComponentModel.IEditableObject;

namespace DelftTools.Functions.Binding
{
    public abstract class EditableBindingListRow : IEditableObject
    {
        //abstract or delegates?
        protected abstract void OnSetColumnValue(int columnIndex, object value);
        protected abstract object OnGetColumnValue(int columnIndex);
        protected abstract int GetNumberOfColumns();

        #region IEditableObject Stuff
        
        public bool InAddMode { get; set; }
        private bool inEditMode;
        private object[] transientValues;
        private bool[] transientValuesChanged;
        public void BeginEdit()
        {
            //BeginEdit is called by many sources, most of which don't actually plan to edit anything. As a result they also don't call EndEdit. 
            //However, while in EditMode, the FunctionBindingListRow no longer updates when the datasource updates.
            //In short: don't go into EditMode here and enter EditMode only when real changes are received (BeginEditManually).
        }

        public void EndEdit()
        {
            if (inEditMode)
            {
                inEditMode = false;
                CommitTransientValues();
            }
        }

        public void CancelEdit()
        {
            if (inEditMode)
            {
                inEditMode = false;
            }
        }

        private void BeginEditManually()
        {
            if (!inEditMode)
            {
                FillTransientValues();
                inEditMode = true;
            }
        }

        protected void SetColumnValue(int columnIndex, object value)
        {
            if (!inEditMode)
            {
                BeginEditManually();
            }

            SetColumnValueTransient(columnIndex, value);
        }

        protected object GetColumnValue(int columnIndex)
        {
            if (inEditMode)
            {
                return GetColumnValueTransient(columnIndex);
            }
            return OnGetColumnValue(columnIndex);
        }

        private void SetColumnValueTransient(int columnIndex, object value)
        {
            transientValues[columnIndex] = value;
            transientValuesChanged[columnIndex] = true;
        }

        private object GetColumnValueTransient(int columnIndex)
        {
            return transientValues[columnIndex];
        }

        private void FillTransientValues()
        {
            if (inEditMode)
            {
                throw new NotSupportedException("Can only fill when not yet in edit mode"); //make sure inEditMode is set to true _after_ this call
            }

            var numberOfColumns = GetNumberOfColumns();
            transientValues = new object[numberOfColumns];
            transientValuesChanged = new bool[numberOfColumns];
            
            if (!InAddMode)
            {
                for (int i = 0; i < transientValues.Length; i++)
                {
                    transientValues[i] = OnGetColumnValue(i);
                }
            }
        }

        protected virtual void CommitTransientValues()
        {
            try
            {
                if (inEditMode)
                {
                    throw new NotSupportedException("Can only commit when no longer in edit mode");
                    //make sure inEditMode is set to false first!
                }

                //processed in reversed order to make sure the argument is set last (as that may trigger sorting)
                for (int i = transientValues.Length - 1; i >= 0; i--)
                {
                    if (transientValuesChanged[i])
                    {
                        OnSetColumnValue(i, transientValues[i]);
                    }
                }
            }
            finally
            {
                InAddMode = false;
            }
        }

        #endregion
    }
}
