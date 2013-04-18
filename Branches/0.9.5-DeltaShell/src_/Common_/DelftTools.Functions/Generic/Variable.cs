using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;
using DelftTools.Functions.Filters;
using DelftTools.Units;
using DelftTools.Utils.Aop.NotifyPropertyChanged;
using DelftTools.Utils.Collections;
using DelftTools.Utils.Collections.Generic;
using DelftTools.Utils.IO;
using DelftTools.Utils.Reflection;
using log4net;
using log4net.Core;

namespace DelftTools.Functions.Generic
{
    [Serializable]
    [NotifyPropertyChanged]
    public class Variable<T> : Function, IVariable<T> where T : IComparable
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Variable<T>));
        private new const string DefaultName = "variable";

        [NoBubbling] private IList<T> noDataValues;
        //[NoBubbling] private IEventedList<IVariable> components;
        [NoBubbling] private object defaultValue;
        [NoBubbling] private object defaultStep;

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
            Unit = unit;


            DefaultStep = GetDefaultStep();

            noDataValues = new List<T>();

            FillDefaultNoDataValue();

            Attributes = new Dictionary<string, string>();

            //variable is its own component
            Components = new EventedList<IVariable>() {this};

            DefaultValue = default(T);

            FixedSize = fixedSize;

            AutoSort = true;
            ChecksIfValuesAreUnique = true;
            ExtrapolationType = ApproximationType.None;
            //default interpolation types
            InterpolationType = ApproximationType.Linear;
        }

        private static object GetDefaultStep()
        {
            if (typeof (T) == typeof (int) || typeof (T) == typeof (long) || typeof (T) == typeof (double)
                || typeof (T) == typeof (float) || typeof (T) == typeof (byte) || typeof (T) == typeof (short) ||
                typeof (T) == typeof (uint)
                || typeof (T) == typeof (ulong) || typeof (T) == typeof (ulong))
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
            lock (Store)
            {
                Store.AddIndependendVariableValues(this, values);
            }
        }

        public virtual IMultiDimensionalArray CreateStorageArray()
        {
            return new MultiDimensionalArray<T> {DefaultValue = DefaultValue, Owner = this};
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
            array.Owner = this;

            return array;
        }

        //TODO: sort on the set == true
        [NoNotifyPropertyChanged]
        public virtual bool AutoSort { get; set; }

        [NoNotifyPropertyChanged]
        public virtual bool GenerateUniqueValueForDefaultValue { get; set; }

        [NoNotifyPropertyChanged]
        public virtual bool ChecksIfValuesAreUnique { get; set; }

        public virtual ApproximationType InterpolationType { get; set; }

        private ApproximationType extrapolationType;

        public virtual ApproximationType ExtrapolationType
        {
            get { return extrapolationType; }
            set { extrapolationType = value; }
        }

        public virtual T MinValue
        {
            get
            {
                if (Parent != null)
                {
                    return (T) ((IVariable) Parent).MinValue;
                }
                lock (Store)
                {
                    return Store.GetMinValue<T>(this);
                }
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

                lock (Store)
                {
                    return Store.GetMaxValue<T>(this);
                }
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

                lock (Store)
                {
                    return Store.GetVariableValues<T>(this);
                }
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
            Type t = (this as IVariable).ValueType;
            var genericValues = TypeUtils.CallStaticGenericMethod(typeof (Enumerable), "Cast", t, values);
            //values.Cast<>()
            //var genericValue = TypeUtils.CallGenericMethod(typeof (Enumerable), "Cast", t, values, null);

            TypeUtils.CallGenericMethod(GetType(), "AddValues", t, this,
                                        new[] {genericValues});
        }

        IList<T> IVariable<T>.NoDataValues
        {
            get { return noDataValues; }
            set { noDataValues = value; }
        }

        IList IVariable.NoDataValues
        {
            get { return (IList) noDataValues; }
            set
            {
                noDataValues.Clear();
                foreach (object o in value)
                {
                    noDataValues.Add((T) Convert.ChangeType(o, ValueType));
                }
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
                Values.DefaultValue = value;
            }
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
            return (IVariable<T>) base.Filter(filters);
        }

        IVariable IVariable.Filter(params IVariableFilter[] filters)
        {
            return Filter(filters);
        }

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

        [NoNotifyPropertyChanged]
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
            //var clone = (Variable<T>)base.Clone(false);

            clone.ExtrapolationType = ExtrapolationType;
            clone.InterpolationType = InterpolationType;
            clone.FixedSize = FixedSize;
            clone.NoDataValues = new List<T>(noDataValues);
            clone.DefaultStep = DefaultStep;
            clone.DefaultValue = DefaultValue == null ? default(T) : (T)DefaultValue;
            clone.Unit = Unit != null ? (IUnit)Unit.Clone() : null;
            clone.Attributes = new Dictionary<string, string>(Attributes); // move to function?
            // already in base clone.Name = Name;

            //clone.noDataValues = new List<T>(noDataValues);
            //clone.defaultStep = DefaultStep;
            //clone.defaultValue = DefaultValue == null ? default(T) : (T)DefaultValue;
            //clone.unit = Unit != null ? (IUnit)Unit.Clone() : null;

            return clone;
        }

        #endregion

        //TODO : split out some logic here. We do a unique values administration AND determine insertion index. Split.
        protected override void OnFunctionValuesChanging(FunctionValuesChangedEventArgs e)
        {
            if (!IsIndependent)
            {
                return;
            }

            if (ChecksIfValuesAreUnique)
            {
                var values = (IList) Values;

                // check if we're replacing the same value
                if (e.Action == NotifyCollectionChangedAction.Replace && (e.Item.Equals(values[e.Index])))
                {
                    return;
                }
            }

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Add:
                    //objects need to be sorted as wel. Look at networklocations in a coverage.

                    IList<T> values = null;

                    if (ChecksIfValuesAreUnique || AutoSort)
                        // performance optimization, get Values only when it is required
                    {
                        values = Values;
                    }

                    if (ChecksIfValuesAreUnique)
                    {
                        UpdateItemValue(e, values);
                    }

                    //find out where to insert.
                    if (AutoSort)
                    {
                        UpdateInsertionIndex(e, values);
                    }


                    if (ChecksIfValuesAreUnique)
                    {
                        if (uniqueValues.Contains((T) e.Item))
                        {
                            var message =
                                string.Format(
                                    "Values added to independent variable must be unique, adding {0} at index {1}",
                                    e.Item,
                                    e.Index);
                            throw new InvalidOperationException(message);
                        }
                    }

                    //TODO : keep a good sort order!!. Nu 5 uur en tijd om te gaan.


                    break;

                case NotifyCollectionChangedAction.Remove:
                    break;
                default:
                    break;
            }
        }

        private void UpdateItemValue(FunctionValuesChangedEventArgs e, IList<T> values)
        {
            if (!GenerateUniqueValueForDefaultValue)
            {
                return;
            }
            if ((e.Item == null && DefaultValue == null) || (e.Item.Equals(DefaultValue) && e.Index > 0))
                // add a new value using current DefaultStep (generate series)
            {
                object previousValue = default(T);
                if (values.Count != 0)
                {
                    previousValue = values[e.Index - 1];
                }

                e.Item = GetNextValue(previousValue);
            }
        }

        private void UpdateInsertionIndex(FunctionValuesChangedEventArgs e, IList<T> values)
        {
            if ((typeof (T) == typeof (string)))
            {
                return; // doesn't matter, it is always unique + we don't care about objects
            }

            var oldIndex = e.Index;

            e.Index = MultiDimensionalArrayHelper.GetInsertionIndex((T) e.Item, values);

            if (e.Action == NotifyCollectionChangedAction.Replace)
            {
                if (e.Index > oldIndex) // !@#@#??????
                {
                    e.Index--;
                }
            }
        }

        // for performance reasons we keep copy of values of independent variables in a hash set

        [NoBubbling] private HashSet<T> uniqueValues = new HashSet<T>();

        protected override void OnFunctionValuesChanged(FunctionValuesChangedEventArgs e)
        {
            if (!IsIndependent)
            {
                return;
            }

            if (ChecksIfValuesAreUnique)
            {
                switch (e.Action)
                {
                        // TODO incompatible with new events.
                    case NotifyCollectionChangedAction.Add:
                        uniqueValues.Add((T) e.Item);
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        uniqueValues.Remove((T) e.Item);
                        break;
                    case NotifyCollectionChangedAction.Replace:
                        // remove previous value
                        uniqueValues.Remove((T) e.Item);

                        // add a new value
                        var values = (IList) Values;
                        uniqueValues.Add((T) values[e.Index]);
                        break;
                }
            }
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
            return null;
        }

        //TODO: move out refactor. get bindinglist to not commit crap to our function.
        public virtual NextValueGenerator<T> NextValueGenerator { get; set; }


        private void FillDefaultNoDataValue()
        {
            if (typeof (T) == typeof (double))
            {
                // noDataValues.Add((T) (object) double.NaN);
            }
            if (typeof (T) == typeof (string))
            {
                // noDataValues.Add((T) (object) string.Empty);
            }
            if (typeof (T) == typeof (int))
            {
                // noDataValues.Add((T) (object) int.MinValue);
            }
            if (typeof (T) == typeof (float))
            {
                // noDataValues.Add((T) (object) float.NaN);
            }
            if (typeof (T) == typeof (long))
            {
                // noDataValues.Add((T) (object) long.MinValue);
            }
            if (typeof (T) == typeof (short))
            {
                // noDataValues.Add((T) (object) short.MinValue);
            }
        }

        public override void Attach()
        {
            base.Attach();
            lock (Store)
            {
                Store.UpdateVariableSize(this);
            }
        }
        
        private void Variable_Components_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
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

        public virtual string DisplayName
        {
            get { return string.Format("{0} [{1}]", Name, ((Unit != null) ? Unit.Symbol : "-")); }
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
                SetValues(values.Cast<T>(), filters);
            }
        }

        protected override IFunction CreateInstance()
        {
            return new Variable<T>();
        }
    }
}