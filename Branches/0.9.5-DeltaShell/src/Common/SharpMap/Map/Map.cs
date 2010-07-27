// Copyright 2005, 2006 - Morten Nielsen (www.iter.dk)
//
// This file is part of SharpMap.
// SharpMap is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
// 
// SharpMap is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.

// You should have received a copy of the GNU Lesser General Public License
// along with SharpMap; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA 

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.Threading;
using DelftTools.Utils;
using DelftTools.Utils.Aop.NotifyPropertyChanged;
using DelftTools.Utils.Collections;
using DelftTools.Utils.Collections.Generic;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;
using log4net;
using SharpMap.Layers;
using System.Linq;
using SharpMap.Utilities;
using GeometryFactory=SharpMap.Converters.Geometries.GeometryFactory;

namespace SharpMap
{
    /// <summary>
    /// Map class
    /// </summary>
    /// <example>
    /// Creating a new map instance, adding layers and rendering the map:
    /// <code lang="C#">
    /// SharpMap.Map myMap = new SharpMap.Map(picMap.Size);
    /// myMap.MinimumZoom = 100;
    /// myMap.BackgroundColor = Color.White;
    /// 
    /// SharpMap.Layers.VectorLayer myLayer = new SharpMap.Layers.VectorLayer("My layer");
    ///	string ConnStr = "Server=127.0.0.1;Port=5432;User Id=postgres;Password=password;Database=myGisDb;";
    /// myLayer.DataSource = new SharpMap.Data.Providers.PostGIS(ConnStr, "myTable", "the_geom", 32632);
    /// myLayer.FillStyle = new SolidBrush(Color.FromArgb(240,240,240)); //Applies to polygon types only
    ///	myLayer.OutlineStyle = new Pen(Color.Blue, 1); //Applies to polygon and linetypes only
    /// //Setup linestyle (applies to line types only)
    ///	myLayer.Style.Line.Width = 2;
    ///	myLayer.Style.Line.Color = Color.Black;
    ///	myLayer.Style.Line.EndCap = System.Drawing.Drawing2D.LineCap.Round; //Round end
    ///	myLayer.Style.Line.StartCap = layRailroad.LineStyle.EndCap; //Round start
    ///	myLayer.Style.Line.DashPattern = new float[] { 4.0f, 2.0f }; //Dashed linestyle
    ///	myLayer.Style.EnableOutline = true;
    ///	myLayer.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias; //Render smooth lines
    ///	myLayer.MaxVisible = 40000;
    /// 
    /// myMap.Layers.Add(myLayer);
    /// // [add more layers...]
    /// 
    /// myMap.Center = new SharpMap.Geometries.Point(725000, 6180000); //Set center of map
    ///	myMap.Zoom = 1200; //Set zoom level
    /// myMap.Size = new System.Drawing.Size(300,200); //Set output size
    /// 
    /// System.Drawing.Image imgMap = myMap.GetMap(); //Renders the map
    /// </code>
    /// </example>
    [NotifyPropertyChanged]
    [Serializable]
    public class Map : IDisposable, INotifyCollectionChanged, INameable, ICloneable
    {
        //used in zoomtoextends to have default 10 percent margin 
        private const int defaultExtendsMarginPercentage = 10;
        private static readonly ILog log = LogManager.GetLogger(typeof (Map));

        /// <summary>
        /// Used for converting numbers to/from strings
        /// </summary>
        public static NumberFormatInfo numberFormat_EnUS =
            new CultureInfo("en-US", false).NumberFormat;

        private IEventedList<ILayer> layers;

        public virtual long Id
        {
            get { return id; }
            set { id = value; }
        }

        private double worldHeight;
        private double worldLeft;
        private double worldTop;

        /// <summary>
        /// Initializes a new map
        /// </summary>
        public Map() : this(new Size(100, 100))
        {
        }

        /// <summary>
        /// Initializes a new map
        /// </summary>
        /// <param name="size">Size of map in pixels</param>
        public Map(Size size)
        {
            name = "map";

            _MaximumZoom = double.MaxValue;
            _MinimumZoom = 0;
            _Center = GeometryFactory.CreateCoordinate(0, 0);
            _Zoom = 1000;
            _PixelAspectRatio = 1.0;

            Size = size;

            Layers = new EventedList<ILayer>();

            BackColor = Color.Transparent;
            mapTransform = new Matrix();
            mapTransformInverted = new Matrix();

            UpdateDimensions();
        }

        private void UpdateDimensions()
        {
            pixelSize = _Zoom/_Size.Width;
            pixelHeight = pixelSize*_PixelAspectRatio;
            worldHeight = (_Zoom*_Size.Height)/_Size.Width;
            worldLeft = _Center.X - _Zoom*0.5;
            worldTop = _Center.Y + worldHeight*0.5*_PixelAspectRatio;
        }

        /// <summary>
        /// Disposes the map object
        /// </summary>
        public void Dispose()
        {
            foreach (Layer layer in Layers)
                if (layer is IDisposable)
                    ((IDisposable) layer).Dispose();
            Layers.Clear();
        }

        #region Events

        /// <summary>
        /// EventHandler for event fired when the zoomlevel or the center point has been changed
        /// </summary>
        public delegate void MapViewChangedHandler();

        /// <summary>
        /// Event fired when the zoomlevel or the center point has been changed
        /// </summary>
        public virtual event MapViewChangedHandler MapViewOnChange;

        /// <summary>
        /// EventHandler for event fired when all layers have been rendered
        /// </summary>
        public delegate void MapLayerRenderedEventHandler(Graphics g, ILayer layer);

        public virtual event MapLayerRenderedEventHandler MapLayerRendered;

        /// <summary>
        /// EventHandler for event fired when all layers have been rendered
        /// </summary>
        public delegate void MapRenderedEventHandler(Graphics g);

        /// <summary>
        /// Event fired when all layers have been rendered
        /// </summary>
        public virtual event MapRenderedEventHandler MapRendered;

        public virtual event MapRenderedEventHandler MapRendering;

        #endregion

        #region Methods

        public virtual Image Image
        {
            get { return image; }
        }

        public static bool UseParallelRendering = true;

        private void SetRenderRequiredForAllLayers()
        {
            if (Layers == null)
            {
                return;
            }

            foreach (ILayer layer in layers)
            {
                layer.RenderRequired = true;
            }
        }

        /// <summary>
        /// Renders the map to an image
        /// </summary>
        /// <returns></returns>
        public virtual Image Render()
        {
            DateTime startTime = DateTime.Now;

            if (Size.IsEmpty)
            {
                return null; // nothing to render
            }

            if (MapRendering != null)
            {
                MapRendering(null);
            }

            if (image != null)
            {
                image.Dispose();
            }

            image = new Bitmap(Size.Width, Size.Height, PixelFormat.Format32bppPArgb);

            int SRID = (Layers.Count > 0 ? Layers[0].SRID : -1); //Get the SRID of the first layer

            if (rendering)
            {
                return null;
            }

            rendering = true;

            // TODO: draw using multiple threads
/*            Action<int> renderLayer = delegate(int i)
                                          {
                                              if (Layers[i].RenderRequired)
                                              {
                                                  Layers[i].Render();
                                              }
                                          };
            Parallel.For(0, Layers.Count, renderLayer);
 */

            for (int i = 0; i < Layers.Count; i++)
            {
                if (Layers[i].RenderRequired)
                {
                    Layers[i].Render();
                }
            }

            // merge all layer bitmaps
            Graphics g = Graphics.FromImage(image);
            g.Clear(BackColor);

            for (int i = Layers.Count - 1; i >= 0; i--)
            {
                if (Layers[i].Enabled && Layers[i].MaxVisible >= Zoom && Layers[i].MinVisible < Zoom)
                {
                    g.DrawImage(Layers[i].Image, 0, 0);
                    if (MapLayerRendered != null)
                    {
                        MapLayerRendered(g, Layers[i]);
                    }

                    #region Logging

                    // show time and number of features rendered by the layer
                    long lastRenderedFeaturesCount = 0;
                    long lastRenderedCoordinatesCount = 0;
                    var vectorLayer = Layers[i] as VectorLayer;

                                        if (vectorLayer != null)
                                        {
                                            lastRenderedFeaturesCount = vectorLayer.LastRenderedFeaturesCount;
                                            lastRenderedCoordinatesCount = vectorLayer.LastRenderedCoordinatesCount;
                                        }
                                        log.DebugFormat("Layer {0} rendered in {1:F0} ms, features / coordinates count:{2} / {3}",
                                                        Layers[i].Name, Layers[i].LastRenderDuration, lastRenderedFeaturesCount,
                                                        lastRenderedCoordinatesCount);

                    #endregion
                }
            }

            g.Transform = MapTransform;
            g.PageUnit = GraphicsUnit.Pixel;

            if (MapRendered != null)
            {
                MapRendered(g);
            }

            g.Dispose();

            /* don't delete, enable when optimizing performance

            double dt = (DateTime.Now - startTime).TotalMilliseconds;
            log.DebugFormat("Map rendered in {0:F0} ms, size {1} x {2} px", dt, Size.Width, Size.Height);
             */

            rendering = false;

            return Image;
        }

        private bool rendering;

        /// <summary>
        /// Returns an enumerable for all layers containing the search parameter in the LayerName property
        /// </summary>
        /// <param name="layername">Search parameter</param>
        /// <returns>IEnumerable</returns>
        public virtual IEnumerable<ILayer> FindLayer(string layername)
        {
            foreach (ILayer l in Layers)
                if (l.Name.Contains(layername))
                    yield return l;
        }

        /// <summary>
        /// Returns a layer by its name
        /// </summary>
        /// <param name="name">Name of layer</param>
        /// <returns>Layer</returns>
        public virtual ILayer GetLayerByName(string name)
        {
            //return Layers.Find(delegate(SharpMap.Layers.ILayer layer) { return layer.LayerName.Equals(name); });
            for (int i = 0; i < Layers.Count; i++)
                if (String.Equals(Layers[i].Name, name, StringComparison.InvariantCultureIgnoreCase))
                    return Layers[i];

            return null;
        }

        /// <summary>
        /// Returns the (first) layer on which this feature is present.
        /// </summary>
        /// <param name="feature">The feature to search for.</param>
        /// <returns>The layer that contains the searched feature.</returns>
        public virtual ILayer GetLayerByFeature(IFeature feature)
        {
            ILayer foundLayer = null;
            foreach (ILayer findLayer in Layers)
            {
                // Check if the feature is on this layer (or it's contained layers)
                foundLayer = FindLayerByFeature(findLayer, feature);
                if (foundLayer != null)
                {
                    // Return the first layer that the feature was found on
                    return foundLayer;
                }
            }
            return foundLayer;
        }

        /// <summary>
        /// Recursively search through the layer(group) to find a given feature.
        /// </summary>
        /// <param name="searchLayer">The layer that should now be searched.</param>
        /// <param name="searchFeature">The feature to find.</param>
        /// <returns>The layer that contains the given feature, or null if it was not found.</returns>
        private static ILayer FindLayerByFeature(ILayer searchLayer, IFeature searchFeature)
        {
            // Searchable layer? Does it contain the feature?
            if (searchLayer is VectorLayer && ((VectorLayer) searchLayer).DataSource != null && ((VectorLayer) searchLayer).DataSource.Contains(searchFeature))
            {
                return searchLayer;
            }
            // Recursively search trough layers if this is a layer group
            if (searchLayer is LayerGroup)
            {
                foreach (ILayer testLayer in ((LayerGroup) searchLayer).Layers)
                {
                    ILayer testResult = FindLayerByFeature(testLayer, searchFeature);
                    if (testResult != null)
                    {
                        return testResult;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Zooms to the extents of all layers
        /// Adds an extra 10 % marge to each border
        /// </summary>
        public virtual void ZoomToExtents()
        {
            IEnvelope boundingBox = GetExtents();
            if (null == boundingBox)
                return;
            boundingBox = (IEnvelope) boundingBox.Clone();
            // beware of true 1d networks
            if ((boundingBox.Width < 1.0e-6) && (boundingBox.Height < 1.0e-6))
            {
                return;
            }
            
            AddMargin(boundingBox,defaultExtendsMarginPercentage);
            ZoomToBox(boundingBox);
        }
        /// <summary>
        /// Expands the given boundingBox by percentage.
        /// </summary>
        /// <param name="boundingBox">Boundingbox to expand</param>
        /// <param name="percentage">Percentage by which boundingBox is expanded</param>
        private static void AddMargin(IEnvelope boundingBox,double percentage)
        {
            double minX = 0.0;
            double minY = 0.0;
            if (boundingBox.Width < 1.0e-6)
            {
                minX = 1.0;
            }
            if (boundingBox.Height < 1.0e-6)
            {
                minY = 1.0;
            }

            var factor = percentage/100;
            boundingBox.ExpandBy(minX + boundingBox.Width * factor, minY + boundingBox.Height * factor);
        }

        /// <summary>
        /// Zooms the map to fit a bounding box
        /// </summary>
        /// <remarks>
        /// NOTE: If the aspect ratio of the box and the aspect ratio of the mapsize
        /// isn't the same, the resulting map-envelope will be adjusted so that it contains
        /// the bounding box, thus making the resulting envelope larger!
        /// </remarks>
        /// <param name="bbox"></param>
        public virtual void ZoomToBox(IEnvelope bbox)
        {
            ZoomToBox(bbox,false);
        }


        /// <summary>
        /// Zooms the map to fit a bounding box
        /// </summary>
        /// <remarks>
        /// NOTE: If the aspect ratio of the box and the aspect ratio of the mapsize
        /// isn't the same, the resulting map-envelope will be adjusted so that it contains
        /// the bounding box, thus making the resulting envelope larger!
        /// </remarks>
        /// <param name="bbox"></param>
        /// <param name="withMargin">Add a default margin?</param>
        public virtual void ZoomToBox(IEnvelope bbox,bool withMargin)
        {
            if (bbox == null || bbox.Width == 0 || bbox.Height == 0)
            {
                return;
            }

            _Zoom = bbox.Width; //Set the private center value so we only fire one MapOnViewChange event
            if (Envelope.Height < bbox.Height)
                _Zoom *= bbox.Height / Envelope.Height;
            Center = bbox.Centre;

            if (withMargin)
            {
                AddMargin(bbox, defaultExtendsMarginPercentage);
            }
            UpdateDimensions();
            SetRenderRequiredForAllLayers();
        }

        /// <summary>
        /// Converts a point from world coordinates to image coordinates based on the current
        /// zoom, center and mapsize.
        /// </summary>
        /// <param name="p">Point in world coordinates</param>
        /// <returns>Point in image coordinates</returns>
        public virtual PointF WorldToImage(ICoordinate p)
        {
            return Transform.WorldtoMap(p, this);
        }

        /// <summary>
        /// Converts a point from image coordinates to world coordinates based on the current
        /// zoom, center and mapsize.
        /// </summary>
        /// <param name="p">Point in image coordinates</param>
        /// <returns>Point in world coordinates</returns>
        public virtual ICoordinate ImageToWorld(PointF p)
        {
            return Transform.MapToWorld(p, this);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the extents of the current map based on the current zoom, center and mapsize
        /// </summary>
        public virtual IEnvelope Envelope
        {
            get
            {
                return GeometryFactory.CreateEnvelope(
                    Center.X - Zoom*.5,
                    Center.X + Zoom*.5,
                    Center.Y - MapHeight*.5,
                    Center.Y + MapHeight*.5);
            }
        }


        [NonSerialized] private Matrix mapTransform;
        [NonSerialized] private Matrix mapTransformInverted;

        /// <summary>
        /// Using the <see cref="MapTransform"/> you can alter the coordinate system of the map rendering.
        /// This makes it possible to rotate or rescale the image, for instance to have another direction than north upwards.
        /// </summary>
        /// <example>
        /// Rotate the map output 45 degrees around its center:
        /// <code lang="C#">
        /// System.Drawing.Drawing2D.Matrix maptransform = new System.Drawing.Drawing2D.Matrix(); //Create transformation matrix
        ///	maptransform.RotateAt(45,new PointF(myMap.Size.Width/2,myMap.Size.Height/2)); //Apply 45 degrees rotation around the center of the map
        ///	myMap.MapTransform = maptransform; //Apply transformation to map
        /// </code>
        /// </example>
        public virtual Matrix MapTransform
        {
            get { return mapTransform; }
            set
            {
                mapTransform = value;
                if (mapTransform.IsInvertible)
                {
                    mapTransformInverted = mapTransform.Clone();
                    mapTransformInverted.Invert();
                }
                else
                    mapTransformInverted.Reset();

                SetRenderRequiredForAllLayers();
            }
        }

        /// <summary>
        /// A collection of layers. The first layer in the list is drawn first, the last one on top.
        /// </summary>
        public virtual IEventedList<ILayer> Layers
        {
            get { return layers; }
            set
            {
                if (layers != null)
                {
                    layers.CollectionChanged -= _Layers_CollectionChanged;
                }

                layers = value;

                layers.CollectionChanged += _Layers_CollectionChanged;

                foreach (var layer in layers)
                {
                    layer.Map = this;
                }
            }
        }

        private void _Layers_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Item is ILayer)
            {
                var layer = (ILayer) e.Item;

                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Replace:
                        throw new NotImplementedException();

                    case NotifyCollectionChangedAction.Add:
                        CheckMapExtends(layer);
                        layer.Map = this;
                        layer.RenderRequired = true;
                        break;
                }
            }

            if (CollectionChanged == null)
            {
                return;
            }

            if (sender == Layers)
            {
                CollectionChanged(this, e);
            }
            else
            {
                CollectionChanged(sender, e);
            }
        }

        /// <summary>
        /// Zooms map to extends if the added layer is the only layer with valid envelope.
        /// </summary>
        /// <param name="layer"></param>
        private void CheckMapExtends(ILayer layer)
        {
            var allVisibleLayersWereEmpty = Layers.Except(new[] { layer }).All(l => l.Envelope.IsNull || !l.IsVisible );
            if (allVisibleLayersWereEmpty && (layer.Envelope != null &&  !layer.Envelope.IsNull))
            {
                ZoomToExtents();
            }
        }

        private Color _BackgroundColor;

        /// <summary>
        /// Map background color (defaults to transparent)
        /// </summary>
        public virtual Color BackColor
        {
            get { return _BackgroundColor; }
            set
            {
                _BackgroundColor = value;
                if (MapViewOnChange != null)
                    MapViewOnChange();
            }
        }

        private ICoordinate _Center;

        /// <summary>
        /// Center of map in WCS
        /// </summary>
        public virtual ICoordinate Center
        {
            get { return _Center; }
            set
            {
                _Center = value;
                UpdateDimensions();
                SetRenderRequiredForAllLayers();
            }
        }

        private double _Zoom;

        /// <summary>
        /// Gets or sets the zoom level of map.
        /// </summary>
        /// <remarks>
        /// <para>The zoom level corresponds to the width of the map in WCS units.</para>
        /// <para>A zoomlevel of 0 will result in an empty map being rendered, but will not throw an exception</para>
        /// </remarks>
        public virtual double Zoom
        {
            get { return _Zoom; }
            set
            {
                double oldZoom = _Zoom;
                if (value < _MinimumZoom)
                {
                    _Zoom = _MinimumZoom;
                }
                else if (value > _MaximumZoom)
                {
                    _Zoom = _MaximumZoom;
                }
                else
                {
                    _Zoom = value;
                }
                UpdateDimensions();
                if (MapViewOnChange != null)
                {
                    MapViewOnChange();
                }
                SetRenderRequiredForAllLayers();
            }
        }

        /// <summary>
        /// Gets the extents of the map based on the extents of all the layers in the layers collection
        /// </summary>
        /// <returns>Full map extents</returns>
        public virtual IEnvelope GetExtents()
        {
            if (Layers == null || Layers.Count == 0)
            {
                return null;
            }

            IEnvelope envelope = new Envelope();
            for (int i = 0; i < Layers.Count; i++)
            {
                if (Layers[i].IsVisible)
                {
                    var layerEnvelope = Layers[i].Envelope;
                    if (layerEnvelope != null && !layerEnvelope.IsNull)
                    {
                        envelope.ExpandToInclude(layerEnvelope);
                    }
                }
            }

            return envelope;
        }

        public double WorldHeight
        {
            get { return worldHeight; }
        }

        public double WorldLeft
        {
            get { return worldLeft; }
        }

        public double WorldTop
        {
            get { return worldTop; }
        }

        /// <summary>
        /// Returns the size of a pixel in world coordinate units
        /// </summary>
        public virtual double PixelSize
        {
            get { return pixelSize; }
        }

        /// <summary>
        /// Returns the width of a pixel in world coordinate units.
        /// </summary>
        /// <remarks>The value returned is the same as <see cref="PixelSize"/>.</remarks>
        public virtual double PixelWidth
        {
            get { return pixelSize; }
        }

        /// <summary>
        /// Returns the height of a pixel in world coordinate units.
        /// </summary>
        /// <remarks>The value returned is the same as <see cref="PixelSize"/> unless <see cref="PixelAspectRatio"/> is different from 1.</remarks>
        public virtual double PixelHeight
        {
            get { return pixelHeight; }
        }

        private double _PixelAspectRatio = 1.0;

        /// <summary>
        /// Gets or sets the aspect-ratio of the pixel scales. A value less than 
        /// 1 will make the map streach upwards, and larger than 1 will make it smaller.
        /// </summary>
        /// <exception cref="ArgumentException">Throws an argument exception when value is 0 or less.</exception>
        public virtual double PixelAspectRatio
        {
            get { return _PixelAspectRatio; }
            set
            {
                if (_PixelAspectRatio <= 0)
                    throw new ArgumentException("Invalid Pixel Aspect Ratio");
                _PixelAspectRatio = value;
                UpdateDimensions();
                SetRenderRequiredForAllLayers();
            }
        }

        /// <summary>
        /// Height of map in world units
        /// </summary>
        /// <returns></returns>
        public virtual double MapHeight
        {
            get { return (Zoom*Size.Height)/Size.Width*PixelAspectRatio; }
        }

        private Size _Size;

        /// <summary>
        /// Size of output map
        /// </summary>
        public virtual Size Size
        {
            get { return _Size; }
            set
            {
/*
                var oldSize = _Size;
                var oldZoomLevel = pixelSize * _Size.Width;
*/

                _Size = value;
/*

                _Zoo
                pixelSize = _Zoom / _Size.Width;
                pixelHeight = pixelSize * _PixelAspectRatio;
                worldHeight = (_Zoom * _Size.Height) / _Size.Width;
                worldLeft = _Center.X - _Zoom * 0.5;
                worldTop = _Center.Y + worldHeight * 0.5 * _PixelAspectRatio;
*/

                UpdateDimensions();
                SetRenderRequiredForAllLayers();
            }
        }

        private double _MinimumZoom;

        /// <summary>
        /// Minimum zoom amount allowed
        /// </summary>
        public virtual double MinimumZoom
        {
            get { return _MinimumZoom; }
            set
            {
                if (value < 0)
                    throw (new ArgumentException("Minimum zoom must be 0 or more"));
                _MinimumZoom = value;
                SetRenderRequiredForAllLayers();
            }
        }

        private double _MaximumZoom;
        private string name;
        private long id;

        private Image image;
        public static int MaxThreadsCount = 4;
        private double pixelSize;
        private double pixelHeight;

        /// <summary>
        /// Maximum zoom amount allowed
        /// </summary>
        public virtual double MaximumZoom
        {
            get { return _MaximumZoom; }
            set
            {
                if (value <= 0)
                    throw (new ArgumentException("Maximum zoom must larger than 0"));
                _MaximumZoom = value;
                SetRenderRequiredForAllLayers();
            }
        }

        public virtual string Name
        {
            get { return name; }
            set { name = value; }
        }

        #endregion

        //#region ISerializable Members

        ///// <summary>
        ///// Populates a SerializationInfo with the data needed to serialize the target object.
        ///// </summary>
        ///// <param name="info"></param>
        ///// <param name="context"></param>
        //public void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
        //{
        //    System.Runtime.Serialization.SurrogateSelector ss = SharpMap.Utilities.Surrogates.GetSurrogateSelectors();
        //    info.AddValue("BackgroundColor", this._BackgroundColor);
        //    info.AddValue("Center", this._Center);
        //    info.AddValue("Layers", this.layers);
        //    info.AddValue("MapTransform", this._MapTransform);
        //    info.AddValue("MaximumZoom", this._MaximumZoom);
        //    info.AddValue("MinimumZoom", this._MinimumZoom);
        //    info.AddValue("Size", this._Size);
        //    info.AddValue("Zoom", this._Zoom);

        //}
        ///// <summary>
        ///// Deserialization constructor.
        ///// </summary>
        ///// <param name="info"></param>
        ///// <param name="ctxt"></param>
        //internal Map(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext ctxt)
        //{
        //    this._BackgroundColor = (System.Drawing.Color)info.GetValue("BackgroundColor", typeof(System.Drawing.Color));
        //    this._Center = (SharpMap.Geometries.Point)info.GetValue("Center", typeof(SharpMap.Geometries.Point));
        //    this.layers = (List<SharpMap.Layers.ILayer>)info.GetValue("Layers", typeof(List<SharpMap.Layers.ILayer>));
        //    this._MapTransform = (System.Drawing.Drawing2D.Matrix)info.GetValue("MapTransform", typeof(System.Drawing.Drawing2D.Matrix));
        //    this._MaximumZoom = info.GetDouble("MaximumZoom");
        //    this._MinimumZoom = info.GetDouble("MinimumZoom");
        //    this._Size = (System.Drawing.Size)info.GetValue("Size", typeof(System.Drawing.Size));
        //    this._Zoom = info.GetDouble("Zoom");
        //}

        //#endregion

        #region INotifyCollectionChanged Members

        public virtual event NotifyCollectionChangedEventHandler CollectionChanged;
        public virtual event NotifyCollectionChangedEventHandler CollectionChanging;

        #endregion

        public object Clone()
        {
            Map clone = new Map(this.Size);
            clone.name = this.name;
            clone.Center = new Coordinate(this.Center);

            clone._MinimumZoom = _MinimumZoom;
            clone._MaximumZoom = _MaximumZoom;
            clone.Zoom = Zoom;

            foreach(ILayer layer in layers)
            {
                clone.layers.Add((ILayer) layer.Clone());
            }
            return clone;
        }
    }
}