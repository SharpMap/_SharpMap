using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using log4net;

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
                var argumentIndex = Index[columnIndex];

                //log.DebugFormat("GetColumnValue: {0}, {1}", columnIndex, argumentIndex);

                var values = owner.Function.Arguments[columnIndex].Values;
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

                // component column
                var component = owner.Function.Components[componentIndex];
                var values = component.Values;

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

            var index = Index;

            if (columnIndex < owner.Function.Arguments.Count)
            {
                //do not clear component values}
                if (value == DBNull.Value)
                {
                    return;
                }

                // argument column
                //log.DebugFormat("Row value changed column: {0}, row: {1}", columnIndex, index);
                var argumentIndex = Index[columnIndex];
                owner.Function.Arguments[columnIndex].Values[argumentIndex] = value;
            }
            else
            {
                // component column
                //log.DebugFormat("Row value changed column: {0}, row: {1}", columnIndex, index);
                var component = owner.Function.Components[columnIndex - owner.Function.Arguments.Count];
                component.Values[index] = value;
            }
        }

        public int[] Index
        {
            get
            {
                return owner.GetMultiDimensionalRowIndex(this);
            }
        }
    }
}