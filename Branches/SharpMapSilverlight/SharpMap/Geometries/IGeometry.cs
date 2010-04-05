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
	/// Defines basic interface for a Geometry
	/// </summary>
    public interface IGeometry
	{
		#region "Basic Methods on Geometry"

		/// <summary>
		///  The inherent dimension of this Geometry object, which must be less than or equal to the coordinate dimension.
		/// </summary>
		int Dimension { get; }

        /// <summary>
        ///  Returns the Spatial Reference System ID for this Geometry.
        /// </summary>
        int SRID { get; set; }

        /// <summary>
        /// The minimum bounding box for this Geometry, returned as a Geometry. The
        /// polygon is defined by the corner points of the bounding box ((MINX, MINY), (MAXX, MINY), (MAXX,
        /// MAXY), (MINX, MAXY), (MINX, MINY)).
        /// </summary>
        Geometry Envelope();

		/// <summary>
		/// The minimum bounding box for this Geometry.
		/// </summary>
		/// <returns>BoundingBox for this geometry</returns>
		BoundingBox GetBoundingBox();

        /// <summary>
        /// Exports this Geometry to a specific well-known text representation of Geometry.
        /// </summary>
		string AsText();

		/// <summary>
		/// Exports this Geometry to a specific well-known binary representation of Geometry.
		/// </summary>
		byte[] AsBinary();

		/// <summary>
		/// Returns a WellKnownText representation of the geometry
		/// </summary>
		/// <returns>Well-known text</returns>
		string ToString();

		/// <summary>
		/// If true, then this Geometry represents the empty point set, Ø, for the coordinate space. 
		/// </summary>
		/// <returns>Returns 'true' if this Geometry is the empty geometry</returns>
		bool IsEmpty();

		/// <summary>
		///  Returns 'true' if this Geometry has no anomalous geometric points, such as self
		/// intersection or self tangency. The description of each instantiable geometric class will include the specific
		/// conditions that cause an instance of that class to be classified as not simple.
		/// </summary>
		/// <returns>true if the geometry is simple</returns>
		bool IsSimple();

        /// <summary>
        /// Returns the closure of the combinatorial boundary of this Geometry. The
        /// combinatorial boundary is defined as described in section 3.12.3.2 of [1]. Because the result of this function
        /// is a closure, and hence topologically closed, the resulting boundary can be represented using
        /// representational geometry primitives
        /// </summary>
		/// <returns>Closure of the combinatorial boundary of this Geometry</returns>
		Geometry Boundary();

		/// <summary>
		/// Returns 'true' if this geometry is spatially related to another Geometry, by testing
		/// for intersections between the Interior, Boundary and Exterior of the two geometries
		/// as specified by the values in the intersectionPatternMatrix
		/// </summary>
		/// <param name="other">Geometry to relate to</param>
		/// <param name="intersectionPattern">Intersection Pattern</param>
		/// <returns>True if spatially related</returns>
		bool Relate(Geometry other, string intersectionPattern);


        #endregion

        #region "Methods for testing Spatial Relations between geometric objects"

        /// <summary>
        /// Returns 'true' if this Geometry is ‘spatially equal’ to anotherGeometry
        /// </summary>
		bool Equals(Geometry geom);

        /// <summary>
        /// Returns 'true' if this Geometry is ‘spatially disjoint’ from anotherGeometry
        /// </summary>
		bool Disjoint(Geometry geom);

        /// <summary>
        /// Returns 'true' if this Geometry ‘spatially intersects’ anotherGeometry
        /// </summary>
		bool Intersects(Geometry geom);

        /// <summary>
        /// Returns 'true' if this Geometry ‘spatially touches’ anotherGeometry.
        /// </summary>
		bool Touches(Geometry geom);

        /// <summary>
        /// Returns 'true' if this Geometry ‘spatially crosses’ anotherGeometry.
        /// </summary>
		bool Crosses(Geometry geom);

        /// <summary>
        /// Returns 'true' if this Geometry is ‘spatially within’ anotherGeometry.
        /// </summary>
		bool Within(Geometry geom);

        /// <summary>
        /// Returns 'true' if this Geometry ‘spatially contains’ anotherGeometry.
        /// </summary>
		bool Contains(Geometry geom);

        /// <summary>
        /// Returns 'true' if this Geometry ‘spatially overlaps’ anotherGeometry.
        /// </summary>
		bool Overlaps(Geometry geom);


        #endregion

        #region "Methods that support Spatial Analysis"

        /// <summary>
        /// Returns the shortest distance between any two points in the two geometries
        /// as calculated in the spatial reference system of this Geometry.
        /// </summary>
		/// <param name="geom">Geometry to calculate distance to</param>
		/// <returns>Shortest distance between any two points in the two geometries</returns>
		double Distance(Geometry geom);

        /// <summary>
        /// Returns a geometry that represents all points whose distance from this Geometry
        /// is less than or equal to distance. Calculations are in the Spatial Reference
        /// System of this Geometry.
        /// </summary>
		/// <param name="d">Buffer distance</param>
		/// <returns>Buffer around geometry</returns>
		Geometry Buffer(double d);


        /// <summary>
        /// Geometry—Returns a geometry that represents the convex hull of this Geometry.
        /// </summary>
		/// <returns>The convex hull</returns>
		Geometry ConvexHull();

		/// <summary>
		/// Returns a geometry that represents the point set intersection of this Geometry
		/// with anotherGeometry.
		/// </summary>
		/// <param name="geom">Geometry to intersect with</param>
		/// <returns>Returns a geometry that represents the point set intersection of this Geometry with anotherGeometry.</returns>
		Geometry Intersection(Geometry geom);

		/// <summary>
		/// Returns a geometry that represents the point set union of this Geometry with anotherGeometry.
		/// </summary>
		/// <param name="geom">Geometry to union with</param>
		/// <returns>Unioned geometry</returns>
		Geometry Union(Geometry geom);

        /// <summary>
        /// Returns a geometry that represents the point set difference of this Geometry with anotherGeometry.
        /// </summary>
		/// <param name="geom">Geometry to compare to</param>
		/// <returns>Geometry</returns>
		Geometry Difference(Geometry geom);

        /// <summary>
        /// Returns a geometry that represents the point set symmetric difference of this Geometry with anotherGeometry.
        /// </summary>
		/// <param name="geom">Geometry to compare to</param>
		/// <returns>Geometry</returns>
		Geometry SymDifference(Geometry geom);

		#endregion
	}
}
