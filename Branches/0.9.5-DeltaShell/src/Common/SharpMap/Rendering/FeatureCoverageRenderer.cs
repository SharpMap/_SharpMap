using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;
using SharpMap.Api;
using SharpMap.Layers;
using SharpMap.Styles;

namespace SharpMap.Rendering
{
    public class FeatureCoverageRenderer : IFeatureRenderer
    {
        static IGeometryFactory geometryFactory = new GeometryFactory();
        
        #region Implementation of IFeatureRenderer

        /// <summary>
        /// Renders all features on the currently visible part of the map, using the values from the feature coverage.
        /// </summary>
        /// <param name="feature">The coverage to render.</param>
        /// <param name="g">Graphics object to be used as a target for rendering.</param>
        /// <param name="layer">Layer where coverage belongs to.</param>
        /// <returns>When rendering succeds returns true, otherwise false.</returns>
        public virtual bool Render(IFeature feature, Graphics g, ILayer layer)
        {
            // Use the FeatureCoverage function to get the coverage values
            var coverageLayer = (FeatureCoverageLayer)layer;
            IFeatureCoverage coverage = coverageLayer.FeatureCoverageToRender;
            var map = coverageLayer.Map;

            // No theme? No rendering! (This should be set in the FeatureCoverageLayer class' set_Features.)
            if (coverageLayer.Theme == null)
            {
                return false;
            }

            // What features to render?
            IList featuresToRender;

            IFeature[] coverageFeatures;
            double[] values;

            lock (coverage.Store) // makes sure that features don't change before we get values
            {
                featuresToRender = coverageLayer.GetFeatures(geometryFactory.ToGeometry(map.Envelope), false).ToArray();
                if (featuresToRender.Count <= 0)
                {
                    // No features in the envelope, so no rendering required.
                    return true;
                }
                
                //get the component values in an array
                values = coverage.Components[0].Values.Cast<double>().ToArray();
                if (values.Length == 0) //reset features to render if no values are found..perhaps other arguments are cleared
                {
                    coverageFeatures = new IFeature[] {};
                }
                else
                {
                    //render all features
                    coverageFeatures = coverage.FeatureVariable.Values.Cast<IFeature>().ToArray();
                }
            }


            for (var i = 0; i < coverageFeatures.Length; i++)
            {
                var featureToRender = coverageFeatures[i];

                if(!featuresToRender.Contains(featureToRender))
                {
                    continue;
                }

                var geometry = featureToRender.Geometry;
                if (GeometryForFeatureDelegate != null)
                {
                    geometry = GeometryForFeatureDelegate(featureToRender);
                }

                // Use the GetStyle with the retrieved value
                var style = coverageLayer.Theme.GetStyle(values[i]) as VectorStyle;
                if (style != null)
                {
                    // Draw background of all line-outlines first

                    if (geometry is ILineString)
                    {
                        if (style.Enabled && style.EnableOutline)
                            VectorRenderingHelper.DrawLineString(g, geometry as ILineString, style.Outline, map);
                    }
                    else if (geometry is IMultiLineString)
                    {
                        if (style.Enabled && style.EnableOutline)
                            VectorRenderingHelper.DrawMultiLineString(g, geometry as IMultiLineString,
                                                                      style.Outline, map);
                    }

                    // Draw actual geometry
                    VectorRenderingHelper.RenderGeometry(g, map, geometry, style, VectorLayer.DefaultPointSymbol,
                                                         coverageLayer.ClippingEnabled);
                }
                else
                {
                    throw new ArgumentException("No style could be gotten from the theme; the feature cannot be rendered.");
                }
            }

            return true;
        }

        public IGeometry GetRenderedFeatureGeometry(IFeature feature, ILayer layer)
        {
            return GeometryForFeatureDelegate != null ? GeometryForFeatureDelegate(feature) : feature.Geometry;
        }

        public bool UpdateRenderedFeatureGeometry(IFeature feature, ILayer layer)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<IFeature> GetFeatures(IGeometry geometry, ILayer layer)
        {
            return GetFeatures(geometry.EnvelopeInternal, layer);
        }

        #endregion

        #region IFeatureRenderer Members


        public IEnumerable<IFeature> GetFeatures(IEnvelope box, ILayer layer)
        {
            return layer.GetFeatures(geometryFactory.ToGeometry(box), false);

            var intersectedFeatures = new List<IFeature>();

            foreach (IFeature feature in layer.DataSource.Features)
            {
                IGeometry geometry = GetRenderedFeatureGeometry(feature, layer);
                if (geometry.EnvelopeInternal.Intersects(box))
                {
                    intersectedFeatures.Add(feature);
                }
            }
            return intersectedFeatures;
        }

        #endregion

        public Func<IFeature,IGeometry> GeometryForFeatureDelegate { get; set; }
    }
}
