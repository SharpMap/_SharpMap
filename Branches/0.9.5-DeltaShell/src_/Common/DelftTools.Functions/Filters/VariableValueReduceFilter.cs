using System;
using DelftTools.Utils.Data;

namespace DelftTools.Functions.Filters
{
    /// <summary>
    /// Used to exclude <see cref="Variable"/> from the results when querying values from <see cref="Function"/>.
    /// Reduces dimensionality of the <see cref="IMultiDimensionalArray"/> returned by the <see cref="IFunction.GetValues"/>.
    /// <seealso cref="IMultiDimensionalArrayView.Reduce"/> 
    /// </summary>
    public class VariableReduceFilter : Unique<long>, IVariableFilter
    {
        private IVariable variable;

        public VariableReduceFilter(IVariable variable)
        {
            this.variable = variable;
        }

        public IVariable Variable
        {
            get { return variable; }
            set { variable = value; }
        }

        public IVariableFilter Intersect(IVariableFilter filter)
        {

            if (filter == null)
            {
                return this;
            }

            if (filter.Variable != variable)
            {
                throw new ArgumentOutOfRangeException("Filters are incompatible");
            }

            if (!(filter is VariableReduceFilter))
            {
                throw new NotImplementedException("Currently only filter of the same type can be intersected");
            } 
            //two reduce filters are equal to one. No need to reduce double
            return this;
        }

        public object Clone()
        {
            throw new NotImplementedException();
        }
    }
}