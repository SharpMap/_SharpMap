using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using SharpMap.Renderer;
using SharpMap.Layers;
using System.IO;
using SharpMap.Geometries;

namespace SharpMap.Presentation.AspNet.Impl
{
    public abstract class WebMapBase<TMapRequestConfig, TOutput>
         : IWebMap<TMapRequestConfig, TOutput>
         where TMapRequestConfig : IMapRequestConfig
    {
        public WebMapBase(HttpContext context)
        {
            this._context = context;
        }

        #region IWebMap<TMapRequestConfig,TOutput> Members

        private IMapRenderer<TOutput> _renderer;
        public IMapRenderer<TOutput> MapRenderer
        {
            get
            {
                if (_renderer == null)
                    _renderer = CreateMapRenderer();
                return _renderer;
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

        public event EventHandler BeforeConfigureMapView;
        protected void RaiseBeforeConfigureMapView()
        {
            if (this.BeforeConfigureMapView != null)
                this.BeforeConfigureMapView(this, EventArgs.Empty);
        }
        public event EventHandler MapViewConfigDone;
        protected void RaiseMapViewConfigDone()
        {
            if (this.MapViewConfigDone != null)
            {
                this.MapViewConfigDone(this, EventArgs.Empty);
            }
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
        protected abstract IMapRenderer<TOutput> CreateMapRenderer();

        /// <summary>
        /// Create a config factory here
        /// </summary>
        /// <returns></returns>
        protected abstract IMapRequestConfigFactory<TMapRequestConfig> CreateConfigFactory();


        /// <summary>
        /// Use to add layers to the map. Not necessary if the layers were added when the map was created or if the map was retrieved from a persistance medium.
        /// </summary>
        public abstract void LoadLayers();

        /// <summary>
        /// this method will configure the map view by calling ConfigureMap on the MapRequestConfig
        /// </summary>
        public virtual void ConfigureMapView()
        {
            MapRequestConfig.ConfigureMap(this.Map);
        }

        /// <summary>
        /// Use this method to load user state into the map. Or leave empty if not required.
        /// </summary>
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




        private IMapRenderer<TOutput> _renderWrapper;


        private void EnsureRenderWrapper()
        {
            if (_renderWrapper == null)
            {
                _renderWrapper = CreateMapRenderer();
            }
        }


        TMapRequestConfig _config;
        public TMapRequestConfig MapRequestConfig
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
        IMapRequestConfigFactory<TMapRequestConfig> _configFactory;
        public IMapRequestConfigFactory<TMapRequestConfig> ConfigFactory
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
        protected HttpContext Context
        {
            get
            {
                return _context ?? HttpContext.Current;
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




        public virtual Stream Render(out string mimeType)
        {

            RaiseBeforeInitializeMap();
            InitMap();
            RaiseMapInitialized();

            RaiseBeforeLoadLayers();

            LoadLayers();
            RaiseLayersLoaded();
            Map.Center = new Point(0, 0);

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
                RaiseMapRenderDone();
                mimeType = MapRequestConfig.MimeType;
                return s;
            }
            catch
            {
                if (s != null)
                    s.Close();
                throw;
            }

        }


        #region IWebMap<TMapRequestConfig,TOutput> Members

        public Func<TOutput, Stream> StreamBuilder
        {
            get
            {
                return IoC.Container.ResolveObject<Func<TOutput, Stream>>();
            }
        }

        #endregion
    }
}
