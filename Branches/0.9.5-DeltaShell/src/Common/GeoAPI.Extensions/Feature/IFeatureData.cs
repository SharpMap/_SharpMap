using DelftTools.Utils;
using DelftTools.Utils.Data;

namespace GeoAPI.Extensions.Feature
{
    public interface IFeatureData : INameable, IUnique<long>
    {
        IFeature Feature { get; set; }
        object Data { get; set; }
    }

}