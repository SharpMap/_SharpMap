using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using DelftTools.Utils.Aop;
using DelftTools.Utils.Collections;
using DelftTools.Utils.Threading;
using log4net;

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
    public class FunctionBindingList : BindingList<FunctionBindingListRow>, IFunctionBindingList, ISynchronizeInvoke, IDisposable
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(FunctionBindingList));

        private IFunction function;
        private IMultiDimensionalArray values;
        internal bool changing;
        public override string ToString()
        {
            return ID;
        }
        public FunctionBindingList(IFunction function)
        {
            ID = Guid.NewGuid().ToString();

            SetFunction(function);

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
                    Thread.Sleep(0);
                }

                var variables = Function.Arguments.Concat(Function.Components);
                return variables.Select(v => v.Name).ToArray();
            }
        }

        public virtual string[] DisplayNames
        {
            get
            {
                while (changing)
                {
                    Thread.Sleep(0);
                }

                var variables = Function.Arguments.Concat(Function.Components);
                return variables.Select(v => v.Name).ToArray();
            }
        }
        
        public IFunction Function
        {
            get { return function; }
            set { SetFunction(value); }
        }

        private void SetFunction(IFunction f)
        {
            if(function != null)
            {
                //function.ValuesChanged -= delayedEventHandler;
                function.ValuesChanged -= FunctionValuesChanged;
                function.Components.CollectionChanged -= Components_CollectionChanged;
                function.Arguments.CollectionChanged -= Arguments_CollectionChanged;
            }

            function = f;

            if(function == null)
            {
                return;
            }

            Fill();

            function.ValuesChanged += FunctionValuesChanged;
            function.Components.CollectionChanged += Components_CollectionChanged;
            function.Arguments.CollectionChanged += Arguments_CollectionChanged;
        }
        
        private void Fill()
        {
            while (changing)
            {
                Thread.Sleep(0);
            }

            changing = true;

            AllowNew = function.Arguments.Count <= 1 && function.IsEditable;
            AllowEdit = function.Arguments.Count <= 1 && function.IsEditable;
            AllowRemove = function.Arguments.Count <= 1 && function.IsEditable;

            // fill in binding list rows
            RaiseListChangedEvents = false;

            Clear();

            if (function.Components.Count > 0)
            {
                values = function.Components[0].Values;
                for (var i = 0; i < values.Count; i++)
                {
                    AddNew();
                }
            }
            RaiseListChangedEvents = true;

            // raise Reset in subscribers
            ResetBindings();
            changing = false;
        }

        void Arguments_CollectionChanged(object sender, NotifyCollectionChangingEventArgs e)
        {
            changing = true;
            Clear();
            ResetBindings();
            changing = false;
        }

        void Components_CollectionChanged(object sender, NotifyCollectionChangingEventArgs e)
        {
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

        private void FunctionValuesChanged(object sender, FunctionValuesChangingEventArgs e)
        {
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
                Thread.Sleep(0); // wait until other change has been processed
            }

            changing = true;
            
            var argumentIndex = function.Arguments.IndexOf((IVariable) e.Function);;

            //find out the property descriptor for this function and raise event.
            switch (e.Action)
            {
                case NotifyCollectionChangeAction.Reset:
                    //SetFunction(function);

                    Clear();
                    ResetBindings();
                    changing = false;
                    Fill();
                    changing = true;

                    break;
                case NotifyCollectionChangeAction.Replace:
                    OnListChanged(new ListChangedEventArgs(ListChangedType.ItemChanged, e.Index));
//                     log.DebugFormat("Function[{0}] value replaced {1}, number of rows: {2}", args.Function, e.Index, Count);
                    break;
                case NotifyCollectionChangeAction.Add:
                    // react only on chainges in arguments
                    if (!function.Arguments.Contains((IVariable)e.Function))
                    {
                        break;
                    }

                    // do nothing if at least 1 of arguments is empty
                    if(function.Arguments.Any(a => a.Values.Count == 0))
                    {
                        break;
                    }

                    // get number of rows to insert
                    var shape = function.Components[0].Values.Shape;
                    //TODO : this would be wrong if not the dimensions is added.
                    var countValuesToInsert = MultiDimensionalArrayHelper.GetTotalLength(shape.Skip(1).ToArray());

                    var index = new int[shape.Length];
                    for (var i = 0; i < countValuesToInsert; i++)
                    {
                        Insert(e.Index, CreateEmptyBindingListRow());
                        //rowIndices.Insert(e.Index, index);
                        MultiDimensionalArrayHelper.IncrementIndex(index, shape, 0);
                    }


//                    log.DebugFormat("Function[{0}] value added {1}, number of rows: {2}", args.Function, e.Index, Count);
                    break;
                case NotifyCollectionChangeAction.Remove:
                    // react only on chainges in arguments
                    if (!function.Arguments.Contains((IVariable) e.Function))
                    {
                        break;
                    }

                    // 1d case, faster implementation
                    if (function.Arguments.Count == 1)
                    {
                        AllowRemove = true;
                        RemoveAt(e.Index);
                        AllowRemove = false;
                        break;
                    }

                    for (var i = Count - 1; i >= 0; i--)
                    {
                        if (this[i].Index[argumentIndex] == e.Index)
                        {
                            AllowRemove = true;
                            RemoveAt(i);
                            AllowRemove = false;
                        }
                    }

//                    log.DebugFormat("Function[{0}] value removed {1}, number of rows: {2}", args.Function, e.Index, Count);
                    break;
                default:
                    changing = false;
                    throw new NotImplementedException(e.ToString());
            }

            changing = false;
        }

        protected virtual FunctionBindingListRow CreateEmptyBindingListRow()
        {
            return new FunctionBindingListRow(this);
        }

        [InvokeRequired]
        private void Insert(int index, FunctionBindingListRow row)
        {
            base.Insert(index, row);
        }

        [InvokeRequired]
        private void RemoveAt(int index)
        {
            base.RemoveAt(index);
        }

        /// <summary>
        /// Adds item to the binded function. The new item should be unique.
        /// </summary>
        /// <returns></returns>
        protected override object AddNewCore()
        {
            //log.Debug("AddNewCore");
            IList<bool> generateUniqueValueForDefaultValues = new List<bool>();
            function.Arguments.ForEach(a =>
                                           {
                                               generateUniqueValueForDefaultValues.Add(
                                                   a.GenerateUniqueValueForDefaultValue);
                                               a.GenerateUniqueValueForDefaultValue = true;
                                           });
            object newObject = base.AddNewCore();
            for (int i = 0; i < generateUniqueValueForDefaultValues.Count; i++)
            {
                function.Arguments[i].GenerateUniqueValueForDefaultValue = generateUniqueValueForDefaultValues[i];
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

        private bool changingFromGui;
        
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

//            log.DebugFormat("List changed: {0}, {1}", e.ListChangedType, e.NewIndex);

            switch(e.ListChangedType)
            {
                case ListChangedType.ItemAdded:
                    if (function.IsIndependent)
                    {
                        foreach (var variable in function.Components)
                        {
                            variable.Values.InsertAt(0, e.NewIndex);
                        }
                    }
                    else
                    {
                        // we can add values only in 1D functions, otherwise list is read-only
                        function.Arguments[0].Values.InsertAt(0, e.NewIndex);
                    }
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


        private bool disposed;
        private string ID;

        public virtual void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            
            if (!this.disposed)
            {
                // If disposing equals true, dispose all managed 
                // and unmanaged resources.
                if (disposing)
                {
                    DelayedEventHandlerController.FireEventsChanged -= DelayedEventHandlerController_FireEventsChanged;
                    //delayedEventHandler.Dispose();
                }
            }
            SetFunction(null);
            disposed = true;
        }

        public void Refresh()
        {
            Fill();
        }

        void DelayedEventHandlerController_FireEventsChanged(object sender, EventArgs e)
        {
            if (DelayedEventHandlerController.FireEvents && !disposed)
            {
                Fill();
            }
        }
    }
}