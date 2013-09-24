using System.Drawing;
using DelftTools.Utils.Data;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;

namespace SharpMap.Api.Editors
{
    /// <summary>
    /// The ITrackerFeature represent a simple feature that is typically used as a visual helper
    /// to the user to manipulate a feature in a mapcontrol.
    /// TrackerFeatures are usually managed by FeatureMutators.
    /// A TrackerFeature does have to be visible; a FeatureMutator can use invisible trackerFeatures
    /// to support extra manipulation features.
    /// eg. LineStringMutator and AllTracker
    /// todo: add support for Trackers thatr are not represented by a bitmap.
    /// </summary>
    public class TrackerFeature : Unique<long>, IFeature
    {
        public TrackerFeature(IFeatureInteractor featureMutator, IGeometry geometry, int index, Bitmap bitmap)
        {
            FeatureInteractor = featureMutator;
            Geometry = geometry;
            Bitmap = bitmap;
            Index = index;
        }

        /// <summary>
        ///  A bitmap that is used to draw a tracker on the map. This member
        ///  is null for invisible Trackers.
        ///  </summary>
        public Bitmap Bitmap { get; set; }

        /// <summary>
        ///  Indicates whether a tracker is focused. Focused Trackers are normally represented
        ///  by a different bitmap. 
        ///  </summary>
        public bool Selected { get; set; }

        /// <summary>
        ///  The FeatureInteractor that is responsible for mutating the feature the tracker belongs to.
        ///  </summary>
        public IFeatureInteractor FeatureInteractor { get; set; }

        /// <summary>
        ///  A index that normally matches a coordinate in the geometry of the referenced Feature. For
        ///  special Trackers this value is typically -1.
        ///  </summary>
        public int Index { get; set; }

        #region IFeature Members

        public IGeometry Geometry { get; set; }

        public long Id { get; set; }

        public IFeatureAttributeCollection Attributes { get; set; }

        public object Clone()
        {
            return new TrackerFeature(FeatureInteractor, Geometry, Index, Bitmap);
        }

        #endregion
    }
}