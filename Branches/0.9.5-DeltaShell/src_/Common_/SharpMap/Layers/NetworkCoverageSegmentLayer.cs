using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;
using SharpMap.Data.Providers;
using SharpMap.Rendering;
using SharpMap.Styles;

namespace SharpMap.Layers
{
    public class NetworkCoverageSegmentLayer : VectorLayer, ICoverageLayer
    {
        private readonly NetworkCoverageSegmentRenderer segmentRenderer;

        public NetworkCoverageSegmentLayer()
        {
            //Coverage = coverage;
            segmentRenderer = new NetworkCoverageSegmentRenderer();
            CustomRenderers.Add(segmentRenderer);
        }
        
        public override IEnvelope Envelope
        {
            get
            {
                //intersectect the geometries of all the segments.
                if (Coverage == null)
                {
                    return new Envelope();
                }

                var result = new Envelope();
                foreach (var s in Coverage.Segments.Values)
                {
                    result.ExpandToInclude(s.Geometry.EnvelopeInternal);
                }
                return result;
            }
        }

        private NetworkCoverageFeatureCollection NetworkCoverageFeatureCollection
        {
            get { return (NetworkCoverageFeatureCollection)DataSource; }
        }
        public INetworkCoverage Coverage
        {
            get { return NetworkCoverageFeatureCollection.NetworkCoverage; }
            set { NetworkCoverageFeatureCollection.NetworkCoverage = value; }
        }

        public void SetCurrentTime(DateTime value)
        {
            CurrentTime = value;
        }


        public virtual DateTime? CurrentTime
        {
            get; set;
        }

        public override void OnRender(System.Drawing.Graphics g, Map map)
        {
            segmentRenderer.Render(Coverage, g, this);
        }

        ICoverage ICoverageLayer.Coverage
        {
            get { return Coverage; }
            set { Coverage = (INetworkCoverage) value; }
        }
    }
}
