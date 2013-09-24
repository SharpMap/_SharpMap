using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DelftTools.Functions.Filters;
using DelftTools.Functions.Generic;
using DelftTools.TestUtils;
using DelftTools.Utils.Collections;
using log4net;
using NUnit.Framework;
using SharpTestsEx;

namespace DelftTools.Functions.Tests
{

    [TestFixture]
    public class MemoryFunctionStoreTest
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(MemoryFunctionStore));

        [TestFixtureSetUp]
        public void SetUp()
        {
            LogHelper.ConfigureLogging();
        }

        [TestFixtureTearDown]
        public void TearDown()
        {
            LogHelper.ResetLogging();
        }

        [Test]
        public void DefaultVariableStore()
        {
            IVariable x = new Variable<double>();
            Assert.IsTrue(x.Store is MemoryFunctionStore, "default value store of the variable should be MemoryFunctionStore");
        }
        
        [Test]
        public void SetSingleVariableValues()
        {
            IVariable x = new Variable<double>();
            var values = new[] { 1.0, 2.0, 3.0 };
            x.Store.SetVariableValues(x, values);
            
            
            Assert.AreEqual(3, x.Values.Count, "values assigned directly to store must appear in the variable");
            
            //redefine value
            values = new[] { 4.0, 5.0, 6.0 };
            x.Store.SetVariableValues(x, values);
            Assert.AreEqual(6, x.Values.Count);
        }
        [Test]
        public void CheckFunctionValuesChangedConsistency()
        {
            //assert we only get a FunctionValuesChanged event when the function is consistent.
            IFunction func = new Function();
            IVariable x = new Variable<int>("x");
            IVariable y = new Variable<int>("y");
            func.Arguments.Add(x);
            func.Components.Add(y);
            func[0] = 2;
            func.Store.FunctionValuesChanged += delegate
                                                    {
                                                        Assert.IsTrue(func.Components[0].Values.Count == func.Arguments[0].Values.Count);
                                                    };
            func.Clear();
        }

        
        [Test]
        public void CopyComponentWhenAssignedToDifferentStore()
        {
            IFunction func = new Function();
            IVariable y = new Variable<int>("y");
            IVariable x = new Variable<int>("x");
            IVariable h = new Variable<int>("H");
            func.Arguments.Add(x);
            func.Arguments.Add(y);
            func.Components.Add(h);
            h[1, 1] = 1;
            Assert.AreEqual(1, h[1, 1]);
            
            //switch store
            IFunctionStore functionStore = new MemoryFunctionStore();
            functionStore.Functions.Add(func);
            Assert.AreEqual(4,functionStore.Functions.Count);
            Assert.AreEqual(1,func.Components[0][1,1]);
        }

        [Test]
        public void CopyDependendFunctionValuesWhenAdded()
        {
            //depended variable 
            IVariable y = new Variable<int>("y");
            IVariable x = new Variable<int>("x");
            
            y.Arguments.Add(x);
            y.SetValues(new[] { 10, 20, 30 }, new VariableValueFilter<int>(x, new[] { 1, 2, 3 }));

            //switch store
            var store = new MemoryFunctionStore();
            store.Functions.Add(y);

            //get values for x and y
            Assert.AreEqual(3, store.GetVariableValues(x).Count);
            Assert.AreEqual(3, store.GetVariableValues(y).Count);

            Assert.AreEqual(30,y[3]);
        }

        [Test]
        public void CopyIndependendFunctionValuesWhenAdded()
        {
            //indep variable 
            IVariable x = new Variable<double>();

            var values = new[] { 1.0, 2.0, 3.0 };
            x.Store.SetVariableValues(x, values);

            var store = new MemoryFunctionStore();
            store.Functions.Add(x);

            Assert.AreEqual(3.0, store.GetVariableValues(x)[2]);
            Assert.AreEqual(3, store.GetVariableValues(x).Count);
        }
        
        [Test]
        public void StoreTwoIndependentVariables()
        {
            IVariable x = new Variable<double>();
            IVariable y = new Variable<double>();
            x.Store.Functions.Add(y); // use store from x

            x.SetValues(new[] { 1.0, 2.0, 3.0 });
            y.SetValues(new[] { 10.0, 20.0, 30.0, 40.0 });

            var store = x.Store;

            Assert.AreEqual(2, store.Functions.Count);
            Assert.AreEqual(3, store.GetVariableValues(x).Count);
            Assert.AreEqual(4, store.GetVariableValues(y).Count);
        }

        [Test]
        public void ShareArgumentVariablesBetweenFunctionsInOneValueStore()
        {
            IVariable xVariable = new Variable<double>("x");
            IVariable tVariable = new Variable<double>("t");
            IVariable f1Variable = new Variable<double>("f1");
            IVariable f2Variable = new Variable<double>("f2");
            IVariable f3Variable = new Variable<double>("f2");

            IFunction f1 = new Function();
            f1.Arguments.Add(xVariable);
            f1.Arguments.Add(tVariable);
            f1.Components.Add(f1Variable);
            f1.Components.Add(f2Variable);

            IFunction f2 = new Function();

            IFunctionStore store = f1.Store;
            store.Functions.Add(f2); // add it to the same store where f1 is stored

            f2.Arguments.Add(xVariable);
            f2.Arguments.Add(tVariable);
            f2.Components.Add(f3Variable);
            // Assert the store is the same for every thing
            Assert.AreSame(f1.Store,f2.Store);
            Assert.AreSame(f1.Store, xVariable.Store);
            Assert.AreSame(f1.Store, tVariable.Store);
            Assert.AreSame(f1.Store, f1Variable.Store);
            Assert.AreSame(f1.Store, f2Variable.Store);
            Assert.AreSame(f1.Store, f3Variable.Store);
        }
        [Test]
        public void SimpleDependendVariable()
        {
            //create a single variable dependency.
            IVariable<double> x = new Variable<double>("x");
            IVariable<double> y = new Variable<double>("y");
            y.Arguments.Add(x);

            IFunctionStore store = y.Store;
            
            store.SetVariableValues(x, new[] {0.0, 0.1, 0.2});
            
            Assert.AreEqual(3, store.GetVariableValues(y).Count);
        }

        [Test]
        public void DependentOn2Variables()
        {
            IVariable<double> x1 = new Variable<double>("x1");
            IVariable<double> x2 = new Variable<double>("x2");
            IVariable<double> y = new Variable<double>("y");

            y.Arguments.Add(x1);
            y.Arguments.Add(x2);

            IFunctionStore store = y.Store;

            store.SetVariableValues(x1, new[] { 0.0, 0.1, 0.2 });
            store.SetVariableValues(x2, new[] { 0.0, 0.1 });

            Assert.AreEqual(6, store.GetVariableValues(y).Count);
        }

        private class TestNode : IComparable
        {
            public string Name { get; set; }

            public int CompareTo(object obj)
            {
                return obj is TestNode ? 0 : -1;
            }
        }

        [Test]
        [Ignore("Used for code behavior mimicing in rewrite of GetVariableValueFilterIndexes (and that works now), but test as a whole failes due to non-comparability in other code parts")]
        public void GetVariableValueFiltersShouldDealWithNonValueComparableValues()
        {
            IVariable<TestNode> x = new Variable<TestNode>("x");
            IVariable<double> y = new Variable<double>("y");

            var amount = 2;

            y.Arguments.Add(x);

            var node1 = new TestNode { Name = "1" };
            var node2 = new TestNode { Name = "2" };
            var node3 = new TestNode { Name = "3" };
            var node4 = new TestNode { Name = "4" };

            y[node1] = 2.0;
            y[node2] = 5.0;
            y[node3] = 8.0;
            y[node4] = 10.0;

            IFunctionStore store = y.Store;

            IList<TestNode> valuesToSelect = new List<TestNode>();
            valuesToSelect.Add(node1);
            valuesToSelect.Add(node2);
            valuesToSelect.Add(node3);
            valuesToSelect.Add(node4);

            IMultiDimensionalArray array = store.GetVariableValues(x, new VariableValueFilter<TestNode>(x, valuesToSelect));

            array[0].Should("1").Be.EqualTo(node1);
            array[1].Should("2").Be.EqualTo(node2);
            array[2].Should("3").Be.EqualTo(node3);
            array[3].Should("4").Be.EqualTo(node4);
        }

        [Test]
        [Category(TestCategory.Performance)]
        public void GetVariableValueFilterIndexesShouldBeFast()
        {
            IVariable<double> x = new Variable<double>("x");
            IVariable<double> y = new Variable<double>("y");

            var amount = 5000;

            IList<double> allValues = new List<double>(amount);

            for (int i = 0; i < amount; i++)
                allValues.Add(i);
            
            x.AddValues(allValues);

            y.Arguments.Add(x);

            IFunctionStore store = y.Store;

            IList<double> valuesToSelect = new List<double>();
            valuesToSelect.Add(allValues[0]);
            valuesToSelect.Add(allValues[50]);
            valuesToSelect.Add(allValues[amount-1]);

            IMultiDimensionalArray array = null;

            TestHelper.AssertIsFasterThan(130,
                                          () =>
                                              {
                                                  for (int i = 0; i < 5000; i++)
                                                  {
                                                      array = store.GetVariableValues(x,new VariableValueFilter<double>(x,valuesToSelect));
                                                  }
                                              });
            //orig: 600ms
            //now: 15ms

            Assert.AreEqual(3,array.Count);
        }

        
        [Test]
        public void FunctionContainingTwoComponents()
        {

            IVariable x1= new Variable<double>("x");
            IVariable x2 = new Variable<double>("x");
            IVariable f1 = new Variable<double>("f1");
            IVariable f2 = new Variable<double>("f2");

            IFunction function = new Function();
            function.Arguments.Add(x1);
            function.Arguments.Add(x2);
            function.Components.Add(f1);
            function.Components.Add(f2);
            Assert.AreEqual(2,function.Components[0].Values.Rank);
        }
        
        [Test]
        [ExpectedException(typeof(ArgumentException), ExpectedMessage = "Value of type System.String, but expected type System.Int32 for variable variable")]
        public void SetValuesWithAWrongTypeGivesFormatException()
        {
            IVariable<int> x = new Variable<int>();

            //go and put a bad string in there
            x.SetValues(new [] {"lalala"});
        }

        [Test]
        public void SetValuesUsingFilters2D()
        {
            //Y is dependend of x1 and x2. 
            IVariable<int> y = new Variable<int>();
            IVariable<int> x1 = new Variable<int>();
            IVariable<int> x2 = new Variable<int>();
            y.Arguments.Add(x1);
            y.Arguments.Add(x2);

            x1.SetValues(new[] { 0, 1, 2 });
            x2.SetValues(new[] { 0, 1, 2 });
            Assert.AreEqual(9,y.Values.Count);
            //set y = 2 where x = 0 or x=2 
            //or is this call wrong and should it be {5,5} or setfunctionValue
            y.Store.SetVariableValues(y, new[] { 5 }, new VariableValueFilter<int>(x1, new[] { 0 }));
            
            //check values of y using MDA interface. The first argument is x1
            Assert.AreEqual(5,y.Values[0, 0]);
            Assert.AreEqual(5,y.Values[0, 1]);
            Assert.AreEqual(5,y.Values[0, 2]);

            y.Store.SetVariableValues(y, new[] { 3,2,1 }, new VariableValueFilter<int>(x2, new[] { 0 }));
            Assert.AreEqual(3, y.Values[0, 0]);
            Assert.AreEqual(2, y.Values[1, 0]);
            Assert.AreEqual(1, y.Values[2, 0]);
            Assert.AreEqual(5, y.Values[0, 1]);
            Assert.AreEqual(5, y.Values[0, 2]);
        }


        [Test]
        public void GetIndependentValuesFiltersGeneric()
        {
            IFunctionStore store = new MemoryFunctionStore();

            IVariable<double> x1 = new Variable<double>("x1");
            //add one independent variable
            store.Functions.Add(x1);

            x1.SetValues(new[] { 0.0d, 1.0d, 2.0d });
            Assert.AreEqual(0.0, x1.Values[0]);

            IMultiDimensionalArray<double> filteredValues = store.GetVariableValues<double>(x1, new VariableValueFilter<double>(x1, new[] {0.0d, 2.0d}));
            Assert.AreEqual(0.0, filteredValues[0]);
            Assert.AreEqual(2.0, filteredValues[1]);
        }

        [Test]
        public void GetIndependentValuesUsingMultipleFilters()
        {
            Variable<int> x = new Variable<int>();
            x.SetValues(new[] {1, 2, 3, 4, 5});
            
            IFunctionStore store = x.Store;

            IMultiDimensionalArray<int> filteredValues;

            filteredValues = store.GetVariableValues<int>(
                x, 
                new VariableValueFilter<int>(x, new[] {1, 2, 3})
                );

            Assert.AreEqual(3, filteredValues.Count);
            Assert.AreEqual(1, filteredValues[0]);

            //same filters different ordering
            filteredValues = store.GetVariableValues<int>(
                x, 
                new VariableValueFilter<int>(x, new[] {3, 2, 1})
                );

            Assert.AreEqual(3, filteredValues.Count);
            Assert.AreEqual(1, filteredValues[0]);
        }
        [Test]
        public void SetDependendVariable()
        {
            // y = f(x) 

            //IFunctionStore store = new MemoryFunctionStore();
            IVariable<double> x = new Variable<double>("x");
            IVariable<double> y = new Variable<double>("y");
            y.Arguments.Add(x);
            x.SetValues(new [] {1.0d,2.0d,3.0d});
            y.SetValues(new [] {2.0d,4.0d,6.0d});

            //store.Functions.Add(y);
            //what value for y where x = 3?
            IMultiDimensionalArray<double> xValues = y.Store.GetVariableValues<double>(x, new VariableValueFilter<double>(x, new[] { 3.0d }));
            double d = xValues[0];
            Assert.AreEqual(3.0,d);
            //TODO : refactor to getvalue<TEventArgs>
            IMultiDimensionalArray<double> yValues = y.Store.GetVariableValues<double>(y, new VariableValueFilter<double>(x, new[] {3.0d}));
            Assert.AreEqual(6.0,yValues[0]);
        }

        [Test,Explicit("Is this really a nice feature?")]
        public void MakeFilteringLessTypeSensitive()
        {
            IVariable<double> x = new Variable<double>();
            x.SetValues(new[] { 1.0, 2.0, 3.0 });
            IMultiDimensionalArray<double> xValues = x.Store.GetVariableValues<double>(x, new VariableValueFilter<double>(x, 2));
            Assert.AreEqual(2.0, xValues[0]);
        }

        [Test]
        public void MultiDimensionalIndexOnFunctionValuesChanged()
        {
            IVariable xVariable = new Variable<double>("x");
            IVariable tVariable = new Variable<double>("t");
            IVariable f1Variable = new Variable<double>("f1");

            IFunction f1 = new Function();
            f1.Arguments.Add(xVariable);
            f1.Arguments.Add(tVariable);
            f1.Components.Add(f1Variable);

            f1[1.0d, 2.0d] = 25.0;
            f1.Store.FunctionValuesChanged += ((sender, e) => Assert.AreEqual(new[] {0, 0}, e.MultiDimensionalIndex));
            f1[1.0d, 2.0d] = 25.0;
        }

        [Test]
        public void RemoveValues()
        {
            IVariable<double> x = new Variable<double>("x");
            IVariable<double> y = new Variable<double>("y");

            IFunction f = new Function();
            f.Arguments.Add(x);
            f.Components.Add(y);

            f.SetValues(new[] { 100.0, 200.0, 300.0 }, new VariableValueFilter<double>(x, new[] { 1.0, 2.0, 3.0 }));

            //update argument
            f.Store.RemoveFunctionValues(x);

            //component resizes
            Assert.AreEqual(0,y.Values.Count);
        }

        [Test]
        public void RemovingAFunctionRemovesFunctionValues()
        {
            MemoryFunctionStore store= new MemoryFunctionStore();
            IFunction f = new Function();
            IVariable<double> x = new Variable<double>("x");
            store.Functions.Add(f);
            f.Components.Add(x);
            Assert.AreEqual(2,store.Functions.Count);
            f.Components.Clear();
            //store.Functions.Remove(x);
            Assert.AreEqual(1, store.Functions.Count);
        }

        [Test]
        public void Clone()
        {
            IVariable<int> x = new Variable<int>("x") { Values = { 1, 2, 3 } };
            IVariable<double> y = new Variable<double>("y");
            IFunction f = new Function { Arguments = {x}, Components = {y} };
            f.SetValues(new[] { 100.0, 200.0, 300.0 });
            var store = (MemoryFunctionStore)f.Store;

            var clone = (MemoryFunctionStore)store.Clone(); // clone it!

            clone.Functions.Count
                .Should().Be.EqualTo(3);

            clone.Functions[0]
                .Should("check f").Be.OfType<Function>();

            clone.Functions[1]
                .Should("check x").Be.OfType<Variable<int>>();

            clone.Functions[2]
                .Should("check y").Be.OfType<Variable<double>>();

            Assert.AreEqual(new [] { 100.0, 200.0, 300.0}, clone.Functions[0].GetValues());
        }

        [Test]
        public void CopyConstructor()
        {
            IVariable<int> x = new Variable<int>("x") { Values = { 1, 2, 3 } };
            IVariable<double> y = new Variable<double>("y");
            IFunction f = new Function { Arguments = { x }, Components = { y } };
            f.SetValues(new[] { 100.0, 200.0, 300.0 });
            var store = (MemoryFunctionStore)f.Store;

            var copy = new MemoryFunctionStore(store); // copy

            copy.Functions.Count
                .Should().Be.EqualTo(3);

            copy.Functions[0]
                .Should("check f").Be.OfType<Function>();

            copy.Functions[1]
                .Should("check x").Be.OfType<Variable<int>>();

            copy.Functions[2]
                .Should("check y").Be.OfType<Variable<double>>();

            var variables = copy.Functions.OfType<IVariable>();
            
            variables.ForEach(v => v.Values.Count.Should().Be.EqualTo(0));
        }

        [Test]
        [ExpectedException(typeof(ArgumentException), ExpectedMessage = "Number of values to be written to dependent variable 'y' exceeds argument values range. Got 3 values expected at most 1.")]
        public void FunctionStoreShouldThrowExceptionIfNumberOfValuesExceedsArgumentRange()
        {
            var f = FunctionHelper.Get1DFunction<int, int>();
            //setting 3 values for a single argument value doesn't fit
            f[1] = new[] {1, 2, 3};
        }

        [Test]
        [Category(TestCategory.Performance)]
        public void AddIndependentVariableValuesScalesWell()
        {
            var x = new Variable<double>("x");
            var y = new Variable<double>("x");
            y.Arguments.Add(x);

            // add an remove one value to make sure that all internal caches are initialized (TypeUtils.CallGeneric)
            x.Values.Add(0);
            x.Values.Clear();

            var memoryStore = y.Store;

            var values = new List<double>();

            for (int i = 0; i < 10000; i++)
            {
                values.Add(i);
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            memoryStore.AddIndependendVariableValues(x, values);
            stopwatch.Stop();

            var dt1 = stopwatch.ElapsedMilliseconds;

            log.DebugFormat("First call took {0} ms", dt1);
            stopwatch.Reset();

            for (int i = 0; i < 10000; i++)
            {
                values[i] = i + 10000;
            }
            
            stopwatch.Start();
            memoryStore.AddIndependendVariableValues(x, values);
            stopwatch.Stop();

            var dt2 = stopwatch.ElapsedMilliseconds;

            log.DebugFormat("Second call took {0} ms", dt2);

            var percentage = 100.0 * (dt2 - dt1) / dt1;

            log.DebugFormat("Difference is: {0}%", percentage); 
            Assert.IsTrue(percentage < 30, "Adding 10000 values second time is almost as fast as adding them second time, should be < 30% but was: "  + percentage + "%");
        }
   }
}