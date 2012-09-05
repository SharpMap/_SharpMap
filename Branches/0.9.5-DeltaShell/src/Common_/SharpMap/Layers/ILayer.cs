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
using System.Drawing;
using SharpMap.Data.Providers;
using SharpMap.Rendering;
using SharpMap.Rendering.Thematics;

namespace SharpMap.Layers
{
	/// <summary>
	/// Interface for map layers
	/// </summary>
	public interface ILayer : ICloneable
	{
		long Id { get; set; }

        /// <summary>
        /// Image of the layer for current map, layer uses it to render it's content to.
        /// Layer image contains only graphics rendered by one layer.
        /// </summary>
        Image Image { get; }

        /// <summary>
        /// Use this method to render layer manually. Results will be rendered into Image property.
        /// 
        /// This method should call OnRender which can be overriden in the implementations.
        /// </summary>
        void Render();

        /// <summary>
        /// Custom renderers which can be added to the layer and used to render something in addition to / instead of default rendering.
        /// </summary>
        IList<IFeatureRenderer> CustomRenderers { get; set; }

        /// <summary>
        /// True if layers needs to be rendered. Map will check this flag while it will render itself.
        /// If flag is set to true - Render() will be called before Image is drawn on Map.
        /// 
        /// Setting this flag to true in some layers and calling Map.Refresh() will make sure that only required layers will be rendered.
        /// 
        /// Calling Render() resets this flag automatically.
        /// </summary>
        bool RenderRequired { get; set; }

        /// <summary>
        /// Duration of last rendering in ms.
        /// </summary>
        double LastRenderDuration { get; } // TODO: move it somewhere else, it should be an aspect

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
		string Name { get; set; }

		/// <summary>
		/// Gets the boundingbox of the entire layer
		/// </summary>
		GeoAPI.Geometries.IEnvelope Envelope { get; }
		//System.Collections.Generic.List<T> Features { get; }

		/// <summary>
		/// The spatial reference ID (CRS)
		/// </summary>
		int SRID { get; set;}

		/// <summary>
		/// Gets or sets map where this layer belongs to, or null.
		/// </summary>
        Map Map { get; set; }

        /// <summary>
        /// Defines if layer should be shown in a legend of map layers. Useful to hide supplementary layers such as drawing trackers or geometries.
        /// </summary>
        bool ShowInLegend { get; set; }
        
        /// <summary>
        /// Defines if layer should be shown in a treeview of map layers. Useful to hide supplementary layers such as drawing trackers or geometries.
        /// </summary>
        bool ShowInTreeView { get; set; }

        bool ReadOnly { get; }

	    //SharpMap.CoordinateSystems.CoordinateSystem CoordinateSystem { get; set; }

        IFeatureProvider DataSource { get; set; }
        
        ITheme Theme { get; set; }

        /// <summary>
        /// If the layer is part of a layerGroup the layerGroup
        /// </summary>
        ILayerGroup LayerGroup { get; set; }

        /// <summary>
        /// Is the layer visible. By default this mathes the Enabled property except if the layer is part of a layergroup
        /// </summary>
        bool IsVisible { get; }

        /// <summary>
        /// Can features of the layer be selected. 
        /// This defaults to Readonly and Enabled but also takes parent layergroup into account. 
        /// </summary>
        bool IsSelectable { get; }
	}
}
