using System;
using System.ComponentModel;
using System.Linq;
using DelftTools.Shell.Core;
using DelftTools.Units.Generics;
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
        private static DataItemSet CreateDataItemSet<T>()
        {
            return new DataItemSet(typeof (T));
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

            data.Add(parameter);

            // add new float patameter
            const string floatParameterName = "p2";
            const float floatParameterValue = 10.4f;
            var parameter1 = new Parameter<float> {Name = floatParameterName, Value = floatParameterValue};

            data.Add(parameter1);

            // check results of the operations above.
            Assert.AreEqual(parameter.Value, timeParameterValue);
            Assert.AreEqual(parameter.Name, timeParameterName);

            Assert.AreEqual(parameter1.Value, floatParameterValue);
            Assert.AreEqual(parameter1.Name, floatParameterName);

            Assert.AreEqual(data.Count, 2);

            Assert.AreSame(data["p1"].Value, parameter, "Access parameter 'p1' by its name");
            Assert.AreSame(data["p2"].Value, parameter1, "Access parameter 'p2' by its name");
        }

        [Test]
        public void AddingItemsToAdapterSetsIsRemoveableCorrectly()
        {
            var dataItemSet = CreateDataItemSet<Url>(); // new DataItemSet(new List<Url>(), "My FBC list", DataItemRole.Input, false);
            //get an adapter for the set and add to
            var urlList = dataItemSet.AsEventedList<Url>();
            urlList.Add(new Url());
            Assert.IsFalse(dataItemSet[0].IsRemoveable);
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
        public void AddSubDataItems()
        {
            DataItemSet data = CreateEmptyDataItemSet();

            // add child datasets
            var childData1 = new DataItemSet("data1");
            var childData2 = new DataItemSet("data2");
            data.Add(childData1);
            data.Add(childData2);

            // add data1/ child data items
            var parameter1 = new Parameter<float>("parameter1");
            childData1.Add(parameter1);

            // add data2/ child data items
            var startTime = new Parameter<DateTime>("start time");
            var duration = new Parameter<TimeSpan>("duration");
            childData2.Add(startTime);
            childData2.Add(duration);

            // asserts
            Assert.AreEqual(data.Count, 2);
            Assert.AreSame(data["data1"], childData1);
            Assert.AreSame(data["data2"], childData2);

            var data1 = (DataItemSet) data["data1"];
            Assert.AreEqual(data1.Count, 1);
            Assert.AreSame(parameter1, data1["parameter1"].Value);

            var data2 = (DataItemSet) data["data2"];
            Assert.AreEqual(data2.Count, 2);
            Assert.AreSame(startTime, data2["start time"].Value);
            Assert.AreSame(duration, data2["duration"].Value);

            // TODO: make workaround for index[] operator
            // int indexOfData2 = 1;
            // int indexOfDuration = 1;
            // Assert.AreSame(data2["duration"], data[indexOfData2][indexOfDuration]);
        }

        [Test]
        public void CollectionChangedEventBubbling()
        {
            int count = 0;


            var dataItemSet = CreateDataItemSet<Url>();
            dataItemSet.CollectionChanged += delegate { count++; };
            var list = dataItemSet.AsEventedList<Url>();
            list.Add(new Url());
            list.Add(new Url());
            list.Add(new Url());
            Assert.AreEqual(3, dataItemSet.Count);
            Assert.AreEqual(3, count);


            list.Remove(list[1]);
            Assert.AreEqual(2, dataItemSet.Count);
            Assert.AreEqual(4, count);
        }

        [Test]
        public void GetAllItemsRecursive()
        {
            var dataItemSet = new DataItemSet();
            var dataItem = new DataItem();
            string value = "value";
            dataItem.Value = value;
            dataItemSet.Add(dataItem);

            Assert.AreEqual(new object[] {value, dataItem, dataItemSet}, dataItemSet.GetAllItemsRecursive().ToArray());
        }

        [Test]
        public void TestAddingAndRemovingValuesToDataItemSetUpdatesAdaptedList()
        {
            //TODO: remove this is adapter
            //Create a dataitemset and list.
            var dataItemSet = CreateDataItemSet<Url>();
            var url = new Url();
            var dataItem = new DataItem(url);
            var list = dataItemSet.AsEventedList<Url>();
            
            //action! add an item to the set
            dataItemSet.Add(dataItem);

            //make sure the list got updated
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual(url, list[0]);
            
            dataItemSet.RemoveAt(0);
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
            var dataItemSet = CreateDataItemSet<Url>();
            var list = dataItemSet.AsEventedList<Url>();
            list.Add(new Url());
            list.Add(new Url());
            list.Add(new Url());

            Assert.AreEqual(3, dataItemSet.Count,
                            "Values added to list must create data items on-the-fly in data item set");

            list.Remove(list[1]);
            Assert.AreEqual(2, dataItemSet.Count);
        }

        /// <summary>
        /// All dataitems in the set should have the same role; the owner should be the dataitemset.
        /// </summary>
        [Test]
        public void AddSetsOwnerAndRoleOfChildItem()
        {
            var dataItemSet = CreateDataItemSet<object>();
            dataItemSet.Role = DataItemRole.Output;
            var objectList = dataItemSet.AsEventedList<object>();
            
            //action! add an object throught list adapter
            objectList.Add(new object());

            Assert.AreEqual(1, dataItemSet.Count);
            Assert.AreEqual(DataItemRole.Output, dataItemSet[0].Role);
            Assert.AreEqual(dataItemSet, dataItemSet[0].Owner);
        }

        [Test]
        public void InsertSetsOwnerAndRoleOfChildItem()
        {
            var dataItemSet = CreateDataItemSet<object>();
            dataItemSet.Role = DataItemRole.Output;
            var objectList = dataItemSet.AsEventedList<object>();
            
            //action! add an object throught list adapter
            objectList.Insert(0,new object());

            Assert.AreEqual(1, dataItemSet.Count);

            Assert.AreEqual(DataItemRole.Output, dataItemSet[0].Role);
            Assert.AreEqual(dataItemSet, dataItemSet[0].Owner);

            
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
        public void AddingAnElementShouldCauseCollectionChangedWithSenderDataItemSet()
        {
            //TODO: remove this is adapter logic
            var dataItemSet = CreateDataItemSet<Url>();
            var list = dataItemSet.AsEventedList<Url>();
            //check the collectionchanged
            int callCountWithSenderDataItemSet= 0;
            
            dataItemSet.CollectionChanged +=
                delegate(object sender, NotifyCollectionChangedEventArgs e)
                    {
                        if (sender == dataItemSet)
                            callCountWithSenderDataItemSet++;
                    };

            //Action! should cause one collectionchanged events. One for the list and one for the dataitemset
            list.Add(new Url());

            //assert
            Assert.AreEqual(1, callCountWithSenderDataItemSet);
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
            set.Add(item);
            Assert.AreEqual(set, item.Owner);

            var o = new object();

            //replacing dataitem should update owner
            set.Replace(o,0);
            Assert.AreEqual(set, set[0].Owner);
        }

        [Test]
        [NUnit.Framework.Category("JIRA")]
        public void Issue1372DataItemSetIsConsistenBeforeAnyBubblingOccurs()
        {
            var dataItemSet = CreateDataItemSet<IRegularGridCoverage>();
            
            var items = dataItemSet.AsEventedList<IRegularGridCoverage>();
            var coverage = new RegularGridCoverage {Name = "oldCoverage"};
            items.Add(coverage);

            Assert.AreEqual(1,dataItemSet.Count);
            var newCoverage = new RegularGridCoverage {Name = "newCoverage"};
            
            int callCount = 0;
            ((INotifyPropertyChanged) dataItemSet).PropertyChanged += delegate
                                                                          {
                                                                              callCount++;
                                                                              //synchronization problem
                                                                              Assert.AreEqual(newCoverage,items[0]);
                                                                          };

            dataItemSet[0].Value = newCoverage;

            Assert.AreEqual(1, callCount);
        }

    }
}