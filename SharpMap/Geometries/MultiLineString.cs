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
	/// A MultiLineString is a MultiCurve whose elements are LineStrings.
	/// </summary>
	[Serializable]
	public class MultiLineString : MultiCurve
	{
		private List<LineString> _LineStrings;
		/// <summary>
		/// Initializes an instance of a MultiLineString
		/// </summary>
		public MultiLineString()
		{
			_LineStrings = new System.Collections.Generic.List<LineString>();
		}

		/// <summary>
		/// Collection of <see cref="LineString">LineStrings</see> in the <see cref="MultiLineString"/>
		/// </summary>
		public List<LineString> LineStrings
		{
			get { return _LineStrings; }
			set { _LineStrings = value; }
		}

		/// <summary>
		/// Returns an indexed geometry in the collection
		/// </summary>
		/// <param name="index">Geometry index</param>
		/// <returns>Geometry at index</returns>
		public new LineString this[int index]
		{
			get { return _LineStrings[index]; }
		}

		/// <summary>
		/// Returns true if all LineStrings in this MultiLineString is closed (StartPoint=EndPoint for each LineString in this MultiLineString)
		/// </summary>
		public override bool IsClosed
		{
			get
			{
				for (int i = 0; i < _LineStrings.Count;i++)
					if (!_LineStrings[i].IsClosed)
						return false;
				return true;
			}
		}

		/// <summary>
		/// The length of this MultiLineString which is equal to the sum of the lengths of the element LineStrings.
		/// </summary>
		public override double Length
		{
			get {
				double l=0;
				for (int i = 0; i < _LineStrings.Count;i++)
					l += _LineStrings[i].Length;
				return l;
			}
		}

		/// <summary>
		/// If true, then this Geometry represents the empty point set, �, for the coordinate space. 
		/// </summary>
		/// <returns>Returns 'true' if this Geometry is the empty geometry</returns>
		public override bool IsEmpty()
		{
			if (_LineStrings == null || _LineStrings.Count == 0)
				return true;
			for(int i=0;i<_LineStrings.Count;i++)
				if(!_LineStrings[i].IsEmpty())
					return false;
			return true;
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

		/// <summary>
		/// Returns the number of geometries in the collection.
		/// </summary>
		public override int NumGeometries
		{
			get { return _LineStrings.Count; }
		}

		/// <summary>
		/// Returns an indexed geometry in the collection
		/// </summary>
		/// <param name="N">Geometry index</param>
		/// <returns>Geometry at index N</returns>
		public override Geometry Geometry(int N)
		{
			return _LineStrings[N];
		}

		/// <summary>
		/// The minimum bounding box for this Geometry.
		/// </summary>
		/// <returns></returns>
		public override BoundingBox GetBoundingBox()
		{
			if (_LineStrings==null || _LineStrings.Count == 0)
				return null;
			BoundingBox bbox = _LineStrings[0].GetBoundingBox();
			for (int i = 1; i < _LineStrings.Count; i++)
				bbox = bbox.Join(_LineStrings[i].GetBoundingBox());
			return bbox;
		}

		/// <summary>
		/// Return a copy of this geometry
		/// </summary>
		/// <returns>Copy of Geometry</returns>
		public new MultiLineString Clone()
		{
			MultiLineString geoms = new MultiLineString();
			for (int i = 0; i < _LineStrings.Count;i++ )
				geoms.LineStrings.Add(_LineStrings[i].Clone());
			return geoms;
		}

		#region IEnumerable<Geometry> Members

		/// <summary>
		/// Gets an enumerator for enumerating the geometries in the GeometryCollection
		/// </summary>
		/// <returns></returns>
		public override IEnumerator<Geometry> GetEnumerator()
		{
			foreach (LineString l in this._LineStrings)
				yield return l;
		}
		#endregion
	}
}
