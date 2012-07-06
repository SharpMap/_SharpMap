using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using DelftTools.Functions.Filters;
using DelftTools.Functions.Generic;
using DelftTools.Functions.Tests.TestObjects;
using DelftTools.TestUtils;
using DelftTools.Units;
using DelftTools.Utils;
using DelftTools.Utils.Collections.Generic;
using DelftTools.Utils.IO;
using log4net;
using NUnit.Framework;
using Rhino.Mocks;
using Rhino.Mocks.Constraints;
using Category = NUnit.Framework.CategoryAttribute;

namespace DelftTools.Functions.Tests
{
    [TestFixture]
    public class FunctionTest
    {
        private static readonly ILog log = LogManager.GetLogger(typeof (FunctionTest));
        private static readonly MockRepository mocks = new MockRepository();

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            LogHelper.ConfigureLogging();

            // precache variables (reflection cache)

            new Variable<int>().GetValues();
            new Variable<int>().SetValues(new []{0});
            new Variable<double>().GetValues();
            new Variable<double>().SetValues(new[] { 0.0 });
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            LogHelper.ResetLogging();
        }

        [Test]
        [Category(TestCategory.Jira)] //TOOLS-4934
        public void EventBubbleCorerctlyForFunctionWithReducedArgument()
        {
            IFunction function = new Function
                                     {
                                         Arguments = {new Variable<int>("x1"), new Variable<int>("x2")},
                                         Components = {new Variable<int>("f")}
                                     };

            function[0, 1] = new[] {1};
            function[0, 2] = new[] {2};
            function[1, 1] = new[] {3};
            function[1, 2] = new[] {4};

            var arg1 = function.Arguments[0];

            var filteredFunction = function.Filter(new VariableValueFilter<int>(arg1, 0), new VariableReduceFilter(arg1));
            
            int called = 0;

            filteredFunction.ValuesChanged += (s, e) => called++;

            function[0, 2] = 3; //set value

            Assert.AreEqual(1, called);
        }

        [Test]
        public void FilteredFunctionStore()
        {
            IFunction function = new Function { Arguments = { new Variable<int>("x") }, Components = { new Variable<int>("y") } };

            function[1] = 1;
            function[2] = 2;

            var store = function.Store;
            
            var filteredFunction = function.Filter(new VariableValueFilter<int>(function.Arguments[0], new[] {2}));

            filteredFunction.Store
                .Should().Be.EqualTo(store);

            store.Functions.Count
                .Should("Filtered functions not added to store").Be.EqualTo(3);
        }

        [Test]
        public void SetFunctionValuesUsingFilteredFunction()
        {
            IFunction function = new Function
            {
                Arguments = { new Variable<int>("x") },
                Components = { new Variable<int>("f1"), new Variable<int>("f2") }
            };

            function[1] = new[] { 1, 10 };
            function[2] = new[] { 2, 20 };

            var filteredFunction = function.Filter(new VariableValueFilter<int>(function.Arguments[0], new[] { 2 }));

            var f1ValueChangeCount = 0;
            filteredFunction.Components[0].ValuesChanged += delegate { f1ValueChangeCount++; };

            // replace value
            filteredFunction.Components[0].Values[0] = 3;

            function.Components[0].Values.Cast<int>()
                .Should().Have.SameSequenceAs(new[] {1, 3});

            function.Components[1].Values.Cast<int>()
                .Should().Have.SameSequenceAs(new[] { 10, 20 });

            f1ValueChangeCount
                .Should().Be.EqualTo(1);
        }

        [Test]
        [Category("Example")]
        public void VelocitsyFieldFunction()
        {
            IList<int> ints = new List<int>();
            ints.Insert(0,2);
            Assert.AreEqual(1,ints.Count);
        }

        [Test]
        [Category("Example")]
        public void VelocityFieldFunction()
        {
            var x = new Variable<double> { Name = "x", Values = { 1, 2 } };
            var y = new Variable<double> { Name = "y", Values = { 1, 2, 3 } };
            var vx = new Variable<double> { Name = "vx" };
            var vy = new Variable<double> { Name = "vy" };

            var velocity = new Function { Components = { vx,  vy }, Arguments = { x, y } };

            vx.SetValues(new[] { 1.0, 2.0, 3.0, 4.0, 5.0, 6.0 });
            vy.SetValues(new[] { 1.0, 2.0, 3.0, 4.0, 5.0, 6.0 });

            velocity.Components[0].Should().Be.EqualTo(vx);
            velocity.Components[1].Should().Be.EqualTo(vy);
            velocity.Arguments[0].Should().Be.EqualTo(x);
            velocity.Arguments[1].Should().Be.EqualTo(y);

            vx.Values.Should().Have.SameSequenceAs(new[] { 1.0, 2.0, 3.0, 4.0, 5.0, 6.0 });
            vy.Values.Should().Have.SameSequenceAs(new[] { 1.0, 2.0, 3.0, 4.0, 5.0, 6.0 });
        }

        [Test]
        [Ignore("For communication")]
        public void SetComponentAtArgumentValueZero()
        {
            var function = new Function();

            function.Components.Add(new Variable<double>("y"));
            function.Arguments.Add(new Variable<double>("x"));

            //0.0 is replaced by 'nextvalue' because it is DefaultValue.
            //if you want to assign 0.0 you should change the DefaultValue now
            //-> Maybe dont wont a default by default or other default?
            //FIX:
            function.Arguments[0].DefaultValue = -999.0;
            var argumentValues = new[] {-10.0, 0.0, 10.0};
            var componentValues = new[] { 8.0, 9.0, 10.0 };
            for (int i = 0; i < argumentValues.Count(); i++)
            {
                function[argumentValues[i]] = componentValues[i];
            }

            Assert.AreEqual(new[] {-10.0, 0.0, 10.0},function.Arguments[0].Values);
            Assert.AreEqual(new[] { 8.0, 9.0, 10.0 }, function.Components[0].Values);
            function.Components[0].Values.Count
                .Should("there should be three values").Be.EqualTo(3);
            function.Components[0].Values[1]
                .Should("Value should be componentValue").Be.EqualTo(componentValues[1]);

            function.Arguments[0].Values.Count
                .Should("there should be three values").Be.EqualTo(3);
            function.Arguments[0].Values[1]
                .Should("Value should be argumentValues").Be.EqualTo(argumentValues[1]);
        }

        [Test]
        public void ArgumentValuesInsertAt()
        {
            IVariable x = new Variable<double>("x");
            IVariable y = new Variable<double>("y");

            IFunction f = new Function();
            f.Arguments.Add(x);
            f.Components.Add(y);

            f[10.0] = 100.0;
            f[20.0] = 200.0;

            x.Values.InsertAt(0, 1);

            Assert.AreEqual(3, y.Values.Count);
            Assert.AreEqual(y.DefaultValue, y.Values[0]);
            Assert.AreEqual(100.0, y.Values[1]);
            Assert.AreEqual(200.0, y.Values[2]);
        }

        [Test]
        public void GetDefaultValuesAfterArgument0ValueHasBeenRemoved()
        {
            IVariable<double> x = new Variable<double>("x");
            IVariable<double> y = new Variable<double>("y");
            IVariable<double> z = new Variable<double>("z");
            var defaultValue = 101.0;
            z.DefaultValue = defaultValue;

            IFunction f = new Function();
            f.Arguments.Add(x);
            f.Arguments.Add(y);
            f.Components.Add(z);

            x.Values.Add(1.0);
            y.Values.Add(1.0);
            Assert.AreEqual(defaultValue, (double)f[1.0, 1.0], 1.0e-6);

            Assert.AreEqual(1,z.Values.Count);
            x.Values.Remove(1.0);
            x.Values.Add(2.0);
            Assert.AreEqual(defaultValue, (double)f[2.0, 1.0], 1.0e-6);
        }

        [Test]
        public void ArgumentValuesRemoveAt()
        {
            IVariable<double> x = new Variable<double>("x");
            IVariable<double> y = new Variable<double>("y");

            IFunction f = new Function();
            f.Arguments.Add(x);
            f.Components.Add(y);

            f[10.0] = 100.0;
            f[20.0] = 200.0;
            f[30.0] = 300.0;

            x.Values.RemoveAt(1);

            Assert.AreEqual(2, y.Values.Count);
            Assert.AreEqual(100.0, y.Values[0]);
            Assert.AreEqual(300.0, y.Values[1]);
        }

        [Test]
        public void ArgumentValuesRemoveAtFiresCollectionChanged()
        {
            IVariable<double> x = new Variable<double>("x");
            IVariable<double> y = new Variable<double>("y");

            IFunction f = new Function();
            f.Arguments.Add(x);
            f.Components.Add(y);

            f[10.0] = 100.0;
            f[20.0] = 200.0;
            f[30.0] = 300.0;

            var functionCounter = 0;
            f.CollectionChanged += (s, e) => { functionCounter++; };

            var argumentCounter = 0;
            x.CollectionChanged += (s, e) => { argumentCounter++; };

            var argumentValuesChangingCounter = 0;
            x.Values.CollectionChanging += (s, e) => { argumentValuesChangingCounter++; };

            x.Values.RemoveAt(1);

            Assert.AreNotEqual(0, argumentValuesChangingCounter);
            //Assert.AreNotEqual(0, argumentCounter);
            //Assert.AreNotEqual(0, functionCounter);
        }

        [Test]
        public void Assign2ComponentsDifferentType()
        {
            // F = (fx,fy)(x)
            var f = new Function();
            f.Components.Add(new Variable<int>("fx"));
            f.Components.Add(new Variable<double>("fy"));
            f.Arguments.Add(new Variable<int>("x"));
            
            f[1] = new object[] {1, 1.5};
            
            Assert.AreEqual(1, f.Components[0].Values[0]);
            Assert.AreEqual(1.5, f.Components[1].Values[0]);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException), ExpectedMessage = "Value of type System.Double, but expected type System.Int32 for variable f")]
        public void AssignFloatValuesToIntVariableShouldThrowException()
        {
            IVariable f = new Variable<int>("f");
            IVariable x = new Variable<float>("x1");

            IFunction function = new Function("Fail Test");
            function.Components.Add(f);
            function.Arguments.Add(x);

            x.Values.Add(0.0f);
            function.SetValues(new[] {1.7},
                               new VariableValueFilter<float>(x, new[] {0.0f}));

            Assert.AreEqual(2, f.Values[0]);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException), ExpectedMessage = "Value of type System.Int32, but expected type System.Double for variable f")]
        public void AssignIntToDoubleComponentShouldThrowException()
        {
            IVariable c = new Variable<double>("f");
            IVariable x = new Variable<float>("x1");

            IFunction function = new Function("Fail Test");
            function.Components.Add(c);
            function.Arguments.Add(x);

            function[0.0f] = 5;
            
            Assert.AreEqual(5,function[0.0f]);
        }

        [Test]
        public void ChangeArgumentValueChangesFunction()
        {
            IFunction function = new Function();
            IVariable<int> component = new Variable<int>();
            IVariable<int> argument = new Variable<int>();
            function.Arguments.Add(argument);
            function.Components.Add(component);
            function[0] = 2;

            function.Arguments[0].Values[0] = 32;
            Assert.AreEqual(2, function.Components[0][32]);
        }

        [Test]
        public void ChangingArgumentValueMovesDimensionValues()
        {
            var function = new Function {Name = "F"};
            var x = new Variable<double>("x");
            var y = new Variable<double>("y");
            var f = new Variable<double>("f");

            function.Arguments.Add(x);
            function.Arguments.Add(y);
            function.Components.Add(f);

            // 1st way
            f[0.0, 0.0] = 0.0;
            f[0.0, 1.0] = 1.0;
            f[0.0, 2.0] = 2.0;
            f[1.0, 0.0] = 3.0;
            f[1.0, 1.0] = 4.0;
            f[1.0, 2.0] = 5.0;

            y.Values[1] = 3.0;

            Assert.AreEqual(2, y.Values[1]);
            Assert.AreEqual(3, y.Values[2]);

            Assert.AreEqual(2, f.Values[0, 1]);
            Assert.AreEqual(5, f.Values[1, 1]);
            Assert.AreEqual(1, f.Values[0, 2]);
            Assert.AreEqual(4, f.Values[1, 2]);
        }

        [Test]
        public void Clear()
        {
            IVariable<double> x = new Variable<double>("x");
            IVariable<double> y = new Variable<double>("y");

            IFunction f = new Function();
            f.Arguments.Add(x);
            f.Components.Add(y);

            f.SetValues(new[] {100.0, 200.0, 300.0}, new VariableValueFilter<double>(x, new[] {1.0, 2.0, 3.0}));

            IList values = f.GetValues();
            Assert.AreEqual(3, values.Count);

            f.Clear();
            Assert.AreEqual(0, values.Count);
        }

        [Test]
        public void Clone()
        {
            IVariable<double> x = new Variable<double>("x", new Unit("dist","m"));
            IVariable<double> y = new Variable<double>("y");

            IFunction f = new Function { Arguments = {x}, Components = {y} };

            f[10.0] = 100.0;
            f[20.0] = 200.0;
            f[30.0] = 300.0;

            var clone = (IFunction) f.Clone();

            Assert.AreEqual(f.Name, clone.Name);
            Assert.AreEqual(f.Arguments.Count, clone.Arguments.Count);
            Assert.AreEqual(f.Components.Count, clone.Components.Count);

            var clonedValues = clone.GetValues();
            var expectedValues = f.GetValues();
            Assert.AreEqual(expectedValues, clonedValues, "values must be cloned");

            Assert.AreNotSame(f.Arguments[0].Unit, clone.Arguments[0].Unit);
            Assert.AreEqual(f.Arguments[0].Unit.Name, clone.Arguments[0].Unit.Name);
        }

        [Test]
        public void CloneShouldRetainAutoSortBehaviour()
        {
            IVariable<double> x = new Variable<double>("x");
            IVariable<double> y = new Variable<double>("y");

            IFunction f = new Function { Arguments = { x }, Components = { y } };

            f[10.0] = 100.0;
            f[20.0] = 200.0;
            f[30.0] = 300.0;

            var clone = (IFunction)f.Clone();

            Assert.AreEqual(f.Name, clone.Name);
            Assert.AreEqual(f.Arguments.Count, clone.Arguments.Count);
            Assert.AreEqual(f.Components.Count, clone.Components.Count);

            var clonedValues = clone.GetValues();
            var expectedValues = f.GetValues();
            Assert.AreEqual(expectedValues, clonedValues, "values must be cloned");
            f[5.0] = 50.0;
            clone[5.0] = 50.0;

            Assert.AreEqual(f.GetValues(), clone.GetValues());
        }

        [Test]
        public void CloneShouldRetainDefaultValueBehaviour()
        {
            IVariable<double> x = new Variable<double>("x");
            IVariable<double> y = new Variable<double>("y") {DefaultValue = 1.23};

            IFunction f = new Function { Arguments = { x }, Components = { y } };
            var clone = (IFunction) f.Clone();

            Assert.AreEqual(1.23, (double)f.Components[0].DefaultValue, 1.0e-6);
            Assert.AreEqual(1.23, (double)f.Components[0].Values.DefaultValue, 1.0e-6);

            Assert.AreEqual(1.23, (double)clone.Components[0].DefaultValue, 1.0e-6);
            Assert.AreEqual(1.23, (double)clone.Components[0].Values.DefaultValue, 1.0e-6);

            f.Arguments[0].AddValues(new[] {100.0});
            Assert.AreEqual(1.23, (double)f[100.0], 1.0e-6);

            clone.Arguments[0].AddValues(new[] {100.0});
            // next line fails if clone.Components[0].Values.DefaultValue is incorrect
            Assert.AreEqual(1.23, (double)clone[100.0], 1.0e-6);
        }

        [Test]
        public void CloneFunctionNotCallGetSetValues()
        {
            IVariable<double> x = new Variable<double>("x");
            IVariable<double> y = new Variable<double>("y");

            IFunction f = new Function { Arguments = { x }, Components = { y } };

            var fileBasedStore = mocks.Stub<IMockFileBasedFunctionStore>();
            fileBasedStore.Functions = new EventedList<IFunction> { f, x, y };

            using (mocks.Record())
            {
                fileBasedStore.Expect(s => s.Clone()).Return(fileBasedStore); // assert, clone should be called

                fileBasedStore.FunctionValuesChanged += null;
                LastCall.Constraints(Is.NotNull()).Repeat.Any();

                fileBasedStore.FunctionValuesChanging += null;
                LastCall.Constraints(Is.NotNull()).Repeat.Any();

                //fileBasedStore.BeforeFunctionValuesChanged += null;
                //LastCall.Constraints(Is.NotNull()).Repeat.Any();
            }

            using (mocks.Playback())
            {
                f.Store = fileBasedStore;
                f.Clone();
            }
        }

        [Test]
        public void CloneFuntionWithTwoVariablesWhereTheClonedVariablesShouldHaveSameStore()
        {
            IVariable<double> x = new Variable<double>("x");
            IVariable<double> x2 = new Variable<double>("x2");
            IVariable<double> y = new Variable<double>("y");

            IFunction f = new Function
                              {
                                  Arguments = { x,x2 },
                                  Components = { y }
                              };

            var clone = (IFunction)f.Clone();

            Assert.AreEqual(4, f.Store.Functions.Count);
            Assert.AreEqual(4, clone.Store.Functions.Count);
            Assert.AreEqual(4, clone.Arguments[0].Store.Functions.Count);

            Assert.AreNotSame(f.Store,clone.Store);
            Assert.AreSame(clone.Arguments[0].Store, clone.Components[0].Store);
            Assert.AreSame(clone.Arguments[0].Store, clone.Arguments[1].Store);

        }

        [Test]
        public void CloneWith2Arguments()
        {
            var x = new Variable<int>("x");
            var y = new Variable<int>("y");
            var component = new Variable<int>("f");
            var f = new Function
                        {
                            Arguments = {x, y},
                            Components = {component}
                        };

            f[1, 10] = 100;
            f[2, 20] = 200;

            var clone = (IFunction) f.Clone();

            Assert.AreEqual(f.Name, clone.Name);
            Assert.AreEqual(f.Arguments.Count, clone.Arguments.Count);
            Assert.AreEqual(f.Components.Count, clone.Components.Count);

            var clonedX = (IVariable<int>) clone.Arguments[0];
            var clonedY = (IVariable<int>) clone.Arguments[1];

            Assert.AreEqual(x.Values.Count, clonedX.Values.Count);
            Assert.AreEqual(y.Values.Count, clonedY.Values.Count);
            Assert.AreEqual(component.Values.Count, component.Values.Count);
        }

        [Test]
        public void ClonedArgumentShouldNotChangeTheValueOfOriginalArgument()
        {
            var x = new Variable<int>("x");
            var y = new Variable<int>("y");
            var component = new Variable<int>("f");
            var f = new Function
                        {
                            Arguments = { x, y },
                            Components = { component }
                        };

            f[1, 10] = 100;
            f[2, 20] = 200;

            var clone = (IFunction)f.Clone();

            f[1, 10] = 199;
            f[2, 20] = 299;

            Assert.AreNotEqual(199, clone[1, 10]);
            Assert.AreNotEqual(299, clone[2, 20]);
        }

        [Test]
        public void CloneWithTargetStore()
        {
            var x = new Variable<int>("x");
            var y = new Variable<int>("y");

            var f = new Variable<int>("f");
            
            var function = new Function
                               {
                                   Arguments = { x, y },
                                   Components = { f }
                               };

            f[1, 10] = 100;
            f[2, 20] = 200;

            var targetStore = new MemoryFunctionStore();
            targetStore = (MemoryFunctionStore) f.Store;

            var clonedFunction = (IFunction)function.Clone();//(targetStore);

            // assserts
            //clonedFunction.Store
            //    .Should().Be.EqualTo(targetStore);

            targetStore.Functions.Count
                .Should("function in the new store should be: function, f, x, y").Be.EqualTo(4);

            clonedFunction.Components.Count
                .Should("1 component: f").Be.EqualTo(1);
            clonedFunction.Arguments.Count
                .Should("2 arguments: x, y").Be.EqualTo(2);
            
            var clonedX = (IVariable<int>)clonedFunction.Arguments[0];
            var clonedY = (IVariable<int>)clonedFunction.Arguments[1];
            var clonedF = (IVariable<int>)clonedFunction.Components[0];

            clonedX.Values
                .Should("values of cloned x").Have.SameSequenceAs(new[] { 1, 2 });
            
            clonedY.Values
                .Should("values of cloned y").Have.SameSequenceAs(new[] { 10, 20 });
            
            clonedF.Values
                .Should("values of cloned f component").Have.SameSequenceAs(new[] { 100, 0, 0, 200 });
        }

        [Test]
        public void CloneTwoComponentsFunctionAndAddValues()
        {
            IVariable x = new Variable<double>("x");
            IVariable l = new Variable<double>("left");
            IVariable r = new Variable<double>("right");

            IFunction f = new Function();
            f.Arguments.Add(x);
            f.Components.Add(l);
            f.Components.Add(r);

            f[0.0] = new[] {10.0,3.0 };
            f[1.0] = new[] { 20.0, 6.0 };

            var clone = (IFunction)f.Clone();

            clone[2.0] = new[] { 30.0, 9.0 };

            Assert.AreEqual(3,clone.Arguments[0].Values.Count);
        }

        [Test]
        public void ComponentsAndArguments()
        {
            IVariable x = new Variable<double>("x");
            IVariable y = new Variable<double>("y");

            var f = new Function();
            f.Arguments.Add(x);
            f.Components.Add(y);

            // these asserts are a bit tricky, but thats it, check math function definition :)
            Assert.AreEqual(1, f.Components[0].Arguments.Count,
                            "1st component of the function f must depend on 1 variable");
            Assert.AreEqual(x, f.Components[0].Arguments[0], "y component of f depends on x variable");

            Assert.AreEqual(1, f.Components[0].Components.Count,
                            "1st component of the f must have 1 component, which is y itself");
            Assert.AreEqual(y, f.Components[0].Components[0]);
        }

        [Test]
        public void DefaultFunctionStore()
        {
            var f = new Function();
            Assert.IsTrue(f.Store is MemoryFunctionStore);
        }

        [Test]
        public void DependendVariableOnExistingArgument()
        {
            IVariable<float> y = new Variable<float>("f");
            IVariable<float> x = new Variable<float>("x1");

            x.Values.Add(1.0f);

            // y should expand because of x
            y.Arguments.Add(x);

            Assert.AreEqual(1, y.Values.Count);
        }

        [Test]
        public void FunctionNotifyPropertyChanged()
        {
            Function function = new Function();
            IVariable<int> y = new Variable<int>("component");
            IVariable<int> x = new Variable<int>("argument");
            function.Arguments.Add(x);
            function.Components.Add(y);
            
            var variableChangedCount = 0;
            ((INotifyPropertyChanged) x).PropertyChanged += delegate
                                                                {
                                                                    variableChangedCount++;
                                                                };

            var variablesPropChangedCount = 0;
            ((INotifyPropertyChanged)function.Arguments).PropertyChanged += delegate { variablesPropChangedCount++; };
            
            var functionPropChangedCount = 0;
/*
            (function).PropertyChanged += delegate(object sender, PropertyChangedEventArgs e)
                                            {
                                                Debug.WriteLine("changing " + e.PropertyName +" to " + x.InterpolationType );
                                                functionPropChangedCount++;
                                            };
*/

            var functionPropChangedCount2 = 0;
            ((INotifyPropertyChanged)function).PropertyChanged += delegate {
                functionPropChangedCount2++;
            };

            x.InterpolationType = InterpolationType.None;

            Assert.AreEqual(1, variableChangedCount);
            Assert.AreEqual(1,variablesPropChangedCount);
//            Assert.AreEqual(1,functionPropChangedCount);
            Assert.AreEqual(1, functionPropChangedCount2);
        }

        [Test]
        public void FunctionValuesChanged()
        {
            IFunction function = new Function();
            IVariable<string> y = new Variable<string>("component");
            IVariable<int> x = new Variable<int>("argument");
            function.Arguments.Add(x);
            function.Components.Add(y);

            function[0] = "first";

            int callCount = 0;
            function.ValuesChanged += delegate(object sender, FunctionValuesChangingEventArgs e)
                                          {
                                              Debug.WriteLine(e.Function.Name + " action " + e.Action);
                                              callCount++;
                                          };
            function[1] = "second";

            //we get 3 calls. 
            // (1) add of value to component
            // (2) add of value to argument
            // (3) replace value of component
            Assert.AreEqual(3, callCount);

            //change the value of an argument
            function.Arguments[0].Values[1] = 2;
            Assert.AreEqual("second", function.Components[0][2]);
            Assert.AreEqual(4, callCount);
        }

        [Test]
        public void FunctionValuesChangedForDependendVariable()
        {
            IVariable<string> dependend = new Variable<string>("dependend");
            IVariable<int> argument = new Variable<int>("argument");
            dependend.Arguments.Add(argument);

            dependend[2] = "string";

            //we add a value and except 2 valueschanged events
            int callCount = 0;
            dependend.ValuesChanged += delegate(object sender, FunctionValuesChangingEventArgs e)
                                           {
                                               Debug.WriteLine(e.Action + " " + e.Function.Name);
                                               Assert.IsNotNull(e.Function);
                                               callCount++;
                                           };

            dependend[3] = "another string";

            // Add dependend
            // Add argument
            // Replace dependend
            Assert.AreEqual(3, callCount);
        }

        [Test]
        public void FunctionValuesChangedEventsForIndependendVariableShouldNotIncludeOtherEvents()
        {
            var f = new Function
                        {
                            Arguments = {new Variable<int>("x")},
                            Components = {new Variable<int>("y")}
                        };

            var eventsSentToFunction = 0;
            var eventsSentToArgument = 0;
            var eventsSentToComponent = 0;

            f.ValuesChanged += delegate { eventsSentToFunction++; };
            f.Arguments[0].ValuesChanged += delegate { eventsSentToArgument++; };
            f.Components[0].ValuesChanged += delegate { eventsSentToComponent++; };

            f.Arguments[0].Values.Add(1);

            eventsSentToFunction
                .Should().Be.EqualTo(2);
            eventsSentToComponent
                .Should().Be.EqualTo(2);
            eventsSentToArgument
                .Should().Be.EqualTo(1);
        }

        [Test]
        public void FunctionValuesChangeWhenComponentChanged()
        {
            IFunction function = new Function();
            IVariable<string> component = new Variable<string>();
            IVariable<int> argument = new Variable<int>();
            function.Arguments.Add(argument);
            function.Components.Add(component);
            function[0] = "first";

            int callCount = 0;
            function.ValuesChanged += delegate(object sender, FunctionValuesChangingEventArgs e)
                                          {
                                              Assert.AreEqual(function.Components[0], sender);
                                              Assert.AreEqual(function.Components[0], e.Function);
                                              callCount++;
                                          };
            //when a component changes its value the function should know about it
            function.Components[0].Values[0] = "second";
            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void GetAllItemsRecursive()
        {
            var function = new Function();
            Assert.AreEqual(new object[] { function, function.Store }, function.GetAllItemsRecursive().ToArray());
        }

        [Test]
        public void GetValues()
        {
            IVariable x = new Variable<double>("x");
            IVariable y = new Variable<double>("y");
            IVariable fx = new Variable<double>("fx");
            IVariable fy = new Variable<double>("fy");

            IFunction f = new Function();
            f.Arguments.Add(x);
            f.Arguments.Add(y);
            f.Components.Add(fx);
            f.Components.Add(fy);

            Assert.AreEqual(2,fy.Values.Rank);

            // set values of the x, y arguments, fx, fy components will be set to their default values.
            x.SetValues(new[] {1.0, 2.0});
            y.SetValues(new[] {10.0, 20.0});

            // get values, all values equal DefaultValue
            IMultiDimensionalArray values = fx.GetValues();
            Assert.AreEqual(4, values.Count, "number of values after argument values are set");
            Assert.AreEqual(fx.DefaultValue, values[0, 0], "default value");

            // set value using indexes.
            int i = 1;
            int j = 1;

            fx.Values[i, j] = 111.0;
            fy.Values[i, j] = 222.0;

            // ... and it is also the same as (accessing component variables directly):
            f.Components[0].Values[i, j] = 111.0;
            f.Components[1].Values[i, j] = 222.0;

            // ... you can also assign *all* values in the following way
            var array2d = new[,] {{fx.DefaultValue, fx.DefaultValue}, {111.0, 111.0}};
            fx.SetValues(array2d);

            array2d = new[,] {{fy.DefaultValue, fy.DefaultValue}, {222.0, 222.0}};
            fy.SetValues(array2d);

            fx[2.0, 10.0] = 111.0;
            fy[2.0, 20.0] = 222.0;

            // we can also get a single value in this way
            Assert.AreEqual(111.0, (double) fx[2.0, 10.0]);
            Assert.AreEqual(222.0, (double) fy[2.0, 20.0]);

            // a single value can not be asked using indexes of the arguments. 
            // now you have to find the values via the arguments
            var value = (double) fx.Values[i, j];
            Assert.AreEqual(111.0, value);
        }

        [Test]
        [Ignore("this way of setting values is too complex, unclear and unituitive")]
        public void GetValuesGeneric()
        {
            IVariable<double> x = new Variable<double>("x");
            IVariable<double> y = new Variable<double>("y");
            IVariable<double> fx = new Variable<double>("fx");
            IVariable<double> fy = new Variable<double>("fy");

            IFunction f = new Function();
            f.Arguments.Add(x);
            f.Arguments.Add(y);
            f.Components.Add(fx);
            f.Components.Add(fy);

            // set (fx, fy) values to (100.0, 200.0) for a combination of x and y values.
            f.SetValues(
                new[] {100.0, 200.0},
                new VariableValueFilter<double>(x, new[] {1.0, 2.0}),
                new VariableValueFilter<double>(y, new[] { 10.0, 20.0 }));

            IMultiDimensionalArray<double> values = fx.GetValues();

            int expectedValuesCount = 4;
            Assert.AreEqual(expectedValuesCount, values.Count);
            Assert.AreEqual(expectedValuesCount, x.Values.Count*y.Values.Count);

            double expectedFxValue = 100.0;
            double expectedFyValue = 200.0;
            Assert.AreEqual(expectedFxValue, fx.Values[0, 0]);
            Assert.AreEqual(expectedFyValue, fy.Values[0, 0]);
        }

        [Test]
        public void Implicit1DAccess()
        {
            //m = F(x,t)
            IVariable<int> measurement = new Variable<int>();
            IVariable<int> time = new Variable<int>();
            IVariable<int> space = new Variable<int>();
            time.SetValues(new[] {1, 2, 3});
            space.SetValues(new[] {10, 20, 30});

            measurement.Arguments.Add(time);
            measurement.Arguments.Add(space);
            Assert.AreEqual(9, measurement.GetValues().Count);
            measurement[2, 10] = 200;
            //select one location and expect a singe dimensional array
            IMultiDimensionalArray<int> values = measurement.GetValues(new VariableValueFilter<int>(space, 10));
            Assert.AreEqual(3, values.Count);
            //do you really want this?
            Assert.AreEqual(200, values[1]);
        }

        /// <summary>
        /// Constructs simple function containing one string component and no arguments.
        /// In other words it can be presented as a list, for example to store substance names:
        /// 
        /// <code>
        /// <![CDATA[
        /// 
        /// Declaration:
        ///     substance = substance name()
        /// 
        /// Example:
        ///     substance = { "nitrogen", "oxygen", "halogens" }
        /// 
        /// Definition:
        ///     substance/ --------------- Function
        ///         Components/
        ///             substance name --- Variable<string>, the only one function component
        ///         Arguments/
        ///             <empty>
        /// ]]>
        /// </code>
        /// 
        /// </summary>
        [Test]
        public void OneComponentAndNoArguments_Substance()
        {
            //TODO: why use a function here ????
            //throw new NotSupportedException("Don't use this. Variable is sorted unless it is component");
            IVariable substance = new Variable<string>("A list of substances") {IsAutoSorted = false};

            substance.SetValues(new[] {"nitrogen", "oxygen", "halogens"});

            Assert.AreEqual(3, substance.Values.Count);
            Assert.AreEqual("nitrogen", substance.Values[0]);
            Assert.AreEqual("oxygen", substance.Values[1]);
            Assert.AreEqual("halogens", substance.Values[2]);
        }

        /// <summary>
        /// Tests creation of a function to define rating curve.
        /// <code>
        /// <![CDATA[
        /// Declaration:
        ///     Q = Q(H)
        /// 
        ///     Q - component: Name = water flow, ShortName = Q, Quantity = flow, Unit = m^3/s, ValueType = float
        ///     H - argument: Name = water depth, ShortName = H, Quantity = depth, Unit = m, ValueType = float
        /// 
        /// Example:
        ///     Q = {(H, Q) - pairs}:
        /// 
        ///         H   | Q
        ///         --------
        ///         0.0 | 0.0
        ///         1.0 | 10.0
        ///         5.0 | 100.0
        /// 
        /// Definition:
        ///     rating curve/ -------- Function
        ///         Components/
        ///             water flow --- Variable<float>
        ///         Arguments/
        ///             water depth -- Variable<float>
        /// 
        /// ]]>
        /// </code>
        /// </summary>
        [Test]
        public void OneComponentOneArgument_RatingCurve()
        {
            // create rating curve function arguments and components
            var q = new Variable<double>("flow");
            var h = new Variable<double>("depth");

            // create rating curve function
            IFunction ratingCurve = new Function("rating curve");
            ratingCurve.Components.Add(q);
            ratingCurve.Arguments.Add(h);

            // add values
            ratingCurve[0.0] = 0.0;
            ratingCurve[1.0] = 10.0;
            ratingCurve[5.0] = 100.0;

            // asserts
            Assert.AreEqual(h, ratingCurve.Arguments[0]);
            Assert.AreEqual(q, ratingCurve.Components[0]);

            //what do we want to check?
            Assert.AreEqual(0.0, ratingCurve.Arguments[0].Values[0]);
            Assert.AreEqual(1.0, ratingCurve.Arguments[0].Values[1]);
            Assert.AreEqual(5.0, ratingCurve.Arguments[0].Values[2]);

            Assert.AreEqual(0.0, q.Values[0]);
            Assert.AreEqual(10.0, q.Values[1]);
            Assert.AreEqual(100.0, q.Values[2]);
        }

        [Test]
        public void ReAddComponent()
        {
            IFunction function = new Function();
            function.Arguments.Add(new Variable<double>("arg"));
            function.Components.Add(new Variable<double>("first"));
            Assert.AreEqual(3,function.Store.Functions.Count);
            Assert.AreEqual(function.Store, function.Components[0].Store);
            function.Components.Clear();
            function.Components.Add(new Variable<double>("second"));

            Assert.AreEqual(3, function.Store.Functions.Count);
            Assert.AreEqual(function.Store, function.Components[0].Store);
        }

        [Test]
        public void RebuildFunctionAfterClear()
        {
            IFunction function = new Function();
            function.Arguments.Add(new Variable<double>("flow"));
            function.Components.Add(new Variable<double>("depth"));

            function.Arguments.Clear();
            function.Components.Clear();

            //Assert.AreEqual(1,function.Store.Functions.Count);
            function.Arguments.Add(new Variable<double>("flow"));
            Assert.AreEqual(function.Arguments[0].Store, function.Store);

            function.Components.Add(new Variable<double>("depth"));
            function.Arguments.Add(new Variable<double>("flow"));
        }

        [Test]
        public void RemoveComponent()
        {
            IFunction f = new Function();
            f.Components.Add(new Variable<double>());
            f.Components.Add(new Variable<double>());
            Assert.AreEqual(2, f.Components.Count);
            Assert.AreEqual(3, f.Store.Functions.Count);

            f.Components.Clear();
            Assert.AreEqual(0, f.Components.Count);
            Assert.AreEqual(1, f.Store.Functions.Count);
        }

        [Test]
        public void ScaleArguments()
        {
            var f = new Function();
            IVariable<double> comp = new Variable<double>();
            IVariable<double> x = new Variable<double>();
            f.Components.Add(comp);
            f.Arguments.Add(x);

            x.SetValues(new[] {1.0, 2.0, 3.0});
            Assert.IsTrue(new[] {0.0, 0.0, 0.0}.SequenceEqual(comp.Values));
        }
        
        [Test]
        [Category(TestCategory.Performance)]
        [Category(TestCategory.WorkInProgress)] // slow
        public void Set10kFunctionValues2D()
        {
            var f = new Function();
            IVariable<double> component = new Variable<double>();
            IVariable<double> x = new Variable<double>();
            IVariable<double> y = new Variable<double>();
            f.Components.Add(component);
            f.Arguments.Add(x);
            f.Arguments.Add(y);

            const int valuesToAddCount = 500;
            var doubles = new List<double>();
            for (var i = 0; i < valuesToAddCount; i++)
            {
                doubles.Add(i);
            }

            Action action = delegate
                                {
                                    x.SetValues(doubles);
                                    y.SetValues(doubles); // sets 10000 values for function 
                                };

            TestHelper.AssertIsFasterThan(150, action);
        }

        /// <summary>
        /// Note: this works ~70% faster compare to the above
        /// </summary>
        [Test]
        [Category(TestCategory.Performance)]
        [Category(TestCategory.WorkInProgress)] // slow
        public void Set10kFunctionValues2D_SetFirstDimensionAsLast()
        {
            var f = new Function();
            IVariable<double> component = new Variable<double>();
            IVariable<double> x = new Variable<double>();
            IVariable<double> y = new Variable<double>();
            f.Components.Add(component);
            f.Arguments.Add(x);
            f.Arguments.Add(y);

            const int valuesToAddCount = 500;
            var doubles = new List<double>();
            for (var i = 0; i < valuesToAddCount; i++)
            {
                doubles.Add(i);
            }

            Action action = delegate
                                {
                                    y.SetValues(doubles);
                                    x.SetValues(doubles); // sets 10000 values for function 
                                };

            
            TestHelper.AssertIsFasterThan(75, action);
        }

        [Test]
        [Category(TestCategory.Performance)]
        [Category(TestCategory.WorkInProgress)] // slow
        public void CloneShouldBeFast()
        {
            var f = new Function { Name = "f" };
            IVariable<double> component = new Variable<double> { Name = "component" };
            IVariable<double> x = new Variable<double> { Name = "x" };
            IVariable<double> y = new Variable<double> { Name = "y"};
            f.Components.Add(component);
            f.Arguments.Add(x);
            f.Arguments.Add(y);

            x.SetValues(new[] { 0.1, 0.2, 1, 2, 3, 4, 5, 6, 7 });
            y.SetValues(new[] { 0.1, 0.2, 1, 2, 3, 4, 5, 6, 7 });
            component.SetValues(new[] { 1.0 });

            Action action = delegate
                                {
                                    for (int i = 0; i < 1000; i++)
                                    {
                                        var f2 = f.Clone();
                                    }
                                };

            TestHelper.AssertIsFasterThan(750, action);
        }

        [Test]
        [Category(TestCategory.Performance)]
        [Category(TestCategory.BadQuality)] // TODO: split this test in two tests measuring actual time and use TestHelper.AssertIsFasterThan()
        public void IntuitiveFunctionApiShouldBeAsFastAsSetValuesCall()
        {
            //make sure a significant number calls is made. Otherwise startup effects are dominant
            const int ammount = 10000;

            var f1 = FunctionHelper.Get1DFunction<int, int>();
            var runTimeUsingSetValues = TestHelper.GetRunTime(() =>
            {
                for (int i = 0; i < ammount; i++)
                {
                    f1.Components[0].SetValues(new[] { i }, new VariableValueFilter<int>(f1.Arguments[0], i));
                }
            });

            var f2 = FunctionHelper.Get1DFunction<int, int>();
            var runTimeUsingIndexer = TestHelper.GetRunTime(() =>
            {
                for (int i = 0; i < ammount; i++)
                {
                    f2[i] = i;
                }
            });

            Debug.WriteLine(string.Format("Using indexer took {0}",runTimeUsingIndexer));
            Debug.WriteLine(string.Format("Using setvalues took {0}",runTimeUsingSetValues));
            //check they are about the same speed...
            var ratio = runTimeUsingIndexer / runTimeUsingSetValues;
            ratio.Should("Ratio should be approximately 1").Be.LessThan(1.3).And.Be.GreaterThan(0.7);
        }

        [Test]
        [Category(TestCategory.Performance)]
        [Category(TestCategory.WorkInProgress)] // slow
        public void SettingValuesUsingSetValuesShouldBeFastForSingleComponentFunctions()
        {
            var f = FunctionHelper.Get1DFunction<int, int>();
            TestHelper.AssertIsFasterThan(100, () =>
            {
                for (int i = 0; i < 1000; i++)
                {
                    f.Components[0].SetValues(new[]{i},new VariableValueFilter<int>(f.Arguments[0],i));
                }
            });

        }


        [Test]
        public void SetFunctionValuesUsingComponentFilter()
        {
            //Setup a function with 2 components and 1 argument
            var f = new Function();
            IVariable vX = new Variable<int>("vX");
            IVariable vY = new Variable<int>("vY");

            f.Arguments.Add(new Variable<int>("x"));
            f.Components.Add(vX);
            f.Components.Add(vY);

            int[] xValues = {1, 2, 3, 4};
            int[] vXValues = {0, -20, -20, 0};
            f.Arguments[0].SetValues(xValues);
            f.SetValues(vXValues, new ComponentFilter(vX));

            Assert.AreEqual(4, f.Components[0].Values.Count);
            //TODO: make this syntax fly
            //Assert.IsTrue(new[]{0,-20,-20,0}.SequenceEqual<int>(f.Components[0].Values));
            Assert.AreEqual(0, vX.Values[0]);
            Assert.AreEqual(-20, vX.Values[1]);
            Assert.AreEqual(-20, vX.Values[2]);
            Assert.AreEqual(0, vX.Values[3]);
        }

        [Test]
        public void SetMultiValues()
        {
            object defaultValue = -1.0f;
            var x = new Variable<float>("x");
            var y = new Variable<float>("y");
            var z = new Variable<float>("z");
            var t = new Variable<int>("t");

            var vx = new Variable<float>("vx");
            var vy = new Variable<float>("vy");
            var vz = new Variable<float>("vz");

            vx.DefaultValue = defaultValue;
            vy.DefaultValue = defaultValue;
            vz.DefaultValue = defaultValue;

            var function = new Function("3D Test");
            function.Arguments.Add(x);
            function.Arguments.Add(y);
            function.Arguments.Add(z);
            function.Arguments.Add(t);
            function.Components.Add(vx);
            function.Components.Add(vy);
            function.Components.Add(vz);

            IList values;

            // (vx, vy, vz)(0.0, 0.0, 0.0, 0) = (0.0, 0.0, 0.0)
            function[0.0f, 0.0f, 0.0f, 0] = new[] {0.0f, 0.0f, 0.0f};
            //   0.0    0.0    0.0      0      0.0     0.0     0.0

            function.SetValues(
                new[] {3.0f, 4.0f, 5.0f},
                new VariableValueFilter<float>(x, 1.0F), new VariableValueFilter<float>(y, 2.0F));
            //   0.0    0.0    0.0      0      0.0     0.0     0.0
            //   0.0    2.0    0.0      0    default default default
            //   1.0    0.0    0.0      0    default default default
            //   1.0    2.0    0.0      0      3.0     4.0     5.0
            Assert.AreEqual(0.0F, vx.Values[0, 0, 0, 0]);
            Assert.AreEqual(0.0F, vy.Values[0, 0, 0, 0]);
            Assert.AreEqual(0.0F, vz.Values[0, 0, 0, 0]);
            Assert.AreEqual(defaultValue, vx.Values[0, 1, 0, 0]);
            Assert.AreEqual(defaultValue, vy.Values[0, 1, 0, 0]);
            Assert.AreEqual(defaultValue, vz.Values[0, 1, 0, 0]);
            Assert.AreEqual(defaultValue, vx.Values[1, 0, 0, 0]);
            Assert.AreEqual(defaultValue, vx.Values[1, 0, 0, 0]);
            Assert.AreEqual(defaultValue, vx.Values[1, 0, 0, 0]);
            Assert.AreEqual(3.0F, vx.Values[1, 1, 0, 0]);
            Assert.AreEqual(4.0F, vy.Values[1, 1, 0, 0]);
            Assert.AreEqual(5.0F, vz.Values[1, 1, 0, 0]);

            function.SetValues(new[] {7.0f, 8.0f, 9.0f}, new VariableValueFilter<int>(t, 1));
            //   0.0    0.0    0.0      0      0.0     0.0     0.0
            //   0.0    0.0    0.0      1      7.0     8.0     9.0
            //   0.0    2.0    0.0      0     default default default
            //   0.0    2.0    0.0      1      7.0     8.0     9.0
            //   1.0    0.0    0.0      0     default default default
            //   1.0    0.0    0.0      1      7.0     8.0     9.0
            //   1.0    2.0    0.0      0      3.0     4.0     5.0
            //   1.0    2.0    0.0      1      7.0     8.0     9.0
            Assert.AreEqual(0.0F, vx.Values[0]);
            Assert.AreEqual(0.0F, vy.Values[0]);
            Assert.AreEqual(0.0F, vz.Values[0]);
            Assert.AreEqual(7.0F, vx.Values[1]);
            Assert.AreEqual(8.0F, vy.Values[1]);
            Assert.AreEqual(9.0F, vz.Values[1]);
            Assert.AreEqual(defaultValue, vx.Values[2]);
            Assert.AreEqual(defaultValue, vy.Values[2]);
            Assert.AreEqual(defaultValue, vz.Values[2]);
            Assert.AreEqual(7.0F, vx.Values[3]);
            Assert.AreEqual(8.0F, vy.Values[3]);
            Assert.AreEqual(9.0F, vz.Values[3]);
            Assert.AreEqual(defaultValue, vx.Values[4]);
            Assert.AreEqual(defaultValue, vy.Values[4]);
            Assert.AreEqual(defaultValue, vz.Values[4]);
            Assert.AreEqual(7.0F, vx.Values[5]);
            Assert.AreEqual(8.0F, vy.Values[5]);
            Assert.AreEqual(9.0F, vz.Values[5]);
            Assert.AreEqual(3.0F, vx.Values[6]);
            Assert.AreEqual(4.0F, vy.Values[6]);
            Assert.AreEqual(5.0F, vz.Values[6]);
            Assert.AreEqual(7.0F, vx.Values[7]);
            Assert.AreEqual(8.0F, vy.Values[7]);
            Assert.AreEqual(9.0F, vz.Values[7]);

            function.SetValues(
                new[] {10.0f, 11.0f, 12.0f, 13.0f, 14.0f, 15.0f, 16.0f, 17.0f, 18.0f, 19.0f, 20.0f, 21.0f},
                new VariableValueFilter<int>(t, 1));
            //   0.0    0.0    0.0      0      0.0     0.0     0.0
            //   0.0    0.0    0.0      1     10.0    11.0    12.0
            //   0.0    2.0    0.0      0     default default default
            //   0.0    2.0    0.0      1     13.0    14.0    15.0
            //   1.0    0.0    0.0      0     default default default
            //   1.0    0.0    0.0      1     16.0    17.0    18.0 
            //   1.0    2.0    0.0      0      3.0     4.0     5.0
            //   1.0    2.0    0.0      1     19.0    20.0    21.0
            Assert.AreEqual(0.0F, vx.Values[0]);
            Assert.AreEqual(0.0F, vy.Values[0]);
            Assert.AreEqual(0.0F, vz.Values[0]);
            Assert.AreEqual(10.0F, vx.Values[1]);
            Assert.AreEqual(11.0F, vy.Values[1]);
            Assert.AreEqual(12.0F, vz.Values[1]);
            Assert.AreEqual(defaultValue, vx.Values[2]);
            Assert.AreEqual(defaultValue, vy.Values[2]);
            Assert.AreEqual(defaultValue, vz.Values[2]);
            Assert.AreEqual(13.0F, vx.Values[3]);
            Assert.AreEqual(14.0F, vy.Values[3]);
            Assert.AreEqual(15.0F, vz.Values[3]);
            Assert.AreEqual(defaultValue, vx.Values[4]);
            Assert.AreEqual(defaultValue, vy.Values[4]);
            Assert.AreEqual(defaultValue, vz.Values[4]);
            Assert.AreEqual(16.0F, vx.Values[5]);
            Assert.AreEqual(17.0F, vy.Values[5]);
            Assert.AreEqual(18.0F, vz.Values[5]);
            Assert.AreEqual(3.0F, vx.Values[6]);
            Assert.AreEqual(4.0F, vy.Values[6]);
            Assert.AreEqual(5.0F, vz.Values[6]);
            Assert.AreEqual(19.0F, vx.Values[7]);
            Assert.AreEqual(20.0F, vy.Values[7]);
            Assert.AreEqual(21.0F, vz.Values[7]);

            function.SetValues(new[] {6.0f, 7.0f, 8.0f},
                               new VariableValueFilter<int>(t, 1), new VariableValueFilter<float>(x, 1.0f));
            //   0.0    0.0    0.0      0      0.0     0.0     0.0
            //   0.0    0.0    0.0      1     10.0    11.0    12.0
            //   0.0    2.0    0.0      0     default default default
            //   0.0    2.0    0.0      1     13.0    14.0    15.0
            //   1.0    0.0    0.0      0     default default default
            //   1.0    0.0    0.0      1      6.0     7.0     8.0
            //   1.0    2.0    0.0      0      3.0     4.0     5.0
            //   1.0    2.0    0.0      1      6.0     7.0     8.0

            Assert.AreEqual(0.0F, vx.Values[0]);
            Assert.AreEqual(0.0F, vy.Values[0]);
            Assert.AreEqual(0.0F, vz.Values[0]);
            Assert.AreEqual(10.0F, vx.Values[1]);
            Assert.AreEqual(11.0F, vy.Values[1]);
            Assert.AreEqual(12.0F, vz.Values[1]);
            Assert.AreEqual(defaultValue, vx.Values[2]);
            Assert.AreEqual(defaultValue, vy.Values[2]);
            Assert.AreEqual(defaultValue, vz.Values[2]);
            Assert.AreEqual(13.0F, vx.Values[3]);
            Assert.AreEqual(14.0F, vy.Values[3]);
            Assert.AreEqual(15.0F, vz.Values[3]);
            Assert.AreEqual(defaultValue, vx.Values[4]);
            Assert.AreEqual(defaultValue, vy.Values[4]);
            Assert.AreEqual(defaultValue, vz.Values[4]);
            Assert.AreEqual(6.0F, vx.Values[5]);
            Assert.AreEqual(7.0F, vy.Values[5]);
            Assert.AreEqual(8.0F, vz.Values[5]);
            Assert.AreEqual(3.0F, vx.Values[6]);
            Assert.AreEqual(4.0F, vy.Values[6]);
            Assert.AreEqual(5.0F, vz.Values[6]);
            Assert.AreEqual(6.0F, vx.Values[7]);
            Assert.AreEqual(7.0F, vy.Values[7]);
            Assert.AreEqual(8.0F, vz.Values[7]);

            function.SetValues(new[] {20.0f, 21.0f, 22.0f, 23.0f, 24.0f, 25.0f},
                               new VariableValueFilter<int>(t, 1), new VariableValueFilter<float>(x, 1.0f));
            //   0.0    0.0    0.0      0      0.0     0.0     0.0
            //   0.0    0.0    0.0      1     10.0    11.0    12.0
            //   0.0    2.0    0.0      0     default default default
            //   0.0    2.0    0.0      1     13.0    14.0    15.0
            //   1.0    0.0    0.0      0     default default default
            //   1.0    0.0    0.0      1     20.0    21.0    22.0
            //   1.0    2.0    0.0      0      3.0     4.0     5.0
            //   1.0    2.0    0.0      1     23.0    24.0    25.0
            Assert.AreEqual(0.0F, vx.Values[0]);
            Assert.AreEqual(0.0F, vy.Values[0]);
            Assert.AreEqual(0.0F, vz.Values[0]);
            Assert.AreEqual(10.0F, vx.Values[1]);
            Assert.AreEqual(11.0F, vy.Values[1]);
            Assert.AreEqual(12.0F, vz.Values[1]);
            Assert.AreEqual(defaultValue, vx.Values[2]);
            Assert.AreEqual(defaultValue, vy.Values[2]);
            Assert.AreEqual(defaultValue, vz.Values[2]);
            Assert.AreEqual(13.0F, vx.Values[3]);
            Assert.AreEqual(14.0F, vy.Values[3]);
            Assert.AreEqual(15.0F, vz.Values[3]);
            Assert.AreEqual(defaultValue, vx.Values[4]);
            Assert.AreEqual(defaultValue, vy.Values[4]);
            Assert.AreEqual(defaultValue, vz.Values[4]);
            Assert.AreEqual(20.0F, vx.Values[5]);
            Assert.AreEqual(21.0F, vy.Values[5]);
            Assert.AreEqual(22.0F, vz.Values[5]);
            Assert.AreEqual(3.0F, vx.Values[6]);
            Assert.AreEqual(4.0F, vy.Values[6]);
            Assert.AreEqual(5.0F, vz.Values[6]);
            Assert.AreEqual(23.0F, vx.Values[7]);
            Assert.AreEqual(24.0F, vy.Values[7]);
            Assert.AreEqual(25.0F, vz.Values[7]);
        }

        [Test]
        [NUnit.Framework.Category(TestCategory.Integration)]
        public void SetValues()
        {
            object defaultValue = default(float);
            var f = new Variable<float>("f");
            var x1 = new Variable<float>("x1");
            var x2 = new Variable<float>("x2");

            var function = new Function("OneComponentTwoArguments Test");
            function.Components.Add(f);
            function.Arguments.Add(x1);
            function.Arguments.Add(x2);

            function[0.0f, 0.0f] = 0.0f;

            //   x1    x2     f
            //  0.0   0.0   0.0

            Assert.AreEqual(1, x1.Values.Count);


            var x2values = new[] {0.1f, 0.2f};

            // set multiple values
            function.SetValues(new[] {5.0f},
                               new VariableValueFilter<float>(x1, 1.0f), new VariableValueFilter<float>(x2, x2values));

            //   x1    x2     f
            //  0.0   0.0   0.0
            //  1.0   0.0  default
            //  0.0   0.1  default
            //  1.0   0.1   5.0
            //  0.0   0.2  default
            //  1.0   0.2   5.0

            Assert.AreEqual(2, x1.Values.Count);
            Assert.AreEqual(3, x2.Values.Count);
            Assert.AreEqual(6, function.GetValues().Count);
            Assert.AreEqual(0.0F,
                            function.GetValues(new VariableValueFilter<float>(x1, 0.0F), new VariableValueFilter<float>(x2, 0.0F))[0]);
            Assert.AreEqual(defaultValue, function.GetValues(
                                              new VariableValueFilter<float>(x1, 1.0F),
                                              new VariableValueFilter<float>(x2, 0.0F))[0]
                );
            Assert.AreEqual(defaultValue, function.GetValues(
                                              new VariableValueFilter<float>(x1, 0.0F),
                                              new VariableValueFilter<float>(x2, 0.1F))[0]
                );
            Assert.AreEqual(5.0F, function.GetValues(
                                      new VariableValueFilter<float>(x1, 1.0F),
                                      new VariableValueFilter<float>(x2, 0.1F))[0]
                );
            Assert.AreEqual(defaultValue, function.GetValues(
                                              new VariableValueFilter<float>(x1, 0.0F),
                                              new VariableValueFilter<float>(x2, 0.2F))[0]
                );
            Assert.AreEqual(5.0F, function.GetValues(
                                      new VariableValueFilter<float>(x1, 1.0F),
                                      new VariableValueFilter<float>(x2, 0.2F))[0]
                );

            function.SetValues(new[] { 6.0f }, new VariableValueFilter<float>(x1, 2.0f));
            //   x1    x2     f
            //  0.0   0.0   0.0
            //  1.0   0.0  default
            //  0.0   0.1  default
            //  1.0   0.1   5.0
            //  0.0   0.2  default
            //  1.0   0.2   5.0
            //  2.0   0.0   6.0
            //  2.0   0.1   6.0
            //  2.0   0.2   6.0
            Assert.AreEqual(x1.Values.Count, 3);
            Assert.AreEqual(x2.Values.Count, 3);
            Assert.AreEqual(function.GetValues().Count, 9);
            Assert.AreEqual(0.0F,
                            function.GetValues(new VariableValueFilter<float>(x1, 0.0F),
                                               new VariableValueFilter<float>(x2, 0.0F))[0]);
            Assert.AreEqual(defaultValue,
                            function.GetValues(new VariableValueFilter<float>(x1, 1.0F), new VariableValueFilter<float>(x2, 0.0F))[0]);
            Assert.AreEqual(defaultValue,
                            function.GetValues(new VariableValueFilter<float>(x1, 0.0F), new VariableValueFilter<float>(x2, 0.1F))[0]);
            Assert.AreEqual(5.0F,
                            function.GetValues(new VariableValueFilter<float>(x1, 1.0F),
                                               new VariableValueFilter<float>(x2, 0.1F))[0]);
            Assert.AreEqual(defaultValue,
                            function.GetValues(new VariableValueFilter<float>(x1, 0.0F), new VariableValueFilter<float>(x2, 0.2F))[0]);
            Assert.AreEqual(5.0F,
                            function.GetValues(new VariableValueFilter<float>(x1, 1.0F),
                                               new VariableValueFilter<float>(x2, 0.2F))[0]);
            Assert.AreEqual(6.0F,
                            function.GetValues(new VariableValueFilter<float>(x1, 2.0F),
                                               new VariableValueFilter<float>(x2, 0.0F))[0]);
            Assert.AreEqual(6.0F,
                            function.GetValues(new VariableValueFilter<float>(x1, 2.0F),
                                               new VariableValueFilter<float>(x2, 0.1F))[0]);
            Assert.AreEqual(6.0F,
                            function.GetValues(new VariableValueFilter<float>(x1, 2.0F),
                                               new VariableValueFilter<float>(x2, 0.2F))[0]);

            function.SetValues(new[] {3.0f},
                               new VariableValueFilter<float>(x1, 0.0f));
            //   x1    x2     f
            //  0.0   0.0   3.0
            //  1.0   0.0  default
            //  0.0   0.1   3.0
            //  1.0   0.1   5.0
            //  0.0   0.2   3.0
            //  1.0   0.2   5.0
            //  2.0   0.0   6.0
            //  2.0   0.1   6.0
            //  2.0   0.2   6.0
            Assert.AreEqual(x1.Values.Count, 3);
            Assert.AreEqual(x2.Values.Count, 3);
            Assert.AreEqual(function.GetValues().Count, 9);
            Assert.AreEqual(3.0F,
                            function.GetValues(new VariableValueFilter<float>(x1, 0.0F),
                                               new VariableValueFilter<float>(x2, 0.0F))[0]);
            Assert.AreEqual(defaultValue,
                            function.GetValues(new VariableValueFilter<float>(x1, 1.0F), new VariableValueFilter<float>(x2, 0.0F))[0]);
            Assert.AreEqual(3.0F,
                            function.GetValues(new VariableValueFilter<float>(x1, 0.0F),
                                               new VariableValueFilter<float>(x2, 0.1F))[0]);
            Assert.AreEqual(5.0F,
                            function.GetValues(new VariableValueFilter<float>(x1, 1.0F),
                                               new VariableValueFilter<float>(x2, 0.1F))[0]);
            Assert.AreEqual(3.0F,
                            function.GetValues(new VariableValueFilter<float>(x1, 0.0F),
                                               new VariableValueFilter<float>(x2, 0.2F))[0]);
            Assert.AreEqual(5.0F,
                            function.GetValues(new VariableValueFilter<float>(x1, 1.0F),
                                               new VariableValueFilter<float>(x2, 0.2F))[0]);
            Assert.AreEqual(6.0F,
                            function.GetValues(new VariableValueFilter<float>(x1, 2.0F),
                                               new VariableValueFilter<float>(x2, 0.0F))[0]);
            Assert.AreEqual(6.0F,
                            function.GetValues(new VariableValueFilter<float>(x1, 2.0F),
                                               new VariableValueFilter<float>(x2, 0.1F))[0]);
            Assert.AreEqual(6.0F,
                            function.GetValues(new VariableValueFilter<float>(x1, 2.0F),
                                               new VariableValueFilter<float>(x2, 0.2F))[0]);
        }

        [Test]
        public void SetValuesNonExistingFilter()
        {
            IVariable<float> f = new Variable<float>("f");
            IVariable<float> x = new Variable<float>("x1");

            IFunction function = new Function("Fail Test");
            function.Components.Add(f);
            function.Arguments.Add(x);

            function.SetValues(new float[] {100},
                               new VariableValueFilter<float>(x, new[] {0.0f}));
        }

        [Test]
        public void SetValuesOfDiffentTypes()
        {
            var f = new Function();
            f.Components.Add(new Variable<double>());
            f.Components.Add(new Variable<int>());
            f.Arguments.Add(new Variable<int>());
            f[5] = new[] {1.0, 2};
        }


        [Test]
        public void SetValuesUsingJaggerdArrayAndCheckToStringResults()
        {
            var values = new[,]
                             {
                                 {1, 2, 3},
                                 {4, 5, 6}
                             };

            var x = new Variable<int>("x");
            var y = new Variable<int>("y");
            var f = new Variable<int>("f");

            // TODO: arguments are added in a different way compare to array, refactor!
            f.Arguments.Add(y);
            f.Arguments.Add(x);

            x.SetValues(new[] {0, 1, 2});
            y.SetValues(new[] {0, 1});
            f.SetValues(values);

            f.GetValues().ToString().Should().Be.EqualTo("{{1, 2, 3}, {4, 5, 6}}");
        }

        [Test]
        public void StringsAreNotIEnumerableForFunction()
        {
            Assert.IsTrue("string" is IEnumerable);
            IFunction function = new Function();
            IVariable<string> component = new Variable<string>();
            IVariable<int> argument = new Variable<int>();
            function.Arguments.Add(argument);
            function.Components.Add(component);
            function[0] = "first";
            Assert.AreEqual("first", function.Components[0][0]);
        }

        [Test]
        public void ToXml()
        {
            var f = new Function("f");
            f.Arguments.Add(new Variable<int>("x"));
            f.Components.Add(new Variable<int>("y"));
            f[1] = 2;
            string expected = File.ReadAllText(@"TestData\FunctionToXml.xml");
            TestHelper.AssertXmlEquals(expected, f.ToXml());
        }

        [Test]
        public void TwoArgumentsOfWhichOneIsOShouldNotResizeTheComponents()
        {
            var f = new Function("f");
            var arg1 = new Variable<int>("x1");
            f.Arguments.Add(arg1);
            f.Arguments.Add(new Variable<int>("x2"));
            f.Components.Add(new Variable<int>("fx"));
            arg1.SetValues(new[] {1, 2, 3});
            Assert.AreEqual(0,f.Components[0].Values.Count);
        }

        /// <summary>
        /// Tests creation of a function to define cross-section properties as a complex 
        /// <code>
        /// <![CDATA[
        /// Declaration:
        /// 
        /// CrossSectionParameters = (A, P)(H)
        /// 
        /// A(h) - dependent variable
        /// P(h) - dependent variable
        /// H - independent variable
        /// 
        ///     A - component: Name = cross-section area, ShortName = A, Quantity = area, Unit = m^2, ValueType = float
        ///     P - component: Name = wetted perimeter, ShortName = P, Quantity = length, Unit = m, ValueType = float
        ///     H - argument: Name = water depth, ShortName = H, Quantity = depth, Unit = m, ValueType = float
        /// 
        /// Example:
        ///         H   | A     | P
        ///         -----------------
        ///         0.0 | 0.0   | 0.0
        ///         1.0 | 10.0  | 12.0
        ///         5.0 | 100.0 | 110.0
        /// 
        /// Definition:
        ///     cross section/ ------------- Function
        ///         Components/
        ///             area --------------- Variable<float>
        ///             wetted perimeter --- Variable<float>
        ///         Arguments/
        ///             water depth -------- Variable<float>
        /// 
        /// ]]>
        /// </code>
        /// </summary>
        [Test]
        public void TwoComponentsOneArgument_CrossSectionParametersUsingIndexes()
        {
            // TODO: make default Units work!
            //Quantity areaQuantity = new Quantity("area", "A", new Unit("m^2"));
            //Quantity lengthQuantity = new Quantity("length", "L", new Unit("m"));
            //Quantity depthQuantity = new Quantity("depth", "H", new Unit("m"));
            var areaUnit = new Unit("area", "A");
            var lengthUnit = new Unit("length", "L");
            var depthUnit = new Unit("depth", "H");

            // create rating curve function arguments and components
            IVariable<double> a = new Variable<double>("area", areaUnit);
            IVariable<double> p = new Variable<double>("wetted_perimeter", lengthUnit);
            IVariable<double> h = new Variable<double>("water_depth", depthUnit);

            // f = (a, p)(h)
            IFunction f = new Function("rating curve");
            f.Components.Add(a);
            f.Components.Add(p);
            f.Arguments.Add(h);

            // set values of the variable
            h.SetValues(new[] {0.1, 1.1, 3.1});

            // value based argument referencing.
            f[0.1] = new[] {0.0, 0.0};
            f[1.1] = new[] {10.0, 100.0};
            f[3.1] = new[] {20.0, 200.0};

            h.Values.Remove(3.1);
            h.Values.Add(3.1);
            f[h.Values.Last()] = new[] {1.0, 2.0};

            Assert.AreEqual(1.0, a.Values[2]);
            Assert.AreEqual(2.0, p.Values[2]);
            Assert.AreEqual(3.1, h.Values[2]);

            Assert.AreEqual(10.0, a.Values[1]);
            Assert.AreEqual(100.0, p.Values[1]);

            // asserts for argument
            Assert.AreEqual(3, f.Arguments[0].Values.Count);
            Assert.AreEqual(0.1, f.Arguments[0].Values[0]);
            Assert.AreEqual(1.1, f.Arguments[0].Values[1]);
            Assert.AreEqual(3.1, f.Arguments[0].Values[2]);

            //asserts for components
            Assert.AreEqual(0.0, f.Components[0].Values[0]);
            Assert.AreEqual(10.0, f.Components[0].Values[1]);
            Assert.AreEqual(1.0, f.Components[0].Values[2]);

            Assert.AreEqual(0.0, f.Components[1].Values[0]);
            Assert.AreEqual(100.0, f.Components[1].Values[1]);
            Assert.AreEqual(2.0, f.Components[1].Values[2]);
        }

        [Test]
        public void UpdateMaxValueWhenValueAddedUsingInsertAt()
        {
            var f = new Function();
            IVariable<double> y = new Variable<double>();
            IVariable<double> x = new Variable<double>();
            f.Components.Add(y);
            f.Arguments.Add(x);

            f[1.0] = 1.0;
            f[2.0] = 2.0;
            f[3.0] = 3.0;

            Assert.AreEqual(3.0, y.Values.MaxValue);

            x.Values.InsertAt(0, 3);

            y.Values[3] = 5;

            Assert.AreEqual(5.0, y.Values.MaxValue);
        }

        [Test]
        public void ValueDoesNotExist()
        {
            var function = new Function();

            IVariable<double> component = new Variable<double>();
            IVariable<double> argument = new Variable<double>();

            function.Components.Add(component);
            function.Arguments.Add(argument);

            object values = function[0.0];
            Assert.AreEqual(null, values);
        }

        [Test]
        public void InsertArgument()
        {
            var f = new Function();
            IVariable<double> y = new Variable<double>();
            IVariable<double> x = new Variable<double>();
            f.Components.Add(y);
            f.Arguments.Add(x);
            //insert an argument at index 0 
            var time = new Variable<DateTime>("t");
            f.Arguments.Insert(0,time);
            Assert.AreEqual(time, f.Arguments[0]);
            Assert.AreEqual(time, y.Arguments[0]);
        }

        [Test]
        public void FunctionIsEditableByDefault()
        {
            var f = new Function();
            Assert.IsTrue(f.IsEditable);
        }

        
        [Test]
        [Category(TestCategory.Performance)]
        [Category(TestCategory.WorkInProgress)] // slow
        public void SetALotOfValuesWithoutEvents()
        {
            //do a manual of function to speed up..disable all the event stuff..
            var f = new Function("f");
            var x = new Variable<int>("x");
            var y = new Variable<int>("y");
            f.Arguments.Add(x);
            f.Components.Add(y);
            f.Store.FireEvents = false;

            int count = 1000000;
            var xValues = Enumerable.Range(1, count);
            var yValues = Enumerable.Repeat(10, count);

            Action action = delegate
                                {
                                    x.SetValues(xValues);
                                    y.SetValues(yValues);
                                };

            TestHelper.AssertIsFasterThan(2525, action);
            
            Assert.AreEqual(10, y[count-1]);
            Assert.AreEqual(count, x.Values[count-1]);
        }

        [Test]
        [Category(TestCategory.Performance)]
        [Category(TestCategory.WorkInProgress)] // slow
        public void SetALotOfValuesWithEvents()
        {
            //do a manual of function to speed up..disable all the event stuff..
            var f = new Function("f");
            var x = new Variable<int>("x");
            var y = new Variable<int>("y");
            f.Arguments.Add(x);
            f.Components.Add(y);

            int count = 1000000;
            var xValues = Enumerable.Range(1, count);
            var yValues = Enumerable.Repeat(10, count);

            TestHelper.AssertIsFasterThan(3400, () =>
                                                    {
                                                        x.SetValues(xValues);
                                                        y.SetValues(yValues);
                                                    });
            
            //quality check
            Assert.AreEqual(10, y[count - 1]);
            Assert.AreEqual(count, x.Values[count - 1]);
        }

        [Test]
        public void RemoveValuesCausesValueChanged()
        {
            var x = new Variable<int>();
            x.SetValues(new[] {1, 2, 3});

            int callCount = 0;
            x.ValuesChanged += delegate { callCount++; };

            x.RemoveValues();

            Assert.AreEqual(3, callCount);
        }

        [Test]
        public void CloneWithoutValues()
        {
            var x = new Variable<int>("x") { Values = { 1, 2, 3 } };
            var y = new Variable<double>("y") { Values = { 1, 2, 3 } };
            var f = new Function("f") { Arguments = { x }, Components = { y } };
            
            var clone = (IFunction) f.Clone(false);

            Assert.AreEqual(1, clone.Arguments.Count);
            Assert.AreEqual(1, clone.Components.Count);
            Assert.IsTrue(clone is Function);
            Assert.IsTrue(clone.Arguments[0] is IVariable<int>);
            Assert.IsTrue(clone.Components[0] is IVariable<double>);

            var store = clone.Store;
            Assert.AreEqual(store, clone.Arguments[0].Store);
            Assert.AreEqual(store, clone.Components[0].Store);
            
            Assert.AreEqual(0, clone.Arguments[0].Values.Count);
        }

        /// <summary>
        /// Test if a default value is added properly
        /// test triggered by SobekNetworkFileReaderTest::ImportElbeNetwork
        /// </summary>
        [Test]
        [Ignore("ignore for now because of shitty implemnentation IVariable")]
        public void AddDefaultValue()
        {
            var x = new Variable<double>("x");
            var y = new Variable<double>("y");
            var f = new Function("f") { Arguments = { x }, Components = { y } };
            Assert.AreEqual(0.0, (double)x.DefaultValue, 1e-6);
            x.SetValues(new [] {-0.8, -0.6, -0.4, -0.2, 0.0, 0.2, 0.4, 0.6, 0.8});
            // test is no exception is thrown:
            // default = 0.0
            // step = 1.0
            // -0.8, -0.6, -0.4, -0.2, 0.0, 0.2, 0.4, 0.6, 0.8
            // -0.8, -0.6, -0.4, -0.2, 0.8, 0.2, 0.4, 0.6, BOOOOOOOOOOOOOM
            //                     value = predeccessor + step
            //                           = -0.2 + 1.0 = 0.8
        }

        [Test]
        public void ChangingFunctionValuesWithSortOptionOnMustSortComponentValues()
        {
            var x = new Variable<int>("x");
            var y = new Variable<int>("y");

            var f = new Function {Arguments = {x}, Components = {y}};

            f[1] = 1;
            f[5] = 5;
            f[10] = 10;
            f[15] = 15;

            f[2] = 2; //add new value
            //x.Values[2] = 2; // change existing x value at position 2, must trigger auto-sort

            log.DebugFormat("x values: {0}", x.Values);
            log.DebugFormat("y values: {0}", y.Values);

            x.Values
                .Should().Have.SameSequenceAs(new [] { 1, 2, 5,10, 15 });

            y.Values
                .Should().Have.SameSequenceAs(new[] { 1, 2, 5, 10, 15 });
        }

        [Test] 
        public void InsertAtForClonedFunction()
        {
            var x = new Variable<int>("x");
            var y = new Variable<int>("y");

            var f = new Function { Arguments = { x }, Components = { y } };

            f[1] = 1;
            f[5] = 5;
            f[10] = 10;
            f[15] = 15;

            var clone = (IFunction) f.Clone();
            clone.Arguments[0].Values.InsertAt(0,4);

            //clone.Arguments[0].Values.InsertAt(0, 4);
            Assert.AreEqual(5,clone.Components[0].Values.Count);
        }

        [Test]
        public void ChangeLastValueToFirst()
        {
            IFunction function = new Function();
            var x = new Variable<double>("x");
            var y = new Variable<double>("y");

            function.Arguments.Add(x);
            function.Components.Add(y);
            function[1.0] = 20.0;
            function[2.0] = 30.0;
            function[3.0] = 50.0;

            //var list = new FunctionBindingList(function);
            //change the 3.0 to a 0.0
            x.Values[2] = 0.0;

            Assert.AreEqual(new[] { 0.0, 1.0, 2.0 }, x.Values);
            Assert.AreEqual(new[] { 50.0, 20.0, 30.0 }, y.Values);
        }

        [Test]
        public void PropertyChangingShouldBeBubbledToFunction()
        {
            var function = new Function();
            function.Arguments.Add(new Variable<TestNotifyPropertyChangedObject>());
            function.Components.Add(new Variable<double>());
            
            var testNotifyPropertyChangedObject = new TestNotifyPropertyChangedObject();
            function[testNotifyPropertyChangedObject] = 1.0d;

            var callCount = 0;
            ((INotifyPropertyChanging) function).PropertyChanging += delegate { callCount++; };
            
            testNotifyPropertyChangedObject.FireChanging();

            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void PropertyChangingShouldBeBubbledToArgument()
        {
            var function = new Function();
            function.Arguments.Add(new Variable<TestNotifyPropertyChangedObject>());
            function.Components.Add(new Variable<double>());

            var testNotifyPropertyChangedObject = new TestNotifyPropertyChangedObject();
            function[testNotifyPropertyChangedObject] = 1.0d;

            var callCount = 0;
            ((INotifyPropertyChanging) function.Arguments[0]).PropertyChanging += delegate { callCount++; };

            testNotifyPropertyChangedObject.FireChanging();

            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void PropertyChangedShouldBeBubbledToFunction()
        {
            var function = new Function();
            function.Arguments.Add(new Variable<TestNotifyPropertyChangedObject>());
            function.Components.Add(new Variable<double>());

            var testNotifyPropertyChangedObject = new TestNotifyPropertyChangedObject();
            function[testNotifyPropertyChangedObject] = 1.0d;

            var callCount = 0;
            ((INotifyPropertyChanged) function).PropertyChanged += delegate { callCount++; };

            testNotifyPropertyChangedObject.FireChanged();

            Assert.AreEqual(2, callCount); // TODO: Should be 1...!?
        }

        [Test]
        public void PropertyChangedShouldBeBubbledToArgument()
        {
            var function = new Function();
            function.Arguments.Add(new Variable<TestNotifyPropertyChangedObject>());
            function.Components.Add(new Variable<double>());

            var testNotifyPropertyChangedObject = new TestNotifyPropertyChangedObject();
            function[testNotifyPropertyChangedObject] = 1.0d;

            var callCount = 0;
            ((INotifyPropertyChanged) function.Arguments[0]).PropertyChanged += delegate { callCount++; };

            testNotifyPropertyChangedObject.FireChanged();

            Assert.AreEqual(1, callCount);
        }

        /// <summary>
        /// When event is fired - array in the second component of the function is empty (incosistent with Arguments.Values.Count)
        /// </summary>
        [Test]
        [Category(TestCategory.WorkInProgress)]
        [Ignore]
        public void ValuesChangedEventFiredWhileFunctionIsNotConsistent()
        {
            IVariable a = new Variable<double>("a");
            IVariable c1 = new Variable<double>("c1");
            IVariable c2 = new Variable<double>("c2");

            IFunction f = new Function();
            f.Arguments.Add(a);
            f.Components.Add(c1);
            f.Components.Add(c2);

            f.ValuesChanged += (s, e) => Assert.AreEqual(1,f.Components[1].Values.Count);

            f[0.0] = new[] { 0.0, 0.0 };
                       
        }

        [Test]
        public void SetFunction2DValuesUsingSlice()
        {
            var x = new Variable<double>("x");
            var y = new Variable<double>("y");

            var f = new Variable<double>("f") {Arguments = {x, y}};

            Assert.IsFalse(f.Values.IsAutoSorted);

            y.Values.Add(0.0);
            y.Values.Add(1.0);

            const double xValue = 10.0;

            f[xValue] = new[] { 1.0, 2.0 };

            x.Values[0]
                .Should().Be.Equals(10.0);

            Assert.AreEqual(new[] { 1.0, 2.0 }, f.Values);

            f.Values
                .Should().Have.SameSequenceAs(new[] { 1.0, 2.0 });
        }

        [Test]
        public void AddArgumentWithValues()
        {
            // y= f(x)
            var x = new Variable<double>("x");
            x.SetValues(new[] { 1.0, 2.0 });

            var y = new Variable<double>("y");
            
            y.Arguments.Add(x);
        }

        [Test]
        public void AfterAddDoNotFireCollectionChangedEvent()
        {
            var i = 0;
            IVariable a = new Variable<double>("a");
            IVariable c1 = new Variable<double>("c1");

            IFunction f = new Function();
            f.Arguments.Add(a);
            f.Components.Add(c1);

            // addng function values generates FunctionValuesChanged event and not CollectionChanged
            f.CollectionChanged += (s, e) => i++;

            f[0.0] = 0.0; //add: should not fire collection changed

            Assert.AreEqual(0, i);

        }
        
        [Test]
        public void ReplaceArgValueInTwoDimensionalFunctionWithOneArgumentEmpty()
        {
            IVariable arg1 = new Variable<double>();
            IVariable arg2 = new Variable<double>();
            IVariable component = new Variable<double>();

            IFunction f = new Function();
            f.Arguments.Add(arg1);
            f.Arguments.Add(arg2);
            f.Components.Add(component);

            arg1.SetValues(new double[]{1,2,3,4,5,6});
            arg1.Values[0] = 10;
        }
    }

    public interface IMockFileBasedFunctionStore : IFunctionStore, IFileBased
    {
        
    }
}