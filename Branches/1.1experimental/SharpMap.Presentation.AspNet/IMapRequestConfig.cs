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
using System;
using System.Drawing;
using SharpMap.Geometries;
using System.Web;

namespace SharpMap.Presentation.AspNet
{
    /// <summary>
    /// An object  representing the desired configuration of the map.
    /// It can be extended to include things like enabled layers, selection shape and state specific to a particular user.
    /// </summary>
    public interface IMapRequestConfig
    {
        /// <summary>
        /// a string which uniquely identifies this map. To be used by any caching block.
        /// </summary>
        String CacheKey { get; set; }
        /// <summary>
        /// The expected value to be written to the HttpHeader ContentType. 
        /// </summary>
        string MimeType { get; }
        BoundingBox RealWorldBounds { get; set; }
        Size OutputSize { get; set; }

        /// <summary>
        /// Use the data contained within this instance to set the general state on the MapView.
        /// </summary>
        /// <param name="map"></param>
        void ConfigureMap(Map map);

        HttpContext Context { get; set; }

        // IMapRenderer CreateMapRenderer();

    }
}
