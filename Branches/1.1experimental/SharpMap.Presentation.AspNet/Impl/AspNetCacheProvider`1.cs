using System;
using System.IO;
using System.Web;
using System.Web.Caching;
using SharpMap.Presentation.AspNet.IoC;

namespace SharpMap.Presentation.AspNet.Impl
{
    public class AspNetCacheProvider : IMapCacheProvider
    {
        protected Cache Cache
        {
            get
            {
                return HttpRuntime.Cache;
            }
        }

        TimeSpan _cacheTime = TimeSpan.FromMinutes(5);
        public TimeSpan CacheTime
        {
            get { return _cacheTime; }
            set { _cacheTime = value; }
        }

        public bool ExistsInCache(IMapRequestConfig config)
        {
            return Cache[config.CacheKey] != null;
        }

        public System.IO.Stream RetrieveStream(IMapRequestConfig config)
        {
            if (ExistsInCache(config))
            {
                byte[] bytes = (byte[])Cache[config.CacheKey];
                MemoryStream ms = new MemoryStream(bytes);
                ms.Position = 0;
                return ms;
            }
            throw new NullReferenceException("Cache Miss");

        }

        public void SaveToCache(IMapRequestConfig config, Stream data)
        {
            data.Position = 0;
            byte[] b;
            BinaryReader br = new BinaryReader(data);
            b = br.ReadBytes((int)data.Length);
            data.Position = 0;

            Cache.Insert(config.CacheKey, b, null, Cache.NoAbsoluteExpiration, CacheTime);
        }

        public void RemoveFromCache(IMapRequestConfig config)
        {
            if (ExistsInCache(config))
                Cache.Remove(config.CacheKey);
        }

    }

    public class AspNetCacheProvider<TOutput>
        : AspNetCacheProvider, IMapCacheProvider<TOutput>
    {
      
        public AspNetCacheProvider()
            : base()
        {
        }

        public void SaveToCache(IMapRequestConfig config, TOutput data)
        {
            SaveToCache(config, Container.Instance.Resolve<Func<TOutput, Stream>>()(data));
        }


        #region IMapCacheProvider<TOutput> Members

        public TOutput RetrieveObject(IMapRequestConfig config)
        {
            return Container.Instance.Resolve<Func<Stream, TOutput>>()(this.RetrieveStream(config));
        }

        #endregion
    }
}
