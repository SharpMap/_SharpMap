using System;
using System.Collections.Generic;
using System.Text;

namespace SharpMap.Presentation.AspNet
{
    public interface ICachingWebMap<TMapRequestConfig, TOutput, TMapCacheProvider>
        : IWebMap<TMapRequestConfig, TOutput>
        where TMapRequestConfig : IMapRequestConfig
        where TMapCacheProvider : IMapCacheProvider<TMapRequestConfig, TOutput>
    {
        TMapCacheProvider CacheProvider { get; }

    }
}
