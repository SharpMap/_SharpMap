using System;
using DelftTools.Utils;
using GeoAPI.Extensions.Feature;

namespace GeoAPI.Extensions.Networks
{
    public interface INetworkFeature: IFeature, IComparable<INetworkFeature>, IComparable, INameable
    {
        INetwork Network { get; set; }
        string Description { get; set; }
    }
}