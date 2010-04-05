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

using SharpMap.CoordinateSystems.Projections;

namespace SharpMap.CoordinateSystems.Transformations
{
	/// <summary>
	/// Creates coordinate transformations.
	/// </summary>
	public class CoordinateTransformationFactory : ICoordinateTransformationFactory
	{
		#region ICoordinateTransformationFactory Members

		/// <summary>
		/// Creates a transformation between two coordinate systems.
		/// </summary>
		/// <remarks>
		/// This method will examine the coordinate systems in order to construct
		/// a transformation between them. This method may fail if no path between 
		/// the coordinate systems is found, using the normal failing behavior of 
		/// the DCP (e.g. throwing an exception).</remarks>
		/// <param name="sourceCS">Source coordinate system</param>
		/// <param name="targetCS">Target coordinate system</param>
		/// <returns></returns>		
		public ICoordinateTransformation CreateFromCoordinateSystems(ICoordinateSystem sourceCS, ICoordinateSystem targetCS)
		{
			IProjectedCoordinateSystem projectedCS = null;
			IGeographicCoordinateSystem geographicCS = null;
			bool isInverse = false;
			if (sourceCS is IProjectedCoordinateSystem && targetCS is IGeographicCoordinateSystem)
			{
				projectedCS = (IProjectedCoordinateSystem)sourceCS;
				geographicCS = (IGeographicCoordinateSystem)targetCS;
				isInverse = true;
			}
			else if (targetCS is IProjectedCoordinateSystem && sourceCS is IGeographicCoordinateSystem)
			{
				projectedCS = (IProjectedCoordinateSystem)targetCS;
				geographicCS = (IGeographicCoordinateSystem)sourceCS;
			}
			else if (targetCS is IGeocentricCoordinateSystem && sourceCS is IGeographicCoordinateSystem)
			{
				IMathTransform geocMathTransform = CreateCoordinateOperation(targetCS as IGeocentricCoordinateSystem);
				geographicCS = (IGeographicCoordinateSystem)sourceCS;
				return new CoordinateTransformation(targetCS, sourceCS, TransformType.Conversion, geocMathTransform, String.Empty, String.Empty, -1, String.Empty, String.Empty);
			}
			else if (sourceCS is IGeocentricCoordinateSystem && targetCS is IGeographicCoordinateSystem)
			{
				IMathTransform geocMathTransform = CreateCoordinateOperation(sourceCS as IGeocentricCoordinateSystem).Inverse();
				geographicCS = (IGeographicCoordinateSystem)sourceCS;
				return new CoordinateTransformation(sourceCS, targetCS, TransformType.Conversion, geocMathTransform, String.Empty, String.Empty, -1, String.Empty, String.Empty);
			}
			if (projectedCS == null || geographicCS == null)
			{
				throw new InvalidOperationException("Need a geographic and a projected coordinate reference system to make a transform.");
			}
			IMathTransform mathTransform = CreateCoordinateOperation(projectedCS.Projection, projectedCS.GeographicCoordinateSystem.HorizontalDatum.Ellipsoid);
			if (isInverse)
				mathTransform = mathTransform.Inverse();

			return new CoordinateTransformation(
				geographicCS,projectedCS,TransformType.Transformation,mathTransform,
				String.Empty, String.Empty, -1, String.Empty, String.Empty);
		}

		#endregion
		private static IMathTransform CreateCoordinateOperation(IGeocentricCoordinateSystem geo)
		{
			List<ProjectionParameter> parameterList = new List<ProjectionParameter>(2);
			parameterList.Add(new ProjectionParameter("semi_major", geo.HorizontalDatum.Ellipsoid.SemiMajorAxis));
			parameterList.Add(new ProjectionParameter("semi_minor", geo.HorizontalDatum.Ellipsoid.SemiMinorAxis));
			return new Geocentric(parameterList);
		}
		private static IMathTransform CreateCoordinateOperation(IProjection projection, IEllipsoid ellipsoid)
		{
			List<ProjectionParameter> parameterList = new List<ProjectionParameter>(projection.NumParameters);
			for (int i = 0; i < projection.NumParameters; i++)
			{
				parameterList.Add(projection.GetParameter(i));
			}
			parameterList.Add(new ProjectionParameter("semi_major", ellipsoid.SemiMajorAxis));
			parameterList.Add(new ProjectionParameter("semi_minor", ellipsoid.SemiMinorAxis));

			IMathTransform transform = null;
			switch (projection.ClassName.ToLower())
			{
				case "mercator_1sp":
				case "mercator_2sp":
					//1SP
					transform = new Mercator(parameterList);
					break;
				case "transverse_mercator":
					transform = new TransverseMercator(parameterList);
					break;
				case "albers":
					transform = new AlbersProjection(parameterList);
					break;
				case "lambert_conformal_conic_2sp":
					transform = new LambertConformalConic2SP(parameterList);
					break;
				default:
					throw new NotSupportedException(String.Format("Projection {0} is not supported.", projection.ClassName));
			}
			return transform;
		}

	}
}
