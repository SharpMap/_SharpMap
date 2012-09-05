using System;
using System.Collections.Generic;
using System.Reflection;
using log4net;

namespace DelftTools.Utils.Reflection
{
    /// <summary>
    /// Utility class containing functions for retrieving specific information about a .net assembly.
    /// </summary>
    public static class AssemblyUtils
    {
        private static readonly ILog log = LogManager.GetLogger(typeof (AssemblyUtils));

        /// <summary>
        /// Return attributes for a specific assembly
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns></returns>
        public static AssemblyInfo GetAssemblyInfo(Assembly assembly)
        {
            AssemblyInfo info = new AssemblyInfo();

            if(assembly.Location == "")
            {
                return info;
            }

            info.Version = assembly.GetName().Version.ToString();

            var assemblyTitleAttribute = GetAssemblyAttributeValue<AssemblyTitleAttribute>(assembly);
            if (assemblyTitleAttribute != null)
            {
                info.Title = assemblyTitleAttribute.Title;
            }
            
            var assemblyDescriptionAttribute = GetAssemblyAttributeValue<AssemblyDescriptionAttribute>(assembly);
            if(assemblyDescriptionAttribute != null)
            {
                info.Description = assemblyDescriptionAttribute.Description;
            }

            var assemblyProductAttribute = GetAssemblyAttributeValue<AssemblyProductAttribute>(assembly);
            if (assemblyProductAttribute != null)
            {
                info.Product = assemblyProductAttribute.Product;
            }

            var assemblyCopyrightAttribute = GetAssemblyAttributeValue<AssemblyCopyrightAttribute>(assembly);
            if (assemblyCopyrightAttribute != null)
            {
                info.Copyright = assemblyCopyrightAttribute.Copyright;
            }

            var assemblyCompanyAttribute = GetAssemblyAttributeValue<AssemblyCompanyAttribute>(assembly);
            if (assemblyCompanyAttribute != null)
            {
                info.Company = assemblyCompanyAttribute.Company;
            }

            return info;
        }

        /// <summary>
        /// Return attributes for the executing assembly
        /// </summary>
        /// <returns></returns>
        public static AssemblyInfo GetExecutingAssemblyInfo()
        {
            return GetAssemblyInfo(Assembly.GetExecutingAssembly());
        }

        private static T GetAssemblyAttributeValue<T>(Assembly assembly) where T : class
        {
            object[] attributes = assembly.GetCustomAttributes(typeof (T), true);

            if (attributes.Length == 0)
            {
                return null;
            }

            return (T) attributes[0];
        }

        /// <summary>
        /// Gets types in a given assembly derived from a given type.
        /// </summary>
        /// <param name="baseType"></param>
        /// <param name="assembly"></param>
        /// <returns></returns>
        public static IList<Type> GetDerivedTypes(Type baseType, Assembly assembly)
        {
            List<Type> types = new List<Type>();

            log.Debug(assembly.ToString());
            try
            {
                foreach (Type t in assembly.GetTypes())
                {
                    if (t.IsSubclassOf(baseType))
                    {
                        types.Add(t);
                    }
                    else if (t.GetInterface(baseType.ToString()) != null)
                    {
                        types.Add(t);
                    }
                }
            }
            catch (Exception e) //hack because of unregistered ocx files TOOLS-518
            {
                log.Debug(e);
            }

            return types;
        }

        /// <summary>
        /// Gets types derived from a given type. Searches all assemblies in a current application domain.
        /// </summary>
        /// <param name="baseType"></param>
        /// <returns></returns>
        public static IList<Type> GetDerivedTypes(Type baseType)
        {
            List<Type> types = new List<Type>();
            IList<Assembly> assemblies = new List<Assembly>(AppDomain.CurrentDomain.GetAssemblies());

            foreach (Assembly a in assemblies)
            {
                types.AddRange(GetDerivedTypes(baseType, a));
            }

            return types;
        }

        /// <summary>
        /// Returns the type based on the full type name. Throws an exception if the types
        /// is found in multiple assemblies
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public static Type GetTypeByName(string typeName)
        {
            Type result = null;
            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type foundType = a.GetType(typeName);
                if (foundType != null)
                    if (result == null)
                        result = foundType;
                    else
                        throw new Exception("Type found in multiple assemblies");
            }
            return result;
        }

        /// <summary>
        /// Returns the version for the assembly from which this method is called
        /// </summary>
        /// <returns></returns>
        public static string GetAssemblyVersion()
        {
            return Assembly.GetCallingAssembly().GetName().Version.ToString();
        }

        #region Nested type: AssemblyInfo

        /// <summary>
        /// structure containing assembly attributes as strings.
        /// </summary>
        [Serializable]
        public struct AssemblyInfo
        {
            public string Company;
            public string Copyright;
            public string Description;
            public string Product;
            public string Title;
            public string Version;
        }

        #endregion
    }
}