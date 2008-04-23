using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using SharpMap.Renderer;
using System.IO;

namespace SharpMap.Presentation.AspNet.Impl
{
    public abstract class CachingWebMapBase<TMapRequestConfig, TOutput, TMapCacheProvider>
        : WebMapBase<TMapRequestConfig, TOutput>, ICachingWebMap<TMapRequestConfig, TOutput, TMapCacheProvider>
        where TMapRequestConfig : IMapRequestConfig
        where TMapCacheProvider : IMapCacheProvider<TMapRequestConfig, TOutput>
    {

        public CachingWebMapBase(HttpContext context)
            : base(context)
        { }

        TMapCacheProvider _cacheProvider;
        public TMapCacheProvider CacheProvider
        {
            get
            {
                EnsureCacheProvider();
                return _cacheProvider;
            }
        }

        private void EnsureCacheProvider()
        {
            if (_cacheProvider == null)
                _cacheProvider = CreateCacheProvider();
        }

        protected abstract TMapCacheProvider CreateCacheProvider();

        public override Stream Render(out string mimeType)
        {
            mimeType = MapRequestConfig.MimeType;

            if (CacheProvider.ExistsInCache(MapRequestConfig))
                return CacheProvider.GetStream(MapRequestConfig);
            else
            {
                RaiseBeforeInitializeMap();
                InitMap();
                RaiseMapInitialized();

                RaiseBeforeLoadLayers();
                LoadLayers();
                RaiseLayersLoaded();

                RaiseBeforeConfigureMapView();
                ConfigureMapView();
                RaiseMapViewConfigDone();

                RaiseBeforeLoadMapState();
                LoadMapState();
                RaiseMapStateLoaded();

                RaiseBeforeMapRender();

                Stream s = null;
                s = StreamBuilder(Map.Render(MapRenderer));
                try
                {
                    CacheProvider.SaveToCache(MapRequestConfig, s);
                    RaiseMapRenderDone();
                    return s;
                }
                catch
                {
                    if (s != null)
                        s.Close();

                    CacheProvider.RemoveFromCache(MapRequestConfig);
                    throw;
                }
            }

        }
    }
}
