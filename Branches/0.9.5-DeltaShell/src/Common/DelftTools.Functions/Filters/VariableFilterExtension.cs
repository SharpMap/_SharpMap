using System.Collections.Generic;
using System.Linq;

namespace DelftTools.Functions.Filters
{
    public static class VariableFilterExtension
    {
        public static IVariableFilter Intersect(this IEnumerable<IVariableFilter> filters)
        {
            var filterList = filters.ToList();
            if (filterList.Count == 1)
            {
                return filterList[0];
            }

            IVariableFilter intersectedFilter = null;
            foreach (var filter in filterList)
            {
                intersectedFilter = filter.Intersect(intersectedFilter);
            }
            return intersectedFilter;
        }
    }
}