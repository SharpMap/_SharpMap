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

namespace SharpMap.Geometries
{
	/// <summary>
	/// A LineString is a Curve with linear interpolation between points. Each consecutive pair of points defines a
	/// line segment.
	/// </summary>
	[Serializable]
	public class LineString : Curve
	{
		private List<Point> _Vertices;

		/// <summary>
		/// Initializes an instance of a LineString from a set of vertices
		/// </summary>
		/// <param name="vertices"></param>
		public LineString(List<Point> vertices)
		{
			_Vertices = vertices;
		}

		/// <summary>
		/// Initializes an instance of a LineString
		/// </summary>
		public LineString() : this(new List<Point>()) { }

		/// <summary>
		/// Gets or sets the collection of vertices in this Geometry
		/// </summary>
		public virtual List<Point> Vertices
		{
			get { return _Vertices; }
			set { _Vertices = value; }
		}

		#region OpenGIS Methods

		/// <summary>
		/// Returns the specified point N in this Linestring.
		/// </summary>
		/// <remarks>This method is supplied as part of the OpenGIS Simple Features Specification</remarks>
		/// <param name="N"></param>
		/// <returns></returns>
		public Point Point(int N)
		{
			return _Vertices[N];
		}

		/// <summary>
		/// The number of points in this LineString.
		/// </summary>
		/// <remarks>This method is supplied as part of the OpenGIS Simple Features Specification</remarks>
		public virtual int NumPoints
		{
			get { return _Vertices.Count; }
		}
		#endregion
		
		/// <summary>
		/// Transforms the linestring to image coordinates, based on the map
		/// </summary>
		/// <param name="map">Map to base coordinates on</param>
		/// <returns>Linestring in image coordinates</returns>
		public System.Drawing.PointF[] TransformToImage(Map map)
		{
			System.Drawing.PointF[] v = new System.Drawing.PointF[_Vertices.Count];
			for (int i = 0; i < this.Vertices.Count; i++)
				v[i] = SharpMap.Utilities.Transform.WorldtoMap(_Vertices[i], map);
			return v;
		}	


		#region "Inherited methods from abstract class Geometry"

		/// <summary>
		/// Checks whether this instance is spatially equal to the LineString 'l'
		/// </summary>
		/// <param name="l">LineString to compare to</param>
		/// <returns>true of the objects are spatially equal</returns>
		public bool Equals(LineString l)
		{
			if (l == null)
				return false;
			if (l.Vertices.Count != this.Vertices.Count)
				return false;
			for(int i = 0; i < l.Vertices.Count; i++)
				if (!l.Vertices[i].Equals(this.Vertices[i]))
					return false;
			return true;
		}

		/// <summary>
		/// Serves as a hash function for a particular type. <see cref="GetHashCode"/> is suitable for use 
		/// in hashing algorithms and data structures like a hash table.
		/// </summary>
		/// <returns>A hash code for the current <see cref="GetHashCode"/>.</returns>
		public override int GetHashCode()
		{
			int hash = 0;
			for (int i = 0; i < Vertices.Count; i++)
				hash = hash ^ Vertices[i].GetHashCode();
			return hash;
		}

		/// <summary>
		/// If true, then this Geometry represents the empty point set, �, for the coordinate space. 
		/// </summary>
		/// <returns>Returns 'true' if this Geometry is the empty geometry</returns>
		public override bool IsEmpty()
		{
			return _Vertices == null || _Vertices.Count == 0;
		}

		/// <summary>
		///  Returns 'true' if this Geometry has no anomalous geometric points, such as self
		/// intersection or self tangency. The description of each instantiable geometric class will include the specific
		/// conditions that cause an instance of that class to be classified as not simple.
		/// </summary>
		/// <returns>true if the geometry is simple</returns>
		public override bool IsSimple()
		{
			List<Point> verts = new List<Point>(_Vertices.Count);
			for (int i = 0; i < _Vertices.Count;i++ )
				if (!verts.Exists(delegate(SharpMap.Geometries.Point p) { return p.Equals(_Vertices[i]);}))
					verts.Add(_Vertices[i]);
			return (verts.Count == this._Vertices.Count-(this.IsClosed?1:0));
		}

		/// <summary>
		/// Returns the closure of the combinatorial boundary of this Geometry. The
		/// combinatorial boundary is defined as described in section 3.12.3.2 of [1]. Because the result of this function
		/// is a closure, and hence topologically closed, the resulting boundary can be represented using
		/// representational geometry primitives
		/// </summary>
		/// <returns>Closure of the combinatorial boundary of this Geometry</returns>
		public override Geometry Boundary()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Returns the shortest distance between any two points in the two geometries
		/// as calculated in the spatial reference system of this Geometry.
		/// </summary>
		/// <param name="geom">Geometry to calculate distance to</param>
		/// <returns>Shortest distance between any two points in the two geometries</returns>
		public override double Distance(Geometry geom)
		{
			throw new NotImplementedException();
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

		#endregion

		/// <summary>
		/// Returns the vertice where this Geometry begins
		/// </summary>
		/// <returns>First vertice in LineString</returns>
		public override Point StartPoint
		{
			get
			{
				if (_Vertices.Count == 0)
					throw new ApplicationException("No startpoint found: LineString has no vertices.");
				return this._Vertices[0];
			}
		}

		/// <summary>
		/// Gets the vertice where this Geometry ends
		/// </summary>
		/// <returns>Last vertice in LineString</returns>
		public override Point EndPoint
		{
			get {
				if (_Vertices.Count == 0)
					throw new ApplicationException("No endpoint found: LineString has no vertices.");
				return _Vertices[_Vertices.Count - 1];
			}
		}

		/// <summary>
		/// Returns true if this LineString is closed and simple
		/// </summary>
		public override bool IsRing
		{
			get { return (this.IsClosed && this.IsSimple()); }
		}

		/// <summary>
		/// The length of this LineString, as measured in the spatial reference system of this LineString.
		/// </summary>
		public override double Length
		{
			get {
				if (this.Vertices.Count < 2)
					return 0;
				double sum = 0;
				for(int i = 1; i < this.Vertices.Count; i++)
					sum += this.Vertices[i].Distance(this.Vertices[i-1]);
				return sum;
			}
		}

		/// <summary>
		/// The position of a point on the line, parameterised by length.
		/// </summary>
		/// <param name="t">Distance down the line</param>
		/// <returns>Point at line at distance t from StartPoint</returns>
		public override Point Value(double t)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// The minimum bounding box for this Geometry.
		/// </summary>
		/// <returns>BoundingBox for this geometry</returns>
		public override BoundingBox GetBoundingBox()
		{
			if (this.Vertices == null || this.Vertices.Count == 0)
				return null;
			BoundingBox bbox = new BoundingBox(this.Vertices[0], this.Vertices[0]);
			for (int i = 1; i < this.Vertices.Count; i++)
			{
				bbox.Min.X = this.Vertices[i].X < bbox.Min.X ? this.Vertices[i].X : bbox.Min.X;
				bbox.Min.Y = this.Vertices[i].Y < bbox.Min.Y ? this.Vertices[i].Y : bbox.Min.Y;
				bbox.Max.X = this.Vertices[i].X > bbox.Max.X ? this.Vertices[i].X : bbox.Max.X;
				bbox.Max.Y = this.Vertices[i].Y > bbox.Max.Y ? this.Vertices[i].Y : bbox.Max.Y;
			}
			return bbox;
		}

		#region ICloneable Members

		/// <summary>
		/// Return a copy of this geometry
		/// </summary>
		/// <returns>Copy of Geometry</returns>
		public new LineString Clone()
		{
			LineString l = new LineString();
			for (int i = 0; i < _Vertices.Count;i++ )
				l.Vertices.Add(_Vertices[i].Clone());
			return l;
		}

		#endregion
	}
}
