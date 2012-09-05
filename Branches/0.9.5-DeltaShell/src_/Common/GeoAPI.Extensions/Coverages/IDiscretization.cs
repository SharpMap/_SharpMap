namespace GeoAPI.Extensions.Coverages
{
    public interface IDiscretization : INetworkCoverage
    {
        void ToggleFixedPoint(INetworkLocation networkLocation);
        bool IsFixedPoint(INetworkLocation location);
    }
}