using System.Collections.Generic;
using System.Linq;
using DelftTools.Shell.Core;
using DelftTools.Shell.Core.Extensions;
using DelftTools.Shell.Core.Workflow;
using DelftTools.Tests.Core.Mocks;
using DelftTools.Units.Generics;
using NUnit.Framework;
using Rhino.Mocks;

namespace DelftTools.Tests.Core
{
    [TestFixture]
    public class ProjectTest
    {
        readonly MockRepository mocks = new MockRepository();

        [Test]
        public void CreateWithDefaultName()
        {
            Project project = CreateEmptyProject();
            Assert.AreEqual(Project.DefaultName, project.Name);
        }

        [Test]
        public void CreateWithCustomName()
        {
            const string projectName = "Test Project";
            Project project = CreateEmptyProjectWithName(projectName);
            Assert.AreEqual(project.Name, projectName);
        }

        [Test]
        public void DefaultData()
        {
            Project project = CreateEmptyProject();
            Assert.AreEqual(project.RootFolder.DataItems.Count(), 0); // the data must be empty for the new projects
        }

        [Test]
        public void AddModel()
        {
            var project = CreateEmptyProject();
            var model = mocks.Stub<IModel>();
            project.RootFolder.Items.Add(model);
            Assert.AreEqual(project.RootFolder.Models.Count(), 1);
        }

        [Test]
        public void ProjectIsChangedAfterAddingAFolder()
        {
            var project = CreateEmptyProject();
            Assert.IsFalse(project.IsChanged);
            project.RootFolder.Items.Add(new Folder());
            Assert.IsTrue(project.IsChanged);
        }

        [Test]
        public void ProjectIsChangedAfterAddingDataItemInAFolder()
        {
            var project = CreateEmptyProject();
            var folder = new Folder();
            project.RootFolder.Items.Add(folder);
            project.IsChanged = false;
            folder.Add(new DataItem());
            Assert.IsTrue(project.IsChanged);
        }

        [Test]
        public void Folders()
        {
            Project project = CreateEmptyProject();
            Folder folder = new Folder();

            project.RootFolder.Items.Add(folder);

            Assert.AreEqual(project.RootFolder.Folders.Count(), 1);
        }

        /// <summary>
        /// Add two string parameters to the project data.
        /// 
        /// <code>
        /// <![CDATA[
        /// <New Project>/ ------ Project
        ///      Data/ ---------- Dataset
        ///         parameter1 -- Parameter<string>, value = "value 1"
        ///         parameter2 -- Parameter<string>, value = "value 2"
        /// ]]>
        /// </code>
        /// </summary>
        [Test]
        public void AddTwoParametersToTheData()
        {
            Project project = CreateEmptyProject();

            // create parameters using parameter constructor
            Parameter<string> parameter1 = new Parameter<string>("parameter1");
            Parameter<string> parameter2 = new Parameter<string>("parameter2");

            // set values
            const string parameter1Value = "value 1";
            const string parameter2Value = "value 2";

            parameter1.Value = parameter1Value;
            parameter2.Value = parameter2Value;

            // add parameters to the project data
            project.RootFolder.Items.Add(new DataItem(parameter1));
            project.RootFolder.Items.Add(new DataItem(parameter2));

            // get data from the project
            IDataItem[] dataItems = project.RootFolder.DataItems.ToArray();
            Parameter<string> expectedParameter1 = dataItems[0].Value as Parameter<string>;
            Parameter<string> expectedParameter2 = dataItems[1].Value as Parameter<string>;

            const int expectedNumberOfDataItems = 2;
            Assert.AreEqual(project.RootFolder.DataItems.Count(), expectedNumberOfDataItems);

            Assert.AreSame(dataItems[0].Value, parameter1);
            Assert.AreSame(dataItems[1].Value, parameter2);
            Assert.AreSame(parameter1, expectedParameter1);
            Assert.AreSame(parameter2, expectedParameter2);

            Assert.AreEqual(parameter1.Value, parameter1Value);
            Assert.AreEqual(parameter2.Value, parameter2Value);
        }

        [Test]
        public void CloneProject()
        {
            Project p = new Project();
            Folder f = new Folder();
            Folder f2 = new Folder();
            f.Add(f2);
            p.RootFolder.Items.Add(f);

            Project p2 = new Project();
            p2.RootFolder.Items.Add((Folder) f.DeepClone());
            Assert.AreEqual(p2.RootFolder.Items.Count, 1);
            Assert.AreEqual(p2.RootFolder.Folders.FirstOrDefault().Items.Count(), 1);
        }
        [Test]
        public void GetItemsRecursive()
        {   
            Project p = Create17ItemsProjectTree();
            
            IEnumerable<object> objects = p.RootFolder.GetAllItemsRecursive();
            IList<object> list = objects.ToList();

            Assert.AreEqual(17, list.Count);
        }


        [Test]
        public void CloneModelOfProject()
        {
            Project p = Create17ItemsProjectTree();

            var model = (TimeDependentModelBase)p.RootFolder.Models.First().DeepClone();

            Assert.AreEqual(5, model.DataItems.Count);
        }


        [Test]
        public void GetItemsRecursiveShouldReturnModelsContainedByCompositeMOdel()
        {
            var model = new CompositeModel();
            var model1 = new SimplerModel();
            model.Models.Add(model1);

            Project project = new Project();
            project.RootFolder.Add(model);
            Assert.IsTrue(project.GetAllItemsRecursive().Contains(model1));
        }

        [Test]
        public void GetItemsRecursiveShouldOfTypeDataItem()
        {
            var project = Create17ItemsProjectTree();
            Assert.AreEqual(8, project.GetAllItemsRecursive().OfType<IDataItem>().Count());
        }

        [Test]
        public void RemoveExternalLinks()
        {
            var item = new DataItem("item");
            var sourceItem = new DataItem("source");

            item.LinkTo(sourceItem);
            item.GetAllItemsRecursive().OfType<IDataItem>().RemoveOuterDataItemLinks();
            Assert.IsFalse(item.IsLinked);
        }

        [Test]
        public void RemoveExternalLinksAndKeepInternalLinks()
        {
            var folder = new Folder("folder");
            var internal1 = new DataItem("internal1");
            folder.Items.Add(internal1);
            var internal2 = new DataItem("internal2");
            folder.Items.Add(internal2);

            var external = new DataItem("external2");

            //internalLink
            internal2.LinkTo(internal1);
            //externalLink
            internal1.LinkTo(external);

            folder.GetAllItemsRecursive().OfType<IDataItem>().RemoveOuterDataItemLinks();

            Assert.IsFalse(internal1.IsLinked);
            Assert.IsTrue(internal2.IsLinked);
        }



        #region Private Methods

         private static Project Create17ItemsProjectTree()
         {
             //   RootFolder
             //      |-DataItem1
             //          |-DataItem1Value
             //      |-Folder 1
             //          |-DataItemSet1
             //              |-DataItem2
             //                  |-string
             //      |-Model
             //          |-DataItem3
             //          |-StartTime
             //              |-StartTimeValue
             //          |-StopTime
             //              |-StopTimeValue
             //          |-CurrentTime
             //              |-CurrentTimeValue
             //          |-TimeStep
             //              |-TimeStepValue

             Project p = new Project();
             var folder = new Folder("folder1");
             p.RootFolder.Items.Add(new DataItem { Value = "dataItem1Value" });
             var set = new DataItemSet("dataItemSet1");
             var dataItem2 = new DataItem { Name = "DataItem2" };
             dataItem2.Value = "string";
             set.Add(dataItem2);

             //TODO : replace simplermodel with a mock/stubbed object
             SimplerModel model = new SimplerModel();
             model.DataItems.Add(new DataItem() { Name = "DataItem3" });
             folder.Items.Add(set);

             p.RootFolder.Add(folder);
             p.RootFolder.Add(model);
             return p;
         }

        private static Project CreateEmptyProject()
        {
            return new Project();
        }

        private static Project CreateEmptyProjectWithName(string projectName)
        {
            return new Project(projectName);
        }

        #endregion


        }
}