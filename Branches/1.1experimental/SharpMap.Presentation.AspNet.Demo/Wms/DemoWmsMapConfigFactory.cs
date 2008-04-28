/*
 *  The attached / following is part of SharpMap.Presentation.AspNet
 *  SharpMap.Presentation.AspNet is free software © 2008 Newgrove Consultants Limited, 
 *  www.newgrove.com; you can redistribute it and/or modify it under the terms 
 *  of the current GNU Lesser General Public License (LGPL) as published by and 
 *  available from the Free Software Foundation, Inc., 
 *  59 Temple Place, Suite 330, Boston, MA 02111-1307 USA: http://fsf.org/    
 *  This program is distributed without any warranty; 
 *  without even the implied warranty of merchantability or fitness for purpose.  
 *  See the GNU Lesser General Public License for the full details. 
 *  
 *  Author: John Diss 2008
 * 
 */
using SharpMap.Presentation.AspNet.WmsServer;
using SharpMap.Web.Wms;

namespace SharpMap.Presentation.AspNet.Demo.Wms
{
    public class DemoWmsMapConfigFactory
        : WmsConfigFactoryBase
    {

        private readonly static Capabilities.WmsServiceDescription _description = GetDescription();
        private static Capabilities.WmsServiceDescription GetDescription()
        {
            Capabilities.WmsServiceDescription description
                = new Capabilities.WmsServiceDescription("Acme Corp. Map Server", "http://roadrunner.acmecorp.com/ambush");

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

            return description;

        }


        public override Capabilities.WmsServiceDescription Description
        {
            get { return _description; }
        }
    }
}
