using System;
using System.IO;
using SharpMap.Data.Providers;
using SharpMap.Layers;
using SharpMap.Rendering;
using SharpMap.Rendering.Thematics;
using SharpMap.Styles;
using ColorBlend=SharpMap.Rendering.Thematics.ColorBlend;
using Point=SharpMap.Geometries.Point;

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
            Layer countryLayer = new Layer("Countries");
            //Set the datasource to a shapefile in the App_data folder
            countryLayer.DataSource = new ShapeFile("GeoData/World/countries.shp", true);
            //Set fill-style to green
            
            VectorStyle vectorStyle = new VectorStyle();
            vectorStyle.Fill = new Brush() { Fill = Color.Green};
             //Set the polygons to have a black outline
            vectorStyle.Outline = new Pen() { Color = Color.Black };
            vectorStyle.EnableOutline = true;
            countryLayer.Style = vectorStyle;
            countryLayer.SRID = 4326;
            map.Layers.Add(countryLayer);

            //set up cities layer
            Layer cityLayer = new Layer("Cities");
            //Set the datasource to a shapefile in the App_data folder
            cityLayer.DataSource = new ShapeFile("GeoData/World/cities.shp", true);
            cityLayer.Style = new VectorStyle() { SymbolScale = 0.8f };
            cityLayer.MaxVisible = 0.09;
            cityLayer.SRID = 4326;
            map.Layers.Add(cityLayer);

            //Set up a country label layer
            LabelLayer countryLabelLayer = new LabelLayer("Country labels");
            countryLabelLayer.DataSource = countryLayer.DataSource;
            countryLabelLayer.Enabled = true;
            countryLabelLayer.MaxVisible = 0.18;
            countryLabelLayer.MinVisible = 0.054;
            countryLabelLayer.SRID = 4326;
            countryLabelLayer.LabelColumn = "NAME";
            var labelStyle = new LabelStyle();
            labelStyle.ForeColor = Color.Black;
            labelStyle.Font = new Font() { FontFamily = "GenericSerif", Size = 12 };
            labelStyle.BackColor = new Brush() { Fill = new Color() { A = 128, R = 255, G = 255, B = 255 } };
            labelStyle.HorizontalAlignment = LabelStyle.HorizontalAlignmentEnum.Center;
            countryLabelLayer.MultipartGeometryBehaviour = LabelLayer.MultipartGeometryBehaviourEnum.Largest;
            countryLabelLayer.Style = labelStyle;
            map.Layers.Add(countryLabelLayer);

            //Set up a city label layer
            LabelLayer cityLabelLayer = new LabelLayer("City labels");
            cityLabelLayer.DataSource = cityLayer.DataSource;
            cityLabelLayer.Enabled = true;
            cityLabelLayer.LabelColumn = "NAME";
            cityLabelLayer.MaxVisible = countryLabelLayer.MinVisible;
            cityLabelLayer.MinVisible = 0;
            cityLabelLayer.SRID = 4326;
            cityLabelLayer.LabelFilter = LabelCollisionDetection.ThoroughCollisionDetection;
            
            var cityLabelStyle = new LabelStyle();
            cityLabelStyle.ForeColor = Color.Black;
            cityLabelStyle.Font = new Font() { FontFamily = "GenericSerif", Size = 11 };
            cityLabelStyle.HorizontalAlignment = LabelStyle.HorizontalAlignmentEnum.Left;
            cityLabelStyle.VerticalAlignment = LabelStyle.VerticalAlignmentEnum.Bottom;
            cityLabelStyle.Offset = new Offset() { X = 3, Y = 3 };
            cityLabelStyle.Halo = new Pen() { Color = Color.Yellow, Width = 2 };
            cityLabelStyle.CollisionDetection = true;
            cityLabelLayer.Style = cityLabelStyle;
            map.Layers.Add(cityLabelLayer);
            
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
            countryLayer.Theme = popdens;

            //Lets scale the labels so that big countries have larger texts as well
            LabelStyle lblMin = new LabelStyle();
            LabelStyle lblMax = new LabelStyle();
            lblMin.ForeColor = Color.Black;
            lblMin.Font = new Font() { FontFamily = "Sans Serif", Size = 6 };
            lblMax.ForeColor = Color.Black;
            lblMax.BackColor = new Brush() { Fill = new Color() { A = 128, R = 255, G = 255, B = 255 } };
            lblMin.BackColor = lblMax.BackColor;
            lblMax.Font = new Font() { FontFamily = "Sans Serif", Size = 9 };
            countryLabelLayer.Theme = new GradientTheme("PopDens", 0, 400, lblMin, lblMax);

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
            cityLayer.Theme = new GradientTheme("Population", 1000000, 5000000, citymin, citymax);
            
            Layer geodanLayer = new Layer("Geodan");
            geodanLayer.DataSource = new MemoryProvider(new Point(4.9130567, 52.3422033));
            var style = new VectorStyle();
            style.Symbol = new Bitmap() { data = new FileStream(iconPath, FileMode.Open, FileAccess.Read) };
            geodanLayer.Style = style;
            map.Layers.Add(geodanLayer);

            //limit the zoom to 360 degrees width
            map.MaximumZoom = 360;
            map.BackColor = Color.White;

            return map;
        }
    }
}