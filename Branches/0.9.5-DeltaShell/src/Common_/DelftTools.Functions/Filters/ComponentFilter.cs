using System;

namespace DelftTools.Functions.Filters
{
    public class ComponentFilter : IComponentFilter
    {
        private IVariable variable;

        public long Id { get; set; }

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