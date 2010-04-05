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
	/// SharpMap.Layers.WmsLayer myLayer = new SharpMap.Layers.WmsLayer("Demis WMS");
    /// myLayer.WmsResource = "http://www2.demis.nl/mapserver/request.asp?WMTVER=1.1.1&amp;LAYERS=Bathymetry,Countries,Topography,Hillshading,Builtup areas,Coastlines,Waterbodies,Inundated,Rivers,Streams,Railroads,Highways,Roads,Trails,Borders,Cities,Settlements,Spot elevations,Airports,Ocean features&amp;STYLES=&amp;FORMAT=image/png&amp;SRS=EPSG:4326";
	/// myMap.Layers.Add(myLayer);
	/// myMap.Center = new SharpMap.Geometries.Point(0, 0);
	/// myMap.Zoom = 360;
	/// myMap.MaximumZoom = 360;
	/// myMap.MinimumZoom = 0.1;
	/// </code>
	/// </example>
	public class WmsLayer : SharpMap.Layers.Layer
	{
		/// <summary>
		/// Initializes a new layer
		/// </summary>
		/// <param name="layername">Name of layer</param>
		public WmsLayer(string layername)
		{
			_TimeOut = 10000;
			this.LayerName = layername;
			_ContinueOnError = true;
		}

		#region ILayer Members

		/// <summary>
		/// Renders the layer
		/// </summary>
		/// <param name="g">Graphics object reference</param>
		/// <param name="map">Map which is rendered</param>
		public override void Render(System.Drawing.Graphics g, Map map)
		{
			
			string strBBOX = "BBOX=" + map.Envelope.Min.X.ToString(SharpMap.Map.numberFormat_EnUS) + "," +
								map.Envelope.Min.Y.ToString(SharpMap.Map.numberFormat_EnUS) + "," +
								map.Envelope.Max.X.ToString(SharpMap.Map.numberFormat_EnUS) + "," +
								map.Envelope.Max.Y.ToString(SharpMap.Map.numberFormat_EnUS);
			System.Text.StringBuilder strReq = new StringBuilder(this.WmsResource);
			if(!this.WmsResource.EndsWith("&") && !this.WmsResource.EndsWith("?"))
				strReq.Append("&");

			strReq.Append("REQUEST=GetMap&" + strBBOX);
			strReq.Append("&WIDTH=" + map.Size.Width.ToString() + "&HEIGHT=" + map.Size.Height.ToString());
			Uri myUri = new Uri(strReq.ToString());
			System.Net.WebRequest myWebRequest = System.Net.WebRequest.Create(myUri);
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
#if !CFBuild //No Image.FromStream in CF use new instead. No DrawImageUnscaled either. May be identical to Full Framework.
                    System.Drawing.Image img = System.Drawing.Image.FromStream(myWebResponse.GetResponseStream());
					g.DrawImageUnscaled(img, 0, 0,map.Size.Width,map.Size.Height);
#else
                    //Use a bitmap.
                    System.Drawing.Bitmap img = new System.Drawing.Bitmap(myWebResponse.GetResponseStream());
                    g.DrawImage(img, 0, 0);
#endif

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
#if !CFBuild //no Trace.Write in CF Lost functionality.
					System.Diagnostics.Trace.Write("There was a problem connecting to the WMS server when rendering layer '" + this.LayerName + "': " + webEx.Message);
#else
                { }
#endif
			}
			catch (System.Exception ex)
			{
				if (!_ContinueOnError)
					throw (new SharpMap.Rendering.Exceptions.RenderException("There was a problem rendering layer '" + this.LayerName + "'", ex));
				else
                //Write out a trace warning instead of throwing an error to help debugging WMS problems
#if !CFBuild //no Trace.Write in CF Lost Functionality
					System.Diagnostics.Trace.Write("There was a problem connecting to the WMS server when rendering layer '" + this.LayerName + "': " + ex.Message);
#else
                { }
#endif

			}
			base.Render(g, map);
		}

		/// <summary>
		/// Returns the extent of the layer
		/// </summary>
		/// <returns>Bounding box corresponding to the extent of the features in the layer</returns>
		public override SharpMap.Geometries.BoundingBox Envelope
		{
			get
			{
				//TODO:
				throw new NotImplementedException();
			}
		}

		private Boolean _ContinueOnError;

		/// <summary>
		/// Specifies whether to throw an exception if the Wms request failed, or to just skip the layer
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
	

		private string _WmsResource;

		/// <summary>
		/// Specifies the URL for retrieving a WMS layer.
		/// </summary>
		/// <example>
		/// http://www.myserver.com/WMSrequest.asp?WMTVER=1.1.1
		/// </example>
		public string WmsResource
		{
			get { return _WmsResource; }
			set { _WmsResource = value; }
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
