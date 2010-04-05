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
	/// Class for defining units
	/// </summary>
    public class Unit : Info, IUnit
    {
		/// <summary>
		/// Initializes a new unit
		/// </summary>
		/// <param name="conversionFactor">Conversion factor to base unit</param>
		/// <param name="name">Name of unit</param>
		/// <param name="authority">Authority name</param>
		/// <param name="authorityCode">Authority-specific identification code.</param>
		/// <param name="alias">Alias</param>
		/// <param name="abbreviation">Abbreviation</param>
		/// <param name="remarks">Provider-supplied remarks</param>
		internal Unit(double conversionFactor, string name, string authority, long authorityCode, string alias, string abbreviation, string remarks)
			:
			base(name, authority, authorityCode, alias, abbreviation, remarks)
		{
			_ConversionFactor = conversionFactor;
		}

		/// <summary>
		/// Initializes a new unit
		/// </summary>
		/// <param name="name">Name of unit</param>
		/// <param name="conversionFactor">Conversion factor to base unit</param>
		internal Unit(string name, double conversionFactor)
			: this(conversionFactor, name, String.Empty, -1, String.Empty, String.Empty, String.Empty)
		{
		}

		private double _ConversionFactor;

		/// <summary>
		/// Gets or sets the number of units per base-unit.
		/// </summary>
		public double ConversionFactor
		{
			get { return _ConversionFactor; }
			set { _ConversionFactor = value; }
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
				sb.AppendFormat("UNIT[\"{0}\", {1}", Name, _ConversionFactor);
#else
                System.Globalization.CultureInfo CI = new System.Globalization.CultureInfo("");
                sb.AppendFormat(CI, "UNIT[\"{0}\", {1}", Name, _ConversionFactor);
#endif
				if (!String.IsNullOrEmpty(Authority) && AuthorityCode > 0)
#if !CFBuild //CF needs a CultureInfo overload.This can likely be changed in the full framework version with no ill effect.
					sb.AppendFormat(", AUTHORITY[\"{0}\", \"{1}\"]", Authority, AuthorityCode);
#else
                    sb.AppendFormat(CI,", AUTHORITY[\"{0}\", \"{1}\"]", Authority, AuthorityCode);
#endif
				sb.Append("]");
				return sb.ToString();
			}
		}
    }
}
