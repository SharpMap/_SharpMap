using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
            // HACK: use another folder for tests
            var stackTrace = new StackTrace(false);
            if (stackTrace.ToString().ToLower().Contains("test."))
            {
                return new AssemblyInfo { Company = "Deltares", Product = "Delta Shell", Version = "Tests Development" };
            }

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

        public static IEnumerable<Stream> GetAssemblyResourceStreams(Assembly assembly, Func<string, bool> resourceNameFilter)
        {
            return from resourceName in assembly.GetManifestResourceNames()
                   where resourceNameFilter(resourceName)
                   select assembly.GetManifestResourceStream(resourceName);
        }

        public static Stream GetAssemblyResourceStream(Assembly assembly, string fileName)
        {
            return
                assembly.GetManifestResourceNames().Where(resourceName => resourceName.EndsWith(fileName)).Select(
                    assembly.GetManifestResourceStream).First();
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

        /// <summary>
        /// This method checks if a file is a managed dll. It's based on how the file command on linux works. <see cref="http://www.darwinsys.com/file/"/> 
        /// </summary>
        /// <param name="path">path of dll</param>
        /// <returns>true if file is a managed dll</returns>
        public static bool IsManagedDll(string path)
        {
            if (!path.EndsWith(".dll", StringComparison.Ordinal) && !path.EndsWith(".exe", StringComparison.Ordinal))
            {
                return false;
            }

            // HACK: skip python dlls somehow they look like .NET assemblies
            if (path.Contains("ic_msvcr90.dll") || path.Contains("python26.dll"))
            {
                return false;
            }

            FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            fs.Seek(0x3C, SeekOrigin.Begin); // PE, SizeOfHeaders starts at 0x3B, second byte is 0x80 for .NET
            int i1 = fs.ReadByte();

            fs.Seek(0x86, SeekOrigin.Begin); // 0x03 for managed code
            int i2 = fs.ReadByte();

            fs.Close();
            
            var isManagedDll = i1 == 0x80 && i2 == 0x03;

            //Debug.WriteLine(path + ": " + (isManagedDll?"managed":"unmanaged"));

            return isManagedDll;
        }

        public static void LoadAllAssembliesFromDirectory(string path)
        {
            foreach (string filename in Directory.GetFiles(path).Where(name => name.EndsWith(".dll")))
            {
                Assembly[] existingAssemblies = AppDomain.CurrentDomain.GetAssemblies();

                if (!AssemblyUtils.IsManagedDll(filename))
                {
                    continue;
                }
                var assemblyName = Path.GetFileNameWithoutExtension(filename);
                if (!existingAssemblies.Any(ass => ass.GetName().Name.Equals(assemblyName)))
                {
                    log.DebugFormat("Loading {0}", filename);

                    try
                    {
                        Assembly.LoadFrom(filename);
                    }
                    catch (Exception exception)
                    {
                        log.ErrorFormat("Could not read assembly information for {0} : {1}", assemblyName, exception.Message);
                    }
                }
            }
        }
    }
}