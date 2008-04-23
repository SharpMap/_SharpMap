<%@ WebHandler Language="C#" Class="wms" %>

using System;
using System.Web;
using SharpMap.Renderers.ImageMap;
using SharpMap.Styles;
using SharpMap.Data;
using SharpMap.Layers;
using System.Xml;

public class wms : IHttpHandler
{

    public void ProcessRequest(HttpContext context)
    {
        //Get the path of this page
        string url = (context.Request.Url.Query.Length > 0 ?
            context.Request.Url.AbsoluteUri.Replace(context.Request.Url.Query, "") : context.Request.Url.AbsoluteUri);
        SharpMap.Web.Wms.Capabilities.WmsServiceDescription description =
            new SharpMap.Web.Wms.Capabilities.WmsServiceDescription("Acme Corp. Map Server", url);

        // The following service descriptions below are not strictly required by the WMS specification.

        // Narrative description and keywords providing additional information 
        description.Abstract = "Map Server maintained by Acme Corporation. Contact: webmaster@wmt.acme.com. High-quality maps showing roadrunner nests and possible ambush locations.";
        description.Keywords = new string[3];
        description.Keywords[0] = "bird";
        description.Keywords[1] = "roadrunner";
        description.Keywords[2] = "ambush";

        //Contact information 
        description.ContactInformation.PersonPrimary.Person = "John Doe";
        description.ContactInformation.PersonPrimary.Organisation = "Acme Inc";
        description.ContactInformation.Address.AddressType = "postal";
        description.ContactInformation.Address.Country = "Neverland";
        description.ContactInformation.VoiceTelephone = "1-800-WE DO MAPS";
        //Impose WMS constraints
        description.MaxWidth = 1000; //Set image request size width
        description.MaxHeight = 500; //Set image request size height


        //Call method that sets up the map
        //We just add a dummy-size, since the wms requests will set the image-size
        SharpMap.Map myMap = MapHelper.InitializeMap(new System.Drawing.Size(700, 500));

        ////Parse the request and create a response
        SharpMap.Web.Wms.WmsServer.ParseQueryString(myMap, description);


        /*
         ///experimental bit -  ignore..
 
                ImageMapRenderer imr = new ImageMapRenderer();
                imr.ImageMapStyle = new ImageMapStyle(0, 1000, true);

                imr.ImageMapStyle.Line.BufferWidth = 3;
                imr.ImageMapStyle.Line.Enabled = true;
                imr.ImageMapStyle.Line.MinVisible = 0;
                imr.ImageMapStyle.Line.MaxVisible = 1000;

                imr.ImageMapStyle.Point.Radius = 50;
                imr.ImageMapStyle.Point.MinVisible = 0;
                imr.ImageMapStyle.Point.MaxVisible = 1000;

                imr.ImageMapStyle.Polygon.Enabled = true;
                imr.ImageMapStyle.Polygon.MinVisible = 0;
                imr.ImageMapStyle.Polygon.MaxVisible = 1000;



                imr.AttributeProviders.Add(
                    "id",
                    new Func<ILayer, FeatureDataRow, string>(
                        delegate(ILayer o, FeatureDataRow a)
                        {
                            return (string)a[0];
                        }
                    ));

                imr.AttributeProviders.Add("layer",
                    new Func<ILayer, FeatureDataRow, string>(
                        delegate(ILayer o, FeatureDataRow a)
                        {
                            return o.LayerName;
                        }
                    ));

                imr.AttributeProviders.Add("geomType",
                    new Func<ILayer, FeatureDataRow, string>(
                        delegate(ILayer o, FeatureDataRow a)
                        {
                            return a.Geometry.GetType().Name;
                        }
                    ));

                myMap.ZoomToExtents();

                XmlDocument doc = myMap.Render(imr);

                context.Response.Clear();
                context.Response.ContentType = "text/xml";
                context.Response.Write(doc.DocumentElement.OuterXml);
                context.Response.End();
        */
    }

    public bool IsReusable
    {
        get
        {
            return false;
        }
    }

}