using System.IO;

namespace SharpMap.Presentation.AspNet
{

    public interface IMapCacheProvider
    {
        bool ExistsInCache(IMapRequestConfig config);
        void SaveToCache(IMapRequestConfig config, Stream data);
        Stream RetrieveStream(IMapRequestConfig config);
        void RemoveFromCache(IMapRequestConfig config);

    }

    public interface IMapCacheProvider<TOutput> : IMapCacheProvider
    {
        TOutput RetrieveObject(IMapRequestConfig config);
        void SaveToCache(IMapRequestConfig config, TOutput data);
    }
}
