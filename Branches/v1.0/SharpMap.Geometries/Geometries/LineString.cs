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
using System.Linq;
using GeoAPI.Geometries;
using SharpMap.Utilities;

namespace SharpMap.Geometries
{
    /// <summary>
    /// A LineString is a Curve with linear interpolation between points. Each consecutive pair of points defines a
    /// line segment.
    /// </summary>
    [Serializable]
    public class LineString : Curve, ILineString
    {
        //private IList<Coordinate> _vertices;
        private SharpMapCoordinateSequence _vertices;
        /// <summary>
        /// Initializes an instance of a LineString from a set of vertices
        /// </summary>
        /// <param name="vertices"></param>
        public LineString(IList<Coordinate> vertices)
        {
            _vertices = SharpMapCoordinateSequenceFactory.Instance.Create(vertices.ToArray());
        }

        /// <summary>
        /// Initializes an instance of a LineString
        /// </summary>
        public LineString()
            : this(SharpMapCoordinateSequenceFactory.Instance.Create(0, 2))
        {
        }

        /// <summary>
        /// Initializes an instance of a LineString
        /// </summary>
        /// <param name="points"></param>
        public LineString(IEnumerable<double[]> points)
        {
            var vertices = new Collection<Coordinate>();

            foreach (var point in points)
                vertices.Add(new Coordinate(point[0], point[1]));

            _vertices = vertices;
        }

        /// <summary>
        /// Gets or sets the collection of vertices in this Geometry
        /// </summary>
        public virtual IList<Coordinate> Vertices
        {
            get { return _vertices; }
            set { _vertices = value; }
        }

        /// <summary>
        /// Returns the vertice where this Geometry begins
        /// </summary>
        /// <returns>First vertice in LineString</returns>
        public override IPoint StartPoint
        {
            get
            {
                if (_vertices.Count == 0)
                    throw new ApplicationException("No startpoint found: LineString has no vertices.");
                return new Point(_vertices[0]);
            }
        }

        /// <summary>
        /// Gets the vertice where this Geometry ends
        /// </summary>
        /// <returns>Last vertice in LineString</returns>
        public override IPoint EndPoint
        {
            get
            {
                if (_vertices.Count == 0)
                    throw new ApplicationException("No endpoint found: LineString has no vertices.");
                return new Point(_vertices[_vertices.Count - 1]);
            }
        }

        /// <summary>
        /// Returns true if this LineString is closed and simple
        /// </summary>
        public override bool IsRing
        {
            get { return (IsClosed && IsSimple()); }
        }

        /// <summary>
        /// The length of this LineString, as measured in the spatial reference system of this LineString.
        /// </summary>
        public override double Length
        {
            get
            {
                if (Vertices.Count < 2)
                    return 0;
                double sum = 0;
                for (int i = 1; i < Vertices.Count; i++)
                    sum += Vertices[i].Distance(Vertices[i - 1]);
                return sum;
            }
        }

        #region OpenGIS Methods

        /// <summary>
        /// The number of points in this LineString.
        /// </summary>
        /// <remarks>This method is supplied as part of the OpenGIS Simple Features Specification</remarks>
        public virtual int NumPoints
        {
            get { return _vertices.Count; }
        }

        public override Coordinate[] Coordinates
        {
            get { return _vertices.ToArray(); }
        }

        public override IPoint PointOnSurface
        {
            get { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Returns the specified point N in this Linestring.
        /// </summary>
        /// <remarks>This method is supplied as part of the OpenGIS Simple Features Specification</remarks>
        /// <param name="n"></param>
        /// <returns></returns>
        public IPoint Point(int n)
        {
            return new Point(_vertices[n]);
        }

        #endregion

        /// <summary>
        /// The position of a point on the line, parameterised by length.
        /// </summary>
        /// <param name="t">Distance down the line</param>
        /// <returns>Point at line at distance t from StartPoint</returns>
        public override Point Value(double t)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// The minimum bounding box for this Geometry.
        /// </summary>
        /// <returns>GeoAPI.Geometries.Envelope for this geometry</returns>
        public override Envelope GetBoundingBox()
        {
            if (Vertices == null || Vertices.Count == 0)
                return null;
            var bbox = new Envelope(Vertices[0].X, Vertices[0].Y, Vertices[0].X, Vertices[0].Y);
            //GeoAPI.Geometries.Envelope bbox = new GeoAPI.Geometries.Envelope(Vertices[0], Vertices[0]);
            for (int i = 1; i < Vertices.Count; i++)
            {
                bbox.ExpandToInclude(Vertices[i].X, Vertices[i].Y);
                /*
                bbox.Min.X = Vertices[i].X < bbox.Min.X ? Vertices[i].X : bbox.Min.X;
                bbox.Min.Y = Vertices[i].Y < bbox.Min.Y ? Vertices[i].Y : bbox.Min.Y;
                bbox.Max.X = Vertices[i].X > bbox.Max.X ? Vertices[i].X : bbox.Max.X;
                bbox.Max.Y = Vertices[i].Y > bbox.Max.Y ? Vertices[i].Y : bbox.Max.Y;
                 */
            }
            return bbox;
        }

        /// <summary>
        /// Return a copy of this geometry
        /// </summary>
        /// <returns>Copy of Geometry</returns>
        public new LineString Clone()
        {
            var l = new LineString();
            for (var i = 0; i < _vertices.Count; i++)
                l.Vertices.Add((Coordinate)_vertices[i].Clone());
            return l;
        }

        #region "Inherited methods from abstract class Geometry"

        public override GeometryType2 GeometryType
        {
            get
            {
                return GeometryType2.LineString;
            }
        }

        /// <summary>
        /// Checks whether this instance is spatially equal to the LineString 'l'
        /// </summary>
        /// <param name="l">LineString to compare to</param>
        /// <returns>true of the objects are spatially equal</returns>
        public bool Equals(LineString l)
        {
            if (l == null)
                return false;
            if (l.Vertices.Count != Vertices.Count)
                return false;
            for (int i = 0; i < l.Vertices.Count; i++)
                if (!l.Vertices[i].Equals(Vertices[i]))
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
        /// If true, then this Geometry represents the empty point set, Ø, for the coordinate space. 
        /// </summary>
        /// <returns>Returns 'true' if this Geometry is the empty geometry</returns>
        public override bool IsEmpty()
        {
            return _vertices == null || _vertices.Count == 0;
        }

        /// <summary>
        ///  Returns 'true' if this Geometry has no anomalous geometric points, such as self
        /// intersection or self tangency. The description of each instantiable geometric class will include the specific
        /// conditions that cause an instance of that class to be classified as not simple.
        /// </summary>
        /// <returns>true if the geometry is simple</returns>
        public override bool IsSimple()
        {
            //Collection<Point> verts = new Collection<Point>(_Vertices.Count);
            var verts = new Collection<Coordinate>();

            for (int i = 0; i < _vertices.Count; i++)
                //if (!verts.Exists(delegate(SharpMap.Geometries.Point p) { return p.Equals(_Vertices[i]); }))
                if (0 != verts.IndexOf(_vertices[i]))
                    verts.Add(_vertices[i]);

            return (verts.Count == _vertices.Count - (IsClosed ? 1 : 0));
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
            throw new NotSupportedException();
        }

        public ILineString Reverse()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns the shortest distance between any two points in the two geometries
        /// as calculated in the spatial reference system of this Geometry.
        /// </summary>
        /// <param name="geom">Geometry to calculate distance to</param>
        /// <returns>Shortest distance between any two points in the two geometries</returns>
        public override double Distance(IGeometry geom)
        {
            if (geom is Point)
            {
                var coord0 = Vertices;
                var coord = (geom as IPoint).Coordinate;
                // brute force approach!
                double minDist = double.MaxValue;
                for (int i = 0; i < coord0.Count - 1; i++)
                {
                    double dist = CGAlgorithms.DistancePointLine(coord, coord0[i], coord0[i + 1]);
                    if (dist < minDist)
                    {
                        minDist = dist;
                    }
                }
                return minDist;
            }
            
            if (geom is LineString)
            {
                var coord0 = Vertices;
                var coord1 = (geom as LineString).Vertices;
                // brute force approach!
                var minDistance = double.MaxValue;
                for (var i = 0; i < coord0.Count - 1; i++)
                {
                    for (var j = 0; j < coord1.Count - 1; j++)
                    {
                        var dist = CGAlgorithms.DistanceLineLine(
                            coord0[i], coord0[i + 1],
                            coord1[j], coord1[j + 1]);
                        if (dist < minDistance)
                        {
                            minDistance = dist;
                        }
                    }
                }
                return minDistance;
            }

            throw new NotImplementedException();
        }

       

        /// <summary>
        /// Returns a geometry that represents all points whose distance from this Geometry
        /// is less than or equal to distance. Calculations are in the Spatial Reference
        /// System of this Geometry.
        /// </summary>
        /// <param name="d">Buffer distance</param>
        /// <returns>Buffer around geometry</returns>
        public override IGeometry Buffer(double d)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Geometry—Returns a geometry that represents the convex hull of this Geometry.
        /// </summary>
        /// <returns>The convex hull</returns>
        public override IGeometry ConvexHull()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns a geometry that represents the point set intersection of this Geometry
        /// with anotherGeometry.
        /// </summary>
        /// <param name="geom">Geometry to intersect with</param>
        /// <returns>Returns a geometry that represents the point set intersection of this Geometry with anotherGeometry.</returns>
        public override IGeometry Intersection(IGeometry geom)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns a geometry that represents the point set union of this Geometry with anotherGeometry.
        /// </summary>
        /// <param name="geom">Geometry to union with</param>
        /// <returns>Unioned geometry</returns>
        public override IGeometry Union(IGeometry geom)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns a geometry that represents the point set difference of this Geometry with anotherGeometry.
        /// </summary>
        /// <param name="geom">Geometry to compare to</param>
        /// <returns>Geometry</returns>
        public override IGeometry Difference(IGeometry geom)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns a geometry that represents the point set symmetric difference of this Geometry with anotherGeometry.
        /// </summary>
        /// <param name="geom">Geometry to compare to</param>
        /// <returns>Geometry</returns>
        public override IGeometry SymmetricDifference(IGeometry geom)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Implementation of ILineString

        public IPoint GetPointN(int n)
        {
            return Point(n);
        }

        public Coordinate GetCoordinateN(int n)
        {
            return _vertices[n];
        }

        #endregion
    }
}