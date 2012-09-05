using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using DelftTools.Utils.Data;
using log4net;

namespace DelftTools.Utils.Reflection
{
    public static class TypeUtils
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(TypeUtils));
        
        public static bool IsDelegate(Type type)
        {
            return typeof (Delegate).IsAssignableFrom(type) && !type.IsAbstract;
        }

        public static bool Implements<T>(this Type thisType)
        {
            return typeof (T).IsAssignableFrom(thisType);
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

        public static object CreateGeneric(Type generic, Type[] innerTypes, params object[] args)
        {
            Type specificType = generic.MakeGenericType(innerTypes);
            return Activator.CreateInstance(specificType, args);
        }

        public static object GetPropertyValue(object instance, string propertyName, bool throwOnError=true)
        {
            var implementingType = instance.GetType();
//            if (instance is IUnique<long>) //todo: remove this, not required
//            {
//                implementingType = (instance as IUnique<long>).GetEntityType();
//            }

            var propertyInfo = GetPropertyInfo(implementingType, propertyName);
            
            if (!throwOnError && propertyInfo.GetIndexParameters().Count() > 0)
            {
                return null; //invalid combo, would throw
            }

            return propertyInfo.GetValue(instance, new object[0]);
        }

        private readonly static IDictionary<Type, IDictionary<string, PropertyInfo>> PropertyInfoDictionary = new Dictionary<Type, IDictionary<string, PropertyInfo>>();

        private static PropertyInfo GetPropertyInfo(Type type, string propertyName)
        {
            IDictionary<string, PropertyInfo> propertyInfoForType;
            PropertyInfo propertyInfo;

            if (!PropertyInfoDictionary.TryGetValue(type, out propertyInfoForType))
            {
                propertyInfoForType = new Dictionary<string, PropertyInfo>();
                PropertyInfoDictionary.Add(type, propertyInfoForType);
            }

            if (!propertyInfoForType.TryGetValue(propertyName, out propertyInfo))
            {
                propertyInfo = GetPropertyInfoInternal(type, propertyName);
                propertyInfoForType.Add(propertyName, propertyInfo);
            }

            return propertyInfo;
        }

        private static PropertyInfo GetPropertyInfoInternal(Type type, string propertyName)
        {
            //get the property by walking up the inheritance chain. See NHibernate's BasicPropertyAccessor
            //we could extend this logic more by looking there...
            if (type == typeof(object) || type == null)
            {
                // the full inheritance chain has been walked and we could
                // not find the PropertyInfo get
                return null;
            }

            var propertyInfo = type.GetProperty(propertyName,
                                                  BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (propertyInfo != null)
            {
                return propertyInfo;
            }

            return GetPropertyInfoInternal(type.BaseType, propertyName);
        }

        /// <summary>
        /// Returns generic instance method of given name. Cannot use GetMethod() because this does not
        /// work if 2 members have the same name (eg. SetValues and SetValues<T>)
        /// </summary>
        /// <param name="declaringType"></param>
        /// <param name="methodName"></param>
        /// <returns></returns>
        private static MethodInfo GetGenericMethod(Type declaringType, string methodName)
            //,Type genericType,object instance,params object[]args )
        {
            var methods = declaringType.GetMembers(BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            return methods.OfType<MethodInfo>().Where(m => m.Name == methodName && m.IsGenericMethod).First();
        }

        static IDictionary<string, MethodInfo> cachedMethods = new Dictionary<string, MethodInfo>();

        public static object CallGenericMethod(Type declaringType, string methodName, Type genericType,
                                               object instance, params object[] args)
        {
            var key = declaringType + "_" + genericType + "_" + methodName;


            MethodInfo methodInfo = null;

            cachedMethods.TryGetValue(key, out methodInfo);

            if (methodInfo != null) // performance optimization, reflaction is very expensive
            {
                return CallMethod(methodInfo, instance, args);
            }


            MethodInfo nonGeneric = GetGenericMethod(declaringType, methodName);

            methodInfo = nonGeneric.MakeGenericMethod(genericType); // generify

            cachedMethods[key] = methodInfo;

            return CallMethod(methodInfo, instance, args);
        }

        private static object CallMethod(MethodInfo methodInfo, object instance, object[] args)
        {
            object result;
            try
            {
                result = methodInfo.Invoke(instance, args);
            }
            catch (TargetInvocationException e)
            {
                // re-throw original exception
                if (e.InnerException != null)
                {
                    log.Error("Exception occured", e); // log outer exception

                    throw e.InnerException;
                }

                throw;
            }

            return result;
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
            return (IEnumerable) CallStaticGenericMethod(typeof(Enumerable), "Cast", type, enumerable);
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

        public static string GetMemberDescription<T>(Expression<Func<T>> e)
        {
            var member = e.Body as MemberExpression;

            // If the method gets a lambda expression 
            // that is not a member access,
            // for example, () => x + y, an exception is thrown.
            if (member != null)
            {
                var descriptionAttribute = member.Member.GetCustomAttributes(false).OfType<DescriptionAttribute>().FirstOrDefault();
                return descriptionAttribute != null ? descriptionAttribute.Description : member.Member.Name;
            }

            throw new ArgumentException("'" + e + "': is not a valid expression for this method");
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

        public static object GetField(object instance, string fieldName)
        {
            Type type = instance.GetType();

            FieldInfo fieldInfo = GetFieldInfo(type, fieldName);

            if (fieldInfo == null)
            {
                throw new ArgumentOutOfRangeException("fieldName");
            }

            return fieldInfo.GetValue(instance);
        }

        private static FieldInfo GetFieldInfo(Type type, string fieldName)
        {
            if (type == typeof(object) || type == null)
            {
                return null;
            }
            FieldInfo fieldInfo = type.GetField(fieldName, BindingFlags.Instance
                                                           | BindingFlags.NonPublic
                                                           | BindingFlags.Public
                                                           | BindingFlags.Static);
            if (fieldInfo != null)
            {
                return fieldInfo;
            }
            return GetFieldInfo(type.BaseType, fieldName);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TObject">Type of the object where field is stored.</typeparam>
        /// <typeparam name="TField">Type of the field, used as return type</typeparam>
        /// <param name="instance"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public static TField GetField<TObject, TField>(object instance, string fieldName)
        {
            var fieldInfo = typeof (TObject).GetField(fieldName, BindingFlags.Instance
                                                                  | BindingFlags.NonPublic
                                                                  | BindingFlags.Public);

            if (fieldInfo == null)
            {
                throw new ArgumentOutOfRangeException("fieldName");
            }


            return (TField) fieldInfo.GetValue(instance);
        }

        public static void SetField(object obj,string fieldName,object value)
        {
            var fieldInfo = GetFieldInfo(obj.GetType(), fieldName);

            if (fieldInfo == null)
            {
                throw new ArgumentOutOfRangeException("fieldName");
            }

            fieldInfo.SetValue(obj, value);
        }

        public static void SetField<T>(object obj, string fieldName, object value)
        {
            var fieldInfo = typeof(T).GetField(fieldName, BindingFlags.Instance
                                                  | BindingFlags.NonPublic
                                                  | BindingFlags.Public | BindingFlags.FlattenHierarchy);

            if (fieldInfo == null)
            {
                throw new ArgumentOutOfRangeException("fieldName");
            }

            fieldInfo.SetValue(obj, value);
        }

        /// <summary>
        /// Returns the numbers of listeners to the PropertyChanged event. Usefull to detect memory leaks.
        /// </summary>
        /// <param name="propertyChanged"></param>
        /// <returns></returns>
        public static int GetNumberOfPropertyChangedListeners(object propertyChanged)
        {
            var notifyPropertyChangeField = GetAllFields(propertyChanged.GetType().UnderlyingSystemType,
                                                                           BindingFlags.Instance |
                                                                           BindingFlags.NonPublic)
                .FirstOrDefault(f => f.Name == "~DelftTools.Utils.INotifyPropertyChange");

            var notifyPropertyChangeValue = notifyPropertyChangeField.GetValue(propertyChanged);
            var propertyChangedField =
                GetAllFields(notifyPropertyChangeValue.GetType().UnderlyingSystemType,
                                       BindingFlags.Instance | BindingFlags.NonPublic).FirstOrDefault(
                    f => f.Name == "PropertyChanged");
            var propertyChangedValue = propertyChangedField.GetValue(notifyPropertyChangeValue);

            var invoctionCountProperty = GetAllFields(propertyChangedValue.GetType().UnderlyingSystemType,
                                                                BindingFlags.Instance | BindingFlags.NonPublic)
                .FirstOrDefault(f => f.Name == "_invocationCount");
            var invocationCount = invoctionCountProperty.GetValue(propertyChangedValue);
            //var invocationListMethod = nppc.FirstOrDefault(f => f.Name == "_invocationList");
            //var invocationList = invocationListMethod.GetValue(propertyChangedValue);
            return ((IntPtr)invocationCount).ToInt32();
        }

        public static T CallPrivateMethod<T>(object instance, string methodName, params object[] arguments)
        {
            var type = instance.GetType();
            var methodInfo = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);

            return (T) methodInfo.Invoke(instance, arguments);
        }

        public static void CallPrivateMethod(object instance, string methodName, params object[] arguments)
        {
            var type = instance.GetType();
            var methodInfo = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);

            methodInfo.Invoke(instance, arguments);
        }

        public static IEnumerable<PropertyInfo> GetPublicProperties(Type type)
        {
            return type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
        }

        public static object CallPrivateStaticMethod(Type type, string methodName, params object[] arguments)
        {
            var methodInfo = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);

            return methodInfo.Invoke(null, arguments);
        }

        public static void SetPropertyValue(object instance, string propertyName, object value)
        {
            instance.GetType().GetProperty(propertyName).GetSetMethod().Invoke(instance, new[] { value });
        }
        public static void SetPrivatePropertyValue(object instance, string propertyName, object value)
        {
            instance.GetType().GetProperty(propertyName).SetValue(instance, value, null);
        }

        /// <summary>
        /// Test if the assembly is dynamic using a HACK...rewrite if we have more knowledge
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns></returns>
        public static bool IsDynamic(this Assembly assembly)
        {
            //see http://stackoverflow.com/questions/1423733/how-to-tell-if-a-net-assembly-is-dynamic
            //more nice than depending on an exception..
            return (assembly.ManifestModule.GetType().Namespace == "System.Reflection.Emit");
        }
    }
}