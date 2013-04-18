using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DelftTools.Functions.Filters;
using DelftTools.Functions.Generic;
using DelftTools.Utils;
using DelftTools.Utils.Collections;
using DelftTools.Utils.Collections.Extensions;
using DelftTools.Utils.Collections.Generic;
using DelftTools.Utils.Data;

namespace DelftTools.Functions
{
    [Serializable]
    public class MemoryFunctionStore : Unique<long>, IFunctionStore, INotifyPropertyChange
    {
        private IDictionary<IVariable, IEnumerable<IVariable>> dependentVariables;
        private IEventedList<IFunction> functions;
        private IList<IMultiDimensionalArray> __functionValues;
        private bool subscribedToFunctionValues;

        public MemoryFunctionStore()
        {
            Functions = new EventedList<IFunction>();
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
            
            // reset cache
            foreach (var variable in Functions.OfType<IVariable>())
            {
                variable.CachedValues = null;
            }
        }

        /// NOTE : nhibernate uses this accessor.
        private IList<IMultiDimensionalArray> FunctionValues
        {
            get
            {
                if (!subscribedToFunctionValues) //keep functionValues lazy..
                {
                    SubscribeToFunctionValues(__functionValues);
                    subscribedToFunctionValues = true;
                }
                return __functionValues;
            }
            set
            {
                UnSubscribeToFunctionValues(__functionValues);
                __functionValues = value;
                subscribedToFunctionValues = false;
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
            if (!Functions.Contains(variable))
            {
                return null; //cannot throw exception here TODO: why??
            }


            // simple function with one component
            if ((variable.Components.Count == 1) && (filters.Length == 0))
            {
                return GetValues(variable.Components[0]);
            }

            return CreateComponentArrayFromFilters(variable, filters);
        }

        public virtual IMultiDimensionalArray<T> GetVariableValues<T>(IVariable function,
                                                                      params IVariableFilter[] filters)
        {
            return (IMultiDimensionalArray<T>) GetVariableValues(function, filters);
        }

        //TODO: make removefunctionValues and move up to function
        public virtual void RemoveFunctionValues(IFunction function, params IVariableValueFilter[] filters)
        {
            //travel down to indep arguments and delete values there.

            if(filters.Length == 0) // optimized version
            {
                if(function.IsIndependent)
                {
                    foreach (var variable in function.Components)
                    {
                        GetValues(variable).Clear();
                    }
                }
                else
                {
                    foreach (var variable in function.Arguments)
                    {
                        GetValues(variable).Clear();
                    }
                }

                return;
            }

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
        
        private IMultiDimensionalArray GetValues(IVariable variable)
        {
            if(variable.CachedValues == null)
            {
                variable.CachedValues = FunctionValues[functions.IndexOf(variable)];
            }

            return variable.CachedValues;
        }

        /// <summary>
        /// Updates variable size based on size of it's arguments
        /// 
        /// This method is slow :(.
        /// </summary>
        /// <param name="variable"></param>
        public virtual void UpdateVariableSize(IVariable variable)
        {
            UpdateAutoSortOnArrays();
            var shape = new int[variable.Arguments.Count];
            for (int i = 0; i < variable.Arguments.Count; i++)
            {
                shape[i] = (variable.Arguments[i].Values != null) ? variable.Arguments[i].Values.Count : 0;
            }
            var values = FunctionValues[Functions.IndexOf(variable)];
            if (values != null)
            {
                values.Resize(shape);
            }
            UpdateDependentVariables();
        }

        public virtual T GetMaxValue<T>(IVariable variable)
        {
            var array = FunctionValues[Functions.IndexOf(variable.Components[0])];

            var maxValue = array.MaxValue;
            if(maxValue == null)
            {
                return default(T);
            }

            //if maxvalue is nodatavalue get the highest value that is not a noDataValue
            if (variable.NoDataValues.Contains(maxValue))
            {
                return array.OfType<T>().Where(v => !variable.NoDataValues.Contains(v)).DefaultIfEmpty(default(T)).Max();
            }

            return (T)Convert.ChangeType(maxValue, typeof(T));
        }

        public virtual T GetMinValue<T>(IVariable variable)
        {
            var array = FunctionValues[Functions.IndexOf(variable.Components[0])];
            var minValue = array.MinValue;
            if(minValue == null)
            {
                return default(T);
            }

            if (variable.NoDataValues.Contains(minValue))
            {
                return array.OfType<T>().Where(v => !variable.NoDataValues.Contains(v)).DefaultIfEmpty(default(T)).Min();
            }

            return(T) Convert.ChangeType(minValue, typeof (T));
        }

        public virtual void CacheVariable(IVariable variable)
        {
            //no exception since WFM might write output to mem store.
            //throw new NotImplementedException("Why cache in memorystore you dummy!");
        }

        public virtual bool DisableCaching { get; set; }


        public virtual event EventHandler<FunctionValuesChangingEventArgs> FunctionValuesChanged;
        public virtual event EventHandler<FunctionValuesChangingEventArgs> FunctionValuesChanging;

        public virtual event NotifyCollectionChangedEventHandler CollectionChanged;
        public virtual event NotifyCollectionChangingEventHandler CollectionChanging;

        public virtual object Clone()
        {
            //clone all function without values
            //setvalues in clones.
            var clonedStore = new MemoryFunctionStore(this);

            var clonedFunctionValues = new List<IMultiDimensionalArray>();
            foreach (IMultiDimensionalArray array in FunctionValues)
            {
                IMultiDimensionalArray arrayClone = null;
                if (array != null)
                {
                    arrayClone = (IMultiDimensionalArray) array.Clone();
                    arrayClone.IsAutoSorted = array.IsAutoSorted;
                    arrayClone.DefaultValue = array.DefaultValue;
                }
                clonedFunctionValues.Add(arrayClone);
            }
            clonedStore.FunctionValues = clonedFunctionValues;

            return clonedStore;
        }

        #endregion

        #region INotifyPropertyChanged Members

        public virtual event PropertyChangingEventHandler PropertyChanging;
        public virtual event PropertyChangedEventHandler PropertyChanged;

        #endregion

        private void SubscribeToFunctionValues(IEnumerable<IMultiDimensionalArray> arrayList)
        {
            if (arrayList == null)
                return;
            foreach (IMultiDimensionalArray array in arrayList)
            {
                if (array == null)
                    continue;
                SubscribeToArray(array);
            }
        }

        private void SubscribeToArray(IMultiDimensionalArray array)
        {
            array.PropertyChanged += FunctionValuesPropertyChanged;
            array.PropertyChanging += FunctionValuesPropertyChanging;
            array.CollectionChanging += FunctionValuesValuesChanging;
            array.CollectionChanged += FunctionValuesValuesChanged;
        }

        
        private void UnsubscribeFromArray(IMultiDimensionalArray array)
        {
            array.PropertyChanged -= FunctionValuesPropertyChanged;
            array.PropertyChanging -= FunctionValuesPropertyChanging;
            array.CollectionChanging -= FunctionValuesValuesChanging;
            array.CollectionChanged -= FunctionValuesValuesChanged;
        }

        private void UnSubscribeToFunctionValues(IEnumerable<IMultiDimensionalArray> arrayList)
        {
            if (arrayList == null)
                return;
            foreach (IMultiDimensionalArray array in arrayList)
            {
                //array can be null since it is synched with functions. And composite functions dont have arrays.
                if (array == null)
                    continue;

                UnsubscribeFromArray(array);
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

            //this check is to prevent code like f[1] = new[]{1,2,3} where the 2,3 are silently ignored.
            //if performance penalty is high here the check should go
            int count = values.Count();
            if (count > size)
            {
                throw new ArgumentException(string.Format("Number of values to be written to dependent variable '{0}' exceeds argument values range. Got {1} values expected at most {2}.", variable.Name,count,size));    
            }

            int[] stride = MultiDimensionalArrayHelper.GetStride(componentArrayView.Shape);
			
			
			var valuesList = values as IList<T>;
			if(valuesList == null)
			{
				valuesList = values.ToList();
			}
			
            for (int i = 0, j = 0; i < size; i++, j++)
            {
                int[] indexes = MultiDimensionalArrayHelper.GetIndex(i, stride);
				
				if(j == valuesList.Count)
				{
					j = 0; // reset index, repeat from start
				}
                componentArrayView[indexes] = valuesList[j];
            }
        }

        private void SetIndependendFunctionValues(IVariable variable, IVariableFilter[] filters, IEnumerable values)
        {
            IMultiDimensionalArray array = FunctionValues[functions.IndexOf(variable)];

            if (filters.Length != 0) 
            {
                throw new ArgumentException("don't use filters to set independend variables. the only exception is an index filter with index equal to count");    
            }

            if (values is IList)
            {
                array.AddRange((IList) values);
            }
            else
            {
                array.AddRange(values.Cast<object>().ToList());
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

                view = result.Select(0, int.MinValue, int.MaxValue);
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

        private static int[] GetVariableIndexRangesFilterIndexes(VariableIndexRangeFilter filter)
        {
            IList<int> indexesList = new List<int>();
            for (int i = filter.MinIndex; i <= filter.MaxIndex; i++)
            {
                indexesList.Add(i);
            }
            return indexesList.ToArray();
        }

        private static int[] GetVariableIndexRangesFilterIndexes(VariableIndexRangesFilter variableIndexRangeFilter)
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
        private void FunctionValuesValuesChanging(object sender, MultiDimensionalArrayChangingEventArgs args)
        {
            if (!FireEvents) // don't send up
            {
                return;
            }

            var e = args;
            var array = (MultiDimensionalArray) sender;
            IVariable variable = GetVariableForArray(array);

            if (FunctionValuesChanging != null)
            {
                //todo handle/bubble event the complex 
                FunctionValuesChangingEventArgs valuesChangedEventArgs = GetValuesChangedEventArgs(e, variable);

                FunctionValuesChanging(this, valuesChangedEventArgs);

                args.Items = valuesChangedEventArgs.Items;
                args.Index = valuesChangedEventArgs.Index;
                args.Cancel = valuesChangedEventArgs.Cancel;
            }
        }

        private FunctionValuesChangingEventArgs GetValuesChangedEventArgs(MultiDimensionalArrayChangingEventArgs e, IVariable variable)
        {
            return new FunctionValuesChangingEventArgs
                       {
                           Action = e.Action,
                           Items= e.Items,
                           Index = e.Index,
                           OldIndex = -1,
                           Stride = e.Stride,
                           Shape = e.Shape,
                           Function = variable
                       };
        }

        // TODO: make it ValuesChanged
        private void FunctionValuesValuesChanged(object sender, MultiDimensionalArrayChangingEventArgs args)
        {
            var e = args;

            //update dependend arrays
            var array = (MultiDimensionalArray) sender;
            //it this OK?
            IVariable variable = GetVariableForArray(array);

            if (variable.IsIndependent)
            {
                ResizeDependendFunctionValuesForArgument(variable, e.Action, args.Index, args.OldIndex,e.Shape[0]);
            }

            if (!FireEvents) // don't send up
            {
                return;
            }

            //raise event to the outside world if the store is consistent.
            if (FunctionValuesChanged != null)
            {
                FunctionValuesChangingEventArgs valuesChangedEventArgs = GetValuesChangedEventArgs(e, variable);

                FunctionValuesChanged(this, valuesChangedEventArgs);
            }
        }

        private IVariable GetVariableForArray(MultiDimensionalArray array)
        {
            return (IVariable) functions[FunctionValues.IndexOf(array)];
        }

        /// <summary>
        /// Resizes functions for the given argumentVariable. if x is dependend on y, x gets resized
        /// </summary>
        /// <param name="argumentVariable">The argument variable that is altered</param>
        /// <param name="action">action on the argument</param>
        /// <param name="index">index in argument array</param>
        /// <param name="oldIndex"></param>
        /// <param name="i"></param>
        private void ResizeDependendFunctionValuesForArgument(IVariable argumentVariable, NotifyCollectionChangeAction action, int index, int oldIndex, int length)
        {
           
            switch (action)
            {
                case NotifyCollectionChangeAction.Add:

                    foreach (IVariable dependentVariable in DependentVariables[argumentVariable])
                    {
                        int dependentVariableIndex = Functions.IndexOf(dependentVariable);
                        int argumentIndex = dependentVariable.Arguments.IndexOf(argumentVariable);
                        //argument based dependency

                        if (argumentIndex != -1)
                        {
                            FunctionValues[dependentVariableIndex].InsertAt(argumentIndex, index,length);
                        }
                    }
                    break;
                case NotifyCollectionChangeAction.Remove:
                    foreach (IVariable dependentVariable in DependentVariables[argumentVariable])
                    {
                        int dependentVariableIndex = Functions.IndexOf(dependentVariable);
                        int argumentIndex = dependentVariable.Arguments.IndexOf(argumentVariable);
                        //argument based dependency
                        if (argumentIndex != -1)
                        {
                            FunctionValues[dependentVariableIndex].RemoveAt(argumentIndex, index);
                        }
                    }
                    break;
                case NotifyCollectionChangeAction.Replace:
                    if (index != oldIndex)
                    {
                        foreach (IVariable dependentVariable in DependentVariables[argumentVariable])
                        {
                            int dependentVariableIndex = Functions.IndexOf(dependentVariable);
                            int argumentIndex = dependentVariable.Arguments.IndexOf(argumentVariable);

                            //argument based dependency
                            if ((oldIndex != -1) && FunctionValues[dependentVariableIndex].Count != 0)
                            {
                                // TODO: extend it to work with Length > 1
                                FunctionValues[dependentVariableIndex].Move(argumentIndex, oldIndex, 1, index);
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
            IMultiDimensionalArray values = FunctionValues[Functions.IndexOf(filter.Variable)];
            if (values.Rank != 1)
                throw new NotSupportedException("Filtering on multidimensional variable is not supported");

            //traverse the array and find out matching indexes
            var result = new List<int>();
            ArrayList filterValues = new ArrayList(filter.Values);

            for (int fi = 0; fi < filterValues.Count; fi++)
            {
                int index = values.BinaryHintedSearch(filterValues[fi]);
                if (index >= 0)
                {
                    result.Add(index);
                }
            }
            
            //make sure the output is ordered ascending
            result.Sort();

            return result;
        }

        private void Functions_CollectionChanging(object sender, NotifyCollectionChangingEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangeAction.Add:
                    var function = (IFunction) e.Item;
                    if (functions.Contains(function))
                    {
                        throw new ArgumentOutOfRangeException("Function already registered in the store");
                    }
                    break;
            }
        }

        //TODO : this stuff seems too complex. Make it look simler.
        private void Functions_CollectionChanged(object sender, NotifyCollectionChangingEventArgs e)
        {
            var function = (IFunction) e.Item;

            switch (e.Action)
            {
                case NotifyCollectionChangeAction.Add:

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

                        variable.CachedValues = array;
                        
                        SubscribeToArray(array);
                        
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
                case NotifyCollectionChangeAction.Remove:

                    IMultiDimensionalArray multiDimensionalArray = FunctionValues[e.Index];
                    UnsubscribeFromArray(multiDimensionalArray);
                    
                    FunctionValues.RemoveAt(e.Index);

                    //evict the function from the store. Reset arguments and components list to prevent synchronization.
                    
                    function.Arguments = new EventedList<IVariable>();
                    function.Components = new EventedList<IVariable>();
                    function.Store = null;

                    //IFunctionStore newStore = new MemoryFunctionStore();
                    //newStore.Functions.Add(function);
                    break;
                case NotifyCollectionChangeAction.Replace:
                    throw new NotSupportedException();
            }
            UpdateAutoSortOnArrays();
            UpdateDependentVariables();
        }

        private void UpdateAutoSortOnArrays()
        {
            //turns auto sort on for arrays or arguments..
            for (int i=0;i<FunctionValues.Count;i++)
            {
                if ((FunctionValues[i] != null) && (functions[i] is IVariable))
                {
                    FunctionValues[i].IsAutoSorted = ((IVariable) functions[i]).IsAutoSorted;
                }
                
            }
        }

        private void FunctionValuesPropertyChanging(object sender, PropertyChangingEventArgs e)
        {
            if (PropertyChanging != null)
            {
                PropertyChanging(sender, e);
            }
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

        public virtual void SetAutoSortForVariable(IVariable variable,bool value)
        {
            var idx = functions.IndexOf(variable);
            if (idx !=  -1)
            {
            //    throw new InvalidOperationException("Variable not found. Unable to set auto sort");
                FunctionValues[idx].IsAutoSorted = value;
            }
            
        }
    }
}