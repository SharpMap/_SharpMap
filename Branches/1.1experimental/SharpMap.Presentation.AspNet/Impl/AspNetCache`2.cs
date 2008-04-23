using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.IO;
using System.Web.Caching;

namespace SharpMap.Presentation.AspNet.Impl
{
    public class AspNetCache<TMapRequestConfig, TOutput>
        : IMapCacheProvider<TMapRequestConfig, TOutput>
        where TMapRequestConfig : IMapRequestConfig
    {
        HttpContext _context;
        HttpContext Context
        {
            get
            {
                return _context;
            }
        }

        TimeSpan _cacheTime = TimeSpan.FromMinutes(5);
        public TimeSpan CacheTime
        {
            get { return _cacheTime; }
            set { _cacheTime = value; }
        }


        #region IMapCacheProvider<TMapRequestConfig> Members

        public bool ExistsInCache(TMapRequestConfig config)
        {
            return Context.Cache.Get(config.CacheKey) != null;
        }

        public System.IO.Stream GetStream(TMapRequestConfig config)
        {
            if (ExistsInCache(config))
                return new MemoryStream((byte[])Context.Cache.Get(config.CacheKey));

            throw new NullReferenceException("Cache Miss");

        }

        public void SaveToCache(TMapRequestConfig config, System.IO.Stream data)
        {
            data.Position = 0;
            byte[] b = new byte[data.Length];
            data.Write(b, 0, (int)data.Length);
            Context.Cache.Insert(config.CacheKey, b, null, Cache.NoAbsoluteExpiration, CacheTime);
        }

        #endregion

        #region IMapCacheProvider<TMapRequestConfig> Members


        public void RemoveFromCache(TMapRequestConfig config)
        {
            if (ExistsInCache(config))
                Context.Cache.Remove(config.CacheKey);
        }

        #endregion

        public AspNetCache(HttpContext context)
        {
            this._context = context;
        }

        #region IMapCacheProvider<TMapRequestConfig,TOutput> Members

        public TOutput RetrieveFromCache(TMapRequestConfig config)
        {
            throw new NotImplementedException();
        }

        public void SaveToCache(TMapRequestConfig config, TOutput data)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
