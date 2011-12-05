using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using DelftTools.TestUtils;
using DelftTools.Utils.Collections;
using DelftTools.Utils.Collections.Generic;
using log4net;
using NUnit.Framework;

namespace DelftTools.Tests.Utils.Collections.Generic
{
    [TestFixture]
    public class EventedListTest
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(EventedListTest));

        [SetUp]
        public void SetUp()
        {
            LogHelper.ConfigureLogging();
        }

        [Test]
        public void CollectionChangedWhenValueIsAdded()
        {
            var eventedList = new EventedList<object>();
            var callCount = 0;
            var item = new object();
            eventedList.CollectionChanged += delegate(object sender, NotifyCollectionChangedEventArgs e)
                                                 {
                                                     Assert.AreEqual(eventedList, sender);
                                                     Assert.AreEqual(item, e.Item);
                                                     callCount++;
                                                 };
            eventedList.Add(item);
            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void CollectionChangedWhenValueIsRemoved()
        {
            var eventedList = new EventedList<int>();

            var called = false;
            eventedList.Add(1);
            eventedList.CollectionChanged += delegate { called = true; };
            eventedList.Remove(1);
            Assert.IsTrue(called);
        }

        [Test]
        public void ItemReplaced()
        {
            var eventedList = new EventedList<int>();

            var called = false;
            eventedList.Add(1);
            eventedList.CollectionChanged += delegate { called = true; };
            eventedList[0] = 2;
            Assert.IsTrue(called);
        }

        [Test]
        public void UnsubscribeFromOldItemOnReplace()
        {
            var eventedList = new EventedList<MockClassWithTwoProperties>();

            var aPropertyChangeCount = 0;
            var listPropertyChangeCount = 0;

            var a = new MockClassWithTwoProperties {StringProperty = "a"};

            eventedList.Add(a);

            eventedList.PropertyChanged += delegate { listPropertyChangeCount++; };
            a.PropertyChanged += delegate { aPropertyChangeCount++; };

            // replace item
            eventedList[0] = new MockClassWithTwoProperties {StringProperty = "second a"};

            a.StringProperty = "a2";

            Assert.AreEqual(0, listPropertyChangeCount);
            Assert.AreEqual(1, aPropertyChangeCount);
        }

        [Test]
        public void UnsubscribeFromRemovedItems()
        {
            var eventedList = new EventedList<MockClassWithTwoProperties>();

            var aPropertyChangeCount = 0;
            var listPropertyChangeCount = 0;

            var a = new MockClassWithTwoProperties {StringProperty = "a"};

            eventedList.Add(a);

            eventedList.PropertyChanged += delegate { listPropertyChangeCount++; };
            a.PropertyChanged += delegate { aPropertyChangeCount++; };

            // replace item
            eventedList.Remove(a);

            a.StringProperty = "a2";

            Assert.AreEqual(0, listPropertyChangeCount);
            Assert.AreEqual(1, aPropertyChangeCount);
        }

        [Test]
        public void AddRangeTest()
        {
            var eventedList = new EventedList<int>();

            //keep record of number of collectionchanges.
            var i = 0;
            eventedList.CollectionChanged += delegate { i++; };

            //add three integers to the list.
            eventedList.AddRange(new[] {1, 2, 3});

            //three collectionchanged events will be generated.
            Assert.AreEqual(3, i);

            //check if items where added to the list.
            Assert.IsTrue(eventedList.SequenceEqual(new[] {1, 2, 3}));
        }

        [Test]
        public void ListShouldSubscribeToPropertyChangesInChildObjectAfterAddRange()
        {
            var eventedList = new EventedList<MockClassWithTwoProperties>();

            //add three integers to the list.
            var properties = new MockClassWithTwoProperties();
            eventedList.AddRange(new[] { properties });

            object theSender=null;
            PropertyChangedEventArgs theEventArgs = null;
            eventedList.PropertyChanged += delegate(object sender, PropertyChangedEventArgs e) { theSender = sender;
                                                                                                   theEventArgs = e;};
            properties.StringProperty = "iets";
            Assert.AreEqual(properties,theSender);
            Assert.AreEqual("StringProperty", theEventArgs.PropertyName);
        }
        
        [Test]
        public void ListShouldSubscribeToPropertyChangesInChildObjectAfterAdd()
        {
            var eventedList = new EventedList<MockClassWithTwoProperties>();

            //add three integers to the list.
            var properties = new MockClassWithTwoProperties();
            eventedList.Add( properties );

            object theSender = null;
            PropertyChangedEventArgs theEventArgs = null;
            eventedList.PropertyChanged += delegate(object sender, PropertyChangedEventArgs e)
                                               {
                                                   theSender = sender;
                                                   theEventArgs = e;
                                               };
            properties.StringProperty = "iets";
            Assert.AreEqual(properties, theSender);
            Assert.AreEqual("StringProperty", theEventArgs.PropertyName);
        }

        [Test]
        [NUnit.Framework.Category("Performance")]
        public void EventSubscription()
        {
            var eventedList = new EventedList<MockWithPropertyAndCollectionChanged>();

            var stopwatch = new Stopwatch();

            stopwatch.Start();
            AddManyObjectsWithEvents(eventedList);
            stopwatch.Stop();

            log.DebugFormat("Elapsed time: {0} ms", stopwatch.ElapsedMilliseconds);

            stopwatch.ElapsedMilliseconds
                .Should().Be.LessThan(70);
        }

        private void AddManyObjectsWithEvents(EventedList<MockWithPropertyAndCollectionChanged> eventedList)
        {
            for(var i = 0; i < 100000; i++)
            {
                eventedList.Add(new MockWithPropertyAndCollectionChanged());
            }
        }

        private class MockWithPropertyAndCollectionChanged : INotifyPropertyChanged, INotifyCollectionChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;
            public event NotifyCollectionChangedEventHandler CollectionChanged;
            public event NotifyCollectionChangedEventHandler CollectionChanging;
        }
    }
}