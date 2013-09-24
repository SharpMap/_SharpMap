using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using DelftTools.Utils;
using DelftTools.Utils.Editing;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;
using SharpMap.Api;
using SharpMap.Api.Editors;
using SharpMap.Editors;
using SharpMap.Editors.FallOff;
using SharpMap.Editors.Snapping;
using log4net;
using SharpMap.Data.Providers;
using SharpMap.Layers;
using SharpMap.Rendering;
using SharpMap.UI.Helpers;
using SharpMap.Styles;

namespace SharpMap.UI.Tools
{
    public class MoveTool : MapTool
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(MoveTool));

        protected ICoordinate LastMouseLocation { get; set; }
        protected ICoordinate MouseDownLocation { get; set; }
        private IFallOffPolicy fallOffPolicy;
        private bool isBusy;
        private IFeature dragSource;
        private IFeature dragTarget;
        private VectorLayer targetLayer;
        private TrackerFeature snappingSource;
        private SnapResult SnapResult { get; set; }
        private TrackerFeature TrackerFeature { get; set; }

        public FallOffType FallOffPolicy
        {
            get
            {
                return null != fallOffPolicy ? fallOffPolicy.FallOffPolicy : FallOffType.None;
            }
            set
            {
                switch (value)
                {
                    case FallOffType.Linear:
                        fallOffPolicy = new LinearFallOffPolicy();
                        break;
                    case FallOffType.None:
                        fallOffPolicy = new NoFallOffPolicy();
                        break;
                    default:
                        break;
                }
            }
        }

        public MoveTool()
        {
            //CancelKey = Keys.Escape;
            dragLayers = new List<VectorLayer>();
            Name = "Move";
            fallOffPolicy = new NoFallOffPolicy();
        }

        private readonly List<VectorLayer> dragLayers;
        private List<VectorLayer> DragLayers
        {
            get { return dragLayers; }
        }
        private void ResetDragLayers()
        {
            foreach (VectorLayer layer in dragLayers)
            {
                foreach (IFeatureRenderer featureRenderer in layer.CustomRenderers)
                {
                    if (featureRenderer is IDisposable)
                    {
                        ((IDisposable)featureRenderer).Dispose();
                    }
                }
            }
            DragLayers.Clear();
        }
        private VectorLayer GetDragLayer(string name)
        {
            foreach (VectorLayer vectorLayer in DragLayers)
            {
                if (vectorLayer.Name == name)
                    return vectorLayer;
            }
            return null;
        }

        public override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Render(e.Graphics, MapControl.Map);
        }

        public override void Render(Graphics graphics, Map mapBox)
        {
            MapControl.SelectTool.Render(graphics, mapBox);
            foreach (VectorLayer vectorLayer in DragLayers)
            {
                // Render the dragLayer; bypass ILayer.Render and call OnRender directly; this is much more efficient
                // It only usefull to use ILayer.Render when layer contents are not modified between draw
                // operations.
                vectorLayer.OnRender(graphics, mapBox);
            }
            MapControl.SnapTool.Render(graphics, mapBox);
        }

        private VectorStyle selectionStyle;
        private VectorStyle errorSelectionStyle;

        /// <summary>
        /// StartDragging prepares dragging of feature
        /// </summary>
        /// <param name="temp"></param>
        /// <param name="feature"></param>
        /// <returns></returns>
        private IFeature StartDragging(ICoordinate temp, IFeature feature)
        {
            // No feature selected; do not drag
            if (null == feature)
                return null;
            // Is it allowed to drag the selected feature?
            if (!MapControl.SelectTool.SelectedFeatureInteractors[0].AllowMove())
                return null;

            StartDrawing();
            ResetDragLayers();

            // There is 1 feature selected in the selectionLayer of the selecttool. Update the style of this layer based
            // on the style of the 'source' layer
            var sourceLayer = (VectorLayer)Map.GetLayerByFeature(feature);
            selectionStyle = (VectorStyle)sourceLayer.Style.Clone();
            errorSelectionStyle = (VectorStyle)sourceLayer.Style.Clone();
            MapControlHelper.PimpStyle(selectionStyle, true);
            MapControlHelper.PimpStyle(errorSelectionStyle, false);

            MapControl.SelectTool.SelectedFeatureInteractors[0].FallOffPolicy = fallOffPolicy;
            MapControl.SelectTool.SelectedFeatureInteractors[0].WorkerFeatureCreated += MoveTool_WorkerFeatureCreated;
            MapControl.SelectTool.SelectedFeatureInteractors[0].Start();
            dragTarget = MapControl.SelectTool.SelectedFeatureInteractors[0].TargetFeature;
            TrackerFeature = MapControl.SelectTool.SelectedFeatureInteractors[0].GetTrackerAtCoordinate(temp);
            targetLayer = AddFeatureToDragLayers(feature, dragTarget);

            return feature;
        }

        void MoveTool_WorkerFeatureCreated(IFeature sourceFeature, IFeature workFeature)
        {
            AddFeatureToDragLayers(sourceFeature, workFeature);
        }

        private void DoDragging()
        {
            DoDrawing(true);
        }


        private void EndDragging()
        {
            StopDrawing();
            ResetDragLayers();
        }

        private VectorLayer AddFeatureToDragLayers(IFeature sourceFeature, IFeature cloneFeature)
        {
            var sourceLayer = (VectorLayer) Map.GetLayerByFeature(sourceFeature);
            // NOTE: sourceLayer should never return null 
            if (null == sourceLayer)
            {
                throw new ArgumentOutOfRangeException("sourceFeature", "Movetool unable to find sourcelayer; internal corruption caused by removed feature?");
            }
            var dragLayer = GetDragLayer(sourceLayer.Name);
            if (null == dragLayer)
            {
                dragLayer = new VectorLayer(sourceLayer);

                foreach (var customRenderer in sourceLayer.CustomRenderers)
                {
                    var renderer = customRenderer as ICloneable;
                    if (null != renderer)
                    {
                        dragLayer.CustomRenderers.Add((IFeatureRenderer)renderer.Clone());
                    }
                }

                MapControlHelper.PimpStyle(dragLayer.Style, true);

                var dragFeatures = new FeatureCollection();
                dragLayer.DataSource = dragFeatures;
                dragLayer.Map = Map;
                if (sourceLayer.DataSource is FeatureCollection)
                {
                    ((FeatureCollection)dragLayer.DataSource).FeatureType = sourceLayer.DataSource.FeatureType;
                }
                DragLayers.Add(dragLayer);
            }

            if(sourceLayer.Visible && !dragLayer.DataSource.Contains(cloneFeature))
            {
                dragLayer.DataSource.Features.Add(cloneFeature);
                dragLayer.RenderRequired = true;
            }

            return dragLayer;
        }

        public override void OnMouseDown(ICoordinate worldPosition, MouseEventArgs e)
        {
            MapControl.SnapTool.Reset();
            var selectTool = MapControl.SelectTool;
            IFeature oldSelectedFeature = null;
            IList<TrackerFeature> focusedTrackers = null;
            var trackerFeature = selectTool.GetTrackerAtCoordinate(worldPosition);
            
            if (trackerFeature != null)
            {
                oldSelectedFeature = trackerFeature.FeatureInteractor.SourceFeature;
                focusedTrackers = trackerFeature.FeatureInteractor.Trackers.Where(t => t.Selected).ToList();
            }

            // Let the select tool handle the mouse event unless multiple Trackers have focus and
            // there is no key pressed. In this case the user expects to move the focused Trackers
            // and SelectTool will reset them
            if (!((trackerFeature != null && focusedTrackers.Count > 1) && (!selectTool.KeyToggleSelection) &&
                (!selectTool.KeyExtendSelection)/* && (trackerFeature.Selected)*/))
            {
                selectTool.OnMouseDown(worldPosition, e);
                // did we just deselect out only selected tracker?
                if (null != trackerFeature)
                {
                    var focusedTrackersCount = trackerFeature.FeatureInteractor.Trackers.Count(t => t.Selected);
                    if ((focusedTrackers.Count != focusedTrackersCount) && (0 == focusedTrackersCount))
                    {
                        return;
                    }
                }
            }

            if (e.Button != MouseButtons.Left)
            {
                return;
            }
            if (selectTool.SelectedFeatureInteractors.Count != 1)
            {
                return;
            }
            dragSource = null;
            if (selectTool.SelectedFeatureInteractors.Count == 1)
            {
                isBusy = true;
                var feature = selectTool.SelectedFeatureInteractors[0].SourceFeature;
                if (oldSelectedFeature != feature)
                {
                    if (!selectTool.SelectedFeatureInteractors[0].AllowSingleClickAndMove())
                    {
                        isBusy = false;
                        return;
                    }
                }

                if (!selectTool.SelectedFeatureInteractors[0].AllowMove())
                {
                    isBusy = false;
                    return;
                }

                var featureProvider = selectTool.SelectedFeatureInteractors[0].Layer.DataSource;
                // IndexOf doesn't work on shape files; feature rows are recreated during each read
                int dragIndex = featureProvider.Features.IndexOf(feature);
                if (dragIndex == -1)
                {
                    isBusy = false;
                    return;
                }
                dragSource = StartDragging(worldPosition, featureProvider.GetFeature(dragIndex));
                if (dragSource == null)
                {
                    isBusy = false;
                    return;
                }
            }
            else
            {
                return;
            }
            LastMouseLocation = worldPosition;
            MouseDownLocation = worldPosition;
            snappingSource = null;
            var list = selectTool.SelectedFeatureInteractors[0].Trackers.Where(t => t.Selected).ToList();
            if (list.Count == 0)
            {
                return;
            }
            if (list.Count == 1)
            {
                snappingSource = list.First();
            }
        }

        /// <summary>
        /// Synchronise Trackers of the selection with the geometry. This is only necessay when a topology rule
        /// has applied some special adjustments to the geometry during an operation. 
        /// e.g adjusting the angle of a non geometry based cross section.
        /// </summary>
        /// <param name="geometry"></param>
        private void SynchroniseSelectionTrackers(IGeometry geometry)
        {
            MapControl.SelectTool.SelectedFeatureInteractors[0].UpdateTracker(geometry);
        }

        /// <summary>
        /// Processes the mouse movement. Moving an object only works when the left mouse button is pressed (drawgging).
        /// Always call the selecttool to do the default processing such as setting the correct cursor.
        /// </summary>
        /// <param name="worldPosition"></param>
        /// <param name="e"></param>
        public override void OnMouseMove(ICoordinate worldPosition, MouseEventArgs e)
        {
            MapControl.SelectTool.OnMouseMove(worldPosition, e);
            if (LastMouseLocation == null)
            {
                return;
            }
            if ((e.Button != MouseButtons.Left) && (MapControl.SelectTool.IsActive))
            {
                return;
            } 
            if ((worldPosition.X == LastMouseLocation.X) && (worldPosition.Y == LastMouseLocation.Y))
            {
                return;
            }     
            SnapResult = null;
            var selectedFeatureInteractor = MapControl.SelectTool.SelectedFeatureInteractors[0];
            if (dragSource != null)
            {
                SnapResult =
                    MapControl.SnapTool.ExecuteLayerSnapRules(selectedFeatureInteractor.Layer,
                                                              dragSource, dragTarget.Geometry, worldPosition,
                                                              (snappingSource == null) ? -1 : snappingSource.Index);

                targetLayer.Style = (SnapResult == null) ? errorSelectionStyle : selectionStyle;
            }
            if (dragTarget != null)
            {
                double deltaX;
                double deltaY;
                if (SnapResult != null && snappingSource != null)
                {
                    var oldLocation = dragTarget.Geometry.Coordinates[snappingSource.Index];

                    deltaX = SnapResult.Location.X - oldLocation.X;
                    deltaY = SnapResult.Location.Y - oldLocation.Y;
                }
                else
                {
                    deltaX = worldPosition.X - LastMouseLocation.X;
                    deltaY = worldPosition.Y - LastMouseLocation.Y;
                }
                selectedFeatureInteractor.MoveTracker(TrackerFeature, deltaX, deltaY, SnapResult);
                DoDragging();
            }
            LastMouseLocation = worldPosition;
        }

        public override void OnMouseUp(ICoordinate worldPosition, MouseEventArgs e)
        {
            if (isBusy)
            {
                EndDragging();

                if (null != SnapResult)
                //if (!MapControl.SnapTool.Failed)
                {
                    // only execute move when mouse has been moved.
                    if (!((worldPosition.X == MouseDownLocation.X) && (worldPosition.Y == MouseDownLocation.Y)))
                    {
                        if (MapControl.SelectTool.SelectedFeatureInteractors.Count > 0)
                        {
                            // hack? sourceLayer doesn't have to be part of a network; thus we are
                            // required to force repaint. DataSource has no knowledge of layer.
                            VectorLayer sourceLayer = (VectorLayer)Map.GetLayerByFeature(dragSource);
                            var interactor = MapControl.SelectTool.SelectedFeatureInteractors[0];

                            if (interactor.EditableObject != null)
                            {
                                interactor.EditableObject.BeginEdit(string.Format("Move feature {0}",
                                                                                     interactor.SourceFeature is
                                                                                     INameable
                                                                                         ? ((INameable)
                                                                                            interactor.SourceFeature)
                                                                                               .Name
                                                                                         : ""));
                            }

                            interactor.Stop(SnapResult);
                            
                            if(interactor.EditableObject != null)
                            {
                                interactor.EditableObject.EndEdit();
                            }
                            
                            //// hack? sourceLayer doesn't have to be part of a network; thus we are
                            //// required to force repaint. DataSource has no knowledge of layer.
                            //VectorLayer sourceLayer = (VectorLayer)Map.GetLayerByFeature(dragSource);
                            //sourceLayer.RenderRequired = true;
                            sourceLayer.RenderRequired = true;
                        }
                    }
                    Cleanup();
                    //if (MapControl.SelectTool.SelectedFeatureInteractors.Count > 0)
                    //{
                    //    MapControl.SelectTool.SelectedFeatureInteractors[0].SourceFeature.Geometry = dragTarget.Geometry;
                    //    //MapControl.SelectTool.UpdateSelection(dragTarget.Geometry);
                    //}
                    MapControl.SnapTool.Reset();
                }
                else
                {
                    Cancel();
                }
                dragSource = null;
                dragTarget = null;
            }
            // forward handling to selecttool; enable selection cycling
            MapControl.SelectTool.OnMouseUp(worldPosition, e);
        }

        public override bool IsBusy
        {
            get { return isBusy; }
        }

        private void Cleanup()
        {
            fallOffPolicy.Reset();
            LastMouseLocation = null;
            dragSource = null;
            isBusy = false;
        }

        public override void Cancel()
        {
            EndDragging();
            Cleanup();
        }

    }
}
