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
	/// Class defining a set of named spatial relationship operators for geometric shape objects.
	/// </summary>
	public class SpatialRelations
	{
		/// <summary>
		/// Returns TRUE if otherGeometry is wholly contained within the source geometry. This is the same as
		/// reversing the primary and comparison shapes of the Within operation
		/// </summary>
		/// <param name="sourceGeometry"></param>
		/// <param name="otherGeometry"></param>
		/// <returns></returns>
		public static bool Contains(Geometry sourceGeometry, Geometry otherGeometry)
		{
			return (otherGeometry.Within(sourceGeometry));
		}

		/// <summary>
		/// Returns TRUE if the intersection of the two geometries results in a geometry whose dimension is less than
		/// the maximum dimension of the two geometries and the intersection geometry is not equal to either
		/// geometry.
		/// </summary>
		/// <param name="g1"></param>
		/// <param name="g2"></param>
		/// <returns></returns>
		public static bool Crosses(Geometry g1, Geometry g2)
		{
			Geometry g = g2.Intersection(g1);
			return (g.Intersection(g1).Dimension < Math.Max(g1.Dimension, g2.Dimension) && !g.Equals(g1) && !g.Equals(g2));
		}

		/// <summary>
		/// Returns TRUE if otherGeometry is disjoint from the source geometry.
		/// </summary>
		/// <param name="g1"></param>
		/// <param name="g2"></param>
		/// <returns></returns>
		public static bool Disjoint(Geometry g1, Geometry g2)
		{
			return !g2.Intersects(g1);
		}

		/// <summary>
		/// Returns TRUE if otherGeometry is of the same type and defines the same point set as the source geometry.
		/// </summary>
		/// <param name="g1">source geometry</param>
		/// <param name="g2">other Geometry</param>
		/// <returns></returns>
		public static bool Equals(Geometry g1, Geometry g2)
		{
			if (g1.GetType() != g2.GetType())
				return false;
			switch(g1.GetType().FullName)
			{
				case "SharpMap.Geometries.Point":
					return ((Point)g1).Equals((Point)g2);
				case "SharpMap.Geometries.LineString":
					return ((LineString)g1).Equals((LineString)g2);
				case "SharpMap.Geometries.Polygon":
					return ((Polygon)g1).Equals((Polygon)g2);
				//case "SharpMap.Geometries.Surface":
				//	return ((Surface)g1).Equals((Surface)g2);
				//case "SharpMap.Geometries.Curve":
				//	return ((Curve)g1).Equals((Curve)g2);
				case "SharpMap.Geometries.MultiPoint":
					return ((MultiPoint)g1).Equals((MultiPoint)g2);
				case "SharpMap.Geometries.MultiLineString":
					return ((MultiLineString)g1).Equals((MultiLineString)g2);
				case "SharpMap.Geometries.MultiPolygon":
					return ((MultiPolygon)g1).Equals((MultiPolygon)g2);
				//case GeometryType.MultiSurface:
				//	return ((MultiSurface)g1).Equals((MultiSurface)g2);
				//case GeometryType.MultiCurve:
				//	return ((MultiCurve)g1).Equals((MultiCurve)g2);
				default:
					throw new ArgumentException("The method or operation is not implemented on this geometry type.");
			}			
		}

		/// <summary>
		/// Returns TRUE if there is any intersection between the two geometries.
		/// </summary>
		/// <param name="g1"></param>
		/// <param name="g2"></param>
		/// <returns></returns>
		public static bool Intersects(Geometry g1, Geometry g2)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Returns TRUE if the intersection of the two geometries results in an object of the same dimension as the
		/// input geometries and the intersection geometry is not equal to either geometry.
		/// </summary>
		/// <param name="g1"></param>
		/// <param name="g2"></param>
		/// <returns></returns>
		public static bool Overlaps(Geometry g1, Geometry g2)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Returns TRUE if the only points in common between the two geometries lie in the union of their boundaries.
		/// </summary>
		/// <param name="g1"></param>
		/// <param name="g2"></param>
		/// <returns></returns>
		public static bool Touches(Geometry g1, Geometry g2)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Returns TRUE if the primary geometry is wholly contained within the comparison geometry.
		/// </summary>
		/// <param name="g1"></param>
		/// <param name="g2"></param>
		/// <returns></returns>
		public static bool Within(Geometry g1, Geometry g2)
		{
			return g1.Contains(g2);
		}
	}
}
