// Copyright 2005, 2006 - Morten Nielsen (www.iter.dk)
//
// This file is part of SharpMap.
// SharpMap is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
// 
// SharpMap is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.

// You should have received a copy of the GNU Lesser General Public License
// along with SharpMap; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA 

namespace SharpMap.Web.Wms
{
    using System;
    using System.Web;
    using Data;
    using Geometries;
    using Handlers;

    /// <summary>
    /// This is a helper class designed to make it easy to create a WMS Service
    /// </summary>
    public static class WmsServer
    {
        public delegate FeatureDataTable InterSectDelegate(FeatureDataTable featureDataTable, BoundingBox box);

        private static InterSectDelegate _intersectDelegate;

        private static int _pixelSensitivity;

        /// <summary>
        /// Generates a WMS 1.3.0 compliant response based on a <see cref="SharpMap.Map"/> and the current HttpRequest.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The Web Map Server implementation in SharpMap requires v1.3.0 compatible clients,
        /// and support the basic operations "GetCapabilities" and "GetMap"
        /// as required by the WMS v1.3.0 specification. SharpMap does not support the optional
        /// GetFeatureInfo operation for querying.
        /// </para>
        /// <example>
        /// Creating a WMS server in ASP.NET is very simple using the classes in the SharpMap.Web.Wms namespace.
        /// <code lang="C#">
        /// void page_load(object o, EventArgs e)
        /// {
        ///		//Get the path of this page
        ///		string url = (Request.Url.Query.Length>0?Request.Url.AbsoluteUri.Replace(Request.Url.Query,""):Request.Url.AbsoluteUri);
        ///		SharpMap.Web.Wms.Capabilities.WmsServiceDescription description =
        ///			new SharpMap.Web.Wms.Capabilities.WmsServiceDescription("Acme Corp. Map Server", url);
        ///		
        ///		// The following service descriptions below are not strictly required by the WMS specification.
        ///		
        ///		// Narrative description and keywords providing additional information 
        ///		description.Abstract = "Map Server maintained by Acme Corporation. Contact: webmaster@wmt.acme.com. High-quality maps showing roadrunner nests and possible ambush locations.";
        ///		description.Keywords.Add("bird");
        ///		description.Keywords.Add("roadrunner");
        ///		description.Keywords.Add("ambush");
        ///		
        ///		//Contact information 
        ///		description.ContactInformation.PersonPrimary.Person = "John Doe";
        ///		description.ContactInformation.PersonPrimary.Organisation = "Acme Inc";
        ///		description.ContactInformation.Address.AddressType = "postal";
        ///		description.ContactInformation.Address.Country = "Neverland";
        ///		description.ContactInformation.VoiceTelephone = "1-800-WE DO MAPS";
        ///		//Impose WMS constraints
        ///		description.MaxWidth = 1000; //Set image request size width
        ///		description.MaxHeight = 500; //Set image request size height
        ///		
        ///		//Call method that sets up the map
        ///		//We just add a dummy-size, since the wms requests will set the image-size
        ///		SharpMap.Map myMap = MapHelper.InitializeMap(new System.Drawing.Size(1,1));
        ///		
        ///		//Parse the request and create a response
        ///		SharpMap.Web.Wms.WmsServer.ParseQueryString(myMap,description);
        /// }
        /// </code>
        /// </example>
        /// </remarks>
        /// <param name="map">Map to serve on WMS</param>
        /// <param name="description">Description of map service</param>
        public static void ParseQueryString(Map map, Capabilities.WmsServiceDescription description)
        {
            ParseQueryString(map, description, 1, null);
        }

        /// <summary>
        /// Generates a WMS 1.3.0 compliant response based on a <see cref="SharpMap.Map"/> and the current HttpRequest.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The Web Map Server implementation in SharpMap requires v1.3.0 compatible clients,
        /// and support the basic operations "GetCapabilities" and "GetMap"
        /// as required by the WMS v1.3.0 specification. SharpMap does not support the optional
        /// GetFeatureInfo operation for querying.
        /// </para>
        /// <example>
        /// Creating a WMS server in ASP.NET is very simple using the classes in the SharpMap.Web.Wms namespace.
        /// <code lang="C#">
        /// void page_load(object o, EventArgs e)
        /// {
        ///		//Get the path of this page
        ///		string url = (Request.Url.Query.Length>0?Request.Url.AbsoluteUri.Replace(Request.Url.Query,""):Request.Url.AbsoluteUri);
        ///		SharpMap.Web.Wms.Capabilities.WmsServiceDescription description =
        ///			new SharpMap.Web.Wms.Capabilities.WmsServiceDescription("Acme Corp. Map Server", url);
        ///		
        ///		// The following service descriptions below are not strictly required by the WMS specification.
        ///		
        ///		// Narrative description and keywords providing additional information 
        ///		description.Abstract = "Map Server maintained by Acme Corporation. Contact: webmaster@wmt.acme.com. 
        ///     High-quality maps showing roadrunner nests and possible ambush locations.";
        ///		description.Keywords.Add("bird");
        ///		description.Keywords.Add("roadrunner");
        ///		description.Keywords.Add("ambush");
        ///		
        ///		//Contact information 
        ///		description.ContactInformation.PersonPrimary.Person = "John Doe";
        ///		description.ContactInformation.PersonPrimary.Organisation = "Acme Inc";
        ///		description.ContactInformation.Address.AddressType = "postal";
        ///		description.ContactInformation.Address.Country = "Neverland";
        ///		description.ContactInformation.VoiceTelephone = "1-800-WE DO MAPS";
        ///		//Impose WMS constraints
        ///		description.MaxWidth = 1000; //Set image request size width
        ///		description.MaxHeight = 500; //Set image request size height
        ///		
        ///		//Call method that sets up the map
        ///		//We just add a dummy-size, since the wms requests will set the image-size
        ///		SharpMap.Map myMap = MapHelper.InitializeMap(new System.Drawing.Size(1,1));
        ///		
        ///		//Parse the request and create a response
        ///		SharpMap.Web.Wms.WmsServer.ParseQueryString(myMap,description);
        /// }
        /// </code>
        /// </example>
        /// </remarks>
        /// <param name="map">Map to serve on WMS</param>
        /// <param name="description">Description of map service</param>
        ///<param name="pixelSensitivity">Tolerance for GetFeatureInfo requests</param>
        ///<param name="intersectDelegate">Delegate for Getfeatureinfo intersecting, when null, the WMS will default to ICanQueryLayer implementation</param>
        public static void ParseQueryString(Map map, Capabilities.WmsServiceDescription description, int pixelSensitivity, InterSectDelegate intersectDelegate)
        {
            _pixelSensitivity = pixelSensitivity;
            _intersectDelegate = intersectDelegate;

            if (map == null)
                throw (new ArgumentException("Map for WMS is null"));
            if (map.Layers.Count == 0)
                throw (new ArgumentException("Map doesn't contain any layers for WMS service"));

            if (HttpContext.Current == null)
                throw new ApplicationException("An attempt was made to access the WMS server outside a valid HttpContext");

            HttpContext context = HttpContext.Current;

            //IgnoreCase value should be set according to the VERSION parameter
            //v1.3.0 is case sensitive, but since it causes a lot of problems with several WMS clients, we ignore casing anyway.
            const bool ignorecase = true;

            //Collect parameters
            string request = context.Request.Params["REQUEST"];
            string version = context.Request.Params["VERSION"];

            //Check for required parameters
            //Request parameter is mandatory            
            if (request == null)
            {
                WmsException.ThrowWmsException("Required parameter REQUEST not specified");
                return;
            }

            //Check if version is supported            
            if (version != null)
            {
                if (String.Compare(version, "1.3.0", ignorecase) != 0)
                {
                    WmsException.ThrowWmsException("Only version 1.3.0 supported");
                    return;
                }
            }
            else
            {
                //Version is mandatory if REQUEST!=GetCapabilities. Check if this is a capabilities request, since VERSION is null
                if (String.Compare(request, "GetCapabilities", ignorecase) != 0)
                {
                    WmsException.ThrowWmsException("VERSION parameter not supplied");
                    return;
                }
            }

            HandlerParams @params = new HandlerParams(context, map, description, ignorecase);
            GetFeatureInfoParams infoParams = new GetFeatureInfoParams(_pixelSensitivity, _intersectDelegate);

            IWmsHandler handler = null;
            if (String.Compare(request, "GetCapabilities", ignorecase) == 0)
                handler = new GetCapabilities(@params);
            else if (String.Compare(request, "GetFeatureInfo", ignorecase) == 0)
                handler = new GetFeatureInfo(@params, infoParams);
            else if (String.Compare(request, "GetMap", ignorecase) == 0)
                handler = new GetMap(@params);

            if (handler == null)
            {
                WmsException.ThrowWmsException(
                    WmsException.WmsExceptionCode.OperationNotSupported, String.Format("Invalid request: {0}", request));
            }
            else handler.Handle();
        }
    }
}