using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using DelftTools.Functions.Binding;
using DelftTools.Functions.Filters;
using DelftTools.Functions.Generic;
using DelftTools.Functions.Tuples;
using DelftTools.TestUtils;
using DelftTools.Units;
using DelftTools.Utils.Threading;
using log4net;
using NUnit.Framework;
using TypeConverter=DelftTools.Utils.TypeConverter;

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
        public void MultipleFunctions()
        {
            IFunction function1 = new Function();

            function1.Arguments.Add(new Variable<int>("x"));
            function1.Components.Add(new Variable<double>("y1"));

            function1[0] = 1.0;
            function1[1] = 2.0;
            function1[2] = 3.0;

            IFunction function2 = new Function();

            function2.Arguments.Add(new Variable<int>("x"));
            function2.Components.Add(new Variable<double>("y2"));

            function2[0] = 1.0;
            function2[1] = 4.0;
            function2[2] = 9.0;

            List<IFunction> functions = new List<IFunction>() { function1, function2 };
            IFunctionBindingList functionBindingList = new MultipleFunctionBindingList(functions);

            Assert.AreEqual("x", functionBindingList.DisplayNames[0]);
            Assert.AreEqual("y1", functionBindingList.DisplayNames[1]);
            Assert.AreEqual("y2", functionBindingList.DisplayNames[2]);

            MultipleFunctionBindingListRow firstRow = (MultipleFunctionBindingListRow)functionBindingList[0];
            Assert.AreEqual(0, firstRow["x"]);
            Assert.AreEqual(1, firstRow["y1"]);
            Assert.AreEqual(1, firstRow["y2"]);

            MultipleFunctionBindingListRow secondRow = (MultipleFunctionBindingListRow)functionBindingList[1];
            Assert.AreEqual(1, secondRow["x"]);
            Assert.AreEqual(2, secondRow["y1"]);
            Assert.AreEqual(4, secondRow["y2"]);
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

            Assert.AreEqual(row[1], three); //still set
            Assert.AreEqual(row[1], function[0]); //committed
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

            variablex1.Values.Insert(1,42);

            //check functionbindinglist values
            //two functionbindinglist rows where added at position 2 and three of the bindinglist containing default componentvalues
            Assert.AreEqual(value, ((FunctionBindingListRow)functionBindingList[2])[2]);
            Assert.AreEqual(value, ((FunctionBindingListRow)functionBindingList[3])[2]);
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
            
            Assert.AreEqual("kees", variable.Values[0].Name);
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
        [Category(TestCategory.WorkInProgress)] // slow
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

            //write a lot of values in function...
            var argumentValues = Enumerable.Range(1, 1000).Select(i => Convert.ToDouble(i));
            var componentValues = Enumerable.Range(1, 1000).Select(i => Convert.ToDouble(i));
            f.SetComponentArgumentValues(componentValues,argumentValues);

            //assert the bindinglist did NOT update
            Assert.AreEqual(0,bindingList.Count);

            //re-enable event in DelayedEventHandlerController
            DelayedEventHandlerController.FireEvents = true;

            //check the bindinglist got the message..(rowcount+sending a 'reset' event to be handled by the tableview.
            Assert.AreEqual(1000, bindingList.Count);


        }

        [Test]
        [Category(TestCategory.Performance)]
        [Category(TestCategory.WorkInProgress)] // slow
        public void GetIndexOfRowIsFast()
        {
            var f = new Function
            {
                Arguments = { new Variable<int>("x") },
                Components = { new Variable<int>("y") }
            };
            //function with 10000 values
            f.SetComponentArgumentValues(Enumerable.Range(1, 10000), Enumerable.Range(1, 10000));

            var bindingList = new FunctionBindingList(f);
            TestHelper.AssertIsFasterThan(50, () =>
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

            FunctionBindingList bindingList = null;
            
            //create a disposing handler
            DelayedEventHandlerController.FireEventsChanged += (s, e) => { bindingList.Dispose(); };

            //create a bindinglist (second event handler)
            Function f = GetFunction();
            bindingList = new FunctionBindingList(f);

            //cause the event to be fired
            DelayedEventHandlerController.FireEvents = true;
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