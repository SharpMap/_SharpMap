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

namespace SharpMap.Utilities
{
	/// <summary>
	/// Class for transforming between world and image coordinate
	/// </summary>
	public class Transform
	{
		/// <summary>
		/// Transforms from world coordinate system (WCS) to image coordinates
		/// NOTE: This method DOES NOT take the MapTransform property into account (use SharpMap.Map.MapToWorld instead)
		/// </summary>
		/// <param name="p">Point in WCS</param>
		/// <param name="map">Map reference</param>
		/// <returns>Point in image coordinates</returns>
		public static System.Drawing.PointF WorldtoMap(SharpMap.Geometries.Point p, SharpMap.Map map)
		{
			System.Drawing.PointF result = new System.Drawing.Point();
			double Height = (map.Zoom * map.Size.Height) / map.Size.Width;
			double left = map.Center.X - map.Zoom/2;
			double top = map.Center.Y + Height/2;
			double pxSize = map.Zoom / map.Size.Width;
			result.X = (float)(((p.X - left) / pxSize));
			result.Y = (float)(((top - p.Y) / pxSize));
			return result;
		}

		/// <summary>
		/// Transforms from image coordinates to world coordinate system (WCS).
		/// NOTE: This method DOES NOT take the MapTransform property into account (use SharpMap.Map.MapToWorld instead)
		/// </summary>
		/// <param name="p">Point in image coordinate system</param>
		/// <param name="map">Map reference</param>
		/// <returns>Point in WCS</returns>
		public static SharpMap.Geometries.Point MapToWorld(System.Drawing.PointF p, SharpMap.Map map)
		{
			SharpMap.Geometries.BoundingBox env = map.Envelope;
			return new SharpMap.Geometries.Point(env.Min.X + p.X * map.PixelSize,
					env.Max.Y - p.Y * map.PixelSize);
		}
	}
}
