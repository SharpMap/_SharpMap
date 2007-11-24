/*
 * Erstellt mit SharpDevelop.
 * Benutzer: Christian
 * Datum: 20.11.2007
 * Zeit: 21:38
 * 
 * Sie können diese Vorlage unter Extras > Optionen > Codeerstellung > Standardheader ändern.
 */

using System;
using NTS = GisSharpBlog.NetTopologySuite;
using GeoAPI.Geometries;

namespace SharpMap.Converters.Geometries
{
	/// <summary>
	/// Description of GeometryFactory.
	/// </summary>
	public class GeometryFactory
	{
		private static NTS.Geometries.GeometryFactory geomFactory = new NTS.Geometries.GeometryFactory();

        public static ICoordinate CreateCoordinate()
        {
            return new NTS.Geometries.Coordinate();
        }

        public static ICoordinate CreateCoordinate(double x, double y)
        {
            return new NTS.Geometries.Coordinate(x, y);
        }

        public static ICoordinate CreateCoordinate(double x, double y, double z)
        {
            return new NTS.Geometries.Coordinate(x, y, z);
        }

        public static ICoordinate CreateCoordinate(double[] point)
        {
            if (point.Rank == 2) // 2 dimensions
                return CreateCoordinate(point[0], point[1]);
            else if (point.Rank == 3) // 2 dimensions
                return CreateCoordinate(point[0], point[1], point[2]);
            else
                return CreateCoordinate();
        }

        public static IPoint CreatePoint(double x, double y)
        {
            return geomFactory.CreatePoint(new NTS.Geometries.Coordinate(x, y));
        }

        public static IPoint CreatePoint(ICoordinate coord)
        {
            return geomFactory.CreatePoint(coord);
        }
		
		public static IMultiPoint CreateMultiPoint(IPoint[] points)
		{
			return geomFactory.CreateMultiPoint(points);
		}

        public static IEnvelope CreateEnvelope(double minx, double maxx, double miny, double maxy)
		{
			return new NTS.Geometries.Envelope(minx, maxx, miny, maxy);
		}
		
		public static IEnvelope CreateEnvelope()
		{
			return new NTS.Geometries.Envelope();
		}

		public static ILineString CreateLineString(ICoordinate[] coords)
		{
			return geomFactory.CreateLineString(coords);
		}		
		
		public static IMultiLineString CreateMultiLineString(ILineString[] lineStrings)
		{
			return geomFactory.CreateMultiLineString(lineStrings);
		}		
		
		public static ILinearRing CreateLinearRing(ICoordinate[] coords)
		{
			return geomFactory.CreateLinearRing(coords);
		}		

		public static IPolygon CreatePolygon(ILinearRing shell, ILinearRing[] holes)
		{
			return geomFactory.CreatePolygon(shell, holes);
		}		
		
		public static IMultiPolygon CreateMultiPolygon(IPolygon[] polygons)
		{
			return geomFactory.CreateMultiPolygon(polygons);
		}			
		public static IMultiPolygon CreateMultiPolygon()
		{
			return geomFactory.CreateMultiPolygon(null);
		}	
		
		public static IGeometryCollection CreateGeometryCollection(IGeometry[] geometries)
		{
			return geomFactory.CreateGeometryCollection(geometries);
		}		
		public static IGeometryCollection CreateGeometryCollection()
		{
			return geomFactory.CreateGeometryCollection(null);
		}		
		
		public static bool IsCCW(ICoordinate[] ring)
		{
			return NTS.Algorithm.CGAlgorithms.IsCCW(ring);
		}
	
	}
}
