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
using DelftTools.Utils.Aop.NotifyPropertyChange;
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
    [NotifyPropertyChange]
    [Serializable]
    public class Map : IDisposable, INotifyCollectionChange, INameable, ICloneable
    {
        //used in zoomtoextends to have default 10 percent margin 
        private const int defaultExtendsMarginPercentage = 10;
        
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

            maximumZoom = double.MaxValue;
            minimumZoom = 0;
            center = GeometryFactory.CreateCoordinate(0, 0);
            zoom = 1000;
            pixelAspectRatio = 1.0;

            Size = size;

            Layers = new EventedList<ILayer>();

            BackColor = Color.Transparent;
            mapTransform = new Matrix();
            mapTransformInverted = new Matrix();

            UpdateDimensions();
        }

        private void UpdateDimensions()
        {
            pixelSize = zoom/size.Width;
            pixelHeight = pixelSize*pixelAspectRatio;
            worldHeight = (zoom*size.Height)/size.Width;
            worldLeft = center.X - zoom*0.5;
            worldTop = center.Y + worldHeight*0.5*pixelAspectRatio;
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
                if (Layers[i].Visible && Layers[i].MaxVisible >= Zoom && Layers[i].MinVisible < Zoom)
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
/*
                                        log.DebugFormat("Layer {0} rendered in {1:F0} ms, features / coordinates count:{2} / {3}",
                                                        Layers[i].Name, Layers[i].LastRenderDuration, lastRenderedFeaturesCount,
                                                        lastRenderedCoordinatesCount);
*/

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
        /// Returns the (first) layer on which <paramref name="feature"/> is present.
        /// </summary>
        /// <param name="feature">The feature to search for.</param>
        /// <returns>The layer that contains the <paramref name="feature"/>.</returns>
        public virtual ILayer GetLayerByFeature(IFeature feature)
        {
            return GetLayerByFeatureLayerFindFunction(FindLayerByFeature, feature);
        }

        /// <summary>
        /// Returns the (first) layer with features of the same type as <paramref name="feature"/>.
        /// </summary>
        /// <param name="feature">The feature to search for.</param>
        /// <returns>The layer that contains features of the same type as <paramref name="feature"/>.</returns>
        public virtual ILayer GetLayerByFeatureType(IFeature feature)
        {
            return GetLayerByFeatureLayerFindFunction(FindLayerByFeatureType, feature);
        }

        private ILayer GetLayerByFeatureLayerFindFunction(Func<ILayer, IFeature, ILayer> featureLayerSearchFunction, IFeature feature)
        {
            ILayer foundLayer = null;
            foreach (ILayer findLayer in Layers)
            {
                foundLayer = featureLayerSearchFunction(findLayer, feature);
                if (foundLayer != null)
                {
                    // Return the first found layer
                    return foundLayer;
                }
            }
            return foundLayer;
        }

        /// <summary>
        /// Try to find a layer that contains <paramref name="searchFeature"/>.
        /// </summary>
        /// <param name="searchLayer">The layer that should be searched recursively.</param>
        /// <param name="searchFeature">The feature to find.</param>
        /// <returns>The layer that contains <paramref name="searchFeature"/>, or null if it was not found.</returns>
        private static ILayer FindLayerByFeature(ILayer searchLayer, IFeature searchFeature)
        {
            return FindLayerByFeatureRecursively(searchLayer, searchFeature, (layer, feature) => ((VectorLayer)layer).DataSource.Contains(feature));
        }

        /// <summary>
        /// Try to find a layer that contains features of the same type as <paramref name="searchFeature"/>
        /// </summary>
        /// <param name="searchLayer">The layer that should be searched (recursively).</param>
        /// <param name="searchFeature">The feature to find.</param>
        /// <returns>The layer that contains features of the same type as <paramref name="searchFeature"/>, or null if it was not found.</returns>
        private static ILayer FindLayerByFeatureType(ILayer searchLayer, IFeature searchFeature)
        {
            return FindLayerByFeatureRecursively(searchLayer, searchFeature, (layer, feature) => ((VectorLayer)layer).DataSource.FeatureType == feature.GetType());
        }

        private static ILayer FindLayerByFeatureRecursively(ILayer searchLayer, IFeature searchFeature, Func<ILayer, IFeature, bool> condition)
        {
            // Searchable layer? Condition is true?
            if (searchLayer is VectorLayer && ((VectorLayer)searchLayer).DataSource != null && condition(searchLayer, searchFeature))
            {
                return searchLayer;
            }

            // Recursively search trough layers if this is a layer group
            if (searchLayer is GroupLayer)
            {
                foreach (ILayer testLayer in ((GroupLayer)searchLayer).Layers)
                {
                    ILayer testResult = FindLayerByFeatureRecursively(testLayer, searchFeature, condition);
                    if (testResult != null)
                    {
                        return testResult;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Find the grouplayer for a given layer. Returns null if the layer is not contained in a group.
        /// </summary>
        /// <param name="map">Map containing the layer</param>
        /// <param name="childLayer">Child layer to be found</param>
        /// <returns>Grouplayer containing the childlayer or null if no grouplayer is found</returns>
        public IGroupLayer GetGroupLayerContainingLayer(ILayer childLayer)
        {
            return GetAllGroupLayers(Layers).FirstOrDefault(l => l.Layers.Contains(childLayer));
        }

        public IEnumerable<ILayer> GetAllLayers(bool includeGroupLayers)
        {
            return GetAllMapLayers(Layers, includeGroupLayers, false);
        }

        public IEnumerable<ILayer> GetAllVisibleLayers(bool includeGroupLayers)
        {
            return GetAllMapLayers(Layers, includeGroupLayers, true);
        }

        private static IEnumerable<ILayer> GetAllMapLayers(IEnumerable<ILayer> layers, bool includeGroupLayers, bool onlyVisibleLayers)
        {
            foreach (ILayer layer in layers)
            {
                if (onlyVisibleLayers && !layer.Visible)
                {
                    continue;
                }

                if (layer is GroupLayer)
                {
                    if (includeGroupLayers)
                    {
                        yield return layer;
                    }
                    IEnumerable<ILayer> childLayers = GetAllMapLayers(((GroupLayer)layer).Layers, includeGroupLayers, onlyVisibleLayers);
                    foreach (ILayer childLayer in childLayers)
                    {
                        yield return childLayer;
                    }
                }
                else
                {
                    yield return layer;
                }
            }
        }

        /// <summary>
        /// Gets all grouplayers in map. Including nested ones.
        /// </summary>
        /// <param name="map"></param>
        private static IEnumerable<IGroupLayer> GetAllGroupLayers(IEnumerable<ILayer> layers)
        {
            return GetAllMapLayers(layers, true, false).OfType<IGroupLayer>();
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
            ZoomToFit(boundingBox);
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

            var factor = percentage/200;//factor is used left and right so divide by 200 (iso 100)
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
        public virtual void ZoomToFit(IEnvelope bbox)
        {
            ZoomToFit(bbox, false);
        }

        /// <summary>
        /// Zooms the map to fit a bounding box. 
        /// </summary>
        /// <remarks>
        /// NOTE: If the aspect ratio of the box and the aspect ratio of the mapsize
        /// isn't the same, the resulting map-envelope will be adjusted so that it contains
        /// the bounding box, thus making the resulting envelope larger!
        /// </remarks>
        /// <param name="bbox"></param>
        /// <param name="addMargin">Add a default margin?</param>
        public virtual void ZoomToFit(IEnvelope bbox, bool addMargin)
        {
            if (bbox == null || bbox.Width == 0 || bbox.Height == 0)
            {
                return;
            }
            //create a copy so we don't mess up any given envelope...
            bbox = (IEnvelope) bbox.Clone();

            if (addMargin)
            {
                AddMargin(bbox, defaultExtendsMarginPercentage);
            }

            desiredEnvelope = bbox;

            zoom = bbox.Width; //Set the private center value so we only fire one MapOnViewChange event
            //if the map height is smaller than the given bbox height scale to the height
            if (Envelope.Height < bbox.Height)
                zoom *= bbox.Height / Envelope.Height;
            center = bbox.Centre;
            
            UpdateDimensions();

            if(GetExtents() == null || GetExtents().IsNull)
            {
                desiredEnvelope = Envelope;
            }


            if (MapViewOnChange != null)
            {
                MapViewOnChange();
            }
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
                    layers.CollectionChanging -= layers_CollectionChanging;
                    layers.CollectionChanged -= layers_CollectionChanged;
                }

                layers = value;

                if (layers != null)
                {
                    layers.CollectionChanging += layers_CollectionChanging;
                    layers.CollectionChanged += layers_CollectionChanged;
                }

                foreach (var layer in layers)
                {
                    layer.Map = this;
                }
            }
        }

        private void layers_CollectionChanging(object sender, NotifyCollectionChangingEventArgs e)
        {
            if(CollectionChanging != null) // bubble event up
            {
                CollectionChanging(sender, e);
            }
        }

        private void layers_CollectionChanged(object sender, NotifyCollectionChangingEventArgs e)
        {
            if (e.Item is ILayer)
            {
                var layer = (ILayer) e.Item;

                switch (e.Action)
                {
                    case NotifyCollectionChangeAction.Replace:
                        throw new NotImplementedException();

                    case NotifyCollectionChangeAction.Add:
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
            var allVisibleLayersWereEmpty = Layers.Except(new[] { layer }).All(l => l.Envelope.IsNull || !l.Visible);

            if (!allVisibleLayersWereEmpty)
            {
                return;
            }

            var layerEnvelope = layer.Envelope;

            if (layerEnvelope != null && !layerEnvelope.IsNull)
            {
                ZoomToExtents();
            }
        }

        private Color backColor;

        /// <summary>
        /// Map background color (defaults to transparent)
        /// </summary>
        public virtual Color BackColor
        {
            get { return backColor; }
            set
            {
                backColor = value;
                if (MapViewOnChange != null)
                    MapViewOnChange();
            }
        }

        private ICoordinate center;

        /// <summary>
        /// Center of map in WCS
        /// </summary>
        public virtual ICoordinate Center
        {
            get { return center; }
            set
            {
                var oldCenterX = center.X;
                var oldCenterY = center.Y;

                center = value;

                desiredEnvelope.SetCentre(center);

                ZoomToFit(desiredEnvelope, false);
            }
        }

        /// <summary>
        /// The envelope as last set by ZoomToFit(). Used to re-ZoomToFit on resize. Adjusted whenever Zoom is manually set.
        /// </summary>
        private IEnvelope desiredEnvelope;

        private double zoom;

        /// <summary>
        /// Gets or sets the zoom level of map.
        /// </summary>
        /// <remarks>
        /// <para>The zoom level corresponds to the width of the map in WCS units.</para>
        /// <para>A zoomlevel of 0 will result in an empty map being rendered, but will not throw an exception</para>
        /// </remarks>
        public virtual double Zoom
        {
            get { return zoom; }
            set
            {
                double oldZoom = zoom;
                double clippedZoom;

                if (value < minimumZoom)
                {
                    clippedZoom = minimumZoom;
                }
                else if (value > maximumZoom)
                {
                    clippedZoom = maximumZoom;
                }
                else
                {
                    clippedZoom = value;
                }
                
                desiredEnvelope.Zoom(100 * (clippedZoom / oldZoom)); //adjust desiredEnvelope 
                
                ZoomToFit(desiredEnvelope,false);

                zoom = clippedZoom; //using intermediate value because desired.Zoom(100*) causes minor rounding issues in ZoomToFit
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
                if (Layers[i].Visible)
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

        private double pixelAspectRatio = 1.0;

        /// <summary>
        /// Gets or sets the aspect-ratio of the pixel scales. A value less than 
        /// 1 will make the map streach upwards, and larger than 1 will make it smaller.
        /// </summary>
        /// <exception cref="ArgumentException">Throws an argument exception when value is 0 or less.</exception>
        public virtual double PixelAspectRatio
        {
            get { return pixelAspectRatio; }
            set
            {
                if (pixelAspectRatio <= 0)
                    throw new ArgumentException("Invalid Pixel Aspect Ratio");
                pixelAspectRatio = value;
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

        private Size size;

        /// <summary>
        /// Size of output map
        /// </summary>
        public virtual Size Size
        {
            get { return size; }
            set
            {
                size = value;

                this.ZoomToFit(desiredEnvelope ?? this.Envelope, false);
            }
        }

        private double minimumZoom;

        /// <summary>
        /// Minimum zoom amount allowed
        /// </summary>
        public virtual double MinimumZoom
        {
            get { return minimumZoom; }
            set
            {
                if (value < 0)
                    throw (new ArgumentException("Minimum zoom must be 0 or more"));
                minimumZoom = value;
                SetRenderRequiredForAllLayers();
            }
        }

        private double maximumZoom;
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
            get { return maximumZoom; }
            set
            {
                if (value <= 0)
                    throw (new ArgumentException("Maximum zoom must larger than 0"));
                maximumZoom = value;
                SetRenderRequiredForAllLayers();
            }
        }

        public virtual string Name
        {
            get { return name; }
            set { name = value; }
        }

        #endregion

        #region INotifyCollectionChange Members

        public virtual event NotifyCollectionChangedEventHandler CollectionChanged;
        public virtual event NotifyCollectionChangingEventHandler CollectionChanging;

        #endregion

        public object Clone()
        {
            Map clone = new Map(this.Size);
            clone.name = this.name;
            clone.Center = new Coordinate(this.Center);

            clone.minimumZoom = minimumZoom;
            clone.maximumZoom = maximumZoom;
            clone.desiredEnvelope = desiredEnvelope;
            clone.Zoom = Zoom;

            foreach(ILayer layer in layers)
            {
                clone.layers.Add((ILayer) layer.Clone());
            }
            return clone;
        }

        public override string ToString()
        {
            return (!string.IsNullOrEmpty(this.Name)) ? Name : base.ToString();
        }
    }
}