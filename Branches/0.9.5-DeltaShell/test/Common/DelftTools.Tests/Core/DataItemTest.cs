using System;
using System.ComponentModel;
using System.Linq;
using DelftTools.Shell.Core;
using DelftTools.Tests.Core.Mocks;
using DelftTools.Utils;
using DelftTools.Utils.IO;
using NetTopologySuite.Extensions.Coverages;
using NUnit.Framework;
using Rhino.Mocks;

namespace DelftTools.Tests.Core
{
    [TestFixture]
    public class DataItemTest
    {
        private static readonly MockRepository mocks = new MockRepository();

        [Test]
        public void CreateContainingObjectValue()
        {
            var dataObject = new CloneableClassWithThreeProperties {Name = "b"};
            var data = new DataItem(dataObject, "a");

            Assert.AreEqual(dataObject, data.Value);
            Assert.AreEqual("a", data.Name);
            Assert.AreEqual(typeof(CloneableClassWithThreeProperties), data.ValueType);
            Assert.AreEqual(DataItem.DefaultRole, data.Role);
            Assert.AreEqual(null, data.Owner);
            Assert.AreEqual(0, data.Id);

            Assert.AreEqual(0, data.LinkedBy.Count);
            Assert.AreEqual(null, data.LinkedTo);
        }

        [Test]
        public void LinkAndUnlinkTwoDataItems()
        {
            var dataObject1 = new CloneableClassWithThreeProperties();
            var dataObject2 = new CloneableClassWithThreeProperties();

            var dataItem1 = new DataItem(dataObject1, "a1");
            var dataItem2 = new DataItem(dataObject2, "a2");

            dataItem2.LinkTo(dataItem1);
            

            Assert.IsTrue(dataItem2.IsLinked);
            Assert.AreSame(dataItem1, dataItem2.LinkedTo);
            Assert.AreSame(dataItem2, dataItem1.LinkedBy[0]);

            dataItem2.Unlink();

            Assert.AreEqual(null, dataItem2.LinkedTo);
            Assert.AreEqual(0, dataItem1.LinkedBy.Count);
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void AssigningValueOfWrongValueTypeShowThrowException()
        {
            var dataItem = new DataItem { Name = "item", ValueType = typeof(int) };

            dataItem.Value = 0.0; // <-- throws exception
        }
        
        [Test]
        [NUnit.Framework.Description("TODO: is this type of data items allowed? Or only object-based")]
        public void CreateContainingSimpleValue()
        {
            var dataItem = new DataItem { Name = "item", ValueType = typeof(int) };
            
            int i = 0;

            dataItem.Value = i;
            dataItem.Name = "i";
            dataItem.Description = "simple integer value";

            Assert.AreEqual(dataItem.Name, "i");
        }
        
        [Test]
        public void  GetAllItemsRecursive()
        {
            DataItem di = new DataItem();
            di.Value = 9;
            Assert.AreEqual(new object[]{di,9},di.GetAllItemsRecursive().ToArray());

            DataItem emptyDi = new DataItem();
            Assert.AreEqual(new object[] { emptyDi}, emptyDi.GetAllItemsRecursive().ToArray());

            DataItem stringDi = new DataItem();
            stringDi.Value = "9";
            Assert.AreEqual(new object[] { stringDi, "9" }, stringDi.GetAllItemsRecursive().ToArray());

        }
        
        [Test]
        public void Clone()
        {
            DataItem di = new DataItem(9,"name",typeof(int),DataItemRole.Output,"tag");
            di.Id = 5;

            DataItem clone = (DataItem) di.DeepClone();
            //be sure the id gets reset
            Assert.AreEqual(0,clone.Id);
            Assert.AreEqual(di.Value,clone.Value);
            Assert.AreEqual(di.Name, clone.Name);
            Assert.AreEqual(di.ValueType, clone.ValueType);
            Assert.AreEqual(di.Role, clone.Role);
            Assert.AreEqual(di.Tag, clone.Tag);
            Assert.AreEqual(di.IsCopyable, clone.IsCopyable);
            Assert.AreEqual(di.IsRemoveable, clone.IsRemoveable);
        }

        [Test]
        [ExpectedException(typeof(NotSupportedException))]
        public void CloneNotCloneableValueObject()
        {
            IDataItem dataItem = new DataItem();
            dataItem.Value = mocks.Stub<IFileBased>();
            var clone = (DataItem)dataItem.DeepClone();
        }

        [Test]
        public void CloneLinkedDataItem()
        {
            var dataItem = new DataItem();
            dataItem.Value = new CloneableClassWithThreeProperties();
            //TODO: why do we need to set the valuetype. Why is this not Typeof(value)???
            //or (next best) why is this determined at construction time.
            dataItem.ValueType = typeof (CloneableClassWithThreeProperties);

            IDataItem linkedDataItem = new DataItem();
            linkedDataItem.ValueType = typeof(CloneableClassWithThreeProperties);
            linkedDataItem.LinkTo(dataItem);
            Assert.AreEqual(dataItem.ValueType,linkedDataItem.ValueType);

            //action! clone the linked dataitem
            var clonedLinkedDataItem = (DataItem)linkedDataItem.DeepClone();
            Assert.AreEqual(linkedDataItem.ValueType,clonedLinkedDataItem.ValueType );

        }

        [Test]
        public void CloneShouldNotCausePropertyChangeOnSourceValue()
        {
            //set a nameable property changing class in the dataItem
            var propertyChangingClass = new CloneableClassWithThreeProperties();
            var dataItem = new DataItem(propertyChangingClass);

            propertyChangingClass.PropertyChanged += delegate
            {
                Assert.Fail("Clone should not cause a change in the source!");
            };

            //action clone the dataitem.
            dataItem.DeepClone();
        }


        [Test]
        public void NameShouldNotChangeWhenSettingValueWithANotNamedValue()
        {
            IDataItem dataItem = new DataItem();
            dataItem.Name = "Hoi";
            //assign a not nameble value.
            dataItem.Value = new DateTime();

            Assert.AreEqual("Hoi", dataItem.Name);
        }

        [Test]
        public void NameShouldChangeWhenSettingValueWithANamedValue()
        {
            IDataItem dataItem = new DataItem {Name = "Hoi"};
            
            //assign a nameble value.
            var cloneableClassWithThreeProperties = new CloneableClassWithThreeProperties { Name = "Dag" };
            dataItem.Value = cloneableClassWithThreeProperties;
            
            Assert.AreEqual("Dag", dataItem.Name);
        }

        [Test]
        public void VerifyDefaults()
        {
            IDataItem dataItem = new DataItem { Name = "Hoi" };

            Assert.IsTrue(dataItem.IsRemoveable);
            Assert.IsTrue(dataItem.IsCopyable);
            Assert.IsFalse(dataItem.NameIsReadOnly);
        }

        [Test]
        public void ShouldBeAbleToChangeNameOfNotINameAbleClass()
        {
            IDataItem dataItem = new DataItem { Name = "Hoi" };

            //a class with a name but not inameable..for example 3rd party ..for example grouplayer,vectorlayer
            var nameableClass = new ClassWithNameButNotINameable();
            dataItem.Value = nameableClass;
            nameableClass.Name = "kees";

            //check the name did not go..maybe change this functionality but this requires reflection..
            Assert.AreEqual("Hoi", dataItem.Name);
        }

        [Test]
        public void DataItemNameShouldChangeWhenNamedValueNameChanges()
        {
            IDataItem dataItem = new DataItem {Name = "Hoi"};
            //assign a nameble value.
            CloneableClassWithThreeProperties cloneableClassWithThreeProperties = new CloneableClassWithThreeProperties {Name = "Dag"};
            dataItem.Value = cloneableClassWithThreeProperties;
            cloneableClassWithThreeProperties.Name = "huh";
            Assert.AreEqual("huh", dataItem.Name);
        }

        [Test]
        public void NameValueNameShouldChangeWhenDataItemNameChanges()
        {
            IDataItem dataItem = new DataItem { Name = "Hoi" };
            //assign a nameble value.
            CloneableClassWithThreeProperties cloneableClassWithThreeProperties = new CloneableClassWithThreeProperties { Name = "Dag" };
            dataItem.Value = cloneableClassWithThreeProperties;
            dataItem.Name = "huh";
            //cloneableClassWithThreeProperties.Name = "huh";
            Assert.AreEqual("huh", cloneableClassWithThreeProperties.Name);
        }

        /// <summary>
        /// When creating a dataitem with a named value should be ignored
        /// </summary>
        [Test]
        public void NameShouldBeSetWhenCreatingNameableDataItem()
        {
            var dataObject = new CloneableClassWithThreeProperties { Name = "OldName" };
            var dataItem = new DataItem(dataObject, "NewName");

            Assert.AreEqual("NewName", dataItem.Name);
        }

        /// <summary>
        /// When creating a dataitem with a named value should be ignored
        /// </summary>
        [Test]
        public void NameShouldBeKeptWhenCreatingNameableDataItem()
        {
            var dataObject = new CloneableClassWithThreeProperties { Name = "OldName" };
            var dataItem = new DataItem(dataObject);

            Assert.AreEqual("OldName", dataItem.Name);
        }


        [Test]
        public void NameShouldChangeWhenLinkedNamedValueNameChanges()
        {
            var dataObject1 = new CloneableClassWithThreeProperties();
            var dataObject2 = new CloneableClassWithThreeProperties();

            var dataItem1 = new DataItem(dataObject1, "name1");
            var dataItem2 = new DataItem(dataObject2, "name2");

            dataItem2.LinkTo(dataItem1);

            Assert.AreEqual("name1", dataItem1.Name);
            Assert.AreEqual("name2", dataItem2.Name, "name of the target data item remains the same after linking");
            
            Assert.AreEqual("name1", ((INameable)dataItem1.Value).Name);
            Assert.AreEqual("name1", ((INameable)dataItem2.Value).Name);

            dataObject2.Name = "newName2";
            Assert.AreEqual("name1", ((INameable)dataItem1.Value).Name);
            Assert.AreEqual("name1", ((INameable)dataItem2.Value).Name);

            dataObject1.Name = "newName1";
            Assert.AreEqual("newName1", ((INameable)dataItem1.Value).Name);
            Assert.AreEqual("newName1", ((INameable)dataItem2.Value).Name);

            dataItem2.Unlink();

            // unlinking results in a new object of the original type as Value in the 
            // item with an uninitialized name. 
            // The original dataObject2 is now an orphan.
            Assert.AreEqual("newName1", ((INameable)dataItem1.Value).Name);
            Assert.AreEqual("newName1", ((INameable)dataItem2.Value).Name); // item was linked to newName1

            ((INameable)dataItem2.Value).Name = "newerName2";
            Assert.AreEqual("newerName2", dataItem2.Name);

            dataItem2.Name = "weereensietsanders";
            Assert.AreEqual("weereensietsanders", ((INameable)dataItem2.Value).Name);
        }
        [Test]
        public void PropertyChangedWorkOnLinkedItems()
        {
            var propertyChangedClass = new CloneableClassWithThreeProperties();
            var sourceDataItem = new DataItem(propertyChangedClass);
            var linkedDataItem = new DataItem(new CloneableClassWithThreeProperties());

            linkedDataItem.LinkTo(sourceDataItem);

            int callCount = 0;
            ((INotifyPropertyChanged) linkedDataItem).PropertyChanged += (s, e) =>
                                                                             {
                                                                                 callCount++;
                                                                                 Assert.AreEqual(propertyChangedClass, s);
                                                                                 Assert.AreEqual("StringProperty", e.PropertyName);
                                                                             };

            propertyChangedClass.StringProperty = "newName";
            Assert.AreEqual(1,callCount);
               
        }

        [Test]       
        public void LinkedEventIncludesPreviousValue()
        {
            //old value is needed to close views.
            var oldValue = new Url();
            var sourceDataItem = new DataItem(new Url());
            var targetDataItem = new DataItem(oldValue);

            int callCount = 0;
            targetDataItem.Linked += (s, e) =>
                                         {
                                             callCount++;
                                             Assert.AreEqual(sourceDataItem, e.Source);
                                             Assert.AreEqual(targetDataItem, e.Target);
                                             Assert.AreEqual(oldValue,e.PreviousValue);
                                         };
            
            targetDataItem.LinkTo(sourceDataItem);
        }

        [Test]
        public void UnLinkEventIncludesPreviousValue()
        {
            //old value is needed to close views.
            var linkedValue = new Url();
            var sourceDataItem = new DataItem(linkedValue);
            var targetDataItem = new DataItem(new Url());

            int callCount = 0;
            targetDataItem.Unlinked += (s, e) =>
            {
                callCount++;
                Assert.AreEqual(sourceDataItem, e.Source);
                Assert.AreEqual(targetDataItem, e.Target);
                Assert.AreEqual(linkedValue, e.PreviousValue);
            };
            targetDataItem.LinkTo(sourceDataItem);
            
            //action! unlink
            targetDataItem.Unlink();
        }

        [Test]
        public void LinkAndUnlinkTwoDiscretizationItems()
        {
            var dataObject1 = new Discretization();
            var dataObject2 = new Discretization();

            var dataItem1 = new DataItem(dataObject1, "a1");
            var dataItem2 = new DataItem(dataObject2, "a2");

            dataItem2.LinkTo(dataItem1);

            Assert.IsTrue(dataItem2.IsLinked);
            Assert.AreSame(dataItem1, dataItem2.LinkedTo);
            Assert.AreSame(dataItem2, dataItem1.LinkedBy[0]);

            dataItem2.Unlink();

            Assert.AreEqual(null, dataItem2.LinkedTo);
            Assert.AreEqual(0, dataItem1.LinkedBy.Count);
        }

        [Test]
        public void LinkAndUnlinkTwoDiscretizationItemsCheckValueAndName()
        {
            var grid1 = new Discretization { Name = "grid1" };
            var grid2 = new Discretization { Name = "grid2" };

            var dataItem1 = new DataItem(grid1, "a1");
            var dataItem2 = new DataItem(grid2, "a2");

            dataItem2.LinkTo(dataItem1);

            dataItem2.Unlink();

            Assert.AreEqual(typeof(Discretization), dataItem2.Value.GetType());
            Assert.AreEqual(typeof(Discretization), dataItem1.Value.GetType());

            var grid2X = (Discretization)dataItem2.Value;

            Assert.AreEqual("a2", grid2X.Name, "name is the same as a data item name");
        }

        [Test]
        public void DoNotCallGetAllItemsRecursiveOnValuesWhenDataItemIsLinked()
        {
            var value = mocks.StrictMock<IItemContainer>();

            var source = new DataItem(value, "source");
            var target = new DataItem(value, "target");

            mocks.ReplayAll();

            target.LinkTo(source);

            target.GetAllItemsRecursive().ToArray(); // should not call value.GetAllItemsRecursive()

            mocks.VerifyAll();
        }
    }
}