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

namespace SharpMap.CoordinateSystems.Transformations
{
	/// <summary>
	/// The GeographicTransform class is implemented on geographic transformation objects and
	/// implements datum transformations between geographic coordinate systems.
	/// </summary>
	public class GeographicTransform : MathTransform
	{
		internal GeographicTransform(IGeographicCoordinateSystem sourceGCS, IGeographicCoordinateSystem targetGCS)
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
		/// Returns the Well-known text for this object
		/// as defined in the simple features specification. [NOT IMPLEMENTED].
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

		#endregion

		/// <summary>
		/// Creates the inverse transform of this object.
		/// </summary>
		/// <remarks>This method may fail if the transform is not one to one. However, all cartographic projections should succeed.</remarks>
		/// <returns></returns>
		public override IMathTransform Inverse()
		{
			throw new Exception("The method or operation is not implemented.");
		}

		/// <summary>
		/// Transforms a coordinate point. The passed parameter point should not be modified.
		/// </summary>
		/// <param name="point"></param>
		/// <returns></returns>
		public override SharpMap.Geometries.Point Transform(SharpMap.Geometries.Point point)
		{
			SharpMap.Geometries.Point pOut = point.Clone();
			pOut.X /= SourceGCS.AngularUnit.RadiansPerUnit;
			pOut.X -= SourceGCS.PrimeMeridian.Longitude / SourceGCS.PrimeMeridian.AngularUnit.RadiansPerUnit;
			pOut.X += TargetGCS.PrimeMeridian.Longitude / TargetGCS.PrimeMeridian.AngularUnit.RadiansPerUnit;
			pOut.X *= SourceGCS.AngularUnit.RadiansPerUnit;
			return pOut;
		}

		/// <summary>
		/// Transforms a list of coordinate point ordinal values.
		/// </summary>
		/// <remarks>
		/// This method is provided for efficiently transforming many points. The supplied array 
		/// of ordinal values will contain packed ordinal values. For example, if the source 
		/// dimension is 3, then the ordinals will be packed in this order (x0,y0,z0,x1,y1,z1 ...).
		/// The size of the passed array must be an integer multiple of DimSource. The returned 
		/// ordinal values are packed in a similar way. In some DCPs. the ordinals may be 
		/// transformed in-place, and the returned array may be the same as the passed array.
		/// So any client code should not attempt to reuse the passed ordinal values (although
		/// they can certainly reuse the passed array). If there is any problem then the server
		/// implementation will throw an exception. If this happens then the client should not
		/// make any assumptions about the state of the ordinal values.
		/// </remarks>
		/// <param name="points"></param>
		/// <returns></returns>
		public override List<SharpMap.Geometries.Point> TransformList(List<SharpMap.Geometries.Point> points)
		{
			List<SharpMap.Geometries.Point> trans = new List<SharpMap.Geometries.Point>(points.Count);
			foreach (SharpMap.Geometries.Point p in points)
				trans.Add(Transform(p));
			return trans;
		}

		/// <summary>
		/// Reverses the transformation
		/// </summary>
		public override void Invert()
		{
			throw new Exception("The method or operation is not implemented.");
		}
	}
}
