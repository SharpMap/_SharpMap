using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using DelftTools.Functions.Filters;
using DelftTools.Units;
using DelftTools.Utils.Aop;
using DelftTools.Utils.Collections;
using DelftTools.Utils.Collections.Generic;
using log4net;

namespace DelftTools.Functions.Generic
{
    [Entity(FireOnCollectionChange = false)]
    public class Variable<T> : Function, IVariable<T>
    {
        private bool isAutoSorted = true;

        private static readonly ILog log = LogManager.GetLogger(typeof(Variable<T>));
        private new const string DefaultName = "variable";

        [NoNotifyPropertyChange]
        private IList<T> noDataValues;
        
        //[NoNotifyPropertyChangeAttribute] private IEventedList<IVariable> components;
        [NoNotifyPropertyChange]
        private object defaultValue;

        [NoNotifyPropertyChange]
        private object defaultStep;

        /// <summary>
        /// Initializes a new instance of the <see cref="Variable&lt;T&gt;"/> class.
        /// </summary>
        public Variable() : this(DefaultName)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Variable&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="name">The function argument name.</param>
        public Variable(string name)
            : this(name, null)
        {
        }

        public Variable(string name, IUnit unit)
            : this(name, unit, -1)
        {
        }

        public Variable(string name, int fixedSize)
            : this(name, null, fixedSize)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Variable&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="name">The function argument name.</param>
        /// <param name="quantity">The physical quantity.</param>
        /// <param name="fixedSize">Size of the variable. Used by netcdf for static dimenions</param>
        public Variable(string name, IUnit unit, int fixedSize) : base(name)
        {
            if (!typeof(IComparable).IsAssignableFrom(ValueType))
            {
                isAutoSorted = false; //for non-IComparable, the default is false
            }

            Unit = unit;

            DefaultStep = GetDefaultStep();
            MinValidValue = GetDefaultMin();
            MaxValidValue = GetDefaultMax();

            noDataValues = new List<T>();

            

            Attributes = new Dictionary<string, string>();

            //variable is its own component
            Components = new EventedList<IVariable>() {this};

            DefaultValue = default(T);

            FixedSize = fixedSize;
            
            //ChecksIfValuesAreUnique = true;
            extrapolationType = ExtrapolationType.None;
            //default interpolation types
            interpolationType = InterpolationType.Linear;
            
            CachedValues = null;

            allowSetExtrapolationType = true;
            allowSetInterpolationType = true;
        }

        [NoNotifyPropertyChange]
        public override IFunctionStore Store
        {
            get { return NHStore; }
            set { NHStore = value; }
        }
        private static object GetDefaultStep()
        {
            if (typeof (T) == typeof (int) || typeof (T) == typeof (long) || typeof (T) == typeof (double)
                || typeof (T) == typeof (float) || typeof (T) == typeof (byte) || typeof (T) == typeof (short) ||
                typeof (T) == typeof (uint)
                || typeof (T) == typeof (ulong))
            {
                return (T) Convert.ChangeType(1, typeof (T));
            }
            if (typeof (T) == typeof (TimeSpan))
            {
                return (T) (object) new TimeSpan(0, 0, 1, 0); // 1 min
            }
            if (typeof (T) == typeof (DateTime))
            {
                return new TimeSpan(0, 0, 1, 0); // 1 min
            }
            if (typeof (T) == typeof (string))
            {
                return (T) (object) string.Empty;
            }

            return default(T);
        }

        private static object GetDefaultMin()
        {
            if (typeof(T) == typeof(int))
            {
                return int.MinValue;
            }
            if (typeof(T) == typeof(long))
            {
                return long.MinValue;
            }
            if (typeof(T) == typeof(double))
            {
                return double.MinValue;
            }
            if (typeof(T) == typeof(float))
            {
                return float.MinValue;
            }
            if (typeof(T) == typeof(byte))
            {
                return byte.MinValue;
            }
            if (typeof(T) == typeof(short))
            {
                return short.MinValue;
            }
            if (typeof(T) == typeof(uint))
            {
                return uint.MinValue;
            }
            if (typeof(T) == typeof(ulong))
            {
                return ulong.MinValue;
            }
            if (typeof(T) == typeof(DateTime))
            {
                return DateTime.MinValue;
            }
            if (typeof(T) == typeof(TimeSpan))
            {
                return TimeSpan.MinValue;
            }
            if (typeof (T) == typeof (bool))
            {
                return false;
            }
            if (IsNullable(typeof(T)))
            {
                return (T)(object)null;
            }
            return default(T);
        }


        public static bool IsNullable(Type type)
        {
            return type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(Nullable<>));
        } 

        private static object GetDefaultMax()
        {
            if (typeof(T) == typeof(int))
            {
                return int.MaxValue;
            }
            if (typeof(T) == typeof(long))
            {
                return long.MaxValue;
            }
            if (typeof(T) == typeof(double))
            {
                return double.MaxValue;
            }
            if (typeof(T) == typeof(float))
            {
                return float.MaxValue;
            }
            if (typeof(T) == typeof(byte))
            {
                return byte.MaxValue;
            }
            if (typeof(T) == typeof(short))
            {
                return short.MaxValue;
            }
            if (typeof(T) == typeof(uint))
            {
                return uint.MaxValue;
            }
            if (typeof(T) == typeof(ulong))
            {
                return ulong.MaxValue;
            }
            if (typeof(T) == typeof(DateTime))
            {
                return DateTime.MaxValue;
            }
            if (typeof(T) == typeof(TimeSpan))
            {
                return TimeSpan.MaxValue;
            }
            if (typeof (T) == typeof (bool))
            {
                return true;
            }
            if (IsNullable(typeof(T)))
            {
                return (T)(object)null;
            }
            return default(T);
        }

        #region IVariable<T> Members

        private IUnit unit;

        public virtual IUnit Unit
        {
            get
            {
                if (Parent != null)
                {
                    return ((IVariable) Parent).Unit;
                }

                return unit;
            }
            set
            {
                if (Parent == null)
                {
                    unit = value;
                }
                else
                {
                    throw new Exception("Can't set unit for a filtered function.");
                }
            }
        }

        public virtual Type ValueType
        {
            get { return typeof (T); }
            set { }
        }

        IMultiDimensionalArray IVariable.Values
        {
            get
            {
                return Values;
            }
            set
            {
                SetValues(value);
            }
        }

        /// <summary>
        /// Returns copy of the argument values.
        /// </summary>
        public virtual IMultiDimensionalArray<T> Values
        {
            get { return GetValues<T>(); }
            set { SetValues(value); }
        }

        T IVariable<T>.DefaultValue
        {
            get { return (T) DefaultValue; }
            set { DefaultValue = value; }
        }

        IMeasurable IMeasurable.Clone()
        {
            return (IMeasurable) Clone();
        }

        //TODO: get this addvalues mess out.
        public virtual void AddValues<T>(IEnumerable<T> values)
        {
            if (!IsIndependent)
                throw new ArgumentException("Adding values is only possible for independend variables.");
            Store.AddIndependendVariableValues(this, values);
        }

        /* This is something the store should do! This is here to avoid Activator.CreateInstance call (speed-up) */
        public virtual IMultiDimensionalArray CreateStorageArray()
        {
            return new MultiDimensionalArray<T> {DefaultValue = DefaultValue};
        }

        public virtual IMultiDimensionalArray CreateStorageArray(IMultiDimensionalArray values)
        {
            IMultiDimensionalArray array;

            if (values is IMultiDimensionalArray<T>)
            {
                array = new MultiDimensionalArray<T>((IMultiDimensionalArray<T>)values, values.Shape);
            }
            else
            {
                array = new MultiDimensionalArray<T>(values.Cast<T>().ToList(), values.Shape);
            }

            array.DefaultValue = DefaultValue;

            return array;
        }


        /// <summary>
        /// Determines whether the values of the variable are automatically sorted.
        /// Cannot be determined by IsIndependent because sometimes the variable is unsorted but still independent. For example
        /// the locations of a network coverage.
        /// </summary>
        public virtual bool IsAutoSorted
        {
            get { return isAutoSorted; }
            set
            {
                if (value && !(typeof(IComparable).IsAssignableFrom(ValueType)))
                {
                    throw new NotSupportedException(
                        String.Format("ValueType {0} must implement IComparable for AutoSorted to work.", ValueType));
                }

                isAutoSorted = value;

                if(Store is MemoryFunctionStore) // hackje
                {
                    (Store as MemoryFunctionStore).SetAutoSortForVariable(this,value);
                }
            }
        }

        private bool generateUniqueValueForDefaultValue;
        [NoNotifyPropertyChange]
        public virtual bool GenerateUniqueValueForDefaultValue
        {
            get
            {
                var parentAsVariable = Parent as IVariable;
                if (parentAsVariable != null)
                {
                    return parentAsVariable.GenerateUniqueValueForDefaultValue;
                }
                return generateUniqueValueForDefaultValue;
            }
            set
            {
                var parentAsVariable = Parent as IVariable;
                if (parentAsVariable != null)
                {
                    parentAsVariable.GenerateUniqueValueForDefaultValue = value;
                    return;
                }
                generateUniqueValueForDefaultValue = value;
            }
        }


        /*[NoNotifyPropertyChange]
        public virtual bool GenerateUniqueValueForDefaultValue { get; set; }*/

        private InterpolationType interpolationType;
        public virtual InterpolationType InterpolationType
        {
            get
            {
                if (Parent != null)
                {
                    return ((IVariable) Parent).InterpolationType;
                }
                return interpolationType;
            }
            set
            {
                if (Parent != null)
                {
                    throw new InvalidOperationException("Can't set InterpolationType for filtered variable; set it on the (unfiltered) parent.");
                }
                interpolationType = value;
            }
        }

        private ExtrapolationType extrapolationType;

        public virtual ExtrapolationType ExtrapolationType
        {
            get
            {
                if (Parent != null)
                {
                    return ((IVariable)Parent).ExtrapolationType;
                }
                return extrapolationType;
            }
            set
            {
                if (Parent != null)
                {
                    throw new InvalidOperationException("Can't set ExtrapolationType for filtered variable; set it on the (unfiltered) parent.");
                }
                extrapolationType = value;
            }
        }

        public virtual T MinValue
        {
            get
            {
                if (Parent != null)
                {
                    return (T) ((IVariable) Parent).MinValue;
                }

                return Store.GetMinValue<T>(this);
            }
        }

        public virtual T MaxValue
        {
            get
            {
                if (Parent != null)
                {
                    return (T) ((IVariable) Parent).MaxValue;
                }

                
                return Store.GetMaxValue<T>(this);
                
            }
        }

        public virtual IMultiDimensionalArray<T> AllValues
        {
            get
            {
                if (Parent != null)
                {
                    return ((IVariable<T>)Parent).AllValues;
                }

                return Store.GetVariableValues<T>(this);
            }
        }

        object IVariable.MaxValue
        {
            get { return MaxValue; }
        }

        object IVariable.MinValue
        {
            get { return MinValue; }
        }


        public virtual void AddValues(IEnumerable values)
        {
            var typedValues = values.Cast<T>();
            AddValues(typedValues);
        }

        IList<T> IVariable<T>.NoDataValues
        {
            get { return NoDataValues; }
            set
            {
                ThrowIfSettingNoDataValuesOnFilteredFunction();

                noDataValues = value;
            }
        }

        IList IVariable.NoDataValues
        {
            get { return (IList)NoDataValues; }
            set
            {
                ThrowIfSettingNoDataValuesOnFilteredFunction();

                noDataValues.Clear();
                foreach (object o in value)
                {
                    noDataValues.Add((T) Convert.ChangeType(o, ValueType));
                }
            }
        }

        private IList<T> NoDataValues
        {
            get
            {
                if (Parent != null && Parent is IVariable<T>)
                {
                    return (Parent as IVariable<T>).NoDataValues.ToList().AsReadOnly();
                }
                return noDataValues;
            }
        }

        // TODO: change to T
        public virtual object NoDataValue
        {
            get
            {
                return NoDataValues.Count != 0 ? (object)NoDataValues[0] : null;
            }
            set
            {
                ThrowIfSettingNoDataValuesOnFilteredFunction();

                noDataValues.Clear();
                if (value != null)
                {
                    noDataValues.Add((T)Convert.ChangeType(value, ValueType));
                }
            }
        }

        private void ThrowIfSettingNoDataValuesOnFilteredFunction()
        {
            if (Parent != null)
            {
                throw new NotSupportedException("Cannot set No Data values on filtered function, please set it on the parent function.");
            }
        }

        public virtual object DefaultValue
        {
            get { return defaultValue; }
            set
            {
                if (value is IConvertible)
                {
                    defaultValue = Convert.ChangeType(value, typeof (T));
                }
                else
                {
                    defaultValue = value;
                }
                
                // Values is expensive; cheaper way
                // set the default value in the underlying array
                var values = Values;
                if (values != null)
                    values.DefaultValue = value;
            }
        }

        [NoNotifyPropertyChange]
        private object minValidValue;
        [NoNotifyPropertyChange]
        private object maxValidValue;

        [NoNotifyPropertyChange]
        public virtual object MinValidValue
        {
            get { return minValidValue; }
            set { minValidValue = value; }
        }

        [NoNotifyPropertyChange]
        public virtual object MaxValidValue
        {
            get { return maxValidValue; }
            set { maxValidValue = value; }
        }

        public virtual object DefaultStep
        {
            get { return defaultStep; }
            set { defaultStep = value; }
        }

        public new virtual IMultiDimensionalArray<T> GetValues(params IVariableFilter[] filters)
        {
            if (Parent != null)
            {
                return Parent.GetValues<T>(filters.Concat(Filters).ToArray());
            }

            var allFilters = filters;

            // performance: avoid Concat
            if (filters.Length != 0 || Filters.Count != 0)
            {
                allFilters = filters.Concat(Filters).ToArray();
            }

            lock (Store)
            {
                return Store.GetVariableValues<T>(this, allFilters);
            }
        }

        public new virtual IVariable<T> Filter(params IVariableFilter[] filters)
        {
            return (IVariable<T>)base.Filter(filters);
        }

        IVariable IVariable.Filter(params IVariableFilter[] filters)
        {
            return Filter(filters);
        }

        [NoNotifyPropertyChange]
        public override IEventedList<IVariable> Components
        {
            get { return base.Components; }
            set
            {
                if (Components != null)
                {
                    Components.CollectionChanged -= Variable_Components_CollectionChanged;
                }
                base.Components = value;
                if (Components != null)
                {
                    Components.CollectionChanged += Variable_Components_CollectionChanged;
                }
            }
        }

        /// <summary>
        /// -1: non-fixed
        /// 
        /// </summary>
        public virtual int FixedSize { get; set; }

        [NoNotifyPropertyChange]
        public virtual bool IsFixedSize
        {
            get { return (FixedSize != -1); }
        }

        IVariable IVariable.Clone()
        {
            return (IVariable) Clone();
        }

        IVariable<T> IVariable<T>.Clone()
        {
            return (IVariable<T>) Clone();
        }

        public override object Clone(bool copyValues)
        {
            return Clone(copyValues, false, false);
        }

        public override object Clone(bool copyValues, bool skipArguments, bool skipComponents)
        {
            if (copyValues)
            {
                return base.Clone(true, skipArguments, skipComponents);
            }

            var clone = (IVariable<T>)base.Clone(false, skipArguments, skipComponents);

            clone.CopyFrom(this);

            return clone;
        }

        public virtual void CopyFrom(object source)
        {
            var variableSource = (IVariable<T>) source;

            ExtrapolationType = variableSource.ExtrapolationType;
            InterpolationType = variableSource.InterpolationType;
            FixedSize = variableSource.FixedSize;
            noDataValues = new List<T>(variableSource.NoDataValues);
            DefaultStep = variableSource.DefaultStep;
            DefaultValue = variableSource.DefaultValue == null ? default(T) : (T)variableSource.DefaultValue;
            MinValidValue = variableSource.MinValidValue == null ? default(T) : (T)variableSource.MinValidValue;
            MaxValidValue = variableSource.MaxValidValue == null ? default(T) : (T)variableSource.MaxValidValue;
            Unit = variableSource.Unit != null ? (IUnit)variableSource.Unit.Clone() : null;
            Attributes = new Dictionary<string, string>(variableSource.Attributes); // move to function?
            CachedValues = null;
            IsAutoSorted = variableSource.IsAutoSorted;
        }

        #endregion

        //TODO : split out some logic here. We do a unique values administration AND determine insertion index. Split.
        protected override void OnFunctionValuesChanging(FunctionValuesChangingEventArgs e)
        {
            if (!IsIndependent)
            {
                return;
            }

            var values = (IList<T>) Values;

            // check if we're replacing the same value
            if (e.Action == NotifyCollectionChangeAction.Replace && e.Items.Count == 1 &&
                (e.Items[0].Equals(values[e.Index])))
            {
                return;
            }

            switch (e.Action)
            {
                case NotifyCollectionChangeAction.Replace:
                case NotifyCollectionChangeAction.Add:
                    //objects need to be sorted as wel. Look at networklocations in a coverage.
                    if (GenerateUniqueValueForDefaultValue)
                    {
                        MakeItemValuesUnique(e, values);    
                    }

                    if (!SkipUniqueValuesCheck)
                    {
                        if (e.Items.Cast<object>().Any(i => values.Contains((T) (i))))
                        {
                            var message =
                                string.Format(
                                    "Values added to independent variable must be unique, adding {0} index {1}",
                                    e.Items.Cast<object>().Aggregate("", (current, item) => current + item + ", "),
                                    e.Index);
                            throw new InvalidOperationException(message);
                        }
                    }
                    break;
                case NotifyCollectionChangeAction.Remove:
                    break;
            }
        }

        private void MakeItemValuesUnique(FunctionValuesChangingEventArgs e, IList<T> values)
        {
            var previousValue = default(T);
            bool previousValueDefined = false;

            if (values.Count != 0 && e.Index > 0)
            {
                previousValue = values[e.Index - 1];
                previousValueDefined = true;
            }

            for (int i = 0; i < e.Items.Count; i++)
            {
                bool currentAndDefaultNull = false;

                var item = e.Items[i];

                if (item == null && DefaultValue == null)
                {
                    currentAndDefaultNull = true;
                }

                if (currentAndDefaultNull || item != null && item.Equals(DefaultValue))
                {
                    if (i > 0)
                    {
                        previousValue = (T) e.Items[i - 1];
                        previousValueDefined = true;
                    }

                    if (previousValueDefined || currentAndDefaultNull)
                    {
                        e.Items[i] = GetNextValue(previousValue);
                        // add a new value using current DefaultStep (generate series)
                    }
                }
            }
        }

        protected override bool ShouldReceiveChangedEventsForFunction(IFunction source)
        {
            if ((source == this) && IsIndependent )
                return true;
            return base.ShouldReceiveChangedEventsForFunction(source);
        }

        private object GetNextValue(object previousValue)
        {
            if (NextValueGenerator != null)
                return NextValueGenerator.GetNextValue();

            if (typeof (T) == typeof (double))
            {
                return (double) previousValue + (double) DefaultStep;
            }
            if (typeof (T) == typeof (int))
            {
                return (int) previousValue + (int) DefaultStep;
            }
            if (typeof (T) == typeof (float))
            {
                return (float) previousValue + (float) DefaultStep;
            }
            if (typeof (T) == typeof (long))
            {
                return (long) previousValue + (long) DefaultStep;
            }
            if (typeof (T) == typeof (short))
            {
                return (short) previousValue + (short) DefaultStep;
            }
            if (typeof (T) == typeof (DateTime))
            {
                return ((DateTime) previousValue).Add((TimeSpan) DefaultStep);
            }
            if (typeof (T) == typeof (TimeSpan))
            {
                return ((TimeSpan) previousValue).Add((TimeSpan) DefaultStep);
            }
            throw new InvalidOperationException(string.Format("Unable to generate next value for variable of type {0}. Add a NextValueGenerator to the variable", typeof(T)));
        }

        //TODO: move out refactor. get bindinglist to not commit crap to our function.
        [NoNotifyPropertyChange]
        public virtual NextValueGenerator<T> NextValueGenerator { get; set; }


        protected override void ArgumentsCollectionChanged(object sender, NotifyCollectionChangingEventArgs e)
        {
            isIndependentDirty = true; // performance optimization

            //if a function 
            if (Arguments.Count > 0)
            {
                IsAutoSorted = false;
            }

            base.ArgumentsCollectionChanged(sender, e);
        }

        private void Variable_Components_CollectionChanged(object sender, NotifyCollectionChangingEventArgs e)
        {
            if (IsIndependent)
            {
                throw new InvalidOperationException("Cannot change components collection of an independent variable");
            }
        }

        public override string ToXml()
        {
            //<variable name=x>
            //  <values>1,2,3</values>
            //</variable>
            XmlDocument doc = new XmlDocument();

            XmlNode variableNode = doc.CreateElement("variable");
            var nameAttribute = doc.CreateAttribute("name");
            nameAttribute.Value = Name;
            variableNode.Attributes.Append(nameAttribute);
            doc.AppendChild(variableNode);

            XmlNode valuesNode = doc.CreateElement("values");
            variableNode.AppendChild(valuesNode);
            valuesNode.AppendChild(doc.CreateTextNode(Values.ToString()));
            return doc.InnerXml;
        }

        #region IMeasurable Members

        /// <summary>
        /// HACK: move it to presentation layer
        /// </summary>
        public virtual string DisplayName
        {
            get
            {
               return string.Format("{0} [{1}]", Name, ((Unit != null) ? Unit.Symbol : "-"));
            }
        }

        #endregion

        public virtual IVariableValueFilter CreateValueFilter(object value)
        {
            return new VariableValueFilter<T>(this, (T)value);
        }

        /// <summary>
        /// Factory method.
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public virtual IVariableValueFilter CreateValuesFilter(IEnumerable values)
        {
            return new VariableValueFilter<T>(this, values.Cast<T>());
        }

        public override void SetValues(IEnumerable values, params IVariableFilter[] filters)
        {
            if (Parent != null)
            {
                base.SetValues(values, filters);
            }
            else
            {
                Type innerType = values.GetInnerType();

                if (innerType != null) //if list is empty (innerType == null), just continue and don't throw exceptions
                    if (!typeof (T).IsAssignableFrom(innerType)) //check if innerType is equal to, or subclass of, T
                        throw new ArgumentException(String.Format("Value of type {0}, but expected type {1} for variable {2}",innerType, typeof (T), Name));

                SetValues(values.Cast<T>(), filters);
            }
        }

        protected override IFunction CreateInstance()
        {
            return new Variable<T>();
        }

        /// <summary>
        /// Performance optimization for MemoryFunctionStore.
        /// </summary>
        [NoNotifyPropertyChange]
        public virtual IMultiDimensionalArray CachedValues { get; set; }

        private bool allowSetInterpolationType;
        public virtual bool AllowSetInterpolationType
        {
            get
            {
                if (Parent != null)
                {
                    return false; //can't set on filtered on, so we're not going to lie
                }
                return allowSetInterpolationType;
            }
            set
            {
                if (Parent != null)
                {
                    throw new InvalidOperationException("Can't set AllowSetInterpolationType for filtered variable; set it on the (unfiltered) parent.");
                }
                allowSetInterpolationType = value;
            }
        }

        private bool allowSetExtrapolationType;
        public virtual bool AllowSetExtrapolationType
        {
            get
            {
                if (Parent != null)
                {
                    return false; //can't set on filtered on, so we're not going to lie
                }
                return allowSetExtrapolationType;
            }
            set
            {
                if (Parent != null)
                {
                    throw new InvalidOperationException("Can't set AllowSetExtrapolationType for filtered variable; set it on the (unfiltered) parent.");
                }
                allowSetExtrapolationType = value;
            }
        }

        public virtual bool SkipUniqueValuesCheck
        {
            get; set;
        }
    }
}