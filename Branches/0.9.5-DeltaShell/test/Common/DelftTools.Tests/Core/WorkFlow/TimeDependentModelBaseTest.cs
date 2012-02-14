using System;
using System.Linq;
using DelftTools.Shell.Core;
using DelftTools.Tests.Core.Mocks;
using DelftTools.Units.Generics;
using DelftTools.Utils;
using NUnit.Framework;

namespace DelftTools.Tests.Core.WorkFlow
{
    [TestFixture]
    public class TimeDependentModelBaseTest
    {
        [Test]
        public void GetAllItemsRecursive()
        {
            SimplerModel model = new SimplerModel();
            var dataItem = new DataItem();
            model.DataItems.Add(dataItem);

            var items = model.GetAllItemsRecursive().ToArray();
            Assert.AreEqual(new object[]
                                {
                                    model, model.DataItems[0], model.DataItems[1], model.DataItems[2], model.DataItems[3],
                                    dataItem, model.DataItems[0].Value, model.DataItems[1].Value, model.DataItems[2].Value,
                                    model.DataItems[3].Value
                                },
                            model.GetAllItemsRecursive().ToArray());
        }
        
        [Test]
        public void CheckDefaultDataItemRoles()
        {
            var model = new SimplerModel();
            
            Assert.AreEqual(3, model.DataItems.Where(di => di.Role == DataItemRole.Input).Count());
            Assert.AreEqual(1, model.DataItems.Where(di => di.Role == DataItemRole.None).Count());
        }

        [Test]
        public void OnInputDataChangedIsCalledOnceParameterChanges()
        {
            //should get two property changes. one for parameter.Value and one for ProgressStart
            var model = new SimplerModel();
            Assert.AreEqual(0, model.OnInputDataChangedCallCount);
            ((Parameter<DateTime>)model.DataItems.First(di => di.ValueType == typeof(Parameter<DateTime>)).Value).Value = DateTime.Now;
            
            Assert.AreEqual(1, model.OnInputDataChangedCallCount);
        }

        [Test]
        public void OnInputDataChangedIsCalledOnceWhenLinkingInputDataItems()
        {
            //setup a sourceitem and a model
            var sourceItem = new DataItem
                                 {
                                     Value = new Parameter<DateTime>()
                                 };



            var model = new SimplerModel();

            //connect the model to the sourceitem
            model.DataItems.First(di => di.ValueType == typeof(Parameter<DateTime>)).LinkTo(sourceItem);
            
            Assert.AreEqual(1, model.OnInputDataChangedCallCount);
        }

        [Test]
        public void OnInputDataChangedIsCalledOnceWhenUnLinkingInputDataItems()
        {
            //setup a sourceitem connected to a model
            var sourceItem = new DataItem
                                 {
                                     Value = new Parameter<DateTime>()
                                 };
            var model = new SimplerModel();
            model.DataItems.First(di => di.ValueType == typeof(Parameter<DateTime>)).LinkTo(sourceItem);
            //reset call counts
            model.OnInputDataChangedCallCount = 0;

            //unlink 
            model.DataItems.First(di => di.ValueType == typeof(Parameter<DateTime>)).Unlink();
            
            Assert.AreEqual(1, model.OnInputDataChangedCallCount);
        }

        [Test]
        public void OnInputDataChangedIsCalledTwiceWhenChangingLinkTo()
        {
            //this is unwanted behaviour since we only need one notification actually
            //but i see no simple way of detecting unlink/link without introducing extra complexity
            //now is chosen to see the change as two separate changes. 


            //setup a sourceitem connected to a model
            var sourceItem = new DataItem
                                 {
                                     Value = new Parameter<DateTime>()
                                 };
            var model = new SimplerModel();
            model.DataItems.First(di => di.ValueType == typeof(Parameter<DateTime>)).LinkTo(sourceItem);
            //reset call count
            model.OnInputDataChangedCallCount = 0;

            //link to other item
            var otherItem = new DataItem();
            model.DataItems.First(di => di.ValueType == typeof(Parameter<DateTime>)).LinkTo(otherItem);

            Assert.AreEqual(2, model.OnInputDataChangedCallCount);
        }

        [Test]
        public void OnInputDataChangedIsCalledWhenTheValueOfADataItemChanges()
        {
            var parameter = new Parameter<DateTime>();
            var sourceItem = new DataItem
                                 {
                                     Value = parameter
                                 };
            var model = new SimplerModel();
            model.DataItems.First(di => di.ValueType == typeof(Parameter<DateTime>)).LinkTo(sourceItem);
            //reset call count
            model.OnInputDataChangedCallCount = 0;

            //action..set a value
            parameter.Value = new DateTime(2010);

            Assert.AreEqual(1, model.OnInputDataChangedCallCount);
        }
    }
}