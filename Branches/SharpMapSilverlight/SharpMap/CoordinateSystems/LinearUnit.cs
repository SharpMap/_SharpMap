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
	/// Definition of linear units.
	/// </summary>
	public class LinearUnit : Info, ILinearUnit
	{
		/// <summary>
		/// Creates an instance of a linear unit
		/// </summary>
		/// <param name="metersPerUnit">Number of meters per <see cref="LinearUnit" /></param>
		/// <param name="name">Name</param>
		/// <param name="authority">Authority name</param>
		/// <param name="authorityCode">Authority-specific identification code.</param>
		/// <param name="alias">Alias</param>
		/// <param name="abbreviation">Abbreviation</param>
		/// <param name="remarks">Provider-supplied remarks</param>
		public LinearUnit(double metersPerUnit, string name, string authority, long authorityCode, string alias, string abbreviation, string remarks)
			:
			base(name, authority, authorityCode, alias, abbreviation, remarks)
		{
			_MetersPerUnit = metersPerUnit;
		}

		#region Predefined units
		/// <summary>
		/// Returns the meters linear unit.
		/// Also known as International metre. SI standard unit.
		/// </summary>
		public static ILinearUnit Metre
		{
			get { return new LinearUnit(1.0,"metre", "EPSG", 9001, "m", String.Empty, "Also known as International metre. SI standard unit."); }
		}
		/// <summary>
		/// Returns the foot linear unit (1ft = 0.3048m).
		/// </summary>
		public static ILinearUnit Foot
		{
			get { return new LinearUnit(0.3048, "foot", "EPSG", 9002, "ft", String.Empty, String.Empty); }
		}
		/// <summary>
		/// Returns the US Survey foot linear unit (1ftUS = 0.304800609601219m).
		/// </summary>
		public static ILinearUnit USSurveyFoot
		{
			get { return new LinearUnit(0.304800609601219, "US survey foot", "EPSG", 9003, "American foot", "ftUS", "Used in USA."); }
		}
		/// <summary>
		/// Returns the Nautical Mile linear unit (1NM = 1852m).
		/// </summary>
		public static ILinearUnit NauticalMile
		{
			get { return new LinearUnit(1852, "nautical mile", "EPSG", 9030, "NM", String.Empty, String.Empty); }
		}
		#endregion

		#region ILinearUnit Members

		private double _MetersPerUnit;

		/// <summary>
		/// Gets or sets the number of meters per <see cref="LinearUnit"/>.
		/// </summary>
		public double MetersPerUnit
		{
			get { return _MetersPerUnit; }
			set { _MetersPerUnit = value; }
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
				sb.AppendFormat("UNIT[\"{0}\", {1}", Name, MetersPerUnit);
#else
                System.Globalization.CultureInfo CI = new System.Globalization.CultureInfo("");
                sb.AppendFormat(CI, "UNIT[\"{0}\", {1}", Name, MetersPerUnit);
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