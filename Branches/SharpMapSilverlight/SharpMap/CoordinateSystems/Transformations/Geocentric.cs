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
using SharpMap.Geometries;
using SharpMap.CoordinateSystems;
using SharpMap.CoordinateSystems.Transformations;

namespace SharpMap.CoordinateSystems.Transformations
{
	/// <summary>
	/// 
	/// </summary>
	/// <remarks>
	/// <para>Latitude, Longitude and ellipsoidal height in terms of a 3-dimensional geographic system
	/// may by expressed in terms of a geocentric (earth centered) Cartesian coordinate reference system
	/// X, Y, Z with the Z axis corresponding to the earth's rotation axis positive northwards, the X
	/// axis through the intersection of the prime meridian and equator, and the Y axis through
	/// the intersection of the equator with longitude 90 degrees east. The geographic and geocentric
	/// systems are based on the same geodetic datum.</para>
	/// <para>Geocentric coordinate reference systems are conventionally taken to be defined with the X
	/// axis through the intersection of the Greenwich meridian and equator. This requires that the equivalent
	/// geographic coordinate reference systems based on a non-Greenwich prime meridian should first be
	/// transformed to their Greenwich equivalent. Geocentric coordinates X, Y and Z take their units from
	/// the units of the ellipsoid axes (a and b). As it is conventional for X, Y and Z to be in metres,
	/// if the ellipsoid axis dimensions are given in another linear unit they should first be converted
	/// to metres.</para>
	/// </remarks>
	public class Geocentric : MathTransform
	{
		protected bool _isInverse = false;
		private double es;				// eccentricity squared
		private double semiMajor;		// major axis
		private double semiMinor;		// minor axis
		private double ab;				// Semi_major / semi_minor
		private double ba;				// Semi_minor / semi_major
		private double ses;				// second eccentricity squared : (a^2 - b^2)/b^2    
		protected List<ProjectionParameter> _Parameters;
		protected MathTransform _inverse;

		protected Geocentric(List<ProjectionParameter> parameters, bool isInverse)
			: this(parameters)
		{
			_isInverse = isInverse;
		}

		internal Geocentric(List<ProjectionParameter> parameters)
		{
			_Parameters = parameters;
			semiMajor = _Parameters.Find(delegate(ProjectionParameter par)
								{ return par.Name.Equals("semi_major", StringComparison.OrdinalIgnoreCase); }).Value;
			semiMinor = _Parameters.Find(delegate(ProjectionParameter par)
								{ return par.Name.Equals("semi_minor", StringComparison.OrdinalIgnoreCase); }).Value;
		
			es = 1.0 - (semiMinor * semiMinor ) / (semiMajor * semiMajor); //e^2
			ses = (Math.Pow(semiMajor, 2) - Math.Pow(semiMinor, 2)) / Math.Pow(semiMinor, 2);			
			ba = semiMinor / semiMajor;
			ab = semiMajor / semiMinor;
			//this._e  = Math.Sqrt(_es); //e (eccentricity)
		}


		/// <summary>
		/// Returns the inverse of this conversion.
		/// </summary>
		/// <returns>IMathTransform that is the reverse of the current conversion.</returns>
		public override IMathTransform Inverse()
		{
			if (_inverse == null)
				_inverse = new Geocentric(this._Parameters, !_isInverse);
			return _inverse;
		}

		/// <summary>
		/// Converts coordinates in decimal degrees to projected meters.
		/// </summary>
		/// <param name="lonlat">The point in decimal degrees.</param>
		/// <returns>Point in projected meters</returns>
		private SharpMap.Geometries.Point DegreesToMeters(SharpMap.Geometries.Point lonlat)
		{
			double lon = Degrees2Radians(lonlat.X);
			double lat = Degrees2Radians(lonlat.Y);
			double h = 0;
			if (lonlat is Point3D) h = (lonlat as Point3D).Z;
			double v = semiMajor / Math.Sqrt(1 - es * Math.Pow(Math.Sin(lat), 2));
			double x = (v + h) * Math.Cos(lat) * Math.Cos(lon);
			double y = (v + h) * Math.Cos(lat) * Math.Sin(lon);
			double z = ((1 - es) * v + h) * Math.Sin(lat);
			return new SharpMap.Geometries.Point3D(x, y, z);
		}
		/// <summary>
		/// Converts coordinates in projected meters to decimal degrees.
		/// </summary>
		/// <param name="p">Point in meters</param>
		/// <returns>Transformed point in decimal degrees</returns>
		private SharpMap.Geometries.Point MetersToDegrees(SharpMap.Geometries.Point pnt)
		{
			if (!(pnt is Point3D))
				throw new ArgumentException("Need 3D point to convert from geocentric coordinates");
			double p = Math.Sqrt(pnt.X * pnt.X + pnt.Y * pnt.Y);
			double lon = Math.Atan(pnt.Y / pnt.X);
			double Z = (pnt as Point3D).Z;
			double prevLat = Double.MaxValue;
			int i = 0;
			//Use Bowrings iterative method for finding the latitude
			double u = Math.Atan(ab * Z / p); //Initial latitude guess
			double lat = Double.MinValue;
			while (Math.Abs(lat - prevLat) > 0.000000001) //Iterate towards latitude
			{
				prevLat = lat;
				lat = Math.Atan(ab * (Z + ses * semiMinor * Math.Pow(Math.Sin(u), 3)) /
						(pnt.X - es * semiMajor * Math.Pow(Math.Cos(u), 3)));
				u = Math.Atan(ab* Math.Tan(lat));
				i++;
				if (i > 25)
					throw new ApplicationException("Conversion failed to converge");
			}
			if(pnt is Point3D)
			{
				double v = semiMajor / Math.Sqrt(1 - es * Math.Pow(Math.Sin(lat), 2));
				double h = pnt.X * lat * 3600 * lon * 3600 - v;
				double n = Math.Pow(semiMajor, 2) / Math.Sqrt(Math.Pow(semiMajor, 2) * Math.Pow(Math.Cos(lat), 2) + Math.Pow(semiMinor, 2) * Math.Pow(Math.Sin(lat), 2));
				h = p / Math.Cos(lat) - n;
				lat = Radians2Degrees(lat);
				lon = Radians2Degrees(lon);
				return new Point3D(lon, lat, h);
			}
			else
				return new Point(lon, lat);
		}
		
		public override SharpMap.Geometries.Point Transform(SharpMap.Geometries.Point point)
		{
			if (!_isInverse)
				return this.DegreesToMeters(point);
			else
				return this.MetersToDegrees(point);
		}

		public override List<SharpMap.Geometries.Point> TransformList(List<SharpMap.Geometries.Point> points)
		{
			List<SharpMap.Geometries.Point> result = new List<SharpMap.Geometries.Point>(points.Count);
			for (int i = 0; i < points.Count; i++)
			{
				SharpMap.Geometries.Point point = points[i];
				result.Add(Transform(point));
			}
			return result;
		}

		/// <summary>
		/// Reverses the transformation
		/// </summary>
		public override void Invert()
		{
			_isInverse = !_isInverse;
		}
		
	}
}
