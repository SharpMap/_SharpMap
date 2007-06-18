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
using SharpMap.Geometries;

namespace SharpMap.Layers
{
	/// <summary>
	/// Abstract class for common layer properties
	/// Implement this class instead of the ILayer interface to save a lot of common code.
	/// </summary>
	public abstract class Layer : ILayer, ICloneable
	{
		#region Events
		/// <summary>
		/// EventHandler for event fired when the layer has been rendered
		/// </summary>
		/// <param name="layer">Layer rendered</param>
		/// <param name="g">Reference to graphics object used for rendering</param>
		public delegate void LayerRenderedEventHandler(SharpMap.Layers.Layer layer, System.Drawing.Graphics g);

		/// <summary>
		/// Event fired when the layer has been rendered
		/// </summary>
		public event LayerRenderedEventHandler LayerRendered;
		#endregion

		/// <summary>
		/// Returns the name of the layer.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return this.LayerName;
		}


		private SharpMap.CoordinateSystems.Transformations.ICoordinateTransformation _CoordinateTransform;

		/// <summary>
		/// Gets or sets the <see cref="SharpMap.CoordinateSystems.Transformations.ICoordinateTransformation"/> applied 
		/// to this vectorlayer prior to rendering
		/// </summary>
		public virtual SharpMap.CoordinateSystems.Transformations.ICoordinateTransformation CoordinateTransformation
		{
			get { return _CoordinateTransform; }
			set { _CoordinateTransform = value; }
		}

		#region ILayer Members

		private string _LayerName;
		/// <summary>
		/// Gets or sets the name of the layer
		/// </summary>
		public string LayerName
		{
			get { return _LayerName; }
			set { _LayerName = value; }
		}
		
		private int _SRID = -1;
		/// <summary>
		/// The spatial reference ID (CRS)
		/// </summary>
		public virtual int SRID
		{
			get { return _SRID; }
			set { _SRID = value; }
		}

		//public abstract SharpMap.CoordinateSystems.CoordinateSystem CoordinateSystem { get; set; }
	

		/// <summary>
		/// Renders the layer
		/// </summary>
		/// <param name="g">Graphics object reference</param>
		/// <param name="map">Map which is rendered</param>
		public virtual void Render(System.Drawing.Graphics g, Map map)
		{
			if(LayerRendered!=null) LayerRendered(this, g); //Fire event
		}

		/// <summary>
		/// Returns the extent of the layer
		/// </summary>
		/// <returns>Bounding box corresponding to the extent of the features in the layer</returns>
		public abstract SharpMap.Geometries.BoundingBox Envelope { get; }

		#region Properties

		private double _MinVisible = 0;
		/// <summary>
		/// Minimum visibility zoom, including this value
		/// </summary>
		public double MinVisible
		{
			get { return _MinVisible; }
			set { _MinVisible = value; }
		}

		private double _MaxVisible = double.MaxValue;

		/// <summary>
		/// Maximum visibility zoom, excluding this value
		/// </summary>
		public double MaxVisible
		{
			get { return _MaxVisible; }
			set { _MaxVisible = value; }
		}

		private bool _Enabled = true;

		/// <summary>
		/// Specified whether the layer is rendered or not
		/// </summary>
		public bool Enabled
		{
			get { return _Enabled; }
			set { _Enabled = value; }
		}

		#endregion

		#endregion

		#region ICloneable Members

		/// <summary>
		/// Clones the layer
		/// </summary>
		/// <returns>cloned object</returns>
		public abstract object Clone();

		#endregion

	}
}
