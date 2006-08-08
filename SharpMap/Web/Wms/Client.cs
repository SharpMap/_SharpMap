using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace SharpMap.Web.Wms
{
	[Serializable]
	public class Client
	{
		private XmlNamespaceManager nsmgr;

		#region WMS Data structures
		public struct WmsServerLayer
		{
			public int ID;
			public int ParentID;
			public string Title;
			public string Name;
			public string Abstract;
			public bool Queryable;
			public string[] Keywords;
			public string[] Style;
			public string[] CRS;
			public WmsServerLayer[] ChildLayers;
			public SharpMap.Geometries.BoundingBox BoundingBox;
		}

		public struct WmsLayerStyle
		{
			public string Name;
			public string Title;
			public string Abstract;
			public WmsStyleLegend LegendUrl;
			public WmsOnlineResource StyleSheetUrl;
		}

		public struct WmsStyleLegend
		{
			public WmsOnlineResource OnlineResource;
			public System.Drawing.Size Size;
		}

		public struct WmsOnlineResource
		{
			public string Type;
			public string OnlineResource;
		}

		#endregion

		#region Properties

		private Capabilities.WmsServiceDescription _ServiceDescription;

		public Capabilities.WmsServiceDescription ServiceDescription
		{
			get { return _ServiceDescription; }
		}
		private string _WmsVersion;

		internal string WmsVersion
		{
			get { return _WmsVersion; }
		}

		private List<string> _GetMapOutputFormats;

		internal List<string> GetMapOutputFormats
		{
			get { return _GetMapOutputFormats; }
		}

		private string[] _ExceptionFormats;

		internal string[] ExceptionFormats
		{
			get { return _ExceptionFormats; }
		}

		private WmsOnlineResource[] _GetMapRequests;

		public WmsOnlineResource[] GetMapRequests
		{
			get { return _GetMapRequests; }
		}
	

		private WmsServerLayer _Layer;

		internal WmsServerLayer Layer
		{
			get { return _Layer; }
			set { _Layer = value; }
		}
		#endregion

		public Client(string url)
		{
			System.Text.StringBuilder strReq = new StringBuilder(url);
			if (!url.Contains("?"))
				strReq.Append("?");
			if (!strReq.ToString().EndsWith("&") && !strReq.ToString().EndsWith("?"))
				strReq.Append("&");
			if(!url.ToLower().Contains("service=wms"))
				strReq.AppendFormat("SERVICE=WMS&");
			if (!url.ToLower().Contains("request=getcapabilities"))
				strReq.AppendFormat("REQUEST=GetCapabilities&");		

			XmlDocument xml = GetRemoteXml(strReq.ToString());
			ParseCapabilities(xml);
		}


		/// <summary>
		/// Downloads servicedescription from WMS service
		/// </summary>
		/// <returns>XmlDocument from Url. Null if Url is empty or inproper XmlDocument</returns>
		private XmlDocument GetRemoteXml(string Url)
		{
			try
			{
				System.Net.WebRequest myRequest = System.Net.WebRequest.Create(Url);
				System.Net.WebResponse myResponse = myRequest.GetResponse();
				System.IO.Stream stream = myResponse.GetResponseStream();
				XmlTextReader r = new XmlTextReader(Url, stream);
				XmlDocument doc = new XmlDocument();
				doc.Load(r);
				stream.Close();
				nsmgr = new XmlNamespaceManager(doc.NameTable);
				return doc;
			}
			catch (System.Exception ex)
			{
				throw new ApplicationException("Could now download capabilities", ex);
			}
		}


		/// <summary>
		/// Parses a servicedescription and stores the data in the ServiceDescription property
		/// </summary>
		/// <param name="doc">XmlDocument containing a valid Service Description</param>
		private void ParseCapabilities(XmlDocument doc)
		{
			if (doc.DocumentElement.Attributes["version"] != null)
			{
				_WmsVersion = doc.DocumentElement.Attributes["version"].Value;
				if (_WmsVersion != "1.0.0" && _WmsVersion != "1.1.0" && _WmsVersion != "1.1.1" && _WmsVersion != "1.3.0")
					throw new ApplicationException("WMS Version " + _WmsVersion + " not supported");

				nsmgr.AddNamespace(String.Empty, "http://www.opengis.net/wms");
				if (_WmsVersion == "1.3.0")
				{
					nsmgr.AddNamespace("sm", "http://www.opengis.net/wms");
				}
				else
					nsmgr.AddNamespace("sm", "");
				nsmgr.AddNamespace("xlink", "http://www.w3.org/1999/xlink");
				nsmgr.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance");
			}
			else
				throw (new ApplicationException("No service version number found!"));

			XmlNode xnService = doc.DocumentElement.SelectSingleNode("sm:Service", nsmgr);
			XmlNode xnCapability = doc.DocumentElement.SelectSingleNode("sm:Capability", nsmgr);
			if (xnService != null)
				ParseServiceDescription(xnService);
			else
				throw (new ApplicationException("No service tag found!"));



			if (xnCapability != null)
				ParseCapability(xnCapability);
			else
				throw (new ApplicationException("No capability tag found!"));
		}


		//Udtræk Name, Title, OnlineResource
		//Evt. udtræk Abstract, Keywordlist, Contact Information, Fees, Access Constraints
		private void ParseServiceDescription(XmlNode xnlServiceDescription)
		{
			XmlNode node = xnlServiceDescription.SelectSingleNode("sm:Title", nsmgr);
			_ServiceDescription.Title = (node != null ? node.InnerText : null);
			node = xnlServiceDescription.SelectSingleNode("sm:OnlineResource/@xlink:href", nsmgr);
			_ServiceDescription.OnlineResource = (node != null ? node.InnerText : null);
			node = xnlServiceDescription.SelectSingleNode("sm:Abstract", nsmgr);
			_ServiceDescription.Abstract = (node != null ? node.InnerText : null);
			node = xnlServiceDescription.SelectSingleNode("sm:Fees", nsmgr);
			_ServiceDescription.Fees = (node != null ? node.InnerText : null);
			node = xnlServiceDescription.SelectSingleNode("sm:AccessConstraints", nsmgr);
			_ServiceDescription.AccessConstraints = (node != null ? node.InnerText : null);

			XmlNodeList xnlKeywords = xnlServiceDescription.SelectNodes("sm:KeywordList/sm:Keyword", nsmgr);
			if (xnlKeywords != null)
			{
				_ServiceDescription.Keywords = new string[xnlKeywords.Count];
				for (int i = 0; i < xnlKeywords.Count; i++)
					ServiceDescription.Keywords[i] = xnlKeywords[i].InnerText;
			}
			//Contact information
			_ServiceDescription.ContactInformation = new Capabilities.WmsContactInformation();
			node = xnlServiceDescription.SelectSingleNode("sm:ContactInformation/sm:ContactAddress/sm:Address", nsmgr);
			_ServiceDescription.ContactInformation.Address.Address = (node != null ? node.InnerText : null);
			node = xnlServiceDescription.SelectSingleNode("sm:ContactInformation/sm:ContactAddress/sm:AddressType", nsmgr);
			_ServiceDescription.ContactInformation.Address.AddressType = (node != null ? node.InnerText : null);
			node = xnlServiceDescription.SelectSingleNode("sm:ContactInformation/sm:ContactAddress/sm:City", nsmgr);
			_ServiceDescription.ContactInformation.Address.City = (node != null ? node.InnerText : null);
			node = xnlServiceDescription.SelectSingleNode("sm:ContactInformation/sm:ContactAddress/sm:Country", nsmgr);
			_ServiceDescription.ContactInformation.Address.Country = (node != null ? node.InnerText : null);
			node = xnlServiceDescription.SelectSingleNode("sm:ContactInformation/sm:ContactAddress/sm:PostCode", nsmgr);
			_ServiceDescription.ContactInformation.Address.PostCode = (node != null ? node.InnerText : null);
			node = xnlServiceDescription.SelectSingleNode("sm:ContactInformation/sm:ContactElectronicMailAddress", nsmgr);
			_ServiceDescription.ContactInformation.Address.StateOrProvince = (node != null ? node.InnerText : null);
			node = xnlServiceDescription.SelectSingleNode("sm:ContactInformation/sm:ContactElectronicMailAddress", nsmgr);
			_ServiceDescription.ContactInformation.ElectronicMailAddress = (node != null ? node.InnerText : null);
			node = xnlServiceDescription.SelectSingleNode("sm:ContactInformation/sm:ContactFacsimileTelephone", nsmgr);
			_ServiceDescription.ContactInformation.FacsimileTelephone = (node != null ? node.InnerText : null);
			node = xnlServiceDescription.SelectSingleNode("sm:ContactInformation/sm:ContactPersonPrimary/sm:ContactOrganisation", nsmgr);
			_ServiceDescription.ContactInformation.PersonPrimary.Organisation = (node != null ? node.InnerText : null);
			node = xnlServiceDescription.SelectSingleNode("sm:ContactInformation/sm:ContactPersonPrimary/sm:ContactPerson", nsmgr);
			_ServiceDescription.ContactInformation.PersonPrimary.Person = (node != null ? node.InnerText : null);
			node = xnlServiceDescription.SelectSingleNode("sm:ContactInformation/sm:ContactVoiceTelephone", nsmgr);
			_ServiceDescription.ContactInformation.VoiceTelephone = (node != null ? node.InnerText : null);
		}
				
		private void ParseCapability(XmlNode xnlCapability)
		{
			XmlNode xnlRequest = xnlCapability.SelectSingleNode("sm:Request", nsmgr);
			if (xnlRequest == null)
				throw (new System.Exception("Request parameter not specified in Service Description"));
			ParseRequest(xnlRequest);
			XmlNode xnlLayer = xnlCapability.SelectSingleNode("sm:Layer", nsmgr);
			if (xnlLayer == null)
				throw (new System.Exception("No layer tag found in Service Description")); 
			_Layer = ParseLayer(xnlLayer);
			//TODO:
			//XmlNode xnlException = xnlCapability.SelectSingleNode("/Exception");		
		}

		private void ParseRequest(XmlNode xmlRequestNode)
		{
			XmlNode xnGetMap = xmlRequestNode.SelectSingleNode("sm:GetMap",nsmgr);
			ParseGetMapRequest(xnGetMap);
			//TODO:
			//XmlNode xnGetFeatureInfo = xmlRequestNodes.SelectSingleNode("/GetFeatureInfo");
			//XmlNode xnCapa = xmlRequestNodes.SelectSingleNode("/GetCapabilities"); <-- We don't really need this do we?			
		}

		private void ParseGetMapRequest(XmlNode GetMapRequestNodes)
		{
			XmlNode xnlHttp = GetMapRequestNodes.SelectSingleNode("sm:DCPType/sm:HTTP", nsmgr);
			if (xnlHttp != null && xnlHttp.HasChildNodes)
			{
				_GetMapRequests = new WmsOnlineResource[xnlHttp.ChildNodes.Count];
				for (int i = 0; i < xnlHttp.ChildNodes.Count; i++)
				{

					WmsOnlineResource wor = new WmsOnlineResource();
					wor.Type = xnlHttp.ChildNodes[i].Name;
					wor.OnlineResource = xnlHttp.ChildNodes[i].SelectSingleNode("sm:OnlineResource", nsmgr).Attributes["xlink:href"].InnerText;
					_GetMapRequests[i] = wor;
				}
			}
			XmlNodeList xnlFormats = GetMapRequestNodes.SelectNodes("sm:Format", nsmgr);
			_GetMapOutputFormats = new List<string>(xnlFormats.Count);
			for (int i = 0; i < xnlFormats.Count;i++ )
				_GetMapOutputFormats.Add(xnlFormats[i].InnerText);
		}

		//Iterates through the layers recursively
		private WmsServerLayer ParseLayer(XmlNode xmlLayer)
		{
			WmsServerLayer layer = new WmsServerLayer();
			XmlNode node = xmlLayer.SelectSingleNode("sm:Name", nsmgr);
			layer.Name = (node != null ? node.InnerText : null);
			node = xmlLayer.SelectSingleNode("sm:Title", nsmgr);
			layer.Title = (node != null ? node.InnerText : null);
			node = xmlLayer.SelectSingleNode("sm:Abstract", nsmgr);
			layer.Abstract = (node != null ? node.InnerText : null);
			XmlAttribute attr = xmlLayer.Attributes["queryable"];
			layer.Queryable = (attr != null && attr.InnerText == "1");


			XmlNodeList xnlKeywords = xmlLayer.SelectNodes("sm:KeywordList/sm:Keyword", nsmgr);
			if (xnlKeywords != null)
			{
				layer.Keywords = new string[xnlKeywords.Count];
				for (int i = 0; i < xnlKeywords.Count; i++)
					layer.Keywords[i] = xnlKeywords[i].InnerText;
			}
			XmlNodeList xnlCrs = xmlLayer.SelectNodes("sm:CRS", nsmgr);
			if (xnlCrs != null)
			{
				layer.CRS = new string[xnlCrs.Count];
				for (int i = 0; i < xnlCrs.Count; i++)
					layer.CRS[i] = xnlCrs[i].InnerText;
			}
			XmlNodeList xnlStyle = xmlLayer.SelectNodes("sm:Style", nsmgr);
			if (xnlStyle != null)
			{
				layer.Style = new string[xnlStyle.Count];
				for (int i = 0; i < xnlStyle.Count; i++)
					layer.Style[i] = xnlStyle[i].InnerText;
			}
			XmlNodeList xnlLayers = xmlLayer.SelectNodes("sm:Layer", nsmgr);
			if (xnlLayers != null)
			{
				layer.ChildLayers = new WmsServerLayer[xnlLayers.Count];
				for (int i = 0; i < xnlLayers.Count; i++)
					layer.ChildLayers[i] = ParseLayer(xnlLayers[i]);
			}
			node = xmlLayer.SelectSingleNode("sm:LatLonBoundingBox", nsmgr);
			if(node!=null)
			{
				double minx=0; double miny=0; double maxx=0; double maxy=0;
				if(!double.TryParse(node.Attributes["minx"].InnerText,out minx) &&
					!double.TryParse(node.Attributes["miny"].InnerText,out miny) &&
					!double.TryParse(node.Attributes["maxx"].InnerText,out maxx) &&
					!double.TryParse(node.Attributes["maxy"].InnerText,out maxy))
						throw new ArgumentException("Invalid LatLonBoundingBox on layer '" + layer.Name + "'");
					layer.BoundingBox = new SharpMap.Geometries.BoundingBox(minx, miny, maxx, maxy);
			}
			return layer;
		}
	}
}