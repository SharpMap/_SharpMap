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
	/// A MultiPoint is a 0 dimensional geometric collection. The elements of a MultiPoint are
	/// restricted to Points. The points are not connected or ordered.
	/// </summary>
	public class MultiPoint : GeometryCollection
	{
		private List<Point> _Points;

		/// <summary>
		/// Initializes a new MultiPoint collection
		/// </summary>
		public MultiPoint()
		{
			_Points = new System.Collections.Generic.List<Point>();
		}

		/// <summary>
		/// Gets the n'th point in the MultiPoint collection
		/// </summary>
		/// <param name="n">Index in collection</param>
		/// <returns>Point</returns>
		public new Point this[int n]
		{
			get { return _Points[n]; }
		}

		/// <summary>
		/// Gets or sets the MultiPoint collection
		/// </summary>
		public List<Point> Points
		{
			get { return _Points; }
			set { _Points = value; }
		}

		/// <summary>
		/// Returns the number of geometries in the collection.
		/// </summary>
		public override int NumGeometries
		{
			get { return _Points.Count; }
		}

		/// <summary>
		/// Returns an indexed geometry in the collection
		/// </summary>
		/// <param name="N">Geometry index</param>
		/// <returns>Geometry at index N</returns>
		public new Point Geometry(int N)
		{
			return _Points[N];
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
			return (_Points != null && _Points.Count == 0);
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
		/// The boundary of a MultiPoint is the empty set (null).
		/// </summary>
		/// <returns></returns>
		public override Geometry Boundary()
		{
			return null;
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

		/// <summary>
		/// The minimum bounding box for this Geometry.
		/// </summary>
		/// <returns></returns>
		public override BoundingBox GetBoundingBox()
		{
			if (_Points == null || _Points.Count == 0)
				return null;
			BoundingBox bbox = new BoundingBox(_Points[0], _Points[0]);
			for (int i = 1; i < _Points.Count; i++)
			{
				bbox.Min.X = _Points[i].X < bbox.Min.X ? _Points[i].X : bbox.Min.X;
				bbox.Min.Y = _Points[i].Y < bbox.Min.Y ? _Points[i].Y : bbox.Min.Y;
				bbox.Max.X = _Points[i].X > bbox.Max.X ? _Points[i].X : bbox.Max.X;
				bbox.Max.Y = _Points[i].Y > bbox.Max.Y ? _Points[i].Y : bbox.Max.Y;
			}
			return bbox;
		}

		/// <summary>
		/// Return a copy of this geometry
		/// </summary>
		/// <returns>Copy of Geometry</returns>
		public new MultiPoint Clone()
		{
			MultiPoint geoms = new MultiPoint();
			for(int i=0;i<_Points.Count;i++)
				geoms.Points.Add(_Points[i].Clone());
			return geoms;
		}

		#region IEnumerable<Geometry> Members

		/// <summary>
		/// Gets an enumerator for enumerating the geometries in the GeometryCollection
		/// </summary>
		/// <returns></returns>
		public override IEnumerator<Geometry> GetEnumerator()
		{
			foreach (Point p in this._Points)
				yield return p;
		}
		#endregion
	}
}
