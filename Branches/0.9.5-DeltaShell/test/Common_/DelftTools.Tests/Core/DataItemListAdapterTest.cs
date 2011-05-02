using DelftTools.Shell.Core;
using DelftTools.Utils.Collections;
using DelftTools.Utils.Collections.Generic;
using NUnit.Framework;

namespace DelftTools.Tests.Core
{
    [TestFixture]
    public class DataItemListAdapterTest
    {

        [Test]
        public void CollectionChangedAddToDataItemsResultInCollectionChangedForAdapter()
        {
            IEventedList < IDataItem > dataItems  = new EventedList<IDataItem>();

            var adapter = new DataItemListAdapter<Url>(dataItems);

            var url = new Url();
            int callCount = 0;
            adapter.CollectionChanged += (sender, e) =>
                                             {
                                                 callCount++;
                                                 Assert.AreEqual(adapter, sender);
                                                 Assert.AreEqual(url, e.Item);
                                                 Assert.AreEqual(0, e.Index);
                                             };
            //action! add
            dataItems.Add(new DataItem(url));
            Assert.AreEqual(1,callCount);
        }


        [Test]
        public void CollectionChangedRemoveDataItemsResultInCollectionChangedForAdapter()
        {
            //create a list of dataItem containing urls
            IEventedList<IDataItem> dataItems = new EventedList<IDataItem>();
            var url = new Url();
            var dataItem = new DataItem(url);
            dataItems.Add(dataItem);

            //adapter for the list
            var adapter = new DataItemListAdapter<Url>(dataItems);

            int callCount = 0;
            adapter.CollectionChanged += (sender, e) =>
            {
                callCount++;
                Assert.AreEqual(adapter, sender);
                Assert.AreEqual(url, e.Item);
                Assert.AreEqual(0, e.Index);
            };

            //action! remove the url from the dataitems list
            dataItems.Remove(dataItem);
            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void CollectionChangedReplaceDataItemsResultInCollectionChangedForAdapter()
        {
            //create a list of dataItem containing urls
            IEventedList<IDataItem> dataItems = new EventedList<IDataItem>();
            var oldUrl = new Url();
            var newUrl = new Url();
            var dataItem = new DataItem(oldUrl);
            dataItems.Add(dataItem);

            //adapter for the list
            var adapter = new DataItemListAdapter<Url>(dataItems);

            int callCount = 0;
            adapter.CollectionChanged += (sender, e) =>
            {
                callCount++;
                Assert.AreEqual(NotifyCollectionChangedAction.Replace,e.Action);
                Assert.AreEqual(adapter, sender);
                Assert.AreEqual(newUrl, e.Item);
                //discutable but current eventedlist implementation does this
                Assert.AreEqual(-1, e.OldIndex);
                Assert.AreEqual(0, e.Index);
            };

            //action! replace one dataitem with another
            dataItems[0] = new DataItem(newUrl);
            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void DataItemLinkToIsCollectionChanged()
        {
            //create a list of dataItem containing urls
            IEventedList<IDataItem> dataItems = new EventedList<IDataItem>();
            var oldUrl = new Url();
            var dataItem = new DataItem(oldUrl);
            dataItems.Add(dataItem);

            //adapter for the list
            var adapter = new DataItemListAdapter<Url>(dataItems);

            
            var newUrl = new Url();
            var sourceDataItem = new DataItem(newUrl);
            int callCount = 0;
            adapter.CollectionChanged += (sender, e) =>
            {
                callCount++;
                Assert.AreEqual(NotifyCollectionChangedAction.Replace, e.Action);
                Assert.AreEqual(adapter, sender);
                Assert.AreEqual(newUrl, e.Item);
                //discutable but current eventedlist implementation does this
                Assert.AreEqual(-1, e.OldIndex);
                Assert.AreEqual(0, e.Index);
            };

            //action! link the first dataItem.
            dataItems[0].LinkTo(sourceDataItem);
            Assert.AreEqual(1, callCount);
        }



        [Test]
        public void DataValueSettingIsCollectionChanged()
        {
            //create a list of dataItem containing urls
            IEventedList<IDataItem> dataItems = new EventedList<IDataItem>();
            var oldUrl = new Url();
            var dataItem = new DataItem(oldUrl);
            dataItems.Add(dataItem);

            //adapter for the list
            var adapter = new DataItemListAdapter<Url>(dataItems);


            var newUrl = new Url();
            var sourceDataItem = new DataItem(newUrl);
            int callCount = 0;
            adapter.CollectionChanged += (sender, e) =>
            {
                callCount++;
                Assert.AreEqual(NotifyCollectionChangedAction.Replace, e.Action);
                Assert.AreEqual(adapter, sender);
                Assert.AreEqual(newUrl, e.Item);
                //discutable but current eventedlist implementation does this
                Assert.AreEqual(-1, e.OldIndex);
                Assert.AreEqual(0, e.Index);
            };

            //action! set Value of the first dataItem.
            dataItems[0].Value = newUrl;
            
            Assert.AreEqual(1, callCount);
        }
        [Test]
        public void AddToAdapter()
        {
            IEventedList<IDataItem> dataItems = new EventedList<IDataItem>();
            var adapter = new DataItemListAdapter<Url>(dataItems);

            int callCount = 0;
            var newUrl = new Url();
            adapter.CollectionChanged += (sender, e) =>
                                             {
                                                 callCount++;
                                                 Assert.AreEqual(NotifyCollectionChangedAction.Add, e.Action);
                                                 Assert.AreEqual(adapter, sender);
                                                 Assert.AreEqual(newUrl, e.Item);
                                                 //discutable but current eventedlist implementation does this
                                                 Assert.AreEqual(-1, e.OldIndex);
                                                 Assert.AreEqual(0, e.Index);
                                             };
            //action! add a new url
            adapter.Add(newUrl);

            Assert.AreEqual(1,dataItems.Count);
            Assert.AreEqual(1,callCount);
        }

        [Test]
        public void UpdatesInDataItemResultInPropertyChangedOfAdaptingList()
        {
            //note: this is really not a nice feature. The whole property changed bubbling stuff bubbling is BAD.
            IEventedList<IDataItem> dataItems = new EventedList<IDataItem>();
            var adapter = new DataItemListAdapter<Url>(dataItems);
            var url = new Url();
            adapter.Add(url);
            int callCount = 0;
            adapter.PropertyChanged += (s, e) =>
                                           {
                                               callCount++;
                                               Assert.AreEqual("Name", e.PropertyName);
                                               Assert.AreEqual(url, s);
                                           };

            ((Url)(dataItems[0].Value)).Name = "Hooi";
            Assert.AreEqual(1,callCount);
            //why is this bad 
        }

        
    }
}
