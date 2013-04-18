namespace GeoAPI.Extensions.Coverages
{
    // TODO: this has to be removed, domain-specific
    public interface IDiscretization : INetworkCoverage
    {
        void ToggleFixedPoint(INetworkLocation networkLocation);
        bool IsFixedPoint(INetworkLocation location);
    }
}