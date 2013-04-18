using System;
using DelftTools.Utils.Data;
using ValidationAspects;

namespace DelftTools.Functions.Filters
{
    public class VariableAggregationFilter: Unique<long>, IVariableFilter {
        
        private IVariable variable;
        private readonly int minIndex;
        private readonly int maxIndex;
        private int stepSize;
        
        public VariableAggregationFilter(IVariable variable, [GreaterThan(0)]int stepSize, int minIndex, int maxIndex)
        {
            this.minIndex = minIndex;
            this.maxIndex = maxIndex;
            this.stepSize = stepSize;
            this.variable = variable;
        }

        public int MaxIndex
        {
            get { return maxIndex; }
        }

        public int MinIndex
        {
            get { return minIndex; }
        }

        /// <summary>
        /// Total number of values to read.
        /// </summary>
        public int Count
        {
            get { return (maxIndex - minIndex)/stepSize + 1; }
        }

        public object Clone()
        {
            return new VariableAggregationFilter(variable, stepSize,minIndex,maxIndex);
        }

        public IVariable Variable
        {
            get { return variable; }
            set { variable = value; }
        }

        public int StepSize
        {
            get { return this.stepSize; }
            set { this.stepSize = value; }
        }

        public IVariableFilter Intersect(IVariableFilter filter)
        {
            if (filter == null)
            {
                return (IVariableFilter)Clone();
            }
            throw new NotImplementedException();
        }
        
    }
}