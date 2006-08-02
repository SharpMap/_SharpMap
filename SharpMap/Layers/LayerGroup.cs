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
	/// Class for holding a group of layers.
	/// </summary>
	/// <remarks>
	/// The Group layer is useful for grouping a set of layers,
	/// for instance a set of image tiles, and expose them as a single layer
	/// </remarks>
	public class LayerGroup : Layer, IDisposable
	{

		/// <summary>
		/// Initializes a new group layer
		/// </summary>
		/// <param name="layername">Name of layer</param>
		public LayerGroup(string layername)
		{
			this.LayerName = layername;
			_Layers = new List<Layer>();
		}

		private List<Layer> _Layers;

		/// <summary>
		/// Sublayers in the group
		/// </summary>
		public List<Layer> Layers
		{
			get { return _Layers; }
			set { _Layers = value; }
		}

		/// <summary>
		/// Returns a layer by its name
		/// </summary>
		/// <param name="name">Name of layer</param>
		/// <returns>Layer</returns>
		public SharpMap.Layers.Layer GetLayerByName(string name)
		{
			return _Layers.Find( delegate(SharpMap.Layers.Layer layer) { return layer.LayerName.Equals(name); });
		}

		/// <summary>
		/// Renders the layer
		/// </summary>
		/// <param name="g">Graphics object reference</param>
		/// <param name="map">Map which is rendered</param>
		public override void Render(System.Drawing.Graphics g, Map map)
		{
			for (int i = 0; i < _Layers.Count;i++ )
				if (_Layers[i].Enabled && _Layers[i].MaxVisible >= map.Zoom && _Layers[i].MinVisible < map.Zoom)
						_Layers[i].Render(g, map);
		}

		/// <summary>
		/// Returns the extent of the layer
		/// </summary>
		/// <returns>Bounding box corresponding to the extent of the features in the layer</returns>
		public override SharpMap.Geometries.BoundingBox Envelope
		{
			get
			{
				if (this.Layers.Count == 0)
					return null;
				SharpMap.Geometries.BoundingBox bbox = this.Layers[0].Envelope;
				for (int i = 1; i < this.Layers.Count; i++)
					bbox = bbox.Join(this.Layers[i].Envelope);
				return bbox;
			}
		}


		#region ICloneable Members

		/// <summary>
		/// Clones the layer
		/// </summary>
		/// <returns>cloned object</returns>
		public override object Clone()
		{
			throw new NotImplementedException();
		}

		#endregion

		#region IDisposable Members

		/// <summary>
		/// Disposes the object
		/// </summary>
		public void Dispose()
		{
			foreach (SharpMap.Layers.Layer layer in this.Layers)
				if (layer is IDisposable)
					((IDisposable)layer).Dispose();
			this.Layers.Clear();
		}

		#endregion
	}
}
