using System.Collections.Generic;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;

namespace SharpMap.UI.Snapping
{
    public interface ISnapResult
    {
        /// <summary>
        /// index of the curvepoint that precedes Location. If snapping was succesfull at a curvepoint
        /// SnapIndexPrevious == SnapIndexNext
        /// </summary>
        int SnapIndexPrevious { get; }

        /// <summary>
        /// index of the curvepoint that follows Location. If snapping was succesfull at a curvepoint
        /// SnapIndexPrevious == SnapIndexNext
        /// </summary>
        int SnapIndexNext { get; }

        /// <summary>
        /// The geometry that is closest to the snapping Location. SnapIndexPrevious and SnapIndexNext
        /// refer to this IGeometry object.
        /// </summary>
        IGeometry NearestTarget { get; }

        /// <summary>
        /// The feature that is snapped to
        /// todo replace NearestTarget with SnappedFeature
        /// </summary>
        IFeature SnappedFeature { get; }

        /// <summary>
        /// coordinate of the succesfull snap position
        /// </summary>
        ICoordinate Location { get; }
      
        /// <summary>
        /// Additional geometries to visualize the snapping result. Used for snapping to structures and 
        /// structure features.
        /// </summary>
        IList<IGeometry> VisibleSnaps { get; set; }
    }
}