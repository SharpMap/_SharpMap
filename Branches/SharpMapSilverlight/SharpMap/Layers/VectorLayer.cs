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
    public class VectorLayer : Layer
    {
        private SharpMap.Rendering.Thematics.ITheme _theme;
        private SharpMap.Data.Providers.IProvider _DataSource;
        private Styles.VectorStyle _Style;

        /// <summary>
        /// Initializes a new layer
        /// </summary>
        /// <param name="layername">Name of layer</param>
        public VectorLayer(string layername)
        {
            this.Style = new SharpMap.Styles.VectorStyle();
            this.LayerName = layername;
        }

        /// <summary>
        /// Gets or sets thematic settings for the layer. Set to null to ignore thematics
        /// </summary>
        public SharpMap.Rendering.Thematics.ITheme Theme
        {
            get { return _theme; }
            set { _theme = value; }
        }

        /// <summary>
        /// Gets or sets the datasource
        /// </summary>
        public SharpMap.Data.Providers.IProvider DataSource
        {
            get { return _DataSource; }
            set { _DataSource = value; }
        }

        /// <summary>
        /// Gets or sets the rendering style of the vector layer.
        /// </summary>
        public Styles.VectorStyle Style
        {
            get { return _Style; }
            set { _Style = value; }
        }

        /// <summary>
        /// Gets or sets the SRID of this VectorLayer's data source
        /// </summary>
        /// 
        public override int SRID
        {
            get
            {
                if (this.DataSource == null)
                    throw (new Exception("DataSource property not set on layer '" + this.LayerName + "'"));

                return this.DataSource.SRID;
            }
            set { this.DataSource.SRID = value; }
        }

        #region ILayer Members

        /// <summary>
        /// Renders the layer to a graphics object
        /// </summary>
        /// <param name="g">Graphics object reference</param>
        /// <param name="map">Map which is rendered</param>
        public override void Render(IRenderer renderer, IMapTransform mapTransform)
        {
            renderer.Render(DataSource, CreateStyleMethod(Style, Theme), CoordinateTransformation, mapTransform);
            base.Render(renderer, mapTransform);
        }

        private static Func<IFeatureRow, IStyle>  CreateStyleMethod(IStyle style, ITheme theme)
        {
            if (theme == null) 
                return (row) => style;
            else 
                return (row) => theme.GetStyle(row);
        }

        /// <summary>
        /// Returns the extent of the layer
        /// </summary>
        /// <returns>Bounding box corresponding to the extent of the features in the layer</returns>
        public override BoundingBox Envelope
        {
            get
            {
                if (this.DataSource == null)
                    throw (new Exception("DataSource property not set on layer '" + this.LayerName + "'"));

                if (DataSource is IVectorProvider)
                {

                    bool wasOpen = (this.DataSource as IVectorProvider).IsOpen;
                    if (!wasOpen)
                        (this.DataSource as IVectorProvider).Open();
                    SharpMap.Geometries.BoundingBox box = this.DataSource.GetExtents();
                    if (!wasOpen) //Restore state
                        (this.DataSource as IVectorProvider).Close();
                    if (this.CoordinateTransformation != null)
                        throw new NotImplementedException();
                    //!!!return ProjNet.CoordinateSystems.Transformations.GeometryTransform.TransformBox(box, this.CoordinateTransformation.MathTransform);
                    return box;
                }
                else
                {
                    return (DataSource as IRasterProvider).GetExtents();
                }
            }
        }

        #endregion
    }
}
