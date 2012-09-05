using System;
using System.Collections.Generic;
using System.Linq;
using DelftTools.Shell.Core;
using DelftTools.Shell.Core.Workflow;
using DelftTools.Utils;
using DelftTools.Utils.Collections.Generic;
using NUnit.Framework;
using Rhino.Mocks;

namespace DelftTools.Tests.Core
{
    [TestFixture]
    public class FolderTest
    {
        readonly MockRepository mocks = new MockRepository();

        [Test]
        public void Clone()
        {
            Folder f = new Folder();
            const string folderName = "test1";
            f.Name = folderName;
            Folder f2 = (Folder) f.DeepClone();
            Assert.AreEqual(f2.Name, f.Name);
        }

        /// <summary>
        /// When list of dataitems is assigned to folder dataitems owner property should be updated
        /// </summary>
        [Test]
        public void UpdateDataItemOwner()
        {
            EventedList<IDataItem> dataItems = new EventedList<IDataItem>();
            DataItem dataItem = new DataItem();
            dataItems.Add(dataItem);
            Folder folder = new Folder {DataItems = dataItems};
            Assert.AreEqual(folder, dataItem.Owner);

            EventedList<IDataItem> dataItems2 = new EventedList<IDataItem>();
            DataItem dataItem2 = new DataItem();
            dataItems2.Add(dataItem2);
            folder.DataItems = dataItems2;
        }

        /// <summary>
        /// When list of folders is assigned to folder folders parent property should be updated
        /// </summary>
        [Test]
        public void UpdateFolderParent()
        {
            Folder folder = new Folder();
            Folder childFolder = new Folder();
            EventedList<Folder>folders=new EventedList<Folder>();
            folders.Add(childFolder);
            folder.Folders = folders;
            Assert.AreEqual(folder, childFolder.Parent);

        }

        [Test]
        public void AddDataItemImplicitly()
        {
            var folder = new Folder();

            var value = "test";

            folder.Add(value);

            Assert.AreEqual(1, folder.Items.Count);
            Assert.AreEqual(typeof(string), folder.DataItems.FirstOrDefault().ValueType);
            Assert.AreEqual(value, folder.DataItems.FirstOrDefault().Value);
        }
        
        [Test]
        public void GetAllItemsRecursive()
        {
            Folder folder = new Folder();
            DataItem dataItem = new DataItem();
            var subFolder = new Folder {Name = "subFolder"};
            var model = mocks.Stub<IModel>();

            Expect.Call(model.GetDirectChildren()).Return(Enumerable.Empty<object>()).Repeat.Any();
            mocks.ReplayAll();

            folder.Items.Add(model);
            folder.Items.Add(dataItem);
            folder.Items.Add(subFolder);
            // no default sorting expect same sequence as adding 
            Assert.AreEqual(new object[] { folder, model,dataItem,subFolder }, folder.GetAllItemsRecursive().ToArray());
        }

        [Test]
        public void CannotDeleteLinkedDataItem()
        {
            Folder folder = new Folder();
            DataItem dataItem = new DataItem();
            folder.Items.Add(dataItem);

            //create a reference to the dataitem
            DataItem linkedItem = new DataItem();
            linkedItem.LinkTo(dataItem);

            //try to delete the item this should not happen
            folder.Items.RemoveAt(0);
            //todo: find a way to read the message in the log4net logger.
            Assert.AreEqual(1,folder.DataItems.Count());
        }

        [Test]
        public void DisposingFolderDisposesContents()
        {
            Folder folder = new Folder();
            IDataItem dataItem = mocks.StrictMultiMock<IDataItem>(new[] { typeof(IDisposable) });
            
            (dataItem as IDisposable).Expect(d => d.Dispose()).Repeat.Once(); //our test
            dataItem.Expect(d => d.LinkedBy).Return(new List<IDataItem>()).Repeat.Any();
            dataItem.Expect(d => d.Owner).Repeat.Any().SetPropertyAndIgnoreArgument();
            
            mocks.ReplayAll();

            folder.Items.Add(dataItem);
            folder.Dispose();

            mocks.VerifyAll();
        }

        [Test]
        public void SetDataItemValueByName()
        {
            var folder = new Folder();

            var dataItem = new DataItem("s1");

            folder.Add(dataItem);

            var item = folder["s1"];

            item
                .Should().Be.EqualTo(dataItem);
        }
    }
}