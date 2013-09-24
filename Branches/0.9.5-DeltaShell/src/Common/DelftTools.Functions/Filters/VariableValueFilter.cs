using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using DelftTools.Utils.Data;

namespace DelftTools.Functions.Filters
{
    //why not IVariable<T>?
    public class VariableValueFilter<T> : Unique<long>, IVariableValueFilter
    {
        private IList<T> values;
        private IVariable variable;
        
        public VariableValueFilter(IVariable variable, T value)
            : this(variable, new[] { value })
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="variable"></param>
        /// <param name="values">
        ///   Precondition: collection size must be > 0 and all items in collection must be assignable to <paramref name="variable"/>
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   When <paramref name="values"/> is not > 0 or when not all items in <paramref name="values"/> are assignable to <paramref name="variable"/>
        /// </exception>
        public VariableValueFilter(IVariable variable, IEnumerable<T> values)
        {
            if (!values.Any())
                throw new ArgumentOutOfRangeException("values", "Cannot be empty");

            this.values = new List<T>();
            foreach (var value in values)
            {
                if (!variable.ValueType.IsAssignableFrom(value.GetType()))
                {
                    throw new ArgumentOutOfRangeException("values", "Invalid value type");
                }
                this.values.Add(value);
            }

            this.variable = variable;
        }

        public VariableValueFilter()
        {
        }

        
        public virtual IVariable Variable
        {
            get { return variable; }
            set { variable = value; }
        }

        public virtual IVariableFilter Intersect(IVariableFilter filter)
        {
            if(filter == null)
            {
                return (IVariableFilter) Clone();
            }

            if (filter.Variable != variable)
            {
                throw new ArgumentOutOfRangeException("filter", "Filters are incompatible");
            }

            if (!(filter is VariableValueFilter<T>))
            {
                throw new NotImplementedException("Currently only filter of the same type can be intersected");
            }

            var f = (VariableValueFilter<T>) filter;
            IList<T> newValues = new List<T>();
            foreach (var value in values)
            {
                if(f.Values.Contains(value))
                {
                    newValues.Add(value);
                }
            }

            return new VariableValueFilter<T>(variable, newValues);
        }

        IList IVariableValueFilter.Values
        {
            get { return (IList) values; }
            set { values = value.Cast<T>().ToList(); }
        }

        public virtual IList<T> Values
        {
            get { return values; }
            set { values = value; }
        }

        public virtual object Clone()
        {
            return new VariableValueFilter<T>(variable, values);
        }
    }
}