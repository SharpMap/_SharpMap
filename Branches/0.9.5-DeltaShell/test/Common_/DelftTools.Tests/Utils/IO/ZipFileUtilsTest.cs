using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using DelftTools.TestUtils;
using DelftTools.Utils.IO;
using NUnit.Framework;

namespace DelftTools.Tests.Utils.IO
{
    [TestFixture]
    public class ZipFileUtilsTest
    {
        [Test,Category("DataAccess")]
        public void ExtractFromZipFile()
        {
            File.Delete("LogoNLMW.xml");
            ZipFileUtils.Extract(@"..\..\..\..\Data\ZipFile\metadata.zip",".");
            Assert.IsTrue(File.Exists("LogoNLMW.xml"));
        }

        [Test,Category("DataAccess")]
        public void AddToZipFile()
        {
            string textpath = TestHelper.GetCurrentMethodName() + ".txt";
            using(TextWriter writer = new StreamWriter(textpath))
            {
                writer.Write("silly text");
            }

            string textpath2 = TestHelper.GetCurrentMethodName() + "1.txt";
            File.Copy(textpath, textpath2, true);
            string zipFilePath = TestHelper.GetCurrentMethodName()+".zip";
            if (File.Exists(zipFilePath))
            {
                File.Delete(zipFilePath);
            }
            Assert.IsTrue(ZipFileUtils.AddFileToOrReplaceFileInZipFile(Path.GetFullPath(textpath), Path.GetFullPath(zipFilePath)));
            Assert.IsTrue(ZipFileUtils.AddFileToOrReplaceFileInZipFile(textpath2, zipFilePath));
            
        }
    }
}
