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
using System.Collections.ObjectModel;
using SharpMap.Data;
using SharpMap.Geometries;
using SharpMap.Rendering;

namespace SharpMap.Layers
{
    /// <summary>
    /// Class for holding a group of layers.
    /// </summary>
    /// <remarks>
    /// The Group layer is useful for grouping a set of layers,
    /// for instance a set of image tiles, and expose them as a single layer
    /// </remarks>
    public class LayerGroup : Layer, IQueryLayer, IDisposable
    {
        private Collection<ILayer> _Layers;

        /// <summary>
        /// Initializes a new group layer
        /// </summary>
        /// <param name="layername">Name of layer</param>
        public LayerGroup(string layername) : base(layername)
        {
            LayerName = layername;
            _Layers = new Collection<ILayer>();
        }

        /// <summary>
        /// Sublayers in the group
        /// </summary>
        public Collection<ILayer> Layers
        {
            get { return _Layers; }
            set { _Layers = value; }
        }

        /// <summary>
        /// Returns the extent of the layer
        /// </summary>
        /// <returns>Bounding box corresponding to the extent of the features in the layer</returns>
        public override BoundingBox Envelope
        {
            get
            {
                if (Layers.Count == 0)
                    return null;
                BoundingBox bbox = Layers[0].Envelope;
                for (int i = 1; i < Layers.Count; i++)
                    bbox = bbox.Join(Layers[i].Envelope);
                return bbox;
            }
        }

        #region IDisposable Members

        /// <summary>
        /// Disposes the object
        /// </summary>
        public void Dispose()
        {
            foreach (Layer layer in Layers)
                if (layer is IDisposable)
                    ((IDisposable) layer).Dispose();
            Layers.Clear();
        }

        #endregion

        /// <summary>
        /// Returns a layer by its name
        /// </summary>
        /// <param name="name">Name of layer</param>
        /// <returns>Layer</returns>
        public ILayer GetLayerByName(string name)
        {
            //return _Layers.Find( delegate(SharpMap.Layers.Layer layer) { return layer.LayerName.Equals(name); });

            for (int i = 0; i < _Layers.Count; i++)
                if (String.Equals(_Layers[i].LayerName, name, StringComparison.InvariantCultureIgnoreCase))
                    return _Layers[i];

            return null;
        }

        /// <summary>
        /// Render the layer
        /// </summary>
        /// <param name="renderer"></param>
        /// <param name="view"></param>
         public void Render(IRenderer renderer, IView view)
        {
            foreach (ILayer layer in _Layers)
                if (layer.Enabled && layer.MaxVisible >= view.Resolution && layer.MinVisible < view.Resolution)
                    layer.Render(renderer, view);
        }
        
        public IFeatures GetFeatures(BoundingBox box)
        {
            foreach (Layer layer in Layers)
            {
                if (layer is IQueryLayer)
                {
                    //Not implemented because not sure how to deal with multiple result sets //!!!
                    //Update: I now think the grouplayer shoudl not implement IQueryLayer. The
                    //calling code should iterate over its children to call the individual layers.
                    throw new NotImplementedException("Not implemented");
                    var queryLayer = layer as IQueryLayer;
                    var features = queryLayer.GetFeatures(box);
                    return features;
                }
            }
            return null;
        }

    }
}