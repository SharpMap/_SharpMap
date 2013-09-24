using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using DelftTools.Functions.Filters;
using DelftTools.Functions.Generic;
using DelftTools.Utils;
using DelftTools.Utils.Aop;
using DelftTools.Utils.Collections;
using DelftTools.Utils.Collections.Generic;
using DelftTools.Utils.Data;
using DelftTools.Utils.Editing;
using log4net;

namespace DelftTools.Functions
{
    /// <summary>
    /// Implements multi-dimensional discrete vector function consisting of a set of components and arguments.
    /// 
    /// See more links for reference: 
    /// http://mathworld.wolfram.com/VectorFunction.html
    /// http://www.mathreference.com/ca-mv,vec.html
    /// 
    /// Example:
    /// 
    /// F - velocity field in 2D space, each function value should contain 2 components
    /// to define X and Y components of the velocity.
    /// 
    /// F = (Vx, Vy)(t), where
    ///
    /// Vx, Vy - function components
    /// t - function argument
    /// </summary>
    [Entity(FireOnCollectionChange = false)]
    public class Function : EditableObjectUnique<long>, IFunction, IItemContainer
    {
        /// <summary>
        /// HACK: field used to route changes in function as a PropertyChanged event.
        /// </summary>
        public virtual bool Dummy { get; set; }

        private static readonly ILog log = LogManager.GetLogger(typeof(Function));
        protected const string DefaultName = "new function";

        [NoNotifyPropertyChange] 
        private IEventedList<IVariable> components;

        [NoNotifyPropertyChange]
        private IEventedList<IVariable> arguments;
        
        private string name;

        private readonly bool useDefaultValueIfNoValuesDefined;

        private IFunctionStore store;
        
        private IDictionary<string, string> attributes;

        //private bool isIndependent;
        /// <summary>
        /// Should the function notify observers of changes in the store?
        /// </summary>
        private bool skipObserverNotification;

        /// <summary>
        /// Creates an instance of a new Function which uses memory-based values store by default.
        /// </summary>
        public Function() : this(DefaultName)
        {
        }

        /// <summary>
        /// Creates an instance of a new Function which uses memory-based values store by default.
        /// </summary>
        public Function(string name)
        {
            this.name = name;

            Attributes = new Dictionary<string, string>();
            Filters = new EventedList<IVariableFilter>();
            Components = new EventedList<IVariable>();
            Arguments = new EventedList<IVariable>();

            // create default store for this function
            store = new MemoryFunctionStore();
            store.Functions.Add(this);
            Store = store;

            useDefaultValueIfNoValuesDefined = true;
            IsEditable = true;

            

            isIndependentDirty = true;
        }
        
        #region IFunction Members
        
        /// <summary>
        /// Readable name of the function.
        /// </summary>
        public virtual string Name
        {
            get { return name; }
            set { name = value; }
        }

        /// <summary>
        /// Gets function components.
        /// </summary>
        [NoNotifyPropertyChange]
        public virtual IEventedList<IVariable> Components
        {
            get { return components; }
            set
            {
                if (Components != null)
                {
                    Components.CollectionChanged -= Components_CollectionChanged;
                    ((INotifyPropertyChange)Components).PropertyChanged -= OnComponentPropertyChanged;
                    ((INotifyPropertyChange)Components).PropertyChanging -= OnComponentPropertyChanging;
                }

                components = value;

                if (Components != null)
                {
                    Components.CollectionChanged += Components_CollectionChanged;
                    if (!(this is IVariable))
                    {
                        ((INotifyPropertyChange)Components).PropertyChanged += OnComponentPropertyChanged;
                        ((INotifyPropertyChange)Components).PropertyChanging += OnComponentPropertyChanging;
                    }
                }
            }
        }

        /// <summary>
        /// Gets function arguments.
        /// </summary>
        [NoNotifyPropertyChange]
        public virtual IEventedList<IVariable> Arguments
        {
            get { return arguments; }
            set
            {
                if (Arguments != null)
                {
                    Arguments.CollectionChanged -= ArgumentsCollectionChanged;
                    ((INotifyPropertyChange)Arguments).PropertyChanged -= OnArgumentPropertyChanged;
                    ((INotifyPropertyChange)Arguments).PropertyChanging -= OnArgumentPropertyChanging;
                }

                arguments = value;

                if (Arguments != null)
                {
                    Arguments.CollectionChanged += ArgumentsCollectionChanged;
                    ((INotifyPropertyChange)Arguments).PropertyChanged += OnArgumentPropertyChanged;
                    ((INotifyPropertyChange)Arguments).PropertyChanging += OnArgumentPropertyChanging;
                }

                isIndependentDirty = true;
            }
        }

        public virtual IDictionary<string, string> Attributes
        {
            get { return attributes; }
            set { attributes = value; }
        }

        public virtual IFunctionStore Store
        {
            get { return NHStore; }
            set
            {
                NHStore = value;

                // propagate store to argument / component variables
                foreach (var variable in Arguments.Concat(Components))
                {
                    if (variable != this && variable.Store != store)
                    {
                        variable.Store = store;
                    }
                }
            }
        }

        protected virtual IFunctionStore NHStore
        {
            get { return (Parent != null) ? Parent.Store : store; }
            set
            {
                if (store != null)
                {
                    store.FunctionValuesChanging -= StoreValuesChanging;
                    store.FunctionValuesChanged -= StoreValuesChanged;
                }

                store = value;

                if (store != null)
                {
                    store.FunctionValuesChanging += StoreValuesChanging;
                    store.FunctionValuesChanged += StoreValuesChanged;
                }
               
            }
        }

        private void StoreValuesChanging(object sender, FunctionValuesChangingEventArgs e)
        {
            var isThisFunction = ReferenceEquals(e.Function, this);

            if (isThisFunction)
            {
                OnFunctionValuesChanging(e);
            }

            if (!isThisFunction && IsIndependent)
            {
                return; // no bubbling
            }

            if (ValuesChanging != null)
            {
                ValuesChanging(e.Function, e);
            }
        }
        
        private void FireValuesChanged(FunctionValuesChangingEventArgs e)
        {
            if (ValuesChanged != null)
            {
                ValuesChanged(e.Function, e);
            }
        }
        
        protected virtual bool ShouldReceiveChangedEventsForFunction(IFunction source)
        {
            bool thisFunction = source == this;

            //don't handle changes of other arguments..
            if (!thisFunction && IsIndependent)
            {
                return false;
            }

            return !skipObserverNotification || (ValuesChanged != null);
        }

        private void StoreValuesChanged(object sender, FunctionValuesChangingEventArgs e)
        {
            if ((e.Function == this) )
            {
                //do handle the change locally
                OnFunctionValuesChanged(e);
                //but don't 'publish' it
                if (!skipObserverNotification)
                {
                    NotifyObserversOnFunctionValuesChanged();    
                }
            }

            if (ValuesChanged != null && (!IsIndependent || e.Function == this))
            {
                // bubble event
                FireValuesChanged(e);
            }
        }

        protected virtual void OnFunctionValuesChanging(FunctionValuesChangingEventArgs e)
        {
        }

        protected virtual void OnFunctionValuesChanged(FunctionValuesChangingEventArgs e)
        {
        }

        /// <summary>
        /// Returns a function based on filters. 
        /// If for example a coverage is filtered on time it is still a coverage. If argument
        /// locations is reduced it would no longer be a coverage.
        /// </summary>
        /// <param name="filters"></param>
        /// <returns></returns>
        private IFunction GetNewFilteredFunctionInstance(IEnumerable<IVariableFilter> filters)
        {
            //any argument reduction results in a general function
            if (filters.Any(f => f is VariableReduceFilter && IsArgumentObligatoryForType(f.Variable)) && !(this is IVariable))
                return new Function();
            return CreateInstance();
        }

        protected virtual bool IsArgumentObligatoryForType(IVariable variable)
        {
            return false;
        }

        /// <summary>
        /// Create instance of this class, if many instances need to be created - override this method and create instance explicitly.
        /// </summary>
        /// <returns></returns>
        protected virtual IFunction CreateInstance()
        {
            return (IFunction) Activator.CreateInstance(GetType(), true);
        }

        /// <summary>
        /// Creates filtered function by filtering it's arguments to a specified set of values.
        /// Filtered function is still based on original function which can be accessed using <see cref="Parent"/> property.
        /// </summary>
        /// <returns></returns>
        public virtual IFunction Filter(params IVariableFilter[] filters)
        {
            var filteredFunction = GetNewFilteredFunctionInstance(filters);

            filteredFunction.Name = Name;
            filteredFunction.Parent = this;
            filteredFunction.Filters = filters;
            
            if (!IsIndependent)
            {
                if (!(filteredFunction is IVariable)) // clear components only for vector functions
                {
                    filteredFunction.Components.Clear();

                    foreach (var component in Components)
                    {
                        filteredFunction.Components.Add(component.Filter(filters));
                        // TODO: handle reduce filters here as well
                    }
                }

                filteredFunction.Arguments.Clear();

                foreach (var argument in Arguments)
                {
                    if (filters.OfType<VariableReduceFilter>().All(f => f.Variable != argument))
                    {
                        var newArgument = argument.Filter(filters);
                        newArgument.IsEditable = argument.IsEditable;
                        filteredFunction.Arguments.Add(newArgument);
                    }
                }
            }

            //log.DebugFormat("Filtered function created: {0}", filteredFunction.Name);

            return filteredFunction;
        }

        private IFunction parent;

        [NoNotifyPropertyChange]
        public virtual IFunction Parent
        {
            get { return parent; }
            set
            {
                // TODO: make this reference weak, so that filtered functions will be removed from memory when they are not needed anymore
                if (parent != null)
                {
                    parent.ValuesChanged -= ParentValuesChanged;    
                }
                parent = value;
                if (parent != null)
                {
                    parent.ValuesChanged += ParentValuesChanged;
                }
            }
        }

        private bool IsFunctionAffectedByParentChange(FunctionValuesChangingEventArgs e)
        {
            if (e.Action == NotifyCollectionChangeAction.Replace)
            {
                return true;
            }
            if (e.Function == null)
            {
                return true;
            }
            if (!e.Function.IsIndependent)
            {
                return false;
            }

            //change in argument and this function is reduced:
            var reduceFilter = Filters.OfType<VariableReduceFilter>().FirstOrDefault();
            if (reduceFilter != null)
            {
                var reduceValueFilter = Filters.OfType<IVariableValueFilter>().First();

                if (reduceFilter.Variable == e.Function && !reduceValueFilter.Values.Contains(e.Items))
                {
                    return false;
                }
            }
            return true;
        }

        void ParentValuesChanged(object sender, FunctionValuesChangingEventArgs e)
        {
            if (ValuesChanged == null) 
                return;

            // reset everything if we have too complicated change (argument is being changed or values being added / removed)
            if (e.Function == null 
                || e.Function.IsIndependent 
                || e.Action != NotifyCollectionChangeAction.Replace 
                || Arguments.Count != e.Function.Arguments.Count) //reduce filter
            {
                if (IsFunctionAffectedByParentChange(e))
                {
                    ValuesChanged(this,
                                  new FunctionValuesChangingEventArgs
                                      {
                                          Function = this,
                                          Action = NotifyCollectionChangeAction.Reset
                                      });
                }
            }
            else
            {
                var filteredVariable = GetFilteredVariable((IVariable)e.Function);

                if (filteredVariable == null)
                {
                    //turns out we're not interested
                    return;
                }

                var stride = filteredVariable.Values.Stride;
                var indexInChild = GetComponentValueIndexInChildComponent(e.MultiDimensionalIndex, stride);

                if(indexInChild == -1)
                {
                    return; // out of filtered arguments domain
                }

                var args = new FunctionValuesChangingEventArgs
                               {
                                   Action = e.Action,
                                   Cancel = e.Cancel,
                                   Function = filteredVariable,
                                   Index = indexInChild,
                                   Items = e.Items,
                                   Shape = e.Shape,
                                   Stride = stride
                               };
                ValuesChanged(this, args);
            }
        }

        private IVariable GetFilteredVariable(IVariable variable)
        {
            if(Parent == variable)
            {
                return (IVariable) this;
            }

            // search it in arguments / components (slow :()
            return Components.Concat(Arguments).FirstOrDefault(v => v.Parent == variable);
        }


        /// <summary>
        /// Compute index in filtered multi-dimensional array based on index in parent multi-dimensional array
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private int GetComponentValueIndexInChildComponent(int[] index, int[] stride)
        {
            var childIndex = new int[index.Length];

            for (var i = 0; i < index.Length; i++)
            {
                var argument = Arguments[i];
                var argumentParent = (IVariable)Arguments[i].Parent;

                childIndex[i] = argument.Values.IndexOf(argumentParent.Values[index[i]]);
                if(childIndex[i] == -1)
                {
                    return -1; // argument value is filtered
                }
            }

            return MultiDimensionalArrayHelper.GetIndex1d(childIndex, stride);
        }

        [NoNotifyPropertyChange]
        public virtual IList<IVariableFilter> Filters { get; set; }

        [NoNotifyPropertyChange]
        public virtual object this[params object[] argumentValues]
        {
            get
            {
                var filters = CreateArgumentFilters(argumentValues);
                IMultiDimensionalArray values = GetValues(filters);
                if (values.Count == 0)
                {
                    return null;
                }
                return values.Count == 1 ? values[0] : values;
            }
            set
            {
                var filters = CreateArgumentFilters(argumentValues);

                //dont treat string like ienumerable although it is 
                var values = value is IEnumerable && !(value is string) ? (IEnumerable)value : new[] { value };

                if (Components.Count == 1) //quicker this way
                {
                    Components[0].SetValues(values, filters);
                }
                else
                {
                    SetValues(values, filters);
                }
            }
        }

        public virtual IMultiDimensionalArray<T> GetValues<T>(params IVariableFilter[] filters)
        {
            if (Parent != null)
            {
                return Parent.GetValues<T>(GetFiltersInParent(filters));
            }

            // performance: avoid unnecessary function calls for simple functions
            if (filters.Length == 0 && Filters.Count == 0)
            {
                return Store.GetVariableValues<T>(Components[0]);
            }

            // TODO: interpret filters and types.
            IVariable variableToGet = Components.Count == 1 ? Components[0] : GetComponentFromFilters(filters);

            return Store.GetVariableValues<T>(variableToGet, GetFiltersInParent(filters));
        }

        public virtual IMultiDimensionalArray GetValues(params IVariableFilter[] filters)
        {

            //TODO: redirect to generic. override in variable
            if (Parent != null)
            {
                return Parent.GetValues(GetFiltersInParent(filters));
            }

            //IMultiDimensionalArray values = 
            //hook up event to the array below
            //values.CollectionChanged += onFunctionStructureChanged;
            IVariable variableToGet = GetComponentFromFilters(filters);

            return Store.GetVariableValues(variableToGet, GetFiltersInParent(filters));
        }

        public virtual bool IsEditable { get; set; }


        /// <summary>
        /// Returns filters containing variables which exist in the store. 
        /// If variable in the filter is filtered - use parent vararible.
        /// </summary>
        /// <param name="filters"></param>
        /// <returns></returns>
        private IVariableFilter[] GetFiltersInParent(IEnumerable<IVariableFilter> filters)
        {
            if (!filters.Any() && Filters.Count == 0)
            {
                return new IVariableFilter[0];
            }

            var allFilters = new List<IVariableFilter>();
            
            //convert variableValuefilter to parent..some assumptions here refactor to a more decorated architecture
            foreach (IVariableFilter filter in filters)
            {
                //TODO : refactor to a base class or something
                if ((filter is IVariableValueFilter) && (filter.Variable.Parent != null))
                {
                    var valueFilter = (filter as IVariableValueFilter);
                    var parentVariable = (IVariable) (valueFilter.Variable.Parent);
                    allFilters.Add(parentVariable.CreateValuesFilter(valueFilter.Values));
                }

                else if ((filter is VariableIndexRangesFilter) && (filter.Variable.Parent != null))
                {
                    //this assumen indexes in the parent are the same..
                    var indexFilter = (filter as VariableIndexRangesFilter);
                    allFilters.Add(new VariableIndexRangesFilter((IVariable) (indexFilter.Variable.Parent),
                                                                 indexFilter.IndexRanges));
                }
                else if ((filter is VariableAggregationFilter) && (filter.Variable.Parent != null))
                {
                    //this assumen indexes in the parent are the same..
                    var aggregationFilter = (filter as VariableAggregationFilter);
                    var clone = (VariableAggregationFilter) aggregationFilter.Clone();
                    clone.Variable = (IVariable) filter.Variable.Parent;
                    allFilters.Add(clone);
                }
                else
                {
                    allFilters.Add(filter);
                }
            }

            if(Filters.Count != 0)
            {
                allFilters = Filters.Concat(allFilters).ToList();
            }

            List<IVariableFilter> uniqueFilters = CombineCompatibleFilters(allFilters);
            if (uniqueFilters.OfType<IVariableValueFilter>().Any(f => f.Values.Count == 0))
            {
                throw new InvalidOperationException("Variable value filters should have values!");
            }
            return uniqueFilters.ToArray();
        }

        /// <summary>
        /// Combines compatible filters. 
        /// </summary>
        /// <param name="allFilters"></param>
        /// <returns></returns>
        private static List<IVariableFilter> CombineCompatibleFilters(IEnumerable<IVariableFilter> allFilters)
        {
            var uniqueFilters = new List<IVariableFilter>();
            foreach (var filter in allFilters)
            {
                IVariableFilter f2 = filter;
                if (!uniqueFilters.Any(f => f.GetType() == f2.GetType() && f.Variable == f2.Variable))
                    // check if it is not added already
                {
                    //for every type of filter and for every variable we only need one filter.
                    var compatibleFilters =
                        allFilters.Where(f => f.GetType() == f2.GetType() && f.Variable == f2.Variable);
                    uniqueFilters.Add(compatibleFilters.Intersect());
                }
            }
            return uniqueFilters;
        }
        
        protected double GetRatio(object value, object leftValue, object rightValue)
        {
            if (value is double)
                return GetDoubleRatio((double)value, (double)leftValue, (double)rightValue);
            if (value is DateTime)
                return GetDateTimeRatio((DateTime) value, (DateTime) leftValue, (DateTime) rightValue);
            throw new NotSupportedException("Cannot determine ratio");
        }

        private static double GetDateTimeRatio(DateTime value, DateTime leftValue, DateTime rightValue)
        {
            return (value - leftValue).TotalMilliseconds / (rightValue - leftValue).TotalMilliseconds;
        }

        private static double GetDoubleRatio(double value, double leftValue, double rightValue)
        {
            return  (value- leftValue) / (rightValue- leftValue);
        }

        private void ValidateOrThrow(IVariableFilter[] filters) // TODO: migrate me to ValidationAspects
        {
            if (filters == null)
            {
                throw new ArgumentNullException("filters");
            }
            if (Components.Count != 1)
            {
                throw new NotSupportedException("Interpolation is currently supported only for 1 component functions");
            }
            if (Arguments.Count > 2)
            {
                throw new NotSupportedException("Interpolation is currently supported only for 1d and 2d functions");
            }
            var notFoundVariable =
                filters.OfType<IVariableValueFilter>().FirstOrDefault(
                    f => !Arguments.Concat(Components).Contains(f.Variable));
            if (notFoundVariable != null)
            {
                var message = string.Format("Variable '{0}' was not found in arguments or components of function '{1}'",
                                            notFoundVariable.Variable, Name);
                throw new NotSupportedException(message);
            }

            //validate the argument values are compatible with extrapolation
            if (!useDefaultValueIfNoValuesDefined || Components[0].Values.Count != 0)
                ValidateArgumentValuesAreInRange(filters);
        }

        private void ValidateArgumentValuesAreInRange(IEnumerable<IVariableFilter> filters)
        {
            var variableValueFilters = filters.OfType<IVariableValueFilter>();
            foreach (IVariable arg in Arguments)
            {
                IVariable argument = arg;
                var argumentValue = (IComparable)variableValueFilters.First(f => f.Variable == argument).Values[0];
                
                bool needToExtrapolate;
                if (argument.Values.Count == 0)
                {
                    needToExtrapolate = true;
                }
                else
                {
                    needToExtrapolate = argumentValue.CompareTo(argument.Values[0]) < 0 ||
                                        (argumentValue.CompareTo(argument.Values[argument.Values.Count - 1])) > 0;
                }

                if (needToExtrapolate && arg.ExtrapolationType == ExtrapolationType.None)
                {
                    throw new ArgumentOutOfRangeException("Extrapolation is disabled");
                }
            }
        }
        
        public virtual T Evaluate<T>(params IVariableFilter[] filters)
        {
            ValidateOrThrow(filters);

            var argumentValues = new object[Arguments.Count];
            for (int i = 0; i < Arguments.Count; i++)
            {
                int index = i;
                var argumentFilter = filters.OfType<IVariableValueFilter>().First(f => f.Variable == Arguments[index]);
                argumentValues[i] = argumentFilter.Values[0];
            }
            
            return EvaluateArguments<T>(argumentValues);
        }

        /// <summary>
        /// Gives function value for the given argument values (interpolated,extrapolated or defined). Used by evaluate.
        /// TODO: name is WRONG!
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="argumentValues"></param>
        /// <returns></returns>
        private T EvaluateArguments<T>(object[] argumentValues)
        {
            int freeArgumentIndex = -1;
            for (int i = 0; i < argumentValues.Length; i++)
            {
                if (!Arguments[i].Values.Contains(argumentValues[i]))
                {
                    freeArgumentIndex = i;
                    break;
                }
            }

            //no free arguments..found
            if (freeArgumentIndex == -1)
            {
                //convert back to argument filters
                var variableValueFilters = argumentValues.Select((t, i) => Arguments[i].CreateValueFilter(t)).ToArray();
#if MONO
				IMultiDimensionalArray array = GetValues<T>(filters);
                return (T)array[0];
#else

                return GetValues<T>(variableValueFilters)[0];
#endif
            }

            //TODO : get extrapolation working here by returning null and doing something
            var argumentValue = (IComparable) argumentValues[freeArgumentIndex];
            IComparable leftArgumentValue = GetLeftValue(freeArgumentIndex, argumentValue);
            IComparable rightArgumentValue = GetRightValue(freeArgumentIndex, argumentValue);
            if (leftArgumentValue == null || rightArgumentValue == null)
            {
                return GetExtrapolatedValue<T>(argumentValues, freeArgumentIndex, leftArgumentValue, rightArgumentValue);
            }

            //interpolate linear or constant
            if (Arguments[freeArgumentIndex].InterpolationType == InterpolationType.Linear)
            {
                return GetLinearInterpolatedValue<T>(argumentValues, freeArgumentIndex, argumentValue, leftArgumentValue, rightArgumentValue);
            }
            
            if (Arguments[freeArgumentIndex].InterpolationType == InterpolationType.Constant)
            {
                object[] leftArgumentValues = (object[])argumentValues.Clone();
                leftArgumentValues[freeArgumentIndex] = leftArgumentValue;
                return EvaluateArguments<T>(leftArgumentValues);
            }
            throw new ArgumentOutOfRangeException("No interpolation method specified");

        }

        private T GetLinearInterpolatedValue<T>(object[] argumentValues, int freeArgumentIndex, IComparable argumentValue, IComparable leftArgumentValue, IComparable rightArgumentValue)
        {
            double argumentRatio = GetRatio(argumentValue, leftArgumentValue, rightArgumentValue);
            //double? argumentRatio = (argumentValue - leftArgumentValue)/(rightArgumentValue - leftArgumentValue);

            var leftArgumentValues = (object[])argumentValues.Clone();
            leftArgumentValues[freeArgumentIndex] = leftArgumentValue;
            var leftComponentValue = EvaluateArguments<T>(leftArgumentValues);

            var rightArgumentValues = (object[])argumentValues.Clone();
            rightArgumentValues[freeArgumentIndex] = rightArgumentValue;
            var rightComponentValue = EvaluateArguments<T>(rightArgumentValues);

            //OMG WTF 
            double dr = Convert.ToDouble(rightComponentValue);
            double dl = Convert.ToDouble(leftComponentValue);

            /*if (!(typeof(T) == typeof(double)))
                    throw new NotImplementedException("Not yet");*/
            return (T)Convert.ChangeType(dl + (dr - dl) * argumentRatio, typeof(T));
        }

        private T GetExtrapolatedValue<T>(object[] argumentValues, int freeArgumentIndex, IComparable leftArgumentValue, IComparable rightArgumentValue)
        {
            if (leftArgumentValue == null && rightArgumentValue == null)
            {
                return (T) Components[0].DefaultValue;
            }

            var newArgumentValues = (object[]) argumentValues.Clone();
            
            if (Arguments[freeArgumentIndex].ExtrapolationType == ExtrapolationType.Constant)
            {
                newArgumentValues[freeArgumentIndex] = leftArgumentValue ?? rightArgumentValue;
                return EvaluateArguments<T>(newArgumentValues);
            }
            
            if (Arguments[freeArgumentIndex].ExtrapolationType == ExtrapolationType.Linear)
            {
                var xValue = argumentValues[freeArgumentIndex];
                var freeArgumentValues = Arguments[freeArgumentIndex].Values;

                if (freeArgumentValues.Count == 1) //cannot do linear interpolation here, so do constant
                {
                    newArgumentValues[freeArgumentIndex] = leftArgumentValue ?? rightArgumentValue;
                    return EvaluateArguments<T>(newArgumentValues);
                }

                // Set x-values:
                object x1, x2;
                if (leftArgumentValue == null)
                {
                    // Extrapolate leftward:
                    x1 = freeArgumentValues[0];
                    x2 = freeArgumentValues[1];
                }
                else
                {
                    // Extrapolate rightward:
                    x1 = freeArgumentValues[freeArgumentValues.Count - 2];
                    x2 = freeArgumentValues[freeArgumentValues.Count - 1];
                }

                // Get y-values for x-values:
                newArgumentValues[freeArgumentIndex] = x2;
                var y2 = EvaluateArguments<T>(newArgumentValues);

                newArgumentValues[freeArgumentIndex] = x1;
                var y1 = EvaluateArguments<T>(newArgumentValues);

                // Extrapolate linearly:
                return DetermineValueFromGradient(y1, y2, x1, x2, xValue);
            }
            
            if (Arguments[freeArgumentIndex].ExtrapolationType == ExtrapolationType.Periodic)
            {
                object valueInPeriod;
                var argMaxValue = Arguments[freeArgumentIndex].Values.MaxValue;
                var argMinValue = Arguments[freeArgumentIndex].Values.MinValue;
                var argumentValue = argumentValues[freeArgumentIndex];

                //determine how many times the period fits the argument value
                // 0 1 2 --> Period == 2
                // for 5 period = 2
                if (argumentValues[freeArgumentIndex] is double)
                {
                    var period = (double)argMaxValue - (double)argMinValue;

                    valueInPeriod = DetermineValueInPeriod((double)argumentValue, (double)argMinValue, period);
                }
                else if (argumentValues[freeArgumentIndex] is DateTime)
                {
                    var period = (DateTime)argMaxValue - (DateTime)argMinValue;

                    valueInPeriod = DetermineValueInPeriod((DateTime)argumentValue, (DateTime)argMinValue, period);
                }
                else
                {
                    throw new NotImplementedException(string.Format("Periodic extrapolation is not support for arguments of type {0}", Arguments[freeArgumentIndex].ValueType));
                }

                //update arguments and continue evaluation
                newArgumentValues[freeArgumentIndex] = valueInPeriod;
                return EvaluateArguments<T>(newArgumentValues);
            }

            throw new ArgumentOutOfRangeException("ExtrapolationType set to none.");
        }

        private static double DetermineValueInPeriod(double argumentValue, double argMinValue, double period)
        {
            var n = Convert.ToInt32(Math.Floor((argumentValue - argMinValue) / period));

            // value in period is determined
            return argumentValue - n * period;
        }

        private static DateTime DetermineValueInPeriod(DateTime argumentValue, DateTime argMinValue, TimeSpan period)
        {
            //convert to tick to find out how many periods fit
            double ratio = (double)(argumentValue - argMinValue).Ticks / period.Ticks;

            //subtract whole periods
            var n = Convert.ToInt32(Math.Floor(ratio));
            return argumentValue - new TimeSpan(n * period.Ticks);
        }

        private T DetermineValueFromGradient<T>(T y1, T y2, object x1, object x2, object xValue)
        {
            //(dY / dX)
            double gradient = GetGradient(y1, y2, x1, x2);
            double value = Convert.ToDouble(y1) -
                           gradient*(SubstractAsDouble(x1, xValue));
            return (T) Convert.ChangeType(value, typeof (T));
        }

        private double GetGradient(object Y1, object Y2, object X1, object X2)
        {
            var substractAsDouble = SubstractAsDouble(Y2, Y1);
            var asDouble = SubstractAsDouble(X2, X1);
            return substractAsDouble/asDouble;
        }

        private double AddAsDouble(object x2, object x1)
        {
            return Convert.ToDouble(x2) + Convert.ToDouble(x1);
        }

        private double SubstractAsDouble(object x2, object x1)
        {
            if (x2 is DateTime)
            {
                return ((DateTime) x2 - (DateTime) x1).Ticks;
            }
            return Convert.ToDouble(x2) - Convert.ToDouble(x1);
        }


        private IComparable GetRightValue(int argumentIndex, IComparable value)
        {
            return FunctionHelper.GetFirstValueBiggerThan(value, Arguments[argumentIndex].Values);
        }

        private IComparable GetLeftValue(int argumentIndex, IComparable value)
        {
            //get the last smaller than value
            return FunctionHelper.GetLastValueSmallerThan(value, Arguments[argumentIndex].Values);
        }

        [EditAction(typeof(FunctionSetValuesEditAction))]
        public virtual void SetValues(IEnumerable values, params IVariableFilter[] filters)
        {
            if (!values.Cast<object>().Any())
                throw new ArgumentOutOfRangeException("values", "Size of 'values' argument must be greater than 0.");
            
            if (Parent != null)
            {
                Parent.SetValues(values, GetFiltersInParent(filters));
            }
            else
            {
                lock (Store)
                {
                    //split up the enumerable of values among the components..
                    IList<IVariable> variablesToSet = GetComponentsToSet(filters);
                    List<Type> types = variablesToSet.Select(v => v.ValueType).ToList();
                    IList<IEnumerable> enumerablesToSet = FunctionHelper.SplitEnumerable(values, types);
                    //set the components one-by-one
                    for (int i = 0; i < variablesToSet.Count; i++)
                    {
                        variablesToSet[i].SetValues(enumerablesToSet[i], filters);
                    }
                }
                NotifyObserversOnFunctionValuesChanged();
            }
        }

        [EditAction(typeof(FunctionRemoveValuesEditAction))]
        public virtual void RemoveValues(params IVariableValueFilter[] filters)
        {
            lock (Store)
            {
                var allFilters = GetFiltersInParent(filters).
                    Where(x => x is IVariableValueFilter).
                    Cast<IVariableValueFilter>().ToArray();

                if (Parent != null)
                {
                    Parent.RemoveValues(allFilters);
                }
                else
                {
                    Store.RemoveFunctionValues(this, allFilters);
                }
            }
            NotifyObserversOnFunctionValuesChanged();
            
        }

        /// <summary>
        /// Use to notify observers (views) of a change in the function.
        /// 
        /// HACK: we should handle CollectionChanged or other event instead of redirecting collection changed as property changed
        /// </summary>
        [EditAction]
        private void NotifyObserversOnFunctionValuesChanged()
        {
            Dummy = !Dummy;
        }

        public virtual void Clear()
        {
            RemoveValues();

            foreach (var arg in arguments.Where(a => a.IsFixedSize))
            {
                arg.FixedSize = 0; //otherwise NetCdf will write (and read!!) back default values
            }
        }

        //TODO : move to variable?..it is now non-public because division among components is not done here
        //now. Maybe refactor if needed. Move to varaible?
        [EditAction(typeof(FunctionSetValuesEditAction))]
        protected virtual void SetValues<T>(IEnumerable<T> values, params IVariableFilter[] filters)
        {
            //skip notification during set...we'll do this in the end
            skipObserverNotification = true;
            if (Parent != null)
            {
                Parent.SetValues(values, GetFiltersInParent(filters));
            }
            else
            {
                lock (Store)
                {
                    //if the variable is independ scale the independend values first
                    //TODO: clean up by overriding in variable.
                    IVariable variableToSet = GetComponentFromFilters(filters);
                    if (!variableToSet.IsIndependent)
                    {
                        AddArgumentVariableValuesFromFilters(variableToSet, filters);
                    }
                    Store.SetVariableValues(variableToSet, values, GetFiltersInParent(filters));
                }
            }
            skipObserverNotification = false;
            NotifyObserversOnFunctionValuesChanged();
            
        }

        private IList<IVariable> GetComponentsToSet(IVariableFilter[] filters)
        {
            if (!filters.Any(f => f is ComponentFilter))
                return Components;
            //return the component which are mentioned by the filters
            var q = from c in Components
                    where filters.OfType<ComponentFilter>().Any(f => f.Variable == c)
                    select c;

            return q.ToList();
        }

        /// <summary>
        /// Returns the variable for which a component filter is defined.
        /// </summary>
        /// <param name="filters"></param>
        /// <returns></returns>
        private IVariable GetComponentFromFilters(IVariableFilter[] filters)
        {
            IVariable variableToSet = Components[0];
            //apply component filters or default to component 0 
            if (filters.Any(f => f is ComponentFilter))
            {
                if (filters.Count(f => f is ComponentFilter) > 1)
                {
                    throw new NotImplementedException("Setting or getting multiple components at once is not supported");
                }
                variableToSet = filters.First(f => f is ComponentFilter).Variable;
            }
            return variableToSet;
        }

        private void AddArgumentVariableValuesFromFilters(IFunction function, IEnumerable<IVariableFilter> filters)
        {
            foreach (var variableValueFilter in filters.OfType<IVariableValueFilter>())
            {
                if (variableValueFilter.Values.Count == 0)
                {
                    continue;
                }

                //expand dimensions given the filters. this should resize the other arrays
                if (!function.Arguments.Contains(variableValueFilter.Variable))
                {
                    throw new NotSupportedException(
                        "Only filtering values on argument variables is supported for now: " +
                        variableValueFilter.Variable.Name);
                }
                //Store.AddIndependendVariableValues(variableValueFilter.Variable, variableValueFilter.Values);
                variableValueFilter.Variable.AddValues(variableValueFilter.Values); //todo: shouldn't we add only values not yet present? this gives issues in NetCdf
                //SetVariableValues<double>(variableValueFilter.Variable, (IEnumerable<double>)variableValueFilter.Values);
            }
        }

        #endregion

        protected virtual void ArgumentsCollectionChanged(object sender, NotifyCollectionChangingEventArgs e)
        {
            var argument = (IVariable) e.Item;
            //TODO: this is too complex. just rewrite the component's schema on a schema change.
            switch (e.Action)
            {
                case NotifyCollectionChangeAction.Add:
                    // add argument to the current Store if not filtered
                    if (argument.Store != Store && Parent == null)
                    {
                        if (this is IVariable)
                        {
                            //can we get this by definition?
                        }
                        Store.Functions.Add(argument);
                        isIndependentDirty = true;

                        // add argument to our components
                        foreach (IVariable component in Components)
                        {
                            if (!component.Arguments.Contains(argument))
                            {
                                component.Arguments.Insert(e.Index, argument);

                                component.Store.UpdateVariableSize(component);
                            }
                        }
                    }
                    break;

                case NotifyCollectionChangeAction.Remove:
                    Store.Functions.Remove(argument);
                    isIndependentDirty = true;
                    foreach (IVariable component in Components)
                    {
                        if (component.Arguments.Contains(argument))
                        {
                            component.Arguments.Remove(argument);
                        }
                    }
                    break;

                case NotifyCollectionChangeAction.Replace:
                    throw new NotImplementedException();

            }
        }

        protected virtual void Components_CollectionChanged(object sender, NotifyCollectionChangingEventArgs e)
        {
            var component = (IFunction) e.Item;

            if (component == this)
            {
                return;
            }

            switch (e.Action)
            {
                case NotifyCollectionChangeAction.Add:
                    if (component.Store != Store)
                    {
                        Store.Functions.Add(component);
                    }
                    //cannot simple assign arguments array to the component...nhibernate
                    foreach (var argument in Arguments)
                    {
                        if (!component.Arguments.Contains(argument))
                        {
                            component.Arguments.Add(argument);
                        }
                    }
                    if (component.Parent == null)
                    {
                        var variable = (IVariable)component;
                        component.Store.UpdateVariableSize(variable);
                    }
                    //component.Arguments = Arguments; 

                    break;

                case NotifyCollectionChangeAction.Remove:
                    Store.Functions.Remove(component);
                    break;
                case NotifyCollectionChangeAction.Replace:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// A function is independ if it does not have any arguments AND there is no independend function
        /// with more components of which this is one.
        /// </summary>
        public virtual bool IsIndependent
        {
            get
            {
                if (isIndependentDirty)
                {
                    isIndependent = Arguments.Count == 0;
                    isIndependentDirty = false;
                }
                
                return isIndependent;
            }
        }

        public virtual event EventHandler<FunctionValuesChangingEventArgs> ValuesChanging;

        public virtual event EventHandler<FunctionValuesChangingEventArgs> ValuesChanged;

        protected virtual IVariableFilter[] CreateArgumentFilters(object[] argumentValues)
        {
            if (argumentValues.Length > Arguments.Count)
            {
                throw new ArgumentOutOfRangeException("argumentValues",
                                                      "Number of argument values is greater than number of arguments");
            }

            var filters = new IVariableFilter[argumentValues.Length];
            for (int i = 0; i < argumentValues.Length; i++)
            {
                filters[i] = Arguments[i].CreateValueFilter(argumentValues[i]);
            }

            return filters;
        }

        public override string ToString()
        {
            return Name; // TODO: return structure here, like F=(fx, fy)(x, y, t)
        }

        public virtual IEnumerable<object> GetDirectChildren()
        {
            if (Store != null)
            {
                yield return Store;
            }
        }

        public virtual object Clone()
        { 
            return Clone(true);
            
        }

        public virtual object Clone(bool copyValues)
        {
            return Clone(copyValues, false, false);
        }

        public virtual object Clone(bool copyValues, bool skipArguments, bool skipComponents)
        {
            if (copyValues)
            {
                var storeClone = (IFunctionStore)Store.Clone();
                var functionIndex = Store.Functions.IndexOf(this); // TODO: don't use index here!

                if (functionIndex >= storeClone.Functions.Count)
                {
                    log.WarnFormat("Clone was unsuccessful: missing data?");
                }
                else
                {
                    if (functionIndex != -1)
                    {
                        return storeClone.Functions[functionIndex];
                    }
                }
            }

            var clone = GetType() == typeof(Function) ? new Function() : CreateInstance();
            
            if (Filters.Count > 0)
            {
                throw new NotImplementedException("Clone filters property (redirect to new cloned variables)");
            }

            clone.Name = Name;

            clone.Arguments = new EventedList<IVariable>();

            clone.Attributes = new Dictionary<string, string>();
            foreach(var attributeKey in Attributes.Keys)
            {
                clone.Attributes.Add(attributeKey, Attributes[attributeKey]);
            }

            if (!skipArguments)
            {
                foreach (var argument in Arguments)
                {
                    var newArgument = (IVariable) argument.Clone(false);
                    clone.Arguments.Add(newArgument);
                }
            }

            if (!(clone is IVariable))
            {
                clone.Components = new EventedList<IVariable>();

                if (!skipComponents)
                {
                    foreach (var component in Components)
                    {
                        var newComponent = (IVariable) component.Clone(false);

                        //don't take the cloned arguments. We have them in the function
                        newComponent.Arguments.Clear();
                        clone.Components.Add(newComponent);
                    }
                }
            }

            return clone;
        }

        public virtual string ToXml()
        {
            return FunctionHelper.ToXml(this);
        }

        #region INotifyCollectionChange Members

        public virtual event NotifyCollectionChangedEventHandler CollectionChanged;

        public virtual event NotifyCollectionChangingEventHandler CollectionChanging;

        #endregion

        private bool ShouldBubblePropertyEventForComponent(object sender)
        {
            if (!ReferenceEquals(sender, this))
            {
                var variable = sender as IVariable;
                if (variable != null && !variable.IsIndependent)
                {
                    return true;
                }
            }
            return false;
        }
        
        private void OnComponentPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (ShouldBubblePropertyEventForComponent(sender))
            {
                OnPropertyChanged(sender, e);
            }
        }

        private void OnComponentPropertyChanging(object sender, PropertyChangingEventArgs e)
        {
            if (ShouldBubblePropertyEventForComponent(sender))
            {
                OnPropertyChanging(sender, e);
            }
        }

        private void OnArgumentPropertyChanged(object sender, PropertyChangedEventArgs eventArgs)
        {
            if (!ReferenceEquals(sender, this) && sender is IVariable)
            {
                OnPropertyChanged(sender, eventArgs);
            }
        }

        private void OnArgumentPropertyChanging(object sender, PropertyChangingEventArgs eventArgs)
        {
            if (!ReferenceEquals(sender, this) && sender is IVariable)
            {
                OnPropertyChanging(sender, eventArgs);
            }
        }


        public virtual object Clone(IFunctionStore targetStore)
        {
            throw new NotImplementedException();
        }

        bool INotifyCollectionChange.HasParentIsCheckedInItems { get; set; }

        bool INotifyCollectionChange.SkipChildItemEventBubbling { get; set; }

        private bool isIndependent; // cache
        protected bool isIndependentDirty; // cache

        private class FunctionSetValuesEditAction : EditActionBase
        {
            public FunctionSetValuesEditAction() : base("Set Function Values") { }
        }

        private class FunctionRemoveValuesEditAction : EditActionBase
        {
            public FunctionRemoveValuesEditAction() : base("Remove Function Values") { }
        }

        #region PropertyChanged
        private static readonly MethodInfo onPropertyChanging = typeof(Function).GetMethod("OnPropertyChanging", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo onPropertyChanged = typeof(Function).GetMethod("OnPropertyChanged", BindingFlags.Instance | BindingFlags.NonPublic);

        private Action<object, PropertyChangingEventArgs> _onPropertyChangingAction;
        private Action<object, PropertyChangingEventArgs> OnPropertyChanging
        {
            get
            {
                return _onPropertyChangingAction ??
                       (_onPropertyChangingAction =
                        (Action<object, PropertyChangingEventArgs>)
                        Delegate.CreateDelegate(typeof (Action<object, PropertyChangingEventArgs>), this, onPropertyChanging));
            }
        }

        private Action<object, PropertyChangedEventArgs> _onPropertyChangedAction;
        private Action<object, PropertyChangedEventArgs> OnPropertyChanged
        {
            get
            {
                return _onPropertyChangedAction ??
                       (_onPropertyChangedAction =
                        (Action<object, PropertyChangedEventArgs>)
                        Delegate.CreateDelegate(typeof(Action<object, PropertyChangedEventArgs>), this, onPropertyChanged));
            }
        }
        #endregion

        #region IEditableObject overrides

        public override void BeginEdit(IEditAction action)
        {
            if (Parent != null)
                Parent.BeginEdit(action);
            else
                base.BeginEdit(action);
        }

        public override void EndEdit()
        {
            if (Parent != null)
                Parent.EndEdit();
            else
                base.EndEdit();
        }

        public override void CancelEdit()
        {
            if (Parent != null)
                Parent.CancelEdit();
            else
                base.CancelEdit();
        }

        public override bool IsEditing
        {
            get
            {
                if (Parent != null)
                    return Parent.IsEditing;
                return base.IsEditing;
            }
        }

        public override bool EditWasCancelled
        {
            get
            {
                if (Parent != null)
                    return Parent.EditWasCancelled;
                return base.EditWasCancelled;
            }
        }

        public override IEditAction CurrentEditAction
        {
            get
            {
                if (Parent != null)
                    return Parent.CurrentEditAction;
                return base.CurrentEditAction;
            }
        }

        public override bool IsNestedEditingDone()
        {
            if (Parent != null)
                return Parent.IsNestedEditingDone();
            return base.IsNestedEditingDone();
        }

        #endregion
    }
};