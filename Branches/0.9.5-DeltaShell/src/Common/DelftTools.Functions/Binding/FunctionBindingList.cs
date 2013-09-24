using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using DelftTools.Utils.Aop;
using DelftTools.Utils.Collections;
using DelftTools.Utils.Editing;
using DelftTools.Utils.Threading;
using log4net;
using IEditableObject = DelftTools.Utils.Editing.IEditableObject;

namespace DelftTools.Functions.Binding
{
    /// <summary>
    /// Adapter to IFunction to IBindinglist
    /// 
    /// Removed code that introduced multithreading while code is not safe:
    /// - removed SynchronizeWaitMethod which is de facto a call to Application.DoEvents; this messes up the code.
    ///       Do not call SynchronizeWaitMethod (= application.DoEvents)! 
    ///       1 - It changes program flow if there are events in the queue
    ///           Difficult to test. Run CoverageViewTest.ShowWithNetworkCoverage() add new networklocation
    ///           to bottom and close editor by clicking in the second column. DevExpress will mix old and new
    ///           focused column.
    ///           The only safe case to do this (DoEvents) is while reading data (eg during a draw operation).
    ///       2 - It can completely kill performance 
    ///           Create tableview with 1000+ rows; select number of rows; press delete
    /// 
    /// - removed delayedEventHandler
    ///   causes problem in coverage view: DelayedEventHandler triggered by ValuesChanged causes BindingsList.InsertItem
    ///   which messes up program flow
    ///   Again very difficuylt to test
    ///      Startup CoverageViewTest.ShowWithNetworkCoverage()
    ///      In last Row call up NetworkLocationEditor by clicking arrow down button; enter a new valid 
    ///      networklocation and press the arrow up button. Processing is inerupted which results in 2 empty rows and an 
    ///      invalid funcion.
    /// 
    /// TODO: make it IBindingList<IFunctionValue> (IFunctionValue is a combination of ComponentValues + ArgumentValues)
    /// </summary>
    public class FunctionBindingList : BindingList<FunctionBindingListRow>, IFunctionBindingList, ISynchronizeInvoke, IDisposable, IEditableObject
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(FunctionBindingList));

        private IFunction function;
        private IMultiDimensionalArray values;
        internal bool changing;
        internal bool changingFromGui;

        public override string ToString()
        {
            return ID;
        }

        public FunctionBindingList(IFunction function)
        {
            ID = Guid.NewGuid().ToString();

            Function = function;

            // TODO: do not access DelayedEventHandlerController here directly! Use DelayedEventHandler with FullRefresh instead
            DelayedEventHandlerController.FireEventsChanged += DelayedEventHandlerController_FireEventsChanged;
        }
        
        /// <summary>
        /// Used to allow thread-safe behaviour (e.g. in Windows.Forms).
        /// </summary>
        public ISynchronizeInvoke SynchronizeInvoke { get; set; }

        #region IFunctionBindingList
        
        public virtual string[] ColumnNames
        {
            get
            {
                while (changing)
                {
                    Wait();
                }

                var variables = Function.Arguments.Concat(Function.Components);
                return variables.Select(v => v.Name).ToArray();
            }
        }

        public virtual string[] DisplayNames
        {
            get { return ColumnNames; }
        }
        
        private void Wait()
        {
            if (SynchronizeWaitMethod != null)
            {
                SynchronizeWaitMethod();
            }
            else
            {
                Thread.Sleep(0);
            }
        }
        
        public IFunction Function
        {
            get { return function; }
            set 
            {
                if(function != null)
                {
                    function.ValuesChanged -= OnFunctionValuesChanged;
                    function.Components.CollectionChanged -= ComponentsCollectionChanged;
                    function.Arguments.CollectionChanged -= ArgumentsCollectionChanged;
                    var propertyChanged = function as INotifyPropertyChanged;
                    if (propertyChanged != null)
                    {
                        propertyChanged.PropertyChanged -= OnFunctionPropertyChanged;
                    }
                }

                function = value;

                if (function != null)
                {
                    Fill();

                    function.ValuesChanged += OnFunctionValuesChanged;
                    function.Components.CollectionChanged += ComponentsCollectionChanged;
                    function.Arguments.CollectionChanged += ArgumentsCollectionChanged;
                    var propertyChanged = function as INotifyPropertyChanged;
                    if (propertyChanged != null)
                    {
                        propertyChanged.PropertyChanged += OnFunctionPropertyChanged;
                    }
                }
            }
        }

        private void OnFunctionPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (changing)
            {
                return;
            }

            if (Equals(sender, function) && e.PropertyName == "IsEditing")
            {
                if (function.IsNestedEditingDone()) // 'true' endedit
                {
                    Refresh();
                }
            }
        }

        private void Fill()
        {
            while (changing)
            {
                Wait();
            }

            changing = true;

            AllowNew = function.Arguments.Count <= 1 && function.Components.Count >= 1 && function.IsEditable;
            AllowEdit = function.Arguments.Count <= 1 && function.Components.Count >= 1 && function.IsEditable;
            AllowRemove = function.Arguments.Count <= 1 && function.Components.Count >= 1 && function.IsEditable;

            // fill in binding list rows
            RaiseListChangedEvents = false;

            Clear();

            if (function.Components.Count > 0)
            {
                values = function.Components[0].Values;
                for (var i = 0; i < values.Count; i++)
                {
                    //We use AddNewCore since AddNew is SLOW because it uses IndexOf to determine the
                    //insertion index (to support cancellation). Add some reflection if we also need this.
                    AddNewCore(); 
                }
            }
            RaiseListChangedEvents = true;

            // raise Reset in subscribers
            ResetBindings();
            changing = false;
        }

        void ArgumentsCollectionChanged(object sender, NotifyCollectionChangingEventArgs e)
        {
            if (function == null || function.IsEditing)
            {
                return;
            }

            changing = true;
            Clear();
            ResetBindings();
            changing = false;
        }

        void ComponentsCollectionChanged(object sender, NotifyCollectionChangingEventArgs e)
        {
            if (function == null || function.IsEditing)
            {
                return;
            }

            changing = true;
            Clear();
            ResetBindings();
            changing = false;
        }

        #endregion

        /// <summary>
        /// Returns multi-dimensional row index of the row based on the current sort order, filters, etc.
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        public int[] GetMultiDimensionalRowIndex(FunctionBindingListRow row)
        {
            return MultiDimensionalArrayHelper.GetIndex(GetIndexOfRow(row), values.Stride);
        }

        protected override void OnAddingNew(AddingNewEventArgs e)
        {
            e.NewObject = CreateEmptyBindingListRow();
        }

        private void OnFunctionValuesChanged(object sender, FunctionValuesChangingEventArgs e)
        {
            if (function == null || function.IsEditing)
            {
                return;
            }

            //should not a delayedEventHandler be used?
            if(!DelayedEventHandlerController.FireEvents)
            {
                return;
            }

            if (changingFromGui)
            {
                return;
            }

            while (changing)
            {
                Wait();
            }

            changing = true;
            
            //find out the property descriptor for this function and raise event.
            switch (e.Action)
            {
                case NotifyCollectionChangeAction.Reset:
                    Clear();
                    changing = false;
                    Fill();
                    changing = true;
                    break;
                case NotifyCollectionChangeAction.Replace:
                    OnListChanged(new ListChangedEventArgs(ListChangedType.ItemChanged, e.Index));
                    break;
                case NotifyCollectionChangeAction.Add:
                    OnFunctionValuesAdd(e);
                    break;
                case NotifyCollectionChangeAction.Remove:
                    OnFunctionValuesRemoved(e);
                    break;
                default:
                    changing = false;
                    throw new NotImplementedException(e.ToString());
            }

            if (FunctionValuesChanged != null)
            {
                FunctionValuesChanged(sender, e);
            }

            changing = false;
        }

        protected virtual void OnFunctionValuesRemoved(FunctionValuesChangingEventArgs e)
        {
            if (function == null || function.IsEditing)
            {
                return;
            }

            int argumentIndex = function.Arguments.IndexOf((IVariable)e.Function);

            if (!function.Arguments.Contains((IVariable) e.Function))
            {
                return;
            }

            var indicesToRemove = new List<int>();

            // 1d case, faster implementation
            if (function.Arguments.Count == 1)
            {
                var index = e.Index;
                for (var i = e.Items.Count - 1; i >= 0; i--)
                {
                    indicesToRemove.Add(index + i);
                }
            }
            else
            {
                for (var i = Count - 1; i >= 0; i--)
                {
                    if (this[i].Index[argumentIndex] == e.Index)
                    {
                        indicesToRemove.Add(i);
                    }
                }
            }

            if (indicesToRemove.Count > 0)
                RemoveRows(indicesToRemove);
        }

        [InvokeRequired]
        private void RemoveRows(IList<int> indices)
        {
            var couldRemoveBefore = AllowRemove;
            AllowRemove = true;
            for (int i = 0; i < indices.Count; i++)
            {
                RemoveAt(indices[i]);
            }
            AllowRemove = couldRemoveBefore;
        }

        protected virtual void OnFunctionValuesAdd(FunctionValuesChangingEventArgs e)
        {
            if (function == null || function.IsEditing)
            {
                return;
            }
            
            if (!function.Arguments.Contains((IVariable)e.Function))
            {
                return;
            }

            // do nothing if at least 1 of arguments is empty
            if(function.Arguments.Any(a => a.Values.Count == 0))
            {
                return;
            }

            var shape = function.Components[0].Values.Shape;
            var valuesPerRow = MultiDimensionalArrayHelper.GetTotalLength(shape.Skip(1).ToArray());
            var numRowsAdded = e.Items.Count;
            var rowsToAdd = numRowsAdded*valuesPerRow;
            if (rowsToAdd > 0)
                InsertRows(shape, rowsToAdd, e.Index);
        }

        [InvokeRequired]
        private void InsertRows(int[] shape, int rowsToAdd, int listIndex)
        {
            var index = new int[shape.Length];
            for (var i = 0; i < rowsToAdd; i++)
            {
                Insert(listIndex, CreateEmptyBindingListRow());
                MultiDimensionalArrayHelper.IncrementIndex(index, shape, 0);
            }
        }

        protected virtual FunctionBindingListRow CreateEmptyBindingListRow()
        {
            return new FunctionBindingListRow(this);
        }
        
        /// <summary>
        /// Adds item to the binded function. The new item should be unique.
        /// </summary>
        /// <returns></returns>
        protected override object AddNewCore()
        {
            object newObject = base.AddNewCore();

            if (!changing) //if not part of fill
            {
                var editableRow = newObject as EditableBindingListRow;
                if (editableRow != null)
                {
                    editableRow.InAddMode = true;
                }
            }

            return newObject;
        }

        public void AddIndex(PropertyDescriptor property)
        {
            //log.Debug("AddIndex");
        }

        public void RemoveIndex(PropertyDescriptor property)
        {
            //log.Debug("RemoveIndex");
        }

        public override void EndNew(int itemIndex)
        {
            var wasAdding = itemIndex >= 0 && this[itemIndex].InAddMode;
            
            if (wasAdding)
                CancelNew(itemIndex);
            else
                base.EndNew(itemIndex);
        }

        public override void CancelNew(int itemIndex)
        {
            changing = true;

            if (itemIndex >= 0)
            {
                this[itemIndex].InAddMode = false;
            }

            base.CancelNew(itemIndex);
            changing = false;
        }

        protected override void RemoveItem(int index)
        {
            var row = (this[index] as EditableBindingListRow);
            deletedRowIsTransient = row != null && row.InAddMode; //prevent delete in function

            base.RemoveItem(index);

            deletedRowIsTransient = false;
        }

        private bool deletedRowIsTransient;

        [InvokeRequired]
        protected override void OnListChanged(ListChangedEventArgs e)
        {
            UpdateIndices(e.ListChangedType,e.NewIndex);

            if (changing)
            {
                base.OnListChanged(e);
                return;
            }

            changing = true;

            changingFromGui = true;

            var editableObject = function as IEditableObject;

            switch(e.ListChangedType)
            {
                case ListChangedType.ItemAdded:
                    // handled in row
                    break;
                case ListChangedType.ItemChanged:
                    break;
                case ListChangedType.Reset:
                    break;
                case ListChangedType.ItemDeleted:
                    
                    if (!AllowRemove)
                    {
                        break; //bug in Xtragrid?! We should never get here, see comment in TOOLS-4415
                    }

                    if (deletedRowIsTransient)
                    {
                        break;
                    }

                    if (editableObject != null) editableObject.BeginEdit(new DefaultEditAction("Removing data row"));

                    if (function.IsIndependent)
                    {
                        foreach (var variable in function.Components)
                        {
                            variable.Values.RemoveAt(0, e.NewIndex);
                        }
                    }
                    else
                    {
                        // we can remove values only in 1D functions, otherwise list is read-only
                        function.Arguments[0].Values.RemoveAt(0, e.NewIndex);
                    }

                    if (editableObject != null) editableObject.EndEdit();

                    break;
                case ListChangedType.ItemMoved:
                    break;
            }

            changing = false;

            changingFromGui = false;

            base.OnListChanged(e);
        }

        private bool rowToIndexMappingDirty = true;
        
        private void UpdateIndices(ListChangedType type, int index)
        {
            if (type != ListChangedType.ItemChanged)
            {
                if (type == ListChangedType.ItemAdded && index == rowToIndexMapping.Count) //added to end
                {
                    rowToIndexMapping[this[index]] = index;
                    return;
                }
                rowToIndexMappingDirty = true;
            }
        }

        private Dictionary<FunctionBindingListRow, int> rowToIndexMapping = new Dictionary<FunctionBindingListRow, int>();
        
        public int GetIndexOfRow(FunctionBindingListRow row)
        {
            RefreshRowToIndexMappingIfNeeded();

            if (rowToIndexMapping.ContainsKey(row))
            {
                return rowToIndexMapping[row];
            }
            return -1;
        }

        private void RefreshRowToIndexMappingIfNeeded()
        {
            if (rowToIndexMappingDirty)
            {
                rowToIndexMapping.Clear();
                for (int i = 0; i < Count; i++)
                {
                    rowToIndexMapping[this[i]] = i;
                }
                rowToIndexMappingDirty = false;
            }
        }

        protected override void ClearItems()
        {
            if(changing ){base.ClearItems();
                return;
            }

            if (Count == 0) return;

            for (var i = Count - 1; i >= 0; i--)
            {
                var item = this[i];
                Remove(item);
            }        
        }

        #region ITypedList

        public string GetListName(PropertyDescriptor[] listAccessors)
        {
            return Function.Name;
        }

        public virtual PropertyDescriptorCollection GetItemProperties(PropertyDescriptor[] listAccessors)
        {
            var variables = Function.Arguments.Concat(Function.Components);
            var descriptors = variables.Select(GetRowPropertyDescriptor);

            return new PropertyDescriptorCollection(descriptors.ToArray());
        }

        private FunctionBindingListPropertyDescriptor GetRowPropertyDescriptor(IVariable v, int i)
        {
            if (v.IsIndependent && v.Parent != null) // filtered argument
            {
                return new FunctionBindingListPropertyDescriptor(v.Name, v.DisplayName, v.ValueType, i, true);
            }
            
            return new FunctionBindingListPropertyDescriptor(v.Name, v.DisplayName, v.ValueType, i);
        }

        #endregion

        private class AsyncResult : IAsyncResult
        {
            public bool IsCompleted { get; private set; }
            public WaitHandle AsyncWaitHandle { get; private set; }
            public object AsyncState { get; private set; }
            public bool CompletedSynchronously { get; private set; }
        }

        public IAsyncResult BeginInvoke(Delegate method, object[] args)
        {
            if(SynchronizeInvoke != null)
            {
                return SynchronizeInvoke.BeginInvoke(method, args);
            }

            return new AsyncResult();
        }

        public object EndInvoke(IAsyncResult result)
        {
            if(SynchronizeInvoke != null)
            {
                return SynchronizeInvoke.EndInvoke(result);
            }

            return null;
        }

        public object Invoke(Delegate method, object[] args)
        {
            if(SynchronizeInvoke != null)
            {
                SynchronizeInvoke.Invoke(method, args);
            }

            return null;
        }

        public bool InvokeRequired
        {
            get
            {
                return SynchronizeInvoke != null && SynchronizeInvoke.InvokeRequired;
            }
        }

        public Action SynchronizeWaitMethod { get; set; }

        public event EventHandler<FunctionValuesChangingEventArgs> FunctionValuesChanged;

        public IVariable GetVariableForColumnIndex(int absoluteIndex)
        {
            if (absoluteIndex < Function.Arguments.Count)
            {
                return Function.Arguments[absoluteIndex];
            }
            var componentIndex = absoluteIndex - Function.Arguments.Count;
            if (componentIndex < Function.Components.Count)
            {
                return Function.Components[componentIndex];
            }
            return null;
        }

        private bool disposed;
        private string ID;

        public virtual void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposed)
                return;

            var hadAcquired = Monitor.TryEnter(disposeLock, 200); //prevent deadlock
            try
            {
                if (disposing)
                    DelayedEventHandlerController.FireEventsChanged -= DelayedEventHandlerController_FireEventsChanged;

                Function = null;
                disposed = true;
            }
            finally
            {
                if (hadAcquired)
                    Monitor.Exit(disposeLock);
            }
        }

        public void Refresh()
        {
            if (function == null) return;
            Fill();
        }

        void DelayedEventHandlerController_FireEventsChanged(object sender, EventArgs e)
        {
            lock (disposeLock)
            {
                if (DelayedEventHandlerController.FireEvents && !disposed)
                {
                    if (function == null) return;
                    Fill();
                }
            }
        }

        private readonly object disposeLock = new object();

        #region IEditableObject

        public bool IsEditing
        {
            get { return function.IsEditing; }
        }

        public bool EditWasCancelled
        {
            get { return function.EditWasCancelled; }
        }

        public IEditAction CurrentEditAction
        {
            get { return function.CurrentEditAction; }
        }

        public void BeginEdit(IEditAction action)
        {
            function.BeginEdit(action);
        }

        public void EndEdit()
        {
            function.EndEdit();
        }

        public void CancelEdit()
        {
            function.CancelEdit();
        }

        #endregion
    }
}