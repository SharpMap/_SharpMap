/*
 *  The attached / following is part of SharpMap.Presentation.AspNet
 *  SharpMap.Presentation.AspNet is free software © 2008 Newgrove Consultants Limited, 
 *  www.newgrove.com; you can redistribute it and/or modify it under the terms 
 *  of the current GNU Lesser General Public License (LGPL) as published by and 
 *  available from the Free Software Foundation, Inc., 
 *  59 Temple Place, Suite 330, Boston, MA 02111-1307 USA: http://fsf.org/    
 *  This program is distributed without any warranty; 
 *  without even the implied warranty of merchantability or fitness for purpose.  
 *  See the GNU Lesser General Public License for the full details. 
 *  
 *  Author: John Diss 2008
 * 
 */
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
            using (Stream s = Container.Instance.Resolve<Func<TOutput, Stream>>()(data))
            {
                SaveToCache(config, s);
            }
        }


        #region IMapCacheProvider<TOutput> Members

        public TOutput RetrieveObject(IMapRequestConfig config)
        {
            return Container.Instance.Resolve<Func<Stream, TOutput>>()(this.RetrieveStream(config));
        }

        #endregion
    }
}
