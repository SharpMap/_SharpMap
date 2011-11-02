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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using GeoAPI.Geometries;
using SharpMap.Utilities;

namespace SharpMap.Geometries
{
    /// <summary>
    /// A Polygon is a planar Surface, defined by 1 exterior boundary and 0 or more interior boundaries. Each
    /// interior boundary defines a hole in the Polygon.
    /// </summary>
    /// <remarks>
    /// Vertices of rings defining holes in polygons are in the opposite direction of the exterior ring.
    /// </remarks>
    [Serializable]
    public class Polygon : Surface, IPolygon
    {
        private LinearRing _exteriorRing;
        private IList<LinearRing> _interiorRings;

        /// <summary>
        /// Instatiates a polygon based on one extorier ring and a collection of interior rings.
        /// </summary>
        /// <param name="exteriorRing">Exterior ring</param>
        /// <param name="interiorRings">Interior rings</param>
        public Polygon(LinearRing exteriorRing, IList<LinearRing> interiorRings)
        {
            _exteriorRing = exteriorRing;
            _interiorRings = interiorRings ?? new Collection<LinearRing>();
        }

        /// <summary>
        /// Instatiates a polygon based on one extorier ring.
        /// </summary>
        /// <param name="exteriorRing">Exterior ring</param>
        public Polygon(LinearRing exteriorRing)
            : this(exteriorRing, new Collection<LinearRing>())
        {
        }

        public ILineString GetInteriorRingN(int n)
        {
            return _interiorRings[n];
        }

        /// <summary>
        /// Gets or sets the exterior ring of this Polygon
        /// </summary>
        /// <remarks>This method is supplied as part of the OpenGIS Simple Features Specification</remarks>
        public ILineString ExteriorRing
        {
            get { return _exteriorRing; }
            set
            {
                _exteriorRing = ToLinearRing(value);
            }
        }

        public ILinearRing Shell
        {
            get { return _exteriorRing; }
        }

        public int NumInteriorRings
        {
            get { return _interiorRings.Count; }
        }

        private static LinearRing ToLinearRing(ILineString lineString)
        {
            if (lineString == null)
                return null;
            if (lineString is LinearRing)
                return lineString as LinearRing;
            return new LinearRing(lineString.Coordinates);
        }

        /// <summary>
        /// Gets or sets the interior rings of this Polygon
        /// </summary>
        public ILineString[] InteriorRings
        {
            get { return _interiorRings.ToArray(); }
            set
            {
                _interiorRings = new Collection<LinearRing>();
                if (value == null)
                    return;
                foreach (var lineString in value)
                    _interiorRings.Add(ToLinearRing(lineString));
            }
        }

        public ILinearRing[] Holes
        {
            get { return _interiorRings.ToArray(); }
        }

        /// <summary>
        /// Returns the number of interior rings in this Polygon
        /// </summary>
        /// <remarks>This method is supplied as part of the OpenGIS Simple Features Specification</remarks>
        /// <returns></returns>
        public int NumInteriorRing
        {
            get { return _interiorRings.Count; }
        }

        public override OgcGeometryType OgcGeometryType
        {
            get { return OgcGeometryType.Polygon; }
        }

        /// <summary>
        /// The area of this Surface, as measured in the spatial reference system of this Surface.
        /// </summary>
        public override double Area
        {
            get
            {
                double area = 0.0;
                area += _exteriorRing.Area;
                bool extIsClockwise = _exteriorRing.IsCCW();
                for (int i = 0; i < _interiorRings.Count; i++)
                    //opposite direction of exterior subtracts area
                    if (_interiorRings[i].IsCCW() != extIsClockwise)
                        area -= _interiorRings[i].Area;
                    else
                        area += _interiorRings[i].Area;
                return area;
            }
        }

        /// <summary>
        /// The mathematical centroid for this Surface as a Point.
        /// The result is not guaranteed to be on this Surface.
        /// </summary>
        public override Point Centroid
        {
            get { return new Point(_exteriorRing.EnvelopeInternal.Centre); }
        }

        public override Coordinate[] Coordinates
        {
            get 
            {
                var coll = new List<Coordinate>();
                coll.AddRange(_exteriorRing.Coordinates);
                if (_interiorRings != null)
                foreach (var interiorRing in _interiorRings)
                {
                    coll.AddRange(interiorRing.Coordinates);
                }
                return coll.ToArray();
            }
        }

        /// <summary>
        /// A point guaranteed to be on this Surface.
        /// </summary>
        public override IPoint PointOnSurface
        {
            get { throw new NotSupportedException(); }
        }

        /// <summary>
        /// Returns the Nth interior ring for this Polygon as a LineString
        /// </summary>
        /// <remarks>This method is supplied as part of the OpenGIS Simple Features Specification</remarks>
        /// <param name="n"></param>
        /// <returns></returns>
        public LinearRing InteriorRing(int n)
        {
            return _interiorRings[n];
        }

        /// <summary>
        /// Returns the bounding box of the object
        /// </summary>
        /// <returns>bounding box</returns>
        public override GeoAPI.Geometries.Envelope GetBoundingBox()
        {
            if (_exteriorRing == null || _exteriorRing.Vertices.Count == 0) return null;
            GeoAPI.Geometries.Envelope bbox = new Envelope(_exteriorRing.Vertices[0].X, _exteriorRing.Vertices[0].Y,
                _exteriorRing.Vertices[0].X, _exteriorRing.Vertices[0].Y);
            //GeoAPI.Geometries.Envelope bbox = new GeoAPI.Geometries.Envelope(_ExteriorRing.Vertices[0], _ExteriorRing.Vertices[0]);
            for (int i = 1; i < _exteriorRing.Vertices.Count; i++)
            {
                bbox.ExpandToInclude(_exteriorRing.Vertices[i].X, _exteriorRing.Vertices[i].Y);
                /*
                bbox.Min.X = Math.Min(_ExteriorRing.Vertices[i].X, bbox.Min.X);
                bbox.Min.Y = Math.Min(_ExteriorRing.Vertices[i].Y, bbox.Min.Y);
                bbox.Max.X = Math.Max(_ExteriorRing.Vertices[i].X, bbox.Max.X);
                bbox.Max.Y = Math.Max(_ExteriorRing.Vertices[i].Y, bbox.Max.Y);
                 */
            }
            return bbox;
        }

        /// <summary>
        /// Return a copy of this geometry
        /// </summary>
        /// <returns>Copy of Geometry</returns>
        public new Polygon Clone()
        {
            var p = new Polygon();
            p.ExteriorRing = _exteriorRing.Clone();
            for (int i = 0; i < _interiorRings.Count; i++)
                p._interiorRings.Add(_interiorRings[i].Clone());
            return p;
        }

        #region "Inherited methods from abstract class Geometry"

        /// <summary>
        /// Determines if this Polygon and the specified Polygon object has the same values
        /// </summary>
        /// <param name="p">Polygon to compare with</param>
        /// <returns></returns>
        public bool Equals(Polygon p)
        {
            if (p == null)
                return false;
            if (!p.ExteriorRing.Equals(ExteriorRing))
                return false;
            if (p._interiorRings.Count != _interiorRings.Count)
                return false;
            for (int i = 0; i < p._interiorRings.Count; i++)
                if (!p._interiorRings[i].Equals(_interiorRings[i]))
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
            var hash = _exteriorRing.GetHashCode();
            
            for (int i = 0; i < _interiorRings.Count; i++)
                hash = hash ^ InteriorRings[i].GetHashCode();
            return hash;
        }

        /// <summary>
        /// If true, then this Geometry represents the empty point set, Ø, for the coordinate space. 
        /// </summary>
        /// <returns>Returns 'true' if this Geometry is the empty geometry</returns>
        public override bool IsEmpty()
        {
            return (_exteriorRing == null) || (_exteriorRing.Vertices.Count == 0);
        }

        /// <summary>
        /// Returns 'true' if this Geometry has no anomalous geometric points, such as self
        /// intersection or self tangency. The description of each instantiable geometric class will include the specific
        /// conditions that cause an instance of that class to be classified as not simple.
        /// </summary>
        public override bool IsSimple()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Returns the closure of the combinatorial boundary of this Geometry. The
        /// combinatorial boundary is defined as described in section 3.12.3.2 of [1]. Because the result of this function
        /// is a closure, and hence topologically closed, the resulting boundary can be represented using
        /// representational geometry primitives
        /// </summary>
        public override Geometry Boundary()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Returns the shortest distance between any two points in the two geometries
        /// as calculated in the spatial reference system of this Geometry.
        /// </summary>
        public override double Distance(IGeometry geom)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Returns a geometry that represents all points whose distance from this Geometry
        /// is less than or equal to distance. Calculations are in the Spatial Reference
        /// System of this Geometry.
        /// </summary>
        public override IGeometry Buffer(double d)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Geometry—Returns a geometry that represents the convex hull of this Geometry.
        /// </summary>
        public override IGeometry ConvexHull()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Returns a geometry that represents the point set intersection of this Geometry
        /// with anotherGeometry.
        /// </summary>
        public override IGeometry Intersection(IGeometry geom)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Returns a geometry that represents the point set union of this Geometry with anotherGeometry.
        /// </summary>
        public override IGeometry Union(IGeometry geom)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Returns a geometry that represents the point set difference of this Geometry with anotherGeometry.
        /// </summary>
        public override IGeometry Difference(IGeometry geom)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Returns a geometry that represents the point set symmetric difference of this Geometry with anotherGeometry.
        /// </summary>
        public override IGeometry SymmetricDifference(IGeometry geom)
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}