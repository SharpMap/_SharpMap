namespace SharpMap.Web.Wms.Handlers
{
    using System.Xml;

    internal class GetCapabilities : AbstractHandler
    {
        public GetCapabilities(HandlerParams @params) : base(@params) { }

        public override void Handle()
        {
            string service = this.context.Request.Params["SERVICE"];

            if (service == null)
            {
                WmsException.ThrowWmsException("Required parameter SERVICE not specified");
                return;
            }

            if (!this.Check("WMS", service))
            {
                WmsException.ThrowWmsException("Invalid service for GetCapabilities Request. Service parameter must be 'WMS'");
                return;
            }

            XmlDocument capabilities = Capabilities.GetCapabilities(this.map, this.description);
            this.context.Response.Clear();
            this.context.Response.ContentType = "text/xml";
            XmlWriter writer = XmlWriter.Create(this.context.Response.OutputStream);
            capabilities.WriteTo(writer);
            writer.Close();
            this.context.Response.End();
        }
    }
}
