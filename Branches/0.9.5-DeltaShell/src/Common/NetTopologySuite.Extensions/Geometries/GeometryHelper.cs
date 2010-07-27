using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DelftTools.Functions.Generic;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GisSharpBlog.NetTopologySuite.Geometries;

namespace NetTopologySuite.Extensions.Geometries
{
    public static class GeometryHelper
    {
        // TODO: check if we can use IGeometry methods here (OGC)

        /// <summary>
        /// The SharpMap implementation of Collection<IGeometry>.IndexOf(IGeometry) will use  
        /// bool Geometry::Equals(IGeometry g) which returns true if geometries are of equal shape.
        /// In many cases this is not desired behaviour. For example if we are looking for a BranchSegmentBoundary
        /// it is possible the Node at the same location is returned.
        /// IndexOfGeometry only compares the geometry references
        /// 
        /// NOTE: performance tips: 
        /// If possible minimize calls to 
        ///  - IGeometry.Coordinates converts an internal ICoordinateSequence to an ICoordinate array
        ///    not expensive as such but may be called many times: 
        ///  - ILineString.Length is expensive; move outsize loops as much as possible
        /// </summary>
        /// <param name="geometries"></param>
        /// <param name="geometry"></param>
        /// <returns></returns>
        static public int IndexOfGeometry(IList<IGeometry> geometries, IGeometry geometry)
        {
            for (int i = 0; i < geometries.Count; i++)
            {
                if (ReferenceEquals(geometries[i], geometry))
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Return the coordinate of the point nearest worldPos at lineString.
        /// TODO: Is it distance along the polyline????!!! NAME IT CORRECTLY
        /// TODO: can we merge it with the next method?
        /// </summary>
        /// <param name="lineString"></param>
        /// <param name="worldPos"></param>
        /// <returns></returns>
        public static double Distance(ILineString lineString, ICoordinate worldPos)
        {
            ICoordinate min_c1 = null;
            ICoordinate min_c2 = null;
            double minDistance = Double.MaxValue;
            ICoordinate location;
            // Distance along linestring

            int index = -1;
            ICoordinate c1;
            ICoordinate c2;
            double pointDistance = 0;

            location = null;
            ICoordinate[] coordinates = lineString.Coordinates;
            for (int i = 1; i < coordinates.Length; i++)
            {
                c1 = coordinates[i - 1];
                c2 = coordinates[i];
                double distance = LinePointDistance(c1.X, c1.Y, c2.X, c2.Y, worldPos.X, worldPos.Y);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    min_c1 = c1;
                    min_c2 = c2;
                    index = i;
                }
            }
            if (-1 != index)
            {
                location = NearestPointAtSegment(min_c1.X, min_c1.Y,
                                                 min_c2.X, min_c2.Y, worldPos.X, worldPos.Y);
                for (int i = 1; i < index; i++)
                {
                    c1 = coordinates[i - 1];
                    c2 = coordinates[i];
                    pointDistance += Distance(c1.X, c1.Y, c2.X, c2.Y);
                }
                pointDistance += Distance(min_c1.X, min_c1.Y, location.X, location.Y);
            }
            return pointDistance;
        }

        /// <summary>
        /// returns the minimum distance of geometry to linestring
        /// </summary>
        /// <param name="lineString"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        static public double Distance(ILineString lineString, IGeometry geometry)
        {
            double minDistance = Double.MaxValue;
            ICoordinate c1;
            ICoordinate c2;
            ICoordinate[] coordinates = lineString.Coordinates;

            if (geometry is IPoint)
            {
                var point = (IPoint)geometry;
                for (int i = 1; i < coordinates.Length; i++)
                {
                    double distance;

                    c1 = coordinates[i - 1];
                    c2 = coordinates[i];
                    distance = LinePointDistance(c1.X, c1.Y, c2.X, c2.Y, point.X, point.Y);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                    }
                }
            }
            else if (geometry is ILineString)
            {
                return LineStringFirstIntersectionOffset(lineString, (ILineString)geometry);
            }
            else
            {
                return lineString.Distance(geometry);

/* TODO: check if above code doesn't work good for line string - use trickery trick below 
                // trickery trick someone changed cross section geometry from point to linestring
                // non geometry based cross sections are represented using a 2 point linestring.
                // Use the center of this linestring 
                IPoint crossSectionCenter = GeometryFactory.CreatePoint(CrossSectionHelper.CrossSectionCoordinate(crossSection));
                double distance = GeometryHelper.Distance((ILineString)branch.Geometry, crossSectionCenter);

                float limit;

                if (map != null)
                {
                    limit = (float)MapControlHelper.ImageToWorld(map, 1);
                }
                else
                {
                    limit = (float)(0.1 * Math.Max(branch.Geometry.EnvelopeInternal.Width, branch.Geometry.EnvelopeInternal.Height));
                }


                if (distance < limit)
                {
                    crossSection.Branch = branch;
                    CalculateCrossSectionOffset(crossSection);
                    CrossSectionHelper.UpdateDefaultGeometry(crossSection, crossSection.Geometry.Length / 2);
                }
*/

            }

            return minDistance;
        }

        /// <summary>
        /// Check if a geometry exists in a collection. See also GeometryHelper::IndexOfGeometry
        /// </summary>
        /// <param name="geometries"></param>
        /// <param name="geometry"></param>
        /// <returns></returns>
        static public bool ContainsGeometry(IList<IGeometry> geometries, IGeometry geometry)
        {
            return (IndexOfGeometry(geometries, geometry) != -1) ? true : false;
        }

        //http://www.topcoder.com/tc?module=Static&d1=tutorials&d2=geometry1
        //Line-Point Distance = (AB x AC)/|AB|.
        //    //Compute the distance from A to B
        //    double distance(int[] A, int[] B){
        //        int d1 = A[0] - B[0];
        //        int d2 = A[1] - B[1];
        //        return sqrt(d1*d1+d2*d2);
        //    }

        static public double Distance(double x1, double y1, double X2, double Y2)
        {
            return Math.Sqrt((x1 - X2)*(x1 - X2) + (y1 - Y2)*(y1 - Y2));
        }

        //http://www.topcoder.com/tc?module=Static&d1=tutorials&d2=geometry1
        //Line-Point Distance = (AB x AC)/|AB|.
        //   //Compute the cross product AB x AC
        //    int cross(int[] A, int[] B, int[] C){
        //        AB = new int[2];
        //        AC = new int[2];
        //        AB[0] = B[0]-A[0];
        //        AB[1] = B[1]-A[1];
        //        AC[0] = C[0]-A[0];
        //        AC[1] = C[1]-A[1];
        //        int cross = AB[0] * AC[1] - AB[1] * AC[0];
        //        return cross;
        //    }

        static public double CrossProduct(double Ax, double Ay, double Bx, double By, 
                                          double cx , double cy )
        {
            return (Bx - Ax)*(cy - Ay) - (By - Ay)*(cx - Ax);
        }

        //http://www.topcoder.com/tc?module=Static&d1=tutorials&d2=geometry1
        //Line-Point Distance = (AB x AC)/|AB|.
        ////Compute the dot product AB · BC
        //    int dot(int[] A, int[] B, int[] C){
        //        AB = new int[2];
        //        BC = new int[2];
        //        AB[0] = B[0]-A[0];
        //        AB[1] = B[1]-A[1];
        //        BC[0] = C[0]-B[0];
        //        BC[1] = C[1]-B[1];
        //        int dot = AB[0] * BC[0] + AB[1] * BC[1];
        //        return dot;
        //    }

        static public double Dot(double Ax, double Ay, double Bx, double By, 
                                 double cx, double cy)
        {
            return (Bx - Ax)*(cx - Bx) + (By - Ay)*(cy - By);
        }

        //http://www.topcoder.com/tc?module=Static&d1=tutorials&d2=geometry1
        //Line-Point Distance = (AB x AC)/|AB|.
        ////Compute the distance from AB to C
        //    //if isSegment is true, AB is a segment, not a line.
        //    double linePointDist(int[] A, int[] B, int[] C, boolean isSegment){
        //        double dist = cross(A,B,C) / distance(A,B);
        //        if(isSegment){
        //            int dot1 = dot(A,B,C);
        //            if(dot1 > 0)return distance(B,C);
        //            int dot2 = dot(B,A,C);
        //            if(dot2 > 0)return distance(A,C);
        //        }
        //        return abs(dist);
        //    }

        static public double LinePointDistance(double Ax, double Ay, double Bx, double By, 
                                               double cx, double cy)
        {
            double dist, dot1, dot2;

            dist = Distance(Ax, Ay, Bx, By);
            if (dist < 0.000001)
            {
                return Double.MaxValue;
            }
            dist = CrossProduct(Ax, Ay, Bx, By, cx, cy)/dist;
            // if (isSegment) always true
            dot1 = Dot(Ax, Ay, Bx, By, cx, cy);
            if (dot1 > 0)
                return Distance(Bx, By, cx, cy);
            dot2 = Dot(Bx, By, Ax, Ay, cx, cy);
            if (dot2 > 0)
                return Distance(Ax, Ay, cx, cy);
            return Math.Abs(dist);
        }

        static public ICoordinate NearestPointAtSegment(double Ax, double Ay, double Bx, double By, 
                                                        double cx, double cy)
        {
            // if (AB . BC) > 0) 
            if (Dot(Ax, Ay, Bx, By, cx, cy) > 0)
            {
                return GeometryFactory.CreateCoordinate(Bx, By);
            }
                // else if ((BA . AC) > 0)
            else if (Dot(Bx, By, Ax, Ay, cx, cy) > 0)
            {
                return GeometryFactory.CreateCoordinate(Ax, Ay);
            }
            else
                // both dot products < 0 -> point between A and B
            {
                double AC = Distance(Ax, Ay, cx, cy);
                double BC = Distance(Bx, By, cx, cy);
                return GeometryFactory.CreateCoordinate(Ax + ((AC) / (AC + BC))*(Bx-Ax),
                                                        Ay + ((AC) / (AC + BC)) * (By - Ay));
            }
        }

        public static void SetCoordinate(IGeometry geometry, int coordinateIndex, ICoordinate coordinate)
        {
            geometry.Coordinates[coordinateIndex].X = coordinate.X;
            geometry.Coordinates[coordinateIndex].Y = coordinate.Y;
            geometry.EnvelopeInternal.SetCentre(geometry.Coordinates[coordinateIndex]);
        }

        public static void MoveCoordinate(IGeometry geometry, int coordinateIndex, double deltaX, double deltaY)
        {
            geometry.Coordinates[coordinateIndex].X += deltaX;
            geometry.Coordinates[coordinateIndex].Y += deltaY;
            geometry.EnvelopeInternal.SetCentre(geometry.Coordinates[coordinateIndex]);
        }

        public static void MoveCoordinate(IGeometry targetGeometry, IGeometry sourceGeometry, int coordinateIndex, double deltaX, double deltaY)
        {
            targetGeometry.Coordinates[coordinateIndex].X = sourceGeometry.Coordinates[coordinateIndex].X + deltaX;
            targetGeometry.Coordinates[coordinateIndex].Y = sourceGeometry.Coordinates[coordinateIndex].Y + deltaY;
            targetGeometry.EnvelopeInternal.SetCentre(targetGeometry.Coordinates[coordinateIndex]);
        }

        public static double LineStringGetFraction(ILineString lineString, double distance)
        {
            return distance/lineString.Length;
        }

        public static double LineStringGetFraction(ILineString lineString, int index)
        {
            return LineStringGetDistance(lineString, index) / lineString.Length;
        }

        public static double LineStringGetDistance(ILineString lineString, double fraction)
        {
            return lineString.Length*fraction;
        }

        /// <summary>
        /// returns the offset of coordinate index in the linestring. If index exceeds the 
        /// number of coordinates the length is returned.
        /// </summary>
        /// <param name="lineString"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static double LineStringGetDistance(ILineString lineString, int index)
        {
            double distance = 0;
            ICoordinate[] coordinates = lineString.Coordinates;
            for (int i = 1; i < Math.Min(coordinates.Length, index + 1); i++)
            {
                distance += coordinates[i].Distance(coordinates[i - 1]);
            }
            return distance;
        }

        /// <summary>
        /// Returns the coordinate at an offset of the lineString
        /// </summary>
        /// <param name="lineString"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public static ICoordinate LineStringCoordinate(ILineString lineString, double distance)
        {
            double partialDistance = 0;

            ICoordinate[] coordinates = lineString.Coordinates;
            for (int i = 1; i < coordinates.Length; i++)
            {
                ICoordinate c1 = coordinates[i - 1];
                ICoordinate c2 = coordinates[i];
                double segmentDistance = Distance(c1.X, c1.Y, c2.X, c2.Y);
                if ((partialDistance + segmentDistance) > distance)
                {
                    double factor = (distance - partialDistance)/(segmentDistance);
                    return GeometryFactory.CreateCoordinate(
                        coordinates[i - 1].X + factor * (coordinates[i].X - coordinates[i - 1].X),
                        coordinates[i - 1].Y + factor * (coordinates[i].Y - coordinates[i - 1].Y));
                }
                partialDistance += segmentDistance;
            }
            return (ICoordinate) lineString.Coordinates[lineString.Coordinates.Length-1].Clone();
        }

        /// <summary>
        /// Returns the offset to the first intersection of lineString by cutLineString.
        /// </summary>
        /// <param name="lineString"></param>
        /// <param name="cutLineString"></param>
        /// <returns>offset to the first intersection, -1 if there is no intersection -1</returns>
        public static double LineStringFirstIntersectionOffset(ILineString lineString, ILineString cutLineString)
        {
            if(!lineString.Intersects(cutLineString))
            {
                return -1;
            }

            IGeometry intersection = lineString.Difference(cutLineString);

            if (intersection is IMultiLineString)
            {
                var result = (IMultiLineString) lineString.Difference(cutLineString);
                return result.Geometries[0].Length;
            }
            
            return intersection.Length;
        }

        public static ICoordinate GetNearestPointAtLine(ILineString lineString, ICoordinate coordinate, double tolerance, out int snapVertexIndex)
        {
            snapVertexIndex = -1; 
            ICoordinate nearestPoint = null;

            ICoordinate minC1;
            ICoordinate minC2;

            for (var i = 1; i < lineString.Coordinates.Length; i++)
            {
                var c1 = lineString.Coordinates[i - 1];
                var c2 = lineString.Coordinates[i];
                var distance = LinePointDistance(c1.X, c1.Y, c2.X, c2.Y, coordinate.X, coordinate.Y);

                if (distance >= tolerance)
                {
                    continue;
                }
                tolerance = distance;
                minC1 = c1;
                minC2 = c2;

                nearestPoint = NearestPointAtSegment(minC1.X, minC1.Y, minC2.X, minC2.Y, coordinate.X, coordinate.Y);

                snapVertexIndex = i;
            }

            return nearestPoint;
        }

        public static ICoordinate GetNearestPointAtLine(ILineString geometry, ICoordinate coordinate, double tolerance)
        {
            int snapVertexIndex;
            return GetNearestPointAtLine(geometry, coordinate, tolerance, out snapVertexIndex);
        }

        public static IFeature GetNearestFeature(ICoordinate coordinate, IEnumerable<IFeature> features, double tolerance)
        {
            var minDistance = tolerance;
            IFeature minDistanceFeature = null;

            var point = new Point(coordinate);

            foreach (var feature in features)
            {
                var distance = point.Distance(feature.Geometry);
                if (distance <= minDistance)
                {
                    minDistance = distance;
                    minDistanceFeature = feature;
                }
            }

            return minDistanceFeature;
        }
    }
}