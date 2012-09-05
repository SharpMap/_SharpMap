using System.Collections.Generic;

namespace DelftTools.Functions.Filters
{
    public static class VariableFilterExtension
    {
        public static IVariableFilter Intersect(this IEnumerable<IVariableFilter> filters)
        {
            IVariableFilter intersectedFilter = null;
            foreach (var filter in filters)
            {
                intersectedFilter = filter.Intersect(intersectedFilter);
            }

            return intersectedFilter;
        }
    }
}