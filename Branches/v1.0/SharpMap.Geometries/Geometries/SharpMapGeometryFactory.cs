using System;
using System.Collections.Generic;
using System.Diagnostics;
using GeoAPI.Geometries;

namespace SharpMap.Geometries
{
    public class SharpMapGeometryFactory : IGeometryFactory
    {
        private SharpMapGeometryFactory()
        {
            
        }
        
        #region Implementation of IGeometryFactory

        public IGeometry BuildGeometry(ICollection<IGeometry> geomList)
        {
            throw new NotSupportedException();
        }

        public IGeometry CreateGeometry(IGeometry g)
        {
            if (g is IPoint)
                return CreatePoint(g.Coordinate);
            if (g is ILinearRing)
                return CreateLinearRing(g.Coordinates);
            if (g is ILineString)
                return CreateLineString(g.Coordinates);
            if (g is IPolygon)
            {
                var gp = (IPolygon) g;
                return CreatePolygon(gp.Shell, gp.Holes);
            }

            if (g is IMultiPoint)
                return CreateMultiPoint(g.Coordinates);
            
            if (g is IMultiLineString)
            {
                var gms = (IMultiLineString) g;
                var lineStrings = new ILineString[gms.NumGeometries];
                for (var i = 0; i < gms.NumGeometries; i++)
                    lineStrings[i] = (ILineString) gms[i];
                return CreateMultiLineString(lineStrings);
            }

            if (g is IMultiPolygon)
            {
                var gmp = (IMultiPolygon)g;
                var polygons = new IPolygon[gmp.NumGeometries];
                for (var i = 0; i < gmp.NumGeometries; i++)
                    polygons[i] = (IPolygon)gmp[i];
                return CreateMultiPolygon(polygons);
            }

            if (g is IGeometryCollection)
            {
                var gc = g as IGeometryCollection;
                return CreateGeometryCollection(gc.Geometries);
            }
            
            throw new ArgumentException("Unknown geometry type", "g");
        }

        public IPoint CreatePoint(Coordinate coordinate)
        {
            return new Point(coordinate);
        }

        public IPoint CreatePoint(ICoordinateSequence coordinates)
        {
            Debug.Assert(coordinates != null, "coordinates != null");
            Debug.Assert(coordinates.Count > 0, "coordinates.Count > 0");
            return CreatePoint(coordinates.GetCoordinate(0));
        }

        public ILineString CreateLineString(Coordinate[] coordinates)
        {
            return new LineString(coordinates);
        }

        public ILineString CreateLineString(ICoordinateSequence coordinates)
        {
            return new LineString(coordinates.ToCoordinateArray());
        }

        public ILinearRing CreateLinearRing(Coordinate[] coordinates)
        {
            return new LinearRing(coordinates);
        }

        public ILinearRing CreateLinearRing(ICoordinateSequence coordinates)
        {
            return new LinearRing(coordinates.ToCoordinateArray());
        }

        public IPolygon CreatePolygon(ILinearRing shell, ILinearRing[] holes)
        {

            var exteriorRing = (LinearRing) CreateLinearRing(shell.Coordinates);
            LinearRing[] interiorRings = null;
            if (holes != null)
            {
                interiorRings = new LinearRing[holes.Length];
                for (var i = 0; i < holes.Length; i++)
                    interiorRings[i] = (LinearRing)CreateLinearRing(holes[i].Coordinates);
            }
            return new Polygon(exteriorRing, interiorRings);
        }

        public IMultiPoint CreateMultiPoint(Coordinate[] coordinates)
        {
            return new MultiPoint(coordinates);
        }

        public IMultiPoint CreateMultiPoint(IPoint[] point)
        {
            var coords = new Coordinate[point.Length];
            for (var i = 0; i < coords.Length; i++)
                coords[i] = point[i].Coordinate;
            return new MultiPoint(coords);
        }

        public IMultiPoint CreateMultiPoint(ICoordinateSequence coordinates)
        {
            return new MultiPoint(coordinates.ToCoordinateArray());
        }

        public IMultiLineString CreateMultiLineString(ILineString[] lineStrings)
        {
            var components = new LineString[lineStrings.Length];
            for (var i = 0; i < lineStrings.Length; i++)
            {
                components[i] = ToLineString(lineStrings[i]);
            }
            return new MultiLineString {LineStrings = components};
        }

        private LineString ToLineString(ILineString lineString)
        {
            if (lineString is LineString)
                return (LineString) lineString;
            return (LineString) CreateLineString(lineString.CoordinateSequence);
        }


        public IMultiPolygon CreateMultiPolygon(IPolygon[] polygons)
        {
            var components = new Polygon[polygons.Length];
            for (var i = 0; i < polygons.Length; i++)
                components[i] = ToPolygon(polygons[i]);
            return new MultiPolygon {Polygons = components};
        }

        private Polygon ToPolygon(IPolygon polygon)
        {
            if (polygon is Polygon)
                return (Polygon) polygon;
            return (Polygon) CreatePolygon(polygon.Shell, polygon.Holes);
        }
        public IGeometryCollection CreateGeometryCollection(IGeometry[] geometries)
        {
            var components = new Geometry[geometries.Length];
            for (int i = 0; i < geometries.Length; i++)
                components[i] = ToGeometry(geometries[i]);

            return new GeometryCollection {Collection = components};
        }

        private Geometry ToGeometry(IGeometry geometry)
        {
            if (geometry is Geometry)
                return (Geometry) geometry;
            return (Geometry) CreateGeometry(geometry);
        }

        public IGeometry ToGeometry(Envelope env)
        {
            var pts = new Coordinate[5];
            pts[0] = new Coordinate(env.MinX, env.MinY);
            pts[1] = new Coordinate(env.MinX, env.MaxY);
            pts[2] = new Coordinate(env.MaxX, env.MaxY);
            pts[3] = new Coordinate(env.MaxX, env.MinY);
            pts[4] = new Coordinate(env.MinX, env.MinY);
            
            return new Polygon(new LinearRing(pts));
        }

        public ICoordinateSequenceFactory CoordinateSequenceFactory
        {
            get { return SharpMapCoordinateSequenceFactory.Instance; }
        }

        public int SRID
        {
            get { return 0; }
        }

        public IPrecisionModel PrecisionModel
        {
            get { return null; }
        }

        public static readonly SharpMapGeometryFactory Instance = new SharpMapGeometryFactory();
        private int _srid;

        #endregion
    }


}