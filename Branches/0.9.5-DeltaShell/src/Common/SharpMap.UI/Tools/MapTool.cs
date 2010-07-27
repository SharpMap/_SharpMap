using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;
using System.Windows.Forms;
using log4net;
using NetTopologySuite.Extensions.Coverages;
using SharpMap.Converters.Geometries;
using SharpMap.Data.Providers;
using SharpMap.Extensions;
using SharpMap.Layers;
using SharpMap.UI.Forms;
using SharpMap.UI.Helpers;
using System.ComponentModel;
using DelftTools.Utils.Collections;

namespace SharpMap.UI.Tools
{
    /// <summary>
    /// Abstract baseclass for IMaptool implementations to interact with map
    /// </summary>
    public abstract class MapTool : IMapTool
    {
        private static readonly ILog log = LogManager.GetLogger(typeof (MapTool));

        private IMapControl mapControl;
        private bool isActive;


        public virtual IMapControl MapControl
        {
            get { return mapControl; }
            set { mapControl = value; }
        }

        public Map Map
        {
            get { return MapControl.Map; }
        }

        public string Name { get; set; }

        public MapTool()
        {
        }

        public MapTool(MapControl mapControl)
        {
            this.mapControl = mapControl;
        }

        public virtual void OnMouseDown(ICoordinate worldPosition, MouseEventArgs e)
        {
        }

        public virtual void OnMouseUp(ICoordinate worldPosition, MouseEventArgs e)
        {
        }

        public virtual void OnMouseWheel(ICoordinate worldPosition, MouseEventArgs e)
        {
        }

        public virtual void OnMouseMove(ICoordinate worldPosition, MouseEventArgs e)
        {
        }

        public virtual void OnMouseDoubleClick(object sender, MouseEventArgs e)
        {
        }

        public virtual void OnMouseHover(ICoordinate worldPosition, EventArgs e)
        {
        }

        public virtual void OnDragEnter(DragEventArgs e)
        {
        }

        public virtual void OnDragDrop(DragEventArgs e)
        {
        }

        public virtual void OnKeyDown(KeyEventArgs e)
        {
        }

        public virtual void OnKeyUp(KeyEventArgs e)
        {
        }

        public virtual void OnPaint(PaintEventArgs e)
        {
        }

        public virtual void OnMapLayerRendered(Graphics g, ILayer layer)
        {
        }

        public virtual void OnMapPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
        }

        public virtual void OnMapCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
        }

        public virtual void OnBeforeContextMenu(ContextMenuStrip menu, ICoordinate worldPosition)
        {
        }


        /// <summary>
        /// Returns true if tool is currently busy (working).
        /// </summary>
        public virtual bool IsBusy { get; set; }

        //{
        //    get { return false; }
        //}

        /// <summary>
        /// Returns true if tool is currently busy (working).
        /// </summary>
        public virtual bool Enabled
        {
            get { return true; }
        }

        /// <summary>
        /// Use this property to enable or disable tool.
        /// </summary>
        public virtual bool IsActive
        {
            get
            {
                if (AlwaysActive)
                {
                    return true;
                }

                return isActive;
            }
            set
            {
                if (AlwaysActive)
                {
                    throw new InvalidOperationException("Use Execute() method instead of Activating AlwaysActive tools!");
                }
                isActive = value;
            }
        }

        public virtual bool AlwaysActive
        {
            get { return false; }
        }

        public virtual void Execute()
        {
        }

        public bool IsCtrlPressed
        {
            get { return (Control.ModifierKeys & Keys.Control) == Keys.Control; }
        }

        public bool IsAltPressed
        {
            get { return (Control.ModifierKeys & Keys.Alt) == Keys.Alt; }
        }

        public bool IsShiftPressed
        {
            get { return (Control.ModifierKeys & Keys.Shift) == Keys.Shift; }
        }

        // TODO: to be moved / rmoved
        /// <summary>
        /// Tool may be used only for 1 layer, otherwise null.
        /// </summary>
        public ILayer Layer { get; set; }

        public IFeatureProvider FeatureProvider
        {
            get { return Layer.DataSource; }
        }

        public virtual void ActiveToolChanged(IMapTool newTool)
        {
        }

        public ILayer GetLayerByFeature(IFeature feature)
        {
            return Map.GetLayerByFeature(feature);
        }

        public IFeatureProvider GetFeatureProviderByFeature(IFeature feature)
        {
            ILayer featureLayer = Map.GetLayerByFeature(feature);
            if (featureLayer != null && featureLayer is VectorLayer)
            {
                return featureLayer.DataSource;
            }

            return null;
        }

        public IEnvelope GetEnvelope(ICoordinate worldPos, float buffer)
        {
            return MapControlHelper.GetEnvelope(worldPos, buffer);
        }

        public double ImageToWorld(float imageSize)
        {
            return MapControlHelper.ImageToWorld(Map, imageSize);
        }

        public IFeature FindNearestFeature(ICoordinate worldPos, float limit, out ILayer outLayer,
                                           Func<ILayer, bool> condition)
        {
            IFeature nearestFeature = null;
            outLayer = null;
            // Since we are only interested in one geometry object start with the topmost trackersLayer and stop
            // searching the lower layers when an object is found.

            foreach (ILayer mapLayer in MapHelper.GetAllMapLayers(Map.Layers, false))
            {
                if (mapLayer is VectorLayer)
                {
                    var vectorLayer = mapLayer as VectorLayer;
                    IEnvelope envelope;
                    float localLimit = limit;

                    if ((!vectorLayer.IsSelectable) ||  ((null != condition) && (!condition(vectorLayer))))
                    {
                        continue;
                    }
                    // Adjust the marge limit for Layers with a symbol style and if the size of the symbol exceeds
                    // the minimum limit. Ignore layers with custom renderers
                    if ((vectorLayer.Style.Symbol != null) && (0 == vectorLayer.CustomRenderers.Count))
                    {
                        ICoordinate size = MapControlHelper.ImageToWorld(MapControl.Map, vectorLayer.Style.Symbol.Width,
                                                                         vectorLayer.Style.Symbol.Height);
                        if ((size.X > localLimit) || (size.Y > localLimit))
                        {
                            envelope = MapControlHelper.GetEnvelope(worldPos, size.X, size.Y);
                            localLimit = (float) Math.Max(envelope.Width, envelope.Height);
                        }
                        else
                        {
                            envelope = GetEnvelope(worldPos, localLimit);
                        }
                    }
                    else
                    {
                        envelope = GetEnvelope(worldPos, localLimit);
                    }

                    IFeatureProvider featureProvider = vectorLayer.DataSource;
                    if (null != featureProvider)
                    {
                        // Get features in the envelope
                        IList objectsAt;

                        if (vectorLayer.CustomRenderers.Count > 0)
                        {
                            objectsAt = vectorLayer.CustomRenderers[0].GetFeatures(envelope, vectorLayer);
                        }
                        else
                        {
                            objectsAt = featureProvider.GetFeatures(envelope);
                        }
                        // Mousedown at new position
                        if ((null != objectsAt) && (objectsAt.Count > 0))
                        {
                            double localDistance;
                            IFeature feature = FindNearestFeature(vectorLayer, objectsAt, worldPos, localLimit,
                                                                  out localDistance);
                            if (null != feature)
                            {
                                nearestFeature = feature;
                                outLayer = vectorLayer;
                                break;
                            }
                        }
                    }
                }
                else if (mapLayer is IRegularGridCoverageLayer)
                {
                    try
                    {
                        IRegularGridCoverageLayer regularGridCoverageLayer = (IRegularGridCoverageLayer)mapLayer;
                        IRegularGridCoverage regularGridCoverage = (IRegularGridCoverage)regularGridCoverageLayer.Coverage;

                        nearestFeature = regularGridCoverage.GetRegularGridCoverageCellAtPosition(worldPos.X, worldPos.Y);

                        outLayer = regularGridCoverageLayer;
                    }
                    catch (Exception)
                    {
                        // GetCoordinateAtPosition will throw exception if x, y is not within the coverage
                    }
                }
            }

            return nearestFeature;
        }

        /// <summary>
        /// Returns the next feature at worldPos. 
        /// </summary>
        /// <param name="worldPos"></param>
        /// <param name="limit"></param>
        /// <param name="outLayer"></param>
        /// the layer containing the next feature; null if no next feature is found.
        /// <param name="feature"></param>
        /// <param name="condition"></param>
        /// <returns>the next feature at worldPos, null if there is no next feature.</returns>
        public IFeature GetNextFeatureAtPosition(ICoordinate worldPos, float limit, out Layer outLayer, IFeature feature,
                                                 Func<ILayer, bool> condition)
        {
            IEnvelope envelope = GetEnvelope(worldPos, limit);
            IFeature nextFeature = null;
            bool featureFound = false;
            outLayer = null;

            foreach (ILayer mapLayer in MapHelper.GetAllMapLayers(Map.Layers, false))
            {
                var vectorLayer = mapLayer as VectorLayer;
                IPoint point = GeometryFactory.CreatePoint(worldPos);
                if (vectorLayer == null || !vectorLayer.IsSelectable)
                    continue;
                if ((null != condition) && (!condition(vectorLayer)))
                    continue;

                FeatureCollection provider = vectorLayer.DataSource as FeatureCollection;
                // this will skip shapefile layers
                if (null != provider)
                {
                    IList objectsAt = provider.GetFeatures(envelope);
                    foreach (IFeature featureAt in objectsAt)
                    {
                        // GetFeatures(envelope) uses the geometry bounds; this results in more 
                        // geometries than we actually are interested in (especially linestrings and polygons).
                        double distance = featureAt.Geometry.Distance(point);

                        if (distance >= limit)
                            continue;
                        if (featureFound)
                        {
                            nextFeature = featureAt;
                            outLayer = vectorLayer;
                            return nextFeature;
                        }
                        if (featureAt == feature)
                        {
                            featureFound = true;
                            continue;
                        }
                        if (null != nextFeature)
                            continue;
                        // If feature is last in collections objectsAt nextfeature is first
                        nextFeature = featureAt;
                        outLayer = vectorLayer;
                    }
                }
            }
            return nextFeature;
        }

        public IGeometry FindNearestGeometry(ICoordinate worldPos, float limit, out ILayer nearestLayer,
                                             Func<ILayer, bool> condition)
        {
            IFeature nearestFeature = FindNearestFeature(worldPos, limit, out nearestLayer, condition);
            if (null != nearestFeature)
                return nearestFeature.Geometry;

            return null;
        }

        /// <summary>
        /// Find the nearest feature to worldPos out of a collection of candidates. If there is no geometry
        /// with a distance less than limit null is returned.
        /// </summary>
        /// <param name="candidates"></param>
        /// <param name="worldPos"></param>
        /// <param name="limit"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        private static IFeature FindNearestFeature(VectorLayer vectorLayer, IEnumerable candidates, ICoordinate worldPos,
                                                   float limit, out double distance)
        {
            IPoint point = GeometryFactory.CreatePoint(worldPos.X, worldPos.Y);

            IFeature current = null;
            distance = double.MaxValue;
            foreach (IFeature feature in candidates)
            {
                IGeometry geometry;
                if (vectorLayer.CustomRenderers.Count > 0)
                {
                    geometry = vectorLayer.CustomRenderers[0].GetRenderedFeatureGeometry(feature, vectorLayer);
                }
                else
                {
                    geometry = feature.Geometry;
                }
                double localDistance = geometry.Distance(point);
                if ((localDistance < distance) && (localDistance < limit))
                {
                    current = feature;
                    distance = localDistance;
                }
            }
            return current;
        }

        #region toolDrawing

        /// <summary>
        /// Stores a clone of the map backgroud during dragging draw operations
        /// </summary>
        private Bitmap backGroundImage;

        /// <summary>
        /// Stores a (empty) bitmap during dragging draw operations; only purpose is to prevent creation of large bitmap
        /// for every drawing operation
        /// </summary>
        private Bitmap drawingBitmap;

        private bool dragging;

        public virtual void StartDrawing()
        {
            if (Map.Image.PixelFormat == PixelFormat.Undefined)
            {
                log.Error("drawCache is broken ...");
            }
            backGroundImage = (Bitmap) Map.Image.Clone();
            drawingBitmap = new Bitmap(Map.Image.Width, Map.Image.Height);
            dragging = true;
        }

        public virtual void Render(Graphics graphics, Map mapBox)
        {
        }

        public virtual void OnDraw(Graphics graphics)
        {
        }

        public virtual void DoDrawing(bool drawTools)
        {
            if (!dragging)
                return;
            Graphics graphics = Graphics.FromImage(drawingBitmap);

            // use transform from map; this enables directly calling ILayer.OnRender
            graphics.Transform = Map.MapTransform.Clone();
            graphics.Clear(Color.Transparent);
            graphics.PageUnit = GraphicsUnit.Pixel;

            graphics.Clear(MapControl.BackColor);
            graphics.DrawImageUnscaled(backGroundImage, 0, 0);

            if (drawTools)
            {
                foreach (IMapTool tool in MapControl.Tools)
                {
                    if (tool.IsActive)
                        tool.Render(graphics, Map);
                }
            }
            OnDraw(graphics);

            Graphics graphicsMap = MapControl.CreateGraphics();
            graphicsMap.DrawImageUnscaled(drawingBitmap, 0, 0);

            graphicsMap.Dispose();
            graphics.Dispose();
        }

        public virtual void StopDrawing()
        {
            if (backGroundImage != null)
            {
                backGroundImage.Dispose();
                backGroundImage = null;
            }
            if (drawingBitmap != null)
            {
                drawingBitmap.Dispose();
                drawingBitmap = null;
            }
            dragging = false;
        }

        public virtual void Cancel()
        {
        }

        #endregion
    }
}