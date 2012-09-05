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
using System.Threading;
using DelftTools.Utils.Aop.NotifyPropertyChange;
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
	[NotifyPropertyChange]
    //[NotifyPropertyChanged(AttributeTargetMembers = "SharpMap.Layers.LayerGroup.Map", AttributeExclude = true, AttributePriority = 2)]
    public class GroupLayer : Layer, IGroupLayer//, IDisposable, INotifyCollectionChange
	{
        public GroupLayer(): this("group layer")
        {
        }

		/// <summary>
		/// Initializes a new group layer
		/// </summary>
		/// <param name="layername">Name of layer</param>
		public GroupLayer(string layername)
		{
            Layers = new EventedList<ILayer>();
            Name = layername;
        }

        protected virtual void Layers_CollectionChanged(object sender, NotifyCollectionChangingEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangeAction.Add: //set map property for layers being added
                    ((ILayer) e.Item).Map = Map;
                    ((ILayer) e.Item).RenderRequired = true;
                    break;
                case NotifyCollectionChangeAction.Remove:
                    RenderRequired = true;//render the group if a layer got removed.
                    break;
                case NotifyCollectionChangeAction.Replace:
                    throw new NotImplementedException();
            }
            
            if(CollectionChanged != null)
            {
                CollectionChanged(sender, e);
            }
        }

        void Layers_CollectionChanging(object sender, NotifyCollectionChangingEventArgs e)
        {
            CheckIfLayersIsMutableOrThrow();
            if (CollectionChanging != null)
            {
                CollectionChanging(sender, e);
            }
        }

        private void CheckIfLayersIsMutableOrThrow()
        {
            if (HasReadOnlyLayersCollection)
            {
                throw new InvalidOperationException(
                    "It is not allowed to add or remove layers from a grouplayer that has a read-only layers collection");
            }
        }

        [NoNotifyPropertyChange]
	    public override bool RenderRequired
	    {
	        get
	        {
                /* If subLayer needs redrawing the grouplayer needs redrawing.
                 * test with moving cross section along a branch. */
	            foreach (ILayer layer in Layers)
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
                foreach (ILayer layer in Layers)
                {
                    layer.RenderRequired = value;
                }
                /**/
	            base.RenderRequired = value;
            }
	    }

	    [NoNotifyPropertyChange]
        public override Map Map
        {
            get { return base.Map; }
            set
            {
                base.Map = value;
                foreach (ILayer layer in Layers)
                {
                    layer.Map = value;
                }
            }
        }

		private IEventedList<ILayer> layers;
        

        /// <summary>
		/// Sublayers in the group
		/// </summary>
		//[NoNotifyPropertyChange]
        public virtual IEventedList<ILayer> Layers
		{
			get { return layers; }
			set
			{
                if(layers != null)
                {
                    layers.CollectionChanged -= Layers_CollectionChanged;
                    layers.CollectionChanging -= Layers_CollectionChanging;
                }
			    layers = value;
                layers.CollectionChanged += Layers_CollectionChanged;
                layers.CollectionChanging += Layers_CollectionChanging;
            }
		}

        public virtual bool HasReadOnlyLayersCollection { get; set; }

        bool ILayer.ReadOnly
	    {
            get
	        {
	            return ReadOnly;
	        }
	    }

        public virtual new bool ReadOnly { get; set; }

        /// <summary>
		/// Returns a layer by its name
		/// </summary>
		/// <param name="name">Name of layer</param>
		/// <returns>Layer</returns>
		public virtual ILayer GetLayerByName(string name)
		{
            //return layers.Find( delegate(SharpMap.Layers.Layer layer) { return layer.LayerName.Equals(name); });

            for (int i = 0; i < Layers.Count; i++)
                if (String.Equals(Layers[i].Name, name, StringComparison.InvariantCultureIgnoreCase))
                    return Layers[i];
            
            return null;
		}

		/// <summary>
		/// Renders the layer
		/// </summary>
		/// <param name="g">Graphics object reference</param>
		/// <param name="map">Map which is rendered</param>
		public override void OnRender(System.Drawing.Graphics g, Map map)
		{
            for (int i = Layers.Count - 1; i >= 0; i--)
            {
                var layer = Layers[i];

                if (layer.Visible && layer.MaxVisible >= map.Zoom && layer.MinVisible < map.Zoom)
                {
                    if (layer.RenderRequired)
                    {
                        layer.Render();
                    }

                    // copy image from child layer into current group layer image
                    g.DrawImage(layer.Image, 0, 0);
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
                    if ((!layer.Visible))
                    {
                        continue;
                    }

		            var layerEnvelope = layer.Envelope;

		            if (layerEnvelope != null && !layerEnvelope.IsNull)
		            {
		                envelope.ExpandToInclude(layerEnvelope);
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
		    var clonedLayerGroup = new GroupLayer(name.Clone() as string);
		    
		    foreach (var layer in Layers)
		    {
		        var layerToAdd = layer.Clone() as Layer;
		        clonedLayerGroup.Layers.Add(layerToAdd);
		    }
		    return clonedLayerGroup;
		}

        protected override void OnLayerPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            //skip the render required logic for group layers...layers handle this individually
            //group layer should only react to local changes and renderrequired of child layers
            //if not it will result in a lot of loading exceptions during save/load
            if (sender == this)
            {
                base.OnLayerPropertyChanged(sender,e);//handle like a 'normal' layer
            }
            else
            {
                ILayer source = sender as ILayer;
                if (source == null)
                {
                    return;
                }
                if ((e.PropertyName == "RenderRequired") && !RenderRequired && source.RenderRequired)
                {
                    base.RenderRequired = true;
                }
            }
        }

		/// <summary>
		/// Disposes the object
		/// </summary>
		public void Dispose()
		{
			foreach (SharpMap.Layers.Layer layer in this.Layers)
				if (layer is IDisposable)
					((IDisposable)layer).Dispose();
		}

	    public virtual event NotifyCollectionChangedEventHandler CollectionChanged;
        public virtual event NotifyCollectionChangingEventHandler CollectionChanging;
	}
}
