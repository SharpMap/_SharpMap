using System;
using DelftTools.Utils.Data;

namespace DelftTools.Functions.Filters
{
    public class VariableIndexRangeFilter: Unique<long>, IVariableFilter
    {
        private IVariable variable;
        
        private int minIndex;
        private int maxIndex;
        
        public VariableIndexRangeFilter(IVariable variable, int minIndex, int maxIndex)
        {
            this.variable = variable;
            this.minIndex = minIndex;
            this.maxIndex = maxIndex;
        }

        public VariableIndexRangeFilter(IVariable variable, int index)
        {
            this.variable = variable;
            minIndex = index;
            maxIndex = index;
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
                return (IVariableFilter)Clone();
            }

            if (filter.Variable != variable)
            {
                throw new ArgumentOutOfRangeException("filter", "Filters are incompatible");
            }

            if (!(filter is VariableIndexRangeFilter))
            {
                throw new NotImplementedException("Currently only filter of the same type can be intersected");
            }

            VariableIndexRangeFilter f = (VariableIndexRangeFilter)filter;
            //Create that a range that both filters cover. Could result in minIndex > maxIndex
            return new VariableIndexRangeFilter(variable, Math.Max(f.minIndex, minIndex), Math.Min(f.maxIndex, maxIndex));
            
        }

        public int MinIndex
        {
            get { return minIndex; }
            set { minIndex = value; }
        }

        public int MaxIndex
        {
            get { return maxIndex; }
            set { maxIndex = value; }
        }

        #region ICloneable Members

        public object Clone()
        {
            return new VariableIndexRangeFilter(variable,minIndex,maxIndex);
        }

        #endregion
    }
}