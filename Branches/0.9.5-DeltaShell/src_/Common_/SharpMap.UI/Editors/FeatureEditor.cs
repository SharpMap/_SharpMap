using System;
using System.Collections.Generic;
using System.Reflection;
using GeoAPI.Geometries;
using GeoAPI.Extensions.Feature;
using NetTopologySuite.Extensions.Geometries;
using SharpMap.Layers;
using SharpMap.Styles;
using SharpMap.UI.Helpers;
using System.Windows.Forms;
using SharpMap.Topology;
using SharpMap.UI.Snapping;

namespace SharpMap.UI.Editors
{
    public class FeatureEditor : IFeatureEditor
    {
        public IFeature SourceFeature { get; protected set; }
        public IFeature TargetFeature { get; protected set; }
        public ICoordinateConverter CoordinateConverter { get; set; }

        public ILayer Layer { get; protected set; }
        public VectorStyle VectorStyle { get; protected set; }

        public virtual IFallOffPolicy FallOffPolicy { get; set; }
        // protected IMapControl MapControl { get; set; }
        protected readonly List<ITrackerFeature> trackers = new List<ITrackerFeature>();

        public FeatureEditor(ICoordinateConverter coordinateConverter, ILayer layer, IFeature feature, VectorStyle vectorStyle)
        {
            CoordinateConverter = coordinateConverter;
            Layer = layer;
            SourceFeature = feature;
            VectorStyle = vectorStyle;
            TopologyRules = new List<IFeatureRelationEditor>();
        }

        protected virtual void CreateTrackers()
        {
        }


        public virtual IEnumerable<ITrackerFeature> GetTrackers()
        {
            for (int i = 0; i < trackers.Count; i++)
            {
                yield return trackers[i];
            }
        }

        public virtual bool MoveTracker(ITrackerFeature trackerFeature, double deltaX, double deltaY, ISnapResult snapResult)
        {
            int index = -1;
            IList<int> handles = new List<int>();
            IList<IGeometry> geometries = new List<IGeometry>();

            for (int i = 0; i < trackers.Count; i++)
            {
                geometries.Add(trackers[i].Geometry);
                if (trackers[i].Selected)
                {
                    handles.Add(i);
                }
                if (trackers[i] == trackerFeature)
                {
                    index = i;
                }
            }

            if (-1 == index)
                throw new ArgumentException("Can not find tracker; can not move.");
            //return false;
            if (0 == handles.Count)
                throw new ArgumentException("No trackers selected, can not move.");
            //return false;
            if (null != FallOffPolicy)
            {
                FallOffPolicy.Move(TargetFeature.Geometry, geometries, handles, index, deltaX, deltaY);
            }
            else
            {
                GeometryHelper.MoveCoordinate(TargetFeature.Geometry, index, deltaX, deltaY);
                TargetFeature.Geometry.GeometryChangedAction();

                GeometryHelper.MoveCoordinate(trackerFeature.Geometry, 0, deltaX, deltaY);
                trackerFeature.Geometry.GeometryChangedAction();
            }

            foreach (IFeatureRelationEditor topologyRule in TopologyRules)
            {
                topologyRule.UpdateRelatedFeatures(SourceFeature, TargetFeature.Geometry, handles);
            }

            return true;
        }

        public virtual void Select(ITrackerFeature trackerFeature, bool select)
        {
        }

        public virtual Cursor GetCursor(ITrackerFeature trackerFeature)
        {
// ReSharper disable AssignNullToNotNullAttribute
            return new Cursor(Assembly.GetExecutingAssembly().GetManifestResourceStream("SharpMap.UI.Cursors.moveTracker.cur"));
// ReSharper restore AssignNullToNotNullAttribute
        }

        public virtual ITrackerFeature GetTrackerAtCoordinate(ICoordinate worldPos)
        {
            for (int i = 0; i < trackers.Count; i++)
            {
                ITrackerFeature trackerFeature = trackers[i];

                ICoordinate size;

                if (null != trackerFeature.Bitmap)
                {
                    size = CoordinateConverter.ImageToWorld(trackerFeature.Bitmap.Width, trackerFeature.Bitmap.Height);   
                }
                else
                {
                    // hack for RegularGridCoverageLayer
                    size = CoordinateConverter.ImageToWorld(6, 6);
                }
                IEnvelope boundingBox = MapControlHelper.GetEnvelope(worldPos, size.X, size.Y);

                if (trackerFeature.Geometry.EnvelopeInternal.Intersects(boundingBox))
                    return trackerFeature;
            }
            return null;
        }

        public virtual ITrackerFeature GetTrackerByIndex(int index)
        {
            return trackers[index];
        }

        protected IFeature CreateTargetFeature()
        {
            return (IFeature)SourceFeature.Clone();
        }

        public virtual void Start()
        {
            //var nodeToBranchTopologyRule = new NodeToBranchTopologyRule();
            TargetFeature = CreateTargetFeature();
            foreach (IFeatureRelationEditor topologyRule in GetRelationEditorRules(SourceFeature))
            {
                IFeatureRelationEditor activeRule = topologyRule.Activate(SourceFeature, TargetFeature, AddRelatedFeature, 0, FallOffPolicy);
                if (null != activeRule)
                    TopologyRules.Add(activeRule);
            }
        }

        private void AddRelatedFeature(IList<IFeatureRelationEditor> childTopologyRules, IFeature sourceFeature, IFeature cloneFeature, int level)
        {
            //-->AddFeatureToDragLayers(sourceFeature, cloneFeature);
            OnWorkerFeatureCreated(sourceFeature, cloneFeature);

            foreach (IFeatureRelationEditor topologyRule in GetRelationEditorRules(sourceFeature))
            {
                IFeatureRelationEditor activeRule = topologyRule.Activate(sourceFeature, cloneFeature, AddRelatedFeature, ++level, FallOffPolicy);
                if (null != activeRule)
                    childTopologyRules.Add(activeRule);
            }
        }

        public virtual void Delete()
        {
            Layer.DataSource.Features.Remove(SourceFeature);
            Layer.RenderRequired = true;
        }

        public virtual void Stop()
        {
            if (null == TargetFeature) 
                return;
            
            foreach (IFeatureRelationEditor topologyRule in TopologyRules)
            {
                topologyRule.StoreRelatedFeatures(SourceFeature, TargetFeature.Geometry, new List<int> { 0 });
            }

            for (int i = 0; i < TargetFeature.Geometry.Coordinates.Length; i++)
            {
                SourceFeature.Geometry.Coordinates[i].X = TargetFeature.Geometry.Coordinates[i].X;
                SourceFeature.Geometry.Coordinates[i].Y = TargetFeature.Geometry.Coordinates[i].Y;
            }
            SourceFeature.Geometry.GeometryChangedAction();

            TopologyRules.Clear();

        }

        public virtual void Stop(ISnapResult snapResult)
        {
            Stop();
        }

        public virtual IList<int> GetFocusedTrackerIndices()
        {
            IList<int> selectedIndices = new List<int>();

            for (int i = 0; i < trackers.Count; i++)
            {
                if (trackers[i].Selected)
                {
                    selectedIndices.Add(i);
                }
            }
            return selectedIndices;
        }
        public virtual IList<ITrackerFeature> GetFocusedTrackers()
        {
            IList<ITrackerFeature> focusedTrackers = new List<ITrackerFeature>();

            for (int i = 0; i < trackers.Count; i++)
            {
                if (trackers[i].Selected)
                {
                    focusedTrackers.Add(trackers[i]);
                }
            }
            return focusedTrackers;
        }

        public virtual void UpdateTracker(IGeometry geometry)
        {
        }

        /// <summary>
        /// Default implementation for moving feature is set to false. IFeatureProvider is not required to
        /// return the same objects for each request. For example the IFeatureProvider for shapefiles 
        /// constructs them on the fly in each GetGeometriesInView call. To support deletion and movinf of
        /// shapes local caching and writing of shape files has to be implemented.
        /// </summary>
        /// <returns></returns>
        public virtual bool AllowMove()
        {
            return false;
        }
        /// <summary>
        /// Default set to false. See AllowMove.
        /// </summary>
        /// <returns></returns>
        public virtual bool AllowDeletion()
        {
            return false;
        }
        /// <summary>
        /// Default set to false. See AllowMove.
        /// Typically set to true for IPoint based geomoetries where there is only 1 tracker.
        /// </summary>
        /// <returns></returns>
        public virtual bool AllowSingleClickAndMove()
        {
            return false;
        }

        public event WorkerFeatureCreated WorkerFeatureCreated;
        protected virtual void OnWorkerFeatureCreated(IFeature sourceFeature, IFeature workFeature)
        {
            if (null != WorkerFeatureCreated)
            {
                WorkerFeatureCreated(sourceFeature, workFeature);
            }
        }


        protected IList<IFeatureRelationEditor> TopologyRules { get; set; }

        public virtual IEnumerable<IFeatureRelationEditor> GetRelationEditorRules(IFeature feature)
        {
            yield break;
        }
        public virtual IList<IFeature> GetSnapTargets()
        {
            return null;
        }

        public virtual IGeometry CreateDefaultGeometry(ILayer layer, IGeometry geometry, IGeometry snappedGeometry, ICoordinate location)
        {
            return null;
        }

        public virtual bool UpdateDefaultGeometry(IGeometry parentGeometry, IFeature feature, ICoordinate location)
        {
            return false;
        }

        public virtual void Add(IFeature feature)
        {
            Start();
            Stop();
        }
    }
}