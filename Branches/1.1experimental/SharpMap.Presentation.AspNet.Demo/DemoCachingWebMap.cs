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
using System.Configuration;
using System.Web;
using SharpMap.Layers;
using SharpMap.Presentation.AspNet.Impl;
using SharpMap.Styles;
using SharpMap.Renderer;
using System;
using SharpMap.Data.Providers;
using SharpMap.Rendering.Thematics;
using SharpMap.Data;
using SharpMap.Presentation.AspNet.IoC;
using System.Drawing;

namespace SharpMap.Presentation.AspNet.Demo
{
    public class DemoCachingWebMap
        : DemoWebMap
    {
        public DemoCachingWebMap(HttpContext c)
            : base(c) { }


        protected override IMapCacheProvider CreateCacheProvider()
        {
            return new AspNetCacheProvider();
            //return Container.Instance.Resolve<IMapCacheProvider<Image>>();
        }

    }
}
