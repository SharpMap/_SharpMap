using System;
using System.Collections;
using System.Drawing;
using System.Linq;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;
using SharpMap.Data.Providers;
using SharpMap.Layers;
using SharpMap.Styles;

namespace SharpMap.Rendering
{
    class NetworkCoverageSegmentRenderer : NetworkCoverageRenderer
    {
        public override bool Render(IFeature feature, Graphics g, ILayer layer)
        {
            var segmentsLayer = (NetworkCoverageSegmentLayer) layer;
            var coverage = ((NetworkCoverageFeatureCollection)layer.DataSource).RenderedCoverage;
            IEnvelope mapExtents = layer.Map.Envelope;
            
            var sliceValues = coverage.GetValues();

            // 1 find the segments withing the current extend
            var segments = coverage.Segments.Values;//.Where(seg => seg.Geometry.EnvelopeInternal.Intersects(mapExtents)).ToList();

            for (int i = 0; i < segments.Count; i++)
            {
                INetworkSegment segment = segments[i];
                if (segment.Geometry.EnvelopeInternal.Intersects(mapExtents) && sliceValues.Count > 0)
                {
                    // 2 get the values for this segment
                    // if SegmentGenerationMethod == SegmentGenerationMethod.RouteBetweenLocations the segments and 
                    // location do not have to match; return default value
                    double value = coverage.SegmentGenerationMethod == SegmentGenerationMethod.RouteBetweenLocations
                                       ? 0
                                       : (double) sliceValues[i];

                    // 3 use the Theme of the layer to draw 
                    var style = (VectorStyle)segmentsLayer.Theme.GetStyle(value);
                    VectorRenderingHelper.RenderGeometry(g, layer.Map, segment.Geometry, style, null, true);
                }
            }
            return true;
        }
        
        //Why ask the renderer when you know already?
        public override IList GetFeatures(IEnvelope box, ILayer layer)
        {
            var coverage = ((NetworkCoverageSegmentLayer)layer).Coverage;
            var segments = coverage.Segments.Values
                .Where(networkLocation => networkLocation.Geometry.EnvelopeInternal.Intersects(box)).ToList();
            return segments;
        }
    }
}
