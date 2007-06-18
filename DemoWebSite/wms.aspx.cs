using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;

public partial class WmsPage : System.Web.UI.Page
{
	protected void Page_Load(object sender, EventArgs e)
	{
		//Get the path of this page
		string url = (Request.Url.Query.Length>0?Request.Url.AbsoluteUri.Replace(Request.Url.Query,""):Request.Url.AbsoluteUri);
		SharpMap.Web.Wms.Capabilities.WmsServiceDescription description =
			new SharpMap.Web.Wms.Capabilities.WmsServiceDescription("Acme Corp. Map Server", url);

		// The following service descriptions below are not strictly required by the WMS specification.

		// Narrative description and keywords providing additional information 
		description.Abstract = "Map Server maintained by Acme Corporation. Contact: webmaster@wmt.acme.com. High-quality maps showing roadrunner nests and possible ambush locations.";
		description.Keywords.Add("bird");
		description.Keywords.Add("roadrunner");
		description.Keywords.Add("ambush");

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
		SharpMap.Map myMap = MapHelper.InitializeMap(new System.Drawing.Size(1,1));

		//Parse the request and create a response
		SharpMap.Web.Wms.WmsServer.ParseQueryString(myMap,description);
	}
}
