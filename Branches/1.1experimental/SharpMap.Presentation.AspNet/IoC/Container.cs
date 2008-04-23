using System;
using System.Data;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace SharpMap.Presentation.AspNet.IoC
{
    public static class Container
    {
        private static readonly Dictionary<Type, object> _objectMap;

        public static void RegisterObject<TType>(TType obj)
        {
            if (!_objectMap.ContainsKey(typeof(TType)))
                _objectMap.Add(typeof(TType), obj);
            else
                _objectMap[typeof(TType)] = obj;
        }

        public static TType ResolveObject<TType>()
        {
            return (TType)_objectMap[typeof(TType)];
        }

        private static readonly Dictionary<Type, ObjectBuilder> _TypeMap;
        delegate object ObjectBuilder();

        static Container()
        {
            _TypeMap = new Dictionary<Type, ObjectBuilder>();
            _objectMap = new Dictionary<Type, object>();
        }


        public static void Register<TInterface>(Type implementationType)
        {
            lock (_TypeMap)
            {
                if (!_TypeMap.ContainsKey(typeof(TInterface)))
                    _TypeMap.Add(typeof(TInterface), CreateDynamicConstructor<TInterface>(implementationType));
                else
                {
                    _TypeMap[typeof(TInterface)] = CreateDynamicConstructor<TInterface>(implementationType);
                }
            }
        }

        public static object GetRenderer(Type t)
        {
            if (!_TypeMap.ContainsKey(t))
                throw new InvalidOperationException(string.Format("No mapping for type {0}", t));

            return _TypeMap[t]();
        }

        public static TInterface Resolve<TInterface>()
        {
            return (TInterface)GetRenderer(typeof(TInterface));
        }

        static ObjectBuilder CreateDynamicConstructor<TInterface>(Type t)
        {
            ConstructorInfo ci = t.GetConstructor(Type.EmptyTypes);
            DynamicMethod dm = new DynamicMethod(string.Format("create {0}", t.FullName), t, null, true);
            ILGenerator gen = dm.GetILGenerator();
            gen.Emit(OpCodes.Newobj, ci);
            gen.Emit(OpCodes.Ret);
            return (ObjectBuilder)dm.CreateDelegate(typeof(ObjectBuilder), null);
        }
    }
}
