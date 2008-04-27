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
namespace System
{

#if !DOTNET_35
    public delegate TResult Func<TResult>();
    public delegate TResult Func<TArg0, TResult>(TArg0 arg0);
    public delegate TResult Func<TArg0, TArg1, TResult>(TArg0 arg0, TArg1 arg1);

    public delegate void Action<TArg0, TArg1>(TArg0 arg0, TArg1 arg1);

#endif
}
