using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GeoAPI.Extensions.Networks;
using GeoAPI.Geometries;
using SharpMap.Api;
using SharpMap.Api.Editors;
using SharpMap.CoordinateSystems.Transformations;
using SharpMap.Editors;
using log4net;
using SharpMap.Data.Providers;
using SharpMap.Layers;
using SharpMap.Rendering;
using SharpMap.Rendering.Thematics;
using SharpMap.Styles;
using SharpMap.UI.Forms;
using SharpMap.UI.Helpers;
using GeoAPI.Extensions.Feature;
using System.ComponentModel;
using DelftTools.Utils.Collections;
using GeometryFactory = SharpMap.Converters.Geometries.GeometryFactory;
using Point = GisSharpBlog.NetTopologySuite.Geometries.Point;

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
    /// - Selection is visible to the user via Trackers. Features with an IPoint geometry have 1 
    ///   tracker, based on ILineString and IPolygon have a tracker for each coordinate
    /// - Trackers can have focus. 
    ///   If a Trackers has focus is visible to the user via another symbol (or same symbol in other color)
    ///   A tracker that has the focus is the tracker leading during special operation such as moving. 
    ///   For single selection a feature with an IPoint geometry automatically get the focus to the 
    ///   only tracker
    /// - Multiple Trackers with focus
    /// - adding focus Trackers (KeyExtendSelection; normally the SHIFT key)
    /// - * KeyExtendSelection can be used to select all branches between the most recent two selected branches,
    ///     using shortest path.
    /// - toggling focus Trackers (KeyToggleSelection; normally the CONTROL key)
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

        public MultiSelectionMode MultiSelectionMode { get; set; }
        
        /// <summary>
        /// Interactors created for selected features.
        /// </summary>
        public IList<IFeatureInteractor> SelectedFeatureInteractors { get; private set; }
        
        private readonly Collection<TrackerFeature> trackers = new Collection<TrackerFeature>();
        
        /// <summary>
        /// Current layer where features are being selected (branch, nodes, etc.)
        /// </summary>
        private VectorLayer TrackingLayer // will be TrackingLayer containing tracking geometries
        {   
            get { return trackingLayer; }
        }

        public int SelectedTrackersCount
        {
            get { return SelectedTrackerIndices.Count; }
        }

        public IList<int> SelectedTrackerIndices
        {
            get
            {
                return SelectedFeatureInteractors.Count == 1
                           ? SelectedFeatureInteractors[0].Trackers
                                                          .Where(t => t.Selected)
                                                          .Select(t => t.Index).ToList()
                           : new List<int>();
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
            SelectedFeatureInteractors = new List<IFeatureInteractor>();
            Name = "Select";

            trackingLayer.Name = "Trackers";
            var trackerProvider = new FeatureCollection { Features = trackers };

            trackingLayer.DataSource = trackerProvider;

            var iTheme = new CustomTheme(GetTrackerStyle);
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
                if (null != tracker.FeatureInteractor.SourceFeature)
                {
                    // TODO: add transformation

                    // todo optimize this; only necessary when map extent has changed.
                    tracker.FeatureInteractor.UpdateTracker(tracker.FeatureInteractor.SourceFeature.Geometry);
                }
            }

            SynchronizeTrackers();
            trackingLayer.OnRender(graphics, map);
        }

        public TrackerFeature GetTrackerAtCoordinate(ICoordinate worldPos)
        {
            TrackerFeature trackerFeature = null;
            foreach (IFeatureInteractor featureInteractor in SelectedFeatureInteractors)
            {
                trackerFeature = featureInteractor.GetTrackerAtCoordinate(worldPos);
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

        private IFeatureInteractor GetActiveFeatureInteractor(IFeature feature)
        {
            return SelectedFeatureInteractors.FirstOrDefault(t => ReferenceEquals(t.SourceFeature, feature));
        }

        private INode sourceNode, targetNode;

        public override void OnMouseDown(ICoordinate worldPosition, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            var oldSelectedTrackerIndicesCount = SelectedTrackersCount;
            var oldTrackerFeatureCount = trackers.Count;

            IsBusy = true;
            ILayer selectedLayer;
            mouseDownLocation = worldPosition;

            // Check first if an object is already selected and if the mousedown has occured at this object.
            var trackerFeature = GetTrackerAtCoordinate(worldPosition);
            if (SelectedFeatureInteractors.Count > 1)
            {
                // hack: if multiple selection toggle/select complete feature
                trackerFeature = null;
            }

            SetClickOnExistingSelection(false, null);

            if (null != trackerFeature)
            {
                if (1 == SelectedFeatureInteractors.Count)
                {
                    SetClickOnExistingSelection(true, worldPosition);
                    FocusTracker(trackerFeature);
                    MapControl.Refresh();

                }
                return;
            }
            // single selection. Find the nearest geometry and give 
            var limit = (float)MapHelper.ImageToWorld(Map, 4);
            var nearest = FindNearestFeature(worldPosition, limit, out selectedLayer, ol => ol.Visible);
            if (null != nearest)
            {
                // Create or add a new FeatureInteractor
                if (SelectedFeatureInteractors.Count > 0)
                {
                    IFeatureInteractor currentFeatureInteractor = GetActiveFeatureInteractor(nearest);
                    if (KeyExtendSelection) // Shift key
                    {
                        if (null == currentFeatureInteractor)
                        {
                            var selectedBranch = nearest as IBranch;
                            if (selectedBranch != null)
                            {
                                SelectBranchesAlongShortestPath(worldPosition, selectedLayer, nearest, selectedBranch);
                            }
                            else
                            {
                                // not in selection; add
                                AddSelection(selectedLayer, nearest);
                                targetNode = null;
                            }
                        } // else possibly set default focus tracker
                    }
                    else if (KeyToggleSelection) // CTRL key
                    {
                        if (null == currentFeatureInteractor)
                        {
                            // not in selection; add
                            AddSelection(selectedLayer, nearest);
                            SetTargetNodeIfBranch(worldPosition, nearest);
                        }
                        else
                        {
                            // in selection; remove
                            RemoveSelection(nearest);
                            targetNode = null;
                        }
                    }
                    else
                    {
                        // no special key processing; handle as a single select.
                        Clear(false);
                        if (!StartSelection(selectedLayer, nearest))
                        {
                            StartMultiSelect();
                        }
                        SetTargetNodeIfBranch(worldPosition, nearest);
                    }
                }
                else
                {
                    if (!StartSelection(selectedLayer, nearest))
                    {
                        StartMultiSelect();
                    }
                    SetTargetNodeIfBranch(worldPosition, nearest);
                }
            }
            else
            {
                // We didn't find an object at the position of the mouse button -> start a multiple select
                if (!KeyExtendSelection)
                {
                    // we are not extending the current selection
                    Clear(false);
                }
                if (e.Button == MouseButtons.Left)
                {
                    StartMultiSelect();
                }
            }

            if ((oldSelectedTrackerIndicesCount != SelectedTrackersCount
                || oldTrackerFeatureCount != trackers.Count) && trackingLayer.DataSource.Features.Count != 0)
            {
                MapControl.Refresh();
            }
        }

        private void SelectBranchesAlongShortestPath(ICoordinate worldPosition, ILayer selectedLayer, IFeature nearest,
                                                     IBranch selectedBranch)
        {
            // TODO: add transformation

            sourceNode = targetNode;
            targetNode = selectedBranch.Source.Geometry.Distance(new Point(worldPosition)) <
                         selectedBranch.Target.Geometry.Distance(new Point(worldPosition))
                             ? selectedBranch.Source
                             : selectedBranch.Target;
            var result = selectedBranch.Network.GetShortestPath(sourceNode, targetNode, null);

            foreach (var branch in result)
            {
                AddSelection(selectedLayer, branch);
            }

            // Ensure 'nearest' will be added to the selection
            if (!result.Contains(selectedBranch))
            {
                AddSelection(selectedLayer, nearest);
            }
        }

        private void SetTargetNodeIfBranch(ICoordinate worldPosition, IFeature nearest)
        {
            var selectedBranch = nearest as IBranch;
            if (nearest is IBranch)
            {
                // TODO: add transformation

                targetNode = selectedBranch.Source.Geometry.Distance(new Point(worldPosition)) <
                             selectedBranch.Target.Geometry.Distance(new Point(worldPosition))
                                 ? selectedBranch.Source
                                 : selectedBranch.Target;
            }
            else
            {
                // Not selecting a branch, thus clear 'targetNode'
                targetNode = null;
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
            var trackerFeature = (TrackerFeature)feature;

            VectorStyle style;

            // styles are stored in the cache for performance reasons
            lock (stylesCache)
            {
                if (!stylesCache.ContainsKey(trackerFeature.Bitmap))
                {
                    style = new VectorStyle { Symbol = trackerFeature.Bitmap };
                    stylesCache[trackerFeature.Bitmap] = style;
                }
                else
                {
                    style = stylesCache[trackerFeature.Bitmap];
                }
            }

            return style;
        }

        static readonly IDictionary<Bitmap, VectorStyle> stylesCache = new Dictionary<Bitmap, VectorStyle>();

        /// <summary>
        /// Note: this method seems to assume the whole network is in a non-editing state.
        /// Calling it for a network in the middle of an edit-action might cause bugs.
        /// </summary>
        public void Clear()
        {
            Clear(true);
        }

        private void Clear(bool fireSelectionChangedEvent)
        {
            SelectedFeatureInteractors.Clear();
            if (trackingLayer.DataSource.GetFeatureCount() <= 0)
                return;
            trackers.Clear();
            trackingLayer.RenderRequired = true;
            UpdateMapControlSelection(fireSelectionChangedEvent);
        }

        private void SynchronizeTrackers()
        {
            trackers.Clear();
            foreach (IFeatureInteractor featureInteractor in SelectedFeatureInteractors)
            {
                foreach (TrackerFeature trackerFeature in featureInteractor.Trackers)
                {
                    if (featureInteractor.Layer.CoordinateTransformation != null)
                    {
                        var trackerFeature2 = (TrackerFeature)trackerFeature.Clone();
                        trackerFeature2.Geometry = GeometryTransform.TransformGeometry(trackerFeature.Geometry, featureInteractor.Layer.CoordinateTransformation.MathTransform);
                        trackers.Add(trackerFeature2);
                    }
                    else
                    {
                        trackers.Add(trackerFeature);
                    }
                }
            }
            trackingLayer.RenderRequired = true;
        }

        public IFeatureInteractor GetFeatureInteractor(ILayer layer, IFeature feature)
        {
            try
            {
                if (layer.FeatureEditor == null) return null;

                return layer.FeatureEditor.CreateInteractor(layer, feature);
            }
            catch (Exception exception)
            {
                log.Error("Error creating feature interactor: " + exception.Message);
                return null;
            }
        }

        private bool StartSelection(ILayer layer, IFeature feature)
        {
            var featureInteractor = GetFeatureInteractor(layer, feature);
            if (null == featureInteractor) return false;

            if (featureInteractor.AllowSingleClickAndMove())
            {
                // do not yet select, but allow MltiSelect
                SelectedFeatureInteractors.Add(featureInteractor);
                SynchronizeTrackers();
                UpdateMapControlSelection();
                return true;
            }
            return false;
        }

        public void AddSelection(IEnumerable<IFeature> features)
        {
            foreach (IFeature feature in features)
            {
                var layer = Map.GetLayerByFeature(feature);
                if (layer == null)
                {
                    throw new ArgumentOutOfRangeException("features", "Can't find layer for feature: " + feature);
                }
                AddSelection(layer, feature, false);
            }
            UpdateMapControlSelection();
        }

        public void AddSelection(ILayer layer, IFeature feature, bool synchronizeUI = true)
        {
            if (!layer.Visible) return;
            var featureInteractor = GetFeatureInteractor(layer, feature);
            if (null == featureInteractor) return;

            SelectedFeatureInteractors.Add(featureInteractor);
            if (synchronizeUI)
            {
                UpdateMapControlSelection();
            }
        }

        public IEnumerable<IFeature> Selection
        {
            get { return SelectedFeatureInteractors.Select(interactor => interactor.SourceFeature); }
        }

        private void RemoveSelection(IFeature feature)
        {
            for (int i = 0; i < SelectedFeatureInteractors.Count; i++)
            {
                if (ReferenceEquals(SelectedFeatureInteractors[i].SourceFeature, feature))
                {
                    SelectedFeatureInteractors.RemoveAt(i);
                    break;
                }
            }
            UpdateMapControlSelection();
        }

        /// <summary>
        /// Sets the selected object in the selectTool. SetSelection supports also the toggling/extending the 
        /// selected Trackers.
        /// </summary>
        /// <param name="feature"></param>
        /// <param name="featureLayer"></param>
        /// <returns>A clone of the original object.</returns>
        /// special cases 
        /// feature is ILineString or IPolygon and trackerIndex != 1 : user clicked an already selected 
        /// features -> only selected tracker changes.
        private void SetSelection(IFeature feature, ILayer featureLayer)
        {
            if (null != feature)
            {
                // store selected Trackers
                IList<int> featureTrackers = new List<int>();
                for (int i = 0; i < TrackingLayer.DataSource.Features.Count; i++)
                {
                    var trackerFeature = (TrackerFeature)TrackingLayer.DataSource.Features[i];
                    if (ReferenceEquals(trackerFeature, feature))
                    {
                        featureTrackers.Add(i);
                    }
                }
                // store selected objects 
                AddSelection(featureLayer, feature);
            }
        }
        
        private void FocusTracker(TrackerFeature trackFeature)
        {
            if (null == trackFeature)
                return;

            if (!((KeyToggleSelection) || (KeyExtendSelection)))
            {
                foreach (IFeatureInteractor featureInteractor in SelectedFeatureInteractors)
                {
                    foreach (TrackerFeature trackerFeature in featureInteractor.Trackers)
                    {
                        featureInteractor.SetTrackerSelection(trackerFeature, false);
                    }
                }
            }
            foreach (IFeatureInteractor featureInteractor in SelectedFeatureInteractors)
            {
                foreach (TrackerFeature trackerFeature in featureInteractor.Trackers)
                {
                    if (trackerFeature == trackFeature)
                    {
                        if (KeyToggleSelection)
                        {
                            featureInteractor.SetTrackerSelection(trackFeature, !trackFeature.Selected);
                        }
                        else
                        {
                            featureInteractor.SetTrackerSelection(trackFeature, true);
                        }
                    }
                }
            }
        }

        private readonly List<PointF> selectPoints = new List<PointF>();

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
            var vertices = selectPoints.Select(point => Map.ImageToWorld(point)).ToList();

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

            if (MapControl.Tools.Any(t => t != this && t.IsBusy))
                return; //don't influence cursor when other tools are busy

            Cursor cursor = null;
            foreach (IFeatureInteractor featureInteractor in SelectedFeatureInteractors)
            {
                TrackerFeature trackerFeature = featureInteractor.GetTrackerAtCoordinate(worldPosition);
                if (null != trackerFeature)
                {

                    cursor = ((FeatureInteractor)featureInteractor).GetCursor(trackerFeature);
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
            UpdateMapControlSelection(true);
        }
        
        private void UpdateMapControlSelection(bool fireSelectionChangedEvent)
        {
            SynchronizeTrackers();

            IList<IFeature> selectedFeatures = SelectedFeatureInteractors.Select(t => t.SourceFeature).ToList();

            MapControl.SelectedFeatures = selectedFeatures;

            if (fireSelectionChangedEvent && SelectionChanged != null)
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
            if (e.Button != MouseButtons.Left) return;

            if (IsMultiSelect)
            {
                HandleMultiSelectMouseUp(worldPosition);
            }
            else
            {
                HandleMouseUp(worldPosition, e);
            }
            IsBusy = false;
            orgClickTime = DateTime.Now;
        }

        protected virtual void HandleMouseUp(ICoordinate worldPosition, MouseEventArgs e)
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
                    if (1 == SelectedFeatureInteractors.Count)
                    {
                        // check if selection exists; could be toggled
                        Layer outLayer;
                        IFeature nextFeature = GetNextFeatureAtPosition(worldPosition,
                            // set limit from 4 to 10: TOOLS-1499
                                                                        (float)MapHelper.ImageToWorld(Map, 10),
                                                                        out outLayer,
                                                                        SelectedFeatureInteractors[0].SourceFeature,
                                                                        ol => ol.Visible);
                        if (null != nextFeature)
                        {
                            Clear(false);
                            SetSelection(nextFeature, outLayer); //-1 for ILineString
                            //MapControl.Refresh();
                        }
                    }
                }
            }
            UpdateMapControlSelection(true);
        }

        /// TODO: note if no features are selected the selection rectangle maintains visible after mouse up
        /// ISSUE 2373
        private void HandleMultiSelectMouseUp(ICoordinate worldPosition)
        {
            StopMultiSelect();
            List<IFeature> selectedFeatures = null;
            if (!KeyExtendSelection)
            {
                selectedFeatures = new List<IFeature>(SelectedFeatureInteractors.Select(fe => fe.SourceFeature).ToArray());
                Clear(false);
            }
            var selectionPolygon = CreateSelectionPolygon(worldPosition);
            if (null != selectionPolygon)
            {
                foreach (ILayer layer in Map.GetAllVisibleLayers(false))
                {
                    //make sure parent layer is selectable or null
                    var parentLayer = Map.GetGroupLayerContainingLayer(layer);
                    if ( (parentLayer == null || parentLayer.IsSelectable) && (layer.IsSelectable) && (layer is VectorLayer))
                    {
                        // do not use the maptool provider but the datasource of each layer.
                        var vectorLayer = (VectorLayer)layer;
                        var multiFeatures = vectorLayer.GetFeatures(selectionPolygon).Take(5000);
                        foreach (IFeature feature in multiFeatures)
                        {
                            if ((null != selectedFeatures) && (selectedFeatures.Contains(feature)))
                            {
                                continue;
                            }
                            AddSelection(vectorLayer, feature, false);
                        }
                    }
                }
            }
            else
            {
                // if mouse hasn't moved handle as single select. A normal multi select uses the envelope
                // of the geometry and this has as result that unwanted features will be selected.
                ILayer selectedLayer;
                var limit = (float)MapHelper.ImageToWorld(Map, 4);
                var nearest = FindNearestFeature(worldPosition, limit, out selectedLayer, ol => ol.Visible);
                if (null != nearest)
                {
                    AddSelection(selectedLayer, nearest, false);
                }
            }

            // synchronize with map selection, possible check if selection is already set; do not remove
            UpdateMapControlSelection(true);
        }

        readonly VectorLayer trackingLayer = new VectorLayer(String.Empty);

        public override IMapControl MapControl
        {
            get { return base.MapControl; }
            set
            {
                base.MapControl = value;
                trackingLayer.Map = MapControl.Map;
            }
        }

        /// <summary>
        /// Selects the given features on the map. Will search all layers for the features when no vector layer is provided
        /// </summary>
        /// <param name="featuresToSelect">The feature to select on the map.</param>
        /// <param name="vectorLayer">The layer on which the features reside.</param>
        public bool Select(IEnumerable<IFeature> featuresToSelect, ILayer vectorLayer = null)
        {
            if (featuresToSelect == null)
            {
                Clear(true);
                return false;
            }

            Clear(false);
            foreach (var feature in featuresToSelect)
            {
                var foundLayer = vectorLayer ?? Map.GetLayerByFeature(feature);
                if (foundLayer != null && foundLayer is VectorLayer)
                {
                    AddSelection(foundLayer, feature, ReferenceEquals(feature, featuresToSelect.Last()));
                }
            }
            return true;
        }

        /// <summary>
        /// Selects the given feature on the map. Will search all layers for the feature.
        /// </summary>
        /// <param name="featureToSelect">The feature to select on the map.</param>
        public bool Select(IFeature featureToSelect)
        {
            if (null == featureToSelect)
            {
                Clear(true);
                return false;
            }
            // Find the layer that this feature is on
            ILayer foundLayer = MapControl.Map.GetLayerByFeature(featureToSelect);
            if (foundLayer != null && foundLayer is VectorLayer)
            {
                // Select the feature
                Select(foundLayer, featureToSelect);
                return true;
            }

            return false;
        }

        public void Select(ILayer vectorLayer, IFeature feature)
        {
            if (IsBusy)
            {
                return;
            }

            Clear(false);
            SetSelection(feature, vectorLayer);
            UpdateMapControlSelection(true);
        }

        /// <summary>
        /// Handles changes to the map (or bubbled up from ITheme, ILayer) properties. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public override void OnMapPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var layer = sender as ILayer;
            if (layer != null)
            {
                if (e.PropertyName == "Visible" && !layer.Visible) 
                {
                    RefreshSelection();
                }

                if (e.PropertyName == "Enabled")
                {
                    // If a layer is enabled of disables and features of the layer are selected 
                    // the selection is cleared. Another solution is to remove only features of layer 
                    // from the selection, but this simple and effective.
                    if (layer is GroupLayer)
                    {
                        var layerGroup = (GroupLayer)layer;
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
            if (trackers.Any(trackerFeature => layer == trackerFeature.FeatureInteractor.Layer))
            {
                Clear();
            }
        }

        public override void OnMapCollectionChanged(object sender, NotifyCollectionChangingEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangeAction.Remove:
                    {
                        if(e.Item is ILayer)
                        {
                            RefreshSelection();
                        }

                        if (sender is Map)
                        {
                            var layer = (ILayer)e.Item;
                            if (layer is GroupLayer)
                            {
                                var layerGroup = (GroupLayer)layer;
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
                case NotifyCollectionChangeAction.Replace:
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
            Clear(true);
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

        internal void RefreshFeatureInteractors()
        {
            var selectedFeaturesWithLayer = SelectedFeatureInteractors.Select(fe => new {Feature = fe.SourceFeature, fe.Layer}).ToList();
            SelectedFeatureInteractors.Clear();
            selectedFeaturesWithLayer.ForEach(fl => SelectedFeatureInteractors.Add(GetFeatureInteractor(fl.Layer, fl.Feature)));
            SynchronizeTrackers();
        }

        /// <summary>
        /// Checks if selected features are actually need to be selected.
        /// </summary>
        public void RefreshSelection()
        {
            var layers = Map.GetAllVisibleLayers(true).ToArray();
            SelectedFeatureInteractors.Where(i => !layers.Contains(i.Layer)).ToList().ForEach(i => RemoveSelection(i.SourceFeature));
        }
    }
}