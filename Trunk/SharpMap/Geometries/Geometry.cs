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

namespace SharpMap.Geometries
{
    /// <summary>
	/// <see cref="Geometry"/> is the root class of the Geometry Object Model hierarchy.
	/// <see cref="Geometry"/> is an abstract (non-instantiable) class.
	/// </summary>
	/// <remarks>
	/// <para>The instantiable subclasses of <see cref="Geometry"/> defined in the specification are restricted to 0, 1 and twodimensional
	/// geometric objects that exist in two-dimensional coordinate space (R^2).</para>
    /// <para>All instantiable geometry classes described in this specification are defined so that valid instances of a
	/// geometry class are topologically closed (i.e. all defined geometries include their boundary).</para>
	/// </remarks>
	[Serializable]
	public abstract class Geometry : IGeometry, IEquatable<Geometry>
	{
		private SharpMap.CoordinateSystems.ICoordinateSystem _SpatialReference;

		/// <summary>
		/// Gets or sets the spatial reference system associated with the <see cref="Geometry"/>.
		/// A <see cref="Geometry"/> may not have had a spatial reference system defined for
		/// it, in which case *spatialRef will be NULL.
		/// </summary>
		public SharpMap.CoordinateSystems.ICoordinateSystem SpatialReference
		{
			get { return _SpatialReference; }
			set { _SpatialReference = value; }
		}

		// The following are methods that should be implemented on a geometry object according to
		// the OpenGIS Simple Features Specification
		#region "Basic Methods on Geometry"

		/// <summary>
		///  The inherent dimension of this <see cref="Geometry"/> object, which must be less than or equal
		///  to the coordinate dimension.
		/// </summary>
		/// <remarks>This specification is restricted to geometries in two-dimensional coordinate space.</remarks>
		public abstract int Dimension { get; }

		/// <summary>
		/// The minimum bounding box for this <see cref="Geometry"/>, returned as a <see cref="Geometry"/>. The
		/// polygon is defined by the corner points of the bounding box ((MINX, MINY), (MAXX, MINY), (MAXX,
		/// MAXY), (MINX, MAXY), (MINX, MINY)).
		/// </summary>
		/// <remarks>The envelope is actually the <see cref="BoundingBox"/> converted into a polygon.</remarks>
		/// <seealso cref="GetBoundingBox"/>
		public Geometry Envelope()
		{
			BoundingBox box = this.GetBoundingBox();
			Polygon envelope = new Polygon();
			envelope.ExteriorRing.Vertices.Add(box.Min); //minx miny
			envelope.ExteriorRing.Vertices.Add(new Point(box.Max.X, box.Min.Y)); //maxx minu
			envelope.ExteriorRing.Vertices.Add(box.Max); //maxx maxy
			envelope.ExteriorRing.Vertices.Add(new Point(box.Min.X, box.Max.Y)); //minx maxy
			envelope.ExteriorRing.Vertices.Add(envelope.ExteriorRing.StartPoint); //close ring
			return envelope;
		}


		/// <summary>
		/// The minimum bounding box for this <see cref="Geometry"/>, returned as a <see cref="BoundingBox"/>.
		/// </summary>
		/// <returns></returns>
		public abstract BoundingBox GetBoundingBox();

		/// <summary>
		/// Exports this <see cref="Geometry"/> to a specific well-known text representation of <see cref="Geometry"/>.
		/// </summary>
		public string AsText()
		{
			return SharpMap.Converters.WellKnownText.GeometryToWKT.Write(this);
		}

		/// <summary>
		/// Exports this <see cref="Geometry"/> to a specific well-known binary representation of <see cref="Geometry"/>.
		/// </summary>
		public byte[] AsBinary()
		{
			return SharpMap.Converters.WellKnownBinary.GeometryToWKB.Write(this);
		}

		/// <summary>
		/// Returns a WellKnownText representation of the <see cref="Geometry"/>
		/// </summary>
		/// <returns>Well-known text</returns>
		public override string ToString()
		{
			return this.AsText();
		}

		/// <summary>
		/// Creates a <see cref="Geometry"/> based on a WellKnownText string
		/// </summary>
		/// <param name="WKT">Well-known Text</param>
		/// <returns></returns>
		public static Geometry GeomFromText(string WKT)
		{
			return SharpMap.Converters.WellKnownText.GeometryFromWKT.Parse(WKT);
		}

		/// <summary>
		/// Creates a <see cref="Geometry"/> based on a WellKnownBinary byte array
		/// </summary>
		/// <param name="WKB">Well-known Binary</param>
		/// <returns></returns>
		public static Geometry GeomFromWKB(byte[] WKB)
		{
			return SharpMap.Converters.WellKnownBinary.GeometryFromWKB.Parse(WKB);
		}

		/// <summary>
		/// Returns 'true' if this <see cref="Geometry"/> is the empty geometry . If true, then this
		/// <see cref="Geometry"/> represents the empty point set, �, for the coordinate space. 
		/// </summary>
		public abstract bool IsEmpty();

		/// <summary>
		///  Returns 'true' if this Geometry has no anomalous geometric points, such as self
		/// intersection or self tangency. The description of each instantiable geometric class will include the specific
		/// conditions that cause an instance of that class to be classified as not simple.
		/// </summary>
		public abstract bool IsSimple();

		/// <summary>
		/// Returns the closure of the combinatorial boundary of this <see cref="Geometry"/>. The
		/// combinatorial boundary is defined as described in section 3.12.3.2 of [1]. Because the result of this function
		/// is a closure, and hence topologically closed, the resulting boundary can be represented using
		/// representational geometry primitives
		/// </summary>
		public abstract Geometry Boundary();

		#endregion

		#region "Methods for testing Spatial Relations between geometric objects"


		/// <summary>
		/// Returns 'true' if this Geometry is �spatially disjoint� from another <see cref="Geometry"/>.
		/// </summary>
		public virtual bool Disjoint(Geometry geom) { return SharpMap.Geometries.SpatialRelations.Disjoint(this, geom); }

		/// <summary>
		/// Returns 'true' if this <see cref="Geometry"/> �spatially intersects� another <see cref="Geometry"/>.
		/// </summary>
		public virtual bool Intersects(Geometry geom) { return SharpMap.Geometries.SpatialRelations.Intersects(this, geom); }

		/// <summary>
		/// Returns 'true' if this <see cref="Geometry"/> �spatially touches� another <see cref="Geometry"/>.
		/// </summary>
		public virtual bool Touches(Geometry geom) { return SharpMap.Geometries.SpatialRelations.Touches(this, geom); }

		/// <summary>
		/// Returns 'true' if this <see cref="Geometry"/> �spatially crosses� another <see cref="Geometry"/>.
		/// </summary>
		public virtual bool Crosses(Geometry geom) { return SharpMap.Geometries.SpatialRelations.Crosses(this, geom); }

		/// <summary>
		/// Returns 'true' if this <see cref="Geometry"/> is �spatially within� another <see cref="Geometry"/>.
		/// </summary>
		public virtual bool Within(Geometry geom) { return SharpMap.Geometries.SpatialRelations.Within(this, geom); }

		/// <summary>
		/// Returns 'true' if this <see cref="Geometry"/> �spatially contains� another <see cref="Geometry"/>.
		/// </summary>
		public virtual bool Contains(Geometry geom) { return SharpMap.Geometries.SpatialRelations.Contains(this, geom); }

		/// <summary>
		/// Returns 'true' if this <see cref="Geometry"/> 'spatially overlaps' another <see cref="Geometry"/>.
		/// </summary>
		public virtual bool Overlaps(Geometry geom) { return SharpMap.Geometries.SpatialRelations.Overlaps(this, geom); }


		/// <summary>
		/// Returns 'true' if this <see cref="Geometry"/> is spatially related to another <see cref="Geometry"/>, by testing
		/// for intersections between the Interior, Boundary and Exterior of the two geometries
		/// as specified by the values in the intersectionPatternMatrix
		/// </summary>
		/// <param name="other"><see cref="Geometry"/> to relate to</param>
		/// <param name="intersectionPattern">Intersection Pattern</param>
		/// <returns>True if spatially related</returns>
		public bool Relate(Geometry other, string intersectionPattern)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region "Methods that support Spatial Analysis"

		/// <summary>
		/// Returns the shortest distance between any two points in the two geometries
		/// as calculated in the spatial reference system of this Geometry.
		/// </summary>
		public abstract double Distance(Geometry geom);

		/// <summary>
		/// Returns a geometry that represents all points whose distance from this Geometry
		/// is less than or equal to distance. Calculations are in the Spatial Reference
		/// System of this Geometry.
		/// </summary>
		/// <param name="d">Buffer distance</param>
		public abstract Geometry Buffer(double d);


		/// <summary>
		/// Geometry�Returns a geometry that represents the convex hull of this Geometry.
		/// </summary>
		public abstract Geometry ConvexHull();

		/// <summary>
		/// Returns a geometry that represents the point set intersection of this Geometry
		/// with anotherGeometry.
		/// </summary>
		public abstract Geometry Intersection(Geometry geom);

		/// <summary>
		/// Returns a geometry that represents the point set union of this Geometry with anotherGeometry.
		/// </summary>
		public abstract Geometry Union(Geometry geom);

		/// <summary>
		/// Returns a geometry that represents the point set difference of this Geometry with anotherGeometry.
		/// </summary>
		public abstract Geometry Difference(Geometry geom);

		/// <summary>
		/// Returns a geometry that represents the point set symmetric difference of this Geometry with anotherGeometry.
		/// </summary>
		public abstract Geometry SymDifference(Geometry geom);

		#endregion

		#region ICloneable Members

		/// <summary>
		/// This method must be overridden using 'public new [derived_data_type] Clone()'
		/// </summary>
		/// <returns>Copy of Geometry</returns>
		public Geometry Clone()
		{
			throw (new ApplicationException("Clone() has not been implemented on derived datatype"));
		}

		#endregion

		#region IEquatable<Geometry> Members

		/// <summary>
		/// Returns 'true' if this Geometry is 'spatially equal' to anotherGeometry
		/// </summary>
		public virtual bool Equals(Geometry other)
		{
			return SharpMap.Geometries.SpatialRelations.Equals(this, other);
		}

		/// <summary>
		/// Determines whether the specified <see cref="Object"/> is equal to the current <see cref="Object"/>.
		/// </summary>
		/// <param name="obj">The <see cref="Object"/> to compare with the current <see cref="Object"/>.</param>
		/// <returns>true if the specified <see cref="Object"/> is equal to the current <see cref="Object"/>; otherwise, false</returns>
		public override bool Equals(object obj)
		{
			if (obj == null)
				return false; 
			else
			{
				Geometry g = obj as Geometry;
				if (g == null)
					return false;
				else
					return this.Equals(g);
			}
		}

		/// <summary>
		/// Serves as a hash function for a particular type. <see cref="GetHashCode"/> is suitable for use 
		/// in hashing algorithms and data structures like a hash table.
		/// </summary>
		/// <returns>A hash code for the current <see cref="GetHashCode"/>.</returns>
		public override int GetHashCode()
		{
			return this.AsBinary().GetHashCode();
		}


		#endregion
	}
}