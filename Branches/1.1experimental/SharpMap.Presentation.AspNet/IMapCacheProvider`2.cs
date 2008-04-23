using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace SharpMap.Presentation.AspNet
{
    public interface IMapCacheProvider<TMapRequestConfig, TOutput>
        where TMapRequestConfig : IMapRequestConfig
    {
        bool ExistsInCache(TMapRequestConfig config);
        Stream GetStream(TMapRequestConfig config);
        void SaveToCache(TMapRequestConfig config, Stream data);
        void RemoveFromCache(TMapRequestConfig config);

        TOutput RetrieveFromCache(TMapRequestConfig config);
        void SaveToCache(TMapRequestConfig config, TOutput data);
    }
}
