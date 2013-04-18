using System;
using System.Linq;
using System.Threading;
using log4net;

namespace DelftTools.Functions.Binding
{
    /// TODO: we have to make this class implement IEditableObject when we need transactional add/remove on function
    public class MultipleFunctionBindingListRow // : IEditableObject
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(MultipleFunctionBindingListRow));

        private readonly MultipleFunctionBindingList owner;

        internal MultipleFunctionBindingListRow(MultipleFunctionBindingList owner)
        {
            this.owner = owner;
        }

        private int GetColumnIndex(string columnName)
        {
            return owner.ColumnNames.ToList().IndexOf(columnName);
        }

        public object this[int columnIndex]
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
        public object this[string columnName]
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

        

        internal object GetColumnValue(int columnIndex)
        {
            if (!owner.Contains(this))
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
                // component column
                var component = GetComponentForColumnIndex(columnIndex);

                var values = component.Values;

                var index = Index;
                if(MultiDimensionalArrayHelper.IsIndexWithinShape(index, values.Shape))
                {
                    value = values[Index];
                }
            }

            return value;
        }

        private IVariable GetComponentForColumnIndex(int columnIndex)
        {
            var componentIndex = columnIndex - owner.Function.Arguments.Count;
            foreach (Function f in owner.Functions)
            {
                if (componentIndex >= f.Components.Count)
                {
                    componentIndex -= f.Components.Count;
                }
                else
                {
                    return f.Components[componentIndex];
                }
            }

            throw new ArgumentException("Column index does not belong to valid component");
        }

        internal void SetColumnValue(int columnIndex, object value)
        {
            if (!owner.Contains(this))
            {
                return;
            }

            while (owner.changing)
            {
                Thread.Sleep(0);
            }

            if (columnIndex < owner.Function.Arguments.Count)
            {
                //do not clear component values}
                if (value == DBNull.Value)
                {
                    return;
                }

                // argument column
                log.DebugFormat("Row value changed column: {0}, row: {1}", columnIndex, Index);
                var argumentIndex = Index[columnIndex];
                owner.Function.Arguments[columnIndex].Values[argumentIndex] = value;
            }
            else
            {
                // component column
                var component = GetComponentForColumnIndex(columnIndex);

                log.DebugFormat("Row value changed column: {0}, row: {1}", columnIndex, Index);
                component.Values[Index] = value;
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