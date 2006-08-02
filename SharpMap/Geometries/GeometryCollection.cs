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
	/// A GeometryCollection is a geometry that is a collection of 1 or more geometries.
	/// </summary>
	/// <remarks>
	/// All the elements in a GeometryCollection must be in the same Spatial Reference. This is also the Spatial
	/// Reference for the GeometryCollection.<br/>
	/// GeometryCollection places no other constraints on its elements. Subclasses of GeometryCollection may
	/// restrict membership based on dimension and may also place other constraints on the degree of spatial overlap
	/// between elements.
	/// </remarks>
	public class GeometryCollection : Geometry, IGeometryCollection, IEnumerable<Geometry>
	{
		/// <summary>
		/// Initializes a new GeometryCollection
		/// </summary>
		public GeometryCollection()
		{
			_Geometries = new List<Geometry>();
		}

		/// <summary>
		/// Gets the number of geometries in the collection.
		/// </summary>
		public virtual int NumGeometries { get { return _Geometries.Count; } }

		/// <summary>
		/// Returns an indexed geometry in the collection
		/// </summary>
		/// <param name="N">Geometry index</param>
		/// <returns>Geometry at index N</returns>
		public virtual Geometry Geometry(int N)
		{
			return _Geometries[N];
		}

		private List<Geometry> _Geometries;

		/// <summary>
		/// Returns an indexed geometry in the collection
		/// </summary>
		/// <param name="index">Geometry index</param>
		/// <returns>Geometry</returns>
		public virtual Geometry this[int index]
		{
			get { return _Geometries[index]; }
		}

		/// <summary>
		/// Returns empty of all the geometries are empty or the collection is empty
		/// </summary>
		/// <returns>true of collection is empty</returns>
		public override bool IsEmpty()
		{
			if (_Geometries == null)
				return true;
			for (int i = 0; i < _Geometries.Count;i++ )
				if (!_Geometries[i].IsEmpty())
					return false;
			return true;
		}

		/// <summary>
		/// Determines whether this GeometryCollection is spatially equal to the GeometryCollection 'g'
		/// </summary>
		/// <param name="g"></param>
		/// <returns>True if the GeometryCollections are equals</returns>
		public bool Equals(GeometryCollection g)
		{
			if (g == null)
				return false;
			if (g.Collection.Count != this.Collection.Count)
				return false;
			for(int i=0;i<g.Collection.Count;i++)
				if (!g.Collection[i].Equals((Geometry)this.Collection[i]))
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
			for (int i = 0; i < this._Geometries.Count; i++)
				hash = hash ^ this._Geometries[i].GetHashCode();
			return hash;
		}

		/// <summary>
		/// Gets or sets the GeometryCollection
		/// </summary>
		public virtual List<Geometry> Collection 
		{
			get { return _Geometries; }
			set { _Geometries = value; }
		}

		/// <summary>
		///  The inherent dimension of this Geometry object, which must be less than or equal
		///  to the coordinate dimension.
		/// </summary>
		/// <remarks>This specification is restricted to geometries in two-dimensional coordinate space.</remarks>
		public override int Dimension
		{
			get {
				int dim = 0;
				for (int i = 0; i < this.Collection.Count;i++ )
					dim = (dim < this.Collection[i].Dimension ? this.Collection[i].Dimension : dim);
				return dim;
			} 
		}

		/// <summary>
		/// The minimum bounding box for this Geometry, returned as a BoundingBox.
		/// </summary>
		/// <returns></returns>
		public override BoundingBox GetBoundingBox()
		{
			if (this.Collection.Count == 0)
				return null;
			BoundingBox b = this[0].GetBoundingBox();
			for (int i = 0; i < this.Collection.Count;i++ )
				b = b.Join(this.Collection[i].GetBoundingBox());
			return b;
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

		#region ICloneable Members

		/// <summary>
		/// Return a copy of this geometry
		/// </summary>
		/// <returns>Copy of Geometry</returns>
		public new GeometryCollection Clone()
		{
			GeometryCollection geoms = new GeometryCollection();
			for (int i = 0; i < _Geometries.Count;i++)
				geoms.Collection.Add((Geometry)_Geometries[i].Clone());
			return geoms;
		}

		#endregion

		#region IEnumerable<Geometry> Members

		/// <summary>
		/// Gets an enumerator for enumerating the geometries in the GeometryCollection
		/// </summary>
		/// <returns></returns>
		public virtual IEnumerator<Geometry> GetEnumerator()
		{
			foreach (Geometry g in this.Collection)
				yield return g;
		}

		#endregion

		#region IEnumerable Members

		/// <summary>
		/// Gets an enumerator for enumerating the geometries in the GeometryCollection
		/// </summary>
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			foreach (Geometry g in this.Collection)
				yield return g;
		}

		#endregion
	}
}
