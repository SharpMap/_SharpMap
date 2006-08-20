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

using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing.Imaging;
using System.Drawing;

namespace SharpMap.Layers
{
	/// <summary>
	/// Web Map Service layer
	/// </summary>
	/// <remarks>
	/// The WmsLayer is currently very basic and doesn't support automatic fetching of the WMS Service Description.
	/// Instead you would have to add the nessesary parameters to the URL,
	/// and the WmsLayer will set the remaining BoundingBox property and proper requests that changes between the requests.
	/// See the example below.
	/// </remarks>
	/// <example>
	/// The following example creates a map with a WMS layer the Demis WMS Server
	/// <code lang="C#">
	/// myMap = new SharpMap.Map(new System.Drawing.Size(500,250);
	/// string wmsUrl = "http://www2.demis.nl/mapserver/request.asp";
	/// SharpMap.Layers.WmsLayer myLayer = new SharpMap.Layers.WmsLayer("Demis WMS", myLayer);
	/// myLayer.AddLayer("Bathymetry");
	/// myLayer.AddLayer("Countries");
	/// myLayer.AddLayer("Topography");
	/// myLayer.AddLayer("Hillshading");
	/// myLayer.SetImageFormat(layWms.OutputFormats[0]);
	/// myLayer.SpatialReferenceSystem = "EPSG:4326";	
    /// myMap.Layers.Add(myLayer);
	/// myMap.Center = new SharpMap.Geometries.Point(0, 0);
	/// myMap.Zoom = 360;
	/// myMap.MaximumZoom = 360;
	/// myMap.MinimumZoom = 0.1;
	/// </code>
	/// </example>
	public class WmsLayer : SharpMap.Layers.Layer
	{
		private SharpMap.Web.Wms.Client wmsClient;
		private string _MimeType = "";
		
		/// <summary>
		/// Initializes a new layer, and downloads and parses the service description
		/// </summary>
		/// <remarks>In and ASP.NET application the service description is automatically cached for 24 hours when not specified</remarks>
		/// <param name="layername"></param>
		/// <param name="url"></param>
		public WmsLayer(string layername, string url) : this(layername,url,new TimeSpan(24,0,0))
		{
		}

		private List<string> _LayerList;

		/// <summary>
		/// Gets the list of enabled layers
		/// </summary>
		public List<string> LayerList
		{
			get { return _LayerList; }
		}

		/// <summary>
		/// Adds a layer to WMS request
		/// </summary>
		/// <remarks>Layer names are case sensitive.</remarks>
		/// <param name="name">Name of layer</param>
		/// <exception cref="System.ArgumentException">Throws an exception is an unknown layer is added</exception>
		public void AddLayer(string name)
		{			
			if(!LayerExists(wmsClient.Layer,name))
				throw new ArgumentException("Cannot add WMS Layer - Unknown layername");
			
			_LayerList.Add(name);
		}
		/// <summary>
		/// Recursive method for checking whether a layername exists
		/// </summary>
		/// <param name="layer"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		private bool LayerExists(SharpMap.Web.Wms.Client.WmsServerLayer layer, string name)
		{
			if(name == layer.Name) return true;
			foreach (SharpMap.Web.Wms.Client.WmsServerLayer childlayer in layer.ChildLayers)
				if (LayerExists(childlayer,name)) return true;
			return false;
		}
		/// <summary>
		/// Removes a layer from the layer list
		/// </summary>
		/// <param name="name">Name of layer to remove</param>
		public void RemoveLayer(string name)
		{
			_LayerList.Remove(name);
		}
		/// <summary>
		/// Removes the layer at the specified index
		/// </summary>
		/// <param name="index"></param>
		public void RemoveLayerAt(int index)
		{
			_LayerList.RemoveAt(index);
		}
		/// <summary>
		/// Removes all layers
		/// </summary>
		public void RemoveAllLayers()
		{
			_LayerList.Clear();
		}

		private List<string> _StylesList;

		/// <summary>
		/// Gets the list of enabled styles
		/// </summary>
		public List<string> StylesList
		{
			get { return _StylesList; }
		}

		/// <summary>
		/// Adds a style to the style collection
		/// </summary>
		/// <param name="name">Name of style</param>
		/// <exception cref="System.ArgumentException">Throws an exception is an unknown layer is added</exception>
		public void AddStyle(string name)
		{
			if (!StyleExists(wmsClient.Layer, name))
				throw new ArgumentException("Cannot add WMS Layer - Unknown layername");
			_StylesList.Add(name);
		}

		/// <summary>
		/// Recursive method for checking whether a layername exists
		/// </summary>
		/// <param name="layer">layer</param>
		/// <param name="name">name of style</param>
		/// <returns>True of style exists</returns>
		private bool StyleExists(SharpMap.Web.Wms.Client.WmsServerLayer layer, string name)
		{			
			foreach(SharpMap.Web.Wms.Client.WmsLayerStyle style in layer.Style)
				if (name == style.Name) return true;
			foreach (SharpMap.Web.Wms.Client.WmsServerLayer childlayer in layer.ChildLayers)
				if (StyleExists(childlayer, name)) return true;
			return false;
		}
		/// <summary>
		/// Removes a style from the collection
		/// </summary>
		/// <param name="name">Name of style</param>
		public void RemoveStyle(string name)
		{
			_StylesList.Remove(name);
		}
		/// <summary>
		/// Removes a style at specified index
		/// </summary>
		/// <param name="index">Index</param>
		public void RemoveStyleAt(int index)
		{
			_StylesList.RemoveAt(index);
		}
		/// <summary>
		/// Removes all styles from the list
		/// </summary>
		public void RemoveAllStyles()
		{
			_StylesList.Clear();
		}
		/// <summary>
		/// Initializes a new layer, and downloads and parses the service description
		/// </summary>
		/// <param name="layername"></param>
		/// <param name="url">Url of service description</param>
		/// <param name="cachetime">Time for caching Service Description (ASP.NET only)</param>
		public WmsLayer(string layername, string url, TimeSpan cachetime)
		{
			_TimeOut = 10000;
			this.LayerName = layername;
			_ContinueOnError = true;
			if (System.Web.HttpContext.Current != null && System.Web.HttpContext.Current.Cache["SharpMap_WmsClient_" + url] != null)
			{
				wmsClient = (SharpMap.Web.Wms.Client)System.Web.HttpContext.Current.Cache["SharpMap_WmsClient_" + url];
			}
			else
			{
				wmsClient = new SharpMap.Web.Wms.Client(url, _Proxy);
				if (System.Web.HttpContext.Current != null)
					System.Web.HttpContext.Current.Cache.Insert("SharpMap_WmsClient_"+url, wmsClient, null,
						System.Web.Caching.Cache.NoAbsoluteExpiration, cachetime);				
			}
			//Set default mimetype - We prefer compressed formats
			if (OutputFormats.Contains("image/jpeg")) _MimeType = "image/jpeg";
			else if (OutputFormats.Contains("image/png")) _MimeType = "image/png";
			else if (OutputFormats.Contains("image/gif")) _MimeType = "image/gif";
			else //None of the default formats supported - Look for the first supported output format
			{
				bool formatSupported = false;
				foreach (System.Drawing.Imaging.ImageCodecInfo encoder in System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders())
					if (OutputFormats.Contains(encoder.MimeType.ToLower()))
					{
						formatSupported = true;
						_MimeType = encoder.MimeType;
						break;
					}
				if(!formatSupported)
					throw new ArgumentException("GDI+ doesn't not support any of the mimetypes supported by this WMS service");
			}
			_LayerList = new List<string>();
			_StylesList = new List<string>();
		}

		/// <summary>
		/// Sets the image type to use when requesting images from the WMS server
		/// </summary>
		/// <remarks>
		/// <para>See the <see cref="OutputFormats"/> property for a list of available mime types supported by the WMS server</para>
		/// </remarks>
		/// <exception cref="ArgumentException">Throws an exception if either the mime type isn't offered by the WMS
		/// or GDI+ doesn't support this mime type.</exception>
		/// <param name="mimeType">Mime type of image format</param>
		public void SetImageFormat(string mimeType)
		{
			if (!OutputFormats.Contains(mimeType))
				throw new ArgumentException("WMS service doesn't not offer mimetype '" + mimeType + "'");
			//Check whether SharpMap supports the specified mimetype
			bool formatSupported = false;
			foreach (System.Drawing.Imaging.ImageCodecInfo encoder in System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders())
				if (encoder.MimeType.ToLower() == mimeType.ToLower())
				{
					formatSupported = true;
					break;
				}
			if (!formatSupported)
				throw new ArgumentException("GDI+ doesn't not support mimetype '" + mimeType + "'");
			_MimeType = mimeType;
		}

		/// <summary>
		/// Gets the hiarchial list of available WMS layers from this service
		/// </summary>
		public SharpMap.Web.Wms.Client.WmsServerLayer RootLayer
		{
			get { return wmsClient.Layer; }
		}
		/// <summary>
		/// Gets the list of available formats
		/// </summary>
		public List<string> OutputFormats
		{
			get { return wmsClient.GetMapOutputFormats; }
		}

		private string _SpatialReferenceSystem;

		/// <summary>
		/// Gets or sets the spatial reference used for the WMS server request
		/// </summary>
		public string SpatialReferenceSystem
		{
			get { return _SpatialReferenceSystem; }
			set { _SpatialReferenceSystem = value; }
		}
	

		/// <summary>
		/// Gets the service description from this server
		/// </summary>
		public SharpMap.Web.Wms.Capabilities.WmsServiceDescription ServiceDescription
		{
			get { return wmsClient.ServiceDescription; }
		}
		/// <summary>
		/// Gets the WMS Server version of this service
		/// </summary>
		public string Version
		{
			get { return wmsClient.WmsVersion; }
		}

		private ImageAttributes _ImageAttributes;

		/// <summary>
		/// When specified, applies image attributes at image (fx. make WMS layer semi-transparent)
		/// </summary>
		/// <remarks>
		/// <para>You can make the WMS layer semi-transparent by settings a up a ColorMatrix,
		/// or scale/translate the colors in any other way you like.</para>
		/// <example>
		/// Setting the WMS layer to be semi-transparent.
		/// <code lang="C#">
		/// float[][] colorMatrixElements = { 
		///				new float[] {1,  0,  0,  0, 0},
		///				new float[] {0,  1,  0,  0, 0},
		///				new float[] {0,  0,  1,  0, 0},
		///				new float[] {0,  0,  0,  0.5, 0},
		///				new float[] {0, 0, 0, 0, 1}};
		/// ColorMatrix colorMatrix = new ColorMatrix(colorMatrixElements);
		/// ImageAttributes imageAttributes = new ImageAttributes();
		/// imageAttributes.SetColorMatrix(
		/// 	   colorMatrix,
		/// 	   ColorMatrixFlag.Default,
		/// 	   ColorAdjustType.Bitmap);
		/// myWmsLayer.ImageAttributes = imageAttributes;
		/// </code>
		/// </example>
		/// </remarks>
		public ImageAttributes ImageAttributes
		{
			get { return _ImageAttributes; }
			set { _ImageAttributes = value; }
		}
	

		#region ILayer Members

		/// <summary>
		/// Renders the layer
		/// </summary>
		/// <param name="g">Graphics object reference</param>
		/// <param name="map">Map which is rendered</param>
		public override void Render(System.Drawing.Graphics g, Map map)
		{
			SharpMap.Web.Wms.Client.WmsOnlineResource resource = GetPreferredMethod();			
			Uri myUri = new Uri(GetRequestUrl(map.Envelope,map.Size));
			System.Net.WebRequest myWebRequest = System.Net.WebRequest.Create(myUri);
			myWebRequest.Method = resource.Type;
			myWebRequest.Timeout = _TimeOut;
			if (_Credentials != null)
				myWebRequest.Credentials = _Credentials;
			else
				myWebRequest.Credentials = System.Net.CredentialCache.DefaultCredentials;

			if (_Proxy != null)
				myWebRequest.Proxy = _Proxy;

			try
			{
				System.Net.HttpWebResponse myWebResponse = (System.Net.HttpWebResponse)myWebRequest.GetResponse();
				System.IO.Stream dataStream = myWebResponse.GetResponseStream();

				if (myWebResponse.ContentType.StartsWith("image"))
				{
					System.Drawing.Image img = System.Drawing.Image.FromStream(myWebResponse.GetResponseStream());
					if (_ImageAttributes != null)
						g.DrawImage(img, new Rectangle(0, 0, img.Width, img.Height), 0, 0,
						   img.Width, img.Height, GraphicsUnit.Pixel, this.ImageAttributes);
					else
						g.DrawImageUnscaled(img, 0, 0,map.Size.Width,map.Size.Height);
				}
				dataStream.Close();
				myWebResponse.Close();
			}
			catch (System.Net.WebException webEx)
			{
				if (!_ContinueOnError)
					throw (new SharpMap.Rendering.Exceptions.RenderException("There was a problem connecting to the WMS server when rendering layer '" + this.LayerName + "'", webEx));
				else
					//Write out a trace warning instead of throwing an error to help debugging WMS problems
					System.Diagnostics.Trace.Write("There was a problem connecting to the WMS server when rendering layer '" + this.LayerName + "': " + webEx.Message);
			}
			catch (System.Exception ex)
			{
				if (!_ContinueOnError)
					throw (new SharpMap.Rendering.Exceptions.RenderException("There was a problem rendering layer '" + this.LayerName + "'", ex));
				else
					//Write out a trace warning instead of throwing an error to help debugging WMS problems
					System.Diagnostics.Trace.Write("There was a problem connecting to the WMS server when rendering layer '" + this.LayerName + "': " + ex.Message);
			}
			base.Render(g, map);
		}

		/// <summary>
		/// Gets the URL for a map request base on current settings, the image size and boundingbox
		/// </summary>
		/// <param name="box">Area the WMS request should cover</param>
		/// <param name="size">Size of image</param>
		/// <returns>URL for WMS request</returns>
		public string GetRequestUrl(SharpMap.Geometries.BoundingBox box, System.Drawing.Size size)
		{
			SharpMap.Web.Wms.Client.WmsOnlineResource resource = GetPreferredMethod();			
			System.Text.StringBuilder strReq = new StringBuilder(resource.OnlineResource);
			if(!resource.OnlineResource.Contains("?"))
				strReq.Append("?");
			if (!strReq.ToString().EndsWith("&") && !strReq.ToString().EndsWith("?"))
				strReq.Append("&");

			strReq.AppendFormat(SharpMap.Map.numberFormat_EnUS, "REQUEST=GetMap&BBOX={0},{1},{2},{3}",
				box.Min.X, box.Min.Y, box.Max.X, box.Max.Y);
			strReq.AppendFormat("&WIDTH={0}&Height={1}", size.Width, size.Height);
			strReq.Append("&Layers=");
			if (_LayerList != null && _LayerList.Count > 0)
			{
				foreach (string layer in _LayerList)
					strReq.AppendFormat("{0},", layer);
				strReq.Remove(strReq.Length - 1, 1);
			}
			strReq.AppendFormat("&FORMAT={0}", _MimeType);
			if (_SpatialReferenceSystem == string.Empty)
				throw new ApplicationException("Spatial reference system not set");
			if(wmsClient.WmsVersion=="1.3.0")
				strReq.AppendFormat("&CRS={0}", _SpatialReferenceSystem);
			else
				strReq.AppendFormat("&SRS={0}", _SpatialReferenceSystem);
			strReq.AppendFormat("&VERSION={0}", wmsClient.WmsVersion);
			strReq.Append("&Styles=");
			if (_StylesList != null && _StylesList.Count > 0)
			{
				foreach (string style in _StylesList)
					strReq.AppendFormat("{0},", style);
				strReq.Remove(strReq.Length - 1, 1);
			}
			return strReq.ToString();
		}

		/// <summary>
		/// Returns the extent of the layer
		/// </summary>
		/// <returns>Bounding box corresponding to the extent of the features in the layer</returns>
		public override SharpMap.Geometries.BoundingBox Envelope
		{
			get
			{
				return RootLayer.LatLonBoundingBox;
			}
		}

		private Boolean _ContinueOnError;

		/// <summary>
		/// Specifies whether to throw an exception if the Wms request failed, or to just skip rendering the layer
		/// </summary>
		public Boolean ContinueOnError
		{
			get { return _ContinueOnError; }
			set { _ContinueOnError = value; }
		}

		/// <summary>
		/// Returns the type of the layer
		/// </summary>
		//public override SharpMap.Layers.Layertype LayerType
		//{
		//    get { return SharpMap.Layers.Layertype.Wms; }
		//}

		#endregion

		private SharpMap.Web.Wms.Client.WmsOnlineResource GetPreferredMethod()
		{
			//We prefer posting. Seek for supported post method
			for (int i = 0; i < wmsClient.GetMapRequests.Length; i++)
				if (wmsClient.GetMapRequests[i].Type.ToLower() == "post")
					return wmsClient.GetMapRequests[i];
			//Next we prefer the 'get' method
			for (int i = 0; i < wmsClient.GetMapRequests.Length; i++)
				if (wmsClient.GetMapRequests[i].Type.ToLower() == "get")
					return wmsClient.GetMapRequests[i];
			return wmsClient.GetMapRequests[0];
		}

		private System.Net.ICredentials _Credentials;

		/// <summary>
		/// Provides the base authentication interface for retrieving credentials for Web client authentication.
		/// </summary>
		public System.Net.ICredentials Credentials
		{
			get { return _Credentials; }
			set { _Credentials = value; }
		}

		private System.Net.WebProxy _Proxy;

		/// <summary>
		/// Gets or sets the proxy used for requesting a webresource
		/// </summary>
		public System.Net.WebProxy Proxy
		{
			get { return _Proxy; }
			set { _Proxy = value; }
		}
	

		private int _TimeOut;

		/// <summary>
		/// Timeout of webrequest in milliseconds. Defaults to 10 seconds
		/// </summary>
		public int TimeOut
		{
			get { return _TimeOut; }
			set { _TimeOut = value; }
		}

		#region ICloneable Members

		/// <summary>
		/// Clones the object
		/// </summary>
		/// <returns></returns>
		public override object Clone()
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}
