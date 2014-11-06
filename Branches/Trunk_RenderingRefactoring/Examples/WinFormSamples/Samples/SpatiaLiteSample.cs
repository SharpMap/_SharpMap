using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using GeoAPI.Features;
using GeoAPI.Geometries;
using SharpMap;
using SharpMap.Data.Providers;
using SharpMap.Layers;
using SharpMap.Rendering;
using SharpMap.Styles;

namespace WinFormSamples.Samples
{
    public static class SpatiaLiteSample
    {
        private static string DataSource
        {
            get { return @"Data Source=GeoData\SpatiaLite\sample.sqlite"; }
        }
        
        
        public static Map InitializeMap(float angle)
        {
            //Initialize a new map of size 'imagesize'
            Map map = new Map();

            //Set up the countries layer
            VectorLayer layCountries = new VectorLayer("Countries");

            //Set the datasource to a shapefile in the App_data folder
            layCountries.DataSource = new SpatiaLite(
                DataSource, "countries", "geom", "PK_UID");

            //Set fill-style to green
            layCountries.Style.Fill = new SolidBrush(Color.Green);
            //Set the polygons to have a black outline
            layCountries.Style.Outline = Pens.Black;
            layCountries.Style.EnableOutline = true;

            //Set up a river layer
            VectorLayer layRivers = new VectorLayer("Rivers");
            //Set the datasource to a shapefile in the App_data folder
            layRivers.DataSource = new SpatiaLite(
                DataSource, "rivers", "geom", "PK_UID");
            //Define a blue 3px wide pen
            layRivers.Style.Line = new Pen(Color.LightBlue, 2);
            layRivers.Style.Line.CompoundArray = new[] { 0.2f, 0.8f };
            layRivers.Style.Outline = new Pen(Color.DarkBlue, 3);
            layRivers.Style.EnableOutline = true;

            //Set up a cities layer
            VectorLayer layCities = new VectorLayer("Cities");
            //Set the datasource to the spatialite table
            layCities.DataSource = new SpatiaLite(
                DataSource, "cities", "geom", "PK_UID");
            layCities.Style.SymbolScale = 0.8f;
            layCities.MaxVisible = 40;

            //Set up a country label layer
            LabelLayer layLabel = new LabelLayer("Country labels") 
            {
                DataSource = layCountries.DataSource,
                LabelColumn = "NAME",
                MultipartGeometryBehaviour = LabelLayer.MultipartGeometryBehaviourEnum.Largest,
                LabelFilter = LabelCollisionDetection.ThoroughCollisionDetection,
                PriorityColumn = "POPDENS",
                Style = new LabelStyle
                {
                    ForeColor = Color.White,
                    Font = new Font(FontFamily.GenericSerif, 12),
                    BackColor = new SolidBrush(Color.FromArgb(128, 255, 0, 0)),
                    HorizontalAlignment = LabelStyle.HorizontalAlignmentEnum.Center,
                    MaxVisible = 90,
                    MinVisible = 30,
                    CollisionDetection = true
                }
            };

            //Set up a city label layer
            LabelLayer layCityLabel = new LabelLayer("City labels")
            {
                DataSource = layCities.DataSource,
                LabelColumn = "name",
                PriorityColumn = "population",
                PriorityDelegate = delegate(IFeature fdr) 
                { 
                    Int32 retVal = 10000000 * ( (String)fdr.Attributes["capital"] == "Y" ? 1 : 0 );
                    return  retVal + Convert.ToInt32(fdr.Attributes["population"]);
                },
                TextRenderingHint = TextRendering.AntiAlias,
                SmoothingMode = Smoothing.AntiAlias,
                LabelFilter = LabelCollisionDetection.ThoroughCollisionDetection,
                Style = new LabelStyle
                {
                    ForeColor = Color.Black,
                    Font = new Font(FontFamily.GenericSerif, 11),
                    MaxVisible = layLabel.MinVisible,
                    HorizontalAlignment = LabelStyle.HorizontalAlignmentEnum.Left,
                    VerticalAlignment = LabelStyle.VerticalAlignmentEnum.Bottom,
                    Offset = new PointF(3, 3),
                    Halo = new Pen(Color.Yellow, 2),
                    CollisionDetection = true
                }
            };

            LabelLayer layRiverLabels = new LabelLayer("RiverLabels");
            layRiverLabels.DataSource = layRivers.DataSource;
            layRiverLabels.LabelColumn = "Name";
            layRiverLabels.PriorityDelegate = GetRiverLength;
            layRiverLabels.Style = new LabelStyle
                                       {
                                           Font = new Font("Arial", 10, FontStyle.Bold),
                                           Halo = new Pen(Color.Azure, 2),
                                           ForeColor = Color.DarkCyan,
                                           IgnoreLength = true,
                                           Enabled = true,
                                           CollisionDetection = true,
                                          
                                       };
            layRiverLabels.LabelFilter = LabelCollisionDetection.ThoroughCollisionDetection;

            //Add the layers to the map object.
            //The order we add them in are the order they are drawn, so we add the rivers last to put them on top
            map.Layers.Add(layCountries);
            map.Layers.Add(layRivers);
            map.Layers.Add(layCities);
            map.Layers.Add(layRiverLabels);
            map.Layers.Add(layLabel);
            map.Layers.Add(layCityLabel);

            //limit the zoom to 360 degrees width
            map.MaximumZoom = 360;
            map.BackColor = Color.LightBlue;

            map.ZoomToExtents(); // = 360;

            Matrix mat = new Matrix();
            mat.RotateAt(angle, map.WorldToImage(map.Center));
            map.MapTransform = mat;
            return map;

        }

        internal static Map InitializeMap(float angle, string[] filenames)
        {
            if (filenames == null)
                return null;

            Map map = new Map();

            try
            {
                foreach (string filename in filenames)
                {
                    string connectionString = string.Format("Data Source={0}", filename);
                    foreach (SpatiaLite provider in SpatiaLite.GetSpatialTables(connectionString))
                    {
                        map.Layers.Add(
                            new VectorLayer(
                                string.Format("{0} - {1}", provider.Table, provider.GeometryColumn), provider) { Style = LayerTools.GetRandomVectorStyle() });
                    }
                }
                if (map.Layers.Count > 0)
                {
                    map.ZoomToExtents();

                    Matrix mat = new Matrix();
                    mat.RotateAt(angle, map.WorldToImage(map.Center));
                    map.MapTransform = mat;
                    return map;
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
            return null;
        }

        internal static int GetRiverLength(IFeature row)
        {
            if (row == null) return 0;
            if (row.Geometry == null)
                return 0;
            IMultiLineString ml = row.Geometry as IMultiLineString;
            if (ml != null)
                return Convert.ToInt32(ml.Length);

            ILineString l = row.Geometry as ILineString;
            if (l != null)
                return Convert.ToInt32(l.Length);
            return 0;
        }
    }
}
