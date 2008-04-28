<%@ WebHandler Language="C#" Class="ImageMapHandler" %>

using System;
using System.Web;
using SharpMap.Renderers.ImageMap;
using SharpMap.Styles;
using SharpMap.Data;
using SharpMap.Layers;
using System.Xml;


/// <summary>
/// The maphandler class takes a set of GET or POST parameters and returns a map as PNG (this reminds in many ways of the way a WMS server work).
/// Required parameters are: WIDTH, HEIGHT, ZOOM, X, Y, MAP
/// </summary>
public class ImageMapHandler : IHttpHandler
{

    internal static System.Globalization.NumberFormatInfo numberFormat_EnUS = new System.Globalization.CultureInfo("en-US", false).NumberFormat;

    public void ProcessRequest(HttpContext context)
    {
        int Width = 0;
        int Height = 0;
        double centerX = 0;
        double centerY = 0;
        double Zoom = 0;

        //Parse request parameters
        if (!int.TryParse(context.Request.Params["WIDTH"], out Width))
            throw (new ArgumentException("Invalid parameter"));
        if (!int.TryParse(context.Request.Params["HEIGHT"], out Height))
            throw (new ArgumentException("Invalid parameter"));
        if (!double.TryParse(context.Request.Params["ZOOM"], System.Globalization.NumberStyles.Float, numberFormat_EnUS, out Zoom))
            throw (new ArgumentException("Invalid parameter"));
        if (!double.TryParse(context.Request.Params["X"], System.Globalization.NumberStyles.Float, numberFormat_EnUS, out centerX))
            throw (new ArgumentException("Invalid parameter"));
        if (!double.TryParse(context.Request.Params["Y"], System.Globalization.NumberStyles.Float, numberFormat_EnUS, out centerY))
            throw (new ArgumentException("Invalid parameter"));
        if (context.Request.Params["MAP"] == null)
            throw (new ArgumentException("Invalid parameter"));
        //Params OK

        SharpMap.Map map = InitializeMap(context.Request.Params["MAP"], new System.Drawing.Size(Width, Height));
        if (map == null)
            throw (new ArgumentException("Invalid map"));

        //Set visible map extents
        map.Center = new SharpMap.Geometries.Point(centerX, centerY);
        map.Zoom = Zoom;


        ImageMapRenderer imr = new ImageMapRenderer();
        imr.ImageMapStyle = new ImageMapStyle(0, 1000, true);

        imr.ImageMapStyle.Line.BufferWidth = 3;
        imr.ImageMapStyle.Line.Enabled = true;
        imr.ImageMapStyle.Line.MaxVisible = 1000;
        imr.ImageMapStyle.Line.MinVisible = 0;

        imr.ImageMapStyle.Point.Radius = 50;
        imr.ImageMapStyle.Point.MinVisible = 0;
        imr.ImageMapStyle.Point.MaxVisible = 1000;

        imr.ImageMapStyle.Polygon.Enabled = true;
        imr.ImageMapStyle.Polygon.MaxVisible = 1000;
        imr.ImageMapStyle.Polygon.MinVisible = 0;



        imr.AttributeProviders.Add(
            "id",
            new Func<ILayer, FeatureDataRow, string>(
                delegate(ILayer o, FeatureDataRow a)
                {
                    return (string)a[0];
                }
                    ));

        string mimeType;
        XmlDocument doc = imr.Render(map, out mimeType);

        context.Response.Clear();
        context.Response.ContentType = mimeType;
        context.Response.Write(doc.DocumentElement.OuterXml);

    }

    private SharpMap.Map InitializeMap(string MapID, System.Drawing.Size size)
    {
        //Set up the map. We use the method in the App_Code folder for initializing the map
        switch (MapID)
        {
            //Our simple world map was requested 
            case "SimpleWorld":
                return MapHelper.InitializeMap(size);
            //Gradient theme layer requested. Based on simplemap
            case "Gradient":
                return MapHelper.InitializeGradientMap(size);
            case "WmsClient":
                return MapHelper.InitializeWmsMap(size);
            default:
                throw new ArgumentException("Invalid map '" + MapID + "' requested"); ;
        }
    }

    public bool IsReusable
    {
        get
        {
            return false;
        }
    }

}