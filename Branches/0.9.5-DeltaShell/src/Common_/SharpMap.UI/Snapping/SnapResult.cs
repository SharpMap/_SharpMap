using System.Collections.Generic;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;
using log4net;

namespace SharpMap.UI.Snapping
{
    public class SnapResult : ISnapResult
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(SnapResult));

        public ICoordinate Location { get; protected set; }
        public int SnapIndexPrevious { get; protected set; }
        public int SnapIndexNext { get; protected set; }
        public IGeometry NearestTarget { get; protected set; }
        public IFeature SnappedFeature { get; protected set; }
        public IList<IGeometry> VisibleSnaps { get; set; }

        public SnapResult(ICoordinate location, IFeature snappedFeature, IGeometry nearestTarget,
                          int snapIndexPrevious, int snapIndexNext)
        {
            Location = location;
            SnapIndexPrevious = snapIndexPrevious;
            SnapIndexNext = snapIndexNext;
            NearestTarget = nearestTarget;
            SnappedFeature = snappedFeature;
            VisibleSnaps = new List<IGeometry>();

            //log.DebugFormat("New snap result created, location:{0}, snapIndexPrevious:{1}, snapIndexNext {2}, nearestTarget:{3}, snappedFeature{4}",
            //   location, snapIndexPrevious, snapIndexNext, nearestTarget, snappedFeature);
        }
    }
}