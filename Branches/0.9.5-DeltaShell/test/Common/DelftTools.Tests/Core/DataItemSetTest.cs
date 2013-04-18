using System;
using System.ComponentModel;
using System.Linq;
using DelftTools.Shell.Core;
using DelftTools.TestUtils;
using DelftTools.Units.Generics;
using DelftTools.Utils;
using DelftTools.Utils.Collections;
using GeoAPI.Extensions.Coverages;
using NetTopologySuite.Extensions.Coverages;
using NUnit.Framework;

namespace DelftTools.Tests.Core
{
    [TestFixture]
    public class DataItemSetTest
    {
        private static DataItemSet CreateEmptyDataItemSet()
        {
            var data = new DataItemSet {Name = "Data", Description = "Common data of the test units"};
            return data;
        }

        /// <summary>
        /// Creates an empty dataset and add two parameters:<para/>
        /// <code>
        /// <![CDATA[
        ///     Data/ -------- Dataset
        ///         p1 ------- Parameter<DateTime>, value = DateTime.Now
        ///         p2 ------- Parameter<Single>, value = 10.4
        /// ]]>
        /// </code>
        /// </summary>
        [Test]
        public void AddFloatAndDatetimeParameters()
        {
            DataItemSet data = CreateEmptyDataItemSet();

            // add new time stamp patameter
            const string timeParameterName = "p1";
            DateTime timeParameterValue = DateTime.Now;
            var parameter = new Parameter<DateTime> {Name = timeParameterName, Value = timeParameterValue};

            data.DataItems.Add(new DataItem(parameter));

            // add new float patameter
            const string floatParameterName = "p2";
            const float floatParameterValue = 10.4f;
            var parameter1 = new Parameter<float> {Name = floatParameterName, Value = floatParameterValue};

            data.DataItems.Add(new DataItem(parameter1));

            // check results of the operations above.
            Assert.AreEqual(parameter.Value, timeParameterValue);
            Assert.AreEqual(parameter.Name, timeParameterName);

            Assert.AreEqual(parameter1.Value, floatParameterValue);
            Assert.AreEqual(parameter1.Name, floatParameterName);

            Assert.AreEqual(data.DataItems.Count, 2);
        }

        [Test]
        public void AddingItemsToAdapterSetsIsRemoveableCorrectly()
        {
            var dataItemSet = new DataItemSet(typeof(Url)); // new DataItemSet(new List<Url>(), "My FBC list", DataItemRole.Input, false);
            
            dataItemSet.ReadOnly = true;

            //get an adapter for the set and add to
            var urlList = dataItemSet.AsEventedList<Url>();
            urlList.Add(new Url());
            
            Assert.IsFalse(dataItemSet.DataItems[0].IsRemoveable);
        }

        /// <summary>
        /// Tests child data items construction.
        /// <code>
        /// <![CDATA[
        ///     Data/ ---------------- Dataset
        ///         data1/ ----------- Dataset
        ///             parameter1 --- Parameter<float>
        ///         data2/ ----------- Dataset
        ///             start time --- Parameter<DateTime>
        ///             duration ----- Parameter<TimeSpan>
        /// 
        /// ]]>
        /// </code>
        /// </summary>

        [Test]
        public void CollectionChangedEventBubbling()
        {
            int count = 0;


            var dataItemSet = new DataItemSet(typeof(Url));
            ((INotifyCollectionChange)dataItemSet).CollectionChanged += delegate { count++; };
            var list = dataItemSet.AsEventedList<Url>();
            list.Add(new Url());
            list.Add(new Url());
            list.Add(new Url());
            Assert.AreEqual(3, dataItemSet.DataItems.Count);
            Assert.AreEqual(3, count);


            list.Remove(list[1]);
            Assert.AreEqual(2, dataItemSet.DataItems.Count);
            Assert.AreEqual(4, count);
        }

        [Test]
        public void GetAllItemsRecursive()
        {
            var dataItemSet = new DataItemSet();
            var dataItem = new DataItem();
            string value = "value";
            dataItem.Value = value;
            dataItemSet.DataItems.Add(dataItem);

            Assert.AreEqual(new object[] { dataItemSet, dataItem, value }, dataItemSet.GetAllItemsRecursive().ToArray());
        }

        [Test]
        public void TestAddingAndRemovingValuesToDataItemSetUpdatesAdaptedList()
        {
            //TODO: remove this is adapter
            //Create a dataitemset and list.
            var dataItemSet = new DataItemSet(typeof(Url));
            var url = new Url();
            var dataItem = new DataItem(url);
            var list = dataItemSet.AsEventedList<Url>();
            
            //action! add an item to the set
            dataItemSet.DataItems.Add(dataItem);

            //make sure the list got updated
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual(url, list[0]);

            dataItemSet.DataItems.RemoveAt(0);
            Assert.AreEqual(0, list.Count);
            
        }

        /// <summary>
        /// Test a dataitemset stays synchronized with the evented list it wraps.
        /// Not able to get it working because the collectionchanged event does not give 
        /// enough to find the object (or value in case of int) that is to be removed
        /// </summary>
        [Test]
        public void DataItemSetStaysSynchronizedWithEventedListAdapter()
        {
            //TODO: remove test. This functionality now lies within the adapter.
            //testing it here is redundant.
            //wrap the list of functions in a DataItemSet
            var dataItemSet = new DataItemSet(typeof(Url));
            var list = dataItemSet.AsEventedList<Url>();
            list.Add(new Url());
            list.Add(new Url());
            list.Add(new Url());

            Assert.AreEqual(3, dataItemSet.DataItems.Count,
                            "Values added to list must create data items on-the-fly in data item set");

            list.Remove(list[1]);
            Assert.AreEqual(2, dataItemSet.DataItems.Count);
        }

        /// <summary>
        /// All dataitems in the set should have the same role; the owner should be the dataitemset.
        /// </summary>
        [Test]
        public void AddSetsOwnerAndRoleOfChildItem()
        {
            var dataItemSet = new DataItemSet(typeof(object)) {Role = DataItemRole.Output};
            var objectList = dataItemSet.AsEventedList<object>();
            
            //action! add an object throught list adapter
            objectList.Add(new object());

            Assert.AreEqual(1, dataItemSet.DataItems.Count);
            Assert.AreEqual(DataItemRole.Output, dataItemSet.DataItems[0].Role);
            Assert.AreEqual(dataItemSet, dataItemSet.DataItems[0].Owner);
        }

        [Test]
        public void InsertSetsOwnerAndRoleOfChildItem()
        {
            var dataItemSet = new DataItemSet(typeof(object)) {Role = DataItemRole.Output};
            var objectList = dataItemSet.AsEventedList<object>();
            
            //action! add an object throught list adapter
            objectList.Insert(0,new object());

            Assert.AreEqual(1, dataItemSet.DataItems.Count);

            Assert.AreEqual(DataItemRole.Output, dataItemSet.DataItems[0].Role);
            Assert.AreEqual(dataItemSet, dataItemSet.DataItems[0].Owner);

            
        }



        [Test]
        public void Clone()
        {
            DataItemSet dis = new DataItemSet("kaas","tag",typeof(Url));
            DataItemSet clone = (DataItemSet) dis.DeepClone();

            //TODO: add more asserts.
            Assert.AreEqual(dis.Name, clone.Name); 
            Assert.AreEqual(dis.Tag, clone.Tag);
            
        }

        [Test]
        public void ClonedDataItemsInSetShouldHaveClonedDataItemSetAsOwner()
        {
            DataItemSet dataItemSet = CreateEmptyDataItemSet();
            var dataItem = new DataItem();
            string value = "value";
            dataItem.Value = value;
            dataItemSet.DataItems.Add(dataItem);

            DataItemSet clonedSet = (DataItemSet)dataItemSet.DeepClone();

            Assert.AreEqual(clonedSet, clonedSet.DataItems[0].Owner);

        }

        [Test]
        public void AddingAnElementShouldCauseCollectionChangedWith()
        {
            //TODO: remove this is adapter logic
            var dataItemSet = new DataItemSet(typeof(Url));
            var list = dataItemSet.AsEventedList<Url>();
            
            //check the collectionchanged
            int callCount= 0;
            
            ((INotifyCollectionChange)dataItemSet).CollectionChanged +=
                delegate {
                        callCount++;
                    };

            //Action! should cause one collectionchanged events. One for the list and one for the dataitemset
            list.Add(new Url());

            //assert
            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void PropertyChanged()
        {
            var set = new DataItemSet();
            int callCount = 0;
            ((INotifyPropertyChanged) (set)).PropertyChanged += (s, e) =>
                                                                    {
                                                                        callCount++;
                                                                    };

            set.Name = "kaas";
            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void UpdateOwner()
        {
            var set = new DataItemSet();
            IDataItem item = new DataItem();

            //adding dataitem should update owner
            set.DataItems.Add(item);
            Assert.AreEqual(set, item.Owner);

            var o = new object();

            //replacing dataitem should update owner
            set.DataItems[0] = new DataItem(o);
            Assert.AreEqual(set, set.DataItems[0].Owner);
        }

        [Test]
        [NUnit.Framework.Category(TestCategory.Jira)]
        public void Issue1372DataItemSetIsConsistenBeforeAnyBubblingOccurs()
        {
            var dataItemSet = new DataItemSet(typeof(IRegularGridCoverage));
            
            var items = dataItemSet.AsEventedList<IRegularGridCoverage>();
            var coverage = new RegularGridCoverage {Name = "oldCoverage"};
            items.Add(coverage);

            Assert.AreEqual(1, dataItemSet.DataItems.Count);
            var newCoverage = new RegularGridCoverage {Name = "newCoverage"};
            
            int callCount = 0;
            ((INotifyPropertyChanged) dataItemSet).PropertyChanged += delegate
                                                                          {
                                                                              callCount++;
                                                                              //synchronization problem
                                                                              Assert.AreEqual(newCoverage,items[0]);
                                                                          };

            dataItemSet.DataItems[0].Value = newCoverage;

            Assert.AreEqual(2, callCount, "Value and Name get changed once Value property is set");
        }

    }
}