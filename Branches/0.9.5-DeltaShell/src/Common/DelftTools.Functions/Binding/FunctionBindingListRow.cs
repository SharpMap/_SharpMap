using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DelftTools.Utils.Collections;
using log4net;
using DefaultEditAction = DelftTools.Utils.Editing.DefaultEditAction;

namespace DelftTools.Functions.Binding
{
    /// TODO: we have to make this class implement IEditableObject when we need transactional add/remove on function
    public class FunctionBindingListRow : EditableBindingListRow
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(FunctionBindingListRow));

        protected readonly FunctionBindingList owner;

        public FunctionBindingListRow(FunctionBindingList owner)
        {
            this.owner = owner;
        }

        protected virtual int GetColumnIndex(string columnName)
        {
            return owner.ColumnNames.ToList().IndexOf(columnName);
        }

        public virtual object this[int columnIndex]
        {
            get
            {
                return GetColumnValue(columnIndex);
            }
            set
            {
                SetColumnValue(columnIndex, value);
            }
        }

        public virtual object this[string columnName]
        {
            get
            {
                return this[GetColumnIndex(columnName)];
            }
            set
            {
                this[GetColumnIndex(columnName)] = value;
            }
        }

        protected override object OnGetColumnValue(int columnIndex)
        {
            if (owner.GetIndexOfRow(this) == -1 || owner.Function == null)
            {
                return null;
            }

            object value = null;

            if (columnIndex < owner.Function.Arguments.Count)
            {
                if (columnIndex >= Index.Count())
                {
                    return null;
                }

                var values = owner.GetVariableForColumnIndex(columnIndex).Values;

                var argumentIndex = Index[columnIndex];
                if (values.Count > argumentIndex)
                {
                    value = values[argumentIndex];
                }
            }
            else
            {
                var componentIndex = columnIndex - owner.Function.Arguments.Count;
                if (componentIndex >= owner.Function.Components.Count)
                {
                    return value;
                }

                var values = owner.GetVariableForColumnIndex(columnIndex).Values;

                // component column
                var index = Index;
                if(MultiDimensionalArrayHelper.IsIndexWithinShape(index, values.Shape))
                {
                    value = values[index];
                }
            }

            return value;
        }

        protected override int GetNumberOfColumns()
        {
            return owner.Function.Arguments.Count + owner.Function.Components.Count;
        }

        protected override void OnSetColumnValue(int columnIndex, object value)
        {
            if (owner.GetIndexOfRow(this) == -1)
            {
                return;
            }

            while (owner.changing)
            {
                Thread.Sleep(0);
            }

            var variable = owner.GetVariableForColumnIndex(columnIndex);

            if (columnIndex < owner.Function.Arguments.Count)
            {
                // do not clear component values}
                if (value == DBNull.Value)
                {
                    return;
                }

                // argument column
                var argumentIndex = Index[columnIndex];
                variable.Values[argumentIndex] = value;
            }
            else
            {
                // component column
                variable.Values[Index] = value;
            }
        }

        public int[] Index
        {
            get
            {
                return owner.GetMultiDimensionalRowIndex(this);
            }
        }

        protected override void CommitTransientValues()
        {
            var editableObject = owner.Function as Utils.Editing.IEditableObject;

            if (editableObject != null) editableObject.BeginEdit(new DefaultEditAction("Committed changes to row"));

            var exception = false;
            try
            {
                if (InAddMode)
                {
                    AddSliceToFunction();
                }
                base.CommitTransientValues();
            }
            catch(Exception)
            {
                exception = true;
                throw;
            }
            finally
            {
                if (editableObject != null)
                {
                    if (exception)
                    {
                        editableObject.CancelEdit();
                    }
                    else
                    {
                        editableObject.EndEdit();    
                    }
                }
            }
        }

        private void AddSliceToFunction()
        {
            owner.changingFromGui = true;

            // remember generate unique values flags and set to true
            IList<bool> generateUniqueValueForDefaultValues = new List<bool>();
            owner.Function.Arguments.ForEach(a =>
                                                 {
                                                     generateUniqueValueForDefaultValues.Add(
                                                         a.GenerateUniqueValueForDefaultValue);
                                                     a.GenerateUniqueValueForDefaultValue = true;
                                                 });

            // actual work being done here
            owner.Function.Arguments[0].Values.InsertAt(0, owner.GetIndexOfRow(this));

            // reset generate unique values flags
            for (int i = 0; i < generateUniqueValueForDefaultValues.Count; i++)
            {
                owner.Function.Arguments[i].GenerateUniqueValueForDefaultValue = generateUniqueValueForDefaultValues[i];
            }

            owner.changingFromGui = false;
        }
    }
}