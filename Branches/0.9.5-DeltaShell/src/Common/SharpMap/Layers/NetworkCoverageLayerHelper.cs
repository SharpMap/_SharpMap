using System.Drawing;
using System.Drawing.Drawing2D;
using DelftTools.Utils.Drawing;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Extensions.Networks;
using NetTopologySuite.Extensions.Coverages;
using SharpMap.Rendering.Thematics;
using SharpMap.Styles;
using System.Linq;

namespace SharpMap.Layers
{
    public static class NetworkCoverageLayerHelper
    {
        public static void SetupRouteNetworkCoverageLayerTheme(INetworkCoverageLayer layer, Color ?color)
        {
            if (null == color)
            {
                color = Color.FromArgb(100, Color.Green);
            }
            
            // customize theme
            var segmentTheme = ThemeFactory.CreateSingleFeatureTheme(layer.SegmentLayer.Style.GeometryType, (Color)color, 10);
            var locationTheme = ThemeFactory.CreateSingleFeatureTheme(layer.LocationLayer.Style.GeometryType, (Color)color, 15);

            layer.SegmentLayer.Theme = segmentTheme;
            layer.LocationLayer.Theme = locationTheme;

            var locationStyle = (VectorStyle)locationTheme.DefaultStyle;
            locationStyle.Fill = Brushes.White;
            locationStyle.Shape = ShapeType.Ellipse;
            locationStyle.ShapeSize = 15;

            var segmentStyle = (VectorStyle)segmentTheme.DefaultStyle;
            segmentStyle.Line.EndCap = LineCap.ArrowAnchor;
        }


        /// <summary>
        /// Add a new empty route to the map in a newly added coverage Layer.
        /// </summary>
        /// <param name="map"></param>
        /// <param name="network"></param>
        /// <returns></returns>
        public static void AddNewRoute2Map(Map map, INetwork network)
        {
            int count = map.Layers.Count(l => l is INetworkCoverageLayer);
            var routeCoverage = new NetworkCoverage(string.Format("route_{0}", (count + 1)), false, "Offset", "m")
            {
                Network = network,
                SegmentGenerationMethod = SegmentGenerationMethod.RouteBetweenLocations
            };
            //no sorting on route locations.
            routeCoverage.Locations.AutoSort = false;
            routeCoverage.Segments.AutoSort = false;
            var networkCoverageLayer = new NetworkCoverageLayer
                                           {
                                               NetworkCoverage = routeCoverage,
                                               Name = routeCoverage.Name,
                                               Map = map
                                           };
            map.Layers.Insert(0, networkCoverageLayer);
        }
    }
}