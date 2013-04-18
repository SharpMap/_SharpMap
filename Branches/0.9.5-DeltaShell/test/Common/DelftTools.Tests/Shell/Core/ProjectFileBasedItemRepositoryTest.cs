using System;
using System.IO;
using System.Linq;
using DelftTools.Shell.Core;
using DelftTools.Shell.Core.Dao;
using DelftTools.TestUtils;
using DelftTools.Utils.Aop.NotifyPropertyChange;
using DelftTools.Utils.IO;
using DelftTools.Utils;
using NUnit.Framework;
using Rhino.Mocks;

namespace DelftTools.Tests.Shell.Core
{
    [TestFixture]
    public class ProjectFileBasedItemRepositoryTest
    {
        private readonly MockRepository mocks = new MockRepository();
        
        [Test]
        public void NewFilePathOnItemAdd()
        {
            var dataDirectoryPath = TestHelper.GetCurrentMethodName() + "_data";
            var project = new Project();

            var repository = new ProjectFileBasedItemRepository();
            repository.Initialize(project, dataDirectoryPath);

            IFileBased fileBased;
            using(mocks.Record())
            {
                fileBased = mocks.Stub<IFileBased>();
                fileBased.Expect(fb => fb.Close()).Repeat.Once();
                fileBased.Expect(fb => fb.CreateNew(null)).Repeat.Once().IgnoreArguments()
                    .Do(new Action<string>(delegate(string path) { fileBased.Path = path; }));
            }

            using(mocks.Playback())
            {
                // add file based item to repository
                project.RootFolder.Add(fileBased);

                fileBased.Path
                    .Should("new file path is generated").Not.Be.NullOrEmpty();

                fileBased.Path.StartsWith(dataDirectoryPath)
                    .Should("data directory path is used as a prefix of new generated path").Be.True();

                File.Exists(fileBased.Path)
                    .Should("repository does not create file, only sets path").Be.False();

                repository.Close();
            }
        }

        [Test]
        public void DeleteOnItemRemove()
        {
            var dataDirectoryPath = TestHelper.GetCurrentMethodName() + "_data";
            var project = new Project();

            var repository = new ProjectFileBasedItemRepository();
            repository.Initialize(project, dataDirectoryPath);

            var fileBased = mocks.Stub<IFileBased>();

            using (mocks.Record())
            {
                fileBased.Expect(fb => fb.CreateNew(null)).Repeat.Once().IgnoreArguments()
                    .Do(new Action<string>(delegate(string path) { fileBased.Path = path; }));
                fileBased.Expect(fb => fb.IsOpen).Return(true);
                fileBased.Expect(fb => fb.Close()).Repeat.Once();
                fileBased.Expect(fb => fb.Delete()).Repeat.Once(); // <- files should be deleted after Remove and Close are called
            }

            using (mocks.Playback())
            {
                // add file based item to repository (will create a new path in repository since path is empty)
                project.RootFolder.Add(fileBased);

                // and now delete
                var fileBasedDataItem = project.RootFolder.DataItems.First();
                project.RootFolder.Items.Remove(fileBasedDataItem);

                repository.Close();
            }
        }

        /// <summary>
        /// E.g. happens during drag and drop.
        /// </summary>
        [Test]
        public void NoDeleteOnItemRemoveAndAddForEditableObject()
        {
            IFileBased fileBased;
            using (mocks.Record())
            {
                fileBased = mocks.Stub<IFileBased>();
                fileBased.Expect(fb => fb.CreateNew(null)).IgnoreArguments().Repeat.Once()
                    .Do(new Action<string>(delegate(string path) { fileBased.Path = path; }));
                fileBased.Expect(fb => fb.Close()).Repeat.Once();
                fileBased.Expect(fb => fb.Delete()).Repeat.Never();
            }

            var dataDirectoryPath = TestHelper.GetCurrentMethodName() + "_data";
            var project = new Project();

            var repository = new ProjectFileBasedItemRepository();
            repository.Initialize(project, dataDirectoryPath);

            using (mocks.Playback())
            {
                // add file based item to repository
                project.RootFolder.Add(fileBased);

                // now remove it and add back
                project.BeginEdit("Moving item ...");

                // delete
                var fileBasedDataItem = project.RootFolder.DataItems.First();
                project.RootFolder.Items.Remove(fileBasedDataItem);

                // add it back, thus cancelling delete
                project.RootFolder.Items.Add(fileBasedDataItem);

                project.EndEdit();

                repository.Close();
            }
        }

        [Test]
        public void DeleteFilesWhenEditActionIsCancelled()
        {
            IFileBased fileBased;
            using (mocks.Record())
            {
                fileBased = mocks.Stub<IFileBased>();
                fileBased.Expect(fb => fb.CreateNew(null)).IgnoreArguments().Repeat.Once()
                    .Do(new Action<string>(delegate(string path) { fileBased.Path = path; }));
                fileBased.Expect(fb => fb.IsOpen).Repeat.Once().Return(true);
                fileBased.Expect(fb => fb.Close()).Repeat.Once();
                fileBased.Expect(fb => fb.Delete()).Repeat.Once();
            }

            var dataDirectoryPath = TestHelper.GetCurrentMethodName() + "_data";
            var project = new Project();

            var repository = new ProjectFileBasedItemRepository();
            repository.Initialize(project, dataDirectoryPath);

            using (mocks.Playback())
            {
                // now remove it and add back
                project.BeginEdit("Moving item ...");

                // add file based item to repository
                project.RootFolder.Add(fileBased);

                project.CancelEdit();
            }
        }

        [NotifyPropertyChange]
        private class ClassContainingFileBasedProperty
        {
            public IFileBased FileBased { get; set; }
        }

        [Test]
        public void SettingPropertyOfTypeFileBasedCreateNewPath()
        {
            var dataDirectoryPath = TestHelper.GetCurrentMethodName() + "_data";
            var project = new Project();

            var repository = new ProjectFileBasedItemRepository();
            repository.Initialize(project, dataDirectoryPath);

            // add empty data item and set fileBased item to it.
            var container = new ClassContainingFileBasedProperty();
            project.RootFolder.Add(container);

            IFileBased fileBased;
            using (mocks.Record())
            {
                fileBased = mocks.Stub<IFileBased>();
                fileBased.Expect(fb => fb.CreateNew(null)).Repeat.Once().IgnoreArguments()
                    .Do(new Action<string>(delegate(string path) { fileBased.Path = path; }));
            }

            using (mocks.Playback())
            {
                container.FileBased = fileBased; // <- triggers new Path generation since it is empty
            }

            string.IsNullOrEmpty(fileBased.Path)
                .Should("new path should be generated for an item after it is added to the project").Be.False();

            repository.Close();
        }

        [Test]
        public void SettingPropertyOfTypeFileBasedDoesNotCreateNewIfItemIsExternal()
        {
            var dataDirectoryPath = TestHelper.GetCurrentMethodName() + "_data";
            var project = new Project();

            var repository = new ProjectFileBasedItemRepository();
            repository.Initialize(project, dataDirectoryPath);

            // add empty data item and set fileBased item to it.
            var container = new ClassContainingFileBasedProperty();
            project.RootFolder.Add(container);

            IFileBased fileBased;
            using (mocks.Record())
            {
                fileBased = mocks.Stub<IFileBased>();
                fileBased.Path = "some_external_path";
            }

            using (mocks.Playback())
            {
                container.FileBased = fileBased; // <- triggers new Path generation since it is empty
            }

            repository.Close();

            fileBased.Path
                .Should("Path does not change if item has external path").Be.EqualTo("some_external_path");
        }

        [Test]
        public void CloseShouldCloseAllItems()
        {
            var dataDirectoryPath = TestHelper.GetCurrentMethodName() + "_data";
            var project = new Project();

            var repository = new ProjectFileBasedItemRepository();
            repository.Initialize(project, dataDirectoryPath);

            var fileBased = mocks.Stub<IFileBased>();

            using (mocks.Record())
            {
                fileBased.Expect(fb => fb.CreateNew(null)).Repeat.Once().IgnoreArguments();
                fileBased.Expect(fb => fb.Close()).Repeat.Once();
            }

            project.RootFolder.Add(fileBased);

            using (mocks.Playback())
            {
                repository.Close();
            }
        }

        [Test]
        public void SwitchToSwitchesAllItems()
        {
            var dataDirectoryPath = TestHelper.GetCurrentMethodName() + "_data";
            var dataDirectoryPath2 = TestHelper.GetCurrentMethodName() + "_data2";
            var project = new Project();

            var repository = new ProjectFileBasedItemRepository();
            repository.Initialize(project, dataDirectoryPath);

            var fileBased = mocks.Stub<IFileBased>();

            using (mocks.Record())
            {
                
                fileBased.Expect(fb => fb.CreateNew(null)).Repeat.Once().IgnoreArguments()
                    .Message("new item with unique path is created on add")
                    .Do(new Action<string>(delegate(string path) { fileBased.Path = path; }));

                fileBased.Expect(fb => fb.SwitchTo(null)).Repeat.Once().IgnoreArguments()
                    .Message("item is switched to a new path")
                    .Do(new Action<string>(delegate(string path)
                                               {
                                                   path
                                                       .Should("switched to a new data dir")
                                                           .StartWith(dataDirectoryPath2);
                                                   
                                                   Path.GetFileName(path)
                                                       .Should("file name does not change")
                                                            .Be.EqualTo(Path.GetFileName(fileBased.Path));
                                               }));
            }

            using (mocks.Playback())
            {
                // add file based item to repository (will create a new path in repository since path is empty)
                project.RootFolder.Add(fileBased);

                // switch repository to a new folder
                repository.SwitchTo(dataDirectoryPath2);
            }
        }



        /*        [Test]
                public void SaveProjectAsShouldResetFileBasedPathIfTheyAreInTheProjectDirectory()
                {
                    var path = TestHelper.GetCurrentMethodName() + "myproject.dsproj";
                    var project = new Project("project") { IsChanged = true };
                    project.RootFolder.Add(new DataItem(fileBased));

                    Expect.Call(repository.Path).Repeat.Any().Return(path);
                    Expect.Call(delegate { repository.SaveOrUpdate(project); }).Repeat.Any();
                    Expect.Call(repository.GetProject()).Repeat.Any().Return(project);
                    Expect.Call(delegate { repository.SaveAs(null, null); }).IgnoreArguments();

                    Expect.Call(fileBased.Path).Repeat.Any().Return(TestHelper.GetCurrentMethodName() + "myproject.dsproj_data\\file.nc");
                    Expect.Call(fileBased.Path = TestHelper.GetCurrentMethodName() + "myproject.dsproj_data\\file.nc");
                    Expect.Call(delegate { fileBased.ReConnect(); }).Repeat.Any();

                    var projectFileBasedItemRepository = mocks.Stub<IProjectFileBasedItemRepository>();

                    mocks.ReplayAll();

                    var projectService = new ProjectService(factory) { ProjectFileBasedItemRepository = projectFileBasedItemRepository };

                    projectService.SaveProjectAs(project, "_copy" + path);

                    mocks.VerifyAll();
                    Assert.IsFalse(project.IsChanged);
                }

                [Test]
                public void SaveProjectAsShouldNotResetFileBasedPathIfNotInTheProjectDirectory()
                {
                    var path = Path.Combine(TestHelper.GetCurrentMethodName(), "myproject.dsproj");
                    var project = new Project("project") { IsChanged = true };
                    project.RootFolder.Add(new DataItem(fileBased));

                    Expect.Call(repository.Path).Repeat.Any().Return(path);
                    Expect.Call(delegate { repository.SaveOrUpdate(project); }).Repeat.Any();
                    Expect.Call(repository.GetProject()).Repeat.Any().Return(project);
                    Expect.Call(delegate { repository.SaveAs(null, null); }).IgnoreArguments();

                    Expect.Call(fileBased.Path).Repeat.Any().Return("E:\\NotInTemp\\myproject.dsproj_data\\file.nc");

                    var projectFileBasedItemRepository = mocks.Stub<IProjectFileBasedItemRepository>();

                    mocks.ReplayAll();

                    var projectService = new ProjectService(factory) { ProjectFileBasedItemRepository = projectFileBasedItemRepository };

                    projectService.SaveProjectAs(project, path + "copy");

                    mocks.VerifyAll();
                    Assert.IsFalse(project.IsChanged);
                }
 
                 [Test]
                public void CreateShouldCreateDirectoryForFileBasedItems()
                {
                    var path = TestHelper.GetCurrentMethodName() + ".dsproj";

                    Expect.Call(delegate { repository.Create(path); });
                    Expect.Call(repository.GetProject()).Repeat.Once().Return(new Project());

                    Expect.Call(repository.Path).Repeat.Any().Return(path);

                    mocks.ReplayAll();

                    var projectService = new ProjectService(factory);
                    var project = projectService.Create(path);
                    var dirPath = projectService.ProjectDataDirectory;

                    mocks.VerifyAll();

                    Assert.IsFalse(project.IsTemporary);
                    Assert.IsTrue(Directory.Exists(dirPath));
                    Assert.AreEqual(dirPath, path + "_data");

                    FileUtils.DeleteDirectoryIfExists(dirPath);
                }

        */
    }
}