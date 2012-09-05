namespace GeoAPI.Extensions.Feature.Generic
{
    public interface IFeatureData<TData, TFeature> : IFeatureData where TFeature : IFeature
    {
        new TFeature Feature { get; set; }

        new TData Data { get; set; }
    }
}