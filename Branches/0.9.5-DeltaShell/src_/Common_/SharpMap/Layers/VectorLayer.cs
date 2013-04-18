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
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Reflection;
using DelftTools.Utils;
using DelftTools.Utils.Aop.NotifyPropertyChanged;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;
using NetTopologySuite.Extensions.Features;
using SharpMap.CoordinateSystems.Transformations;
using SharpMap.Data.Providers;
using SharpMap.Rendering;
using SharpMap.Rendering.Thematics;
using SharpMap.Styles;
using Point=GisSharpBlog.NetTopologySuite.Geometries.Point;

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
    [NotifyPropertyChanged]
    public class VectorLayer : Layer, IDisposable
    {
        //private static readonly ILog log = LogManager.GetLogger(typeof (VectorLayer));

        public static readonly Bitmap DefaultPointSymbol = (Bitmap)Image.FromStream(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("SharpMap.Styles.DefaultSymbol.png"));

        /// <summary>
        /// Create vectorlayer with default name.
        /// </summary>
        public VectorLayer() : this("")
        {
        }

        /// <summary>
        /// Initializes a new layer
        /// </summary>
        /// <param name="layername">Name of layer</param>
        public VectorLayer(string layername)
        {
            _Style = new VectorStyle();
            UpdateStyleGeometry();
            name = layername;
            // _SmoothingMode = SmoothingMode.AntiAlias;
            _SmoothingMode = SmoothingMode.HighSpeed;
        }

        /// <summary>
        /// Creates a clone of the vectorlayer given as parameter to the constructor
        /// </summary>
        /// <param name="layer"></param>
        public VectorLayer(VectorLayer layer)
        {
            _Style = (VectorStyle) layer.Style.Clone();
            name = layer.Name;
            _SmoothingMode = SmoothingMode.HighSpeed;
            // _SmoothingMode = SmoothingMode.AntiAlias;
        }

        /// <summary>
        /// Initializes a new layer with a specified datasource
        /// </summary>
        /// <param name="layername">Name of layer</param>
        /// <param name="dataSource">Data source</param>
        public VectorLayer(string layername, IFeatureProvider dataSource) : this(layername)
        {
            DataSource = dataSource;
        }

        private ITheme _Theme;

        /// <summary>
        /// Gets or sets thematic settings for the layer. Set to null to ignore thematics
        /// </summary>
        public override ITheme Theme
        {
            get { return _Theme; }
            set { _Theme = value; }
        }

        private bool _ClippingEnabled;

        /// <summary>
        /// Specifies whether polygons should be clipped prior to rendering
        /// </summary>
        /// <remarks>
        /// <para>Clipping will clip Polygon and
        /// <MultiPolygon to the current view prior
        /// to rendering the object.</para>
        /// <para>Enabling clipping might improve rendering speed if you are rendering 
        /// only small portions of very large objects.</para>
        /// </remarks>
        public virtual bool ClippingEnabled
        {
            get { return _ClippingEnabled; }
            set { _ClippingEnabled = value; }
        }

        private SmoothingMode _SmoothingMode;

        /// <summary>
        /// Render whether smoothing (antialiasing) is applied to lines and curves and the edges of filled areas
        /// </summary>
        public virtual SmoothingMode SmoothingMode
        {
            get { return _SmoothingMode; }
            set { _SmoothingMode = value; }
        }

        /// <summary>
        /// Gets or sets the datasource
        /// </summary>
        public override IFeatureProvider DataSource
        {
            get { return base.DataSource; }
            set
            {
                base.DataSource = value;
                UpdateStyleGeometry();
            }
        }

        private VectorStyle _Style;

        /// <summary>
        /// Gets or sets the rendering style of the vector layer.
        /// </summary>
        public virtual VectorStyle Style
        {
            get { return _Style; }
            set
            {
                _Style = value;
                UpdateStyleGeometry();
            }
        }

        private void UpdateStyleGeometry()
        {
            if (DataSource != null && _Style != null)
            {
                if (DataSource.GetFeatureCount() > 0)
                {
                    IFeature feature = DataSource.GetFeature(0);
                    IGeometry geometry = feature.Geometry;
                    if (geometry != null)
                    {
                        // hack set interface of geometry as VectorStyle.GeometryType
                        if (geometry is Point)
                        {
                            _Style.GeometryType = typeof(IPoint);
                        }
                        else if (geometry is LineString)
                        {
                            _Style.GeometryType = typeof(ILineString);
                        }
                        else if (geometry is MultiLineString)
                        {
                            _Style.GeometryType = typeof(IMultiLineString);
                        }
                        else if (geometry is Polygon)
                        {
                            _Style.GeometryType = typeof(IPolygon);
                        }
                        else if (geometry is MultiPolygon)
                        {
                            _Style.GeometryType = typeof(IMultiPolygon);
                        }
                    }
                }
            }
        }

        #region ILayer Members

        /// <summary>
        /// Renders the layer to a graphics object
        /// </summary>
        /// <param name="g">Graphics object reference</param>
        /// <param name="map">Map which is rendered</param>
        public override void OnRender(Graphics g, Map map) // TODO: remove map as parameter
        {
            if (map.Center == null)
                throw (new ApplicationException("Cannot render map. View center not specified"));

            if (g == null)
            {
                return;
            }
                
            g.SmoothingMode = SmoothingMode;
            
            //View to render
            IEnvelope envelope = map.Envelope; 
            if (CoordinateTransformation != null)
                envelope = GeometryTransform.TransformBox(envelope, CoordinateTransformation.MathTransform.Inverse());

            if (DataSource == null)
            {
                throw (new ApplicationException("DataSource property not set on layer '" + Name + "'"));
            }

            RenderFeatures(g, envelope, map);
        }

        private void RenderFeatures(Graphics g, IEnvelope envelope, Map map)
        {
            bool customRendererUsed = false;
            bool themeOn = Theme != null;

            IList features = DataSource.GetFeatures(envelope);
            
            int featureCount = features.Count; //optimization
            
            DateTime startRenderingTime = DateTime.Now;

            for (int i = 0; i < featureCount; i++)
            {
                IFeature currentFeature = (IFeature)features[i];

                // get geometry
                IGeometry currentGeometry = CoordinateTransformation != null 
                                ? GeometryTransform.TransformGeometry(currentFeature.Geometry, CoordinateTransformation.MathTransform) 
                                : currentFeature.Geometry;

                VectorStyle currentVectorStyle = themeOn 
                                ? Theme.GetStyle(currentFeature) as VectorStyle
                                : Style;

                // TODO: make it render only one time
                foreach (IFeatureRenderer renderer in CustomRenderers)
                {
                    customRendererUsed = renderer.Render(currentFeature, g, this);
                }
                if (!customRendererUsed)
                {
                    //Linestring outlines is drawn by drawing the layer once with a thicker line
                    //before drawing the "inline" on top.
                    if (Style.EnableOutline)
                    {
                        //Draw background of all line-outlines first
                        if (!themeOn ||
                            (currentVectorStyle != null && currentVectorStyle.Enabled &&
                             currentVectorStyle.EnableOutline))
                        {
                            switch (currentGeometry.GeometryType)
                            {
                                case "LineString":
                                    VectorRenderingHelper.DrawLineString(g, currentGeometry as ILineString,
                                                                         currentVectorStyle.Outline, map);
                                    break;
                                case "MultiLineString":
                                    VectorRenderingHelper.DrawMultiLineString(g, currentGeometry as IMultiLineString,
                                                                              currentVectorStyle.Outline, map);
                                    break;
                                default:
                                    break;
                            }
                        }
                    }

                    VectorRenderingHelper.RenderGeometry(g, map, currentGeometry, currentVectorStyle, DefaultPointSymbol, _ClippingEnabled);
                    lastRenderedCoordinatesCount += currentGeometry.Coordinates.Length;
                }
            }

            lastRenderedFeaturesCount = featureCount;
            lastRenderDuration = (DateTime.Now - startRenderingTime).TotalMilliseconds;
        }

        private long lastRenderedFeaturesCount;

        public virtual long LastRenderedFeaturesCount
        {
            get { return lastRenderedFeaturesCount; }
        }

        private long lastRenderedCoordinatesCount;

        public virtual long LastRenderedCoordinatesCount
        {
            get { return lastRenderedCoordinatesCount; }
        }

        private double lastRenderDuration;
        private IEnumerable<DateTime> times;

        public override double LastRenderDuration
        {
            get { return lastRenderDuration; }
        }

        /// <summary>
        /// Used for rendering benchmarking purposes.
        /// </summary>
        /// <param name="featureCount"></param>
        /// <param name="coordinateCount"></param>
        /// <param name="durationInMillis"></param>
        public virtual void SetRenderingTimeParameters(int featureCount, int coordinateCount, double durationInMillis)
        {
            lastRenderedFeaturesCount = featureCount;
            lastRenderedCoordinatesCount = coordinateCount;
            lastRenderDuration = durationInMillis;
        }

        /// <summary>
        /// Returns the extent of the layer
        /// </summary>
        /// <returns>Bounding box corresponding to the extent of the features in the layer</returns>
        public override IEnvelope Envelope
        {
            get
            {
                if (DataSource == null)
                {
                    return new Envelope();
                }

                IEnvelope envelope = DataSource.GetExtents();

                if (envelope != null && CoordinateTransformation != null)
                {
                    return GeometryTransform.TransformBox(envelope, CoordinateTransformation.MathTransform);
                }

                return envelope;
            }
        }

        #endregion

        /// <summary>
        /// Gets or sets the SRID of this VectorLayer's data source
        /// </summary>
        public override int SRID
        {
            get
            {
                if (DataSource == null)
                {
                    throw (new ApplicationException("DataSource property not set on layer '" + Name + "'"));
                }

                return DataSource.SRID;
            }
            set { DataSource.SRID = value; }
        }

        #region ICloneable Members

        /// <summary>
        /// Clones the layer
        /// </summary>
        /// <returns>cloned object</returns>
        public override object Clone()
        {
            var vectorLayer = (VectorLayer)base.Clone();
            vectorLayer.Style = (VectorStyle) Style.Clone();
            vectorLayer.SmoothingMode = SmoothingMode;
            vectorLayer.ClippingEnabled = ClippingEnabled;
            

            return vectorLayer;
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Disposes the object
        /// </summary>
        public virtual void Dispose()
        {
            if (DataSource != null)
            {
                DataSource.Dispose();
            }
        }

        #endregion
    }
}
