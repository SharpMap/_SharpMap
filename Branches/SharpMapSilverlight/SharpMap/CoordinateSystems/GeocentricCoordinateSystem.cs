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
	/// A 3D coordinate system, with its origin at the center of the Earth.
	/// </summary>
	public class GeocentricCoordinateSystem : CoordinateSystem, IGeocentricCoordinateSystem
	{
		internal GeocentricCoordinateSystem(IHorizontalDatum datum, ILinearUnit linearUnit, IPrimeMeridian primeMeridian,
			string name, string authority, long code, string alias, 
			string remarks, string abbreviation)
			: base(name, authority, code, alias, abbreviation, remarks)
		{
			_HorizontalDatum = datum;
			_LinearUnit = linearUnit;
			_Primemeridan = primeMeridian;
		}


		#region IGeocentricCoordinateSystem Members

		private IHorizontalDatum _HorizontalDatum;

		/// <summary>
		/// Returns the HorizontalDatum. The horizontal datum is used to determine where
		/// the centre of the Earth is considered to be. All coordinate points will be 
		/// measured from the centre of the Earth, and not the surface.
		/// </summary>
		public IHorizontalDatum HorizontalDatum
		{
			get { return _HorizontalDatum; }
			set { _HorizontalDatum = value; }
		}

		private ILinearUnit _LinearUnit;

		/// <summary>
		/// Gets the units used along all the axes.
		/// </summary>
		public ILinearUnit LinearUnit
		{
			get { return _LinearUnit; }
			set { _LinearUnit = value; }
		}

		private IPrimeMeridian _Primemeridan;

		/// <summary>
		/// Returns the PrimeMeridian.
		/// </summary>
		public IPrimeMeridian PrimeMeridian
		{
			get { return _Primemeridan; }
			set { _Primemeridan = value; }
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
				sb.AppendFormat("GEOCCS[\"{0}\", {1}, {2}, {3}", Name, HorizontalDatum.WKT, PrimeMeridian.WKT, LinearUnit.WKT);
#else
                System.Globalization.CultureInfo CI = new System.Globalization.CultureInfo("");
                sb.AppendFormat(CI, "GEOCCS[\"{0}\", {1}, {2}, {3}", Name, HorizontalDatum.WKT, PrimeMeridian.WKT, LinearUnit.WKT);
#endif
				if(AxisInfo!=null)
					for (int i = 0; i < AxisInfo.Count; i++)
#if !CFBuild //CF needs a CultureInfo overload.This can likely be changed in the full framework version with no ill effect.
						sb.AppendFormat(", {0}", GetAxis(i).WKT);
#else
                        sb.AppendFormat(CI, ", {0}", GetAxis(i).WKT);
#endif
				if (!String.IsNullOrEmpty(Authority) && AuthorityCode>0)

#if !CFBuild //CF needs a CultureInfo overload. This can likely be changed in the full framework version with no ill effect.
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
