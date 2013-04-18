using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using DelftTools.Utils.Drawing;
using GeoAPI.Extensions.Networks;
using NetTopologySuite.Extensions.Coverages;
using SharpMap.Rendering.Thematics;
using SharpMap.Styles;
using System.Linq;

namespace SharpMap.Layers
{
    public static class NetworkCoverageLayerHelper
    {
        public static void SetupRouteNetworkCoverageLayerTheme(INetworkCoverageGroupLayer groupLayer, Color ?color)
        {
            if (null == color)
            {
                color = Color.FromArgb(100, Color.Green);
            }
            
            // customize theme
            var segmentTheme = ThemeFactory.CreateSingleFeatureTheme(groupLayer.SegmentLayer.Style.GeometryType, (Color)color, 10);
            var locationTheme = ThemeFactory.CreateSingleFeatureTheme(groupLayer.LocationLayer.Style.GeometryType, (Color)color, 15);

            groupLayer.SegmentLayer.Theme = segmentTheme;
            groupLayer.LocationLayer.Theme = locationTheme;

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
            var routeCoverage = new Route(string.Format("route_{0}", GetAvailableRouteNumber(map)), false, "Chainage", "m")
            {
                Network = network,
            };
            
            //no sorting on route locations.
            var networkCoverageLayer = new RouteGroupLayer
                                           {
                                               Route = routeCoverage,
                                               Name = routeCoverage.Name,
                                               Map = map
                                           };
            map.Layers.Insert(0, networkCoverageLayer);
        }

        private static int GetAvailableRouteNumber(Map map)
        {
            int lastNr = 0;

            foreach(var routeLayer in map.GetAllLayers(true).OfType<RouteGroupLayer>())
            {
                try
                {
                    lastNr = Int32.Parse(routeLayer.Route.Name.Split('_')[1]);
                    break;
                }
                catch (Exception)
                {
                    //don't do anything
                }
            }

            return lastNr + 1;
        }
    }
}