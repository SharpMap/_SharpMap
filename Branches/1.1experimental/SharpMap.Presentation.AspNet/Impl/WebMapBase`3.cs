
/*
 *	This file is part of SharpMap
 *  SharpMap is free software © 2008 Newgrove Consultants Limited, 
 *  http://www.newgrove.com; you can redistribute it and/or modify it under the terms 
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
using SharpMap.Layers;
using SharpMap.Renderer;
using System.Diagnostics;

namespace SharpMap.Presentation.AspNet.Impl
{
    public abstract class WebMapBase
        : IWebMap
    {

        public WebMapBase(HttpContext context)
            : base()
        {
            _context = context;
        }



        IMapCacheProvider _cacheProvider;
        public IMapCacheProvider CacheProvider
        {
            get
            {
                EnsureCacheProvider();
                return _cacheProvider;
            }
            set
            {
                _cacheProvider = value;
            }
        }

        private void EnsureCacheProvider()
        {
            if (_cacheProvider == null)
                _cacheProvider = CreateCacheProvider();
        }

        protected abstract IMapCacheProvider CreateCacheProvider();


        #region IWebMap<TMapRequestConfig,TOutput> Members

        private IMapRenderer _renderer;
        public IMapRenderer MapRenderer
        {
            get
            {
                if (_renderer == null)
                    _renderer = CreateMapRenderer();
                return _renderer;
            }
            set
            {
                _renderer = value;
            }
        }

        #endregion

        #region "Events and Raisers"

        public event EventHandler BeforeCreateMapRequestConfig;
        protected void RaiseCreateMapRequestConfig()
        {
            if (this.BeforeCreateMapRequestConfig != null)
                this.BeforeCreateMapRequestConfig(this, EventArgs.Empty);
        }

        public event EventHandler MapRequestConfigCreated;
        protected void RaiseMapRequestConfigCreated()
        {
            if (this.MapRequestConfigCreated != null)
                this.MapRequestConfigCreated(this, EventArgs.Empty);
        }

        public event EventHandler BeforeInitMap;
        protected void RaiseBeforeInitializeMap()
        {
            if (this.BeforeInitMap != null)
                this.BeforeInitMap(this, EventArgs.Empty);
        }

        public event EventHandler MapInitDone;
        protected void RaiseMapInitialized()
        {
            if (this.MapInitDone != null)
                this.MapInitDone(this, EventArgs.Empty);
        }

        public event EventHandler<LayerLoadedEventArgs> LayerLoaded;
        protected void RaiseLayerLoaded(ILayer layer)
        {
            if (this.LayerLoaded != null)
                this.LayerLoaded(this, new LayerLoadedEventArgs(layer));
        }

        public event EventHandler BeforeLoadLayers;
        protected void RaiseBeforeLoadLayers()
        {
            if (this.BeforeLoadLayers != null)
                this.BeforeLoadLayers(this, EventArgs.Empty);
        }

        public event EventHandler LayersLoaded;
        protected void RaiseLayersLoaded()
        {
            if (this.LayersLoaded != null)
                this.LayersLoaded(this, EventArgs.Empty);
        }

        public event EventHandler BeforeLoadMapState;
        protected void RaiseBeforeLoadMapState()
        {
            if (this.BeforeLoadMapState != null)
                this.BeforeLoadMapState(this, EventArgs.Empty);
        }

        public event EventHandler MapStateLoaded;
        protected void RaiseMapStateLoaded()
        {
            if (this.MapStateLoaded != null)
                this.MapStateLoaded(this, EventArgs.Empty);
        }

        public event EventHandler BeforeMapRender;
        protected void RaiseBeforeMapRender()
        {
            if (this.BeforeMapRender != null)
                this.BeforeMapRender(this, EventArgs.Empty);
        }

        public event EventHandler MapRenderDone;
        protected void RaiseMapRenderDone()
        {
            if (this.MapRenderDone != null)
                this.MapRenderDone(this, EventArgs.Empty);
        }

        #endregion

        #region "Hooks"

        /// <summary>
        /// Create or retrieve a MapView and Map object.
        /// You can add layers here if you wish.
        /// </summary>
        public virtual void InitMap()
        {
            Map = new Map();

        }

        /// <summary>
        /// Create the RenderWrapper here
        /// </summary>
        /// <returns></returns>
        protected abstract IMapRenderer CreateMapRenderer();

        /// <summary>
        /// Create a config factory here
        /// </summary>
        /// <returns></returns>
        protected abstract IMapRequestConfigFactory CreateConfigFactory();


        /// <summary>
        /// Use to add layers to the map. Not necessary if the layers were added when the map was created or if the map was retrieved from a persistance medium.
        /// </summary>
        public abstract void LoadLayers();

        /// <summary>
        /// this method will configure the map view by calling ConfigureMap on the MapRequestConfig
        /// </summary>
        public virtual void ConfigureMap()
        {
            MapRequestConfig.ConfigureMap(this.Map);
        }

        /// <summary>
        /// override this method to configure your renderer.
        /// </summary>
        public virtual void ConfigureRenderer()
        {

        }

        /// <summary>
        /// Use this method to load user state into the map. Or leave empty if not required.
        /// </summary>
        /// <remarks>This was put in for v2 to allow such things as setting selections etc. Perhaps it should be removed from this version?</remarks> 
        public virtual void LoadMapState()
        {

        }

        /// <summary>
        /// use this method to detach any events. It is called during disposal.
        /// </summary>
        public virtual void UnwireEvents()
        {

        }

        #endregion




        #region "properties and Ensure"

        private Map _map;
        /// <summary>
        /// The Map, Assign a value to MapView before assigning a value to Map.
        /// </summary>
        public Map Map
        {
            get
            {
                return _map;
            }
            set
            {
                _map = value;
            }
        }




        private IMapRenderer _renderWrapper;
        private void EnsureRenderWrapper()
        {
            if (_renderWrapper == null)
            {
                _renderWrapper = CreateMapRenderer();
            }
        }


        IMapRequestConfig _config;
        public IMapRequestConfig MapRequestConfig
        {
            get
            {
                EnsureMapConfiguration();
                return _config;
            }
        }

        private void EnsureMapConfiguration()
        {
            if (_config == null)
            {

                RaiseCreateMapRequestConfig();
                _config = ConfigFactory.CreateConfig(Context);
                RaiseMapRequestConfigCreated();
            }
        }

        IMapRequestConfigFactory _configFactory;
        public IMapRequestConfigFactory ConfigFactory
        {
            get
            {
                EnsureConfigFactory();
                return _configFactory;
            }
        }

        private void EnsureConfigFactory()
        {
            if (_configFactory == null)
                _configFactory = CreateConfigFactory();
        }


        #endregion


        HttpContext _context;
        public HttpContext Context
        {
            get
            {
                return _context ?? HttpContext.Current;
            }
            set
            {
                _context = value;
            }
        }

        #region IDisposable Members

        ~WebMapBase()
        {
            Dispose(false);
        }

        private bool disposed;
        public virtual void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                    UnwireEvents();

                disposed = true;
            }
        }


        #endregion

        public Stream Render(out string mimeType)
        {

            if (CacheProvider.ExistsInCache(MapRequestConfig))
            {
                ///todo: think of a less hacky solution to mime type here!
                mimeType = MapRequestConfig.MimeType;
                Stream s = CacheProvider.RetrieveStream(MapRequestConfig);
                s.Position = 0;
                return s;
            }
            else
            {
                RaiseBeforeInitializeMap();
                InitMap();
                RaiseMapInitialized();

                RaiseBeforeLoadLayers();
                LoadLayers();
                RaiseLayersLoaded();

                ConfigureMap();

                RaiseBeforeLoadMapState();
                LoadMapState();
                RaiseMapStateLoaded();

                ConfigureRenderer();

                RaiseBeforeMapRender();

                Stream s = MapRenderer.Render(Map, out mimeType);

                Debug.Assert(
                    mimeType == MapRequestConfig.MimeType,
                    "Actual MimeType matches expected MimeType");

                try
                {
                    s.Position = 0;
                    CacheProvider.SaveToCache(MapRequestConfig, s);
                    RaiseMapRenderDone();
                    s.Position = 0;
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
