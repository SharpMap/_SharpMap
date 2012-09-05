using System.Collections;
using DelftTools.Functions;

namespace GeoAPI.Extensions.Feature
{
    public interface IFeatureVariable : IVariable
    {
        IList Features { get; set; }
    }
}