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
using System.IO;
using SharpMap.Geometries;
using System.Globalization;
using SharpMap.Rendering;
using SharpMap.Styles;
using SharpMap.Layers;


namespace SharpMap
{
    /// <summary>
    /// Map class
    /// </summary>
    /// <example>
    /// Creating a new map instance, adding layers and rendering the map:
    /// <code lang="C#">
    /// SharpMap.Map myMap = new SharpMap.Map(picMap.Size);
    /// myMap.MinimumZoom = 100;
    /// myMap.BackgroundColor = Color.White;
    /// 
    /// SharpMap.Layers.VectorLayer myLayer = new SharpMap.Layers.VectorLayer("My layer");
    ///    string ConnStr = "Server=127.0.0.1;Port=5432;User Id=postgres;Password=password;Database=myGisDb;";
    /// myLayer.DataSource = new SharpMap.Data.Providers.PostGIS(ConnStr, "myTable", "the_geom", 32632);
    /// myLayer.FillStyle = new SolidBrush(Color.FromArgb(240,240,240)); //Applies to polygon types only
    ///    myLayer.OutlineStyle = new Pen(Color.Blue, 1); //Applies to polygon and linetypes only
    /// //Setup linestyle (applies to line types only)
    ///    myLayer.Style.Line.Width = 2;
    ///    myLayer.Style.Line.Color = Color.Black;
    ///    myLayer.Style.Line.EndCap = System.Drawing.Drawing2D.LineCap.Round; //Round end
    ///    myLayer.Style.Line.StartCap = layRailroad.LineStyle.EndCap; //Round start
    ///    myLayer.Style.Line.DashPattern = new float[] { 4.0f, 2.0f }; //Dashed linestyle
    ///    myLayer.Style.EnableOutline = true;
    ///    myLayer.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias; //Render smooth lines
    ///    myLayer.MaxVisible = 40000;
    /// 
    /// myMap.Layers.Add(myLayer);
    /// // [add more layers...]
    /// 
    /// myMap.Center = new SharpMap.Geometries.Point(725000, 6180000); //Set center of map
    ///    myMap.Zoom = 1200; //Set zoom level
    /// myMap.Size = new System.Drawing.Size(300,200); //Set output size
    /// 
    /// System.Drawing.Image imgMap = myMap.GetMap(); //Renders the map
    /// </code>
    /// </example>
    public class Map : IDisposable
    {
        /// <summary>
        /// Used for converting numbers to/from strings
        /// </summary>
        internal static NumberFormatInfo numberFormat_EnUS = System.Globalization.CultureInfo.InvariantCulture.NumberFormat;
        // <summary>
        /// Initializes a new map
        /// </summary>
        /// <param name="size"></param>
        public Map()
        {
            this.Layers = new List<SharpMap.Layers.ILayer>();
            this.BackColor = Color.White;
            this._MaximumZoom = double.MaxValue;
            this._MinimumZoom = 0;
        }

        /// <summary>
        /// Disposes 
        /// the map object
        /// </summary>
        public void Dispose()
        {
            this.Layers.Clear();
        }

        #region Events
        /// <summary>
        /// EventHandler for event fired when the maps layer list has been changed
        /// </summary>
        public delegate void LayersChangedEventHandler();

        /// <summary>
        /// Event fired when the maps layer list have been changed
        /// </summary>
        public event LayersChangedEventHandler LayersChanged;

        /// <summary>
        /// EventHandler for event fired when a layer has been added to the Layers collections
        /// </summary>
        [Obsolete("Use LayerChangedEventHandler")]
        public delegate void LayerAddedEventHandler();

        /// <summary>
        /// Event fired when the layer has been rendered
        /// </summary>
        [Obsolete("Use LayerChanged")]
        public event LayerAddedEventHandler LayerAdded;

        /// <summary>
        /// EventHandler for event fired when a layer has been removed from the Layers collection
        /// </summary>
        [Obsolete("Use LayerChangedEventHandler")]
        public delegate void LayerRemovedEventHandler();

        /// <summary>
        /// Event fired when the layer has been rendered
        /// </summary>
        [Obsolete("Use LayerChanged")]
        public event LayerRemovedEventHandler LayerRemoved;

        /// <summary>
        /// EventHandler for event fired when the zoomlevel or the center point has been changed
        /// </summary>
        public delegate void MapViewChangedHandler();
        
        /// <summary>
        /// Event fired when the zoomlevel or the center point has been changed
        /// </summary>
        public event MapViewChangedHandler MapViewOnChange;

        #endregion

        #region Methods

        /// <summary>
        /// Renders the map to an image
        /// </summary>
        /// <returns></returns>
        public void Render(IRenderer renderer, IView transform)
        {
            
            int SRID = (Layers.Count > 0 ? Layers[0].SRID : -1); //Get the SRID of the first layer
            for (int i = 0; i < _Layers.Count; i++)
            {
                //if (SRID != _Layers[i].SRID) //Check that all layers have the same SRID
                //    throw (new ArgumentException("An attempt was made to add two layers with different SRIDs"));
                if (_Layers[i].Enabled 
                        && _Layers[i].MaxVisible >= transform.Resolution 
                        && _Layers[i].MinVisible < transform.Resolution)
                    _Layers[i].Render(renderer, transform);
            }
        }

        /// <summary>
        /// Returns an enumerable for all layers containing the search parameter in the LayerName property
        /// </summary>
        /// <param name="layername">Search parameter</param>
        /// <returns>IEnumerable</returns>
        public IEnumerable<SharpMap.Layers.ILayer> FindLayer(string layername)
        {
            foreach (SharpMap.Layers.ILayer l in this.Layers)
                if (l.LayerName.Contains(layername))
                    yield return l;
        }

        #endregion

        #region Properties

        private List<ILayer> _Layers;

        /// <summary>
        /// A collection of layers. The first layer in the list is drawn first, the last one on top.
        /// </summary>
        public System.Collections.Generic.List<SharpMap.Layers.ILayer> Layers
        {
            get { return _Layers; }
            set {
                int iBefore = 0;
                if (_Layers != null)
                    iBefore = _Layers.Count;
                _Layers = value;
                if (value != null)
                {
                    if (iBefore < _Layers.Count) //We have more layers than before. Fire event
                        if (LayerAdded != null)
                            LayerAdded(); //Layer added. Fire event
                        else if (iBefore > _Layers.Count) //We have fewer layers than before. Fire event
                            if (LayerRemoved != null) LayerRemoved(); //Layer removed. Fire event
                    if (LayersChanged != null) //Layers changed. Fire event
                        LayersChanged();
                    if (MapViewOnChange != null)
                        MapViewOnChange();
                }
            }
        }

        private Color _BackgroundColor;

        /// <summary>
        /// Map background color (defaults to transparent)
        /// </summary>
        public Color BackColor
        {
            get { return _BackgroundColor; }
            set
            {
                _BackgroundColor = value;
                if (MapViewOnChange != null)
                    MapViewOnChange();
            }
        }

        /// <summary>
        /// Gets the extents of the map based on the extents of all the layers in the layers collection
        /// </summary>
        /// <returns>Full map extents</returns>
        public SharpMap.Geometries.BoundingBox GetExtents()
        {
            if (this.Layers.Count == 0)
                throw (new System.Exception("No layers to zoom to"));
            SharpMap.Geometries.BoundingBox bbox = null;
            for (int i = 0; i < this.Layers.Count; i++)
            {
                if (bbox == null)
                    bbox = this.Layers[i].Envelope;
                else
                    bbox = bbox.Join(this.Layers[i].Envelope);
            }
            return bbox;
        }    

        private Size _Size;

        private double _MinimumZoom;

        /// <summary>
        /// Minimum zoom amount allowed
        /// </summary>
        public double MinimumZoom
        {
            get { return _MinimumZoom; }
            set {
                if (value < 0)
                    throw (new System.Exception("Minimum zoom must be 0 or more"));
                _MinimumZoom = value; 
            }
        }

        private double _MaximumZoom;

        /// <summary>
        /// Maximum zoom amount allowed
        /// </summary>
        public double MaximumZoom
        {
            get { return _MaximumZoom; }
            set {
                if (value <= 0)
                    throw (new System.Exception("Maximum zoom must larger than 0"));
                _MaximumZoom = value; 
            }
        }

        #endregion

        
  }
}
