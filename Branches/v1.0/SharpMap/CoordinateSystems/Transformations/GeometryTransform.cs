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
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GeoAPI.Geometries;
using SharpMap.Geometries;

namespace GeoAPI.CoordinateSystems.Transformations
{
    /// <summary>
    /// Helper class for transforming <see cref="IGeometry"/>
    /// </summary>
    public class GeometryTransform
    {
        /// <summary>
        /// Transforms a <see cref="Envelope"/>.
        /// </summary>
        /// <param name="box">GeoAPI.Envelope to transform</param>
        /// <param name="transform">Math Transform</param>
        /// <returns>Transformed object</returns>
        public static Envelope TransformBox(Envelope box, IMathTransform transform)
        {
            if (box == null)
                return null;
            var corners = new Coordinate[4];
            corners[0] = CoordinateEx.FromDoubleArray(transform.Transform(box.BottomLeft().ToDoubleArray())); //LL
            corners[1] = CoordinateEx.FromDoubleArray(transform.Transform(box.TopRight().ToDoubleArray())); //UR
            corners[2] = CoordinateEx.FromDoubleArray(transform.Transform(new Coordinate(box.MinX, box.MaxY).ToDoubleArray())); //UL
            corners[3] = CoordinateEx.FromDoubleArray(transform.Transform(new Coordinate(box.MaxX, box.MinY).ToDoubleArray())); //LR

            var env = new Envelope(corners[0], corners[1]);
            for (var i = 2; i < 4; i++)
                env.ExpandToInclude(corners[i]);
            return env;
        }

        /// <summary>
        /// Transforms a <see cref="IGeometry"/>.
        /// </summary>
        /// <param name="g">Geometry to transform</param>
        /// <param name="transform">MathTransform</param>
        /// <returns>Transformed Geometry</returns>
        public static IGeometry TransformGeometry(IGeometryFactory factory, IGeometry g, IMathTransform transform)
        {
            if (g == null)
                return null;
            if (g is IPoint)
                return TransformPoint(factory, g as IPoint, transform);
            if (g is ILineString)
                return TransformLineString(factory, g as ILineString, transform);
            if (g is IPolygon)
                return TransformPolygon(factory, g as IPolygon, transform);
            if (g is IMultiPoint)
                return TransformMultiPoint(factory, g as IMultiPoint, transform);
            if (g is IMultiLineString)
                return TransformMultiLineString(factory, g as IMultiLineString, transform);
            if (g is IMultiPolygon)
                return TransformMultiPolygon(factory, g as IMultiPolygon, transform);
            if (g is IGeometryCollection)
                return TransformGeometryCollection(factory, g as IGeometryCollection, transform);
            throw new ArgumentException("Could not transform geometry type '" + g.GetType() + "'");
        }

        /// <summary>
        /// Transforms a <see cref="GeoAPI.Geometries.IPoint"/>.
        /// </summary>
        /// <param name="factory">The factory to create the geometry</param>
        /// <param name="p">Point to transform</param>
        /// <param name="transform">MathTransform</param>
        /// <returns>Transformed Point</returns>
        public static IPoint TransformPoint(IGeometryFactory factory, IPoint p, IMathTransform transform)
        {
            try
            {
                return factory.CreatePoint(transform.Transform(p.Coordinate));
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Transforms a <see cref="ILineString"/>.
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="l">LineString to transform</param>
        /// <param name="transform">MathTransform</param>
        /// <returns>Transformed LineString</returns>
        public static ILineString TransformLineString(IGeometryFactory factory, ILineString l, IMathTransform transform)
        {
            try
            {
                var coordSeq = l.CoordinateSequence;
                var transformedSeq = transform.Transform(coordSeq);
                return factory.CreateLineString(transformedSeq);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Transforms a <see cref="ILinearRing"/>.
        /// </summary>
        /// <param name="r">LinearRing to transform</param>
        /// <param name="transform">MathTransform</param>
        /// <returns>Transformed LinearRing</returns>
        public static ILinearRing TransformLinearRing(IGeometryFactory factory, ILinearRing r, IMathTransform transform)
        {
            try
            {
                var coordSeq = r.CoordinateSequence;
                var transformedSeq = transform.Transform(coordSeq);
                return factory.CreateLinearRing(transformedSeq);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Transforms a <see cref="IPolygon"/>.
        /// </summary>
        /// <param name="p">Polygon to transform</param>
        /// <param name="transform">MathTransform</param>
        /// <returns>Transformed Polygon</returns>
        public static IPolygon TransformPolygon(IGeometryFactory factory, IPolygon p, IMathTransform transform)
        {
            var transformedShell = TransformLinearRing(factory, p.Shell, transform);
            var transformedRings = new ILinearRing[p.Holes.Length];
            for (var i = 0; i < p.Holes.Length; i++)
                transformedRings[i] = TransformLinearRing(factory, p.Holes[i], transform);
            return p.Factory.CreatePolygon(transformedShell, transformedRings);
        }

        /// <summary>
        /// Transforms a <see cref="IMultiPoint"/>.
        /// </summary>
        /// <param name="points">MultiPoint to transform</param>
        /// <param name="transform">MathTransform</param>
        /// <returns>Transformed MultiPoint</returns>
        public static IMultiPoint TransformMultiPoint(IGeometryFactory factory, IMultiPoint points, IMathTransform transform)
        {
            var transformedPoints = transform.TransformList(points.Coordinates);
            return factory.CreateMultiPoint(transformedPoints.ToArray());
        }

        /// <summary>
        /// Transforms a <see cref="IMultiLineString"/>.
        /// </summary>
        /// <param name="lines">MultiLineString to transform</param>
        /// <param name="transform">MathTransform</param>
        /// <returns>Transformed MultiLineString</returns>
        public static IMultiLineString TransformMultiLineString(IGeometryFactory factory, IMultiLineString lines, IMathTransform transform)
        {
            var lineStrings = new List<ILineString>(lines.Count);
            foreach (ILineString line in lines.Geometries       )
            {
                lineStrings.Add(TransformLineString(factory, line, transform));
            }
            return factory.CreateMultiLineString(lineStrings.ToArray());
        }

        /// <summary>
        /// Transforms a <see cref="IMultiPolygon"/>.
        /// </summary>
        /// <param name="polys">MultiPolygon to transform</param>
        /// <param name="transform">MathTransform</param>
        /// <returns>Transformed MultiPolygon</returns>
        public static IMultiPolygon TransformMultiPolygon(IGeometryFactory factory, IMultiPolygon polys, IMathTransform transform)
        {
            var polygons = new List<IPolygon>(polys.Count);
            foreach (IPolygon poly in polys)
            {
                polygons.Add(TransformPolygon(factory, poly, transform));
            }
            return factory.CreateMultiPolygon(polygons.ToArray());
        }

        /// <summary>
        /// Transforms a <see cref="IGeometryCollection"/>.
        /// </summary>
        /// <param name="geoms">GeometryCollection to transform</param>
        /// <param name="transform">MathTransform</param>
        /// <returns>Transformed GeometryCollection</returns>
        public static IGeometryCollection TransformGeometryCollection(IGeometryFactory factory, IGeometryCollection geoms, IMathTransform transform)
        {
            var geometries = new List<IGeometry>(geoms.Count);
            foreach (IGeometry geometry in geoms)
            {
                geometries.Add(TransformGeometry(factory, geometry, transform));
            }
            return factory.CreateGeometryCollection(geometries.ToArray());
        }
    }
}
#endif