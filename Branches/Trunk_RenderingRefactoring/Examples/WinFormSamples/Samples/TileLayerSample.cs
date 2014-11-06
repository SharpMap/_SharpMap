using System;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net;
using System.Timers;
using System.Xml;
using BruTile.Web;
using GeoAPI;
using GeoAPI.CoordinateSystems;
using GeoAPI.CoordinateSystems.Transformations;
using GeoAPI.Features;
using GeoAPI.Geometries;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using SharpMap;
using SharpMap.Data;
using SharpMap.Data.Providers;
using SharpMap.Layers;
using SharpMap.Rendering;
using SharpMap.Rendering.Thematics;
using SharpMap.Styles;

namespace WinFormSamples.Samples
{
    class TileLayerSample
    {
        private static Int32 _num;
        
        public static Map InitializeMap(float angle)
        {
            switch (_num++ % 6)
            {
                case 3:
                    return InitializeMapOsm();
                case 4:
                    return InitializeMapBing(BingMapType.Roads);
                case 5:
                    return InitializeMapBing(BingMapType.Aerial);
                case 6:
                    return InitializeMapBing(BingMapType.Hybrid);
                    _num = 0;
                    /*
                case 7:
                    return InitializeMapGoogle(BruTile.Web.GoogleMapType.GoogleMap);
                case 8:
                    return InitializeMapGoogle(BruTile.Web.GoogleMapType.GoogleSatellite);
                case 9:
                    return InitializeMapGoogle(BruTile.Web.GoogleMapType.GoogleSatellite | BruTile.Web.GoogleMapType.GoogleLabels);
                case 10:
                    return InitializeMapGoogle(BruTile.Web.GoogleMapType.GoogleTerrain);
                case 11:
                    _num = 0;
                    return InitializeMapGoogle(BruTile.Web.GoogleMapType.GoogleLabels);
                     */
                case 0:
                    _num++;
                    return InitializeMapOsmWithXls(angle);
                    
                    //Does not work anymore!
                    //return InitializeMapOsmWithVariableLayerCollection(angle);
                case 1:
                    return InitializeMapOsmWithXls(angle);
                case 2:
                    return HeatLayerSample.InitializeMap(angle);

            }
            return InitializeMapOsm();
        }

        private static Map InitializeMapOsm()
        {
            Map map = new Map();

            TileAsyncLayer tileLayer = new TileAsyncLayer(new OsmTileSource(), "TileLayer - OSM");
            map.BackgroundLayer.Add(tileLayer);
            map.ZoomToBox(tileLayer.Envelope);
            
            return map;
        }

        private const string XlsConnectionString = @"Provider=Microsoft.Jet.OLEDB.4.0;Data Source={0}\{1};Extended Properties=""Excel 8.0;HDR=Yes;IMEX=1""";

        private static Map InitializeMapOsmWithXls(float angle)
        {
            Map map = new Map();

            TileAsyncLayer tileLayer = new TileAsyncLayer(new OsmTileSource(), "TileLayer - OSM with XLS");
            map.BackgroundLayer.Add(tileLayer);

            //Get data from excel
            string xlsPath = string.Format(XlsConnectionString, Directory.GetCurrentDirectory(), "GeoData\\Cities.xls");
            DataSet ds = new DataSet("XLS");
            using (OleDbConnection cn = new OleDbConnection(xlsPath))
            {
                cn.Open();
                using (OleDbDataAdapter da = new OleDbDataAdapter(new OleDbCommand("SELECT * FROM [Cities$]", cn)))
                    da.Fill(ds);
            }

#if !DotSpatialProjections

            //The SRS for this datasource is EPSG:4326, therefore we need to transfrom it to OSM projection
            CoordinateTransformationFactory ctf = new CoordinateTransformationFactory();
            CoordinateSystemFactory cf = new CoordinateSystemFactory();
            ICoordinateSystem epsg4326 = cf.CreateFromWkt("GEOGCS[\"WGS 84\",DATUM[\"WGS_1984\",SPHEROID[\"WGS 84\",6378137,298.257223563,AUTHORITY[\"EPSG\",\"7030\"]],AUTHORITY[\"EPSG\",\"6326\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.01745329251994328,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4326\"]]");
            ICoordinateSystem epsg3857 = cf.CreateFromWkt("PROJCS[\"Popular Visualisation CRS / Mercator\", GEOGCS[\"Popular Visualisation CRS\", DATUM[\"Popular Visualisation Datum\", SPHEROID[\"Popular Visualisation Sphere\", 6378137, 0, AUTHORITY[\"EPSG\",\"7059\"]], TOWGS84[0, 0, 0, 0, 0, 0, 0], AUTHORITY[\"EPSG\",\"6055\"]],PRIMEM[\"Greenwich\", 0, AUTHORITY[\"EPSG\", \"8901\"]], UNIT[\"degree\", 0.0174532925199433, AUTHORITY[\"EPSG\", \"9102\"]], AXIS[\"E\", EAST], AXIS[\"N\", NORTH], AUTHORITY[\"EPSG\",\"4055\"]], PROJECTION[\"Mercator\"], PARAMETER[\"False_Easting\", 0], PARAMETER[\"False_Northing\", 0], PARAMETER[\"Central_Meridian\", 0], PARAMETER[\"Latitude_of_origin\", 0], UNIT[\"metre\", 1, AUTHORITY[\"EPSG\", \"9001\"]], AXIS[\"East\", EAST], AXIS[\"North\", NORTH], AUTHORITY[\"EPSG\",\"3857\"]]");
            ICoordinateTransformation ct = ctf.CreateFromCoordinateSystems(epsg4326, epsg3857);
            foreach (DataRow row in ds.Tables[0].Rows)
            {
                if (row["X"] == DBNull.Value || row["Y"] == DBNull.Value) continue;
                double[] coords = new[] { Convert.ToDouble(row["X"]), Convert.ToDouble(row["Y"])};
                coords = ct.MathTransform.Transform(coords);
                row["X"] = coords[0];
                row["Y"] = coords[1];
            }

#else
            var epsg4326 = DotSpatial.Projections.KnownCoordinateSystems.Geographic.World.WGS1984;
            var epsg3857 = DotSpatial.Projections.ProjectionInfo.FromEsriString("PROJCS[\"Popular Visualisation CRS / Mercator\", GEOGCS[\"Popular Visualisation CRS\", DATUM[\"Popular Visualisation Datum\", SPHEROID[\"Popular Visualisation Sphere\", 6378137, 0, AUTHORITY[\"EPSG\",\"7059\"]], TOWGS84[0, 0, 0, 0, 0, 0, 0], AUTHORITY[\"EPSG\",\"6055\"]],PRIMEM[\"Greenwich\", 0, AUTHORITY[\"EPSG\", \"8901\"]], UNIT[\"degree\", 0.0174532925199433, AUTHORITY[\"EPSG\", \"9102\"]], AXIS[\"E\", EAST], AXIS[\"N\", NORTH], AUTHORITY[\"EPSG\",\"4055\"]], PROJECTION[\"Mercator\"], PARAMETER[\"False_Easting\", 0], PARAMETER[\"False_Northing\", 0], PARAMETER[\"Central_Meridian\", 0], PARAMETER[\"Latitude_of_origin\", 0], UNIT[\"metre\", 1, AUTHORITY[\"EPSG\", \"9001\"]], AXIS[\"East\", EAST], AXIS[\"North\", NORTH], AUTHORITY[\"EPSG\",\"3857\"]]");
            foreach (System.Data.DataRow row in ds.Tables[0].Rows)
            {
                if (row["X"] == DBNull.Value || row["Y"] == DBNull.Value) continue;
                var coords = new[] { Convert.ToDouble(row["X"]), Convert.ToDouble(row["Y"])};
                DotSpatial.Projections.Reproject.ReprojectPoints(coords, null, epsg4326, epsg3857, 0, 1);
                row["X"] = coords[0];
                row["Y"] = coords[1];
            }

#endif
            //Add Rotation Column
            ds.Tables[0].Columns.Add("Rotation", typeof (float));
            foreach (DataRow row in ds.Tables[0].Rows)
                row["Rotation"] = -angle;

            //Set up provider
            DataTablePoint xlsProvider = new DataTablePoint(ds.Tables[0], "OID", "X", "Y");
            VectorLayer xlsLayer = new VectorLayer("XLS", xlsProvider)
                               {Style = {Symbol = VectorStyle.DefaultSymbol}};

            //Add layer to map
            map.Layers.Add(xlsLayer);
            LabelLayer xlsLabelLayer = new LabelLayer("XLSLabel")
                                    {
                                        DataSource = xlsProvider,
                                        LabelColumn = "Name",
                                        PriorityColumn = "Population",
                                        Style =
                                            {
                                                CollisionBuffer = new SizeF(2f, 2f),
                                                CollisionDetection = true
                                            },
                                        LabelFilter =
                                            LabelCollisionDetection.ThoroughCollisionDetection
                                    };
            map.Layers.Add(xlsLabelLayer);

            map.ZoomToBox(tileLayer.Envelope);

            return map;
        }

        [Obsolete("Web service no longer available")]
        private static Map InitializeMapOsmWithVariableLayerCollection(float angle)
        {
            Map map = new Map();

            TileAsyncLayer tileLayer = new TileAsyncLayer(new OsmTileSource(), "TileLayer - OSM with VLC");
            map.BackgroundLayer.Add(tileLayer);

            VectorLayer vl = new VectorLayer("Vilnius Transport Data - Bus", 
                new VilniusTransportData(VilniusTransportData.TransportType.Bus));
            PublicTransportTheme pttBus = new PublicTransportTheme(Brushes.DarkGreen);
            vl.Theme = new CustomTheme(pttBus.GetStyle);
            vl.CoordinateTransformation = GetCoordinateTransformation();
            map.VariableLayers.Add(vl);
            vl = new VectorLayer("Vilnius Transport Data - Trolley", 
                new VilniusTransportData(VilniusTransportData.TransportType.TrolleyBus));
            PublicTransportTheme pttTrolley = new PublicTransportTheme(Brushes.Red);
            vl.Theme = new CustomTheme(pttTrolley.GetStyle);
            vl.CoordinateTransformation = GetCoordinateTransformation();
            map.VariableLayers.Add(vl);
            VariableLayerCollection.Interval = 5000;

            map.ZoomToBox(vl.Envelope);

            return map;
        }

#if !DotSpatialProjections

        private static ICoordinateTransformation GetCoordinateTransformation()
        {

            //The SRS for this datasource is EPSG:4326, therefore we need to transfrom it to OSM projection
            CoordinateTransformationFactory ctf = new CoordinateTransformationFactory();
            CoordinateSystemFactory cf = new CoordinateSystemFactory();
            ICoordinateSystem epsg4326 = cf.CreateFromWkt("GEOGCS[\"WGS 84\",DATUM[\"WGS_1984\",SPHEROID[\"WGS 84\",6378137,298.257223563,AUTHORITY[\"EPSG\",\"7030\"]],AUTHORITY[\"EPSG\",\"6326\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.01745329251994328,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4326\"]]");
            ICoordinateSystem epsg3857 = cf.CreateFromWkt("PROJCS[\"Popular Visualisation CRS / Mercator\", GEOGCS[\"Popular Visualisation CRS\", DATUM[\"Popular Visualisation Datum\", SPHEROID[\"Popular Visualisation Sphere\", 6378137, 0, AUTHORITY[\"EPSG\",\"7059\"]], TOWGS84[0, 0, 0, 0, 0, 0, 0], AUTHORITY[\"EPSG\",\"6055\"]],PRIMEM[\"Greenwich\", 0, AUTHORITY[\"EPSG\", \"8901\"]], UNIT[\"degree\", 0.0174532925199433, AUTHORITY[\"EPSG\", \"9102\"]], AXIS[\"E\", EAST], AXIS[\"N\", NORTH], AUTHORITY[\"EPSG\",\"4055\"]], PROJECTION[\"Mercator\"], PARAMETER[\"False_Easting\", 0], PARAMETER[\"False_Northing\", 0], PARAMETER[\"Central_Meridian\", 0], PARAMETER[\"Latitude_of_origin\", 0], UNIT[\"metre\", 1, AUTHORITY[\"EPSG\", \"9001\"]], AXIS[\"East\", EAST], AXIS[\"North\", NORTH], AUTHORITY[\"EPSG\",\"3857\"]]");
            return ctf.CreateFromCoordinateSystems(epsg4326, epsg3857);
#else
        private static DotSpatial.Projections.ICoordinateTransformation GetCoordinateTransformation()
        {
            var epsg4326 = DotSpatial.Projections.KnownCoordinateSystems.Geographic.World.WGS1984;
            var epsg3857 = DotSpatial.Projections.ProjectionInfo.FromEsriString("PROJCS[\"Popular Visualisation CRS / Mercator\", GEOGCS[\"Popular Visualisation CRS\", DATUM[\"Popular Visualisation Datum\", SPHEROID[\"Popular Visualisation Sphere\", 6378137, 0, AUTHORITY[\"EPSG\",\"7059\"]], TOWGS84[0, 0, 0, 0, 0, 0, 0], AUTHORITY[\"EPSG\",\"6055\"]],PRIMEM[\"Greenwich\", 0, AUTHORITY[\"EPSG\", \"8901\"]], UNIT[\"degree\", 0.0174532925199433, AUTHORITY[\"EPSG\", \"9102\"]], AXIS[\"E\", EAST], AXIS[\"N\", NORTH], AUTHORITY[\"EPSG\",\"4055\"]], PROJECTION[\"Mercator\"], PARAMETER[\"False_Easting\", 0], PARAMETER[\"False_Northing\", 0], PARAMETER[\"Central_Meridian\", 0], PARAMETER[\"Latitude_of_origin\", 0], UNIT[\"metre\", 1, AUTHORITY[\"EPSG\", \"9001\"]], AXIS[\"East\", EAST], AXIS[\"North\", NORTH], AUTHORITY[\"EPSG\",\"3857\"]]");
            return new DotSpatial.Projections.CoordinateTransformation { Source = epsg4326, Target = epsg3857 };
#endif
        }

        private static Map InitializeMapBing(BingMapType mt)
        {
            Map map = new Map();

            TileLayer tileLayer = new TileLayer(new BingTileSource(BingRequest.UrlBingStaging, "", mt), "TileLayer - Bing " + mt);
            map.BackgroundLayer.Add(tileLayer);
            map.ZoomToBox(tileLayer.Envelope);
            return map;
        }


/*
        private static SharpMap.Map InitializeMapGoogle(BruTile.Web.GoogleMapType mt)
        {
            var map = new SharpMap.Map();

            BruTile.Web.GoogleRequest req;
            BruTile.ITileSource tileSource;
            SharpMap.Layers.TileLayer tileLayer;

            if (mt == (BruTile.Web.GoogleMapType.GoogleSatellite | BruTile.Web.GoogleMapType.GoogleLabels))
            {
                req = new BruTile.Web.GoogleRequest(BruTile.Web.GoogleMapType.GoogleSatellite);
                tileSource = new BruTile.Web.GoogleTileSource(req);
                tileLayer = new SharpMap.Layers.TileLayer(tileSource, "TileLayer - " + BruTile.Web.GoogleMapType.GoogleSatellite);
                map.Layers.Add(tileLayer);
                req = new BruTile.Web.GoogleRequest(BruTile.Web.GoogleMapType.GoogleLabels);
                tileSource = new BruTile.Web.GoogleTileSource(req);
                mt = BruTile.Web.GoogleMapType.GoogleLabels;
            }
            else
            {
                req = new BruTile.Web.GoogleRequest(mt);
                tileSource = new BruTile.Web.GoogleTileSource(req);
            }

            tileLayer = new SharpMap.Layers.TileLayer(tileSource, "TileLayer - " + mt);
            map.Layers.Add(tileLayer);
            map.ZoomToBox(tileLayer.Envelope);
            return map;
        }
        */

        private class PublicTransportTheme
        {

            private static readonly PointF[] ArrowPoints =
                new[]
                    {
                        new PointF(0, 35), new PointF(6, 0),
                        new PointF(12, 35), new PointF(0, 35)
                    };
            private static Image ColoredArrow(Brush c)
            {
                Bitmap bmp = new Bitmap(13, 36);

                using (IGraphics g = Graphics.FromImage(bmp).G())
                {
                    g.Clear(Color.Wheat);
                    g.FillPolygon(c, ArrowPoints);
                    g.DrawPolygon(Pens.Black, ArrowPoints);
                }

                bmp.MakeTransparent(Color.Wheat);
                return bmp;
            }

            readonly Brush _brush;

            public PublicTransportTheme(Brush brush)
            {
                _brush = brush;
            }
            public IStyle GetStyle(IFeature fdr)
            {
                VectorStyle retval = new VectorStyle();

                if (fdr.Attributes["Bearing"] == DBNull.Value)
                {
                    Bitmap bmp = new Bitmap(36, 36);
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.Clear(Color.Wheat);
                        g.FillEllipse(Brushes.Green, 0, 0, 36, 36);
                        g.DrawEllipse(new Pen(Brushes.Yellow, 3), 4, 4, 28, 28);
                        g.DrawString("H", new Font("Arial", 18, FontStyle.Bold),
                                     Brushes.Yellow,
                                     new RectangleF(2, 2, 34, 34),
                                     new StringFormat
                                         {
                                             Alignment = StringAlignment.Center,
                                             LineAlignment = StringAlignment.Center
                                         });
                    }
                    bmp.MakeTransparent(Color.Wheat);
                    retval.Symbol = bmp;
                }
                else
                {
                    retval.Symbol = ColoredArrow(_brush);
                    float rot =  Convert.ToSingle(fdr.Attributes["Bearing"]);
                    retval.SymbolRotation = rot % 360f;
                }
                return retval;

            }
        }

        /// <summary>
        /// This class is directly derived from GreatMaps
        /// http://gmaps.codeplex.com
        /// </summary>
        private class VilniusTransportData : FeatureProvider
        {

            private bool _isActive;
            private readonly Timer _reQuery = new Timer(5000);

            public enum TransportType
            {
                Bus, TrolleyBus,
            }

            private static FeatureDataTable VehicleDataTable()
            {
                FeatureDataTable dt = new FeatureDataTable { TableName = "VilniusTransportData" };
                DataColumnCollection dcc = dt.Columns;
                dcc.AddRange(new[]
                                  {
                                      new DataColumn("Id", typeof(int)), 
                                      //new System.Data.DataColumn("Lat", typeof(double)), 
                                      //new System.Data.DataColumn("Lng", typeof(double)), 
                                      new DataColumn("Line", typeof(string)), 
                                      new DataColumn("LastStop", typeof(string)), 
                                      new DataColumn("TrackType", typeof(string)), 
                                      new DataColumn("AreaName", typeof(string)), 
                                      new DataColumn("StreetName", typeof(string)), 
                                      new DataColumn("Time", typeof(string)), 
                                      new DataColumn("Bearing", typeof(double)) 
                                  });
                return dt;
            }

            private readonly TransportType _transportType;

            public VilniusTransportData(TransportType transportType)
                :base(VehicleDataTable())
            {
                _transportType = transportType;
                _reQuery.Elapsed += HandleTimerElapsed;
                _reQuery.Start();
                GetVilniusTransportData(String.Empty);
            }

            private void HandleTimerElapsed(object sender, ElapsedEventArgs e)
            {
                GetVilniusTransportData(string.Empty);
            }

            /// <summary>
            /// timeout for map connections
            /// </summary>
            private const int Timeout = 30*1000;

            private readonly IGeometryFactory _factory =
                GeometryServiceProvider.Instance.CreateGeometryFactory(4326);

            /// <summary>
            /// gets realtime data from public transport in city vilnius of lithuania
            /// </summary>
            private void GetVilniusTransportData(string line)
            {
                if (_isActive) return;
                _isActive = true;

                //List<FeatureDataRow> newFeatures = new List<FeatureDataRow>();
                FeatureDataTable fdt = VehicleDataTable();

                string url = "http://www.troleibusai.lt/puslapiai/services/vehiclestate.php?type=";

                switch (_transportType)
                {
                    case TransportType.Bus:
                        {
                            url += "bus";
                        }
                        break;

                    case TransportType.TrolleyBus:
                        {
                            url += "trolley";
                        }
                        break;
                }

                if (!string.IsNullOrEmpty(line))
                {
                    url += "&line=" + line;
                }

                url += "&app=SharpMap.WinFormSamples";

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

                request.Timeout = Timeout;
                request.ReadWriteTimeout = request.Timeout;
                request.Accept = "*/*";
                request.KeepAlive = false;

                string xml;

                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    if (response == null)
                        return;

                    using (Stream responseStream = response.GetResponseStream())
                    {
                        if (responseStream == null)
                            return;
                        using (StreamReader read = new StreamReader(responseStream))
                        {
                            xml = read.ReadToEnd();
                        }
                    }
                }

                XmlDocument doc = new XmlDocument();
                {
                    doc.LoadXml(xml);

                    XmlNodeList devices = doc.GetElementsByTagName("Device");
                    foreach (XmlNode dev in devices)
                    {
                        if (dev.Attributes == null) continue;

                        double? lat = null, lng = null;
                        FeatureDataRow dr = fdt.NewRow();
                        dr["Id"] = int.Parse(dev.Attributes["ID"].InnerText);
                        foreach (XmlElement elem in dev.ChildNodes)
                        {
                            // Debug.WriteLine(d.Id + "->" + elem.Name + ": " + elem.InnerText);

                            switch (elem.Name)
                            {
                                case "Lat":
                                    lat = double.Parse(elem.InnerText, CultureInfo.InvariantCulture);
                                    break;

                                case "Lng":
                                    lng = double.Parse(elem.InnerText, CultureInfo.InvariantCulture);
                                    break;

                                case "Bearing":
                                    if (!string.IsNullOrEmpty(elem.InnerText))
                                        dr["Bearing"] = double.Parse(elem.InnerText, CultureInfo.InvariantCulture);
                                    break;

                                case "LineNum":
                                    dr["Line"] = elem.InnerText;
                                    break;

                                case "AreaName":
                                    dr["AreaName"] = elem.InnerText;
                                    break;

                                case "StreetName":
                                    dr["StreetName"] = elem.InnerText;
                                    break;

                                case "TrackType":
                                    dr["TrackType"] = elem.InnerText;
                                    break;

                                case "LastStop":
                                    dr["LastStop"] = elem.InnerText;
                                    break;

                                case "Time":
                                    dr["Time"] = elem.InnerText;
                                    break;
                            }
                        }

                        if (lat.HasValue && lng.HasValue)
                        {
                            dr.Geometry = _factory.CreatePoint(new Coordinate(lng.Value, lat.Value));
                            fdt.Rows.Add(dr);
                        }
                    }
                }

                Features.Clear();
                FeatureDataTable features = (FeatureDataTable) Features;
                foreach (FeatureDataRow featureDataRow in fdt.Rows)
                {
                    FeatureDataRow fdr = features.NewRow();
                    fdr.ItemArray = featureDataRow.ItemArray;
                    fdr.Geometry = featureDataRow.Geometry;
                    features.AddRow(fdr);
                }
                features.AcceptChanges();

                _isActive = false;
            }
        }

    }
}
