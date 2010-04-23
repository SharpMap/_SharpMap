using System;
using System.IO;
using SharpMap;
using SharpMap.Data.Providers;
using SharpMap.Layers;
using SharpMap.Rendering;
using SharpMap.Rendering.Thematics;
using SharpMap.Styles;
using ColorBlend=SharpMap.Rendering.Thematics.ColorBlend;
using Point=SharpMap.Geometries.Point;
using BruTile.Web;
using SharpMap.Providers;
using BruTile;
using System.Collections.Generic;

namespace SharpMap.Samples
{
    public static class GradiantThemeSample
    {
        public static Map InitializeMap()
        {
            //Initialize a new map based on the simple map
            Map map = new Map();

            //Layer osm = new Layer("OSM");
            //string url = "http://labs.metacarta.com/wms-c/tilecache.py?version=1.1.1&amp;request=GetCapabilities&amp;service=wms-c";
            //var tileSources = TileSourceWmsC.TileSourceBuilder(new Uri(url), null);
            //var tileSource = new List<ITileSource>(tileSources).Find(source => source.Schema.Name == "osm-map");
            //osm.DataSource = new TileProvider(tileSource, "OSM");
            //map.Layers.Add(osm);

            //Set up countries layer
            Layer layCountries = new Layer("Countries");
            //Set the datasource to a shapefile in the App_data folder
            layCountries.DataSource = new ShapeFile("GeoData/World/countries.shp", true);
            //Set fill-style to green
            
            layCountries.Style.Fill = new Brush() { Fill = Color.DarkGreen};
             //Set the polygons to have a black outline
            layCountries.Style.Outline = new Pen() { Color = Color.Black };
            layCountries.Style.EnableOutline = true;
            layCountries.SRID = 4326;
            map.Layers.Add(layCountries);

            //set up cities layer
            Layer layCities = new Layer("Cities");
            //Set the datasource to a shapefile in the App_data folder
            layCities.DataSource = new ShapeFile("GeoData/World/cities.shp", true);
            layCities.Style.SymbolScale = 0.8f;
            layCities.MaxVisible = 0.5;
             layCities.SRID = 4326;
            map.Layers.Add(layCities);

            //Set up a country label layer
            //LabelLayer layLabel = new LabelLayer("Country labels");
            //layLabel.DataSource = layCountries.DataSource;
            //layLabel.Enabled = true;
            //layLabel.LabelColumn = "Name";
            //layLabel.Style = new LabelStyle();
            //layLabel.Style.ForeColor = Color.White;
            //layLabel.Style.Font = new Font(FontFamily.GenericSerif, 12);
            //layLabel.Style.BackColor = new SolidBrush(Color.FromArgb(128, 255, 0, 0));
            //layLabel.MaxVisible = 90;
            //layLabel.MinVisible = 30;
            //layLabel.Style.HorizontalAlignment = LabelStyle.HorizontalAlignmentEnum.Center;
            //layLabel.SRID = 4326;
            ////!!!layLabel.MultipartGeometryBehaviour = LabelLayer.MultipartGeometryBehaviourEnum.Largest;
            //map.Layers.Add(layLabel);

            ////Set up a city label layer
            //LabelLayer layCityLabel = new LabelLayer("City labels");
            //layCityLabel.DataSource = layCities.DataSource;
            //layCityLabel.Enabled = true;
            //layCityLabel.LabelColumn = "Name";
            //layCityLabel.Style = new LabelStyle();
            //layCityLabel.Style.ForeColor = Color.Black;
            //layCityLabel.Style.Font = new Font(FontFamily.GenericSerif, 11);
            //layCityLabel.MaxVisible = layLabel.MinVisible;
            //layCityLabel.Style.HorizontalAlignment = LabelStyle.HorizontalAlignmentEnum.Left;
            //layCityLabel.Style.VerticalAlignment = LabelStyle.VerticalAlignmentEnum.Bottom;
            //layCityLabel.Style.Offset = new PointF(3, 3);
            //layCityLabel.Style.Halo = new Pen(Color.Yellow, 2);
            //layCityLabel.TextRenderingHint = TextRenderingHint.AntiAlias;
            //layCityLabel.SmoothingMode = SmoothingMode.AntiAlias;
            //layCityLabel.SRID = 4326;
            ////!!!layCityLabel.LabelFilter = LabelCollisionDetection.ThoroughCollisionDetection;
            //layCityLabel.Style.CollisionDetection = true;
            //map.Layers.Add(layCityLabel);


            //Set a gradient theme on the countries layer, based on Population density
            //First create two styles that specify min and max styles
            //In this case we will just use the default values and override the fill-colors
            //using a colorblender. If different line-widths, line- and fill-colors where used
            //in the min and max styles, these would automatically get linearly interpolated.
            VectorStyle min = new VectorStyle();
            VectorStyle max = new VectorStyle();
            //Create theme using a density from 0 (min) to 400 (max)
            GradientTheme popdens = new GradientTheme("PopDens", 0, 400, min, max);
            //We can make more advanced coloring using the ColorBlend'er.
            //Setting the FillColorBlend will override any fill-style in the min and max fills.
            //In this case we just use the predefined Rainbow colorscale
            popdens.FillColorBlend = ColorBlend.Rainbow5;
            layCountries.Theme = popdens;

            //Lets scale the labels so that big countries have larger texts as well
            //LabelStyle lblMin = new LabelStyle();
            //LabelStyle lblMax = new LabelStyle();
            //lblMin.ForeColor = Color.Black;
            //lblMin.Font = new Font(FontFamily.GenericSerif, 6);
            //lblMax.ForeColor = Color.Blue;
            //lblMax.BackColor = new SolidBrush(Color.FromArgb(128, 255, 255, 255));
            //lblMin.BackColor = lblMax.BackColor;
            //lblMax.Font = new Font(FontFamily.GenericSerif, 9);
            //!!!layLabel.Theme = new GradientTheme("PopDens", 0, 400, lblMin, lblMax);

            //Lets scale city icons based on city population
            //cities below 1.000.000 gets the smallest symbol, and cities with more than 5.000.000 the largest symbol
            VectorStyle citymin = new VectorStyle();
            VectorStyle citymax = new VectorStyle();
            string iconPath = "Images/icon.png";
            if (!File.Exists(iconPath))
            {
                throw new Exception(
                    String.Format("Error file '{0}' could not be found, make sure it is at the expected location",
                                  iconPath));
            }

            citymin.Symbol = new Bitmap() { data = new FileStream(iconPath, FileMode.Open, FileAccess.Read) };
            citymin.SymbolScale = 0.5f;
            citymax.Symbol = new Bitmap() { data = new FileStream(iconPath, FileMode.Open, FileAccess.Read) };
            citymax.SymbolScale = 1f;
            layCities.Theme = new GradientTheme("Population", 1000000, 5000000, citymin, citymax);

            //limit the zoom to 360 degrees width
            map.MaximumZoom = 360;
            map.BackColor = Color.White;

            return map;
        }
    }
}