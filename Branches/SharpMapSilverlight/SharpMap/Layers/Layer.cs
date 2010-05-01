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
using SharpMap.Data.Providers;
using SharpMap.Styles;
using ProjNet.CoordinateSystems.Transformations;
using SharpMap.Rendering.Thematics;
using SharpMap.Data;
using SharpMap.Rendering;
using SharpMap.Projection;

namespace SharpMap.Layers
{
    /// <summary>
    /// Class for vector layer properties
    /// </summary>
    /// <example>
    /// Adding a VectorLayer to a map:
    /// <code lang="C#">
    /// //Initialize a new map
    /// SharpMap.Map myMap = new SharpMap.Map(new System.Drawing.Size(300,600));
    /// //Create a layer
    /// SharpMap.Layers.VectorLayer myLayer = new SharpMap.Layers.VectorLayer("My layer");
    /// //Add datasource
    /// myLayer.DataSource = new SharpMap.Data.Providers.ShapeFile(@"C:\data\MyShapeData.shp");
    /// //Set up styles
    /// myLayer.Style.Outline = new Pen(Color.Magenta, 3f);
    /// myLayer.Style.EnableOutline = true;
    /// myMap.Layers.Add(myLayer);
    /// //Zoom to fit the data in the view
    /// myMap.ZoomToExtents();
    /// //Render the map:
    /// System.Drawing.Image mapImage = myMap.GetMap();
    /// </code>
    /// </example>
    public class Layer : ILayer, IQueryLayer
    {
        #region Fields

        private SharpMap.Rendering.Thematics.ITheme _theme;
        private SharpMap.Data.Providers.IProvider _DataSource;
        private IStyle _Style;
        private double _MaxVisible = double.MaxValue;
        private double _MinVisible = 0;
        private bool _Enabled = true;
        private string _LayerName;
        private ICoordinateTransformation _CoordinateTransform;

        #endregion

        #region Properties

        /// <summary>
        /// Minimum visibility zoom, including this value
        /// </summary>
        public double MinVisible
        {
            get { return _MinVisible; }
            set { _MinVisible = value; }
        }

        /// <summary>
        /// Maximum visibility zoom, excluding this value
        /// </summary>
        public double MaxVisible
        {
            get { return _MaxVisible; }
            set { _MaxVisible = value; }
        }

        /// <summary>
        /// Specified whether the layer is rendered or not
        /// </summary>
        public bool Enabled
        {
            get { return _Enabled; }
            set { _Enabled = value; }
        }

        /// <summary>
        /// Gets or sets the name of the layer
        /// </summary>
        public string LayerName
        {
            get { return _LayerName; }
            set { _LayerName = value; }
        }

        /// <summary>
        /// Gets or sets thematic settings for the layer. Set to null to ignore thematics
        /// </summary>
        public ITheme Theme
        {
            get { return _theme; }
            set { _theme = value; }
        }

        /// <summary>
        /// Gets or sets the datasource
        /// </summary>
        public IProvider DataSource
        {
            get { return _DataSource; }
            set { _DataSource = value; }
        }

        /// <summary>
        /// Gets or sets the rendering style of the vector layer.
        /// </summary>
        public IStyle Style
        {
            get { return _Style; }
            set { _Style = value; }
        }

        /// <summary>
        /// Gets or sets the SRID of this VectorLayer's data source
        /// </summary>
        /// 
        public int SRID
        {
            get
            {
                if (this.DataSource == null)
                    throw (new Exception("DataSource property not set on layer '" + this.LayerName + "'"));

                return this.DataSource.SRID;
            }
            set { this.DataSource.SRID = value; }
        }

        /// <summary>
        /// Gets or sets the <see cref="SharpMap.CoordinateSystems.Transformations.ICoordinateTransformation"/> applied 
        /// to this layer prior to rendering
        /// </summary>
        public ProjNet.CoordinateSystems.Transformations.ICoordinateTransformation CoordinateTransformation
        {
            get { return _CoordinateTransform; }
            set { _CoordinateTransform = value; }
        }

        /// <summary>
        /// Returns the extent of the layer
        /// </summary>
        /// <returns>Bounding box corresponding to the extent of the features in the layer</returns>
        public virtual BoundingBox Envelope
        {
            get
            {
                if (this.DataSource == null)
                    throw (new Exception("DataSource property not set on layer '" + this.LayerName + "'"));

                bool wasOpen = this.DataSource.IsOpen;
                if (!wasOpen)
                    this.DataSource.Open();
                SharpMap.Geometries.BoundingBox box = this.DataSource.GetExtents();
                if (!wasOpen) //Restore state
                    this.DataSource.Close();
                if (this.CoordinateTransformation != null)
                    return ProjectionHelper.Transform(box, this.CoordinateTransformation);
                return box;
            }
        }

        #endregion

        #region Public methods

        // <summary>
        /// Initializes a new layer
        /// </summary>
        /// <param name="layername">Name of layer</param>
        public Layer(string layername)
        {
            this.Style = new VectorStyle();
            this.LayerName = layername;
        }

        /// <summary>
        /// Renders the layer to a graphics object
        /// </summary>
        /// <param name="g">Graphics object reference</param>
        /// <param name="map">Map which is rendered</param>
        public virtual void Render(IRenderer renderer, IView view)
        {
            renderer.RenderLayer(view, DataSource, CreateStyleMethod(Style, Theme), CoordinateTransformation);
        }

        #endregion

        #region Private methods

        private static Func<IFeature, IStyle> CreateStyleMethod(IStyle style, ITheme theme)
        {
            if (theme == null)
                return (row) => style;
            else
                return (row) => theme.GetStyle(row);
        }

        #endregion


        #region IQueryLayer Members

        public virtual IFeatures GetFeaturesInView(IView view)
        {
            DataSource.Open();
            var features = DataSource.GetFeaturesInView(view);
            DataSource.Close();
            return features;
        }

        #endregion
    }
}
