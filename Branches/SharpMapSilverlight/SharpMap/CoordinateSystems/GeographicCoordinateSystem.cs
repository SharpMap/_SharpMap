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

namespace SharpMap.CoordinateSystems
{
	/// <summary>
	/// A coordinate system based on latitude and longitude. 
	/// </summary>
	/// <remarks>
	/// Some geographic coordinate systems are Lat/Lon, and some are Lon/Lat. 
	/// You can find out which this is by examining the axes. You should also 
	/// check the angular units, since not all geographic coordinate systems 
	/// use degrees.
	/// </remarks>
	public class GeographicCoordinateSystem : HorizontalCoordinateSystem, IGeographicCoordinateSystem
	{

		/// <summary>
		/// Creates an instance of a Geographic Coordinate System
		/// </summary>
		/// <param name="angularUnit">Angular units</param>
		/// <param name="horizontalDatum">Horizontal datum</param>
		/// <param name="primeMeridian">Prime meridian</param>
		/// <param name="axisInfo">Axis info</param>
		/// <param name="name">Name</param>
		/// <param name="authority">Authority name</param>
		/// <param name="authorityCode">Authority-specific identification code.</param>
		/// <param name="alias">Alias</param>
		/// <param name="abbreviation">Abbreviation</param>
		/// <param name="remarks">Provider-supplied remarks</param>
		internal GeographicCoordinateSystem(IAngularUnit angularUnit, IHorizontalDatum horizontalDatum, IPrimeMeridian primeMeridian, List<AxisInfo> axisInfo, string name, string authority, long authorityCode, string alias, string abbreviation, string remarks)
			:
			base(horizontalDatum, axisInfo, name, authority, authorityCode, alias, abbreviation, remarks)
		{
			_AngularUnit = angularUnit;
			_PrimeMeridian = primeMeridian;

		}

		#region Predefined geographic coordinate systems

		/// <summary>
		/// Creates a decimal degrees geographic coordinate system based on the WGS84 ellipsoid, suitable for GPS measurements
		/// </summary>
		public static GeographicCoordinateSystem WGS84
		{
			get {
				List<AxisInfo> axes = new List<AxisInfo>(2);
				axes.Add(new AxisInfo("Lon", AxisOrientationEnum.East));
				axes.Add(new AxisInfo("Lat", AxisOrientationEnum.North));
				return new GeographicCoordinateSystem(SharpMap.CoordinateSystems.AngularUnit.Degrees,
					SharpMap.CoordinateSystems.HorizontalDatum.WGS84, SharpMap.CoordinateSystems.PrimeMeridian.Greenwich, axes,
					"WGS 84", "EPSG", 4326, String.Empty, string.Empty, string.Empty);
			}
		}

		#endregion

		#region IGeographicCoordinateSystem Members
		
		private IAngularUnit _AngularUnit;

		/// <summary>
		/// Gets or sets the angular units of the geographic coordinate system.
		/// </summary>
		public IAngularUnit AngularUnit
		{
			get { return _AngularUnit; }
			set { _AngularUnit = value; }
		}

		private IPrimeMeridian _PrimeMeridian;

		/// <summary>
		/// Gets or sets the prime meridian of the geographic coordinate system.
		/// </summary>
		public IPrimeMeridian PrimeMeridian
		{
			get { return _PrimeMeridian; }
			set { _PrimeMeridian = value; }
		}
			
		/// <summary>
		/// Gets the number of available conversions to WGS84 coordinates.
		/// </summary>
		public int NumConversionToWGS84
		{
			get { return _WGS84ConversionInfo.Count; }
		}

		private List<Wgs84ConversionInfo> _WGS84ConversionInfo;
		
		internal List<Wgs84ConversionInfo> WGS84ConversionInfo
		{
			get { return _WGS84ConversionInfo; }
			set { _WGS84ConversionInfo = value; }
		}

		/// <summary>
		/// Gets details on a conversion to WGS84.
		/// </summary>
		public Wgs84ConversionInfo GetWgs84ConversionInfo(int index)
		{
			return _WGS84ConversionInfo[index];
		}

		/// <summary>
		/// Returns the Well-known text for this object
		/// as defined in the simple features specification.
		/// </summary>
		public override string WKT
		{
			get
			{
				StringBuilder sb = new StringBuilder();
#if !CFBuild //CF needs a CultureInfo overload.This can likely be changed in the full framework version with no ill effect.
				sb.AppendFormat("GEOGCS[\"{0}\", {1}, {2}, {3}",Name, HorizontalDatum.WKT, PrimeMeridian.WKT, AngularUnit.WKT);
#else
                System.Globalization.CultureInfo CI = new System.Globalization.CultureInfo("");
                sb.AppendFormat(CI, "GEOGCS[\"{0}\", {1}, {2}, {3}", Name, HorizontalDatum.WKT, PrimeMeridian.WKT, AngularUnit.WKT);
#endif
				for (int i = 0; i < AxisInfo.Count; i++)
#if !CFBuild //CF needs a CultureInfo overload.This can likely be changed in the full framework version with no ill effect.
					sb.AppendFormat(", {0}", GetAxis(i).WKT);
#else
                    sb.AppendFormat(CI, ", {0}", GetAxis(i).WKT);
#endif
				if (!String.IsNullOrEmpty(Authority) && AuthorityCode > 0)
#if !CFBuild //CF needs a CultureInfo overload.This can likely be changed in the full framework version with no ill effect.
					sb.AppendFormat(", AUTHORITY[\"{0}\", \"{1}\"]", Authority, AuthorityCode);
#else
                    sb.AppendFormat(CI, ", AUTHORITY[\"{0}\", \"{1}\"]", Authority, AuthorityCode);
#endif
				sb.Append("]");
				return sb.ToString();
			}
		}
		#endregion
	}
}
