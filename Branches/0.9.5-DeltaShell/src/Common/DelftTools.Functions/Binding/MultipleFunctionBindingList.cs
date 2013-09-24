using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using DelftTools.Utils.Aop;
using DelftTools.Utils.Collections;
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
    public class MultipleFunctionBindingList : BindingList<MultipleFunctionBindingListRow>, IFunctionBindingList, ISynchronizeInvoke, IDisposable
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(MultipleFunctionBindingList));

        private readonly List<IFunction> functions = new List<IFunction>();
        private IMultiDimensionalArray values;
        internal bool changing;
        internal bool changingFromGui;
        
        public MultipleFunctionBindingList(IEnumerable<IFunction> functions)
        {
            this.functions.AddRange(functions);
            InitializeFunctions();
        }

        /// <summary>
        /// Used to allow thread-safe behaviour (e.g. in Windows.Forms).
        /// </summary>
        public ISynchronizeInvoke SynchronizeInvoke { get; set; }

        #region IFunctionBindingList

        public string[] ColumnNames
        {
            get
            {
                while (changing)
                {
                    Thread.Sleep(0);
                }

                var first = true;

                var names = new List<string>();
                foreach (var function in functions)
                {
                    var variables = first ? function.Arguments.Concat(function.Components) : function.Components;
                    names.AddRange(variables.Select(v => v.DisplayName).ToArray());

                    first = false;
                }

                return names.ToArray();
            }
        }

        public string[] DisplayNames
        {
            get
            {
                return ColumnNames;
            }
        }

        public IFunction Function
        {
            get { return functions.First(); }
        }

        public IEnumerable<IFunction> Functions
        {
            get { return functions; }
        }

        private void InitializeFunctions()
        {
            foreach (var function in functions)
            {
                function.ValuesChanged -= OnFunctionValuesChanged;
                function.Components.CollectionChanged -= ComponentsCollectionChanged;
                function.Arguments.CollectionChanged -= ArgumentsCollectionChanged;

                if (function == Function)
                {
                    Fill();
                }
                
                function.ValuesChanged += OnFunctionValuesChanged;

                function.Components.CollectionChanged += ComponentsCollectionChanged;
                function.Arguments.CollectionChanged += ArgumentsCollectionChanged;

                AllowNew = function.Arguments.Count <= 1;
                AllowRemove = function.Arguments.Count <= 1;
            }
        }

        private void Fill()
        {
            while (changing)
            {
                Thread.Sleep(0);
            }

            changing = true;
            
            RaiseListChangedEvents = false;

            // fill in binding list rows
            if (Function.Components.Count > 0)
            {
                values = Function.Components[0].Values;
                for (var i = 0; i < values.Count; i++)
                {
                    Add(new MultipleFunctionBindingListRow(this));
                }
            }

            RaiseListChangedEvents = true;

            // raise Reset in subscribers
            ResetBindings();

            changing = false;
        }

        void ArgumentsCollectionChanged(object sender, NotifyCollectionChangingEventArgs e)
        {
            changing = true;
            Clear();
            ResetBindings();
            changing = false;
        }

        void ComponentsCollectionChanged(object sender, NotifyCollectionChangingEventArgs e)
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
        public int[] GetMultiDimensionalRowIndex(MultipleFunctionBindingListRow row)
        {
            return MultiDimensionalArrayHelper.GetIndex(IndexOf(row), values.Stride);
        }

        protected override void OnAddingNew(AddingNewEventArgs e)
        {
            e.NewObject = new MultipleFunctionBindingListRow(this);
        }

        public event EventHandler<FunctionValuesChangingEventArgs> FunctionValuesChanged;

        [InvokeRequired]
        protected virtual void OnFunctionValuesChanged(object sender, FunctionValuesChangingEventArgs e)
        {
            if (changingFromGui)
            {
                return;
            }

            while (changing)
            {
                Thread.Sleep(0); // wait until other change has been processed
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
                    OnFunctionValuesAdded(e);
                    break;
                case NotifyCollectionChangeAction.Remove:
                    OnFunctionValuesRemoved(e);
                    break;
                default:
                    changing = false;
                    throw new NotImplementedException(e.ToString());
            }

            FireFunctionValuesChanged(sender, e);

            changing = false;
        }

        private void OnFunctionValuesAdded(FunctionValuesChangingEventArgs e)
        {
            var function = e.Function;
            if (!function.Arguments.Contains((IVariable)e.Function))
            {
                return;
            }

            // do nothing if at least 1 of arguments is empty
            if (function.Arguments.Any(a => a.Values.Count == 0))
            {
                return;
            }

            // get number of rows to insert
            var shape = function.Components[0].Values.Shape;
            var valuesPerRow = MultiDimensionalArrayHelper.GetTotalLength(shape.Skip(1).ToArray());
            var numRowsAdded = e.Items.Count;
            var rowsToAdd = numRowsAdded*valuesPerRow;

            var index = new int[shape.Length];
            for (var i = 0; i < rowsToAdd; i++)
            {
                Insert(e.Index, new MultipleFunctionBindingListRow(this));
                //rowIndices.Insert(e.Index, index);
                MultiDimensionalArrayHelper.IncrementIndex(index, shape, 0);
            }
        }

        private void OnFunctionValuesRemoved(FunctionValuesChangingEventArgs e)
        {
            var function = e.Function;
            if (!function.Arguments.Contains((IVariable)e.Function))
            {
                return;
            }

            // 1d case, faster implementation
            if (function.Arguments.Count == 1)
            {
                RemoveAt(e.Index);
                return;
            }

            var argumentIndex = function.Arguments.IndexOf((IVariable)e.Function);

            for (var i = Count - 1; i >= 0; i--)
            {
                if (this[i].Index[argumentIndex] == e.Index)
                {
                    AllowRemove = true;
                    RemoveAt(i);
                    AllowRemove = false;
                }
            }
        }

        protected virtual void FireFunctionValuesChanged(object sender, FunctionValuesChangingEventArgs e)
        {
            if (FunctionValuesChanged != null)
            {
                FunctionValuesChanged(sender, e);
            }
        }

        [InvokeRequired]
        private void Insert(int index, MultipleFunctionBindingListRow row)
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
            foreach (IFunction function in functions)
            {
                AddNewCoreForFunction(function);
            }

            return null;
        }

        protected virtual object AddNewCoreForFunction(IFunction function)
        {
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

        [InvokeRequired]
        protected override void OnListChanged(ListChangedEventArgs e)
        {
            if (changing)
            {
                base.OnListChanged(e);
                return;
            }

            changing = true;

            changingFromGui = true;

            foreach (var function in functions)
            {
                OnListChangedForFunction(e, function);
            }

            changing = false;

            changingFromGui = false;

            base.OnListChanged(e);
        }

        protected virtual void OnListChangedForFunction(ListChangedEventArgs e, IFunction function)
        {
            switch (e.ListChangedType)
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
        }

        protected override void ClearItems()
        {
            if (changing)
            {
                base.ClearItems();
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

        public PropertyDescriptorCollection GetItemProperties(PropertyDescriptor[] listAccessors)
        {
            bool first = true;
            int index = 0;

            var descriptors = new List<MultipleFunctionBindingListPropertyDescriptor>();
            foreach (IFunction function in functions)
            {
                IEnumerable<IVariable> variables = first ? function.Arguments.Concat(function.Components) : function.Components;
                first = false;

                foreach (IVariable variable in variables)
                {
                    descriptors.Add(new MultipleFunctionBindingListPropertyDescriptor(variable.Name, variable.DisplayName,
                                                                              variable.ValueType, index++));
                }

                /*descriptors.AddRange(
                    variables.Select(
                        (v, i) => new FunctionBindingListPropertyDescriptor(v.Name, v.DisplayName, v.ValueType, i)));*/
            }

            return new PropertyDescriptorCollection(descriptors.ToArray());
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
            if (SynchronizeInvoke != null)
            {
                return SynchronizeInvoke.BeginInvoke(method, args);
            }

            return new AsyncResult();
        }

        public object EndInvoke(IAsyncResult result)
        {
            if (SynchronizeInvoke != null)
            {
                return SynchronizeInvoke.EndInvoke(result);
            }

            return null;
        }

        public object Invoke(Delegate method, object[] args)
        {
            if (SynchronizeInvoke != null)
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
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            
            if (!disposed)
            {
                // If disposing equals true, dispose all managed 
                // and unmanaged resources.
                if (disposing)
                {
                    //delayedEventHandler.Dispose();
                }
            }

            //cleanup:
            foreach (IFunction function in functions)
            {
                function.ValuesChanged -= OnFunctionValuesChanged;
                function.Components.CollectionChanged -= ComponentsCollectionChanged;
                function.Arguments.CollectionChanged -= ArgumentsCollectionChanged;
            }
            functions.Clear();

            disposed = true;
        }

        public IVariable GetVariableForColumnIndex(int absoluteIndex)
        {
            var numArguments = Function.Arguments.Count;
            
            if (absoluteIndex < numArguments)
                return Function.Arguments[absoluteIndex];

            var componentIndex = absoluteIndex - numArguments;
            foreach (Function f in Functions)
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
    }
}