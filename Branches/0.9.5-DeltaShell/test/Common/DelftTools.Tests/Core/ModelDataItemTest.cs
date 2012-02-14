using System;
using System.Linq;
using DelftTools.Shell.Core;
using DelftTools.Shell.Core.Workflow;
using DelftTools.Utils.Collections.Generic;
using NUnit.Framework;
using Rhino.Mocks;

namespace DelftTools.Tests.Core
{
    [TestFixture]
    public class ModelDataItemTest
    {
        readonly MockRepository mocks = new MockRepository();

        [Test]
        public void LinkToModel()
        {
            var linkedCounter = 0;
            var unlinkedCounter = 0;
            var model1 = mocks.Stub<IModel>();
            var model2 = mocks.Stub<IModel>();
            var modelDataItem = new ModelDataItem();

            model1.DataItems = new EventedList<IDataItem> { new DataItem(new object(), "Name", typeof(object), DataItemRole.None, "Tag") };
            model2.DataItems = new EventedList<IDataItem> { new DataItem(new object(), "Name", typeof(object), DataItemRole.None, "Tag") };
            modelDataItem.RequiredDataItems = new EventedList<IDataItem> { new DataItem(new object(), "Name", typeof(object), DataItemRole.None, "Tag") };

            modelDataItem.Linked += delegate { linkedCounter++; };
            modelDataItem.Unlinked += delegate { unlinkedCounter++; };

            Assert.IsFalse(modelDataItem.IsLinked);

            modelDataItem.LinkToModel(model1);

            Assert.AreEqual(1, linkedCounter);
            Assert.AreEqual(0, unlinkedCounter);
            Assert.IsTrue(modelDataItem.IsLinked);

            modelDataItem.LinkToModel(model1);

            // The link already exists => nothing should happen
            Assert.AreEqual(1, linkedCounter);
            Assert.AreEqual(0, unlinkedCounter);
            Assert.IsTrue(modelDataItem.IsLinked);

            modelDataItem.LinkToModel(model2);

            // The previous model should be unlinked before linking again
            Assert.AreEqual(2, linkedCounter);
            Assert.AreEqual(1, unlinkedCounter);
            Assert.IsTrue(modelDataItem.IsLinked);
        }

        [Test]
        public void UnlinkFromModel()
        {
            var counter = 0;
            var model = mocks.Stub<IModel>();
            var modelDataItem = new ModelDataItem();

            model.DataItems = new EventedList<IDataItem> { new DataItem(new object(), "Name", typeof(object), DataItemRole.None, "Tag") };
            modelDataItem.RequiredDataItems = new EventedList<IDataItem> { new DataItem(new object(), "Name", typeof(object), DataItemRole.None, "Tag") };

            modelDataItem.Unlinked += delegate { counter++; };

            Assert.IsFalse(modelDataItem.IsLinked);

            modelDataItem.LinkToModel(model);

            Assert.IsTrue(modelDataItem.IsLinked);

            modelDataItem.UnlinkFromModel();
            
            Assert.AreEqual(1, counter);
            Assert.IsFalse(modelDataItem.IsLinked);
        }

        [Test]
        public void ModelCannotBeLinkedIfRequiredDataItemIsMissing()
        {
            var model = mocks.Stub<IModel>();
            var modelDataItem = new ModelDataItem();

            model.DataItems = new EventedList<IDataItem>();
            modelDataItem.RequiredDataItems = new EventedList<IDataItem> { new DataItem(new object(), "Name", typeof(object), DataItemRole.None, "Tag") };

            Assert.IsFalse(modelDataItem.IsLinked);

            var exceptionThrown = false;

            try
            {
                modelDataItem.LinkToModel(model);
            }
            catch (Exception e)
            {
                exceptionThrown = true;
                Assert.AreEqual("Can't create link: no matching data is found in the source model for the target item with name \"Name\"", e.Message);
            }

            Assert.IsTrue(exceptionThrown);
            Assert.IsFalse(modelDataItem.IsLinked);
        }

        [Test]
        public void ModelIsUnlinkedIfRequiredDataItemLinkIsRemoved()
        {
            var counter = 0;
            var model = mocks.Stub<IModel>();
            var modelDataItem = new ModelDataItem();

            model.DataItems = new EventedList<IDataItem> { new DataItem(new object(), "Name", typeof(object), DataItemRole.None, "Tag") };
            modelDataItem.RequiredDataItems = new EventedList<IDataItem> { new DataItem(new object(), "Name", typeof(object), DataItemRole.None, "Tag") };

            modelDataItem.Unlinked += delegate { counter++; };

            Assert.IsFalse(modelDataItem.IsLinked);

            modelDataItem.LinkToModel(model);

            Assert.IsTrue(modelDataItem.IsLinked);

            model.DataItems.First().LinkedBy.First().Unlink();

            Assert.AreEqual(1, counter);
            Assert.IsFalse(modelDataItem.IsLinked);
        }

        [Test]
        public void ModelDataItemNameDoesNotChangeWhileLinkingAndUnlinking()
        {
            var model = mocks.Stub<IModel>();
            var modelDataItem = new ModelDataItem { Name = "Expected name" };

            model.DataItems = new EventedList<IDataItem> { new DataItem(new object(), "Name", typeof(object), DataItemRole.None, "Tag") };
            modelDataItem.RequiredDataItems = new EventedList<IDataItem> { new DataItem(new object(), "Name", typeof(object), DataItemRole.None, "Tag") };

            modelDataItem.LinkToModel(model);

            Assert.AreEqual("Expected name", modelDataItem.Name);

            modelDataItem.UnlinkFromModel();

            Assert.AreEqual("Expected name", modelDataItem.Name);
        }
    }
}