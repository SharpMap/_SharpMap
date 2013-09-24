using System.Collections.Generic;
using DelftTools.Functions.Generic;

namespace GeoAPI.Extensions.Feature.Generic
{
    public interface IFeatureVariable<T> : IVariable<T>
    {
        IList<T> Features { get; set; }
    }
}
