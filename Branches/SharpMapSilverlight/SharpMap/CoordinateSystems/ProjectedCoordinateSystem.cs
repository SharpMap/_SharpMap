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
	/// A 2D cartographic coordinate system.
	/// </summary>
	public class ProjectedCoordinateSystem : HorizontalCoordinateSystem,  IProjectedCoordinateSystem
	{
		/// <summary>
		/// Initializes a new instance of a projected coordinate system
		/// </summary>
		/// <param name="datum">Horizontal datum</param>
		/// <param name="geographicCoordinateSystem">Geographic coordinate system</param>
		/// <param name="linearUnit">Linear unit</param>
		/// <param name="projection">Projection</param>
		/// <param name="axisInfo">Axis info</param>
		/// <param name="name">Name</param>
		/// <param name="authority">Authority name</param>
		/// <param name="code">Authority-specific identification code.</param>
		/// <param name="alias">Alias</param>
		/// <param name="abbreviation">Abbreviation</param>
		/// <param name="remarks">Provider-supplied remarks</param>
		internal ProjectedCoordinateSystem(IHorizontalDatum datum, IGeographicCoordinateSystem geographicCoordinateSystem,
			ILinearUnit linearUnit, IProjection projection, List<AxisInfo> axisInfo,
			string name, string authority, long code, string alias,
			string remarks, string abbreviation)
			: base(datum, axisInfo, name, authority, code, alias, abbreviation, remarks)
		{
			_GeographicCoordinateSystem = geographicCoordinateSystem;
			_LinearUnit = linearUnit;
			_Projection = projection;
		}

		#region Predefined projected coordinate systems
/*
		/// <summary>
		/// Universal Transverse Mercator - WGS84
		/// </summary>
		/// <param name="Zone">UTM zone</param>
		/// <param name="ZoneIsNorth">true of Northern hemisphere, false if southern</param>
		/// <returns>UTM/WGS84 coordsys</returns>
		public static ProjectedCoordinateSystem WGS84_UTM(int Zone, bool ZoneIsNorth)
		{
			ParameterInfo pInfo = new ParameterInfo();
			pInfo.Add("latitude_of_origin", 0);
			pInfo.Add("central_meridian", Zone * 6 - 183);
			pInfo.Add("scale_factor", 0.9996);
			pInfo.Add("false_easting", 500000);
			pInfo.Add("false_northing", ZoneIsNorth ? 0 : 10000000);
			Projection proj = new Projection(String.Empty,String.Empty,pInfo,AngularUnit.Degrees,
				SharpMap.SpatialReference.LinearUnit.Metre,Ellipsoid.WGS84,
				"Transverse_Mercator", "EPSG", 32600 + Zone + (ZoneIsNorth ? 0 : 100), String.Empty, String.Empty, String.Empty);

			return new ProjectedCoordinateSystem("Large and medium scale topographic mapping and engineering survey.",
				SharpMap.SpatialReference.GeographicCoordinateSystem.WGS84,
				SharpMap.SpatialReference.LinearUnit.Metre, proj, pInfo,
				"WGS 84 / UTM zone " + Zone.ToString() + (ZoneIsNorth ? "N" : "S"), "EPSG", 32600 + Zone + (ZoneIsNorth ? 0 : 100),
				String.Empty,String.Empty,string.Empty);
			
		}*/

		#endregion

		#region IProjectedCoordinateSystem Members

		private IGeographicCoordinateSystem _GeographicCoordinateSystem;

		/// <summary>
		/// Gets or sets the GeographicCoordinateSystem.
		/// </summary>
		public IGeographicCoordinateSystem GeographicCoordinateSystem
		{
			get { return _GeographicCoordinateSystem; }
			set { _GeographicCoordinateSystem = value; }
		}

		private ILinearUnit _LinearUnit;

		/// <summary>
		/// Gets or sets the <see cref="LinearUnit">LinearUnits</see>. The linear unit must be the same as the <see cref="CoordinateSystem"/> units.
		/// </summary>
		public ILinearUnit LinearUnit
		{
			get { return _LinearUnit; }
			set { _LinearUnit = value; }
		}

		private IProjection _Projection;

		/// <summary>
		/// Gets or sets the projection
		/// </summary>
		public IProjection Projection
		{
			get { return _Projection; }
			set { _Projection = value; }
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
				sb.AppendFormat("PROJCS[\"{0}\", {1}, {2}",Name, GeographicCoordinateSystem.WKT, Projection.WKT);
#else
                System.Globalization.CultureInfo CI = new System.Globalization.CultureInfo("");
                sb.AppendFormat(CI, "PROJCS[\"{0}\", {1}, {2}", Name, GeographicCoordinateSystem.WKT, Projection.WKT);
#endif
				for(int i=0;i<Projection.NumParameters;i++)
#if !CFBuild //CF needs a CultureInfo overload.This can likely be changed in the full framework version with no ill effect.
					sb.AppendFormat(", {0}", Projection.GetParameter(i).WKT);
#else
                    sb.AppendFormat(CI,", {0}", Projection.GetParameter(i).WKT);
#endif
				for (int i = 0; i < AxisInfo.Count; i++)
#if !CFBuild //CF needs a CultureInfo overload. This can likely be changed in the full framework version with no ill effect.
					sb.AppendFormat(", {0}", GetAxis(i).WKT);
#else
                    sb.AppendFormat(CI,", {0}", GetAxis(i).WKT);
#endif

				if(!String.IsNullOrEmpty(Authority))
#if !CFBuild //CF needs a CultureInfo overload. This can likely be changed in the full framework version with no ill effect.
					sb.AppendFormat(", {0}]", Authority);
#else
                    sb.AppendFormat(CI,", {0}]", Authority);
#endif

				sb.Append("]");
				return sb.ToString();
			}
		}

		#endregion
	}
}
