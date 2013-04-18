using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DelftTools.Functions.Filters;
using DelftTools.Functions.Generic;
using DelftTools.Units;

namespace DelftTools.Functions.Conversion
{
    /// <summary>
    /// Convert a certain variable within a variable (for example x(t) convert t-t')
    /// Here x is variable t is soucevariable t' is target variable
    /// </summary>
    /// <typeparam name="TVariable">Type of resulting variable</typeparam>
    /// <typeparam name="TTarget">Type of converted variable</typeparam>
    /// <typeparam name="TSource">Type of source variable</typeparam>
    public class ConvertedVariable<TVariable, TTarget, TSource> : ConvertedFunction<TTarget, TSource>, IVariable<TVariable> 
        where TTarget : IComparable where TSource : IComparable where TVariable : IComparable
    {
        public ConvertedVariable(IVariable parent,IVariable<TSource> variableToConvert, Func<TTarget, TSource> toSource, Func<TSource, TTarget> toTarget)
            :base(parent,variableToConvert,toSource,toTarget)
        {
            if (variableToConvert.Arguments.Count > 0)
                throw new NotImplementedException("Conversion of non-arguments not supported yet");
            //convertedVariable = this;
        }
        public void SetValues<T>(IEnumerable<T> values, params IVariableFilter[] filters)
        {
            //convert values to parent values
            IEnumerable<TTarget> targetValues = (IEnumerable<TTarget>)values;
 
            Parent.SetValues(targetValues.Select(s=>toSource(s)),ConvertFilters(filters));
        }

        
        public new IVariable Parent
        {
            get
            {
                return (IVariable) base.Parent;
            }
            set
            {
                base.Parent = value;
            }
        }

        public IMultiDimensionalArray<TVariable> GetValues(params IVariableFilter[] filters)
        {
            return GetValues<TVariable>();   
        }

        IVariable<TVariable> IVariable<TVariable>.Clone()
        {
            throw new NotImplementedException();
        }

        //TODO: get this addvalues mess out.
        public void AddValues<T>(IEnumerable<T> values)
        {
            throw new NotImplementedException();
        }

        public bool AutoSort
        {
            get { return Parent.AutoSort; }
            set { Parent.AutoSort = value; }
        }

        public bool GenerateUniqueValueForDefaultValue { get; set; }

        public bool ChecksIfValuesAreUnique { get; set; }

        public ApproximationType InterpolationType { get; set; }
        public ApproximationType ExtrapolationType { get; set; }

        public NextValueGenerator<TVariable> NextValueGenerator
        {
            get; set;
        }

        object IVariable.MaxValue
        {
            get { return MaxValue; }
        }

        public IMultiDimensionalArray<TVariable> AllValues
        {
            get { return Values; }
        }

        object IVariable.MinValue
        {
            get { return MinValue; }
        }

        public TVariable MinValue
        {
            get
            {
                if ((variableToConvert == Parent) && typeof(TVariable) == typeof(TTarget))
                {
                    //no nice cast here :(
                    return (TVariable)(object)toTarget((TSource)Parent.MinValue);
                }
                return (TVariable)Parent.MinValue;
            }
        }

        public TVariable MaxValue
        {
            get
            {
                if ((variableToConvert == Parent) && typeof(TVariable) == typeof(TTarget))
                {
                    //no nice cast here :(
                    return (TVariable)(object)toTarget((TSource)Parent.MaxValue);
                }
                return (TVariable)Parent.MaxValue;
            }
        }
        

        public void AddValues<T>(IEnumerable<TVariable> values)
        {
            IEnumerable<TTarget> targetValues = (IEnumerable<TTarget>)values;

            Parent.AddValues(targetValues.Select(s => toSource(s)));
        }

        public void AddValues(IEnumerable values)
        {
            //TODO redirect parent?
            IEnumerable<TTarget> targetValues = (IEnumerable<TTarget>)values;

            Parent.AddValues(targetValues.Select(s => toSource(s)));
        }

        public IMultiDimensionalArray CreateStorageArray()
        {
            return new MultiDimensionalArray<TTarget>();
        }

        public IMultiDimensionalArray CreateStorageArray(IMultiDimensionalArray values)
        {
            return new MultiDimensionalArray<TTarget>(values.Cast<TTarget>().ToList(), values.Shape);
        }

        /*
                public TVariable MinValue
                {
                    get { throw new NotImplementedException(); }
                }

                public TVariable MaxValue
                {
                    get { throw new NotImplementedException(); }
                }

                object IVariable.MaxValue
                {
                    get { return MaxValue; }
                }

                object IVariable.MinValue
                {
                    get { return MinValue; }
                }
        */

        public override IMultiDimensionalArray<T> GetValues<T>(params IVariableFilter[] filters)
        {
            return (IMultiDimensionalArray<T>) base.GetValues(filters); 
        }
        private IVariableFilter[] ConvertFilters(IEnumerable<IVariableFilter> filters)
        {
            //rewrite variable value filter to the domain of the source.
            IList<IVariableFilter> filterList = new List<IVariableFilter>();
            foreach (var filter in filters)
            {
                //TODO: rewrite to IEnumerable etc
                if (filter is IVariableValueFilter && filter.Variable == convertedVariable)
                {
                    var variableValueFilter = filter as IVariableValueFilter;
                    IList values = new List<TSource>();
                    foreach (TTarget obj in variableValueFilter.Values)
                    {
                        values.Add(toSource(obj));
                    }
                    filterList.Add(variableToConvert.CreateValuesFilter(values));
                }
                else
                {
                    filterList.Add(filter);
                }
            }
            return filterList.ToArray();
        }

        private IUnit unit;
        public virtual IUnit Unit
        {
            get
            {
                return Parent != null ? Parent.Unit : unit;
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

        public Type ValueType
        {
            get
            {
                if (variableToConvert == Parent)
                    return typeof(TTarget);
                return Parent.ValueType;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public TVariable DefaultValue
        {
            get
            {
                if ((variableToConvert == Parent) && typeof(TVariable) == typeof(TTarget))
                {
                    //no nice cast here :(
                    return (TVariable)(object)toTarget((TSource) Parent.DefaultValue);
                }
                return (TVariable) Parent.DefaultValue;
            }
            set { throw new NotImplementedException(); }
        }

        public IList<TVariable> NoDataValues
        {
            get;
            set;
        }


        public IMultiDimensionalArray<TVariable> Values
        {
            get
            {
                return GetValues<TVariable>();
            }
            set
            {
                SetValues(value);
            }
        }

        object IMeasurable.DefaultValue
        {
            get ; set;
        }

        IMeasurable IMeasurable.Clone()
        {
            return Clone();
        }

        IMultiDimensionalArray IVariable.Values
        {
            get
            {
                return GetValues();
            }
            set
            {
                SetValues((IEnumerable<TVariable>) value);
            }
        }

        public object DefaultStep
        {
            get; set;
        }

        IList IVariable.NoDataValues
        {
            get;
            set;
        }

        public IDictionary<string, string> Attributes
        {
            get;
            set;
        }

        public IVariable Filter(params IVariableFilter[] filters)
        {
            return (IVariable)base.Filter(filters);
        }
        public int FixedSize
        {
            get
            {
                if (Parent != null)
                {
                    return Parent.FixedSize;
                }
                return 0;
            }
            set { Parent.FixedSize = value; }
        }

        public bool IsFixedSize
        {
            get { return Parent.IsFixedSize; }
        }

        public IVariable Clone()
        {
            throw new NotImplementedException();
        }

        #region IMeasurable Members


        public virtual string DisplayName
        {
            get { return string.Format("{0} [{1}]", Name, ((Unit != null) ? Unit.Symbol : "-")); }
        }

        #endregion

        public IVariableValueFilter CreateValueFilter(object value)
        {
            return new VariableValueFilter<TVariable>(this, new[] { (TVariable)value });
        }

        public IVariableValueFilter CreateValuesFilter(IEnumerable values)
        {
            return new VariableValueFilter<TVariable>(this, values.Cast<TVariable>());
        }
    }
}