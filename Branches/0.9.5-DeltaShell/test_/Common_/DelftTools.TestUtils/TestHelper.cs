using System;
using System.Diagnostics;
using System.IO;
using System.Xml.Linq;
using DelftTools.Functions;
using DelftTools.Functions.Filters;
using DelftTools.Functions.Generic;
using DelftTools.Utils.IO;
using log4net;
using NUnit.Framework;

namespace DelftTools.TestUtils
{
    public class TestCategory
    {
        public const string Integration = "Integration";
        public const string DataAccess = "DataAccess";
        public const string WindowsForms = "Windows.Forms";
        public const string Performance = "Performance";
        public const string Failing = "Failing";
        public const string Jira = "JIRA";
    }
    public class TestHelper
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(TestHelper));

        public static string GetCurrentMethodName()
        {
            var callingMethod = new StackFrame(1, false).GetMethod(); //.Name;
            return callingMethod.DeclaringType.Name + "." + callingMethod.Name;
            //return "";
        }

        /// <summary>
        /// Does an XML compare based on the xml documents. 
        /// TODO: get a more precise assert.
        /// </summary>
        /// <param name="xml1"></param>
        /// <param name="xml2"></param>
        /// <returns></returns>
        public static void AssertXmlEquals(string xml1, string xml2)
        {
            //TODO: get a nicer assert with more info about what is different.
            /*
             * does not work on the build server :( */
            XDocument doc1 = XDocument.Parse(xml1);
            XDocument doc2 = XDocument.Parse(xml2);
            //XmlUnit.XmlAssertion.AssertXmlEquals(xml1,xml2);
            Assert.IsTrue(XNode.DeepEquals(doc1, doc2));


            //do a string compare for now..not the issue the issue is in datetime conversion
            //Assert.AreEqual(xml1,xml2);
        }

        //TODO: get this near functiontest. This is not a general function.
        public static IFunction CreateSimpleFunction(IFunctionStore store)
        {
            var function = new Function("test");

            store.Functions.Add(function);

            // initialize schema
            IVariable x = new Variable<double>("x", 3);
            IVariable y = new Variable<double>("y", 2);
            IVariable f1 = new Variable<double>("f1");

            function.Arguments.Add(x);
            function.Arguments.Add(y);
            function.Components.Add(f1);


            // write some data
            var xValues = new double[] {0, 1, 2};
            var yValues = new double[] {0, 1};
            var fValues = new double[] {100, 101, 102, 103, 104, 105};

            function.SetValues(fValues,
                               new VariableValueFilter<double>(x, xValues),
                               new VariableValueFilter<double>(y, yValues),
                               new ComponentFilter(f1));
            return function;
        }

        /// <summary>
        /// Writes an xml file for the given content. Gives a 'nice' layout
        /// </summary>
        /// <param name="path"></param>
        /// <param name="xml"></param>
        public static void WriteXml(string path, string xml)
        {
            /*XDocument doc = XDocument.Parse(xml);
            doc.Save(path);
             */
            //todo: get the formatting nice again. Now 
            File.WriteAllText(path, xml);
        }

        /// <summary>
        /// Returns full path to the file or directory in "test-data"
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetTestDataPath(string path)
        {
            return Path.Combine(TestDataDirectory, path);
        }

        /// <summary>
        /// Returns full path to the file or directory in "test-data"
        /// </summary>
        /// <param name="directoryPath"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetTestDataPath(string directoryPath, string path)
        {
            return Path.Combine(Path.Combine(TestDataDirectory, directoryPath), path);
        }

        private static string solutionRoot;
        

        public static string SolutionRoot
        {
            get
            {
                if (solutionRoot == null)
                {
                    solutionRoot = GetSolutionRoot();
                }
                return solutionRoot;
            }
        }

        private static string GetSolutionRoot()
        {
            const string solutionName = "DeltaShell-Spatial.sln";
            //get the current directory and scope up
            //TODO find a faster safer method 
            string curDir = ".";
            while (Directory.Exists(curDir) && !File.Exists(curDir + @"\"+solutionName))
            {
                curDir += "/../";
            }
                
            if (!File.Exists(Path.Combine(curDir, solutionName)))
                throw new InvalidOperationException("Solution file not found.");
             
            return Path.GetFullPath(curDir);
        }


        
        public static string GetDataDir()
        {
            //string solutionRoot = SolutionRoot;
            string testPath = Path.GetFullPath(@"..\..");
            var testRelativepath = FileUtils.GetRelativePath(SolutionRoot + "test", testPath);
            return Path.Combine(solutionRoot + @"test-data\", testRelativepath) + Path.DirectorySeparatorChar;
            
        }



        public static string TestDataDirectory
        {
            get
            {
                
                var testDataPath = SolutionRoot + @"\test-data\";
                return Path.GetDirectoryName(testDataPath);
            }
        }
        /// <summary>
        /// Get's the path in test-data tree section
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static string GetTestFilePath(string filename)
        {
            var path = Path.Combine(GetDataDir(), filename);
            UriBuilder uri = new UriBuilder(path);
            return Uri.UnescapeDataString(uri.Path);
        }

        public static void AssertIsFasterThan(int maxMilliSeconds, Action action)
        {
            DateTime startTime = DateTime.Now;
            action();
            var actualMillisecond = (DateTime.Now-startTime).TotalMilliseconds;
            
            Assert.IsTrue(actualMillisecond <  maxMilliSeconds,"Maximum of {0} milliseconds exceeded. Actual was {1}",maxMilliSeconds,actualMillisecond);
            log.DebugFormat("Test took {1} milliseconds. Maximum was {0}",maxMilliSeconds,actualMillisecond);
        }
    }
    
}