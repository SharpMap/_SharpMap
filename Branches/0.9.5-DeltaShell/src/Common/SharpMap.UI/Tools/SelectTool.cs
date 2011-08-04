using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Linq.Expressions;
using System.Windows.Forms;
using GeoAPI.Geometries;
using log4net;
using SharpMap.Converters.Geometries;
using SharpMap.Data.Providers;
using SharpMap.Extensions;
using SharpMap.Layers;
using SharpMap.Rendering.Thematics;
using SharpMap.Styles;
using SharpMap.UI.Editors;
using SharpMap.UI.Forms;
using SharpMap.UI.Helpers;
using GeoAPI.Extensions.Feature;
using System.ComponentModel;
using DelftTools.Utils.Collections;

namespace SharpMap.UI.Tools
{
    public enum MultiSelectionMode
    {
        Rectangle = 0,
        Lasso
    }

    /// <summary>
    /// SelectTool enables users to select features in the map
    /// The current implementation supports:
    /// - single selection feature by click on feature
    /// - multiple selection of feature by dragging a rectangle
    /// - adding features to the selection (KeyExtendSelection; normally the SHIFT key)
    /// - toggling selection of features (KeyToggleSelection; normally the CONTROL key)
    ///    if featues is not in selection it is added to selection
    ///    if feature is in selection it is removed from selection
    /// - Selection is visible to the user via trackers. Features with an IPoint geometry have 1 
    ///   tracker, based on ILineString and IPolygon have a tracker for each coordinate
    /// - Trackers can have focus. 
    ///   If a trackers has focus is visible to the user via another symbol (or same symbol in other color)
    ///   A tracker that has the focus is the tracker leading during special operation such as moving. 
    ///   For single selection a feature with an IPoint geometry automatically get the focus to the 
    ///   only tracker
    /// - Multiple trackers with focus
    /// - adding focus trackers (KeyExtendSelection; normally the SHIFT key)
    /// - toggling focus trackers (KeyToggleSelection; normally the CONTROL key)
    /// - Selection cycling, When multiple features overlap clicking on a selected feature will
    ///   result in the selection of the next feature. Compare behavior in Sobek Netter.
    /// 
    /// TODO
    /// - functionality reasonably ok, but TOO complex : refactor using tests
    /// - Selection cycling can be improved:
    ///     - for a ILineString the focus tracker is not set initially which can be set in the second
    ///       click. Thus a ILineString (and IPolygon) can eat a click
    ///     - if feature must be taken into account by selection cycling should be an option
    ///       (topology rule?)
    /// </summary>
    public class SelectTool : MapTool
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(SelectTool));
        public virtual event FeatureEditorCreationEventHandler FeatureEditorCreation;
        public IList<IFeatureEditor> FeatureEditors { get; private set; } // TODO: refactor it, editors are used only to work with trackers, decouple trackers from editors
        private readonly Collection<ITrackerFeature> trackers = new Collection<ITrackerFeature>();
        public MultiSelectionMode MultiSelectionMode { get; set; }
        private ICoordinateConverter CoordinateConverter { get; set; }

        /// <summary>
        /// Current layer where features are being selected (branch, nodes, etc.)
        /// </summary>
        private VectorLayer TrackingLayer // will be TrackingLayer containing tracking geometries
        {
            get { return trackingLayer; }
        }

        public IList<int> SelectedTrackerIndices
        {
            get
            {
                List<int> indices = new List<int>();
                return 1 == FeatureEditors.Count ? FeatureEditors[0].GetFocusedTrackerIndices() : indices;
            }
        }

        public bool KeyToggleSelection
        {
            get { return ((Control.ModifierKeys & Keys.Control) == Keys.Control); }
        }
        public bool KeyExtendSelection
        {
            get { return ((Control.ModifierKeys & Keys.Shift) == Keys.Shift); }
        }

        public SelectTool()
        {
            orgClickTime = DateTime.Now;
            FeatureEditors = new List<IFeatureEditor>();
            Name = "Select";

            trackingLayer.Name = "trackers";
            FeatureCollection trackerProvider = new FeatureCollection {Features = trackers};

            trackingLayer.DataSource = trackerProvider;

            CustomTheme iTheme = new CustomTheme(GetTrackerStyle);
            trackingLayer.Theme = iTheme;
        }

        private bool IsMultiSelect { get; set; }

        public override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Render(e.Graphics, MapControl.Map);
        }

        public override void Render(Graphics graphics, Map map)
        {
            // Render the selectionLayer and trackingLayer
            // Bypass ILayer.Render and call OnRender directly; this is more efficient
            foreach (var tracker in trackers)
            {
                if (null != tracker.FeatureEditor.SourceFeature)
                {
                    // todo optimize this; only necessary when map extent has changed.
                    tracker.FeatureEditor.UpdateTracker(tracker.FeatureEditor.SourceFeature.Geometry);
                }
            }
            trackingLayer.OnRender(graphics, map);
        }


        public ITrackerFeature GetTrackerAtCoordinate(ICoordinate worldPos)
        {
            ITrackerFeature trackerFeature = null;
            for (int i=0; i<FeatureEditors.Count; i++)
            {
                trackerFeature = FeatureEditors[i].GetTrackerAtCoordinate(worldPos);
                if (null != trackerFeature)
                    break;
            }
            return trackerFeature;
        }

        private ICoordinate orgMouseDownLocation;
        private DateTime orgClickTime;
        private bool clickOnExistingSelection;
        private void SetClickOnExistingSelection(bool set, ICoordinate worldPosition)
        {
            clickOnExistingSelection = set;
            if (clickOnExistingSelection) 
            {
                orgMouseDownLocation = (ICoordinate)worldPosition.Clone();
            }
            else
            {
                orgMouseDownLocation = null;
            }
        }

        private IFeatureEditor GetActiveMutator (IFeature feature)
        {
            for (int i=0; i<FeatureEditors.Count; i++)
            {
                if (FeatureEditors[i].SourceFeature == feature)
                    return FeatureEditors[i];
            }
            return null;
        }
        
        public override void OnMouseDown(ICoordinate worldPosition, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            var oldSelectedTrackerIndicesCount = SelectedTrackerIndices.Count;
            var oldTrackerFeatureCount = trackers.Count;

            IsBusy = true;
            ILayer selectedLayer;
            mouseDownLocation = worldPosition;

            // Check first if an object is already selected and if the mousedown has occured at this object.
            ITrackerFeature trackerFeature = GetTrackerAtCoordinate(worldPosition);
            if (FeatureEditors.Count > 1)
            {
                // hack: if multiple selection toggle/select complete feature
                trackerFeature = null;
            }

            SetClickOnExistingSelection(false, null);

            if (null != trackerFeature)
            {
                if (1 == FeatureEditors.Count)
                {
                    SetClickOnExistingSelection(true, worldPosition);
                    FocusTracker(trackerFeature);
                    MapControl.Refresh();

                }
                return;
            }
            // single selection. Find the nearest geometry and give 
            float limit = (float)MapControlHelper.ImageToWorld(Map, 4);
            IFeature nearest = FindNearestFeature(worldPosition, limit, out selectedLayer, ol => ol.IsVisible);
            if (null != nearest)
            {
                // Create or add a new FeatureEditor
                if (FeatureEditors.Count > 0)
                {
                    IFeatureEditor currentMutator = GetActiveMutator(nearest);
                    if (KeyExtendSelection)
                    {
                        if (null == currentMutator)
                        {
                            // not in selection; add
                            AddSelection(selectedLayer, nearest, -1, true);
                        } // else possibly set default focus tracker
                    }
                    else if (KeyToggleSelection)
                    {
                        if (null == currentMutator)
                        {
                            // not in selection; add
                            AddSelection(selectedLayer, nearest, -1, true);
                        }
                        else
                        {
                            // in selection; remove
                            RemoveSelection(nearest);
                        }
                    }
                    else
                    {
                        // no special key processing; handle as a single select.
                        Clear();
                        if (!StartSelection(selectedLayer, nearest, -1))
                        {
                            StartMultiSelect();
                        }
                        //AddSelection(selectedLayer, nearest, -1);
                    }
                }
                else
                {
                    if (!StartSelection(selectedLayer, nearest, -1))
                    {
                        StartMultiSelect();
                    }
                    //AddSelection(selectedLayer, nearest, -1);
                }
            }
            else
            {
                // We didn't find an object at the position of the mouse button -> start a multiple select
                if (!KeyExtendSelection)
                {
                    // we are not extending the current selection
                    Clear();
                }
                if (e.Button == MouseButtons.Left)
                //if (IsActive)
                {
                    StartMultiSelect();
                }
            }

            if ((oldSelectedTrackerIndicesCount != SelectedTrackerIndices.Count 
                || oldTrackerFeatureCount != trackers.Count) && trackingLayer.DataSource.Features.Count != 0)
            {
                MapControl.Refresh();
            }
        }
        private void StartMultiSelect()
        {
            IsMultiSelect = true;
            selectPoints.Clear();
            UpdateMultiSelection(mouseDownLocation);
            StartDrawing();
        }
        private void StopMultiSelect()
        {
            IsMultiSelect = false;
            StopDrawing();
        }

        /// <summary>
        /// Returns styles used by tracker features.
        /// </summary>
        /// <param name="feature"></param>
        /// <returns></returns>
        private static VectorStyle GetTrackerStyle(IFeature feature)
        {
            var trackerFeature = (TrackerFeature) feature;

            VectorStyle style;
            
            // styles are stored in the cache for performance reasons
            lock(stylesCache)
            {
                if (!stylesCache.ContainsKey(trackerFeature.Bitmap))
                {
                    style = new VectorStyle {Symbol = trackerFeature.Bitmap};
                    stylesCache[trackerFeature.Bitmap] = style;
                }
                else
                {
                    style = stylesCache[trackerFeature.Bitmap];
                }
            }

            return style;
        }

        static IDictionary<Bitmap, VectorStyle> stylesCache = new Dictionary<Bitmap, VectorStyle>();

        public void Clear()
        {
            FeatureEditors.Clear();
            if (trackingLayer.DataSource.GetFeatureCount() <= 0) 
                return;
            trackers.Clear();
            trackingLayer.RenderRequired = true;
            UpdateMapControlSelection();
        }


        private void SynchronizeTrackers()
        {
            trackers.Clear();
            for (int i=0; i<FeatureEditors.Count; i++)
            {
                foreach (ITrackerFeature trackerFeature in FeatureEditors[i].GetTrackers())
                {
                    trackers.Add(trackerFeature);
                }
            }
            trackingLayer.RenderRequired = true;
            
        }

        // TODO, HACK: what SelectTool has to do with FeatureEditor? Refactor it.
        public IFeatureEditor GetFeatureEditor(ILayer layer, IFeature feature)
        {
            try
            {
                IFeatureEditor featureEditor = null;

                if (null != FeatureEditorCreation)
                {
                    // allow custom feature editor creation
                    featureEditor = FeatureEditorCreation(layer, feature,
                                                          (layer is VectorLayer) ? ((VectorLayer)layer).Style : null);
                }
                if (null == featureEditor)
                {
                    // no custom feature editor; fall back to default editors.
                    featureEditor = FeatureEditorFactory.Create(CoordinateConverter, layer, feature,
                                                                (layer is VectorLayer)
                                                                    ? ((VectorLayer)layer).Style
                                                                    : null);
                }
                return featureEditor;
            }
            catch (Exception exception)
            {
                log.Error("Error creating feature editor: " + exception.Message);
                return null;
            }
        }

        private bool StartSelection(ILayer layer, IFeature feature, int trackerIndex)
        {
            IFeatureEditor featureEditor = GetFeatureEditor(layer, feature);
            if (null == featureEditor)
                return false;
            if (featureEditor.AllowSingleClickAndMove())
            {
                // do not yet select, but allow MltiSelect
                FeatureEditors.Add(featureEditor);
                SynchronizeTrackers();
                UpdateMapControlSelection();
                return true;
            }
            return false;
        }

        public void AddSelection(ILayer layer, IFeature feature)
        {
            AddSelection(layer, feature, 0, true);
        }

        public void AddSelection(ILayer layer, IFeature feature, int trackerIndex, bool synchronizeUI)
        {
            if (!layer.Enabled)
            {
                return;
            }
            IFeatureEditor featureEditor = GetFeatureEditor(layer, feature);
            if (null == featureEditor) 
                return;
            FeatureEditors.Add(featureEditor);
            SynchronizeTrackers();
            if (synchronizeUI)
            {
                UpdateMapControlSelection();
            }
        }

        public void UpdateSelection(IGeometry geometry) // HACK: select tool must select features, not edit them
        {
            FeatureEditors[0].SourceFeature.Geometry = geometry;
        }

        private void RemoveSelection(IFeature feature)
        {
            for (int i=0; i<FeatureEditors.Count; i++)
            {
                if (FeatureEditors[i].SourceFeature == feature)
                {
                    FeatureEditors.RemoveAt(i);
                    break;
                }
            }
            SynchronizeTrackers();
            UpdateMapControlSelection();
        }


        /// <summary>
        /// Sets the selected object in the selectTool. SetSelection supports also the toggling/extending the 
        /// selected trackers.
        /// </summary>
        /// <param name="feature"></param>
        /// <param name="featureLayer"></param>
        /// <param name="trackerIndex"></param>
        /// <returns>A clone of the original object.</returns>
        /// special cases 
        /// feature is ILineString or IPolygon and trackerIndex != 1 : user clicked an already selected 
        /// features -> only selected tracker changes.
        private void SetSelection(IFeature feature, ILayer featureLayer, int trackerIndex)
        {
            if (null != feature)
            {
                // store selected trackers
                IList<int> featureTrackers = new List<int>();
                for (int i = 0; i < TrackingLayer.DataSource.Features.Count; i++)
                {
                    TrackerFeature trackerFeature = (TrackerFeature) TrackingLayer.DataSource.Features[i];
                    if (trackerFeature == feature)
                    {
                        featureTrackers.Add(i);
                    }
                }
                // store selected objects 
                AddSelection(featureLayer, feature, trackerIndex, true);
            }
        }

        private void FocusTracker(ITrackerFeature trackFeature)
        {
            if (null == trackFeature)
                return;

            if (!((KeyToggleSelection) || (KeyExtendSelection)))
            {
                for (int i=0; i<FeatureEditors.Count; i++)
                {
                    foreach (ITrackerFeature tf in FeatureEditors[i].GetTrackers())
                    {
                        FeatureEditors[i].Select(tf, false);
                    }
                }
            }
            for (int i = 0; i < FeatureEditors.Count; i++)
            {
                foreach (TrackerFeature tf in FeatureEditors[i].GetTrackers())
                {
                    if (tf == trackFeature)
                    {
                        if (KeyToggleSelection)
                        {
                            FeatureEditors[i].Select(trackFeature, !trackFeature.Selected);
                        }
                        else
                        {
                            FeatureEditors[i].Select(trackFeature, true);
                        }
                    }
                }
            }
        }

        private List<PointF> selectPoints = new List<PointF>();
        //private bool lassoSelect = false;

        private void UpdateMultiSelection(ICoordinate worldPosition)
        {
            if (MultiSelectionMode == MultiSelectionMode.Lasso)
            {
                selectPoints.Add(Map.WorldToImage(worldPosition));
            }
            else
            {
                WORLDPOSITION = worldPosition;
            }
        }

        private IPolygon CreatePolygon(double left, double top, double right, double bottom)
        {
            var vertices = new List<ICoordinate>
                                   {
                                       GeometryFactory.CreateCoordinate(left, bottom),
                                       GeometryFactory.CreateCoordinate(right, bottom),
                                       GeometryFactory.CreateCoordinate(right, top),
                                       GeometryFactory.CreateCoordinate(left, top)
                                   };
            vertices.Add((ICoordinate)vertices[0].Clone());
            ILinearRing newLinearRing = GeometryFactory.CreateLinearRing(vertices.ToArray());
            return GeometryFactory.CreatePolygon(newLinearRing, null);

        }

        private IPolygon CreateSelectionPolygon(ICoordinate worldPosition)
        {
            if (MultiSelectionMode == MultiSelectionMode.Rectangle)
            {
                if (0 == Math.Abs(mouseDownLocation.X - worldPosition.X))
                {
                    return null;
                }
                if (0 == Math.Abs(mouseDownLocation.Y - worldPosition.Y))
                {
                    return null;
                }
                return CreatePolygon(Math.Min(mouseDownLocation.X, worldPosition.X),
                                             Math.Max(mouseDownLocation.Y, worldPosition.Y),
                                             Math.Max(mouseDownLocation.X, worldPosition.X),
                                             Math.Min(mouseDownLocation.Y, worldPosition.Y));
            }
            var vertices = new List<ICoordinate>();

            foreach (var point in selectPoints)
            {
                vertices.Add(Map.ImageToWorld(point)); 
            }
            if (vertices.Count == 1)
            {
                // too few points to create a polygon
                return null;
            }
            vertices.Add((ICoordinate)worldPosition.Clone());
            vertices.Add((ICoordinate)vertices[0].Clone());
            ILinearRing newLinearRing = GeometryFactory.CreateLinearRing(vertices.ToArray());
            return GeometryFactory.CreatePolygon(newLinearRing, null);
        }

        private ICoordinate mouseDownLocation; // TODO: remove me
        private ICoordinate WORLDPOSITION;
        public override void OnDraw(Graphics graphics)
        {
            if (MultiSelectionMode == MultiSelectionMode.Lasso)
            {
                GraphicsHelper.DrawSelectionLasso(graphics, KeyExtendSelection ? Color.Magenta : Color.DeepSkyBlue, selectPoints.ToArray());
            }
            else
            {
                ICoordinate coordinate1 = GeometryFactory.CreateCoordinate(mouseDownLocation.X, mouseDownLocation.Y);
                ICoordinate coordinate2 = GeometryFactory.CreateCoordinate(WORLDPOSITION.X, WORLDPOSITION.Y);
                PointF point1 = Map.WorldToImage(coordinate1);
                PointF point2 = Map.WorldToImage(coordinate2);
                GraphicsHelper.DrawSelectionRectangle(graphics, KeyExtendSelection ? Color.Magenta : Color.DeepSkyBlue, point1, point2);
            }
        }

        public override void OnMouseMove(ICoordinate worldPosition, MouseEventArgs e)
        {
            if (IsMultiSelect)
            {
                //WORLDPOSITION = worldPosition;
                UpdateMultiSelection(worldPosition);
                DoDrawing(false);
                return;
            }

            Cursor cursor = null;
            for (int i=0; i<FeatureEditors.Count; i++)
            {
                ITrackerFeature trackerFeature = FeatureEditors[i].GetTrackerAtCoordinate(worldPosition);
                if (null != trackerFeature)
                {
                    cursor = FeatureEditors[i].GetCursor(trackerFeature);
                }
            }
            if (null == cursor)
            {
                cursor = Cursors.Default;
            }
            
            MapControl.Cursor = cursor;
        }
        private void UpdateMapControlSelection()
        {
            IList<IFeature> selectedFeatures = new List<IFeature>();
            for (int i = 0; i < FeatureEditors.Count; i++)
            {
                selectedFeatures.Add(FeatureEditors[i].SourceFeature);
            }

            if(selectedFeatures.Count == 0 && MapControl.SelectedFeatures.Count == 0)
            {
                return; // no refresh
            }

            MapControl.SelectedFeatures = selectedFeatures;

            if(SelectionChanged != null)
            {
                SelectionChanged(this, null);
            }
        }
        public override void OnMouseDoubleClick(object sender, MouseEventArgs e)
        {
            orgMouseDownLocation = null;
        }

        public override void OnMouseUp(ICoordinate worldPosition, MouseEventArgs e)
        {
            if (IsMultiSelect)
            {
                StopMultiSelect();
                List<IFeature> selectedFeatures = null;
                if (!KeyExtendSelection)
                {
                    selectedFeatures = new List<IFeature>(FeatureEditors.Select(fe => fe.SourceFeature).ToArray());
                    Clear();
                }
                IPolygon selectionPolygon = CreateSelectionPolygon(worldPosition);
                if (null != selectionPolygon)
                {
                    foreach (ILayer layer in MapHelper.GetAllMapLayers(Map.Layers, false))
                    {
                        if ((!layer.ReadOnly) && (layer is VectorLayer))
                        {
                            // do not use the maptool provider but the datasource of each layer.
                            VectorLayer vectorLayer = (VectorLayer)layer;
                            if (vectorLayer.IsVisible)
                            {
                                IList multiFeatures = vectorLayer.DataSource.GetFeatures(selectionPolygon);
                                for (int i = 0; i < multiFeatures.Count; i++)
                                {
                                    IFeature feature = (IFeature) multiFeatures[i];
                                    if ((null != selectedFeatures) && (selectedFeatures.Contains(feature)))
                                    {
                                        continue;
                                    }
                                    AddSelection(vectorLayer, feature, -1, false);
                                }
                            }
                        }
                    }
                }
                else
                {
                    // if mouse hasn't moved handle as single select. A normal multi select uses the envelope
                    // of the geometry and this has as result that unwanted features will be selected.
                    ILayer selectedLayer;
                    float limit = (float)MapControlHelper.ImageToWorld(Map, 4);
                    IFeature nearest = FindNearestFeature(worldPosition, limit, out selectedLayer, ol => ol.IsVisible);
                    if (null != nearest) //&& (selectedLayer.IsVisible))
                        AddSelection(selectedLayer, nearest, -1, false);
                }
                // synchronize with map selection, possible check if selection is already set; do not remove
                UpdateMapControlSelection();
                //MapControl.Refresh();
                //IsMultiSelect = false;
            }
            else
            {
                if ((null != orgMouseDownLocation) && (orgMouseDownLocation.X == worldPosition.X) &&
                    (orgMouseDownLocation.Y == worldPosition.Y) && (e.Button == MouseButtons.Left))
                {
                    // check if mouse was pressed at a selected object without moving the mouse. The default behaviour 
                    // should be to select 'the next' object
                    TimeSpan timeSpan = DateTime.Now - orgClickTime;
                    int dc = SystemInformation.DoubleClickTime;
                    if (dc < timeSpan.TotalMilliseconds)
                    {
                        if (1 == FeatureEditors.Count)
                        {
                            // check if selection exists; could be toggled
                            Layer outLayer;
                            IFeature nextFeature = GetNextFeatureAtPosition(worldPosition,
                                // set limit from 4 to 10: TOOLS-1499
                                (float)MapControlHelper.ImageToWorld(Map, 10),
                                out outLayer,
                                FeatureEditors[0].SourceFeature,
                                ol => ol.IsVisible);
                            if (null != nextFeature)
                            {
                                Clear();
                                SetSelection(nextFeature, outLayer, 0); //-1 for ILineString
                                //MapControl.Refresh();
                            }
                        }
                    }
                }
                UpdateMapControlSelection();
            }
            IsBusy = false;
            orgClickTime = DateTime.Now;

            //for (int i=0; i<FeatureEditors.Count; i++)
            //{
            //    IFeatureEditor featureEditor = FeatureEditors[i];

            //    if (featureEditor.Layer.CustomRenderers.Count > 0)
            //    {
            //        // todo move to IFeatureEditor
            //        featureEditor.Layer.CustomRenderers[0].
            //            UpdateRenderedFeatureGeometry(FeatureEditors[i].SourceFeature, FeatureEditors[i].Layer);
            //        IGeometry g = featureEditor.Layer.CustomRenderers[0].
            //            GetRenderedFeatureGeometry(FeatureEditors[i].SourceFeature, FeatureEditors[i].Layer);
            //        FeatureEditors[i].UpdateTracker(g);
            //    }
            //    //featureEditor.Stop();
            //}
        }

        //public override bool IsBusy
        //{
        //    get { return isBusy; }
        //}

        readonly VectorLayer trackingLayer = new VectorLayer(String.Empty);

        public override IMapControl MapControl
        {
            get { return base.MapControl; }
            set 
            { 
                base.MapControl = value;
                trackingLayer.Map = MapControl.Map;
                CoordinateConverter = new CoordinateConverter(MapControl);
            }
        }

        /// <summary>
        /// Selects the given feature on the map. Will search all layers for the feature.
        /// </summary>
        /// <param name="featureToSelect">The feature to select on the map.</param>
        public bool Select(IFeature featureToSelect)
        {
            if (null == featureToSelect)
            {
                Clear();
                return false;
            }
            // Find the layer that this feature is on
            ILayer foundLayer = MapControl.Map.GetLayerByFeature(featureToSelect);
            if (foundLayer != null && foundLayer is VectorLayer)
            {
                // Select the feature
                Select(foundLayer, featureToSelect, -1);
                return true;
            }

            return false;
        }

        public void Select(ILayer vectorLayer, IFeature feature, int trackerIndex)
        {
            if(IsBusy)
            {
                return;
            }

            Clear();
            SetSelection(feature, vectorLayer, trackerIndex);
            UpdateMapControlSelection();
        }

        /// <summary>
        /// Handles changes to the map (or bubbled up from ITheme, ILayer) properties. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public override void OnMapPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is ILayer)
            {
                if (e.PropertyName == "Enabled")
                {
                    // If a layer is enabled of disables and features of the layer are selected 
                    // the selection is cleared. Another solution is to remove only features of layer 
                    // from the selection, but this simple and effective.
                    ILayer layer = (ILayer)sender;
                    if (layer is LayerGroup)
                    {
                        LayerGroup layerGroup = (LayerGroup) layer;
                        foreach (ILayer layerGroupLayer in layerGroup.Layers)
                        {
                            HandleLayerStatusChanged(layerGroupLayer);
                        }
                    }
                    else
                    {
                        HandleLayerStatusChanged(layer);
                    }
                }
            }
        }
        private void HandleLayerStatusChanged(ILayer layer)
        {
            foreach (ITrackerFeature trackerFeature in trackers)
            {
                if (layer != trackerFeature.FeatureEditor.Layer)
                    continue;
                Clear();
                return;
            }
        }
        public override void OnMapCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Remove:
                    {
                        if (sender is Map)
                        {
                            ILayer layer = (ILayer)e.Item;
                            if (layer is LayerGroup)
                            {
                                LayerGroup layerGroup = (LayerGroup)layer;
                                foreach (ILayer layerGroupLayer in layerGroup.Layers)
                                {
                                    HandleLayerStatusChanged(layerGroupLayer);
                                }
                            }
                            else
                            {
                                HandleLayerStatusChanged(layer);
                            }
                        }
                        break;
                    }
                case NotifyCollectionChangedAction.Replace:
                    throw new NotImplementedException();
            }
        }
        /// <summary>
        /// todo add cancel method to IMapTool 
        /// todo mousedown clears selection -> complex selection -> start multi select -> cancel -> original selection lost
        /// </summary>
        public override void Cancel()
        {
            if (IsBusy)
            {
                if (IsMultiSelect)
                {
                    StopMultiSelect();
                }
                IsBusy = false;
            }
            Clear();
        }

        public event EventHandler SelectionChanged;

        public override bool IsActive
        {
            get
            {
                return base.IsActive;
            }
            set
            {
                base.IsActive = value;
                if (false == IsActive)
                {
                    MultiSelectionMode = MultiSelectionMode.Rectangle;
                }
            }
        }
    }
}