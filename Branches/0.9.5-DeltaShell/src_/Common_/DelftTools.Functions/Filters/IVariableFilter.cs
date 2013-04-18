using System;
using DelftTools.Utils.Data;

namespace DelftTools.Functions.Filters
{
    public interface IVariableFilter: ICloneable, IUnique<long>
    {
        IVariable Variable { get; set; }
        
        IVariableFilter Intersect(IVariableFilter filter);
    }
}