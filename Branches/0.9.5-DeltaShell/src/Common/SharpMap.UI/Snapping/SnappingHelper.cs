using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;
using log4net;
using NetTopologySuite.Extensions.Geometries;
using SharpMap.Converters.Geometries;

namespace SharpMap.UI.Snapping
{
    public static class SnappingHelper
    {
        // todo optimize; prevent creation of unused SnapResult objects
        private static readonly ILog log = LogManager.GetLogger(typeof(SnappingHelper));

        public static void PointSnap(ref ISnapResult snapResult, IGeometry geometry)
        {
            snapResult = new SnapResult(geometry.Coordinates[0], null, geometry, 0, 0);
        }

        public static void LineStringSnapStartEnd(ref double minDistance, ref ISnapResult snapResult, ILineString lineString, ICoordinate worldPos)
        {
            ICoordinate c1 = lineString.Coordinates[0];
            ICoordinate location;
            int snapIndexPrevious;
            int snapIndexNext;
            double distance = GeometryHelper.Distance(c1.X, c1.Y, worldPos.X, worldPos.Y);
            if (distance < minDistance)
            {
                location = c1;
                snapIndexPrevious = 0;
                snapIndexNext = 0;
                minDistance = distance;
                snapResult = new SnapResult(location, null, lineString, snapIndexPrevious, snapIndexNext);
            }
            ICoordinate c2 = lineString.Coordinates[lineString.Coordinates.Length - 1];
            distance = GeometryHelper.Distance(c2.X, c2.Y, worldPos.X, worldPos.Y);
            if (distance >= minDistance) 
                return;
            location = c2;
            snapIndexPrevious = lineString.Coordinates.Length - 1;
            snapIndexNext = lineString.Coordinates.Length - 1;
            snapResult = new SnapResult(location, null, lineString, snapIndexPrevious, snapIndexNext);
        }

        public static void LineStringSnapEnd(ref ISnapResult snapResult, ILineString lineString)
        {
            snapResult = new SnapResult(lineString.Coordinates[lineString.Coordinates.Length - 1], null, lineString,
                                  lineString.Coordinates.Length - 1, lineString.Coordinates.Length - 1);
        }

        public static void LineStringSnapStart(ref ISnapResult snapResult, ILineString lineString)
        {
            snapResult = new SnapResult(lineString.Coordinates[0], null, lineString, 0, 0);
        }

        public static void LineStringSnapAllTrackers(ref double minDistance, ref ISnapResult snapResult, ILineString lineString, ICoordinate worldPos)
        {
            for (int i = 0; i < lineString.Coordinates.Length; i++)
            {
                ICoordinate c1 = lineString.Coordinates[i];
                double distance = GeometryHelper.Distance(c1.X, c1.Y, worldPos.X, worldPos.Y);
                if (distance >= minDistance) 
                    continue;
                minDistance = distance;
                snapResult =  new SnapResult(lineString.Coordinates[i], null, lineString, i, i);
            }
        }

        public static void LineStringSnapFreeAtObject(ref double minDistance, ref ISnapResult snapResult, IFeature feature, ILineString lineString, ICoordinate worldPos)
        {
            int vertexIndex;
            var nearestPoint = GeometryHelper.GetNearestPointAtLine(lineString, worldPos, minDistance, out vertexIndex);

            if (nearestPoint == null)
            {
                return;
            }

            minDistance = GeometryHelper.Distance(nearestPoint.X, nearestPoint.Y, worldPos.X, worldPos.Y);
            snapResult = new SnapResult(nearestPoint, feature, lineString, vertexIndex - 1, vertexIndex);
        }

        public static void LineStringSnapFree(ref double minDistance, ref ISnapResult snapResult, ILineString lineString, ICoordinate worldPos)
        {
            for (int i = 1; i < lineString.Coordinates.Length; i++)
            {
                ICoordinate c1 = lineString.Coordinates[i - 1];
                ICoordinate c2 = lineString.Coordinates[i];
                double distance = GeometryHelper.LinePointDistance(c1.X, c1.Y, c2.X, c2.Y,
                                                                   worldPos.X, worldPos.Y);
                if (distance >= minDistance) 
                    continue;
                minDistance = distance;
                snapResult = new SnapResult(GeometryFactory.CreateCoordinate(worldPos.X, worldPos.Y), null, lineString, i - 1, i);
            }
        }

        public static void PolygonSnapFreeAtObject(ref double minDistance, ref ISnapResult snapResult, IPolygon polygon, ICoordinate worldPos)
        {
            for (int i = 1; i < polygon.Coordinates.Length; i++)
            {
                ICoordinate c1 = polygon.Coordinates[i - 1];
                ICoordinate c2 = polygon.Coordinates[i];
                double distance = GeometryHelper.LinePointDistance(c1.X, c1.Y, c2.X, c2.Y,
                                                                   worldPos.X, worldPos.Y);
                if (distance >= minDistance) 
                    continue;
                minDistance = distance;
                ICoordinate min_c1 = polygon.Coordinates[i - 1];
                ICoordinate min_c2 = polygon.Coordinates[i];
                snapResult = new SnapResult(GeometryHelper.NearestPointAtSegment(min_c1.X, min_c1.Y,
                                                                           min_c2.X, min_c2.Y, worldPos.X,
                                                                           worldPos.Y), null, polygon, i - 1, i);
            }
        }

        public static void PolygonSnapFree(ref double minDistance, ref ISnapResult snapResult, IPolygon polygon, ICoordinate worldPos)
        {
            for (int i = 1; i < polygon.Coordinates.Length; i++)
            {
                ICoordinate c1 = polygon.Coordinates[i - 1];
                ICoordinate c2 = polygon.Coordinates[i];
                double distance = GeometryHelper.LinePointDistance(c1.X, c1.Y, c2.X, c2.Y, worldPos.X, worldPos.Y);
                if (distance >= minDistance) 
                    continue;
                minDistance = distance;
                snapResult = new SnapResult(GeometryFactory.CreateCoordinate(worldPos.X, worldPos.Y), null, polygon,
                                            i - 1, i);
            }
        }
    }
}