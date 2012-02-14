using System;
using System.Collections.Generic;
using System.Linq;
using DelftTools.Shell.Core;
using DelftTools.Shell.Core.Workflow;
using DelftTools.Tests.Core.Mocks;
using DelftTools.TestUtils;
using DelftTools.Utils;
using DelftTools.Utils.Collections.Generic;
using DelftTools.Utils.Workflow;
using log4net.Config;
using NUnit.Framework;
using Rhino.Mocks;

namespace DelftTools.Tests.Core.WorkFlow
{
    [TestFixture]
    public class CompositeModelTest
    {
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
        public void TestProgressIndication()
        {
            string result = "";

            var model1 = new SimplerModel { Name = "source" };
            model1.Executing += (s, e) => result += ((SimplerModel)s).Name;

            var model2 = new SimplerModel { Name = "target" };
            model2.Executing += (s, e) => result += ((SimplerModel)s).Name;

            var compositeModel = new CompositeModel
            {
                Name = "composite model",
                Models = { model1, model2 }
            };

            var progressList = new List<string>();
            compositeModel.ProgressChanged += delegate
                                                  {
                                                      progressList.Add(compositeModel.GetProgressText());
                                                  };

            compositeModel.Initialize();
            compositeModel.Execute();

            Assert.AreEqual("50.00 %", progressList[0]);
            Assert.AreEqual("100.00 %", progressList[1]);
        }

        [Test]
        public void TestSequenceLinkFirstSourceThenTarget()
        {
            string result = "";

            SimplerModel sourceModel = new SimplerModel {Name = "source"};
            sourceModel.Executing += (s, e) => result += ((SimplerModel)s).Name;

            SimplerModel targetModel = new SimplerModel { Name = "target" };
            targetModel.Executing += (s, e) => result += ((SimplerModel)s).Name;

            IDataItem sourceInput = new DataItem { Name = "SI", Tag = "SI", Value = new object(), Role = DataItemRole.Input };
            IDataItem sourceOutput = new DataItem { Name = "SO", Tag = "SO", Value = new object(), Role = DataItemRole.Output };
            IDataItem targetInput = new DataItem { Name = "TI", Tag = "TI", Value = new object(), Role = DataItemRole.Input };
            IDataItem targetOutput = new DataItem { Name = "TO", Tag = "TO", Value = new object(), Role = DataItemRole.Output };
            sourceModel.DataItems.Add(sourceInput);
            sourceModel.DataItems.Add(sourceOutput);
            targetModel.DataItems.Add(targetInput);
            targetModel.DataItems.Add(targetOutput);

            var compositeModel = new CompositeModel
            {
                Name = "composite model",
                Models = { sourceModel, targetModel }
            };

            targetInput.LinkTo(sourceOutput);

            compositeModel.Initialize();
            compositeModel.Execute();

            Assert.AreEqual("sourcetarget", result);
        }

        [Test]
        public void TestSequenceLinkFirstTargetThenSource()
        {
            string result = "";

            SimplerModel sourceModel = new SimplerModel { Name = "source" };
            sourceModel.Executing += (s, e) => result += ((SimplerModel)s).Name;

            SimplerModel targetModel = new SimplerModel { Name = "target" };
            targetModel.Executing += (s, e) => result += ((SimplerModel)s).Name;

            IDataItem sourceInput = new DataItem { Name = "SI", Tag = "SI", Value = new object(), Role = DataItemRole.Input };
            IDataItem sourceOutput = new DataItem { Name = "SO", Tag = "SO", Value = new object(), Role = DataItemRole.Output };
            IDataItem targetInput = new DataItem { Name = "TI", Tag = "TI", Value = new object(), Role = DataItemRole.Input };
            IDataItem targetOutput = new DataItem { Name = "TO", Tag = "TO", Value = new object(), Role = DataItemRole.Output };
            sourceModel.DataItems.Add(sourceInput);
            sourceModel.DataItems.Add(sourceOutput);
            targetModel.DataItems.Add(targetInput);
            targetModel.DataItems.Add(targetOutput);

            var compositeModel = new CompositeModel
            {
                Name = "composite model",
                Models = { sourceModel, targetModel }
            };

            sourceInput.LinkTo(targetOutput);

            compositeModel.Initialize();
            compositeModel.Execute();

            Assert.AreEqual("targetsource", result);
        }

        [Test]
        public void TestSequenceLinkFirstSourceThenTargetUseDataSets()
        {
            string result = "";

            SimplerModel sourceModel = new SimplerModel { Name = "source" };
            sourceModel.Executing += (s, e) => result += ((SimplerModel)s).Name;

            SimplerModel targetModel = new SimplerModel { Name = "target" };
            targetModel.Executing += (s, e) => result += ((SimplerModel)s).Name;

            IDataItemSet sourceInputSet = new DataItemSet { Name = "input", Tag = "intput",  Role = DataItemRole.Input };
            IDataItem sourceInput = new DataItem { Name = "SI", Tag = "SI", Value = new object(), Role = DataItemRole.Input };
            sourceInputSet.DataItems.Add(sourceInput);

            IDataItemSet sourceOutputSet = new DataItemSet { Name = "output", Tag = "output", Role = DataItemRole.Output };
            IDataItem sourceOutput = new DataItem { Name = "SO", Tag = "SO", Value = new object(), Role = DataItemRole.Output };
            sourceOutputSet.DataItems.Add(sourceOutput);

            IDataItemSet targetInputSet = new DataItemSet { Name = "input", Tag = "intput", Role = DataItemRole.Input };
            IDataItem targetInput = new DataItem { Name = "TI", Tag = "TI", Value = new object(), Role = DataItemRole.Input };
            targetInputSet.DataItems.Add(targetInput);

            IDataItemSet targetOutputSet = new DataItemSet { Name = "output", Tag = "output", Role = DataItemRole.Output };
            IDataItem targetOutput = new DataItem { Name = "TO", Tag = "TO", Value = new object(), Role = DataItemRole.Output };
            targetOutputSet.DataItems.Add(targetOutput);

            sourceModel.DataItems.Add(sourceInputSet);
            sourceModel.DataItems.Add(sourceOutputSet);
            targetModel.DataItems.Add(targetInputSet);
            targetModel.DataItems.Add(targetOutputSet);

            var compositeModel = new CompositeModel
            {
                Name = "composite model",
                Models = { sourceModel, targetModel }
            };

            targetInput.LinkTo(sourceOutput);

            compositeModel.Initialize();
            compositeModel.Execute();

            Assert.AreEqual("sourcetarget", result);
        }

        [Test]
        public void TestSequenceLinkFirstTargetThenSourceUseDataSets()
        {
            string result = "";

            SimplerModel sourceModel = new SimplerModel { Name = "source" };
            sourceModel.Executing += (s, e) => result += ((SimplerModel)s).Name;

            SimplerModel targetModel = new SimplerModel { Name = "target" };
            targetModel.Executing += (s, e) => result += ((SimplerModel)s).Name;

            IDataItemSet sourceInputSet = new DataItemSet { Name = "input", Tag = "intput", Role = DataItemRole.Input };
            IDataItem sourceInput = new DataItem { Name = "SI", Tag = "SI", Value = new object(), Role = DataItemRole.Input };
            sourceInputSet.DataItems.Add(sourceInput);

            IDataItemSet sourceOutputSet = new DataItemSet { Name = "output", Tag = "output", Role = DataItemRole.Output };
            IDataItem sourceOutput = new DataItem { Name = "SO", Tag = "SO", Value = new object(), Role = DataItemRole.Output };
            sourceOutputSet.DataItems.Add(sourceOutput);

            IDataItemSet targetInputSet = new DataItemSet { Name = "input", Tag = "intput", Role = DataItemRole.Input };
            IDataItem targetInput = new DataItem { Name = "TI", Value = new object(), Role = DataItemRole.Input };
            targetInputSet.DataItems.Add(targetInput);

            IDataItemSet targetOutputSet = new DataItemSet { Name = "output", Tag = "output", Role = DataItemRole.Output };
            IDataItem targetOutput = new DataItem { Name = "TO", Tag = "TO", Value = new object(), Role = DataItemRole.Output };
            targetOutputSet.DataItems.Add(targetOutput);

            sourceModel.DataItems.Add(sourceInputSet);
            sourceModel.DataItems.Add(sourceOutputSet);
            targetModel.DataItems.Add(targetInputSet);
            targetModel.DataItems.Add(targetOutputSet);

            var compositeModel = new CompositeModel
            {
                Name = "composite model",
                Models = { sourceModel, targetModel }
            };

            sourceInput.LinkTo(targetOutput);


            compositeModel.Initialize();
            compositeModel.Execute();

            Assert.AreEqual("targetsource", result);
        }

        /// <summary>
        /// Links models as model2 to model1 and runs parent composition model.
        /// Composition model should detect correct order to run models: model2 then model1
        /// </summary>
/* move to TestPlugin1.Tests
                [Test]
                public void LinkModelDataAndRun()
                {
                    var sourceModel = new SimpleModel {Name = "source model"};
                    var targetModel = new SimpleModel {Name = "target model"};

                    var compositeModel = new CompositeModel
                                             {
                                                 Name = "composite model",
                                                 Models = {targetModel, sourceModel},
                                                 // note that models are added in reverse order
                                                 StartTime = new DateTime(2009, 1, 1, 0, 0, 0),
                                                 StopTime = new DateTime(2009, 1, 1, 5, 0, 0),
                                                 TimeStep = new TimeSpan(1, 0, 0)
                                             };

                    // define input for model2 
                    var time = compositeModel.StartTime;
                    sourceModel.InputTimeSeries[time] = 1.0;
                    sourceModel.InputTimeSeries[time.AddHours(1)] = 2.0;
                    sourceModel.InputTimeSeries[time.AddHours(2)] = 3.0;
                    sourceModel.InputTimeSeries[time.AddHours(3)] = 4.0;
                    sourceModel.InputTimeSeries[time.AddHours(4)] = 5.0;

                    // link model2.OutputTimeSeries -> model1.InputTimeSeries
                    var outputDataItem = sourceModel.GetDataItemByValue(sourceModel.OutputTimeSeries);
                    var inputTimeSeries = targetModel.GetDataItemByValue(targetModel.InputTimeSeries);

                    inputTimeSeries.LinkTo(outputDataItem);

                    Assert.AreEqual(sourceModel.OutputTimeSeries, targetModel.InputTimeSeries,
                                    "Linking should set value of InputTimeSeries of target model to OutputTimeSeries of source model");

                    // initialize
                    compositeModel.Initialize();

                    Assert.AreEqual(0, sourceModel.OutputTimeSeries.Components[0].Values.Count,
                                    "Output values of the source model after initialize are not filled in yet");

                    Assert.AreEqual(0, targetModel.OutputTimeSeries.Components[0].Values.Count,
                                    "Output values of the target model after initialize are not filled in yet");

                    // run model
                    compositeModel.Execute();

                    Assert.AreEqual(ActivityStatus.Executing, compositeModel.Status);
                    Assert.AreEqual(ActivityStatus.Executing, sourceModel.Status);
                    Assert.AreEqual(ActivityStatus.Executing, targetModel.Status);

                    Assert.AreEqual(1, sourceModel.OutputTimeSeries.Components[0].Values.Count,
                                    "Source model should fill in a new value in the output time series");

                    Assert.AreEqual(1, targetModel.OutputTimeSeries.Components[0].Values.Count,
                                    "Target model should fill in a new value in the output time series");
                }
*/

        private static readonly MockRepository mocks = new MockRepository();


        /// <summary>
        /// Composite model should clone contained model (deep clone)
        /// </summary>
        [Test]
        public void CloneModel()
        {
            var compositeModel = new CompositeModel();
            var containedModel = mocks.StrictMock<IModel>();
            var modelClone = mocks.StrictMock<IModel>();
            using (mocks.Record())
            {
                Expect.Call(containedModel.Owner = compositeModel);
                Expect.Call(containedModel.DataItems).Return(new EventedList<IDataItem>()).Repeat.Any();
                Expect.Call(containedModel.GetDirectChildren()).Return(new EventedList<object>()).Repeat.Any();
                Expect.Call(containedModel.DeepClone()).Return(modelClone);

                containedModel.StatusChanged += null;
                LastCall.IgnoreArguments();
                //containedModel.StatusChanged -= null;
                //LastCall.IgnoreArguments();
                Expect.Call(modelClone.Owner = null).IgnoreArguments();
                modelClone.StatusChanged += null; 
                LastCall.IgnoreArguments();
                Expect.Call(modelClone.DataItems).Return(new EventedList<IDataItem>()).Repeat.Any();
                Expect.Call(modelClone.GetDirectChildren()).Return(new EventedList<object>()).Repeat.Any();
            }

            
            CompositeModel clonedModel;
            using (mocks.Playback())
            {
                compositeModel.Models.Add(containedModel);
                clonedModel = (CompositeModel) compositeModel.DeepClone();
            }

            Assert.AreEqual(compositeModel.Models.Count, clonedModel.Models.Count);
        }


        [Test]
        public void CloneShouldUpdateLinksWithinModel()
        {
            //Situation
            // CompositeModel
            //   |-SourceModel
            //   |-LinkedModel (->SourceModel)

            //Clone this composite model and expect the c-linkedmodel to be connected to the cloned-sourcemodel.

            var compositeModel = new CompositeModel();

            IModel sourceModel = new TestModel("source");
            IDataItem sourceDataItem = new DataItem(new Url(), "sourceItem");
            sourceModel.DataItems.Add(sourceDataItem);
            
            compositeModel.Models.Add(sourceModel);

            IModel linkedModel = new TestModel("linked");
            IDataItem linkedDataItem = new DataItem {Name = "linkedItem"};
            linkedModel.DataItems.Add(linkedDataItem);

            compositeModel.Models.Add(linkedModel);

            linkedDataItem.LinkTo(sourceDataItem);

            var clonedModel = (CompositeModel) compositeModel.DeepClone();

            IModel clonedSourceModel = clonedModel.Models.Where(m => m.Name == "source").First();
            IModel clonedLinkedModel = clonedModel.Models.Where(m => m.Name == "linked").First();

            IDataItem clonedLinkedItem = clonedLinkedModel.DataItems.Where(d => d.Name == "linkedItem").First();
            IDataItem clonedSourceItem = clonedSourceModel.DataItems.Where(d => d.Name == "sourceItem").First();

            //the cloned sourceitem should not be the the sourcedataitem.
            Assert.AreNotEqual(clonedSourceItem.Value, sourceDataItem.Value);


            Assert.IsTrue(clonedLinkedItem.IsLinked);
            Assert.AreEqual(clonedSourceItem, clonedLinkedItem.LinkedTo);
        }


        /// <summary>
        /// Composite model should clone contained model (deep clone)
        /// </summary>
        /*  [Test] TODO: move to integration tests?
          public void CloneModelWithLinkedModels()
          {
              var compositeModel = new CompositeModel();
            
              var model1 = new SimpleModel { Name = "model1" };
              var model2 = new SimpleModel { Name = "model2" };

              compositeModel.Models.Add(model1);
              compositeModel.Models.Add(model2);

              // link model input to model output
              var sourceDataItem = model1.GetDataItemByValue(model1.OutputTimeSeries);
              var targetDataItem = model2.GetDataItemByValue(model2.InputTimeSeries);

              targetDataItem.LinkTo(sourceDataItem);

              var compositeModelClone = (CompositeModel) compositeModel.DeepClone();
            
              Assert.AreEqual(compositeModel.DataItems.Count, compositeModelClone.DataItems.Count);

              var model1Clone = (SimpleModel) compositeModelClone.Models[0];
              var model2Clone = (SimpleModel) compositeModelClone.Models[1];

              var sourceDataItemClone = model1Clone.GetDataItemByValue(model1Clone.OutputTimeSeries);
              var targetDataItemClone = model2Clone.GetDataItemByValue(model2Clone.InputTimeSeries);

              Assert.AreEqual(sourceDataItemClone, targetDataItemClone.LinkedTo);
          }
  */

        [Test]
        public void GetAllItemsRecursiveShouldReturnModels()
        {
            var compositeModel = new CompositeModel();
            var model = new SimplerModel();
            compositeModel.Models.Add(model);

            IEnumerable<object> enumerable = compositeModel.GetAllItemsRecursive();
            Assert.IsTrue(enumerable.Contains(model));
            Assert.AreEqual(
                1 +  compositeModel.DataItems.Count*2 +model.GetAllItemsRecursive().Count(),
                enumerable.Count());
        }

        [Test]
        public void CompositeModelBubblesStatusChangesOfChildModels()
        {
            var compositeModel = new CompositeModel();
            var childModel = new TestModel();
            compositeModel.Models.Add(childModel);

            int callCount = 0;
            compositeModel.StatusChanged += (s, e) =>
            {
                Assert.AreEqual(s, childModel);
                callCount++;
            };

            //action change status of child model
            childModel.Status = ActivityStatus.Initialized;
            
            Assert.AreEqual(1, callCount);

            compositeModel.Models.Remove(childModel);
            childModel.Status = ActivityStatus.None;
        }

        [Test]
        public void CompositeModelUnSubscribesFromChildModelsWhenRemoved()
        {
            var compositeModel = new CompositeModel();
            var childModel = new TestModel();
            
            compositeModel.Models.Add(childModel);
            compositeModel.Models.Remove(childModel);

            compositeModel.StatusChanged += (s, e) => Assert.Fail("Should have unsubscribed!");

            childModel.Status = ActivityStatus.Initializing;
        }
    }
}