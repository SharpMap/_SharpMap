using System.Collections;

namespace DelftTools.Functions.Filters
{
    public interface IVariableValueFilter : IVariableFilter
    {
        IList Values { get; set; }
    }
}