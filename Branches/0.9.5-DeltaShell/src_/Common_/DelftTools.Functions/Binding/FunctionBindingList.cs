using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using DelftTools.Utils;
using DelftTools.Utils.Aop;
using DelftTools.Utils.Collections;
using DelftTools.Utils.Threading;
using log4net;

namespace DelftTools.Functions.Binding
{
    /// <summary>
    /// Adapter to IFunction to IBindinglist
    /// 
    /// TODO: make it IBindingList<IFunctionValue> (IFunctionValue is a combination of ComponentValues + ArgumentValues)
    /// </summary>
    public class FunctionBindingList : BindingList<FunctionBindingListRow>, IFunctionBindingList, ISynchronizeInvoke
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(FunctionBindingList));

        private IFunction function;
        private IMultiDimensionalArray values;
        private bool changing;
        //private IList<int[]> rowIndices = new List<int[]>();

        public FunctionBindingList() : this(null)
        {
        }

        public FunctionBindingList(IFunction function)
        {
            delayedEventHandler = new SynchronizedDelayedEventHandler<FunctionValuesChangedEventArgs>(function_ValuesChanged)
            {
                FireLastEventOnly = false,
                Delay = 1,
                FullRefreshEventHandler = function_FullRefresh,
                FullRefreshEventsCount = 100,
                Delay2 = 300,
                SynchronizeInvoke = this
            };

            Function = function;
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
                lock (Function.Store)
                {
                    var variables = Function.Arguments.Concat(Function.Components);
                    return variables.Select(v => v.Name).ToArray();
                }
            }
        }

        public string[] DisplayNames
        {
            get
            {
                lock (Function.Store)
                {
                    var variables = Function.Arguments.Concat(Function.Components);
                    return variables.Select(v => v.Name).ToArray();
                }
            }
        }

        private DelayedEventHandler<FunctionValuesChangedEventArgs> delayedEventHandler;

        public bool IsProcessing { get { return delayedEventHandler.HasEventsToProcess || delayedEventHandler.IsRunning; } }

        public IFunction Function
        {
            get { return function; }
            set
            {
                if(function != null)
                {
                    function.ValuesChanged -= delayedEventHandler;
                    function.Components.CollectionChanged -= Components_CollectionChanged;
                    function.Arguments.CollectionChanged -= Arguments_CollectionChanged;
                }

                function = value;

                if(function == null)
                {
                    return;
                }

                Fill();

                delayedEventHandler.Filter = (sender, args) => function.Arguments.Contains((IVariable)args.Function)
                                                               || args.Action == NotifyCollectionChangedAction.Replace;

                function.ValuesChanged += delayedEventHandler;

                function.Components.CollectionChanged += Components_CollectionChanged;
                function.Arguments.CollectionChanged += Arguments_CollectionChanged;

                AllowNew = function.Arguments.Count <= 1;
                AllowRemove = function.Arguments.Count <= 1;
            }
        }

        private void function_FullRefresh(object sender, EventArgs e)
        {
            changing = true;
            Clear();
            Fill();
            changing = false;
        }

        private void Fill()
        {
            changing = true;

            lock (function.Store)
            {
                // fill in binding list rows
                RaiseListChangedEvents = false;
                if (function.Components.Count > 0)
                {
                    values = function.Components[0].Values;
                    for (var i = 0; i < values.Count; i++)
                    {
                        Add(new FunctionBindingListRow(this));
                    }
                }
                RaiseListChangedEvents = true;
            }

            // raise Reset in subscribers
            ResetBindings();
            changing = false;
        }

        void Arguments_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            changing = true;
            Clear();
            ResetBindings();
            changing = false;
        }

        void Components_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
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
            return MultiDimensionalArrayHelper.GetIndex(IndexOf(row), values.Stride);
        }

        protected override void OnAddingNew(AddingNewEventArgs e)
        {
            e.NewObject = new FunctionBindingListRow(this);
        }

        [InvokeRequired]
        private void function_ValuesChanged(object sender, FunctionValuesChangedEventArgs e)
        {
            if (changing) // this is dangerous, what if values change during list changes, rare but possible?
            {
                return;
            }

            changing = true;
            
            var argumentIndex = function.Arguments.IndexOf((IVariable) e.Function);;

            //find out the property descriptor for this function and raise event.
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Replace:
                    OnListChanged(new ListChangedEventArgs(ListChangedType.ItemChanged, e.Index));
//                     log.DebugFormat("Function[{0}] value replaced {1}, number of rows: {2}", args.Function, e.Index, Count);
                    break;
                case NotifyCollectionChangedAction.Add:
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
                    lock (function.Store)
                    {
                        var shape = function.Components[0].Values.Shape;
                        //TODO : this would be wrong if not the dimensions is added.
                        var countValuesToInsert = MultiDimensionalArrayHelper.GetTotalLength(shape.Skip(1).ToArray());

                        var index = new int[shape.Length];
                        for (var i = 0; i < countValuesToInsert; i++)
                        {
                            Insert(e.Index, new FunctionBindingListRow(this));
                            //rowIndices.Insert(e.Index, index);
                            MultiDimensionalArrayHelper.IncrementIndex(index, shape, 0);
                        }
                    }


//                    log.DebugFormat("Function[{0}] value added {1}, number of rows: {2}", args.Function, e.Index, Count);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    lock (function.Store)
                    {
                        // react only on chainges in arguments
                        if (!function.Arguments.Contains((IVariable) e.Function))
                        {
                            break;
                        }

                        // 1d case, faster implementation
                        if (function.Arguments.Count == 1)
                        {
                            RemoveAt(e.Index);
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
                    }

//                    log.DebugFormat("Function[{0}] value removed {1}, number of rows: {2}", args.Function, e.Index, Count);
                    break;
                default:
                    changing = false;
                    throw new NotImplementedException(e.ToString());
            }

            changing = false;
        }

        public override void CancelNew(int itemIndex)
        {
            //log.Debug("CancelNew");
            base.CancelNew(itemIndex);
        }

        public override void EndNew(int itemIndex)
        {
            //log.Debug("EndNew");
            base.EndNew(itemIndex);
        }

        /// <summary>
        /// Adds item to the binded function. The new item should be unique.
        /// </summary>
        /// <returns></returns>
        protected override object AddNewCore()
        {
            lock (function.Store)
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
        }

        public void AddIndex(PropertyDescriptor property)
        {
            //log.Debug("AddIndex");
        }

        public void RemoveIndex(PropertyDescriptor property)
        {
            //log.Debug("RemoveIndex");
        }

        protected override void OnListChanged(ListChangedEventArgs e)
        {
            if (changing)
            {
                base.OnListChanged(e);
                return;
            }

            changing = true;

//            log.DebugFormat("List changed: {0}, {1}", e.ListChangedType, e.NewIndex);

            switch(e.ListChangedType)
            {
                case ListChangedType.ItemAdded:
                    lock (function.Store)
                    {
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
                    }
                    break;
                case ListChangedType.ItemChanged:
                    break;
                case ListChangedType.Reset:
                    break;
                case ListChangedType.ItemDeleted:
                    lock (function.Store)
                    {
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
                    }
                    break;
                case ListChangedType.ItemMoved:
                    break;
            }

            // wait untill events are processed
            while(delayedEventHandler.IsRunning)
            {
                if (SynchronizeWaitMethod != null)
                {
                    SynchronizeWaitMethod();
                }
                Thread.Sleep(10);
            }

            changing = false;

            base.OnListChanged(e);
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

        public PropertyDescriptorCollection GetItemProperties(PropertyDescriptor[] listAccessors)
        {
            lock (Function.Store)
            {
                var variables = Function.Arguments.Concat(Function.Components);
                var descriptors =
                    variables.Select(
                        (v, i) => new FunctionBindingListPropertyDescriptor(v.Name, v.DisplayName, v.ValueType, i));

                return new PropertyDescriptorCollection(descriptors.ToArray());
            }
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
    }
}