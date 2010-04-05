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
#if !CFBuild //No Serialization support in CF
using System.Runtime.Serialization; 
#endif
using System.Runtime.InteropServices;

namespace SharpMap.Geometries
{
    /// <summary>
    /// A Point is a 0-dimensional geometry and represents a single location in 3D coordinate space. A Point has a x coordinate
	/// value, a y-coordinate value and a z-coordinate value. The boundary of a Point3D is the empty set.
    /// </summary>
    public class Point3D : Point
    {
        private double _Z;
		
		/// <summary>
		/// Initializes a new Point
		/// </summary>
		/// <param name="x">X coordinate</param>
		/// <param name="y">Y coordinate</param>
		/// <param name="z">Z coordinate</param>
        public Point3D(double x, double y, double z) : base(x,y)
        {
            _Z = z;
        }

		/// <summary>
		/// Initializes a new Point at (0,0)
		/// </summary>
		public Point3D() : this(0, 0, 0) {  }
		
		/// <summary>
		/// Gets or sets the Z coordinate of the point
		/// </summary>
		public double Z
        {
            get { return _Z; }
            set { _Z = value; }
        }
		/// <summary>
		/// Returns part of coordinate. Index 0 = X, Index 1 = Y, , Index 2 = Z
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public override double this[uint index]
		{
			get
			{
				if (index == 2)
					return this.Z;
				else 
					return base[index];
			}
			set
			{
				if (index == 2)
					this.Z = value;
				else base[index] = value;
			}

		}
		/// <summary>
		/// Returns the number of ordinates for this point
		/// </summary>
		public override int NumOrdinates
		{
			get { return 3; }
		}
		#region Operators
		/// <summary>
		/// Vector + Vector
		/// </summary>
		/// <param name="v1">Vector</param>
		/// <param name="v2">Vector</param>
		/// <returns></returns>
		public static Point3D operator +(Point3D v1, Point3D v2)
		{ return new Point3D(v1.X + v2.X, v1.Y + v2.Y, v1.Z+v2.Z); }


		/// <summary>
		/// Vector - Vector
		/// </summary>
		/// <param name="v1">Vector</param>
		/// <param name="v2">Vector</param>
		/// <returns>Cross product</returns>
		public static Point3D operator -(Point3D v1, Point3D v2)
		{ return new Point3D(v1.X - v2.X, v1.Y - v2.Y, v1.Z - v2.Z); }

		/// <summary>
		/// Vector * Scalar
		/// </summary>
		/// <param name="m">Vector</param>
		/// <param name="d">Scalar (double)</param>
		/// <returns></returns>
		public static Point3D operator *(Point3D m, double d)
		{ return new Point3D(m.X * d, m.Y * d, m.Z * d); }

		/// <summary>
		/// Vector + Scalar (translation)
		/// </summary>
		/// <param name="m">Vector</param>
		/// <param name="d">Scalar (double)</param>
		/// <returns></returns>
		public static Point3D operator +(Point3D m, double d)
		{ return new Point3D(m.X + d, m.Y + d, m.Z + d); }

#endregion

        #region "Inherited methods from abstract class Geometry"

		/// <summary>
		/// Checks whether this instance is spatially equal to the Point 'o'
		/// </summary>
		/// <param name="p">Point to compare to</param>
		/// <returns></returns>
		public bool Equals(Point3D p)
		{
			return p.X == X && p.Y == Y && p.Z == _Z;
		}

		/// <summary>
		/// Returns the distance between this geometry instance and another geometry, as
		/// measured in the spatial reference system of this instance.
		/// </summary>
		/// <param name="geom"></param>
		/// <returns></returns>
        public override double Distance(Geometry geom)
        {
			if (geom.GetType() == typeof(SharpMap.Geometries.Point3D))
			{
				Point3D p = geom as Point3D;
				return Math.Sqrt(Math.Pow(this.X - p.X, 2) + Math.Pow(this.Y - p.Y, 2) + Math.Pow(this.Z - p.Z, 2));
			}
			else
				return base.Distance(geom);
        }

#endregion

		/// <summary>
		/// This method must be overridden using 'public new [derived_data_type] Clone()'
		/// </summary>
		/// <returns>Clone</returns>
		public new Point3D Clone()
		{
			return new Point3D(this.X, this.Y, this.Z);
		}

		#region IEqualityComparer<Point3D> Members

		/// <summary>
		/// Checks whether the two points are spatially equal
		/// </summary>
		/// <param name="p1">Point 1</param>
		/// <param name="p2">Point 2</param>
		/// <returns>true if the points a spatially equal</returns>
		public bool Equals(Point3D p1, Point3D p2)
		{
			return (p1.X==p2.X && p1.Y==p2.Y && p1.Z==p2.Z);
		}

		#endregion
	}
}
