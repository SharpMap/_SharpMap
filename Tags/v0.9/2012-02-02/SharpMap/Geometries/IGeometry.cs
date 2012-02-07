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

#if !DotSpatialProjections
using ProjNet.CoordinateSystems;
#else
using DotSpatial.Projections;
#endif

namespace SharpMap.Geometries
{
    /// <summary>
    /// Defines basic interface for a Geometry
    /// </summary>
    public interface IGeometry : IGeometryClassifier
    {
        #region "Basic Methods on Geometry"

        /// <summary>
        /// Gets or sets the spatial reference system associated with the <see cref="Geometry"/>.
        /// A <see cref="Geometry"/> may not have had a spatial reference system defined for
        /// it, in which case *spatialRef will be NULL.
        /// </summary>
#if !DotSpatialProjections
        ICoordinateSystem SpatialReference { get; set; }
#else
        ProjectionInfo SpatialReference { get; set; }
#endif
        /// <summary>
        ///  The inherent dimension of this <see cref="Geometry"/> object, which must be less than or equal to the coordinate dimension.
        /// </summary>
        int Dimension { get; }

        /// <summary>
        /// The minimum bounding box for this Geometry, returned as a <see cref="Geometry"/>. The
        /// polygon is defined by the corner points of the bounding box ((MINX, MINY), (MAXX, MINY), (MAXX,
        /// MAXY), (MINX, MAXY), (MINX, MINY)).
        /// </summary>
        Geometry Envelope();

        /// <summary>
        /// The minimum <see cref="BoundingBox"/> for this <see cref="Geometry"/>.
        /// </summary>
        /// <returns><see cref="BoundingBox"/> for this <see cref="Geometry"/></returns>
        BoundingBox GetBoundingBox();

        /// <summary>
        /// Exports this <see cref="Geometry"/> to a specific well-known text representation of <see cref="Geometry"/>.
        /// </summary>
        string AsText();

        /// <summary>
        /// Exports this <see cref="Geometry"/> to a specific well-known binary representation of <see cref="Geometry"/>.
        /// </summary>
        byte[] AsBinary();

        /// <summary>
        /// Returns a WellKnownText representation of the <see cref="Geometry"/>
        /// </summary>
        /// <returns>Well-known text</returns>
        string ToString();

        /// <summary>
        /// If true, then this <see cref="Geometry"/> represents the empty point set, �, for the coordinate space. 
        /// </summary>
        /// <returns>Returns 'true' if this <see cref="Geometry"/> is the empty geometry</returns>
        bool IsEmpty();

        /// <summary>
        ///  Returns 'true' if this <see cref="Geometry"/> has no anomalous geometric points, such as self
        /// intersection or self tangency. The description of each instantiable geometric class will include the specific
        /// conditions that cause an instance of that class to be classified as not simple.
        /// </summary>
        /// <returns>true if the <see cref="Geometry"/> is simple</returns>
        bool IsSimple();

        /// <summary>
        /// Returns the closure of the combinatorial boundary of this <see cref="Geometry"/>. The
        /// combinatorial boundary is defined as described in section 3.12.3.2 of [1]. Because the result of this function
        /// is a closure, and hence topologically closed, the resulting boundary can be represented using
        /// representational geometry primitives
        /// </summary>
        /// <returns>Closure of the combinatorial boundary of this <see cref="Geometry"/></returns>
        Geometry Boundary();

        /// <summary>
        /// Returns 'true' if this <see cref="Geometry"/> is spatially related to another <see cref="Geometry"/>, by testing
        /// for intersections between the Interior, Boundary and Exterior of the two geometries
        /// as specified by the values in the intersectionPatternMatrix
        /// </summary>
        /// <param name="other"><see cref="Geometry"/> to relate to</param>
        /// <param name="intersectionPattern">Intersection Pattern</param>
        /// <returns>True if spatially related</returns>
        bool Relate(Geometry other, string intersectionPattern);

        #endregion

        #region "Methods for testing Spatial Relations between geometric objects"

        /// <summary>
        /// Returns 'true' if this <see cref="Geometry"/> is 'spatially equal' to another <see cref="Geometry"/>
        /// </summary>
        bool Equals(Geometry geom);

        /// <summary>
        /// Returns 'true' if this <see cref="Geometry"/> is 'spatially disjoint' from another <see cref="Geometry"/>
        /// </summary>
        bool Disjoint(Geometry geom);

        /// <summary>
        /// Returns 'true' if this <see cref="Geometry"/> 'spatially intersects' another <see cref="Geometry"/>
        /// </summary>
        bool Intersects(Geometry geom);

        /// <summary>
        /// Returns 'true' if this <see cref="Geometry"/> 'spatially touches' another <see cref="Geometry"/>.
        /// </summary>
        bool Touches(Geometry geom);

        /// <summary>
        /// Returns 'true' if this <see cref="Geometry"/> 'spatially crosses' another <see cref="Geometry"/>.
        /// </summary>
        bool Crosses(Geometry geom);

        /// <summary>
        /// Returns 'true' if this <see cref="Geometry"/> is 'spatially within' another <see cref="Geometry"/>.
        /// </summary>
        bool Within(Geometry geom);

        /// <summary>
        /// Returns 'true' if this <see cref="Geometry"/> 'spatially contains' another <see cref="Geometry"/>.
        /// </summary>
        bool Contains(Geometry geom);

        /// <summary>
        /// Returns 'true' if this <see cref="Geometry"/> 'spatially overlaps' another <see cref="Geometry"/>.
        /// </summary>
        bool Overlaps(Geometry geom);

        #endregion

        #region "Methods that support Spatial Analysis"

        /// <summary>
        /// Returns the shortest distance between any two points in the two geometries
        /// as calculated in the spatial reference system of this <see cref="Geometry"/>.
        /// </summary>
        /// <param name="geom"><see cref="Geometry"/> to calculate distance to</param>
        /// <returns>Shortest distance between any two points in the two geometries</returns>
        double Distance(Geometry geom);

        /// <summary>
        /// Returns a <see cref="Geometry"/> that represents all points whose distance from this <see cref="Geometry"/>
        /// is less than or equal to distance. Calculations are in the Spatial Reference
        /// System of this <see cref="Geometry"/>.
        /// </summary>
        /// <param name="d">Buffer distance</param>
        /// <returns>Buffer around <see cref="Geometry"/></returns>
        Geometry Buffer(double d);


        /// <summary>
        /// Returns a <see cref="Geometry"/> that represents the convex hull of this <see cref="Geometry"/>.
        /// </summary>
        /// <returns>The convex hull</returns>
        Geometry ConvexHull();

        /// <summary>
        /// Returns a <see cref="Geometry"/> that represents the point set intersection of this <see cref="Geometry"/>
        /// with another <see cref="Geometry"/>.
        /// </summary>
        /// <param name="geom"><see cref="Geometry"/> to intersect with</param>
        /// <returns>Returns a <see cref="Geometry"/> that represents the point set intersection of this <see cref="Geometry"/> with another <see cref="Geometry"/>.</returns>
        Geometry Intersection(Geometry geom);

        /// <summary>
        /// Returns a <see cref="Geometry"/> that represents the point set union of this <see cref="Geometry"/> with anotherGeometry.
        /// </summary>
        /// <param name="geom">Geometry to union with</param>
        /// <returns>Unioned <see cref="Geometry"/></returns>
        Geometry Union(Geometry geom);

        /// <summary>
        /// Returns a <see cref="Geometry"/> that represents the point set difference of this <see cref="Geometry"/> with anotherGeometry.
        /// </summary>
        /// <param name="geom"><see cref="Geometry"/> to compare to</param>
        /// <returns><see cref="Geometry"/></returns>
        Geometry Difference(Geometry geom);

        /// <summary>
        /// Returns a geometry that represents the point set symmetric difference of this <see cref="Geometry"/> with anotherGeometry.
        /// </summary>
        /// <param name="geom"><see cref="Geometry"/> to compare to</param>
        /// <returns><see cref="Geometry"/></returns>
        Geometry SymDifference(Geometry geom);

        #endregion
    }
}