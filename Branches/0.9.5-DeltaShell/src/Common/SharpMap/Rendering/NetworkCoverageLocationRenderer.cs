using System;
using System.Collections;
using System.Drawing;
using System.Linq;
using System.Text;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;
using NetTopologySuite.Extensions.Coverages;
using SharpMap.Data.Providers;
using SharpMap.Layers;
using SharpMap.Styles;

namespace SharpMap.Rendering
{
    class NetworkCoverageLocationRenderer : NetworkCoverageRenderer
    {
        public override bool Render(IFeature feature, Graphics g, ILayer layer)
        {
            var locationLayer = (NetworkCoverageLocationLayer) layer;
            var coverage = ((NetworkCoverageFeatureCollection)layer.DataSource).RenderedCoverage;
            var mapExtents = layer.Map.Envelope;

            var sliceValues = coverage.GetValues();
            var locations = coverage.Locations.Values.ToArray();

            if(sliceValues.Count == 0)
            {
                return true;
            }

            //1 find the locations withing the current extend
            /*var locations = coverage.Locations.Values
                .Where(networkLocation => networkLocation.Geometry.EnvelopeInternal.Intersects(mapExtents)).ToList();*/

            for (var i = 0; i < locations.Length; i++)
            {
                var location = locations[i];
                
                if (!location.Geometry.EnvelopeInternal.Intersects(mapExtents))
                {
                    continue;
                }
                
                //2 get the values for this location
                var value = (double) sliceValues[i];
                
                //3 use the Theme of the layer to draw a line.
                var vectorStyle = GetStyle(locationLayer, value);

                VectorRenderingHelper.RenderGeometry(g, layer.Map, location.Geometry, vectorStyle, null, true);
            }

            return true;
        }

        /// <summary>
        /// Returns style based on theme or if no theme is defined uses the style defined on the layer.
        /// </summary>
        /// <param name="locationLayer">Layer on which the style is defined</param>
        /// <param name="value">Value</param>
        /// <returns></returns>
        private static VectorStyle GetStyle(VectorLayer locationLayer, double value)
        {
            if (locationLayer.Theme != null)
            {
                return (VectorStyle)locationLayer.Theme.GetStyle(value);    
            }
            return locationLayer.Style;
        }


        //Why ask the renderer when you know already?
        public override IList GetFeatures(IEnvelope box, ILayer layer)
        {
            return layer.DataSource.GetFeatures(box);
            /*var coverage = ((NetworkCoverageLocationLayer)layer).Coverage;
            var segments = coverage.Locations.Values
                .Where(networkLocation => networkLocation.Geometry.EnvelopeInternal.Intersects(box)).ToList();
            return segments;*/
        }

        
    }
}
;