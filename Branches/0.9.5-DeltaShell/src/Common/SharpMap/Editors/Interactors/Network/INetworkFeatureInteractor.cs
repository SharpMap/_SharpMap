using GeoAPI.Extensions.Networks;

namespace SharpMap.Editors.Interactors.Network
{
    public interface INetworkFeatureInteractor
    {
        INetwork Network { get; set; }
    }
}