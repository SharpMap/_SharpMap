using System;
using System.Diagnostics;
using System.IO;
using DelftTools.Utils.IO;
using NUnit.Framework;

namespace DelftTools.Tests.Utils.IO
{
    /// <summary/>
    [TestFixture]
    public class FileUtilsTest
    {
        [Test]
        public void isSubDirectoryTest()
        {
            const string rootDir = "D:/Habitat";

            Assert.IsFalse(FileUtils.IsSubdirectory(rootDir, "D:/Habitat Kaarten/"));

            Assert.IsTrue(FileUtils.IsSubdirectory(rootDir, "D:/Habitat/Kaarten/"));

            Assert.IsTrue(
                FileUtils.IsSubdirectory(rootDir, "D:/Habitat/Natura2000 presentation 3.prj_data/109/kaart.bil"));

            Assert.IsFalse(FileUtils.IsSubdirectory(rootDir, "C:/Habitat/Kaarten/"));

            Assert.IsTrue(FileUtils.IsSubdirectory(rootDir, "D:/habitat/kaarten/"));
        }

        [Test]
        public void ConvertToRelativePath()
        {
            FileInfo info = new FileInfo("./data/myfile.myext");
            string fullpath = @"c:\Project\data\myfile.myext";
            string projectpath = @"c:\project";

            string result = FileUtils.GetRelativePath(projectpath, fullpath);
            FileInfo resultFile = new FileInfo(result);
            Assert.AreEqual(info.FullName, resultFile.FullName);

            fullpath = @"c:\project\data\myfile.myext";
            projectpath = "c:/project";
            result = FileUtils.GetRelativePath(projectpath, fullpath);
            resultFile = new FileInfo(result);
            Assert.AreEqual(info.FullName, resultFile.FullName);
        }

        [Test]
        public void CompareDirectories()
        {
            Assert.IsTrue(FileUtils.CompareDirectories(@"./data/myfile.myext", @"data\myfile.myext"));
        }

        //Todo Bad design here (http://forums.whirlpool.net.au/forum-replies-archive.cfm/887699.html)
        [Test]
        public void CanCopy()
        {
            //create file
            try
            {
                FileInfo fi = new FileInfo("CanCopy.txt");
                fi.Create();
                Assert.IsTrue(FileUtils.CanCopy(fi.Name, Path.GetTempFileName()));
                fi.Delete();
            }
            catch(IOException e)
            {
                Debug.WriteLine(e);
            }
        }

        [Test]
        public void GetUniqueFileNameWithPathReturnsCorrectFilePath()
        {
            const string someFileName = "somefile.nc";

            using (File.Create(someFileName)) { }

            var path = Path.GetFullPath(someFileName).Replace(someFileName, "");

            string newName = FileUtils.GetUniqueFileNameWithPath(Path.Combine(path, someFileName));
            Assert.AreEqual(Path.Combine(path, "somefile (1).nc"), newName);

            File.Delete(someFileName);

        }

        [Test]
        public void GetUniqueFileNameReturnsTheSameNameWhenNoFileIsFound()
        {
            const string someFileName = "somefile.nc";
            string newName = FileUtils.GetUniqueFileName(someFileName);
        }

        [Test]
        public void GetUniqueFileNameReturnsNewNameBasedOnFilesFound()
        {
            const string someFileName = "somefile.nc";

            using (File.Create(someFileName)) { }

            string newName = FileUtils.GetUniqueFileName(someFileName);
            Assert.AreEqual("somefile (1).nc", newName);                

            File.Delete(someFileName);
        }


        [Test]
        public void GetUniqueFileNameReturnsNewNameBasedOnMultipleFilesFound()
        {
            const string someFileName0 = "somefile.nc";
            using (File.Create(someFileName0)) { }

            const string someFileName1 = "somefile (1).nc";
            using (File.Create(someFileName1)) {}

            string newName = FileUtils.GetUniqueFileName(someFileName0);
            Assert.AreEqual("somefile (2).nc", newName);

            File.Delete(someFileName0);
            File.Delete(someFileName1);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void GetUniqueFileNameThrowsArgumentNullExceptionOnNullArgument()
        {
            string newName = FileUtils.GetUniqueFileName(null);
        }

        [Test]
        public void DeleteFileIfItExists()
        {
            // no error if it does not exist
            FileUtils.DeleteIfExists("somefile.nc");

            File.Create("somefile.nc").Close();
            FileUtils.DeleteIfExists("somefile.nc");
            Assert.IsFalse(File.Exists("somefile.nc"));
        }

        [Test]
        public void DeleteEmptyDirectoryIfItExists()
        {
            // no error if it does not exist
            FileUtils.DeleteDirectoryIfExists("mydir");

            FileUtils.CreateDirectoryIfNotExists("mydir");
            FileUtils.DeleteDirectoryIfExists("mydir");
            Assert.IsFalse(Directory.Exists("mydir"));
        }

        [Test]
        public void DeleteNonEmptyDirectoryIfItExists()
        {
            FileUtils.CreateDirectoryIfNotExists("mydir");
            FileUtils.CreateDirectoryIfNotExists("mydir/subdir");
            File.Create("mydir/somefile.nc").Close();

            FileUtils.DeleteDirectoryRecursivelyIfExists("mydir");
            Assert.IsFalse(Directory.Exists("mydir"));
        }
    }
}