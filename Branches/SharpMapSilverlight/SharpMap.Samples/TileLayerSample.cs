using System;
using System.Collections.Generic;
using System.Text;
using SharpMap.Layers;
using BruTile;
using BruTile.Web;
using SharpMap;
using SharpMap.Geometries;
using SharpMap.Providers;

namespace WinFormSamples.Samples
{
    public class TileLayerSample
    {
        public static Map InitializeMap()
        {
            Map map = new Map();
            Layer osm = new Layer("OSM");
            osm.DataSource = new TileProvider(new TileSourceOsm(), "OSM");
            map.Layers.Add(osm);
            return map;
        }
    }
}
