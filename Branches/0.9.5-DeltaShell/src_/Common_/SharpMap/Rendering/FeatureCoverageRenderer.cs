using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using DelftTools.Functions.Filters;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;
using SharpMap.Layers;
using SharpMap.Rendering.Thematics;
using SharpMap.Styles;

namespace SharpMap.Rendering
{
    public class FeatureCoverageRenderer : IFeatureRenderer
    {
        #region Implementation of IFeatureRenderer

        /// <summary>
        /// Renders all features on the currently visible part of the map, using the values from the feature coverage.
        /// </summary>
        /// <param name="feature">The coverage to render.</param>
        /// <param name="g">Graphics object to be used as a target for rendering.</param>
        /// <param name="layer">Layer where coverage belongs to.</param>
        /// <returns>When rendering succeds returns true, otherwise false.</returns>
        public bool Render(IFeature feature, Graphics g, ILayer layer)
        {
            // Use the FeatureCoverage function to get the coverage values
            var coverageLayer = (FeatureCoverageLayer)layer;
            IFeatureCoverage coverage = coverageLayer.FeatureCoverage;
            Map map = coverageLayer.Map;

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
                featuresToRender = coverageLayer.DataSource.GetFeatures(map.Envelope).Cast<IFeature>().ToArray();
                if (featuresToRender.Count <= 0)
                {
                    // No features in the envelope, so no rendering required.
                    return true;
                }

                coverageFeatures = coverage.FeatureVariable.Values.Cast<IFeature>().ToArray();
                values = coverage.Components[0].Values.Cast<double>().ToArray();
            }


            for (var i = 0; i < coverageFeatures.Length; i++)
            {
                var featureToRender = coverageFeatures[i];

                if(!featuresToRender.Contains(featureToRender))
                {
                    continue;
                }

                // Use the GetStyle with the retrieved value
                var style = coverageLayer.Theme.GetStyle(values[i]) as VectorStyle;
                if (style != null)
                {
                    // Draw background of all line-outlines first
                    if (feature.Geometry is ILineString)
                    {
                        if (style.Enabled && style.EnableOutline)
                            VectorRenderingHelper.DrawLineString(g, featureToRender.Geometry as ILineString, style.Outline, map);
                    }
                    else if (feature.Geometry is IMultiLineString)
                    {
                        if (style.Enabled && style.EnableOutline)
                            VectorRenderingHelper.DrawMultiLineString(g, featureToRender.Geometry as IMultiLineString,
                                                                      style.Outline, map);
                    }

                    // Draw actual geometry
                    VectorRenderingHelper.RenderGeometry(g, map, featureToRender.Geometry, style, VectorLayer.DefaultPointSymbol,
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
            throw new System.NotImplementedException();
        }

        public bool UpdateRenderedFeatureGeometry(IFeature feature, ILayer layer)
        {
            throw new System.NotImplementedException();
        }

        public IList<IFeature> GetIntersectedFeatures(IGeometry geometry, ILayer layer)
        {
            throw new System.NotImplementedException();
        }

        #endregion

        #region IFeatureRenderer Members


        public IList GetFeatures(IEnvelope box, ILayer layer)
        {
            return layer.DataSource.GetFeatures(box);
        }

        #endregion
        public void ClearCache()
        {
        }
    }
}
