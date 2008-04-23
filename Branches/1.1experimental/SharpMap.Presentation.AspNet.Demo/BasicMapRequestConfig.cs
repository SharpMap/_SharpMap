/*
 *  The attached / following is free software © 2008 Newgrove Consultants Limited, 
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
using System.Collections.Generic;
using System.Text;
using SharpMap.Geometries;
using System.Drawing;

namespace SharpMap.Presentation.AspNet.Demo
{
    public class BasicMapRequestConfig
        : IMapRequestConfig
    {
        #region IMapRequestConfig Members

        string _cacheKey;
        public string CacheKey
        {
            get { return _cacheKey; }
            set { _cacheKey = value; }
        }

        string _mimeType;
        public string MimeType
        {
            get { return _mimeType; }
            set { _mimeType = value; }
        }

        BoundingBox _extents;
        public BoundingBox RealWorldBounds
        {
            get { return _extents; }
            set { _extents = value; ; }
        }

        Size _size;
        public Size OutputSize
        {
            get { return _size; }
            set { _size = value; }
        }

        public void ConfigureMap(Map map)
        {
            map.Size = this.OutputSize;
            if (this.RealWorldBounds == null)
                map.ZoomToExtents();
            else
                map.ZoomToBox(this.RealWorldBounds);
        }

        #endregion
    }
}
