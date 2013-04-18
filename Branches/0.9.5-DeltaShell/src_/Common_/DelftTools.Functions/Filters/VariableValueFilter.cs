using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace DelftTools.Functions.Filters
{
    public class VariableValueFilter<T> : IVariableValueFilter where T : IComparable
    {
        private IList<T> values;
        private IVariable variable;

        public long Id { get; set; }

        public VariableValueFilter(IVariable variable, T value)
            : this(variable, new[] { value })
        {
        }

        public VariableValueFilter(IVariable variable, IEnumerable<T> values)
        {
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

        public IList<T> Values
        {
            get { return values; }
            set { values = value; }
        }

        public object Clone()
        {
            return new VariableValueFilter<T>(variable, values);
        }
    }
}