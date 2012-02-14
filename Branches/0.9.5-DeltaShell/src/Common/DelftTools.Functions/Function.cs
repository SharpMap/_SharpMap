using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using DelftTools.Functions.Filters;
using DelftTools.Functions.Generic;
using DelftTools.Utils;
using DelftTools.Utils.Aop.NotifyPropertyChange;
using DelftTools.Utils.Collections;
using DelftTools.Utils.Collections.Generic;
using DelftTools.Utils.Data;
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
    [Serializable]
    [NotifyPropertyChange]
    public class Function : Unique<long>, IFunction, IItemContainer
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
        
        [NoNotifyPropertyChange]
        private string name;

        private readonly bool useDefaultValueIfNoValuesDefined;

        private IFunctionStore store;
        
        [NoNotifyPropertyChange]
        private bool attached = true;
        
        private IDictionary<string, string> attributes;

        [NoNotifyPropertyChange]
        private NotifyPropertyChangeImplementation _propertyChangeImplementation;

        //System.ComponentModel.INotifyPropertyChanged will be added by postsharp, so by reflection we can fire the property changed event
        static FieldInfo propertyChangedImplementationField = typeof(Function).GetField("~DelftTools.Utils.INotifyPropertyChange", BindingFlags.NonPublic | BindingFlags.Instance);
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

            _propertyChangeImplementation = (NotifyPropertyChangeImplementation) propertyChangedImplementationField.GetValue(this);

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
                UnSubscribeEventsForComponents();
/*
                if (Components != null)
                {
                    Components.CollectionChanged -= Components_CollectionChanged;
                }
*/
                components = value;

                SubscribeEventsForComponents();

/*                if (Components != null)
                {
                    Components.CollectionChanged += Components_CollectionChanged;
                }*/
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
                //argument subscription is done manually since bubbling bubbles too much :(
                //TODO: fix the bubbling.
                UnSubScribeEventsForArguments();
                arguments = value;
                SubscribeEventsForArguments();
                //isIndependent = arguments.Count == 0;
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
                    store.FunctionValuesChanged -= Store_ValuesChanged;
                }

                store = value;

                if (store != null)
                {
                    store.FunctionValuesChanging += StoreValuesChanging;
                    store.FunctionValuesChanged += Store_ValuesChanged;
                }
               
            }
        }

        private void StoreValuesChanging(object sender, FunctionValuesChangingEventArgs e)
        {
            if (e.Function == this)
            {
                OnFunctionValuesChanging(e);
            }

            if(IsIndependent && e.Function != this)
            {
                return; // no bubbling
            }

            if (ValuesChanging != null)
            {
                ValuesChanging(e.Function, e);
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

        private void Store_ValuesChanged(object sender, FunctionValuesChangingEventArgs e)
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
                ValuesChanged(e.Function, e);
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
            if (filters.Any(f => f is VariableReduceFilter) && !(this is IVariable))
                return new Function();
            return CreateInstance();
        }

        /// <summary>
        /// Create instance of this class, if many instances need to be created - override this method and create instance explicitly.
        /// </summary>
        /// <returns></returns>
        protected virtual IFunction CreateInstance()
        {
            return (IFunction) Activator.CreateInstance(GetType());
        }

        /// <summary>
        /// Creates filtered function by filtering it's arguments to a specified set of values.
        /// Filtered function is still based on original function which can be accessed using <see cref="Parent"/> property.
        /// </summary>
        /// <returns></returns>
        public virtual IFunction Filter(params IVariableFilter[] filters)
        {
            var filteredFunction = GetNewFilteredFunctionInstance(filters);

            filteredFunction.Name = Name + " (filtered)";
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
                    if (!filters.OfType<VariableReduceFilter>().Any(f => f.Variable == argument))
                    {
                        filteredFunction.Arguments.Add(argument.Filter(filters));
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
                    parent.ValuesChanged -= parent_ValuesChanged;    
                }
                parent = value;
                if (parent != null)
                {
                    parent.ValuesChanged += parent_ValuesChanged;
                }
            }
        }

        void parent_ValuesChanged(object sender, FunctionValuesChangingEventArgs e)
        {
            if(ValuesChanged != null)
            {
                // reset everything if we have too complicated change (argument is being changed or values being added / removed)
                if (e.Function == null 
                    || e.Function.IsIndependent 
                    || e.Action != NotifyCollectionChangeAction.Replace 
                    || Arguments.Count != e.Function.Arguments.Count) //reduce filter
                {
                    ValuesChanged(this, new FunctionValuesChangingEventArgs { Action = NotifyCollectionChangeAction.Reset });
                }
                else
                {
                    var filteredVariable = GetFilteredVariable((IVariable)e.Function);
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
                IEnumerable values = value is IEnumerable && !(value is string) ? (IEnumerable)value : new[] { value };

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
            if (filters.Count() == 0 && Filters.Count == 0)
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
                    VariableIndexRangesFilter indexFilter = (filter as VariableIndexRangesFilter);
                    allFilters.Add(new VariableIndexRangesFilter((IVariable) (indexFilter.Variable.Parent),
                                                                 indexFilter.IndexRanges));
                }
                else if ((filter is VariableAggregationFilter) && (filter.Variable.Parent != null))
                {
                    //this assumen indexes in the parent are the same..
                    VariableAggregationFilter aggregationFilter = (filter as VariableAggregationFilter);
                    VariableAggregationFilter clone = (VariableAggregationFilter) aggregationFilter.Clone();
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
        private List<IVariableFilter> CombineCompatibleFilters(IEnumerable<IVariableFilter> allFilters)
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

        private void ValidateArgumentValuesAreInRange(IVariableFilter[] filters)
        {
            var variableValueFilters = filters.OfType<IVariableValueFilter>();
            foreach (IVariable arg in Arguments)
            {
                IVariable argument = arg;
                IComparable argumentValue =
                    (IComparable)variableValueFilters.FirstOrDefault(f => f.Variable == argument).Values[0];
                
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

            object[] argumentValues = new object[Arguments.Count];
            for (int i = 0; i < Arguments.Count; i++)
            {
                int index = i;
                var argumentFilter = filters.OfType<IVariableValueFilter>().FirstOrDefault(f => f.Variable == Arguments[index]);
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
                var argumentFilters = new List<IVariableValueFilter>();
                for (int i = 0; i < argumentValues.Length; i++)
                {
                    argumentFilters.Add(Arguments[i].CreateValueFilter(argumentValues[i]));
                }

#if MONO			
				IVariableFilter[] filters = argumentFilters.ToArray();
				IMultiDimensionalArray array = GetValues<T>(filters);
                return (T)array[0];
#else
				return GetValues<T>(argumentFilters.ToArray())[0];
#endif				
            }

            //TODO : get extrapolation working here by returning null and doing something
            IComparable argumentValue = (IComparable) argumentValues[freeArgumentIndex];
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

            object[] leftArgumentValues = (object[])argumentValues.Clone();
            leftArgumentValues[freeArgumentIndex] = leftArgumentValue;
            var leftComponentValue = EvaluateArguments<T>(leftArgumentValues);

            object[] rightArgumentValues = (object[])argumentValues.Clone();
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
            object[] newArgumentValues = (object[]) argumentValues.Clone();
            if (Arguments[freeArgumentIndex].ExtrapolationType == ExtrapolationType.Constant)
            {
                if (leftArgumentValue == null)
                {
                    newArgumentValues[freeArgumentIndex] = rightArgumentValue;
                }
                else
                {
                    newArgumentValues[freeArgumentIndex] = leftArgumentValue;
                }
                return EvaluateArguments<T>(newArgumentValues);
            }
            if (Arguments[freeArgumentIndex].ExtrapolationType == ExtrapolationType.Linear)
            {
                var xValue = argumentValues[freeArgumentIndex];
                var freeArgumentValues = Arguments[freeArgumentIndex].Values;
                if (leftArgumentValue == null)
                {
                    //determine 1st point that does have a value for the this argument
                    var x1 = freeArgumentValues[0];
                    newArgumentValues[freeArgumentIndex] = x1;
                    var y1 = EvaluateArguments<T>(newArgumentValues);

                    var x2 = freeArgumentValues[1];
                    newArgumentValues[freeArgumentIndex] = x2;
                    var y2 = EvaluateArguments<T>(newArgumentValues);
                    //(dY / dX)
                    double gradient = GetGradient(y1, y2, x1, x2);
                    double value = Convert.ToDouble(y1) -
                                   gradient*(SubstractAsDouble(x1, xValue));
                    return (T) Convert.ChangeType(value, typeof (T));
                }
                else
                {
                    var x2 = freeArgumentValues[freeArgumentValues.Count - 1];
                    newArgumentValues[freeArgumentIndex] = x2;
                    var y2 = EvaluateArguments<T>(newArgumentValues);

                    var x1 = freeArgumentValues[freeArgumentValues.Count - 2];
                    newArgumentValues[freeArgumentIndex] = x1;
                    var y1 = EvaluateArguments<T>(newArgumentValues);
                    //(dY / dX)
                    double gradient = GetGradient(y1, y2, x1, x2);
                    double value = Convert.ToDouble(y2) +
                                   gradient*(SubstractAsDouble(xValue, x2));
                    return (T) Convert.ChangeType(value, typeof (T));
                }
            }
            if (Arguments[freeArgumentIndex].ExtrapolationType == ExtrapolationType.Periodic)
            {
                //determine period for doubles..
                if (argumentValues[freeArgumentIndex] is double)
                {
                    var argMaxValue = (double) Arguments[freeArgumentIndex].Values.MaxValue;
                    var argMinValue = (double) Arguments[freeArgumentIndex].Values.MinValue;
                    double period = argMaxValue - argMinValue;
                    //determine how many times the period fits the argument value
                    // 0 1 2 --> Period == 2
                    // for 5 period = 2
                    var argumentValue = (double) argumentValues[freeArgumentIndex];
                    int n = Convert.ToInt32(Math.Floor((argumentValue-argMinValue)/period));

                    // value in period is determined
                    var valueInPeriod = argumentValue - n*period;
                    newArgumentValues[freeArgumentIndex] = valueInPeriod;
                    return EvaluateArguments<T>(newArgumentValues);

                }
                if (argumentValues[freeArgumentIndex] is DateTime)
                {
                    var argMaxValue = (DateTime)Arguments[freeArgumentIndex].Values.MaxValue;
                    var argMinValue = (DateTime)Arguments[freeArgumentIndex].Values.MinValue;
                    var period = argMaxValue.Subtract(argMinValue);

                    var argumentValue = (DateTime)argumentValues[freeArgumentIndex];
                    //convert to tick to find out how many periods fit
                    double ratio = (double)argumentValue.Subtract(argMinValue).Ticks / period.Ticks;

                    //substract whole periods
                    int n = Convert.ToInt32(Math.Floor(ratio));
                    var valueInPeriod = argumentValue.Subtract(new TimeSpan(n * period.Ticks));
                    //update arguments and continue evaluation
                    newArgumentValues[freeArgumentIndex] = valueInPeriod;
                    return EvaluateArguments<T>(newArgumentValues);
                }
                throw new NotImplementedException(string.Format("Periodic extrapolation is not support for arguments of type {0}", Arguments[freeArgumentIndex].ValueType));
            }

            throw new ArgumentOutOfRangeException("ExtrapolationType set to none.");
        }

        private double GetGradient(object Y1, object Y2, object X1, object X2)
        {
            return SubstractAsDouble(Y2 ,Y1)/SubstractAsDouble(X2 , X1);
        }
        private double AddAsDouble(object x2, object x1)
        {
            return Convert.ToDouble(x2) + Convert.ToDouble(x1);
        }

        private double SubstractAsDouble(object x2, object x1)
        {
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
        
        
        public virtual void SetValues(IEnumerable values, params IVariableFilter[] filters)
        {
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
        private void NotifyObserversOnFunctionValuesChanged()
        {
            Dummy = !Dummy;
        }

        public virtual void Clear()
        {
            RemoveValues();
        }

        //TODO : move to variable?..it is now non-public because division among components is not done here
        //now. Maybe refactor if needed. Move to varaible?
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
                        AddVariableValuesFromFilters(variableToSet, filters);
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
                variableToSet = filters.FirstOrDefault(f => f is ComponentFilter).Variable;
            }
            return variableToSet;
        }

        private void AddVariableValuesFromFilters(IFunction function, IVariableFilter[] filters)
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
                variableValueFilter.Variable.AddValues(variableValueFilter.Values);
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
            get { return Arguments.Count == 0; }
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
                    return storeClone.Functions[functionIndex];
                }
            }

            var clone = GetType() == typeof(Function) ? new Function() : CreateInstance();
            
            if (Filters.Count > 0)
            {
                throw new NotImplementedException("Clone filters property (redirect to new cloned variables)");
            }

            clone.Name = Name;

            clone.Arguments = new EventedList<IVariable>();

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

        private void SubscribeEventsForArguments()
        {
            if (Arguments != null)
            {
                Arguments.CollectionChanged += ArgumentsCollectionChanged;
                ((INotifyPropertyChanged) Arguments).PropertyChanged += OnArgumentPropertyChanged;
            }
        }

        private void SubscribeEventsForComponents()
        {
            if (Components != null)
            {
                Components.CollectionChanged += Components_CollectionChanged;
                if (!(this is IVariable))
                {
                    ((INotifyPropertyChanged)Components).PropertyChanged += OnComponentPropertyChanged;
                }
            }
        }

        private void OnComponentPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!NotifyPropertyChangeImplementation.BubblingEnabled)
            {
                return;
            }

            if (sender != this)
            {
                var variable = sender as IVariable;
                if (variable != null &&  !variable.IsIndependent)
                {
                    _propertyChangeImplementation.FirePropertyChanged(sender, e);
                }
            }
        }

        private void OnArgumentPropertyChanged(object sender, PropertyChangedEventArgs eventArgs)
        {
            if (!NotifyPropertyChangeImplementation.BubblingEnabled)
            {
                return;
            }

            if (sender != this)
            {
                _propertyChangeImplementation.FirePropertyChanged(sender, eventArgs);
            }
        }

        private void UnSubScribeEventsForArguments()
        {
            if (Arguments != null)
            {
                Arguments.CollectionChanged -= ArgumentsCollectionChanged;
                ((INotifyPropertyChanged)Arguments).PropertyChanged -= OnArgumentPropertyChanged;
            }
        }

        private void UnSubscribeEventsForComponents()
        {
            if (Components != null)
            {
                Components.CollectionChanged -= Components_CollectionChanged;
                ((INotifyPropertyChanged)Components).PropertyChanged -= OnArgumentPropertyChanged;
            }
        }


        public virtual object Clone(IFunctionStore targetStore)
        {
            throw new NotImplementedException();
        }

    }
}