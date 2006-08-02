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

namespace SharpMap.Utilities
{
	/// <summary>
	/// Provider helper utilities
	/// </summary>
	public class Providers
	{
		/// <summary>
		/// Returns a list of available data providers in this assembly
		/// </summary>
		public static List<Type> GetProviders()
		{
			List<Type> ProviderList = new List<Type>();
			System.Reflection.Assembly asm = System.Reflection.Assembly.GetExecutingAssembly();
			foreach (Type t in asm.GetTypes())
			{
				Type[] Interfaces = t.FindInterfaces(new System.Reflection.TypeFilter(MyInterfaceFilter), "SharpMap.Providers.IProvider");
				
				if (Interfaces.Length > 0)
					ProviderList.Add(t);
			}
			return ProviderList;
		}

		/// <summary>
		/// Filter method used for searching for objects in an assembly
		/// </summary>
		/// <param name="typeObj"></param>
		/// <param name="criteriaObj"></param>
		/// <returns></returns>
		private static bool MyInterfaceFilter(Type typeObj, Object criteriaObj)
		{
			return (typeObj.ToString() == criteriaObj.ToString());
		}
	}
}
