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
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;
using System.Runtime.InteropServices;

namespace SharpMap.Geometries
{
    /// <summary>
    /// A Point is a 0-dimensional geometry and represents a single location in coordinate space. A Point has a x coordinate
    /// value and a y-coordinate value. The boundary of a Point is the empty set.
    /// </summary>
    public class Point : Geometry, IComparable<Point>, IEqualityComparer<Point>
    {
        private double _X;
        private double _Y;
		
		/// <summary>
		/// Initializes a new Point
		/// </summary>
		/// <param name="x">X coordinate</param>
		/// <param name="y">Y coordinate</param>
        public Point(double x, double y)
        {
            _X = x; _Y = y;
        }

		/// <summary>
		/// Initializes a new Point at (0,0)
		/// </summary>
		public Point() : this(0, 0) {  }

		/// <summary>
		/// Gets or sets the X coordinate of the point
		/// </summary>
        public double X
        {
            get { return _X; }
            set { _X = value; }
        }

		/// <summary>
		/// Gets or sets the Y coordinate of the point
		/// </summary>
		public double Y
        {
            get { return _Y; }
            set { _Y = value; }
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
				if (index == 0)
					return this.X;
				else if
					(index == 1)
					return this.Y;
				else
					throw (new System.Exception("Point index out of bounds"));
			}
			set
			{
				if (index == 0)
					this.X = value;
				else if (index == 1)
					this.Y = value;
				else
					throw (new System.Exception("Point index out of bounds"));
			}
		}
		/// <summary>
		/// Returns the number of ordinates for this point
		/// </summary>
		public virtual int NumOrdinates
		{
			get { return 2; }
        }


		/// <summary>
		/// Transforms the point to image coordinates, based on the map
		/// </summary>
		/// <param name="map">Map to base coordinates on</param>
		/// <returns>point in image coordinates</returns>
        public Point WorldToMap(IMapTransform transform)
		{
			return transform.WorldToMap(this);
		}

		#region Operators
		/// <summary>
		/// Vector + Vector
		/// </summary>
		/// <param name="v1">Vector</param>
		/// <param name="v2">Vector</param>
		/// <returns></returns>
		public static Point operator +(Point v1, Point v2)
		{ return new Point(v1.X + v2.X, v1.Y + v2.Y); }


		/// <summary>
		/// Vector - Vector
		/// </summary>
		/// <param name="v1">Vector</param>
		/// <param name="v2">Vector</param>
		/// <returns>Cross product</returns>
		public static Point operator -(Point v1, Point v2)
		{ return new Point(v1.X - v2.X, v1.Y - v2.Y); }

		/// <summary>
		/// Vector * Scalar
		/// </summary>
		/// <param name="m">Vector</param>
		/// <param name="d">Scalar (double)</param>
		/// <returns></returns>
		public static Point operator *(Point m, double d)
		{ return new Point(m.X * d, m.Y * d); }

		/// <summary>
		/// Vector + Scalar (translation)
		/// </summary>
		/// <param name="m">Vector</param>
		/// <param name="d">Scalar (double)</param>
		/// <returns></returns>
		public static Point operator +(Point m, double d)
		{ return new Point(m.X + d, m.Y + d); }

#endregion

        #region "Inherited methods from abstract class Geometry"

		/// <summary>
		/// Checks whether this instance is spatially equal to the Point 'o'
		/// </summary>
		/// <param name="p">Point to compare to</param>
		/// <returns></returns>
		public bool Equals(Point p)
		{
			return p.X == _X && p.Y == _Y;
		}

		/// <summary>
		///  The inherent dimension of this Geometry object, which must be less than or equal to the coordinate dimension.
		/// </summary>
		public override int Dimension
        {
            get { return 0; }
        }

		/// <summary>
		/// If true, then this Geometry represents the empty point set, �, for the coordinate space. 
		/// </summary>
		/// <returns>Returns 'true' if this Geometry is the empty geometry</returns>
		public override bool IsEmpty()
        {
            throw new NotImplementedException();
        }

		/// <summary>
		///  Returns 'true' if this Geometry has no anomalous geometric points, such as self
		/// intersection or self tangency. The description of each instantiable geometric class will include the specific
		/// conditions that cause an instance of that class to be classified as not simple.
		/// </summary>
		/// <returns>true if the geometry is simple</returns>
		public override bool IsSimple()
        {
            throw new NotImplementedException();
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
			if (geom.GetType() == typeof(SharpMap.Geometries.Point))
			{
				Point p = geom as Point;
				return Math.Sqrt(Math.Pow(this.X - p.X,2) + Math.Pow(this.Y - p.Y,2));
			}
			else
	            throw new Exception("The method or operation is not implemented for this geometry type.");
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
			return new BoundingBox(this.X,this.Y,this.X,this.Y);
		}

#endregion

		/// <summary>
		/// This method must be overridden using 'public new [derived_data_type] Clone()'
		/// </summary>
		/// <returns>Clone</returns>
		public new Point Clone()
		{
			return new Point(this.X, this.Y);
		}

		#region IComparable<Point> Members

		/// <summary>
		/// Comparator used for ordering point first by ascending X, then by ascending Y.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public int CompareTo(Point other)
		{
			if (this.X < other.X || this.X == other.X && this.Y < other.Y)
				return -1;
			else if (this.X > other.X || this.X == other.X && this.Y > other.Y)
				return 1;
			else// (this.X == other.X && this.Y == other.Y)
				return 0;
		}

		#endregion

		#region IEqualityComparer<Point> Members

		/// <summary>
		/// Checks whether the two points are spatially equal
		/// </summary>
		/// <param name="p1">Point 1</param>
		/// <param name="p2">Point 2</param>
		/// <returns>true if the points a spatially equal</returns>
		public bool Equals(Point p1, Point p2)
		{
			return (p1.X==p2.X && p1.Y==p2.Y);
		}

		/// <summary>
		/// Returns a hash code for the specified point
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public int GetHashCode(Point obj)
		{
			return this.AsBinary().GetHashCode();
		}

		#endregion
	}
}
