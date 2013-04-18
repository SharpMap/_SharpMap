using System;
using System.Collections;
using System.Linq;
using System.Text;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;
using NetTopologySuite.Extensions.Coverages;
using SharpMap.Data.Providers;
using SharpMap.Rendering;
using SharpMap.Styles;

namespace SharpMap.Layers
{
    public class NetworkCoverageLocationLayer : VectorLayer,ICoverageLayer
    {
        private NetworkCoverageFeatureCollection NetworkCoverageFeatureCollection
        {
            get { return (NetworkCoverageFeatureCollection) DataSource; }
        }

        private readonly NetworkCoverageLocationRenderer renderer;

        public NetworkCoverageLocationLayer()
        {
            //Coverage = coverage;
            renderer = new NetworkCoverageLocationRenderer();
            DataSource = new NetworkCoverageFeatureCollection {NetworkCoverageFeatureType= NetworkCoverageFeatureType.Locations};
            CustomRenderers.Add(renderer);
        }
        
        public override void OnRender(System.Drawing.Graphics g, Map map)
        {
            renderer.Render(null, g, this);
        }

        public ICoverage Coverage
        {
            get
            {
                if (NetworkCoverageFeatureCollection != null)
                {
                    return NetworkCoverageFeatureCollection.NetworkCoverage;
                }
                return null;
            }
            set
            {
                if (NetworkCoverageFeatureCollection != null)
                {
                    NetworkCoverageFeatureCollection.NetworkCoverage = (INetworkCoverage) value;
                }
            }
        }
    }
}
