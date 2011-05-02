using System.ComponentModel;
using DelftTools.TestUtils;
using DelftTools.TestUtils.TestClasses;
using DelftTools.Utils.Aop;
using DelftTools.Utils.Aop.NotifyPropertyChanged;
using DelftTools.Utils.Collections;
using DelftTools.Utils.Collections.Generic;
using log4net.Config;
using NUnit.Framework;
using PostSharp;

namespace DelftTools.Tests.Utils.Aop
{
    [TestFixture]
    public class NotifyCollectionChangedAttributeTest
    {
        #region Setup/Teardown

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            LogHelper.ConfigureLogging();
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            LogHelper.ResetLogging();
        }
        #endregion

        [Test]
        public void BubbleEventFromContainingList()
        {
            var o = new CollectionChangedAspectTestClass();

            var changeCount = 0;
            Post.Cast<CollectionChangedAspectTestClass, INotifyCollectionChanged>(o).CollectionChanged +=
                delegate(object sender, NotifyCollectionChangedEventArgs e)
                    {
                        Assert.AreEqual(5, e.Item);
                        changeCount++;
                    };
            
            o.Integers.Add(5);

            Assert.AreEqual(1, changeCount);
        }

        [Test]
        public void TestNoBubbling()
        {
            var o = new CollectionChangedAspectTestClass();
 
            var changeCount = 0;
            Post.Cast<CollectionChangedAspectTestClass, INotifyCollectionChanged>(o).CollectionChanged +=
                delegate { changeCount++; };
            
            o.NoBubblingIntegers.Add(5);

            Assert.AreEqual(0, changeCount);
        }

        [Test]
        public void IsCollection()
        {
            Assert.IsTrue(NotifyCollectionChangedAttribute.IsCollection(typeof(EventedList<int>)));
        }

        [Test]
        public void BubblingOfCollectionChangedEventAfterObjectsListIsChanged()
        {
            var o = new CollectionChangedAspectTestClass();

            var element = new CollectionChangedAspectTestClass();
            o.ListContainers.Add(element);

            var changeCount = 0;
            Post.Cast<CollectionChangedAspectTestClass, INotifyCollectionChanged>(o).CollectionChanged +=
                delegate {
                             changeCount++;
                };

            o.ListContainers.Remove(element);

            Assert.AreEqual(1, changeCount); 
        }

        [Test]
        public void BubblingWorksWhenPropertyOfContainerIsResetWithAnotherEventedList()
        {
            var o = new CollectionChangedAspectTestClass();

            var oldIntegers = o.Integers;
            o.Integers = new EventedList<int>(); // <- reset list property to a new value

            var changeCount = 0;
            Post.Cast<CollectionChangedAspectTestClass, INotifyCollectionChanged>(o).CollectionChanged +=
                delegate {
                             changeCount++;
                };
         
            oldIntegers.Add(0); // <- shouldn't generate event in o anymore
            o.Integers.Add(100);

            Assert.AreEqual(1, changeCount);
        }

        [Test]
        public void BubblingWorksWhenUsingEventedListsAsElementsInEventedList()
        {
            var o = new CollectionChangedAspectTestClass();

            var changeCount = 0;
            Post.Cast<CollectionChangedAspectTestClass, INotifyCollectionChanged>(o).CollectionChanged +=
                delegate {
                             changeCount++;
                };

            o.Lists.Add(new EventedList<int>(new int[] {1,2,3}));
            o.Lists[0][0] = 4;

            Assert.AreEqual(2, changeCount);
        }

        [Test]
        public void BubblingOfCollectionChangedEventAfterListPropertyIsChanged()
        {
            var o = new CollectionChangedAspectTestClass();

            var changeCount = 0;
            Post.Cast<CollectionChangedAspectTestClass, INotifyCollectionChanged>(o).CollectionChanged +=
                delegate {
                             changeCount++;
                };

            o.Integers = new EventedList<int>();

            o.Integers.Add(1);
            Assert.AreEqual(1, changeCount);            
            
            o.Integers[0] = 11;
            Assert.AreEqual(2, changeCount);            

            o.Integers.Remove(11);
            Assert.AreEqual(3, changeCount);

            // the list is empty so add one item again and use clear to get 
            // a (single) change event notification
            o.Integers.Add(1);
            o.Integers.Clear();
            Assert.AreEqual(5, changeCount);

            // the list is empty so clear should not fire another change event
            o.Integers.Clear();
            Assert.AreEqual(5, changeCount);         
        }

        [Test]
        public void BubblingOfCollectionChangedEventAfterListPropertyIsChangedInTreeWithAop()
        {
            var client = new TreeClient();

            int count = 0;
            client.RootChanged += delegate { count++; };

            var node1 = new ClassImplementingIEventedList();
            var node2 = new ClassImplementingIEventedList();
            node1.Children.Add(node2);
            
            client.Root.Add(node1);

            Assert.AreEqual(1, count);

            node2.Add(new ClassImplementingIEventedList());

            Assert.AreEqual(2, count);

            node1.Remove(node2);
            Assert.AreEqual(3, count);
        }

        [NotifyPropertyChanged]
        [NotifyCollectionChanged]
        public class ClassWithMultipleAspects
        {
            private IEventedList<ClassWithPropertyChangedAspect> items;

            public ClassWithMultipleAspects()
            {
                items = new EventedList<ClassWithPropertyChangedAspect>();
            }

            public IEventedList<ClassWithPropertyChangedAspect> Items
            {
                get { return items; } 
                set { items = value; } 
            }
        }

        [NotifyPropertyChanged]
        public class ClassWithPropertyChangedAspect
        {
            public string Name { get; set; } 
        }

        [Test]
        public void MultipleFieldBasedAspectsShouldUnsubscribeProperly()
        {
            var o = new ClassWithMultipleAspects();

            var collectionChangeCount = 0;
            var propertyChangeCount = 0;

            ((INotifyCollectionChanged) o).CollectionChanged += delegate { collectionChangeCount++; };
            ((INotifyPropertyChanged) o).PropertyChanged += delegate { propertyChangeCount++; };

            // replace collection should result in correct subscribe / unsubscribe in the aspecs
            var oldItems = o.Items;
            
            o.Items = new EventedList<ClassWithPropertyChangedAspect>();

            // first add item and change its property in the new list
            var item = new ClassWithPropertyChangedAspect();
            o.Items.Add(item); // <-- bubbles collection changed
            item.Name = "bubble property changed";// <-- bubbles property changed

            // then add item and change its property in the old list
            // ERROR IS HERE, adding items to oldItems should not fire events in "o"
            item = new ClassWithPropertyChangedAspect();
            oldItems.Add(item); // <-- bubbles collection changed
            item.Name = "bubble property changed";// <-- bubbles property changed

            collectionChangeCount
                .Should().Be.EqualTo(1);

            propertyChangeCount
                .Should().Be.EqualTo(2);
            
        }

        [Test]
        public void MultipleFieldBasedAspectsShouldBubbleEventsOnlyOnce()
        {
            var o = new ClassWithMultipleAspects();

            var collectionChangeCount = 0;
            var propertyChangeCount = 0;

            ((INotifyCollectionChanged)o).CollectionChanged += delegate { collectionChangeCount++; };
            ((INotifyPropertyChanged)o).PropertyChanged += delegate { propertyChangeCount++; };

            var item = new ClassWithPropertyChangedAspect();
            o.Items.Add(item); // <-- bubbles collection changed

            item.Name = "bubble property changed";// <-- bubbles property changed

            collectionChangeCount
                .Should().Be.EqualTo(1);

            propertyChangeCount
                .Should().Be.EqualTo(1);
        }
    }
}