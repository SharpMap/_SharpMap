using System;
using System.IO;
using System.Linq;
using System.Reflection;
using DelftTools.Utils.Reflection;

namespace DelftTools.Utils
{
    public static class SettingsHelper
    {
        static SettingsHelper()
        {
            //set defaults based on executing assembly
            var info = AssemblyUtils.GetExecutingAssemblyInfo();
            ApplicationName = info.Product;
            ApplicationVersion = info.Version;
        }
        public static string ApplicationNameAndVersion
        {
            get { return ApplicationName + " " + ApplicationVersion; }
        }

        public static string ApplicationName{ get; set; }
        public static string ApplicationVersion { get; set; }

        public static string GetApplicationLocalUserSettingsDirectory()
        {
            var localSettingsDirectoryPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            
            var executingAssembly = Assembly.GetExecutingAssembly();
            var assemblyInfo = AssemblyUtils.GetAssemblyInfo(executingAssembly);
            var companySettingsDirectoryPath = Path.Combine(localSettingsDirectoryPath, assemblyInfo.Company);

            var applicationVersionDirectory = ApplicationName + "-" + ApplicationVersion; 

            var appSettingsDirectoryPath = Path.Combine(companySettingsDirectoryPath, applicationVersionDirectory);

            if (!Directory.Exists(appSettingsDirectoryPath))
            {
                Directory.CreateDirectory(appSettingsDirectoryPath);
            }

            return appSettingsDirectoryPath;
        }
    }
}