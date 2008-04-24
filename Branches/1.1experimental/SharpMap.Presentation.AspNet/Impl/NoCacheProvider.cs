using System;

namespace SharpMap.Presentation.AspNet.Impl
{
    /// <summary>
    /// A caching provider that does nothing!
    /// use it when you dont want caching
    /// </summary>
    public class NoCacheProvider
        : IMapCacheProvider
    {
        #region IMapCacheProvider Members

        public bool ExistsInCache(IMapRequestConfig config)
        {
            return false;
        }

        public void SaveToCache(IMapRequestConfig config, System.IO.Stream data)
        {
            //do nothing
        }

        public System.IO.Stream RetrieveStream(IMapRequestConfig config)
        {
            ///we should be testing ExistsInCache before calling this method.
            throw new InvalidOperationException();
        }

        public void RemoveFromCache(IMapRequestConfig config)
        {
            //do nothing
        }

        #endregion
    }
}
