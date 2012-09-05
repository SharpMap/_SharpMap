using System;
using System.IO;
using DelftTools.Shell.Core;
using DelftTools.Shell.Core.Extensions;
using DelftTools.TestUtils;
using DelftTools.Utils.IO;
using NUnit.Framework;
using Rhino.Mocks;

namespace DelftTools.Tests.Utils.IO
{
    [TestFixture]
    public class ProjectFileUtilsTest
    {
        private IFileBased fileBased;
        private MockRepository mocks;

        [SetUp]
        public void SetUpMocks()
        {
            mocks = new MockRepository();
            fileBased = mocks.StrictMock<IFileBased>();
        }
        
        [Test]
        [Ignore("Should work if test has been soved: NHibernateProjectRepositoryTest.ProjectSaveAsAndRemoveFile")]
        public void DeleteProject()
        {
/*
            var path = TestHelper.GetCurrentMethodName() + ".dsproj";
            var dir = path + "_data";
            var project = new Project();
            project.RootFolder.Add(new DataItem(fileBased));

            File.Create(path).Close();
            Directory.CreateDirectory(dir);

            Expect.Call(fileBased.Close);

            mocks.ReplayAll();

            ProjectFileUtils.DeleteProject(project,path,dir);

            mocks.VerifyAll();

            Assert.IsFalse(File.Exists(path));
            Assert.IsFalse(Directory.Exists(dir));
*/
        }



    }
}
