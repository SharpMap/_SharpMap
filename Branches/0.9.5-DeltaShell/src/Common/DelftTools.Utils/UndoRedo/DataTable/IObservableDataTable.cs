using System;
using System.Data;

namespace DelftTools.Utils.UndoRedo.DataTable
{
    public interface IObservableDataTable
    {
        DataRow NewRow();
        DataRowCollection Rows { get; }
        DataColumnCollection Columns { get; }

        event DataColumnChangeEventHandler ColumnChanging;
        event DataColumnChangeEventHandler ColumnChanged;

        Action<DataRowChangeEventArgs> BeforeRowChanging { get; set; }
        Action<DataRowChangeEventArgs> AfterRowChanged { get; set; }
    }
}