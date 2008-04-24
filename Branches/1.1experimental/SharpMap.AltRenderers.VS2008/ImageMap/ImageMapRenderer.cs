using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Xml;
using SharpMap.Data;
using SharpMap.Data.Providers;
using SharpMap.Geometries;
using SharpMap.Layers;
using SharpMap.Renderer;
using SharpMap.Renderers.ImageMap.Impl;
using SharpMap.Styles;

namespace SharpMap.Renderers.ImageMap
{
    public class ImageMapRenderer
        : IMapRenderer<XmlDocument>
    {
        public ImageMapRenderer() { }

        private HttpContext Context
        {
            get
            {
                return HttpContext.Current;
            }
        }

        private ImageMapStyle _imageMapStyle;
        public ImageMapStyle ImageMapStyle
        {
            get
            {
                return _imageMapStyle;
            }
            set
            {
                _imageMapStyle = value;
            }
        }


        private Func<ILayer, FeatureDataRow, ImageMapStyle> _themeProvider;
        /// <summary>
        /// A function which determines styling for a feature; for instance the radius of a point  
        /// </summary>
        public Func<ILayer, FeatureDataRow, ImageMapStyle> ImageMapThemeProvider
        {
            get { return _themeProvider; }
            set { _themeProvider = value; }
        }


        private readonly Dictionary<string, Func<ILayer, FeatureDataRow, string>> _attributeProviders
            = new Dictionary<string, Func<ILayer, FeatureDataRow, string>>();

        /// <summary>
        /// a dictionary of functions which add attributes to a Feature Element.
        /// if more than one provider set the same attribute on an element the last one wins.
        /// </summary>
        /// <example>
        /// ImageMapRenderer renderer = new ImageMapRenderer();
        /// renderer.AttributeProviders.Add("href" , new Func[ILayer, FeatureDataRow, string](
        /// delegate(ILayer l,FeatureDataRow f)
        ///     
        ///     { 
        ///         if (l as VectorLayer == null) //the layer is not a vector layer so we ignore it 
        ///         {
        ///             return "";
        ///         }
        ///         else
        ///         { 
        ///             return (string)f["myHrefColumn"]; //return the column 'myHrefColumn' from the FeatureDataRow
        ///         } 
        ///     }
        /// ))
        /// </example> 
        public Dictionary<string, Func<ILayer, FeatureDataRow, string>> AttributeProviders
        {
            get
            {
                return _attributeProviders;
            }
        }




        #region IMapRenderer<XmlDocument> Members

        public event EventHandler RenderDone;
        public event EventHandler<LayerRenderedEventArgs> LayerRendered;


        private void OnRenderDone()
        {
            if (this.RenderDone != null)
                this.RenderDone(this, EventArgs.Empty);
        }

        private void OnLayerRendered(ILayer lyr)
        {
            if (this.LayerRendered != null)
                this.LayerRendered(this, new LayerRenderedEventArgs(lyr));
        }

        public XmlDocument Render(Map map)
        {
            XmlDocument xdoc = new XmlDocument();
            XmlElement mapEl = xdoc.CreateElement("map");
            xdoc.AppendChild(mapEl);

            //if (_attributeProviders.Count == 0)
            //    throw new InvalidOperationException("No attribute providers have been set. The resulting image  map would have no functionality");

            if (_imageMapStyle == null && _themeProvider == null)
                throw new InvalidOperationException("No ImageMapStyle is available. Nothing would be output. Please set ImageMapStyle or ImageMapThemeProvider (or both)");

            List<ImageMapFeatureElement> elements = new List<ImageMapFeatureElement>();


            int SRID = (map.Layers.Count > 0 ? map.Layers[0].SRID : -1); //Get the SRID of the first layer
            for (int i = 0; i < map.Layers.Count; i++)
            {
                ILayer lyr = map.Layers[i];
                if (!Context.Response.IsClientConnected)
                {
                    return new XmlDocument();
                }
                if (SRID != lyr.SRID) //Check that all layers have the same SRID
                    throw (new ArgumentException("An attempt was made to add two layers with different SRIDs"));
                if (lyr.Enabled && lyr.MaxVisible > map.Zoom && lyr.MinVisible <= map.Zoom)
                {
                    CreateFeatureElements(elements, lyr, map);
                    OnLayerRendered(lyr);
                }
            }

            elements.Sort();
            foreach (ImageMapFeatureElement immapel in elements)
            {
                immapel.Render(xdoc);
            }

            OnRenderDone();
            return xdoc;
        }

        private void CreateFeatureElements(List<ImageMapFeatureElement> targetList, ILayer lyr, Map map)
        {
            if (map.Center == null)
                throw (new ApplicationException("Cannot render map. View center not specified"));



            IDataLayer dataLayer = lyr as IDataLayer;
            if (dataLayer == null)
                return;

            IProvider dataSource = dataLayer.DataSource;

            if (this.ImageMapStyle != null || this._themeProvider != null)
            {


                FeatureDataSet ds = new FeatureDataSet();

                dataSource.Open();
                dataSource.ExecuteIntersectionQuery(map.Envelope, ds);
                dataSource.Close();

                if (ds.Tables.Count > 0)
                {
                    FeatureDataTable features = ds.Tables[0];

                    for (int k = 0; k < features.Count; k++)
                    {
                        FeatureDataRow fdr = features[k];

                        ImageMapStyle useStyle =
                            this._themeProvider != null ? this._themeProvider(lyr, fdr) : this.ImageMapStyle;

                        if (useStyle == null
                            || !useStyle.Enabled
                            || map.Zoom < useStyle.MinVisible
                            || map.Zoom > useStyle.MaxVisible)
                            continue;

                        AddFeatureElement(targetList, map, lyr, fdr.Geometry, fdr, useStyle);

                    }
                }
            }
        }

        private void AddFeatureElement(List<ImageMapFeatureElement> storage, Map map, ILayer lyr, Geometry geometry, FeatureDataRow fdr, ImageMapStyle useStyle)
        {
            if (geometry as GeometryCollection != null)
            {
                foreach (Geometry geom in geometry as GeometryCollection)
                {
                    AddFeatureElement(storage, map, lyr, geom, fdr, useStyle);
                }
            }
            else
            {
                ImageMapFeatureElement imel = ImageMapFeatureElement.CreateImageMapElement(geometry, map, useStyle);
                if (imel != null)
                {
                    foreach (KeyValuePair<string, Func<ILayer, FeatureDataRow, string>> attrAccessor in AttributeProviders)
                    {
                        string attrVal = attrAccessor.Value(lyr, fdr);
                        if (!string.IsNullOrEmpty(attrVal))
                            imel.AddAttribute(attrAccessor.Key, attrVal);
                    }
                    storage.Add(imel);
                }
            }
        }

        #endregion

        #region IMapRenderer Members


        Stream IMapRenderer.Render(Map map, out string mimeType)
        {
            mimeType = "text/xml";
            MemoryStream ms = new MemoryStream();
            this.Render(map).Save(ms);
            return ms;
        }

        #endregion
    }
}
