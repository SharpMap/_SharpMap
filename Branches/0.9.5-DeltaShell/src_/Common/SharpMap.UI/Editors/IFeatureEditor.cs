using System.Collections.Generic;
using System.Windows.Forms;
using DelftTools.Utils;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;
using SharpMap.Layers;
using SharpMap.UI.Snapping;

namespace SharpMap.UI.Editors
{
    public delegate void WorkerFeatureCreated(IFeature sourceFeature, IFeature workFeature);

    public interface IFeatureEditor // TODO: rename to ILayerFeatureInteractor, UI-related entity allowing to interact with features
    {
        /// <summary>
        /// original feature
        /// </summary>
        IFeature SourceFeature { get; }

        /// <summary>
        /// a clone of the original feature used during the editing process
        /// </summary>
        IFeature TargetFeature { get; }

        /// <summary>
        /// tolerance in world coordinates used by the editor when no CoordinateConverter is available
        /// </summary>
        double Tolerance { get; set; }

        /// <summary>
        /// CoordinateConverter used to convert coordinates in world coordinates to devivce coordinates and vice versa
        /// </summary>
        ICoordinateConverter CoordinateConverter { get; set; }

        ILayer Layer { get; }
        
        event WorkerFeatureCreated WorkerFeatureCreated;

        Topology.IFallOffPolicy FallOffPolicy { get; set; }
        
        IEnumerable<ITrackerFeature> GetTrackers();

        /// <summary>
        /// Moves selected trackers. trackerFeature is leading amd will be used as source for
        /// falloff policy.
        /// </summary>
        /// <param name="trackerFeature"></param>
        /// <param name="deltaX"></param>
        /// <param name="deltaY"></param>
        /// <returns></returns>
        bool MoveTracker(ITrackerFeature trackerFeature, double deltaX, double deltaY, ISnapResult snapResult);

        /// <summary>
        /// Sets the section state of a tracker.
        /// </summary>
        /// <param name="trackerFeature"></param>
        /// <param name="select"></param>
        void Select(ITrackerFeature trackerFeature, bool select);

        Cursor GetCursor(ITrackerFeature trackerFeature);

        ITrackerFeature GetTrackerAtCoordinate(ICoordinate worldPos);
        
        ITrackerFeature GetTrackerByIndex(int index);

        /// <summary>
        /// Starts the change operation of the feature.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops the change operation of the feature. Enables the editor to cleanup.
        /// </summary>
        void Stop();

        /// <summary>
        /// Stops the change operation of the feature. Enables the editor to cleanup.
        /// </summary>
        /// <param name="snapResult"></param>
        /// contains a snap result that can be used by the editor to connect the feature to other features.
        void Stop(ISnapResult snapResult);

        void Add(IFeature feature);

        void Delete();

        IList<int> GetFocusedTrackerIndices();
        /// <summary>
        /// </summary>
        /// <returns>Returns a list of focused trackers. An empty list is returned if no trackers 
        /// have focus.</returns>
        IList<ITrackerFeature> GetFocusedTrackers();

        /// <summary>
        /// Synchronizes the location of the the tracker with the location of the geometry
        /// of the feature.
        /// e.g. when a structure is moved the tracker is set at the center of the structure.
        /// </summary>
        /// <param name="geometry"></param>
        void UpdateTracker(IGeometry geometry);

        bool AllowMove();
        bool AllowDeletion();
        bool AllowSingleClickAndMove();

        IList<IFeature> GetSnapTargets();

        /// <summary>
        /// Returns a default geometry for the feature
        /// eg. cross sections that are not geometry based will return a linestring geometry perpendicular to the
        /// branch.
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="geometry"></param>
        /// <param name="snappedGeometry"></param>
        /// <param name="location"></param>
        /// <returns></returns>
        IGeometry CreateDefaultGeometry(ILayer layer, IGeometry geometry, IGeometry snappedGeometry, ICoordinate location);

        /// <summary>
        /// Updates the default geometry
        /// e.g. when a non geometry based cross section is moved the linestring is updated based on the offset
        /// on the branch.
        /// </summary>
        /// <param name="parentFeature"></param>
        /// <param name="feature"></param>
        /// <param name="location"></param>
        /// <returns></returns>
        bool UpdateDefaultGeometry(IFeature parentFeature, IFeature feature, ICoordinate location);

        IEditableObject EditableObject { get; set; }
    }
}