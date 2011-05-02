using System;
using System.Collections.Generic;
using System.ComponentModel;
using DelftTools.TestUtils;
using DelftTools.Utils.Aop.NotifyPropertyChanged;
using log4net;
using log4net.Config;
using NUnit.Framework;
using PostSharp;
using DelftTools.TestUtils.TestClasses;

namespace DelftTools.Tests.Utils.Aop
{
    [TestFixture]
    public class NotifyPropertyChangedAttributeTest
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(NotifyPropertyChangedAttributeTest));

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

        [Test]
        [NUnit.Framework.Category("Performance")]
        public void SlowDownBecauseOfPropertyChangeEventsShouldBeSmall()
        {
            var parentObject = new ParentObject();
            var strings = new string[1000000]; // pre-cache strings to have less timing variance

            for (var i = 0; i < 1000000; i++)
            {
                parentObject.Children.Add(new ChildObject());
                strings[i] = i.ToString();
            }

            // without property changed
            var children = parentObject.Children;
            var t = DateTime.Now;
            for (int i = 0; i < 10; i++)
            {
                ChangeChildrenWithoutEvents(children, strings);
            }
            var dtWithoutPropertyChanged = (DateTime.Now - t).TotalMilliseconds;

            // with property changed
            t = DateTime.Now;
            for (int i = 0; i < 10; i++)
            {
                ChangeChildrenWithEvents(children, strings);
            }
            var dtWithPropertyChanged = (DateTime.Now - t).TotalMilliseconds;

            log.InfoFormat("1000 changes without NotifyPropertyChanged: {0} milliseconds", dtWithoutPropertyChanged);
            log.InfoFormat("1000 changes with NotifyPropertyChanged: {0} milliseconds", dtWithPropertyChanged);

            var percentsSlower = (dtWithPropertyChanged / dtWithoutPropertyChanged - 1.0) * 100;

            log.InfoFormat("{0:0.000000}% slower", percentsSlower);

            Assert.LessOrEqual(percentsSlower, 600); // it is still buggy, measured time variaes a lot
        }

        private void ChangeChildrenWithEvents(IList<ChildObject> children, string[] strings)
        {
            for (var i = 0; i < 1000000; i++)
            {
                children[i].Name = strings[i];
            }
        }

        private void ChangeChildrenWithoutEvents(IList<ChildObject> children, string[] strings)
        {
            for (var i = 0; i < 1000000; i++)
            {
                children[i].NameWithoutPropertyChanged = strings[i];
            }
        }

        /// <summary>
        /// Custom event notification in case custom attribute is
        /// set for a specific property within a class implementing INotifyPropertyChanged.
        /// Discussion: Do you expect a property change event if the new Value is the same as the Old Value?
        /// At the moment: Yes
        /// </summary>
        [Test]
        public void FireEventEvenNewIsSameAsOldValue()
        {
            var o = new NotifiableTestClass();
            var observable = Post.Cast<NotifiableTestClass, INotifyPropertyChanged>(o);
            var count = 0;
            observable.PropertyChanged += delegate(object sender, PropertyChangedEventArgs e)
                                              {
                                                  Assert.AreEqual("Name", e.PropertyName);
                                                  count++;
     
                                              };

            o.Name = "Onno"; //event should fire
            o.Name = "Onno";
            Assert.AreEqual(2, count);
        }

        [Test]
        public void EventBubblingChildObject()
        {
            ParentObject parent = new ParentObject();
            int changeCount = 0;
            ChildObject child = new ChildObject();
            parent.Child = child;

            Post.Cast<ParentObject, INotifyPropertyChanged>(parent).PropertyChanged +=
                delegate(object sender, PropertyChangedEventArgs e)
                    {
                        Assert.AreEqual("Name", e.PropertyName);
                        changeCount++;
                    };
            child.Name = "newName";
            Assert.AreEqual(1, changeCount);
        }
        [Test]
        public void EventBubblingChildObjectInAList()
        {
            //same situation but now with a child in a list
            ParentObject parent = new ParentObject();
            int changeCount = 0;

            //add the child in a list
            ChildObject child = new ChildObject();
            parent.Children.Add(child);

            Post.Cast<ParentObject, INotifyPropertyChanged>(parent).PropertyChanged +=
                delegate(object sender, PropertyChangedEventArgs e)
                    {
                        Assert.AreEqual("Name", e.PropertyName);
                        changeCount++;
                    };
            child.Name = "newName";
            Assert.AreEqual(1, changeCount);
        }

        [Test]
        public void SkipNotificationForSelectedProperty()
        {
            ParentObject parentObject = new ParentObject();

            ChildObject childObject = new ChildObject();
            parentObject.Child = childObject;

            int changeCount = 0;
            Post.Cast<ParentObject, INotifyPropertyChanged>(parentObject).PropertyChanged +=
                delegate(object sender, System.ComponentModel.PropertyChangedEventArgs e)
                    {
                        Assert.AreEqual("Name", e.PropertyName);
                        changeCount++;
                    };

            childObject.Parent = parentObject;
            Assert.AreEqual(0, changeCount);

            childObject.Name = "newName";
            Assert.AreEqual(1, changeCount);
        }

        [Test,Ignore("Implement when consensus exists.")]
        public void TestChangingPrivateProperty()
        {
            NotifiableTestClass notifiableTestClass = new NotifiableTestClass();
            int changeCount = 0;
            Post.Cast<NotifiableTestClass, INotifyPropertyChanged>(notifiableTestClass).PropertyChanged +=
                delegate(object sender, System.ComponentModel.PropertyChangedEventArgs e)
                    {
                        Assert.AreEqual("Name", e.PropertyName);
                        changeCount++;
                    };
            notifiableTestClass.SetNameUsingPrivateMethod("My New Name");
            
            Assert.AreEqual("My New Name",notifiableTestClass.Name);
            Assert.AreEqual(1, changeCount);
        }


        [Test]
        public void UnSubscribeWorksFine()
        {
            var subscriber = new BubbleTestClass();
            var subcsriber2 = new BubbleTestClass();

            
            var publisher = new TestPublisher();
            //setup a subscriber
            subscriber.PublicField = publisher;

            //set another subscriber to mess with the first
            subcsriber2.PublicField = publisher;

            //overwrite the field
            subscriber.PublicField = new TestPublisher();

            //the subscriber should not be notified of changes in the old value
            ((INotifyPropertyChanged)subscriber).PropertyChanged += 
                (s, e) => Assert.Fail("Shouldt not fire. UnSubscription failed");

            //action! change the publisher.
            publisher.Name = "new";
        }
    }

    [NotifyPropertyChanged]
    internal class BubbleTestClass 
    {
        private INotifyPropertyChanged privateField;
        protected INotifyPropertyChanged ProtectedField;
        public INotifyPropertyChanged PublicField;

        public INotifyPropertyChanged AutoProperty { get; set; }

        public void SetPrivateField(INotifyPropertyChanged value)
        {
            privateField = value;
        }

        public void SetProtectedField(INotifyPropertyChanged value)
        {
            ProtectedField = value;
        }
        //public event PropertyChangedEventHandler PropertyChanged;
    }

    internal class TestPublisher : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public string Name
        {
            set
            {
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("Name"));
            }
        }
    }
}