using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using DelftTools.Functions.Binding;
using DelftTools.Functions.Filters;
using DelftTools.Functions.Generic;
using DelftTools.Functions.DelftTools.Utils.Tuples;
using DelftTools.TestUtils;
using DelftTools.Units;
using DelftTools.Utils.Aop;
using DelftTools.Utils.Threading;
using log4net;
using NUnit.Framework;
using SharpTestsEx;
using TypeConverter = DelftTools.Utils.TypeConverter;

namespace DelftTools.Functions.Tests.Binding
{
    [TestFixture]
    public class FunctionBindingListTest
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(MultiDimensionalArrayBindingListTest));
        //private readonly MockRepository mocks = new MockRepository();

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            LogHelper.ConfigureLogging();
        }

        [Test]
        [Category(TestCategory.WindowsForms)]
        public void Bind2DFunctionWith1ComponentAndCheckColumnName()
        {
            IFunction function = new Function();

            function.Arguments.Add(new Variable<int>("x") { Unit = new Unit("s", "s") });
            function.Arguments.Add(new Variable<int>("y") { Unit = new Unit("m", "m") });
            function.Components.Add(new Variable<string>("f1") { Unit = new Unit("m/s","m/s") });

            function[0, 0] = new[] { "f1(0,0)"};
            function[1, 0] = new[] { "f1(1,0)"};
            function[0, 1] = new[] { "f1(0,1)" };
            function[1, 1] = new[] { "f1(1,1)"};

            var gridView = new DataGridView();
            IFunctionBindingList functionBindingList = new FunctionBindingList(function) { SynchronizeInvoke = gridView };
            gridView.DataSource = functionBindingList;

            WindowsFormsTestHelper.ShowModal(gridView);
        }

        [Test]
        public void RemoveValuesFromFunctionBindingListWhenDuplicatesExist()
        {
            IFunction function = new Function();

            function.Arguments.Add(new Variable<DateTime>("x") { Unit = new Unit("s", "s") });
            function.Components.Add(new Variable<double>("f1") { Unit = new Unit("m/s", "m/s") });
            
            function[new DateTime(2000, 01, 01, 00, 00, 00)] = new[] { 2.23 };
            function[new DateTime(2000, 01, 01, 12, 00, 00)] = new[] { 2.23 };
            function[new DateTime(2000, 01, 02, 12, 00, 00)] = new[] { 2.23 };
            function[new DateTime(2000, 01, 03, 12, 00, 00)] = new[] { 2.23 };
            function[new DateTime(2000, 01, 04, 12, 00, 00)] = new[] { 3.23 };
            function[new DateTime(2000, 01, 05, 12, 00, 00)] = new[] { 7.87 };
            function[new DateTime(2000, 01, 06, 12, 00, 00)] = new[] { 8.55 };
            function[new DateTime(2000, 01, 07, 12, 00, 00)] = new[] { 5.6 };
            function[new DateTime(2000, 01, 08, 12, 00, 00)] = new[] { 3.31 };
            function[new DateTime(2000, 01, 09, 12, 00, 00)] = new[] { 2.23 };
            function[new DateTime(2000, 01, 10, 12, 00, 00)] = new[] { 2.23 };
            function[new DateTime(2000, 01, 11, 12, 00, 00)] = new[] { 2.23 };
            function[new DateTime(2000, 01, 12, 12, 00, 00)] = new[] { 2.23 };
            function[new DateTime(2000, 01, 13, 12, 00, 00)] = new[] { 2.23 };

            IFunctionBindingList functionBindingList = new FunctionBindingList(function);

            //remove all but the first:
            var last = function.Components[0].Values.Count-1;
            for (int i = last; i >= 1; i-- )
            {
                functionBindingList.RemoveAt(i);
            }
            Assert.AreEqual(1, function.Components[0].Values.Count);
            Assert.AreEqual(2.23, function.Components[0].Values[0]);
        }

        [Test]
        [Category(TestCategory.WindowsForms)]
        public void Bind1DFunctionWith2Components()
        {
            IFunction function = new Function();

            function.Arguments.Add(new Variable<int>("x"));
            function.Components.Add(new Variable<string>("f1"));
            function.Components.Add(new Variable<string>("f2"));

            function[0] = new[] { "f1(0)", "f2(0)" };
            function[1] = new[] { "f1(1)", "f2(1)" };
            function[2] = new[] { "f1(2)", "f2(2)" };

            var gridView = new DataGridView();
            IFunctionBindingList functionBindingList = new FunctionBindingList(function) { SynchronizeInvoke = gridView };
            gridView.DataSource = functionBindingList;

            WindowsFormsTestHelper.ShowModal(gridView);
        }

        [Test]
        [Category(TestCategory.WindowsForms)]
        public void Bind1DFunctionWith2ComponentsTimeSpan()
        {
            IFunction function = new Function();

            function.Arguments.Add(new Variable<TimeSpan>("x"));
            function.Components.Add(new Variable<string>("f1"));
            function.Components.Add(new Variable<string>("f2"));

            function[new TimeSpan(0, 0, 0, 1)] = new[] { "f1(0)", "f2(0)" };
            function[new TimeSpan(0, 0, 0, 2)] = new[] { "f1(1)", "f2(1)" };
            function[new TimeSpan(0, 0, 0, 3)] = new[] { "f1(2)", "f2(2)" };

            var gridView = new DataGridView();
            IFunctionBindingList functionBindingList = new FunctionBindingList(function) { SynchronizeInvoke = gridView };
            gridView.DataSource = functionBindingList;
            
            WindowsFormsTestHelper.ShowModal(gridView);
        }

        [Test]
        [Category(TestCategory.WindowsForms)]
        public void Bind2DFunctionWith2Components()
        {
            IFunction function = new Function();

            function.Arguments.Add(new Variable<int>("x"));
            function.Arguments.Add(new Variable<int>("y"));
            function.Components.Add(new Variable<string>("f1"));
            function.Components.Add(new Variable<string>("f2"));

            function[0, 0] = new[] { "f1(0,0)", "f2(0,0)" };
            function[1, 0] = new[] { "f1(1,0)", "f2(1,0)" };
            function[0, 1] = new[] { "f1(0,1)", "f2(0,1)" };
            function[1, 1] = new[] { "f1(1,1)", "f2(1,1)" };

            var gridView = new DataGridView();
            IFunctionBindingList functionBindingList = new FunctionBindingList(function) { SynchronizeInvoke = gridView };
            gridView.DataSource = functionBindingList;

            WindowsFormsTestHelper.ShowModal(gridView);
        }

        [Test]
        public void AddValuesToFunctionUpdatesBindingList()
        {
            IFunction function = new Function();

            function.Arguments.Add(new Variable<int>("x1"));
            function.Components.Add(new Variable<string>("y1"));

            function[0] = "zero";
            function[1] = "one";
            function[2] = "two";

            IFunctionBindingList functionBindingList = new FunctionBindingList(function);
            function[3] = "three";
            
            Assert.AreEqual(4, functionBindingList.Count);
        }

        [Test]
        public void SetMultipleValuesToFunctionUpdatesBindingList()
        {
            IFunction function = new Function();

            function.Arguments.Add(new Variable<int>("x1"));
            function.Components.Add(new Variable<string>("y1"));

            function[0] = "zero";
            function[1] = "one";
            function[2] = "two";

            IFunctionBindingList functionBindingList = new FunctionBindingList(function);
            function[3] = "three";

            Assert.AreEqual(4, functionBindingList.Count);

            function.Arguments[0].AddValues(new[] {4, 5, 6});

            Assert.AreEqual(7, functionBindingList.Count);

            function.Arguments[0].SetValues(new[] { 8, 9, 10 }); //this adds the values, so expected is 7+3

            Assert.AreEqual(10, functionBindingList.Count);
        }

        [Test]
        public void ChangeValuesViaBindingListMustUpdateValuesInFunction()
        {
            IFunction function = new Function();

            function.Arguments.Add(new Variable<int>("x1"));
            function.Components.Add(new Variable<string>("y1"));

            function[0] = "zero";
            function[1] = "one";
            function[2] = "two";

            IFunctionBindingList functionBindingList = new FunctionBindingList(function);

            var row = (FunctionBindingListRow)functionBindingList[1];
            row[1] = "one_updated";

            row.EndEdit(); //commit

            Assert.AreEqual("one_updated", function[1]);
        }

        [Test]
        public void AddRowsToBindingListUpdatesFunction()
        {
            IFunction function = new Function();

            function.Arguments.Add(new Variable<int>("x1"));
            function.Components.Add(new Variable<string>("y1"));

            function[0] = "zero";
            function[1] = "one";
            function[2] = "two";

            IFunctionBindingList functionBindingList = new FunctionBindingList(function);

            functionBindingList.AddNew();
            var row = (FunctionBindingListRow)functionBindingList[3];
            // row[0] = 3;
            row[1] = "three";

            row.EndEdit();

            row = (FunctionBindingListRow)functionBindingList[3];
            Assert.AreEqual(row[1], function[3]);
        }

        [Test]
        public void ModifyBindingListRowAndCancelDoesNotUpdateFunction()
        {
            IFunction function = new Function();

            function.Arguments.Add(new Variable<int>("x1"));
            function.Components.Add(new Variable<string>("y1"));

            function[0] = "zero";
            function[1] = "one";
            function[2] = "two";

            IFunctionBindingList functionBindingList = new FunctionBindingList(function);

            var row = (FunctionBindingListRow)functionBindingList[0];

            var three = "three";
            row[1] = three;

            Assert.AreEqual(row[1], three); //set
            Assert.AreNotEqual(row[1], function[0]); //but not yet committed

            row.CancelEdit(); //cancelled

            Assert.AreNotEqual(row[1], three); //no longer set
            Assert.AreEqual(row[1], function[0]); //still not committed
        }

        [Test]
        public void ModifyBindingListRowAndCommitUpdatesFunction()
        {
            IFunction function = new Function();

            function.Arguments.Add(new Variable<int>("x1"));
            function.Components.Add(new Variable<string>("y1"));

            function[0] = "zero";
            function[1] = "one";
            function[2] = "two";

            IFunctionBindingList functionBindingList = new FunctionBindingList(function);

            var row = (FunctionBindingListRow)functionBindingList[0];

            var three = "three";
            row[1] = three;

            Assert.AreEqual(row[1], three); //set
            Assert.AreNotEqual(row[1], function[0]); //but not yet committed

            row.EndEdit(); //commit

            row = (FunctionBindingListRow)functionBindingList[0];

            Assert.AreEqual(row[1], three); //still set
            Assert.AreEqual(row[1], function[0]); //committed
        }

        [Test]
        public void ModifyingTableWhichTriggersReorderingGoesFine()
        {
            IFunction function = new Function();

            function.Arguments.Add(new Variable<int>("x1") {IsAutoSorted = true});
            function.Components.Add(new Variable<string>("y1"));

            function[0] = "zero";
            function[1] = "one";
            function[2] = "two";

            var bindingList = new FunctionBindingList(function);

            var called = 0;
            bindingList.ListChanged += (s, e) => called++;

            var row = bindingList[0];
            row[0] = 3; // will cause reordering in function
            row.EndEdit(); // commit

            Assert.Greater(called, 0); //table needs to be notified of changes
            Assert.AreEqual(3, bindingList[2][0]); //expect bindinglist to reorder as well
        }

        [Test]
        public void ClearFunctionBindingListShouldRemoveFunctionValues()
        {
            IFunction function = new Function();

            function.Arguments.Add(new Variable<int>("x1"));
            function.Components.Add(new Variable<string>("y1"));

            function[0] = "zero";
            function[1] = "one";
            function[2] = "two";

            var functionBindingList = new FunctionBindingList(function);
            functionBindingList.Clear();

            Assert.AreEqual(0, function.GetValues<string>().Count);
        }

        [Test]
        [Category(TestCategory.Integration)]
        public void ChangeFunctionValueAndCheckBindingListRowValue()
        {
            IFunction function = new Function();
            function.Arguments.Add(new Variable<int>("x1"));
            function.Components.Add(new Variable<string>("y1"));

            function[0] = "zero";

            IFunctionBindingList functionBindingList = new FunctionBindingList(function);

            // binding list contains rows with argument and component values of the function
            var firstRow = (FunctionBindingListRow)functionBindingList[0];
            Assert.AreEqual(0, firstRow["x1"]);
            Assert.AreEqual("zero", firstRow["y1"]);

            //check if changecalled event fires when we change the underlying function

            int calledCount = 0;
            functionBindingList.ListChanged += delegate { calledCount++; };
            function[0] = "new_one";

            Thread.Sleep(1000);

            //Assert.AreEqual(1, calledCount);
            Assert.AreEqual("new_one", firstRow["y1"]);
        }

        [Test]
        [Category(TestCategory.WindowsForms)]
        public void Bind2DFunctionWithTuple()
        {
            IFunction function = new Function();

            function.Arguments.Add(new Variable<Pair<string, float>>());
            function.Components.Add(new Variable<int>("value"));

            function[new Pair<string, float>("aap", 0)] = 0;
            function[new Pair<string, float>("aap", 11)] = 1;
            function[new Pair<string, float>("muis", 0)] = 1;

            TypeConverter.RegisterTypeConverter<Pair<string, float>, PairTypeConverter<string, float>>();

            Assert.AreEqual(1, function.Arguments.Count);
            Assert.AreEqual(1, function.Components.Count);

            var gridView = new DataGridView();
            IFunctionBindingList functionBindingList = new FunctionBindingList(function) { SynchronizeInvoke = gridView };
            gridView.DataSource = functionBindingList;

            WindowsFormsTestHelper.ShowModal(gridView);
        }


        [Test]
        [Category(TestCategory.WindowsForms)]
        public void ModifyFunctionBindingListFromOtherThreadsWhileDisposing()
        {
            IFunction function = new Function();
            function.Arguments.Add(new Variable<string>("str"));
            function.Components.Add(new Variable<int>("value"));
            function["aap"] = 0;
            function["noot"] = 1;
            function["mies"] = 1;

            var done = false;
            string exceptionMessage = null;
            var invokeForm = new Form();
            InvokeRequiredInfo.SynchronizeObject = invokeForm;

            // one thread is firing static events through the DelayedEventHandlerController.
            var t = new Thread(() =>
                {
                    try
                    {
                        while(!done)
                        {
                            // the bindinglists are subscribed to this event and are responding to it
                            DelayedEventHandlerController.FireEvents = !DelayedEventHandlerController.FireEvents;
                        }
                    }
                    catch (Exception e)
                    {
                        exceptionMessage = e.ToString();
                    }
                });
            t.Start();

            // while another thread is creating & disposing binding lists
            for (int i = 0; i < 100000; i++)
            {
                var bindingList = new FunctionBindingList(function) { SynchronizeInvoke = invokeForm, SynchronizeWaitMethod = Application.DoEvents };
                bindingList.Dispose();
            }
            done = true;
            t.Join();

            DelayedEventHandlerController.FireEvents = true; // reset firing..

            // see if exceptions occured in the other thread, if so, fail
            if (exceptionMessage != null)
                Assert.Fail(exceptionMessage);
        }

        [Test]
        [Category(TestCategory.WindowsForms)]
        public void DisposingFunctionBindingListFromOtherThreadsWhileModifying()
        {
            IFunction function = new Function();
            function.Arguments.Add(new Variable<string>("str"));
            function.Components.Add(new Variable<int>("value"));
            function["aap"] = 0;
            function["noot"] = 1;
            function["mies"] = 1;

            var done = false;
            string exceptionMessage = null;

            var invokeForm = new Form();
            var handle = invokeForm.Handle; // force creation
            InvokeRequiredInfo.SynchronizeObject = invokeForm;

            // one thread is thread is creating & disposing binding lists
            var t = new Thread(() =>
            {
                try
                {
                    for (int i = 0; i < 10000; i++)
                    {
                        var bindingList = new FunctionBindingList(function)
                            {
                                SynchronizeInvoke = invokeForm,
                                SynchronizeWaitMethod = Application.DoEvents
                            };
                        bindingList.Dispose();
                    }
                }
                catch (Exception e)
                {
                    exceptionMessage = e.ToString();
                }
                finally
                {
                    done = true;
                }
            });
            t.Start();

            // while another is firing static events through the DelayedEventHandlerController.
            while (!done)
            {
                // the bindinglists are subscribed to this event and are responding to it
                DelayedEventHandlerController.FireEvents = !DelayedEventHandlerController.FireEvents;
                Application.DoEvents();
            }
            t.Join();

            DelayedEventHandlerController.FireEvents = true; // reset firing..

            // see if exceptions occured in the other thread, if so, fail
            if (exceptionMessage != null)
                Assert.Fail(exceptionMessage);
        }

        [Test]
        public void ResetBindingListWhenArgumentsOrValuesChange()
        {
            IFunction function = new Function();

            function.Arguments.Add(new Variable<int>("index"));
            function.Components.Add(new Variable<int>("value"));

            function[0] = 1;
            function[1] = 2;
            function[2] = 3;

            IFunctionBindingList bindingList= new FunctionBindingList(function);

            Assert.AreEqual(3, bindingList.Count);

            function.Components.Clear();

            Assert.AreEqual(0, bindingList.Count);
        }

        [Test]
        public void RemoveRowsWhenArgumentValueIsRemovedIn2DFunction_Dimension0()
        {
            IFunction function = new Function();

            function.Arguments.Add(new Variable<int>("x1"));
            function.Arguments.Add(new Variable<int>("x2"));
            function.Components.Add(new Variable<string>("y"));

            function[0, 0] = "00";
            function[0, 1] = "01";
            function[1, 0] = "10";
            function[1, 1] = "11";

            var functionBindingList = new FunctionBindingList(function);

            function.Arguments[0].Values.RemoveAt(1);
            
            Assert.AreEqual(function.Components[0].Values.Count, functionBindingList.Count);
        }

        [Test]
        [Category(TestCategory.WindowsForms)]
        public void RemoveRowsWhenArgumentValueIsRemovedIn2DFunction_Dimension0_WithForm()
        {
            IFunction function = new Function();

            function.Arguments.Add(new Variable<int>("x1"));
            function.Arguments.Add(new Variable<int>("x2"));
            function.Components.Add(new Variable<string>("y"));

            function[0, 0] = "00";
            function[0, 1] = "01";
            function[1, 0] = "10";
            function[1, 1] = "11";

            var gridView = new DataGridView();
            IFunctionBindingList functionBindingList = new FunctionBindingList(function) { SynchronizeInvoke = gridView };
            gridView.DataSource = functionBindingList;

            Action<Form> showAction = delegate
                                          {
                                              function.Arguments[0].Values.RemoveAt(1);
                                              Application.DoEvents();
                                              Thread.Sleep(50); 
                                              Application.DoEvents();
                                              Thread.Sleep(50);
                                              Application.DoEvents();
                                              Assert.AreEqual(function.Components[0].Values.Count, functionBindingList.Count);
                                          };
            WindowsFormsTestHelper.ShowModal(gridView, showAction);
        }

        [Test]
        public void InsertRowsWhenArgumentValueAreInsertedIn2DFunction_Dimension0()
        {
            IFunction function = new Function();

            var variablex1 = new Variable<int>("x1");
            function.Arguments.Add(variablex1);
            var variablex2 = new Variable<int>("x2");
            function.Arguments.Add(variablex2);
            function.Components.Add(new Variable<string>("y"));
            var value = "somevalue";
            function.Components[0].DefaultValue =  value ;
            function[0, 0] = "00";
            function[0, 1] = "01";
            function[2, 0] = "20";
            function[2, 1] = "21";

            IFunctionBindingList functionBindingList = new FunctionBindingList(function);

            variablex1.Values.Insert(0,42); //autosorted

            //check functionbindinglist values
            //two functionbindinglist rows where added at position 2 and three of the bindinglist containing default componentvalues
            Assert.AreEqual(value, ((FunctionBindingListRow)functionBindingList[4])[2]);
            Assert.AreEqual(value, ((FunctionBindingListRow)functionBindingList[5])[2]);
            Assert.AreEqual(function.Components[0].Values.Count, functionBindingList.Count);
        }

        [Test]
        public void InsertNewArgumentAndComponentValuesInto1Arg3CompArray()
        {
            IFunction function = new Function();
            var argument = new Variable<int>("argumentValue");
            function.Arguments.Add(argument);
            var component1 = new Variable<int>("component1");
            var component2 = new Variable<int>("component2");
            var component3 = new Variable<int>("component3");
            function.Components.Add(component1);
            function.Components.Add(component2);
            function.Components.Add(component3);

            var functionBindingList = new FunctionBindingList(function);

            function[0] = new[] {1, 2, 3};
            function[3] = new[] {4, 5, 6};
            function[2] = new[] {7, 8, 9};

            var firstRow = functionBindingList[0];
            var secondRow = functionBindingList[1];
            var thirdRow = functionBindingList[2];
            
            Assert.AreEqual(firstRow[1], 1);
            Assert.AreEqual(secondRow[2],secondRow["component2"]);
            Assert.AreEqual(thirdRow[3],6);

            function[1] = new[] {12, 13, 14};

            Assert.AreEqual(functionBindingList[1][1], 12);
            Assert.AreEqual(functionBindingList[1][2], 13);
            
            //to see results: uncomment the following
            //var gridView = new DataGridView { DataSource = functionBindingList };
            //WindowsFormsTestHelper.ShowModal(gridView);
        }

        [Test]
        public void Filtered2DFunction()
        {
            IFunction function = new Function
                                     {
                                         Arguments = { new Variable<int>("x"), new Variable<int>("y") },
                                         Components = {new Variable<int>("f")}
                                     };

            function[1, 1] = 1;
            function[1, 2] = 2;
            function[2, 1] = 3;
            function[2, 2] = 4;

            var filteredFunction = function.Filter(new VariableValueFilter<int>(function.Arguments[0], new[]{1}));

            IFunctionBindingList functionBindingList = new FunctionBindingList(filteredFunction);

            functionBindingList.Count
                .Should().Be.EqualTo(2);
        }

        [Test] 
        public void ClearSourceFunctionShouldClearBindingList()
        {
            IFunction function = new Function
            {
                Arguments = { new Variable<int>("x") },
                Components = { new Variable<int>("f") }
            };

            function[1] = 1;
            function[2] = 4;

            var functionBindingList = new FunctionBindingList(function);

            functionBindingList.Count
                .Should().Be.EqualTo(2);

            function.Clear();

            functionBindingList.Count
                .Should().Be.EqualTo(0);
        }

        [Test]
        public void ClearSourceFunctionShouldClearFilteredBindingList()
        {
            IFunction function = new Function
            {
                Arguments = { new Variable<int>("x")},
                Components = { new Variable<int>("f") }
            };

            function[1] = 1;
            function[2] = 4;

            var filteredFunction = function.Filter(new VariableValueFilter<int>(function.Arguments[0], new[] { 1 }));

            IFunctionBindingList functionBindingList = new FunctionBindingList(filteredFunction);

            functionBindingList.Count
                .Should().Be.EqualTo(1);

            function.Clear();

            functionBindingList.Count
                .Should().Be.EqualTo(0);
        }

        [Test]
        public void ModifySourceFunctionShouldNotClearFilteredBindingList()
        {
            IFunction function = new Function
            {
                Arguments = { new Variable<int>("x") },
                Components = { new Variable<int>("f") }
            };

            function[1] = 1;
            function[2] = 4;

            var filteredFunction = function.Filter(new VariableValueFilter<int>(function.Arguments[0], new[] { 1 }));

            IFunctionBindingList functionBindingList = new FunctionBindingList(filteredFunction);

            functionBindingList.Count
                .Should().Be.EqualTo(1);

            function.Arguments[0].AddValues(new[] {3});

            functionBindingList.Count
                .Should().Be.EqualTo(1);
        }

        [Test]
        public void TestAddNewCoreUsesValueGenerationDelegate()
        {
            IFunction function = new Function();
            var variable = new Variable<TestClass>();
            variable.NextValueGenerator = new TestClassNextValueGenerator();
            function.Arguments.Add(variable);
            
            function.Components.Add(new Variable<int>());

            FunctionBindingList functionBindingList = new FunctionBindingList(function);
            //add two values..the first one is always OK!!??
            functionBindingList.AddNew();
            functionBindingList[0][1] = 3;
            functionBindingList[0].EndEdit();

            Assert.AreEqual("kees", variable.Values[0].Name);
        }

        [Test]
        public void TestEndNewCancelsOnUncommittedValues()
        {
            IFunction function = new Function();
            function.Arguments.Add(new Variable<int>());
            function.Components.Add(new Variable<int>());

            var functionBindingList = new FunctionBindingList(function);
            
            // add one real row
            functionBindingList.AddNew();
            functionBindingList[0][0] = 1;
            functionBindingList[0][1] = 2;
            functionBindingList[0].EndEdit(); //commits
            functionBindingList.EndNew(0);

            // add one row (but do not commit)
            functionBindingList.AddNew();
            functionBindingList[1][1] = 3;

            Assert.AreEqual(2, functionBindingList.Count);

            functionBindingList.EndNew(1); //should cause a cancel: uncommitted values

            Assert.AreEqual(1, functionBindingList.Count);
        }

        private class TestClassNextValueGenerator:NextValueGenerator<TestClass>
        {
            public override TestClass GetNextValue()
            {
                return new TestClass {Name = "kees"};
            }
        }
        private class TestClass:IComparable
        {
            public string Name { get; set; }
            public int CompareTo(object obj)
            {
                if (obj == this)
                    return 0;
                return 1;
            }
        }

        [Test]
        [Ignore("Work in progress, inserting values into 2d function is not implemented yet")]
        public void InsertRowsWhenArgumentValueAreInsertedIn2DFunction_Dimension1()
        {
            IFunction function = new Function();

            function.Arguments.Add(new Variable<int>("x1"));
            function.Arguments.Add(new Variable<int>("x2"));
            function.Components.Add(new Variable<string>("y"));

            function[0, 0] = "00";
            function[0, 2] = "02";
            function[1, 0] = "10";
            function[1, 2] = "12";

            IFunctionBindingList functionBindingList = new FunctionBindingList(function);

            function.Arguments[1].Values.Insert(1, 1);

            Assert.AreEqual(function.Components[0].Values.Count, functionBindingList.Count);
        }

        [Test]
        [Category(TestCategory.Performance)]
        [Category(TestCategory.BadQuality)] // TODO: split this test in two tests measuring actual time and use TestHelper.AssertIsFasterThan()
        public void AddManyFunctionValuesWithBindingListShouldBeFast()
        {
            var f = new Function
                        {
                            Arguments = {new Variable<double>("x")}, 
                            Components = {new Variable<double>("y")}
                        };

            var values = Enumerable.Range(0, 1000).Select(i => (double)i);

            // first time is slower so we add/clear values a few times first
            f.SetValues(values, new VariableValueFilter<double>(f.Arguments[0], values));
            f.Clear();
            f.SetValues(values, new VariableValueFilter<double>(f.Arguments[0], values));
            f.Clear();

            // measure overhead (add values to function)
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            AddValuesWithoutBindingList(f, values);
            stopwatch.Stop();

            var overhead = stopwatch.ElapsedMilliseconds;
            log.DebugFormat("Added 1000 values to function in {0} ms", overhead);

            f.Clear();

            // add values with FunctionBindingList
            new FunctionBindingList(f);

            stopwatch.Reset();
            stopwatch.Start();
            AddValuesWithBindingList(f, values);
            stopwatch.Stop();

            log.DebugFormat("Added 1000 values to function wrapped with a binding list in {0} ms, {1}x slower", stopwatch.ElapsedMilliseconds, (stopwatch.ElapsedMilliseconds / (double)overhead));

            (stopwatch.ElapsedMilliseconds - overhead)
                .Should("Updating binding list should be fast").Be.LessThan(70);

            (stopwatch.ElapsedMilliseconds / (double)overhead)
                .Should("Setting values to function with function binding list should be almost as fast as without it")
                    .Be.LessThan(6); // 6x slower!!!
        }

        [Test]
        public void FunctionBindingListShouldSuspendUpdatesDuringFireEventsFalse()
        {
            //add a function with a bindinglist
            var f = new Function
            {
                Arguments = { new Variable<double>("x") },
                Components = { new Variable<double>("y") }
            };

            var bindingList = new FunctionBindingList(f);
            
            //fake a model run by setting DelayedEventHandlerController.FireEvents to false (rename it to eventscontroller?)
            DelayedEventHandlerController.FireEvents = false;

            try
            {
                //write a lot of values in function...
                var argumentValues = Enumerable.Range(1, 1000).Select(i => Convert.ToDouble(i));
                var componentValues = Enumerable.Range(1, 1000).Select(i => Convert.ToDouble(i));
                f.SetComponentArgumentValues(componentValues,argumentValues);

                //assert the bindinglist did NOT update
                Assert.AreEqual(0,bindingList.Count);
            }
            finally
            {
                //re-enable event in DelayedEventHandlerController
                DelayedEventHandlerController.FireEvents = true;
            }

            //check the bindinglist got the message..(rowcount+sending a 'reset' event to be handled by the tableview.
            Assert.AreEqual(1000, bindingList.Count);


        }

        [Test]
        [Category(TestCategory.Performance)]
        public void GetIndexOfRowIsFast()
        {
            var f = new Function
            {
                Arguments = { new Variable<int>("x") },
                Components = { new Variable<int>("y") }
            };

            // function with 10000 values
            f.SetComponentArgumentValues(Enumerable.Range(1, 10000), Enumerable.Range(1, 10000));

            var bindingList = new FunctionBindingList(f);
            TestHelper.AssertIsFasterThan(15, () =>
                                                  {

                                                      foreach (var row in bindingList)
                                                      {
                                                          var i = bindingList.GetIndexOfRow(row);
                                                      }
                                                  });

        }

        [Test]
        public void ChangeFireEventsWithDisposedList()
        {
            //relates to issue 5520 where a bindinglist fill is called on a disposed list.
            //this is because the dispose is called on FireEventsChanged as well as the fill.
            //because invocation list is not refreshed after dispose the fill is still called

            DelayedEventHandlerController.FireEvents = false;
            try
            {
                FunctionBindingList bindingList = null;
            
                //create a disposing handler
                DelayedEventHandlerController.FireEventsChanged += (s, e) => { bindingList.Dispose(); };

                //create a bindinglist (second event handler)
                Function f = GetFunction();
                bindingList = new FunctionBindingList(f);
            }
            finally
            {
                //cause the event to be fired
                DelayedEventHandlerController.FireEvents = true;
            }
        }

        [Test]
        [Category(TestCategory.Jira)]
        public void RefreshingWithoutFunctionSetShouldNotThrowTools9100()
        {
            var bindingList = new FunctionBindingList(null);
            Assert.DoesNotThrow(bindingList.Refresh);
        }

        private Function GetFunction()
        {
            var f = new Function
                        {
                            Arguments = { new Variable<int>("x") },
                            Components = { new Variable<int>("y") }
                        };
            //function with 10000 values
            f.SetComponentArgumentValues(Enumerable.Range(1, 10000), Enumerable.Range(1, 10000));
            return f;
        }

        /// <summary>
        /// Use separate method for profiling 
        /// </summary>
        /// <param name="f"></param>
        /// <param name="values"></param>
        private void AddValuesWithBindingList(Function f, IEnumerable<double> values)
        {
            f.SetValues(values, new VariableValueFilter<double>(f.Arguments[0], values));
        }

        /// <summary>
        /// Use separate method for profiling 
        /// </summary>
        /// <param name="f"></param>
        /// <param name="values"></param>
        private void AddValuesWithoutBindingList(Function f, IEnumerable<double> values)
        {
            f.SetValues(values, new VariableValueFilter<double>(f.Arguments[0], values));
        }
   }
}