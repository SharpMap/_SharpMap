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

namespace SharpMap.Layers
{
	/// <summary>
	/// Interface for map layers
	/// </summary>
	public interface ILayer
	{
		/// <summary>
		/// Renders the layer
		/// </summary>
		/// <param name="g">Graphics object reference</param>
		/// <param name="map">Map which is rendered</param>
		void Render(System.Drawing.Graphics g, SharpMap.Map map);

		/// <summary>
		/// Minimum visible zoom level
		/// </summary>
		double MinVisible { get; set; }

		/// <summary>
		/// Minimum visible zoom level
		/// </summary>
		double MaxVisible { get; set; }

		/// <summary>
		/// Specifies whether this layer should be rendered or not
		/// </summary>
		bool Enabled { get; set; }

		/// <summary>
		/// Name of layer
		/// </summary>
		string LayerName { get; set; }

		/// <summary>
		/// Gets the boundingbox of the entire layer
		/// </summary>
		SharpMap.Geometries.BoundingBox Envelope { get; }
		//System.Collections.Generic.List<T> Features { get; }

		/// <summary>
		/// The spatial reference ID (CRS)
		/// </summary>
		int SRID { get; set;}

		//SharpMap.CoordinateSystems.CoordinateSystem CoordinateSystem { get; set; }
	}
}
