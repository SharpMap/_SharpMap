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
using DelftTools.Utils.Aop.NotifyPropertyChanged;
using DelftTools.Utils.Collections;
using DelftTools.Utils.Collections.Generic;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;

namespace SharpMap.Layers
{
    /// <summary>
	/// Class for holding a group of layers.
	/// </summary>
	/// <remarks>
	/// The Group layer is useful for grouping a set of layers,
	/// for instance a set of image tiles, and expose them as a single layer
	/// </remarks>
	[NotifyPropertyChanged]
    //[NotifyPropertyChanged(AttributeTargetMembers = "SharpMap.Layers.LayerGroup.Map", AttributeExclude = true, AttributePriority = 2)]
    public class LayerGroup : Layer, ILayerGroup//, IDisposable, INotifyCollectionChanged
	{
        public LayerGroup() : this("layer group")
        {
        }

		/// <summary>
		/// Initializes a new group layer
		/// </summary>
		/// <param name="layername">Name of layer</param>
		public LayerGroup(string layername)
		{
            Layers = new EventedList<ILayer>();
            Name = layername;
        }

        protected virtual void Layers_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add: //set map property for layers being added
                    ((ILayer) e.Item).Map = Map;
                    ((ILayer) e.Item).RenderRequired = true;
                    ((ILayer) e.Item).LayerGroup = this;
                    break;
                case NotifyCollectionChangedAction.Remove:
                    ((ILayer) e.Item).LayerGroup = null;
                    RenderRequired = true;//render the group if a layer got removed.
                    break;
                case NotifyCollectionChangedAction.Replace:
                    throw new NotImplementedException();
            }
            
            if(CollectionChanged != null)
            {
                CollectionChanged(sender, e);
            }
        }

        void Layers_CollectionChanging(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (CollectionChanging != null)
            {
                CollectionChanging(sender, e);
            }
        }

        [NoNotifyPropertyChanged]
	    public override bool RenderRequired
	    {
	        get
	        {
                /* If subLayer needs redrawing the grouplayer needs redrawing.
                 * test with moving cross section along a branch. */
	            foreach (ILayer layer in _Layers)
	            {
	                if(layer.RenderRequired)
	                {
	                    return true;
	                }
	            }
                /**/
	            return base.RenderRequired;
	        }
	        set
	        {
                /**/
                foreach (ILayer layer in _Layers)
                {
                    layer.RenderRequired = value;
                }
                /**/
	            base.RenderRequired = value;
            }
	    }

	    [NoNotifyPropertyChanged]
        public override Map Map
        {
            get { return base.Map; }
            set
            {
                base.Map = value;
                foreach (ILayer layer in _Layers)
                {
                    layer.Map = value;
                }
            }
        }

		private IEventedList<ILayer> _Layers;
        

        /// <summary>
		/// Sublayers in the group
		/// </summary>
        public virtual IEventedList<ILayer> Layers
		{
			get { return _Layers; }
			set
			{
                if(_Layers != null)
                {
                    _Layers.CollectionChanged -= Layers_CollectionChanged;
                    _Layers.CollectionChanging -= Layers_CollectionChanging;
                }
			    _Layers = value;
                _Layers.CollectionChanged += Layers_CollectionChanged;
                _Layers.CollectionChanging += Layers_CollectionChanging;
            }
		}

		/// <summary>
		/// Returns a layer by its name
		/// </summary>
		/// <param name="name">Name of layer</param>
		/// <returns>Layer</returns>
		public virtual ILayer GetLayerByName(string name)
		{
            //return _Layers.Find( delegate(SharpMap.Layers.Layer layer) { return layer.LayerName.Equals(name); });

            for (int i = 0; i < _Layers.Count; i++)
                if (String.Equals(_Layers[i].Name, name, StringComparison.InvariantCultureIgnoreCase))
                    return _Layers[i];
            
            return null;
		}

		/// <summary>
		/// Renders the layer
		/// </summary>
		/// <param name="g">Graphics object reference</param>
		/// <param name="map">Map which is rendered</param>
		public override void OnRender(System.Drawing.Graphics g, Map map)
		{
            for (int i = _Layers.Count - 1; i >= 0; i--)
            {
                if (_Layers[i].Enabled && _Layers[i].MaxVisible >= map.Zoom && _Layers[i].MinVisible < map.Zoom)
                {
                    _Layers[i].Render();

                    // copy image from child layer into current group layer image
                    g.DrawImage(_Layers[i].Image, 0, 0);
                }
            }
		}

		/// <summary>
		/// Returns the extent of the layer
		/// </summary>
		/// <returns>Bounding box corresponding to the extent of the features in the layer</returns>
		public override IEnvelope Envelope
		{
		    get
		    {
                IEnvelope envelope = new Envelope();
                
                if (Layers.Count == 0)
		        {
		            return envelope;
		        }


		        foreach (ILayer layer in Layers)
		        {
		            if ((layer.IsVisible) && (layer.Envelope != null && !layer.Envelope.IsNull))
		            {
		                envelope.ExpandToInclude(layer.Envelope);    
		            }
		        }

		        return envelope;
		    }
		}

		/// <summary>
		/// Clones the layer
		/// </summary>
		/// <returns>cloned object</returns>
		public override object Clone()
		{
		    var clonedLayerGroup = new LayerGroup(name.Clone() as string);
		    
		    foreach (var layer in Layers)
		    {
		        var layerToAdd = layer.Clone() as Layer;
		        clonedLayerGroup.Layers.Add(layerToAdd);
		    }
		    return clonedLayerGroup;
		}

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

	    public virtual event NotifyCollectionChangedEventHandler CollectionChanged;
        public virtual event NotifyCollectionChangedEventHandler CollectionChanging;
	}
}
