using System;
using System.Collections.Generic;
using System.Linq;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;

namespace NetTopologySuite.Extensions.Geometries
{
    public static class GeometryExtensions
    {
        /// <summary>
        /// Returns a clone of the geometry with the sign of the x-coordinate changed
        /// </summary>
        /// <param name="geometry">Geometry to flip</param>
        /// <returns></returns>
        public static IGeometry FlipHorizontal(this IGeometry geometry)
        {
            //create a clone 
            var flipped = (IGeometry)geometry.Clone();
            foreach (var coordinate in flipped.Coordinates)
            {
                coordinate.X = -coordinate.X;
            }
            return flipped;
        }


        /// <summary>
        /// Creates a polygon for given coordinates using coordinates as the edge of the polygon. Close the polygon
        /// by connecting the first and last coordinate.
        /// </summary>
        /// <param name="coordinates"></param>
        /// <returns></returns>
        public static IPolygon ToPolygon(this IEnumerable<ICoordinate> coordinates)
        {
            var list = coordinates.ToList();
            //close the ring if needed
            if (list.Last() != list.First())
            {
                list.Add(list[0]);
            }
            return new Polygon(new LinearRing(list.ToArray()));
        }

        /// <summary>
        /// Creates a lineString from the given coordinates
        /// </summary>
        /// <param name="coordinates"></param>
        /// <returns></returns>
        public static ILineString ToLineString(this IEnumerable<ICoordinate> coordinates)
        {
            return new LineString(coordinates.ToArray());
        }

        public static double GetAngleRad(this LineString lineString)
        {
            return lineString.Angle * Math.PI / 180;
        }

    }
}