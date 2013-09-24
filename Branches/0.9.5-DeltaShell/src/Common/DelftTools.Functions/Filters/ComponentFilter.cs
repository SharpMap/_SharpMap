using System;
using DelftTools.Utils.Data;

namespace DelftTools.Functions.Filters
{
    public class ComponentFilter : Unique<long>, IComponentFilter
    {
        private IVariable variable;
        
        public ComponentFilter(IVariable variable)
        {
            this.variable = variable;
        }

        public IVariable Variable
        {
            get { return variable; }
            set { variable = value; }
        }

        public object Clone()
        {
            return new ComponentFilter(variable);
        }

        public IVariableFilter Intersect(IVariableFilter filter)
        {
            if (filter != null && filter.Variable != variable)
            {
                throw new ArgumentOutOfRangeException("Filters are incompatible");
            }

            return new ComponentFilter(variable);
        }
    }
}