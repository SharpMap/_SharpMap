using System;
using System.Text.RegularExpressions;
using System.Xml;

namespace DelftTools.Utils
{
    public class MappingDocument
    {
        private static string fileVersionRegex = @"(?<mappingname>.*?)\.(?<version>(\d+\.??)*)?\.?hbm\.xml";

        public MappingDocument(string fileName, XmlDocument xmlDocument)
        {
            FileName = fileName;
            XmlDocument = xmlDocument;
        }

        public string FileName { get; private set; }
        public XmlDocument XmlDocument { get; private set; }

        /// <summary>
        /// Root name of mapping. For example weir.1.2.3.hbm.xml -> weir
        /// </summary>
        public string RootName
        {
            get 
            {
                var regex = new Regex(fileVersionRegex);
                var match = regex.Match(FileName);
                return match.Groups["mappingname"].Value;
            }
        }

        public Version Version
        {
            get 
            {
                var regex = new Regex(fileVersionRegex);
                var match = regex.Match(FileName);
                var version = match.Groups["version"].Value;
                return String.IsNullOrEmpty(version) ? null : new Version(version).GetFullVersion();
            }
        }

        
    }
}