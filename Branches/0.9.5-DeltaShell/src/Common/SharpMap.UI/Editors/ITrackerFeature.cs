using GeoAPI.Extensions.Feature;
using System.Drawing;

namespace SharpMap.UI.Editors
{
    /// <summary>
    /// The ITrackerFeature represent a simple feature that is typically used as a visual helper
    /// to the user to manipulate a feature in a mapcontrol.
    /// TrackerFeatures are usually managed by FeatureMutators.
    /// A TrackerFeature does have to be visible; a FeatureMutator can use invisible trackerFeatures
    /// to support extra manipulation features.
    /// eg. LineStringMutator and AllTracker
    /// todo: add support for trackers thatr are not represented by a bitmap.
    /// </summary>
    public interface ITrackerFeature : IFeature
    {
        /// <summary>
        /// A bitmap that is used to draw a tracker on the map. This member
        /// is null for invisible trackers.
        /// </summary>
        Bitmap Bitmap { get; set; }
        /// <summary>
        /// Indicates whether a tracker is focused. Focused trackers are normally represented
        /// by a different bitmap. 
        /// </summary>
        bool Selected { get; set; }
        /// <summary>
        /// The FeatureEditor that is responsible for mutating the feature the tracker belongs to.
        /// </summary>
        IFeatureEditor FeatureEditor { get; set; }
        /// <summary>
        /// A index that normally mathes a coordinate in the geometry of the referenced Feature. For
        /// special trackers this value is typically -1.
        /// </summary>
        int Index { get; }
    }
}