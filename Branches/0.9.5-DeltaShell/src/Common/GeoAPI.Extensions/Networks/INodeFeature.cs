namespace GeoAPI.Extensions.Networks
{
    public interface INodeFeature : INetworkFeature
    {
        INode Node { get; set; }
    }
}