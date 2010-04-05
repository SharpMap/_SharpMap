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
	/// The Projection class defines the standard information stored with a projection
	/// objects. A projection object implements a coordinate transformation from a geographic
	/// coordinate system to a projected coordinate system, given the ellipsoid for the
	/// geographic coordinate system. It is expected that each coordinate transformation of
	/// interest, e.g., Transverse Mercator, Lambert, will be implemented as a class of
	/// type Projection, supporting the IProjection interface.
	/// </summary>
	public class Projection : Info, IProjection
	{
		internal Projection(string className, List<ProjectionParameter> parameters,
			string name, string authority, long code, string alias, 
			string remarks, string abbreviation)
			: base(name, authority, code, alias, abbreviation, remarks)
		{
			_Parameters = parameters;
			_ClassName = className;
		}

		#region Predefined projections
		#endregion

		#region IProjection Members

		/// <summary>
		/// Gets the number of parameters of the projection.
		/// </summary>
		public int NumParameters
		{
			get { return _Parameters.Count; }
		}

		private List<ProjectionParameter> _Parameters;

		/// <summary>
		/// Gets or sets the parameters of the projection
		/// </summary>
		internal List<ProjectionParameter> Parameters
		{
			get { return _Parameters; }
			set { _Parameters = value; }
		}

		/// <summary>
		/// Gets an indexed parameter of the projection.
		/// </summary>
		/// <param name="n">Index of parameter</param>
		/// <returns>n'th parameter</returns>
		public ProjectionParameter GetParameter(int n)
		{
			return _Parameters[n];
		}
				
		private string _ClassName;

		/// <summary>
		/// Gets the projection classification name (e.g. "Transverse_Mercator").
		/// </summary>
		public string ClassName
		{
			get { return _ClassName; }
		}

		/// <summary>
		/// Returns the Well-known text for this object
		/// as defined in the simple features specification.
		/// </summary>
		public override string WKT
		{
			get
			{
					return String.Format("PROJECTION[\"{0}\", {1}]", Name, Authority);
			}
		}

		#endregion
	}
}
