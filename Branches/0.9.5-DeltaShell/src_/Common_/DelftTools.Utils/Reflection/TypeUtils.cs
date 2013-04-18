using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using Castle.Core;

namespace DelftTools.Utils.Reflection
{
    public static class TypeUtils
    {
        public static bool IsDelegate(Type type)
        {
            return typeof (Delegate).IsAssignableFrom(type) && !type.IsAbstract;
        }

        public static bool Implements(this Type thisType, Type type)
        {
            return type.IsAssignableFrom(thisType);
        }


        /// <summary>
        /// Usage: CreateGeneric(typeof(List<>), typeof(string));
        /// </summary>
        /// <param name="generic"></param>
        /// <param name="innerType"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static object CreateGeneric(Type generic, Type innerType, params object[] args)
        {
            Type specificType = generic.MakeGenericType(new[] {innerType});
            return Activator.CreateInstance(specificType, args);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TObject">Type of the object where field is stored.</typeparam>
        /// <typeparam name="TField">Type of the field, used as return type</typeparam>
        /// <param name="instance"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static TField GetField<TObject, TField>(object instance, string name)
        {
            FieldInfo fieldInfo = typeof (TObject).GetField(name, BindingFlags.Instance
                                                                  | BindingFlags.NonPublic
                                                                  | BindingFlags.Public);

            return (TField) fieldInfo.GetValue(instance);
        }

        public static object GetField(object instance, string name)
        {
            Type type = instance.GetType();

            FieldInfo fieldInfo = type.GetField(name, BindingFlags.Instance
                                                      | BindingFlags.NonPublic
                                                      | BindingFlags.Public
                                                      | BindingFlags.Static);

            return fieldInfo.GetValue(instance);
        }

        public static object CreateGeneric(Type generic, Type[] innerTypes, params object[] args)
        {
            Type specificType = generic.MakeGenericType(innerTypes);
            return Activator.CreateInstance(specificType, args);
        }

        /// <summary>
        /// Returns generic instance method of given name. Cannot use GetMethod() because this does not
        /// work if 2 members have the same name (eg. SetValues and SetValues<T>)
        /// </summary>
        /// <param name="declaringType"></param>
        /// <param name="methodName"></param>
        /// <returns></returns>
        private static MethodInfo GetGenericMethod(Type declaringType, string methodName)
            //,Type genericType,object declaringInstance,params object[]args )
        {
            var methods = declaringType.GetMembers(BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            return methods.OfType<MethodInfo>().Where(m => m.Name == methodName && m.IsGenericMethod).First();
        }

        static IDictionary<string, MethodInfo> cachedMethods = new Dictionary<string, MethodInfo>();

        public static object CallGenericMethod(Type declaringType, string methodName, Type genericType,
                                               object declaringInstance, params object[] args)
        {
            var key = declaringType + "_" + genericType + "_" + methodName;

            if (cachedMethods.ContainsKey(key)) // performance optimization, reflaction is very expensive
            {
                return cachedMethods[key].Invoke(declaringInstance, args);
            }


            MethodInfo nonGeneric = GetGenericMethod(declaringType, methodName);

            MethodInfo methodGeneric = nonGeneric.MakeGenericMethod(genericType);

            cachedMethods[key] = methodGeneric;

            return methodGeneric.Invoke(declaringInstance, args);
        }

        public static object CallStaticGenericMethod(Type type, string methodName, Type genericType,
                                                     params object[] args)
        {
            MethodInfo nonGeneric = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public);
            MethodInfo methodGeneric = nonGeneric.MakeGenericMethod(genericType);
            return methodGeneric.Invoke(null, args);
        }

        public static IList GetTypedList(Type t)
        {
            return (IList) CreateGeneric(typeof (List<>), t);
        }

        public static IEnumerable ConvertEnumerableToType(IEnumerable enumerable, Type type)
        {
            return (IEnumerable) TypeUtils.CallStaticGenericMethod(typeof(Enumerable), "Cast", type, enumerable);
        }

        /// <summary>
        /// Returns typeof(int) for List<int> etc.
        /// </summary>
        /// <param name="t"></param>
        public static Type GetFirstGenericTypeParameter(Type t)
        {
            Type[] types = t.GetGenericArguments();
            if (types.Length > 0)
            {
                return types[0];
            }
            else
            {
                return null;
            }
        }

        public static IEnumerable<FieldInfo> GetAllFields(Type t, BindingFlags bindingFlags)
        {
            if (t == null)
                return Enumerable.Empty<FieldInfo>();

            BindingFlags flags = bindingFlags;
            return t.GetFields(flags).Union(GetAllFields(t.BaseType, bindingFlags));
        }

        public static string GetMemberName<T>(Expression<Func<T>> e)
        {
            var member = e.Body as MemberExpression;

            // If the method gets a lambda expression 
            // that is not a member access,
            // for example, () => x + y, an exception is thrown.
            if (member != null)
                return member.Member.Name;
            else
                throw new ArgumentException(
                    "'" + e +
                    "': is not a valid expression for this method");
        }

        public static object GetDefaultValue(Type type)
        {
            return type.IsValueType ?
            Activator.CreateInstance(type) : null;
        }

        public static void SetField(object obj,string propertyName,object value)
        {
            Type type = obj.GetType();

            FieldInfo fieldInfo = type.GetField(propertyName, BindingFlags.Instance
                                                      | BindingFlags.NonPublic
                                                      | BindingFlags.Public);

            fieldInfo.SetValue(obj,value);
        }
    }
}