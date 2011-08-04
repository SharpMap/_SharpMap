using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DelftTools.Functions.Filters;
using DelftTools.Functions.Generic;
using DelftTools.Utils.Collections;
using DelftTools.Utils.Collections.Generic;

namespace DelftTools.Functions
{
    [Serializable]
    public class MemoryFunctionStore : IFunctionStore, INotifyPropertyChanged
    {
        private IDictionary<IVariable, IEnumerable<IVariable>> dependentVariables;
        private IEventedList<IFunction> functions;
        private IList<IMultiDimensionalArray> functionValues;

        public MemoryFunctionStore()
        {
            functions = new EventedList<IFunction>();
            SubscribeToFunctions();
            FireEvents = true;

            FunctionValues = new List<IMultiDimensionalArray>();
            TypeConverters = new List<ITypeConverter>();
        }

        /// <summary>
        /// Copy constructor, creates memory function store with cloned functions from original store.
        /// Does not copy values!!
        /// </summary>
        /// <param name="sourceStore"></param>
        public MemoryFunctionStore(IFunctionStore sourceStore) : this()
        {
            foreach (var function in sourceStore.Functions)
            {
                var clone = (IFunction) function.Clone(false, true, true);
                Functions.Add(clone);
            }

            ReconnectClonedComponentsAndArguments(Functions, sourceStore.Functions);
            UpdateDependentVariables();
        }

        /// NOTE : nhibernate uses this accessor.
        private IList<IMultiDimensionalArray> FunctionValues
        {
            get { return functionValues; }
            set
            {
                UnSubscribeToFunctionValues();
                functionValues = value;
                if (functionValues != null)
                {
                    SetOwnerForFunctionValues();
                }
                SubscribeToFunctionValues();
            }
        }

        //TODO: do we really need this owner stuff?? cant we just use the index?

        private IDictionary<IVariable, IEnumerable<IVariable>> DependentVariables
        {
            get
            {
                if (dependentVariables == null)
                    UpdateDependentVariables();
                return dependentVariables;
            }
            set { dependentVariables = value; }
        }

        #region IFunctionStore Members

        public virtual long Id { get; set; }
        public virtual IList<ITypeConverter> TypeConverters { get; set; }
        public virtual bool FireEvents { get; set; }

        public virtual IEventedList<IFunction> Functions
        {
            get { return functions; }
            set
            {
                UnSubscribeToFunctions();

                functions = value;
                SubscribeToFunctions();
            }
        }

        public virtual void SetVariableValues<T>(IVariable variable, IEnumerable<T> values,
                                                 params IVariableFilter[] filters)
        {
            ValidateFilters(variable, filters);

            if (!Functions.Contains(variable))
            {
                throw new ArgumentOutOfRangeException("function",
                                                      "Function is not a part of the store, add it to the Functions first.");
            }

            if (variable.IsIndependent)
            {
                SetIndependendFunctionValues(variable, filters, values);
            }
            else
            {
                SetDependendVariabeleValues(variable, filters, values);
            }

            CheckConsistency();
        }

        public virtual void AddIndependendVariableValues<T>(IVariable variable, IEnumerable<T> values)
        {
            if (!Functions.Contains(variable))
            {
                throw new ArgumentOutOfRangeException("variable",
                                                      "Function is not a part of the store, add it to the Functions first.");
            }
            IMultiDimensionalArray variableValuesArray = FunctionValues[Functions.IndexOf(variable)];

            bool addingNewValues = variableValuesArray.Count == 0;

            foreach (T o in values)
            {
                if (addingNewValues)
                {
                    variableValuesArray.Add(o);
                }
                else
                {
                    if (!variableValuesArray.Contains(o)) // TODO: slow, optimize it somehow
                    {
                        variableValuesArray.Add(o);
                    }
                }
            }
        }

        public virtual IMultiDimensionalArray GetVariableValues(IVariable variable, params IVariableFilter[] filters)
        {
            //redirect to generic version
            ValidateFilters(variable, filters);

            if (!Functions.Contains(variable))
            {
                return null; //cannot throw exception here 
            }


            // simple function with one component
            if ((variable.Components.Count == 1) && (filters.Length == 0))
            {
                return FunctionValues[Functions.IndexOf(variable.Components[0])];
            }

            return CreateComponentArrayFromFilters(variable, filters);
        }

        public virtual IMultiDimensionalArray<T> GetVariableValues<T>(IVariable function,
                                                                      params IVariableFilter[] filters)
            where T : IComparable
        {
            return (IMultiDimensionalArray<T>) GetVariableValues(function, filters);
        }

        //TODO: make removefunctionValues and move up to function
        public virtual void RemoveFunctionValues(IFunction function, params IVariableValueFilter[] filters)
        {
            ValidateFilters(function, filters);

            //travel down to indep arguments and delete values there.

            if ((function.IsIndependent) && (function is IVariable))
            {
                if (filters.Length == 0)
                {
                    function.Components[0].Values.Clear();
                }
                else
                {
                    IVariableValueFilter functionFilter = filters.FirstOrDefault(f => f.Variable == function);
                    //remove all values specified by the filter
                    if (functionFilter != null)
                    {
                        foreach (object o in functionFilter.Values)
                        {
                            function.Components[0].Values.Remove(o);
                        }
                    }
                }
            }
            foreach (IVariable argument in function.Arguments)
            {
                RemoveFunctionValues(argument, filters);
            }
            return;
        }

        /// <summary>
        /// Updates variable size based on size of it's arguments
        /// 
        /// This method is slow :(.
        /// </summary>
        /// <param name="variable"></param>
        public virtual void UpdateVariableSize(IVariable variable)
        {
            var shape = new int[variable.Arguments.Count];
            for (int i = 0; i < variable.Arguments.Count; i++)
            {
                shape[i] = variable.Arguments[i].Values.Count;
            }
            if (FunctionValues[Functions.IndexOf(variable)] != null)
            {
                FunctionValues[Functions.IndexOf(variable)].Resize(shape);
            }
            UpdateDependentVariables();
        }

        public virtual T GetMaxValue<T>(IVariable variable) where T : IComparable
        {
            return
                (T) Convert.ChangeType(FunctionValues[Functions.IndexOf(variable.Components[0])].MaxValue, typeof (T));
        }

        public virtual T GetMinValue<T>(IVariable variable) where T : IComparable
        {
            // simple function with one component
            return
                (T) Convert.ChangeType(FunctionValues[Functions.IndexOf(variable.Components[0])].MinValue, typeof (T));
        }

        public virtual void CacheVariable(IVariable variable)
        {
            //no exception since WFM might write output to mem store.
            //throw new NotImplementedException("Why cache in memorystore you dummy!");
        }

        public virtual event EventHandler<FunctionValuesChangedEventArgs> FunctionValuesChanged;
        public virtual event EventHandler<FunctionValuesChangedEventArgs> FunctionValuesChanging;

        public virtual event NotifyCollectionChangedEventHandler CollectionChanged;
        public virtual event NotifyCollectionChangedEventHandler CollectionChanging;

        public virtual object Clone()
        {
            //clone all function without values
            //setvalues in clones.
            var clonedStore = new MemoryFunctionStore(this);

            var clonedFunctionValues = new List<IMultiDimensionalArray>();
            foreach (IMultiDimensionalArray array in functionValues)
            {
                IMultiDimensionalArray arrayClone = array != null
                                                        ? (IMultiDimensionalArray) array.Clone()
                                                        : null;

                clonedFunctionValues.Add(arrayClone);
            }
            clonedStore.FunctionValues = clonedFunctionValues;

            return clonedStore;
        }

        #endregion

        #region INotifyPropertyChanged Members

        public virtual event PropertyChangedEventHandler PropertyChanged;

        #endregion

        private void SetOwnerForFunctionValues()
        {
            foreach (IMultiDimensionalArray array in functionValues)
            {
                if (array == null)
                    continue;
                array.Owner = (IVariable) Functions[functionValues.IndexOf(array)];
            }
        }

        private void SubscribeToFunctionValues()
        {
            if (functionValues == null)
                return;
            foreach (IMultiDimensionalArray array in functionValues)
            {
                if (array == null)
                    continue;
                array.PropertyChanged += FunctionValuesPropertyChanged;
                array.CollectionChanging += FunctionValuesValuesChanging;
                array.CollectionChanged += FunctionValuesValuesChanged;
            }
        }

        private void UnSubscribeToFunctionValues()
        {
            if (functionValues == null)
                return;
            foreach (IMultiDimensionalArray array in functionValues)
            {
                //array can be null since it is synched with functions. And composite functions dont have arrays.
                if (array == null)
                    continue;
                array.PropertyChanged -= FunctionValuesPropertyChanged;
                array.CollectionChanging -= FunctionValuesValuesChanging;
                array.CollectionChanged -= FunctionValuesValuesChanged;
            }
        }

        private void UpdateDependentVariables()
        {
            DependentVariables = MemoryFunctionStoreHelper.GetDependentVariables(functions);
        }

        private void SubscribeToFunctions()
        {
            if (functions != null)
            {
                functions.CollectionChanged += Functions_CollectionChanged;
                functions.CollectionChanging += Functions_CollectionChanging;
            }
        }

        private void UnSubscribeToFunctions()
        {
            if (functions != null)
            {
                functions.CollectionChanged -= Functions_CollectionChanged;
                functions.CollectionChanging -= Functions_CollectionChanging;
            }
        }

        private void SetDependendVariabeleValues<T>(IVariable variable, IVariableFilter[] filters, IEnumerable<T> values)
        {
            IMultiDimensionalArray componentArrayView = CreateComponentArrayFromFilters(variable, filters);

            //iterate every value of the view set values for all components.
            IEnumerator enumerator = values.GetEnumerator();
            int size = componentArrayView.Count;

            int[] stride = MultiDimensionalArrayHelper.GetStride(componentArrayView.Shape);

            for (int i = 0; i < size; i++)
            {
                int[] indexes = MultiDimensionalArrayHelper.GetIndex(i, stride);
                //travese our values. Reset if we run out of values
                if (!enumerator.MoveNext())
                {
                    enumerator.Reset();
                    enumerator.MoveNext();
                }
                componentArrayView[indexes] = enumerator.Current;
            }
        }

        private void SetIndependendFunctionValues(IVariable variable, IVariableFilter[] filters, IEnumerable values)
        {
            IMultiDimensionalArray array = FunctionValues[functions.IndexOf(variable)];

            if (filters.Length != 0) 
            {
                throw new ArgumentException("don't use filters to set independend variables. the only exception is an index filter with index equal to count");    
            }
            
            array.AddRange(values);
        }

        /// <summary>
        /// Checks if all filters are compatible with the function and also supported.
        /// </summary>
        /// <param name="function"></param>
        /// <param name="filters"></param>
        private static void ValidateFilters(IFunction function, IVariableFilter[] filters)
        {
            //TODO: get some use for it..does little or nothing now.. and move up to function
            var argumentFiltered = new bool[function.Arguments.Count];
            var componentFiltered = new bool[function.Components.Count];

            for (int i = 0; i < filters.Length; i++)
            {
                IVariableFilter filter = filters[i];
                int argumentIndex = function.Arguments.IndexOf(filter.Variable);
                int componentIndex = function.Components.IndexOf(filter.Variable);

                if (argumentIndex >= 0 && argumentFiltered[argumentIndex])
                {
                    // throw new NotSupportedException("Only one filter per argument can be specified for now");
                }
                if (componentIndex >= 0 && componentFiltered[componentIndex])
                {
                    //  throw new NotSupportedException("Only one filter per component can be specified for now");
                }

                if (argumentIndex >= 0)
                {
                    argumentFiltered[argumentIndex] = true;
                }
                if (componentIndex >= 0)
                {
                    componentFiltered[componentIndex] = true;
                }
            }
        }

        /// <summary>
        /// Prepares the Multidimensional array of function. Expands dimensions where needed.
        /// TODO: this and above methods seem to be redundant
        /// 
        /// TODO: shouldn't it happen in Function?
        /// </summary>
        /// <param name="function"></param>
        /// <param name="filters"></param>
        /// <returns></returns>
        private IMultiDimensionalArray CreateComponentArrayFromFilters(IFunction function,
                                                                       IList<IVariableFilter> filters)
        {
            if (filters.Count == 0)
            {
                return FunctionValues[Functions.IndexOf(function)];
            }

            var variable = (IVariable) function;

            // HACK: will create generic view. based on maxInt
            IMultiDimensionalArrayView view = FunctionValues[Functions.IndexOf(variable)].Select(0, int.MinValue,
                                                                                                 int.MaxValue);

            // check if we have aggregation filters, if yes - return a new array, otherwise return a view based on existing array
            // probably it should be always a view based on existing array, even when aggregation is used
            if (filters.Any(f => f is VariableAggregationFilter))
            {
                var filterVariableIndex = new Dictionary<VariableAggregationFilter, int>();
                foreach (VariableAggregationFilter filter in filters.OfType<VariableAggregationFilter>())
                {
                    filterVariableIndex[filter] = variable.Arguments.IndexOf(filter.Variable);
                }

                // calculate shape of the resulting array
                var shape = (int[]) view.Shape.Clone();
                foreach (VariableAggregationFilter filter in filters.OfType<VariableAggregationFilter>())
                {
                    shape[filterVariableIndex[filter]] = filter.Count;
                }

                // now fill in values into the resulting array
                IMultiDimensionalArray result = variable.CreateStorageArray();
                result.Resize(shape);

                int totalLength = MultiDimensionalArrayHelper.GetTotalLength(shape);
                for (int i = 0; i < totalLength; i++)
                {
                    int[] targetIndex = MultiDimensionalArrayHelper.GetIndex(i, result.Stride);

                    // calculate index in the source array
                    var sourceIndex = (int[]) targetIndex.Clone();
                    foreach (VariableAggregationFilter filter in filters.OfType<VariableAggregationFilter>())
                    {
                        int filterArgumentIndex = filterVariableIndex[filter];
                        sourceIndex[filterArgumentIndex] = GetSourceIndex(filter, targetIndex[filterArgumentIndex]);
                    }

                    result[targetIndex] = view[sourceIndex]; // copy value (only required)
                }


                return result;
            }

            //use variable filters that are relevant to make a selection
            IEnumerable<IVariableFilter> relevantArgumentFilters =
                filters.OfType<IVariableFilter>().Where(
                    f =>
                    (f is IVariableValueFilter || f is VariableIndexRangesFilter || f is VariableIndexRangeFilter) &&
                    (f.Variable == function || function.Arguments.Contains(f.Variable)));

            foreach (IVariableFilter variableFilter in relevantArgumentFilters)
            {
                //determine indexes for this variable..get in a base class?
                int[] indexes = GetArgumentIndexes(variableFilter);

                if (function.IsIndependent && variableFilter.Variable == function)
                {
                    view.SelectedIndexes[0] = indexes; // indepedent variable always dim 0
                }
                else if (function.Arguments.Contains(variableFilter.Variable))
                {
                    int dimensionIndex = function.Arguments.IndexOf(variableFilter.Variable);
                    view.SelectedIndexes[dimensionIndex] = indexes;
                }
            }

            //reduce the view for reduce filters.
            foreach (VariableReduceFilter filter in filters.OfType<VariableReduceFilter>())
            {
                int index = function.Arguments.IndexOf(filter.Variable);
                if (index != -1)
                {
                    view.Reduce[index] = true;
                }
            }

            return view;
        }

        private int GetSourceIndex(VariableAggregationFilter filter, int index)
        {
            int sourceIndex = filter.MinIndex + index*filter.StepSize;
            sourceIndex = Math.Min(sourceIndex, filter.MaxIndex);
            return sourceIndex;
        }

        private int[] GetArgumentIndexes(IVariableFilter variableFilter)
        {
            int[] indexes;
            if (variableFilter is IVariableValueFilter)
            {
                indexes = GetVariableValueFilterIndexes((IVariableValueFilter) variableFilter).ToArray();
            }
            else if (variableFilter is VariableIndexRangeFilter)
            {
                indexes = GetVariableIndexRangesFilterIndexes((VariableIndexRangeFilter)variableFilter);
            }
            else
            {
                indexes = GetVariableIndexRangesFilterIndexes((VariableIndexRangesFilter) variableFilter);
            }
            return indexes;
        }

        private int[] GetVariableIndexRangesFilterIndexes(VariableIndexRangeFilter filter)
        {
            IList<int> indexesList = new List<int>();
            for (int i = filter.MinIndex; i <= filter.MaxIndex; i++)
            {
                indexesList.Add(i);
            }
            return indexesList.ToArray();
        }

        private int[] GetVariableIndexRangesFilterIndexes(VariableIndexRangesFilter variableIndexRangeFilter)
        {
            IList<int> indexesList = new List<int>();
            foreach (var range in  variableIndexRangeFilter.IndexRanges)
            {
                // do not assume the indices are ascending
                int step = (range.First < range.Second) ? 1 : -1;
                int number = Math.Abs(range.First - range.Second) + 1;
                for (int i = 0; i < number; i++)
                {
                    indexesList.Add(range.First + (i*step));
                }
            }
            return indexesList.ToArray();
        }

        // TODO: make it ValuesChanging
        private void FunctionValuesValuesChanging(object sender, NotifyCollectionChangedEventArgs args)
        {
            if (!FireEvents) // don't send up
            {
                return;
            }

            var e = (MultiDimensionalArrayChangedEventArgs) args;
            var array = (MultiDimensionalArray) sender;
            IVariable variable = array.Owner;

            if (FunctionValuesChanging != null)
            {
                var valuesChangedEventArgs = new FunctionValuesChangedEventArgs
                                                 {
                                                     Action = e.Action,
                                                     Item = e.Item,
                                                     Index = e.Index,
                                                     OldIndex = -1,
                                                     Stride = e.Stride,
                                                     MultiDimensionalLength = e.MultiDimensionalLength,
                                                     Function = variable
                                                 };

                FunctionValuesChanging(this, valuesChangedEventArgs);

                args.Item = valuesChangedEventArgs.Item;
                args.Index = valuesChangedEventArgs.Index;
                args.Cancel = valuesChangedEventArgs.Cancel;
            }
        }

        // TODO: make it ValuesChanged
        private void FunctionValuesValuesChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            var e = (MultiDimensionalArrayChangedEventArgs) args;

            //update dependend arrays
            var array = (MultiDimensionalArray) sender;
            IVariable variable = array.Owner;


            ResizeDependendFunctionValues(variable, e.Action, args.Index, args.OldIndex);

            if (!FireEvents) // don't send up
            {
                return;
            }

            //raise event to the outside world if the store is consistent.
            if (FunctionValuesChanged != null)
            {
                var valuesChangedEventArgs = new FunctionValuesChangedEventArgs
                                                 {
                                                     Action = e.Action,
                                                     Item = e.Item,
                                                     Index = e.Index,
                                                     OldIndex = -1,
                                                     Stride = e.Stride,
                                                     MultiDimensionalLength = e.MultiDimensionalLength,
                                                     Function = variable
                                                 };

                FunctionValuesChanged(this, valuesChangedEventArgs);
            }
        }

        /// <summary>
        /// Resizes functions for the given argumentVariable. if x is dependend on y, x gets resized
        /// </summary>
        /// <param name="argumentVariable">The argument variable that is altered</param>
        /// <param name="action">action on the argument</param>
        /// <param name="index">index in argument array</param>
        private void ResizeDependendFunctionValues(IVariable argumentVariable, NotifyCollectionChangedAction action,
                                                   int index, int oldIndex)
        {
            if (!DependentVariables.ContainsKey(argumentVariable))
            {
                return;
            }

            switch (action)
            {
                case NotifyCollectionChangedAction.Add:

                    foreach (IVariable dependentVariable in DependentVariables[argumentVariable])
                    {
                        if (!dependentVariable.Attached)
                            continue;

                        int dependentVariableIndex = Functions.IndexOf(dependentVariable);
                        int argumentIndex = dependentVariable.Arguments.IndexOf(argumentVariable);
                        //argument based dependency

                        if (argumentIndex != -1)
                        {
                            FunctionValues[dependentVariableIndex].InsertAt(argumentIndex, index);
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (IVariable dependentVariable in DependentVariables[argumentVariable])
                    {
                        if (!dependentVariable.Attached)
                            continue;

                        int dependentVariableIndex = Functions.IndexOf(dependentVariable);
                        int argumentIndex = dependentVariable.Arguments.IndexOf(argumentVariable);
                        //argument based dependency
                        if (argumentIndex != -1)
                        {
                            FunctionValues[dependentVariableIndex].RemoveAt(argumentIndex, index);
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    if (index != oldIndex)
                    {
                        foreach (IVariable dependentVariable in DependentVariables[argumentVariable])
                        {
                            if (!dependentVariable.Attached) // TODO: what is attach?
                            {
                                continue;
                            }

                            int dependentVariableIndex = Functions.IndexOf(dependentVariable);
                            int argumentIndex = dependentVariable.Arguments.IndexOf(argumentVariable);

                            //argument based dependency
                            if (oldIndex != -1)
                            {
                                // TODO: extend it to work with Length > 1
                                FunctionValues[dependentVariableIndex].Move(argumentIndex, index,1,oldIndex);//, 1, index);
                            }
                        }
                    }
                    break;
            }
        }

        //private bool DisableEvents { get; set; }

        /// <summary>
        /// Returns the indexes of the variable for the given values
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        /// TODO: move to function. In store work with indexes. Gives problems on insert.
        private IList<int> GetVariableValueFilterIndexes(IVariableValueFilter filter)
        {
            //this must be sloooooooooooooooooooow.
            IList<int> result = new List<int>();
            IMultiDimensionalArray values = FunctionValues[Functions.IndexOf(filter.Variable)];
            if (values.Rank != 1)
                throw new NotSupportedException("Filtering on multidimensional variable is not supported");
            //traverse the array and find out matching indexes
            for (int i = 0; i < values.Count; i++)
            {
                if (filter.Values.Contains(values[i]))
                {
                    result.Add(i);
                }
            }
            return result;
        }

        private void Functions_CollectionChanging(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    var function = (IFunction) e.Item;
                    if (functions.Contains(function))
                    {
                        throw new ArgumentOutOfRangeException("Function already registered in the store");
                    }
                    break;
            }
        }

        //TODO : this stuff seems too complex. Make it look simler.
        private void Functions_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var function = (IFunction) e.Item;

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:

                    FunctionValues.Add(null);
                    /* Get the components of a function. Not of variables */
                    if (!(function is IVariable))
                    {
                        foreach (IVariable component in function.Components)
                        {
                            if (Functions.Contains(component))
                                continue;
                            Functions.Add(component);
                        }
                    }

                    foreach (IVariable argument in function.Arguments)
                    {
                        if (argument.Store != this)
                        {
                            if (Functions.Contains(argument))
                                continue;
                            Functions.Add(argument);
                        }
                    }

                    if (function is IVariable)
                    {
                        var variable = (IVariable) function;

                        var variableValues =  variable.FixedSize == 0 ? null : variable.Values;

                        // avoid unnecessary calls for better performance
                        var array = (variableValues == null || variableValues.Count == 0) 
                            ? variable.CreateStorageArray() 
                            : variable.CreateStorageArray(variable.Values);

                        FunctionValues[e.Index] = array;

                        array.PropertyChanged += FunctionValuesPropertyChanged;
                        array.CollectionChanging += FunctionValuesValuesChanging;
                        array.CollectionChanged += FunctionValuesValuesChanged;

                        // register all variables which for the newly added is an argument
                        IEnumerable<IFunction> dependendFunctions =
                            functions.Where(f => f.Arguments.Contains(variable) && f is IVariable);
                        foreach (IVariable dependentVariable in dependendFunctions)
                        {
                            //DependentVariables[variable].Add(dependentVariable);
                            UpdateVariableSize(dependentVariable);
                        }
                    }

                    function.Store = this;

                    break;
                case NotifyCollectionChangedAction.Remove:

                    FunctionValues[e.Index].CollectionChanging -= FunctionValuesValuesChanging;
                    FunctionValues[e.Index].CollectionChanged -= FunctionValuesValuesChanged;
                    FunctionValues[e.Index].PropertyChanged -= FunctionValuesPropertyChanged;

                    FunctionValues.RemoveAt(e.Index);

                    //evict the function from the store. Reset arguments and components list to prevent synchronization.
                    //HACK: do something nice
                    function.Detach();
                    function.Arguments = new EventedList<IVariable>();
                    function.Components = new EventedList<IVariable>();
                    function.Store = null;

                    //IFunctionStore newStore = new MemoryFunctionStore();
                    //newStore.Functions.Add(function);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    throw new NotSupportedException();
            }

            UpdateDependentVariables();
        }

        private void FunctionValuesPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(sender, e);
            }
        }

        /// <summary>
        /// A store is consistent if the function arguments and components are in synch
        /// </summary>
        private void CheckConsistency()
        {
            foreach (IVariable variable in Functions.Where(f => f is IVariable))
            {
                if (variable.Arguments.Count > 0)
                {
                    if (!variable.Attached)
                        continue;

                    //a component should have as many values as the product of argument values
                    int totalCount = 1;
                    foreach (IVariable argument in variable.Arguments)
                    {
                        totalCount *= argument.Values.Count;
                    }

                    if (variable.Values.Count != totalCount)
                    {
                        string message = string.Format("Variable {0} is inconsistent, number of values" +
                                                       " is not equal to multiple of all argument values", variable);
                        throw new InvalidOperationException(message);
                    }
                }
            }
        }


        /// <summary>
        /// rewire all components and arguments
        /// </summary>
        /// <param name="clonedFunctions"></param>
        /// <param name="sourceFunctions"></param>
        private static void ReconnectClonedComponentsAndArguments(IList<IFunction> clonedFunctions,
                                                                  IList<IFunction> sourceFunctions)
        {
            for (int i = 0; i < sourceFunctions.Count; i++)
            {
                IFunction sourceFunction = sourceFunctions[i];
                IFunction clonedFunction = clonedFunctions[i];

                var arguments = new EventedList<IVariable>();
                foreach (IVariable argument in sourceFunction.Arguments)
                {
                    int argumentIndex = sourceFunctions.IndexOf(argument);
                    arguments.Add((IVariable) clonedFunctions[argumentIndex]);
                }

                clonedFunction.Arguments = arguments;

                var components = new EventedList<IVariable>();
                foreach (IVariable component in sourceFunction.Components)
                {
                    int componentIndex = sourceFunctions.IndexOf(component);
                    components.Add((IVariable) clonedFunctions[componentIndex]);
                }

                clonedFunction.Components = components;
            }
        }
    }
}