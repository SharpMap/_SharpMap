using System.Drawing;
using GeoAPI.Geometries;
using SharpMap.Utilities;

namespace SharpMap.Geometries
{
    /// <summary>
    /// Static geometry 
    /// </summary>
    public static class GeometryHelper
    {
        /// <summary>
        /// Transforms the linestring to image coordinates, based on the map
        /// </summary>
        /// <param name="self">The <see cref="ILineString"/> instance to transform the coordinates from</param>
        /// <param name="map">Map to base coordinates on</param>
        /// <returns>Linestring in image coordinates</returns>
        public static PointF[] TransformToImage(this ILineString self, Map map)
        {
            var coordinates = self.Coordinates;
            var v = new PointF[coordinates.Length];
            for (var i = 0; i < coordinates.Length; i++)
                v[i] = Transform.WorldtoMap(coordinates[i], map);
            return v;
        }

        /// <summary>
        /// Transforms the point to image coordinates, based on the map
        /// </summary>
        /// <param name="self"></param>
        /// <param name="map">Map to base coordinates on</param>
        /// <returns>point in image coordinates</returns>
        public static PointF TransformToImage(this IPoint self, Map map)
        {
            return self.Coordinate.TransformToImage(map);
        }

        /// <summary>
        /// Transforms the point to image coordinates, based on the map
        /// </summary>
        /// <param name="self"></param>
        /// <param name="map">Map to base coordinates on</param>
        /// <returns>point in image coordinates</returns>
        public static PointF TransformToImage(this Coordinate self, Map map)
        {
            return Transform.WorldtoMap(self, map);
        }

        /// <summary>
        /// Transforms the polygon to image coordinates, based on the map
        /// </summary>
        /// <param name="map">Map to base coordinates on</param>
        /// <returns>Polygon in image coordinates</returns>
        public static PointF[] TransformToImage(this IPolygon self, Map map)
        {
            var shell = self.ExteriorRing.CoordinateSequence;
            var vertices = shell.Count;
            for (var i = 0; i < self.NumInteriorRings; i++)
                vertices += self.GetInteriorRingN(i).CoordinateSequence.Count;

            var v = new PointF[vertices];
            for (var i = 0; i < shell.Count; i++)
                v[i] = Transform.WorldtoMap(shell.GetCoordinate(i), map);
            
            var j = shell.Count;
            for (var k = 0; k < self.NumInteriorRings; k++)
            {
                var ring = self.GetInteriorRingN(k).CoordinateSequence;
                for (var i = 0; i < ring.Count; i++)
                    v[j + i] = Transform.WorldtoMap(ring.GetCoordinate(i), map);
                j += ring.Count;
            }
            return v;
        }

    }
}