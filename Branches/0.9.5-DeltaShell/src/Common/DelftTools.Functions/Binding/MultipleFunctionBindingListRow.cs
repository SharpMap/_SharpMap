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
            if (!owner.Contains(this) || !owner.Functions.Any())
            {
                return null;
            }

            object value = null;

            var variable = owner.GetVariableForColumnIndex(columnIndex);
            var values = variable.Values;

            if (columnIndex < owner.Function.Arguments.Count)
            {
                var argumentIndex = Index[columnIndex];

                if (values.Count > argumentIndex)
                {
                    value = values[argumentIndex];
                }
            }
            else
            {
                var index = Index;
                if (MultiDimensionalArrayHelper.IsIndexWithinShape(index, values.Shape))
                {
                    value = values[Index];
                }
            }

            return value;
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

            log.DebugFormat("Row value changed column: {0}, row: {1}", columnIndex, Index);

            var variable = owner.GetVariableForColumnIndex(columnIndex);

            if (columnIndex < owner.Function.Arguments.Count)
            {
                //do not clear argument values
                if (value == DBNull.Value)
                {
                    return;
                }

                // argument column
                variable.Values[Index[columnIndex]] = value;
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
    }
}