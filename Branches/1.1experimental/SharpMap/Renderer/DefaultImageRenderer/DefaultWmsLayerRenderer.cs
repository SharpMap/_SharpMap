/*
 *	This file is part of SharpMap
 *  SharpMap is free software. This file © 2008 Newgrove Consultants Limited, 
 *  http://www.newgrove.com; you can redistribute it and/or modify it under the terms 
 *  of the current GNU Lesser General Public License (LGPL) as published by and 
 *  available from the Free Software Foundation, Inc., 
 *  59 Temple Place, Suite 330, Boston, MA 02111-1307 USA: http://fsf.org/    
 *  This program is distributed without any warranty; 
 *  without even the implied warranty of merchantability or fitness for purpose.  
 *  See the GNU Lesser General Public License for the full details. 
 *  
 *  Author: John Diss 2008
 *  
 *  Portions based on earlier work.
 * 
 */
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using SharpMap.Layers;
using SharpMap.Rendering.Exceptions;
using SharpMap.Web.Wms;

namespace SharpMap.Renderer.DefaultImage
{
    internal class DefaultWmsRenderer
        : DefaultImageRenderer.ILayerRenderer<WmsLayer>
    {

        #region ILayerRenderer<WmsLayer> Members

        public void RenderLayer(WmsLayer layer, Map map, System.Drawing.Graphics g)
        {
            Client.WmsOnlineResource resource = layer.GetPreferredMethod();
            Uri myUri = new Uri(layer.GetRequestUrl(map.Envelope, map.Size));
            WebRequest myWebRequest = WebRequest.Create(myUri);
            myWebRequest.Method = resource.Type;
            myWebRequest.Timeout = layer.TimeOut;
            if (layer.Credentials != null)
                myWebRequest.Credentials = layer.Credentials;
            else
                myWebRequest.Credentials = CredentialCache.DefaultCredentials;

            if (layer.Proxy != null)
                myWebRequest.Proxy = layer.Proxy;

            try
            {
                HttpWebResponse myWebResponse = (HttpWebResponse)myWebRequest.GetResponse();
                Stream dataStream = myWebResponse.GetResponseStream();

                if (myWebResponse.ContentType.StartsWith("image"))
                {
                    Image img = Image.FromStream(myWebResponse.GetResponseStream());
                    if (layer.ImageAttributes != null)
                        g.DrawImage(img, new Rectangle(0, 0, img.Width, img.Height), 0, 0,
                           img.Width, img.Height, GraphicsUnit.Pixel, layer.ImageAttributes);
                    else
                        g.DrawImageUnscaled(img, 0, 0, map.Size.Width, map.Size.Height);
                }
                dataStream.Close();
                myWebResponse.Close();
            }
            catch (WebException webEx)
            {
                if (!layer.ContinueOnError)
                    throw (new RenderException("There was a problem connecting to the WMS server when rendering layer '" + layer.LayerName + "'", webEx));
                else
                    //Write out a trace warning instead of throwing an error to help debugging WMS problems
                    Trace.Write("There was a problem connecting to the WMS server when rendering layer '" + layer.LayerName + "': " + webEx.Message);
            }
            catch (Exception ex)
            {
                if (!layer.ContinueOnError)
                    throw (new RenderException("There was a problem rendering layer '" + layer.LayerName + "'", ex));
                else
                    //Write out a trace warning instead of throwing an error to help debugging WMS problems
                    Trace.Write("There was a problem connecting to the WMS server when rendering layer '" + layer.LayerName + "': " + ex.Message);
            }
            //base.Render(g, map);
        }

        #endregion

        #region ILayerRenderer Members

        public void RenderLayer(ILayer layer, Map map, System.Drawing.Graphics g)
        {
            RenderLayer((WmsLayer)layer, map, g);
        }

        #endregion
    }
}
