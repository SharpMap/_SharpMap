using System.Data;
using System.Linq;
using DelftTools.Utils.Editing;

namespace DelftTools.Utils.UndoRedo.DataTable
{
    /// <summary>
    /// Used to monitor changes to DataTable for Undo/Redo
    /// </summary>
    public class DataTableObserver
    {
        public static bool TrackChanges { get; set; }

        public IEditableObject EditableObject { get; set; }

        private IObservableDataTable observable;
        
        public IObservableDataTable Observable
        {
            get { return observable; }
            set
            {
                if (observable != null)
                {
                    observable.BeforeRowChanging = null;
                    observable.AfterRowChanged = null;
                }
                observable = value;
                if (observable != null)
                {
                    observable.BeforeRowChanging = ObservableRowChanging;
                    observable.AfterRowChanged = ObservableRowChanged;
                }
            }
        }
        
        void ObservableRowChanging(DataRowChangeEventArgs e)
        {
            if (e.Action == DataRowAction.Add)
            {
                EditableObject.BeginEdit(new RowAddEditAction(Observable));
            }
            else if (e.Action == DataRowAction.Delete)
            {
                EditableObject.BeginEdit(new RowDeleteEditAction(Observable, e.Row));
                SetRowIndexInCurrentAction(e);
            }
            else if (e.Action == DataRowAction.Change)
            {
                EditableObject.BeginEdit(new RowChangeEditAction(Observable, e.Row));
                SetRowIndexInCurrentAction(e);
            }
        }

        void ObservableRowChanged(DataRowChangeEventArgs e)
        {
            if (e.Action != DataRowAction.Add && e.Action != DataRowAction.Delete && e.Action != DataRowAction.Change) 
                return;

            if (e.Action == DataRowAction.Add || e.Action == DataRowAction.Change)
            {
                SetRowIndexInCurrentAction(e);
            }

            EditableObject.EndEdit();
        }

        private void SetRowIndexInCurrentAction(DataRowChangeEventArgs e)
        {
            var rowIndex = Observable.Rows.IndexOf(e.Row);
            ((RowAction) EditableObject.CurrentEditAction).RowIndex = rowIndex;
        }

        private abstract class RowAction : EditActionBase
        {
            protected RowAction(IObservableDataTable observable, string name)
                : base(name)
            {
                DataTable = observable;
            }

            protected IObservableDataTable DataTable { get; set; }
            public int RowIndex { get; set; }
            public override bool HandlesRestore { get { return true; } }
        }

        private class RowAddEditAction : RowAction
        {
            public RowAddEditAction(IObservableDataTable observable)
                : base(observable, "Add row") { }
            
            public override void Restore()
            {
                DataTable.Rows.RemoveAt(RowIndex);
            }
        }

        private class RowDeleteEditAction : RowAction
        {
            private object[] RowData { get; set; }

            public RowDeleteEditAction(IObservableDataTable observable, DataRow row)
                : base(observable, "Delete row")
            {
                RowData = (object[])row.ItemArray.Clone();
            }
            
            public override void Restore()
            {
                var newRow = DataTable.NewRow();
                newRow.ItemArray = RowData;
                DataTable.Rows.InsertAt(newRow, RowIndex);
            }
        }

        private class RowChangeEditAction : RowAction
        {
            private object[] RowData { get; set; }

            public RowChangeEditAction(IObservableDataTable observable, DataRow row)
                : base(observable, "Change row")
            {
                RowData = DataTable.Columns
                                   .OfType<DataColumn>()
                                   .Select(c => row[c, DataRowVersion.Current])
                                   .ToArray();
            }

            public override void Restore()
            {
                var readonlyColumns = DataTable.Columns.OfType<DataColumn>().Where(c => c.ReadOnly).ToList();
                foreach(var readonlyColumn in readonlyColumns)
                {
                    readonlyColumn.ReadOnly = false;
                }
                try
                {
                    DataTable.Rows[RowIndex].ItemArray = RowData;
                }
                finally
                {
                    foreach (var readonlyColumn in readonlyColumns)
                    {
                        readonlyColumn.ReadOnly = true;
                    }
                }
            }
        }
    }
}