
/*
 *	This file is part of SharpMap
 *  SharpMap is free software © 2008 Newgrove Consultants Limited, 
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
 */

using System.IO;

namespace SharpMap.Renderer
{
    public delegate void AsyncRenderCallbackDelegate(Stream result, string mimeType);
    public delegate void AsyncRenderCallbackDelegate<TOutput>(TOutput result, string mimeType);

    internal delegate void InternalAsyncRenderDelegate(Map map, AsyncRenderCallbackDelegate callback);
    internal delegate void InternalAsyncRenderDelegate<TRenderFormat>(Map map, AsyncRenderCallbackDelegate<TRenderFormat> callback);


}
