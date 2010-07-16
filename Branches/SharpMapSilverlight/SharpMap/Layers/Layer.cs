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
using ProjNet.CoordinateSystems.Transformations;
using SharpMap.Data;
using SharpMap.Data.Providers;
using SharpMap.Geometries;
using SharpMap.Projection;
using SharpMap.Rendering;
using SharpMap.Rendering.Thematics;
using SharpMap.Styles;

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
        #region Properties

        /// <summary>
        /// Minimum visibility zoom, including this value
        /// </summary>
        public double MinVisible { get; set; }

        /// <summary>
        /// Maximum visibility zoom, excluding this value
        /// </summary>
        public double MaxVisible { get; set; }

        /// <summary>
        /// Specified whether the layer is rendered or not
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets the name of the layer
        /// </summary>
        public string LayerName { get; set; }

        /// <summary>
        /// Gets or sets thematic settings for the layer. Set to null to ignore thematics
        /// </summary>
        public ITheme Theme { get; set; }

        /// <summary>
        /// Gets or sets the datasource
        /// </summary>
        public IProvider DataSource { get; set; }

        /// <summary>
        /// Gets or sets the rendering style of the vector layer.
        /// </summary>
        public IStyle Style { get; set; }

        /// <summary>
        /// Gets or sets the SRID of this VectorLayer's data source
        /// </summary>
        /// 
        public int SRID
        {
            get
            {
                if (DataSource == null)
                    throw (new Exception("DataSource property not set on layer '" + LayerName + "'"));

                return DataSource.SRID;
            }
            set { DataSource.SRID = value; }
        }

        public ICoordinateTransformation CoordinateTransformation { get; set; }

        /// <summary>
        /// Returns the extent of the layer
        /// </summary>
        /// <returns>Bounding box corresponding to the extent of the features in the layer</returns>
        public virtual BoundingBox Envelope
        {
            get
            {
                if (DataSource == null)
                    throw (new Exception("DataSource property not set on layer '" + LayerName + "'"));

                bool wasOpen = DataSource.IsOpen;
                if (!wasOpen)
                    DataSource.Open();
                BoundingBox box = DataSource.GetExtents();
                if (!wasOpen) //Restore state
                    DataSource.Close();
                if (CoordinateTransformation != null)
                    return ProjectionHelper.Transform(box, CoordinateTransformation);
                return box;
            }
        }

        #endregion

        #region Public methods

        public Layer(string layername)
        {
            MinVisible = 0;
            MaxVisible = double.MaxValue;
            Enabled = true;
            Style = new VectorStyle();
            LayerName = layername;
        }

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

        public virtual IFeatures GetFeaturesInView(BoundingBox box, double resolution)
        {
            DataSource.Open();
            var features = DataSource.GetFeaturesInView(box, resolution);
            DataSource.Close();
            return features;
        }

        #endregion
    }
}
