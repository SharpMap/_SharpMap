using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using DelftTools.Functions.Generic;
using DelftTools.Functions.DelftTools.Utils.Tuples;
using DelftTools.TestUtils;
using DelftTools.Units;
using DelftTools.Utils;
using DelftTools.Utils.Collections;
using log4net;
using NUnit.Framework;
using SharpTestsEx;

namespace DelftTools.Functions.Tests.Generic
{
    [TestFixture]
    public class VariableTest
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(VariableTest));

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
        public void SetDateTime()
        {
            IVariable<DateTime> var = new Variable<DateTime>();
            DateTime to = DateTime.Now;
            var.Values.Add(to);
            Assert.IsTrue(var.Values.Contains(to));
        }

        [Test]
        public void Clone()
        {
            var x = new Variable<int>("x")
                {
                    Values = {1, 2, 3},
                    Unit = new Unit("distance", "m"),
                    ExtrapolationType = ExtrapolationType.Constant,
                    InterpolationType = InterpolationType.Linear
                };

            var clone = (IVariable<int>)x.Clone();

            //Assert.IsTrue(clone.Values.SequenceEqual(new[] {1, 2, 3}));
            Assert.AreEqual(x.Name, clone.Name);
            Assert.AreNotEqual(x, clone);
            Assert.AreNotEqual(x.Store, clone.Store); // clone creates a new deep copy of the variable (in-memory)

            x.Values
                .Should("values must be copied").Have.SameSequenceAs(new[] { 1, 2, 3 });
            Assert.AreEqual(x.ExtrapolationType, clone.ExtrapolationType);
            Assert.AreEqual(x.InterpolationType, clone.InterpolationType);
            Assert.AreNotSame(x.Unit, clone.Unit);
            Assert.AreEqual(x.Unit.Name, clone.Unit.Name);
            Assert.AreEqual(x.Unit.Symbol, clone.Unit.Symbol);
        }

        [Test]
        public void CloneUsingStore()
        {
            var x = new Variable<int>("x") { Values = { 1, 2, 3 } };

            var clone = (IVariable<int>)x.Clone();

            //Assert.IsTrue(clone.Values.SequenceEqual(new[] {1, 2, 3}));
            Assert.AreEqual(x.Name, clone.Name);
            Assert.AreNotEqual(x, clone);
            Assert.AreNotEqual(x.Store, clone.Store); // clone creates a new deep copy of the variable (in-memory)

            x.Values
                .Should("values must be copied").Have.SameSequenceAs(new[] { 1, 2, 3 });
        }

        [Test]
        public void CloneDependentVariable()
        {
            var x = new Variable<int>("x") { Values = { 1, 2, 3 } };
            var y = new Variable<int>("y") { Arguments = { x } };

            // TODO: would be nice to call above as: var y = = new Variable<int>("y") { Arguments = { x } };

            var clonedY = (IVariable<int>)y.Clone();

            // check argument
            Assert.AreEqual(y.Name, clonedY.Name);
            Assert.AreNotEqual(y, clonedY);
            Assert.AreNotEqual(y.Store, clonedY.Store);
            Assert.AreEqual(y.Values.Count, clonedY.Values.Count);

            // check argument
            var clonedX = (IVariable<int>)clonedY.Arguments[0];
            Assert.AreEqual(x.Name, clonedX.Name);
            Assert.AreNotEqual(x, clonedX);
            Assert.AreNotEqual(x.Store, clonedX.Store);
            Assert.AreEqual(x.Values.Count, clonedX.Values.Count);
            Assert.IsTrue(x.Values.SequenceEqual(clonedX.Values));
            Assert.AreEqual(y.Arguments[0].Values[0], clonedY.Arguments[0].Values[0]);
        }

        [Test, ExpectedException(typeof(InvalidOperationException))]
        public void AddComponentsToVariableGivesException()
        {
            IVariable x = new Variable<int>("x");
            x.Components.Add(new Variable<int>("y"));
        }

        [Test]
        public void SetName()
        {
            IVariable x = new Variable<int>();
            x.Name = "X";
        }

        [Test]
        public void AddValuesAndGetUsingIndex()
        {
            IVariable<double> v = new Variable<double>();

            v.Values.Add(0.0);
            v.Values.Add(0.1);
            v.Values.Add(0.2);

            Assert.AreEqual(3, v.Values.Count);
            Assert.AreEqual(0.1, v.Values[1]);
        }

        [Test]
        public void SetAndGetValuesAsArray()
        {
            IVariable<double> v = new Variable<double>();

            v.SetValues(new[] { 0.0, 0.1, 0.2 });

            IList<double> values = v.Values;

            Assert.AreEqual(3, v.Values.Count);
            Assert.AreEqual(0.1, values[1]);
        }

        [Test]
        public void SetAndGetValuesAsArrayNonGeneric()
        {
            IVariable v = new Variable<double>();

            v.SetValues(new[] { 0.0, 0.1, 0.2 });

            IList values = v.Values;

            Assert.AreEqual(3, v.Values.Count);
            Assert.AreEqual(0.1, values[1]);
        }

        [Test]
        public void ValueType()
        {
            IVariable v = new Variable<double>();
            v.Values.Add(1.0);

            Assert.AreEqual(typeof(double), v.ValueType);
            Assert.AreEqual(typeof(double), v.Values[0].GetType());
        }

        [Test]
        public void DefaultValue()
        {
            IVariable v = new Variable<double>();
            Assert.AreEqual(default(double), v.DefaultValue);
        }

        [Test]
        public void NoDataValues()
        {
            IVariable v = new Variable<double>();

            // The next test seems rather stupid and it is. Previous implementation of IVariabele 
            // returned a copy to the array thus breaking all modifications
            v.NoDataValues.Add(-99.0);
            Assert.AreEqual(1, v.NoDataValues.Count);
            Assert.AreEqual(-99.0, v.NoDataValues[0]);
        }

        [Test] //this used to lead to an invalid operation exception, but it should not
        public void ReplaceDateTimeValue()
        {
            var dateTime = new DateTime(2008, 1, 1);
            IVariable v = new Variable<DateTime>();
            v.SetValues(new[] { new DateTime(2008, 1, 1) });
            v.Values[0] = dateTime;
        }

        [Test, ExpectedException(typeof(InvalidOperationException))]
        public void AddNonUniqueDateTimeValue()
        {
            var dateTime = new DateTime(2008, 1, 1);
            IVariable v = new Variable<DateTime>();
            v.SetValues(new[] { new DateTime(2008, 1, 1) });
            //try adding non-unique value (not allowed)
            v.Values.Add(dateTime);
        }

        [Test]
        public void Unit()
        {
            IVariable v = new Variable<double>();
            Assert.IsNull(v.Unit);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void IndependendVariableAddSameValueGivesException()
        {
            IVariable<int> variable = new Variable<int>();
            variable.Values.Add(1);
            variable.Values.Add(1); // <- exception
        }

        [Test]
        public void IndependendVariableInsertNewValueAtTheEndAddsAutoincrementedValue()
        {
            // replacing of default value should not be standard behaviour!
            IVariable<int> variable = new Variable<int>
            {
                GenerateUniqueValueForDefaultValue = true
            };
            variable.Values.Add(10);
            variable.Values.InsertAt(0, 1); // add a new value at dimension 0, index 1

            var expectedValue = 10 + (int)variable.DefaultStep;
            Assert.AreEqual(expectedValue, variable.Values[1]);
        }

        [Test]
        public void IndependendVariableInsertNewValueUsingIListAtTheEndAddsAutoincrementedValue()
        {
            // replacing of default value should not be standard behaviour!
            IVariable<int> variable = new Variable<int>
            {
                GenerateUniqueValueForDefaultValue = true
            };
            variable.Values.Add(10);

            IList list = variable.Values;
            list.Insert(1, variable.DefaultValue); // add a new default value at the end autoincrements value

            var expectedValue = 10 + (int)variable.DefaultStep;
            Assert.AreEqual(expectedValue, variable.Values[1]);
        }

        [Test]
        public void ReplaceExistingValueWithTheSameValue()
        {
            IVariable<int> variable = new Variable<int>();
            variable.Values.Add(0);
            variable.Values[0] = 0;  // <- no exception, the same value
            Assert.AreEqual(0, variable.Values[0]);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ReplaceExistingValueWithNonUniqueValueThrowsException()
        {
            IVariable<int> variable = new Variable<int>();
            variable.Values.Add(0);
            variable.Values.Add(1);
            variable.Values[1] = 0; // <- exception
        }

        [Test]
        public void IsIndependent()
        {
            IVariable v = new Variable<double>();
            Assert.IsTrue(v.IsIndependent);

            v.Arguments.Add(new Variable<double>("z"));
            Assert.IsFalse(v.IsIndependent);
        }

        [Test]
        public void DefaultComponentsAndArguments()
        {
            IVariable<double> x = new Variable<double>();

            Assert.AreEqual(x, x.Components[0]);
            Assert.AreEqual(0, x.Arguments.Count);
        }

        [Test]
        public void ChangeDefaultValue()
        {
            IVariable<double> x = new Variable<double>("x");
            x.DefaultValue = 45;
            Assert.AreEqual(x.DefaultValue, x.Store.GetVariableValues(x).DefaultValue);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException), ExpectedMessage = "Value of type System.Double, but expected type System.Single for variable x")]
        public void SetValuesOnVariableWithWrongTypeShouldThrowException()
        {
            IVariable<float> x = new Variable<float>("x");
            x.SetValues(new[] { 1.0, 2.0, 3.0 });
        }

        [Test]
        public void DependentVariableValues()
        {
            IVariable<double> x = new Variable<double>("x");
            IVariable<double> y = new Variable<double>("y");

            y.Arguments.Add(x); // make y = y(x)

            x.SetValues(new[] { 0.0, 0.1, 0.2 });

            Assert.AreEqual(3, x.Values.Count);
            Assert.AreEqual(3, y.Values.Count);
            Assert.AreEqual(y.DefaultValue, y.Values[0]);

            y[0.0] = 5.0d;
            Assert.AreEqual(5.0, y[0.0]);
        }

        [Test]
        public void AddDependentVariableValuesViaMultidimensionalArray()
        {
            IVariable<double> x = new Variable<double>("x");
            IVariable<double> y = new Variable<double>("y");

            y.Arguments.Add(x); // make y = y(x)

            x.Values.Add(0.0);
            x.Values.Add(0.1);
            x.Values.Add(0.2);

            Assert.AreEqual(3, x.Values.Count);
            Assert.AreEqual(3, y.Values.Count);
            Assert.AreEqual(y.DefaultValue, y.Values[0]);
        }

        [Test]
        public void AddDependentVariableValuesViaMultidimensionalArrayAndResize()
        {
            IVariable<double> x = new Variable<double>("x");
            IVariable<double> y = new Variable<double>("y");

            y.Arguments.Add(x); // make y = y(x)
            x.GenerateUniqueValueForDefaultValue = true;

            x.Values.Resize(1);
            x.Values[0] = 0.0;

            x.Values.Resize(2);
            x.Values[1] = 0.1;

            x.Values.Resize(3);
            x.Values[2] = 0.2;

            Assert.AreEqual(3, x.Values.Count);
            Assert.AreEqual(3, y.Values.Count);
            Assert.AreEqual(y.DefaultValue, y.Values[0]);
        }

        [Test]
        public void AddDependentVariableValuesViaMultidimensionalArrayAndResize_Multiple()
        {
            IVariable<double> x = new Variable<double>("x");
            IVariable<double> y = new Variable<double>("y");

            y.Arguments.Add(x); // make y = y(x)
            x.GenerateUniqueValueForDefaultValue = true;

            x.Values.Resize(3);
            x.Values[0] = 0.0;
            x.Values[1] = 0.1;
            x.Values[2] = 0.2;

            Assert.AreEqual(3, x.Values.Count);
            Assert.AreEqual(3, y.Values.Count);
            Assert.AreEqual(y.DefaultValue, y.Values[0]);
        }

        /*[Test]
        public void TestVariableOwnerForSubArray()
        {
            //test on class because our MDA interface does not have owner.
            //test here and not in MDA because owner is a variable
            IMultiDimensionalArray array = new MultiDimensionalArray(3, 3);
            array[1, 1] = 5;
            ((MultiDimensionalArray)array).Owner = new Variable<double>("x");
            array = array.Select(0, 0, 2).Select(1, 0, 2);
            Assert.AreEqual(5, array[1,1]);
        }*/

        [Test, NUnit.Framework.Category(TestCategory.Performance)]
        public void PerformanceAddValuesToArgument_UsingSetValues_WithEvents()
        {
            var x = new Variable<DateTime>("x");
            var y = new Variable<double>("y") { Arguments = { x } };

            // prepare values
            var xValues = new List<DateTime>();
            var yValues = new List<double>();

            const int ValuesToAdd = 50000;
            var t = DateTime.Now;
            var time = DateTime.Now;
            for (var i = 0; i < ValuesToAdd; i++)
            {
                xValues.Add(time);
                yValues.Add(i);

                time = time.AddDays(1);
            }
            var dt = DateTime.Now.Subtract(t).TotalMilliseconds;
            log.DebugFormat("Added {0} values in {1} ms", ValuesToAdd, dt);


            TestHelper.AssertIsFasterThan(220, string.Format("Add {0} vlaues to function", ValuesToAdd), () =>
                {
                    x.SetValues(xValues);
                    y.SetValues(yValues);
                });
        }

        [Test, NUnit.Framework.Category(TestCategory.Performance)]
        public void PerformanceAddValuesToArgument_WithEvents()
        {
            IVariable<double> x = new Variable<double>("x") { GenerateUniqueValueForDefaultValue = true };
            IVariable<double> y = new Variable<double>("y");

            y.Arguments.Add(x); // make y = y(x)
            
            const int valuesToAdd = 20000;

            x.Values.InsertAt(0, 0, valuesToAdd); //warmup
            x.Values.Clear();

            TestHelper.AssertIsFasterThan(30, () =>
                                               x.Values.InsertAt(0, 0, valuesToAdd));
        }

        [Test, NUnit.Framework.Category(TestCategory.Performance)]
        public void PerformanceAddValuesToArgumentWithEventsIndependentVariable()
        {
            IVariable<double> x = new Variable<double>("x");

            x.Values.Add(0.0);
            x.Values.Remove(0.0);

            TestHelper.AssertIsFasterThan(135, () =>
            {
                const int valuesToAdd = 10000;

                for (var i = 0; i < valuesToAdd; i++)
                {
                    x.Values.Add(i);
                }
            });

        }

        [Test, NUnit.Framework.Category(TestCategory.Performance)]
        public void PerformanceAddValuesRangeToArgumentWithEventsIndependentVariable()
        {
            IVariable<double> x = new Variable<double>("x");

            var t = DateTime.Now;

            const int valuesToAdd = 50000;
            var doubles = Enumerable.Range(1, valuesToAdd).Select(Convert.ToDouble).ToArray();

            TestHelper.AssertIsFasterThan(94, () => x.Values.AddRange(doubles));

        }

        [Test]
        public void TupleVariable_AddValues()
        {
            IVariable<Pair<int, double>> tupleVariable = new Variable<Pair<int, double>>();

            tupleVariable.Values.Add(new Pair<int, double>(1, 0.0));
            tupleVariable.Values.Add(new Pair<int, double>(1, 1.0));
            tupleVariable.Values.Add(new Pair<int, double>(1, 2.0));
            tupleVariable.Values.Add(new Pair<int, double>(1, 3.0));
            tupleVariable.Values.Add(new Pair<int, double>(2, 0.0));
            tupleVariable.Values.Add(new Pair<int, double>(2, 0.5));
            tupleVariable.Values.Add(new Pair<int, double>(2, 1.0));

            Assert.IsTrue(tupleVariable.Values.Contains(new Pair<int, double>(1, 3.0)));
            Assert.IsFalse(tupleVariable.Values.Contains(new Pair<int, double>(1, 4.0)));
        }

        [Test]
        [Ignore("Unfinished, 15.06.2009")] // TODO: unfinished test
        public void IndependendFunctionTwoComponents()
        {
            //every pair of c1 c2 is a value of the independed function 
            IVariable x = new Variable<Pair<int, double>>();

            //independend function
            Assert.IsTrue(x.IsIndependent);

            Assert.AreEqual(2, x.Components.Count, "DelftTools.Utils.Tuple variable is composite variables containing variable for each DelftTools.Utils.Tuple component");

            Assert.AreEqual(typeof(int), x.Components[0]);
            Assert.AreEqual(typeof(double), x.Components[0]);

            x.SetValues(new[] { new Pair<int, double>(2, 1.0) });

            IVariable<int> y = new Variable<int>();
            y.Arguments.Add(x);

            Assert.AreEqual(1, y.Values.Count);

            x.SetValues(new[] { new Pair<int, double>(3, 1.0) });

            Assert.AreEqual(2, y.Values.Count);
        }

        [Test]
        public void ReplaceValue()
        {
            IVariable<int> x = new Variable<int>();
            x.Values.Add(1);
            x.Values[0] = 1;
        }

        [Test]
        public void InsertingValuesAreSortedAutomatically()
        {
            var x = new Variable<int>("x") { Values = { 1, 2, 5 } };
            x.Values.Add(3);
            Assert.IsTrue(x.Values.SequenceEqual(new[] { 1, 2, 3, 5 }));

            IVariable<string> strings = new Variable<string>("strings") { DefaultValue = "" };
            strings.Arguments.Add(x);
            Assert.IsTrue(new[] { "", "", "", "" }.SequenceEqual(strings.Values));

            strings[4] = "mies";
            Assert.IsTrue(new[] { "", "", "", "mies", "" }.SequenceEqual(strings.Values));
        }

        [Test]
        public void ChangingValuesRemainsSorted()
        {
            var x = new Variable<int>("x") { Values = { 1, 2, 5 } };
            x.Values[1] = 6;
            Assert.IsTrue(x.Values.SequenceEqual(new[] { 1, 5, 6 }));
        }

        [Test]
        public void AddValuesPossibleForIndependVariable()
        {
            IVariable<int> x = new Variable<int>();
            //TODO: factor to x.Values.Add( ) and check the array in function
            x.AddValues(new[] { 2 });
            Assert.AreEqual(1, x.Values.Count);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void AddValuesGivesExceptionOnDependendVariables()
        {
            IVariable<int> x = new Variable<int>();
            IVariable<int> y = new Variable<int>();
            y.Arguments.Add(x);
            //TODO: factor to x.Values.Add( ) and check the array in function
            y.AddValues(new[] { 2 });
        }

        [Test]
        public void ToXml()
        {
            IVariable x = new Variable<int>("x");
            x.Values.Add(1);
            x.Values.Add(2);
            string expected = File.ReadAllText(@"TestData\VariableToXml.xml");

            TestHelper.AssertXmlEquals(expected, x.ToXml());
        }

        [Test]
        public void FirePropertyChangedEventForValuesImplementingPropertyChanged()
        {
            var variable = new Variable<A>();
            variable.Values.Add(new A { Name = "name1" });

            var count = 0;
            ((INotifyPropertyChanged)variable).PropertyChanged += delegate { count++; };

            variable.Values[0].Name = "name2";

            Assert.AreEqual(1, count);
        }

        [Test]
        public void SetArgumentsUsingPropertyInitializer()
        {
            IVariable<int> x = new Variable<int> { Values = { 1, 2, 3 } };
            IVariable<int> y = new Variable<int> { Values = { 1, 2 } };

            IVariable<double> f = new Variable<double> { Arguments = { x, y } };

            Assert.AreEqual(f.Values.Count, x.Values.Count * y.Values.Count);
        }

        [Test]
        public void AssignValuesOf2DVariableFrom2DArray()
        {
            var x = new Variable<int> { Values = { 1, 2, 3 } };
            var y = new Variable<int> { Values = { 1, 2 } };

            var values2D = new[,] { { 1, 2 }, { 3, 4 }, { 5, 6 } };

            IVariable<int> f = new Variable<int>
            {
                Arguments = { x, y },
                Values = values2D.ToMultiDimensionalArray()
            };
        }

        [Test]
        public void ResizeIndependentVariableAddsUniqueValues()
        {
            var x = new Variable<int> { GenerateUniqueValueForDefaultValue = true };
            x.Values.Resize(5);
            Assert.IsTrue(x.Values.HasUniqueValues());
        }

        [Test]
        public void VariableWithoutSort()
        {
            var x = new Variable<int> { IsAutoSorted = false };
            var values = new[] { 2, 1, 3 };
            x.SetValues(values);

            Assert.AreEqual(values, x.Values, "no sorting must take place");
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "Unable to generate next value for variable of type System.String. Add a NextValueGenerator to the variable")]
        public void ThrowExceptionIfTypeIsNotSupportedInGetNextValue()
        {
            //string is not supported
            var x = new Variable<string> {GenerateUniqueValueForDefaultValue = true};
            //insert a single 'default' value at 0,0
            x.Values.InsertAt(0, 0);
        }

        [Test]
        public void GetMinMaxFromEmptyVariable()
        {
            var v = new Variable<double>();
            
            v.MaxValue
                .Should().Be.EqualTo(default(double));

            v.MinValue
                .Should().Be.EqualTo(default(double));
        }

        [Test]
        public void MinManValidValueForEmptyVariable()
        {
            var doubleVariable = new Variable<double>();
            Assert.AreEqual(double.MinValue, doubleVariable.MinValidValue);
            Assert.AreEqual(double.MaxValue, doubleVariable.MaxValidValue);

            var boolVariable = new Variable<bool>();
            Assert.AreEqual(false, boolVariable.MinValidValue);
            Assert.AreEqual(true, boolVariable.MaxValidValue);
        }

        private class A : INotifyPropertyChange, IComparable
        {
            private string name;

            public string Name
            {
                get { return name; }
                set
                {
                    if (PropertyChanging != null)
                    {
                        PropertyChanging(this, new PropertyChangingEventArgs("Name"));
                    }

                    name = value;

                    if (PropertyChanged != null)
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs("Name"));
                    }
                }
            }

            public int CompareTo(object obj)
            {
                return Name.CompareTo(obj);
            }

            public event PropertyChangingEventHandler PropertyChanging;

            public event PropertyChangedEventHandler PropertyChanged;

            bool INotifyPropertyChange.HasParent
            {
                get; set;
            }
        }
    }
}