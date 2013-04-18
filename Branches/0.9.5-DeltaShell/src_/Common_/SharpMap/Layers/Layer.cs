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
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using DelftTools.Utils.Aop.NotifyPropertyChanged;
using GeoAPI.CoordinateSystems.Transformations;
using GeoAPI.Geometries;
using PostSharp;
using SharpMap.Data.Providers;
using SharpMap.Rendering;
using SharpMap.Rendering.Thematics;
using SharpMap.Services;

namespace SharpMap.Layers
{
    /// <summary>
    /// Abstract class for common layer properties
    /// Implement this class instead of the ILayer interface to save a lot of common code.
    /// </summary>
    [NotifyPropertyChanged]
    public abstract class Layer : ILayer
    {
        //private static readonly ILog log = LogManager.GetLogger(typeof(Layer));

        #region Delegates

        /// <summary>
        /// EventHandler for event fired when the layer has been rendered
        /// </summary>
        /// <param name="layer">Layer rendered</param>
        /// <param name="g">Reference to graphics object used for rendering</param>
        public delegate void LayerRenderedEventHandler(Layer layer, Graphics g);

        #endregion

        private bool _Enabled = true;
        private double _MaxVisible = double.MaxValue;

        private int _SRID = -1;
        private IList<IFeatureRenderer> customRenderers;
        private Image image;
        private double lastRenderDuration;
        [NoBubbling] private ILayerGroup layerGroup;
        [NoBubbling] private Map map;
        protected string name;
        private bool renderRequired;
        private bool showInLegend = true;
        private bool showInTreeView = true;

        protected Layer()
        {
            Post.Cast<Layer, INotifyPropertyChanged>(this).PropertyChanged += Layer_PropertyChanged;
            customRenderers = new List<IFeatureRenderer>();
            renderRequired = true;
        }

        /// <summary>
        /// Gets or sets the <see cref="ICoordinateTransformation"/> applied 
        /// to this vectorlayer prior to rendering
        /// </summary>
        public virtual ICoordinateTransformation CoordinateTransformation { get; set; }

        #region ILayer Members

        /// <summary>
        /// Clones the layer.
        /// </summary>
        /// <returns>cloned object</returns>
        public virtual object Clone()
        {
            //make sure you have a parameterless constructor to be cloneable
            var clone = (Layer) Activator.CreateInstance(GetType());
            clone.Name = Name;
            clone.DataSource = DataSource;
            clone.Theme = Theme != null ? (ITheme)Theme.Clone(): null;
            clone.CustomRenderers = CustomRenderers;
            clone.CoordinateTransformation = CoordinateTransformation;
            clone.Enabled = Enabled;
            return clone;
        }


        /// <summary>
        /// Unique id of the layer.
        /// </summary>
        public virtual long Id { get; set; }

        public virtual Image Image
        {
            get { return image; }
        }

        /// <summary>
        /// Gets or sets the name of the layer
        /// </summary>
        public virtual string Name
        {
            get { return name; }
            set { name = value; }
        }

        /// <summary>
        /// The spatial reference ID (CRS)
        /// </summary>
        public virtual int SRID
        {
            get { return _SRID; }
            set { _SRID = value; }
        }

        [NoNotifyPropertyChanged]
        public virtual Map Map
        {
            get { return map; }
            set { map = value; }
        }

        public virtual bool ShowInLegend
        {
            get { return showInLegend; }
            set { showInLegend = value; }
        }

        public virtual bool ShowInTreeView
        {
            get { return showInTreeView; }
            set { showInTreeView = value; }
        }

        public virtual bool ReadOnly
        {
            get { return false; }
        }


        //don't remove the backingfield...it is used by nhibernate
        private IFeatureProvider dataSource;
        public virtual IFeatureProvider DataSource
        {
            get { return dataSource; }
            set { dataSource = value; }
        }

        //[InvokeRequired]
        public virtual ITheme Theme { get; set; }

        //public abstract SharpMap.CoordinateSystems.CoordinateSystem CoordinateSystem { get; set; }

        public virtual void Render()
        {
            DateTime t = DateTime.Now;
            if (image != null)
            {
                image.Dispose();
            }

            image = new Bitmap(Map.Size.Width, Map.Size.Height, PixelFormat.Format32bppPArgb);

            if (!Enabled || MaxVisible < Map.Zoom || MinVisible > Map.Zoom)
            {
                return;
            }

            Graphics graphics = Graphics.FromImage(image);
            graphics.Transform = Map.MapTransform.Clone();
            graphics.Clear(Color.Transparent);
            graphics.PageUnit = GraphicsUnit.Pixel;

            // call virtual implementation which renders layer
            OnRender(graphics, Map);

            // fire event
            if (LayerRendered != null)
            {
                LayerRendered(this, graphics);
            }

            graphics.Dispose();

            lastRenderDuration = (DateTime.Now - t).Milliseconds;

            RenderRequired = false;
        }

        /// <summary>
        /// Custom renderers which can be added to the layer and used to render something in addition to / instead of default rendering.
        /// </summary>
        public virtual IList<IFeatureRenderer> CustomRenderers
        {
            get { return customRenderers; }
            set { customRenderers = value; }
        }

        public virtual bool RenderRequired
        {
            get { return renderRequired; }
            set
            {
                renderRequired = value;
                if (!(this is LabelLayer))
                {
                    ILayer labelLayer = SharpMapService.GetLabelLayer(this);
                    if (null != labelLayer)
                    {
                        labelLayer.RenderRequired = true;
                    }
                }
                foreach (IFeatureRenderer customRenderer in CustomRenderers)
                {
                    customRenderer.ClearCache();
                }
            }
        }

        [NoNotifyPropertyChanged]
        public virtual double LastRenderDuration
        {
            get { return lastRenderDuration; }
        }

        /// <summary>
        /// Returns the extent of the layer
        /// </summary>
        /// <returns>Bounding box corresponding to the extent of the features in the layer</returns>
        public abstract IEnvelope Envelope { get; }

        /// <summary>
        /// Minimum visibility zoom, including this value
        /// </summary>
        public virtual double MinVisible { get; set; }

        /// <summary>
        /// Maximum visibility zoom, excluding this value
        /// </summary>
        public virtual double MaxVisible
        {
            get { return _MaxVisible; }
            set { _MaxVisible = value; }
        }

        /// <summary>
        /// Specified whether the layer is rendered or not
        /// </summary>
        public virtual bool Enabled
        {
            get { return _Enabled; }
            set { _Enabled = value; }
        }


        public virtual bool IsVisible
        {
            get
            {
                // todo add support for nested layergroups
                return null != layerGroup ? layerGroup.Enabled : _Enabled;
            }
        }

        public virtual bool IsSelectable
        {
            get
            {
                var readOnly = ReadOnly;
                //the child is readonly if the parent is
                if (layerGroup != null)
                {
                    readOnly = readOnly || layerGroup.ReadOnly;
                }

                return IsVisible && !readOnly;
            }
        }

        [NoBubbling]
        [NoNotifyPropertyChanged]
        public virtual ILayerGroup LayerGroup
        {
            get { return layerGroup; }
            set { layerGroup = value; }
        }

        #endregion

        /// <summary>
        /// Event fired when the layer has been rendered
        /// </summary>
        public virtual event LayerRenderedEventHandler LayerRendered;

        private void Layer_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            //provide some excludes on which we dont have to render
            if (sender != this || e.PropertyName == "Name" || e.PropertyName == "RenderRequired")
            {
                return;
            }
            if (!RenderRequired)
            {
                RenderRequired = true;
            }
        }

        public virtual void OnRender(Graphics g, Map map)
        {
        }

        /// <summary>
        /// Returns the name of the layer.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Name;
        }
    }
}