namespace SharpMap.Web.Wms.Handlers
{
    using System;
    using System.Web;

    internal class HandlerParams
    {
        public HandlerParams(HttpContext context, Map map, 
            Capabilities.WmsServiceDescription description) :
            this(context, map, description, true) { }

        public HandlerParams(HttpContext context, Map map, 
            Capabilities.WmsServiceDescription description,
            bool ignoreCase)
        {
            if (context == null)
                throw new ArgumentNullException("context");
            if (map == null)
                throw new ArgumentNullException("map");

            this.context = context;
            this.map = map;
            this.description = description;
            this.ignoreCase = ignoreCase;
        }

        private readonly HttpContext context;

        public HttpContext Context
        {
            get { return this.context; }
        }

        private readonly Map map;

        public Map Map
        {
            get { return this.map; }
        }

        private readonly Capabilities.WmsServiceDescription description;

        public Capabilities.WmsServiceDescription Description
        {
            get { return this.description; }
        }

        private readonly bool ignoreCase;

        public bool IgnoreCase
        {
            get { return this.ignoreCase; }
        }
    }
}