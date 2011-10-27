// Copyright 2005, 2006 - Morten Nielsen (www.iter.dk)
//
// This file is part of SharpMap.
// SharpMap is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
// 
// SharpMap is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.

// You should have received a copy of the GNU Lesser General Public License
// along with SharpMap; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA 

using System;
using System.Drawing;
using SharpMap.Utilities;

namespace SharpMap.Geometries
{
    /// <summary>
    /// A Point is a 0-dimensional geometry and represents a single location in 2D coordinate space. A Point has a x coordinate
    /// value and a y-coordinate value. The boundary of a Point is the empty set.
    /// </summary>
    [Serializable]
    public class Point : Geometry, IComparable<Point>, IPuntal
    {
        //private bool _isEmpty;
        private double _x;
        private double _y;

        /// <summary>
        /// Null ordinate value
        /// </summary>
        public static readonly double NullOrdinate = Double.NaN;

        /// <summary>
        /// Initializes a new Point
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        public Point(double x, double y)
        {
            _x = x;
            _y = y;
        }

        /// <summary>
        /// Initializes a new empty Point
        /// </summary>
        public Point() : this(NullOrdinate, NullOrdinate) //: this(0d, 0d)
        {
            //_isEmpty = true;
        }

        /// <summary>
        /// Create a new point by a douuble[] array
        /// </summary>
        /// <param name="point"></param>
        public Point(double[] point)
        {
            if (point.Length < 2)
                throw new Exception("Only 2 dimensions are supported for points");

            _x = point[0];
            _y = point[1];
        }

        /// <summary>
        /// Sets whether this object is empty
        /// </summary>
        protected bool SetIsEmpty
        {
            set
            { 
                if (value)
                    _x = NullOrdinate;// _isEmpty = value;
                else
                {
                    _x = 0d;
                    _y = 0d;
                }
            }
        }

        /// <summary>
        /// Gets or sets the X coordinate of the point
        /// </summary>
        public double X
        {
            get
            {
                if (!IsEmptyPoint)
                    return _x;
                throw new ApplicationException("Point is empty");
            }
            set
            {
                _x = value;
                //_isEmpty = false;
            }
        }

        /// <summary>
        /// Gets or sets the Y coordinate of the point
        /// </summary>
        public double Y
        {
            get
            {
                if (!IsEmptyPoint)
                    return _y;
                throw new ApplicationException("Point is empty");
            }
            set
            {
                _y = value;
                //_isEmpty = false;
            }
        }

        /// <summary>
        /// Returns part of coordinate. Index 0 = X, Index 1 = Y
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public virtual double this[uint index]
        {
            get
            {
                if (IsEmptyPoint)
                    throw new ApplicationException("Point is empty");
                if (index == 0)
                    return X;
                if
                    (index == 1)
                    return Y;
                throw (new Exception("Point index out of bounds"));
            }
            set
            {
                if (index == 0)
                    X = value;
                else if (index == 1)
                    Y = value;
                else
                    throw (new Exception("Point index out of bounds"));
                //_isEmpty = false;
            }
        }

        /// <summary>
        /// Returns the number of ordinates for this point
        /// </summary>
        public virtual int NumOrdinates
        {
            get { return 2; }
        }

        #region IComparable<Point> Members

        /// <summary>
        /// Comparator used for ordering point first by ascending X, then by ascending Y.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public virtual int CompareTo(Point other)
        {
            if (X < other.X || X == other.X && Y < other.Y)
                return -1;
            if (X > other.X || X == other.X && Y > other.Y)
                return 1;
            return 0;
        }

        #endregion

        /// <summary>
        /// exports a point into a 2-dimensional double array
        /// </summary>
        /// <returns></returns>
        public double[] ToDoubleArray()
        {
            return new[] {_x, _y};
        }

        /// <summary>
        /// Returns a point based on degrees, minutes and seconds notation.
        /// For western or southern coordinates, add minus '-' in front of all longitude and/or latitude values
        /// </summary>
        /// <param name="longDegrees">Longitude degrees</param>
        /// <param name="longMinutes">Longitude minutes</param>
        /// <param name="longSeconds">Longitude seconds</param>
        /// <param name="latDegrees">Latitude degrees</param>
        /// <param name="latMinutes">Latitude minutes</param>
        /// <param name="latSeconds">Latitude seconds</param>
        /// <returns>Point</returns>
        public static Point FromDMS(double longDegrees, double longMinutes, double longSeconds,
                                    double latDegrees, double latMinutes, double latSeconds)
        {
            return new Point(longDegrees + longMinutes/60 + longSeconds/3600,
                             latDegrees + latMinutes/60 + latSeconds/3600);
        }

        /// <summary>
        /// Returns a 2D <see cref="Point"/> instance from this <see cref="Point3D"/>
        /// </summary>
        /// <returns><see cref="Point"/></returns>
        public Point AsPoint()
        {
            return new Point(_x, _y);
        }

        /// <summary>
        /// Transforms the point to image coordinates, based on the map
        /// </summary>
        /// <param name="map">Map to base coordinates on</param>
        /// <returns>point in image coordinates</returns>
        public PointF TransformToImage(Map map)
        {
            return Transform.WorldtoMap(this, map);
        }

        /// <summary>
        /// This method must be overridden using 'public new [derived_data_type] Clone()'
        /// </summary>
        /// <returns>Clone</returns>
        public new Point Clone()
        {
            return new Point(X, Y);
        }

        #region Operators

        /// <summary>
        /// Vector + Vector
        /// </summary>
        /// <param name="v1">Vector</param>
        /// <param name="v2">Vector</param>
        /// <returns></returns>
        public static Point operator +(Point v1, Point v2)
        {
            return new Point(v1.X + v2.X, v1.Y + v2.Y);
        }


        /// <summary>
        /// Vector - Vector
        /// </summary>
        /// <param name="v1">Vector</param>
        /// <param name="v2">Vector</param>
        /// <returns>Cross product</returns>
        public static Point operator -(Point v1, Point v2)
        {
            return new Point(v1.X - v2.X, v1.Y - v2.Y);
        }

        /// <summary>
        /// Vector * Scalar
        /// </summary>
        /// <param name="m">Vector</param>
        /// <param name="d">Scalar (double)</param>
        /// <returns></returns>
        public static Point operator *(Point m, double d)
        {
            return new Point(m.X*d, m.Y*d);
        }

        #endregion

        #region "Inherited methods from abstract class Geometry"

        public override GeometryType2 GeometryType
        {
            get
            {
                return GeometryType2.Point;
            }
        }

        /// <summary>
        ///  The inherent dimension of this Geometry object, which must be less than or equal to the coordinate dimension.
        /// </summary>
        public override int Dimension
        {
            get { return 0; }
        }

        /// <summary>
        /// Checks whether this instance is spatially equal to the Point 'o'
        /// </summary>
        /// <param name="p">Point to compare to</param>
        /// <returns></returns>
        public virtual bool Equals(Point p)
        {
            return p != null && p.X == _x && p.Y == _y && IsEmptyPoint == p.IsEmptyPoint;
        }

        /// <summary>
        /// Serves as a hash function for a particular type. <see cref="GetHashCode"/> is suitable for use 
        /// in hashing algorithms and data structures like a hash table.
        /// </summary>
        /// <returns>A hash code for the current <see cref="GetHashCode"/>.</returns>
        public override int GetHashCode()
        {
            return _x.GetHashCode() ^ _y.GetHashCode() ^ IsEmptyPoint.GetHashCode();
        }

        protected bool IsEmptyPoint { get { return double.IsNaN(_x) || double.IsNaN(_y); } }

        /// <summary>
        /// If true, then this Geometry represents the empty point set, �, for the coordinate space. 
        /// </summary>
        /// <returns>Returns 'true' if this Geometry is the empty geometry</returns>
        public override bool IsEmpty()
        {
            return IsEmptyPoint;
        }

        /// <summary>
        /// Returns 'true' if this Geometry has no anomalous geometric points, such as self
        /// intersection or self tangency. The description of each instantiable geometric class will include the specific
        /// conditions that cause an instance of that class to be classified as not simple.
        /// </summary>
        /// <returns>true if the geometry is simple</returns>
        public override bool IsSimple()
        {
            return true;
        }

        /// <summary>
        /// The boundary of a point is the empty set.
        /// </summary>
        /// <returns>null</returns>
        public override Geometry Boundary()
        {
            return null;
        }

        /// <summary>
        /// Returns the distance between this geometry instance and another geometry, as
        /// measured in the spatial reference system of this instance.
        /// </summary>
        /// <param name="geom"></param>
        /// <returns></returns>
        public override double Distance(Geometry geom)
        {
            if (geom.GetType() == typeof (Point))
            {
                var p = geom as Point;
                return Math.Sqrt(Math.Pow(X - p.X, 2) + Math.Pow(Y - p.Y, 2));
            }
            else if (geom is LineString)
            {
                return geom.Distance(this);
            }
            else if (geom is MultiLineString)
            {
                return geom.Distance(this);
            }
            else
                throw new Exception("The method or operation is not implemented for this geometry type.");
        }

        /// <summary>
        /// Returns the distance between this point and a <see cref="BoundingBox"/>
        /// </summary>
        /// <param name="box"></param>
        /// <returns></returns>
        public double Distance(BoundingBox box)
        {
            return box.Distance(this);
        }

        /// <summary>
        /// Returns a geometry that represents all points whose distance from this Geometry
        /// is less than or equal to distance. Calculations are in the Spatial Reference
        /// System of this Geometry.
        /// </summary>
        /// <param name="d">Buffer distance</param>
        /// <returns>Buffer around geometry</returns>
        public override Geometry Buffer(double d)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Geometry�Returns a geometry that represents the convex hull of this Geometry.
        /// </summary>
        /// <returns>The convex hull</returns>
        public override Geometry ConvexHull()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns a geometry that represents the point set intersection of this Geometry
        /// with anotherGeometry.
        /// </summary>
        /// <param name="geom">Geometry to intersect with</param>
        /// <returns>Returns a geometry that represents the point set intersection of this Geometry with anotherGeometry.</returns>
        public override Geometry Intersection(Geometry geom)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns a geometry that represents the point set union of this Geometry with anotherGeometry.
        /// </summary>
        /// <param name="geom">Geometry to union with</param>
        /// <returns>Unioned geometry</returns>
        public override Geometry Union(Geometry geom)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns a geometry that represents the point set difference of this Geometry with anotherGeometry.
        /// </summary>
        /// <param name="geom">Geometry to compare to</param>
        /// <returns>Geometry</returns>
        public override Geometry Difference(Geometry geom)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns a geometry that represents the point set symmetric difference of this Geometry with anotherGeometry.
        /// </summary>
        /// <param name="geom">Geometry to compare to</param>
        /// <returns>Geometry</returns>
        public override Geometry SymDifference(Geometry geom)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// The minimum bounding box for this Geometry.
        /// </summary>
        /// <returns></returns>
        public override BoundingBox GetBoundingBox()
        {
            return new BoundingBox(X, Y, X, Y);
        }

        /// <summary>
        /// Checks whether this point touches a <see cref="BoundingBox"/>
        /// </summary>
        /// <param name="box">box</param>
        /// <returns>true if they touch</returns>
        public bool Touches(BoundingBox box)
        {
            return box.Touches(this);
        }

        /// <summary>
        /// Checks whether this point touches another <see cref="Geometry"/>
        /// </summary>
        /// <param name="geom">Geometry</param>
        /// <returns>true if they touch</returns>
        public override bool Touches(Geometry geom)
        {
            if (geom is Point && Equals(geom)) return true;
            throw new NotImplementedException("Touches not implemented for this feature type");
        }

        /// <summary>
        /// Checks whether this point intersects a <see cref="BoundingBox"/>
        /// </summary>
        /// <param name="box">Box</param>
        /// <returns>True if they intersect</returns>
        public bool Intersects(BoundingBox box)
        {
            return box.Contains(this);
        }

        /// <summary>
        /// Returns true if this instance contains 'geom'
        /// </summary>
        /// <param name="geom">Geometry</param>
        /// <returns>True if geom is contained by this instance</returns>
        public override bool Contains(Geometry geom)
        {
            return false;
        }

        #endregion
    }
}