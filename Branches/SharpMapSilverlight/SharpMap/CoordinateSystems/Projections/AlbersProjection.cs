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

// SOURCECODE IS MODIFIED FROM ANOTHER WORK AND IS ORIGINALLY BASED ON GeoTools.NET:
/*
 *  Copyright (C) 2002 Urban Science Applications, Inc. 
 *
 *  This library is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public
 *  License as published by the Free Software Foundation; either
 *  version 2.1 of the License, or (at your option) any later version.
 *
 *  This library is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 *  Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public
 *  License along with this library; if not, write to the Free Software
 *  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
 *
 */

using System;
using System.Collections.Generic;
using SharpMap.Geometries;
using SharpMap.CoordinateSystems;
using SharpMap.CoordinateSystems.Transformations;

namespace SharpMap.CoordinateSystems.Projections
{
	/// <summary>
	///		Implements the Albers projection.
	/// </summary>
	/// <remarks>
	/// 	<para>Implements the Albers projection. The Albers projection is most commonly
	/// 	used to project the United States of America. It gives the northern
	/// 	border with Canada a curved appearance.</para>
	/// 	
	///		<para>The <a href="http://www.geog.mcgill.ca/courses/geo201/mapproj/naaeana.gif">Albers Equal Area</a>
	///		projection has the property that the area bounded
	///		by any pair of parallels and meridians is exactly reproduced between the 
	///		image of those parallels and meridians in the projected domain, that is,
	///		the projection preserves the correct area of the earth though distorts
	///		direction, distance and shape somewhat.</para>
	/// </remarks>
	internal class AlbersProjection : MapProjection
	{
		double _falseEasting;
		double _falseNorthing;
		double c;				//constant c 
		double e3;				//eccentricity
		double rh;				//heigth above elipsoid  
		double ns0;				//ratio between meridians
		double lon_center;		//center longitude   
		double es=0;

		#region Constructors

		/// <summary>
		/// Creates an instance of an Albers projection object.
		/// </summary>
		/// <param name="parameters">List of parameters to initialize the projection.</param>
		/// <remarks>
		/// <para>The parameters this projection expects are listed below.</para>
		/// <list type="table">
		/// <listheader><term>Items</term><description>Descriptions</description></listheader>
		/// <item><term>latitude_of_false_origin</term><description>The latitude of the point which is not the natural origin and at which grid coordinate values false easting and false northing are defined.</description></item>
		/// <item><term>longitude_of_false_origin</term><description>The longitude of the point which is not the natural origin and at which grid coordinate values false easting and false northing are defined.</description></item>
		/// <item><term>latitude_of_1st_standard_parallel</term><description>For a conic projection with two standard parallels, this is the latitude of intersection of the cone with the ellipsoid that is nearest the pole.  Scale is true along this parallel.</description></item>
		/// <item><term>latitude_of_2nd_standard_parallel</term><description>For a conic projection with two standard parallels, this is the latitude of intersection of the cone with the ellipsoid that is furthest from the pole.  Scale is true along this parallel.</description></item>
		/// <item><term>easting_at_false_origin</term><description>The easting value assigned to the false origin.</description></item>
		/// <item><term>northing_at_false_origin</term><description>The northing value assigned to the false origin.</description></item>
		/// </list>
		/// </remarks>
		public AlbersProjection(List<ProjectionParameter> parameters)
			: this(parameters, false)
		{
		}
		
		/// <summary>
		/// Creates an instance of an Albers projection object.
		/// </summary>
		/// <remarks>
		/// <para>The parameters this projection expects are listed below.</para>
		/// <list type="table">
		/// <listheader><term>Items</term><description>Descriptions</description></listheader>
		/// <item><term>latitude_of_false_origin</term><description>The latitude of the point which is not the natural origin and at which grid coordinate values false easting and false northing are defined.</description></item>
		/// <item><term>longitude_of_false_origin</term><description>The longitude of the point which is not the natural origin and at which grid coordinate values false easting and false northing are defined.</description></item>
		/// <item><term>latitude_of_1st_standard_parallel</term><description>For a conic projection with two standard parallels, this is the latitude of intersection of the cone with the ellipsoid that is nearest the pole.  Scale is true along this parallel.</description></item>
		/// <item><term>latitude_of_2nd_standard_parallel</term><description>For a conic projection with two standard parallels, this is the latitude of intersection of the cone with the ellipsoid that is furthest from the pole.  Scale is true along this parallel.</description></item>
		/// <item><term>easting_at_false_origin</term><description>The easting value assigned to the false origin.</description></item>
		/// <item><term>northing_at_false_origin</term><description>The northing value assigned to the false origin.</description></item>
		/// </list>
		/// </remarks>
		/// <param name="parameters">List of parameters to initialize the projection.</param>
		/// <param name="isInverse">Indicates whether the projection forward (meters to degrees or degrees to meters).</param>
		public AlbersProjection(List<ProjectionParameter> parameters, bool isInverse)
			: base(parameters, isInverse)
		{
			double sin_po,cos_po;	/* sin and cos values					*/
			double con;				/* temporary variable					*/
			double es,temp;			/* eccentricity squared and temp var	*/
			double ms1;				/* small m 1							*/
			double ms2;				/* small m 2							*/
			double qs0;				/* small q 0							*/
			double qs1;				/* small q 1							*/
			double qs2;				/* small q 2							*/

			double lat0 = Degrees2Radians(_Parameters.Find(delegate(ProjectionParameter par) { return par.Name.Equals("latitude_of_false_origin", StringComparison.OrdinalIgnoreCase); }).Value);
			double lon0 = Degrees2Radians(_Parameters.Find(delegate(ProjectionParameter par) { return par.Name.Equals("longitude_of_false_origin", StringComparison.OrdinalIgnoreCase); }).Value);
			double lat1 = Degrees2Radians(_Parameters.Find(delegate(ProjectionParameter par) { return par.Name.Equals("latitude_of_1st_standard_parallel", StringComparison.OrdinalIgnoreCase); }).Value);
			double lat2 = Degrees2Radians(_Parameters.Find(delegate(ProjectionParameter par) { return par.Name.Equals("latitude_of_2nd_standard_parallel", StringComparison.OrdinalIgnoreCase); }).Value);
			this._falseEasting = Degrees2Radians(_Parameters.Find(delegate(ProjectionParameter par) { return par.Name.Equals("easting_at_false_origin", StringComparison.OrdinalIgnoreCase); }).Value);
			this._falseNorthing = Degrees2Radians(_Parameters.Find(delegate(ProjectionParameter par) { return par.Name.Equals("northing_at_false_origin", StringComparison.OrdinalIgnoreCase); }).Value);
			lon_center = lon0;
			if (Math.Abs(lat1 + lat2) < EPSLN)
			{
				throw new ApplicationException("Equal latitudes for St. Parallels on opposite sides of equator.");
			}
			
			temp = this._semiMinor / this._semiMajor;
			es = 1.0 - SQUARE(temp);
			e3 = Math.Sqrt(es);

			sincos(lat1,out sin_po,out cos_po);
			con = sin_po;

			ms1 = msfnz(e3,sin_po,cos_po);
			qs1 = qsfnz(e3,sin_po,cos_po);

			sincos(lat2,out sin_po,out cos_po);

			ms2 = msfnz(e3,sin_po,cos_po);
			qs2 = qsfnz(e3,sin_po,cos_po);

			sincos(lat0,out sin_po,out cos_po);

			qs0 = qsfnz(e3,sin_po,cos_po);

			if (Math.Abs(lat1 - lat2) > EPSLN)
				ns0 = (ms1 * ms1 - ms2 *ms2)/ (qs2 - qs1);
			else
				ns0 = con;
			c = ms1 * ms1 + ns0 * qs1;
			rh = this._semiMajor * Math.Sqrt(c - ns0 * qs0)/ns0;

		}
		#endregion

		#region Methods
		/// <summary>
		/// Converts coordinates in decimal degrees to projected meters.
		/// </summary>
		/// <param name="lonlat">The point in decimal degrees.</param>
		/// <returns>Point in projected meters</returns>
		public override SharpMap.Geometries.Point DegreesToMeters(SharpMap.Geometries.Point lonlat)
		{
			
			double sin_phi,cos_phi;		/* sine and cos values		*/
			double qs;					/* small q			*/
			double theta;				/* angle			*/ 
			double rh1;					/* height above ellipsoid	*/


			double dLongitude = Degrees2Radians(lonlat.X);
			double dLatitude = Degrees2Radians(lonlat.Y);

			sincos(dLatitude,out sin_phi,out cos_phi);
			qs = qsfnz(e3,sin_phi,cos_phi);
			rh1 = this._semiMajor * Math.Sqrt(c - ns0 * qs)/ns0;
			theta = ns0 * adjust_lon(dLongitude - lon_center);
			return new SharpMap.Geometries.Point(
				rh1 * Math.Sin(theta) + this._falseEasting,
				rh - rh1 * Math.Cos(theta) + this._falseNorthing);
			
		}

		/// <summary>
		/// Converts coordinates in projected meters to decimal degrees.
		/// </summary>
		/// <param name="p">Point in meters</param>
		/// <returns>Transformed point in decimal degrees</returns>
		public override SharpMap.Geometries.Point MetersToDegrees(SharpMap.Geometries.Point p) 
		{
			double dLongitude = Double.NaN;
			double dLatitude = Double.NaN;

			double rh1;			/* height above ellipsoid	*/
			double qs;			/* function q			*/
			double con;			/* temporary sign value		*/
			double theta;		/* angle			*/

			long flag=0;		/* error flag 					*/

			double dX = p.X - this._falseEasting;
			double dY = rh - p.Y + this._falseNorthing;;
			if (ns0 >= 0)
			{
				rh1 = Math.Sqrt(dX * dX + dY * dY);
				con = 1.0;
			}
			else
			{
				rh1 = -Math.Sqrt(dX * dX + dY * dY);
				con = -1.0;
			}
			theta = 0.0;
			if (rh1 != 0.0)
				theta = Math.Atan2(con * dX, con * dX);
			con = rh1 * ns0 / this._semiMajor;
			qs = (c - con * con) / ns0;
			if (e3 >= 1e-10)
			{
				con = 1 - .5 * (1.0 - es) * Math.Log((1.0 - e3) / (1.0 + e3))/e3;
				if (Math.Abs(Math.Abs(con) - Math.Abs(qs)) > .0000000001 )
				{
					dLatitude = phi1z(e3, qs, out flag);
					if (flag != 0)
					{  
						throw new ApplicationException();
					}
				}
				else
				{
					if (qs >= 0)
						dLongitude = .5 * PI;
					else
						dLatitude = -.5 * PI;
				}
			}
			else
			{
				dLatitude = phi1z(e3,qs,out flag);
				if (flag != 0)
				{
					throw new ApplicationException();
				}
				 
			}

			dLongitude = adjust_lon(theta/ns0 + lon_center);
			return new SharpMap.Geometries.Point(Radians2Degrees(dLongitude), Radians2Degrees(dLatitude));
		}
		
		/// <summary>
		/// Returns the inverse of this projection.
		/// </summary>
		/// <returns>IMathTransform that is the reverse of the current projection.</returns>
		public override IMathTransform Inverse()
		{
			if (_inverse==null)
			{
				_inverse = new AlbersProjection(this._Parameters, ! _isInverse);
			}
			return _inverse;
		}
		#endregion

	}
}
