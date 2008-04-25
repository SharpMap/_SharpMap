
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
                
using Microsoft.Practices.Unity;

namespace SharpMap.Presentation.AspNet.IoC
{
    public static class Container
    {
        private static readonly IUnityContainer _instance = new UnityContainer();
        public static IUnityContainer Instance { get { return _instance; } }
    }

    //public static class Container
    //{
    //    private static readonly Dictionary<Type, object> _objectMap;

    //    public static void RegisterObject<TType>(TType obj)
    //    {
    //        if (!_objectMap.ContainsKey(typeof(TType)))
    //            _objectMap.Add(typeof(TType), obj);
    //        else
    //            _objectMap[typeof(TType)] = obj;
    //    }

    //    public static TType ResolveObject<TType>()
    //    {
    //        return (TType)_objectMap[typeof(TType)];
    //    }

    //    private static readonly Dictionary<Type, ObjectBuilder> _TypeMap;
    //    delegate object ObjectBuilder();

    //    static Container()
    //    {
    //        _TypeMap = new Dictionary<Type, ObjectBuilder>();
    //        _objectMap = new Dictionary<Type, object>();
    //    }


    //    public static void Register<TInterface>(Type implementationType)
    //    {
    //        lock (_TypeMap)
    //        {
    //            if (!_TypeMap.ContainsKey(typeof(TInterface)))
    //                _TypeMap.Add(typeof(TInterface), CreateDynamicConstructor<TInterface>(implementationType));
    //            else
    //            {
    //                _TypeMap[typeof(TInterface)] = CreateDynamicConstructor<TInterface>(implementationType);
    //            }
    //        }
    //    }

    //    public static object GetRenderer(Type t)
    //    {
    //        if (!_TypeMap.ContainsKey(t))
    //            throw new InvalidOperationException(string.Format("No mapping for type {0}", t));

    //        return _TypeMap[t]();
    //    }

    //    public static TInterface Resolve<TInterface>()
    //    {
    //        return (TInterface)GetRenderer(typeof(TInterface));
    //    }

    //    static ObjectBuilder CreateDynamicConstructor<TInterface>(Type t)
    //    {
    //        ConstructorInfo ci = t.GetConstructor(Type.EmptyTypes);
    //        DynamicMethod dm = new DynamicMethod(string.Format("create {0}", t.FullName), t, null, true);
    //        ILGenerator gen = dm.GetILGenerator();
    //        gen.Emit(OpCodes.Newobj, ci);
    //        gen.Emit(OpCodes.Ret);
    //        return (ObjectBuilder)dm.CreateDelegate(typeof(ObjectBuilder), null);
    //    }
    //}
}
