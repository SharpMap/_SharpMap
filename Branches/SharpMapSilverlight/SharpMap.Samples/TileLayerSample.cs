using System;
using System.Collections.Generic;
using System.Text;
using SharpMap.Layers;
using BruTile;
using BruTile.Web;
using SharpMap;
using SharpMap.Geometries;
using SharpMap.Providers;
using SharpMap.Styles;
using SharpMap.Data.Providers;
using System.Reflection;
using System.IO;

namespace SharpMap.Samples
{
    public static class TileLayerSample
    {
        public static Map InitializeMap()
        {
            Map map = new Map();
            Layer osm = new Layer("OSM");
            osm.DataSource = new TileProvider(new OsmTileSource(), "OSM");
            map.Layers.Add(osm);

            Layer pointLayer = new Layer("Geodan");
            pointLayer.DataSource = new MemoryProvider(new Point(546919, 6862238)); // lonlat: 4.9130567, 52.3422033
            var style = new VectorStyle();
            style.Symbol = new Bitmap { data = GetImageStreamFromResource("SharpMap.Samples.Images.icon.png") };
            pointLayer.Style = style;
            map.Layers.Add(pointLayer);

            return map;
        }

        private static Stream GetImageStreamFromResource(string resourceString)
        {
            Assembly a = Assembly.GetExecutingAssembly();
            string[] resNames = a.GetManifestResourceNames();
            string icon = resourceString;
            Stream imageStream = a.GetManifestResourceStream(icon);
            return imageStream;
        }
    }
}
