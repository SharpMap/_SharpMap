using SharpMap;
using SharpMap.Layers;
using Point=SharpMap.Geometries.Point;
using SharpMap.Styles;

namespace WinFormSamples.Samples
{
    public static class WmsSample
    {
        public static Map InitializeMap()
        {
            string wmsUrl = "http://www2.demis.nl/mapserver/request.asp";

            Map map = new Map();

            Layer layWms = new Layer("WmsLayer");
            WmsProvider wmsProvider = new WmsProvider(wmsUrl);
            wmsProvider.SpatialReferenceSystem = "EPSG:4326";

            wmsProvider.AddLayer("Bathymetry");
            wmsProvider.AddLayer("Topography");
            wmsProvider.AddLayer("Hillshading");

            wmsProvider.SetImageFormat(wmsProvider.OutputFormats[0]);
            wmsProvider.ContinueOnError = true;
            //Skip rendering the WMS Map if the server couldn't be requested (if set to false such an event would crash the app)
            wmsProvider.TimeOut = 5000; //Set timeout to 5 seconds
            layWms.DataSource = wmsProvider;
            map.Layers.Add(layWms);
            layWms.SRID = 4326;
            
            //limit the zoom to 360 degrees width
            map.MaximumZoom = 360;
            map.BackColor = Color.DarkBlue;

            return map;
        }
    }
}