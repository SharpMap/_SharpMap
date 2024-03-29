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
using System.Collections.ObjectModel;
using System.Text;

namespace SharpMap.CoordinateSystems
{
	/// <summary>
	/// The GeographicTransform class is implemented on geographic transformation objects and
	/// implements datum transformations between geographic coordinate systems.
	/// </summary>
	public class GeographicTransform : Info, IGeographicTransform
	{
		internal GeographicTransform(
			string name, string authority, long code, string alias, string remarks, string abbreviation,
			IGeographicCoordinateSystem sourceGCS, IGeographicCoordinateSystem targetGCS)
			: base(name, authority, code, alias, abbreviation, remarks)
		{
			_SourceGCS = sourceGCS;
			_TargetGCS = targetGCS;
		}

		#region IGeographicTransform Members

		private IGeographicCoordinateSystem _SourceGCS;

		/// <summary>
		/// Gets or sets the source geographic coordinate system for the transformation.
		/// </summary>
		public IGeographicCoordinateSystem SourceGCS
		{
			get { return _SourceGCS; }
			set { _SourceGCS = value; }
		}

		private IGeographicCoordinateSystem _TargetGCS;

		/// <summary>
		/// Gets or sets the target geographic coordinate system for the transformation.
		/// </summary>
		public IGeographicCoordinateSystem TargetGCS
		{
			get { return _TargetGCS; }
			set { _TargetGCS = value; }
		}

		/// <summary>
		/// Returns an accessor interface to the parameters for this geographic transformation.
		/// </summary>
		public IParameterInfo ParameterInfo
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary>
		/// Transforms an array of points from the source geographic coordinate
		/// system to the target geographic coordinate system.
		/// </summary>
		/// <param name="points">On input points in the source geographic coordinate system</param>
		/// <returns>Output points in the target geographic coordinate system</returns>
		public Collection<SharpMap.Geometries.Point> Forward(Collection<SharpMap.Geometries.Point> points)
		{
			throw new NotImplementedException();
			/*
			List<SharpMap.Geometries.Point> trans = new List<SharpMap.Geometries.Point>(points.Count);
			foreach (SharpMap.Geometries.Point p in points)
			{

			}
			return trans;
			*/
		}

		/// <summary>
		/// Transforms an array of points from the target geographic coordinate
		/// system to the source geographic coordinate system.
		/// </summary>
		/// <param name="points">Input points in the target geographic coordinate system,</param>
		/// <returns>Output points in the source geographic coordinate system</returns>
		public Collection<SharpMap.Geometries.Point> Inverse(Collection<SharpMap.Geometries.Point> points)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Returns the Well-known text for this object
		/// as defined in the simple features specification.
		/// </summary>
		public override string WKT
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		/// <summary>
		/// Gets an XML representation of this object [NOT IMPLEMENTED].
		/// </summary>
		public override string XML
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		/// <summary>
		/// Checks whether the values of this instance is equal to the values of another instance.
		/// Only parameters used for coordinate system are used for comparison.
		/// Name, abbreviation, authority, alias and remarks are ignored in the comparison.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns>True if equal</returns>
		public override bool EqualParams(object obj)
		{
			if (!(obj is GeographicTransform))
				return false;
			GeographicTransform gt = obj as GeographicTransform;
			return gt.SourceGCS.EqualParams(this.SourceGCS) && gt.TargetGCS.EqualParams(this.TargetGCS);
		}
		#endregion
	}
}
