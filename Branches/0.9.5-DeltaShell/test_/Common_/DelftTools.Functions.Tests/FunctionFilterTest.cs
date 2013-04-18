using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DelftTools.Functions.Filters;
using DelftTools.Functions.Generic;
using NUnit.Framework;

namespace DelftTools.Functions.Tests
{
    [TestFixture]
    public class FunctionFilterTest
    {

        [Test]
        public void FilterFunction()
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

            x.SetValues(new[] { 1.0, 2.0, 3.0 });
            y.SetValues(new[] { 10.0, 20.0 });

            f.Components[0].Values[1, 1] = 100.0;
            f.Components[1].Values[1, 1] = 200.0;

            // fx       10.0  20.0 
            //          ------------
            //    1.0  | 0.0    0.0
            //    2.0  | 0.0  100.0 
            //    3.0  | 0.0    0.0
            //
            // fy       10.0  20.0 
            //          ------------
            //    1.0  | 0.0    0.0
            //    2.0  | 0.0  200.0 
            //    3.0  | 0.0    0.0

            var filteredFunction = f.Filter(x.CreateValueFilter(2.0), y.CreateValueFilter(20.0));

            Assert.AreEqual(2, filteredFunction.Components.Count);
            Assert.AreEqual(2, filteredFunction.Arguments.Count);

            Assert.AreEqual(1, filteredFunction.Arguments[0].Values.Count);
            Assert.AreEqual(1, filteredFunction.Arguments[1].Values.Count);
            Assert.AreEqual(1, filteredFunction.Components[0].Values.Count);
            Assert.AreEqual(1, filteredFunction.Components[1].Values.Count);

            Assert.AreEqual(100.0, filteredFunction.Components[0].Values[0]);
            Assert.AreEqual(200.0, filteredFunction.Components[1].Values[0]);
        }

        [Test]
        public void FilterDependendVariable()
        {
            //int sa = componentArrays.Aggregate(1, (product, item) => (product *= item.Count));
            IVariable<double> y = new Variable<double>("y");
            IVariable<double> x = new Variable<double>("x");

            y.Arguments.Add(x);

            x.SetValues(new[] { 1.0, 2.0, 3.0 });
            y.SetValues(new[] { 10.0, 20.0, 30.0 });

            var filtered = y.Filter(x.CreateValueFilter(2.0));

            Assert.AreEqual(1, filtered.Components[0].Values.Count);
            Assert.AreEqual(20, filtered.Values[0]);
        }
        [Test]
        [Ignore("Implement later")]
        public void GetValueOfFilteredFunctionUsingValueOfFiltererArgument()
        {
            IVariable<double> y = new Variable<double>("y");
            IVariable<double> x = new Variable<double>("x");

            y.Arguments.Add(x);

            x.SetValues(new[] { 1.0, 2.0, 3.0 });
            y.SetValues(new[] { 10.0, 20.0, 30.0 });

            var filtered = y.Filter(x.CreateValueFilter(2.0));
            
            //arguments of filtered functions are ignored!
            Assert.IsNull(filtered[3.0]);
        }


        [Test]
        public void FilterIndependendVariable()
        {
            IVariable<int> x = new Variable<int>();
            x.SetValues(new[] { 1, 2, 3, 4, 5 });

            //filter out the middle 
            var filtered = x.Filter(x.CreateValuesFilter(new[] { 2, 3, 4 }));

            Assert.AreEqual(3, filtered.Values[1]);
            Assert.AreEqual(3, filtered.GetValues().Count);
        }

        [Test]
        public void AddFilter()
        {
            IVariable<int> x = new Variable<int>();
            x.SetValues(new[] { 1, 2, 3, 4, 5 });

            x.Filters.Add(x.CreateValuesFilter(new[] { 2, 3, 4 }));

            Assert.IsTrue(x.Values.SequenceEqual(new[] { 2, 3, 4 }));
        }

        [Test]
        public void FilterFilteredFunction()
        {
            IVariable<int> x = new Variable<int>();
            x.SetValues(new[] { 1, 2, 3, 4, 5 });

            var filtered = x.Filter(x.CreateValuesFilter(new[] { 2, 3, 4 }));

            var filtered2 = filtered.Filter(x.CreateValueFilter(3));

            Assert.AreEqual(3, filtered.Values.Count);
            Assert.IsTrue(filtered.Values.Cast<int>().SequenceEqual(new[] { 2, 3, 4 }));

            Assert.AreEqual(1, filtered2.Values.Count);
            Assert.AreEqual(3, filtered2.Values[0]);

            // change filters
            ((IVariableValueFilter)filtered2.Filters[0]).Values.Add(5);
            ((IVariableValueFilter)filtered2.Filters[0]).Values.Add(4);
            Assert.AreEqual(2, filtered2.Values.Count);

            // expanding filter in the "filtered" will also expand "filtered2"
            ((IVariableValueFilter)filtered.Filters[0]).Values.Add(5);
            Assert.AreEqual(4, filtered.Values.Count);
            Assert.AreEqual(3, filtered2.Values.Count);
        }

        /// <summary>
        /// Simple test of function with two arguments and 1 component.
        /// </summary>
        [Test]
        public void Function2DGetValuesUsingOneArgumentValue()
        {
            IVariable<float> f = new Variable<float>("f");
            IVariable<float> x1 = new Variable<float>("x1");
            IVariable<float> x2 = new Variable<float>("x2");

            IFunction function = new Function("OneComponentTwoArguments Test");
            function.Components.Add(f);
            function.Arguments.Add(x1);
            function.Arguments.Add(x2);

            /*   x2
             *    ^
             *    |
             *    |  x  x     <=== f(x1,x2)
             *    |  x  x
             *    |  x  x
             *    | 
             *     --------> x1
             * 
             */

            function[0.0f, 0.0f] = 0.0f;
            function[1.0f, 0.0f] = 0.0f;
            function[0.0f, 1.0f] = 100.0f;
            function[1.0f, 1.0f] = 100.0f;
            function[0.0f, 2.0f] = 200.0f;
            function[1.0f, 2.0f] = 200.0f;


            // get all values
            IList allValues = function.GetValues();
            Assert.AreEqual(6, allValues.Count);

            // get values filtered by 1st argument
            IMultiDimensionalArray filteredValues1 = function.GetValues(new VariableValueFilter<float>(x1, 1.0f));

            Assert.AreEqual(x2.Values.Count, filteredValues1.Count);

            // get values filtered by 2 arguments
            IMultiDimensionalArray filteredValues2 = function.GetValues(x1.CreateValueFilter(1.0f), x2.CreateValueFilter(1.0f));

            Assert.AreEqual(1, filteredValues2.Count);
            Assert.AreEqual(100f, filteredValues2[0, 0]);
        }

        [Test]
        public void FilterUsingComponent()
        {
            IFunction f = new Function();
            IVariable c1 = new Variable<int>("c1");
            IVariable c2 = new Variable<int>("c2");
            IVariable x = new Variable<int>("x");
            IVariable y = new Variable<int>("y");
            f.Components.Add(c1);
            f.Components.Add(c2);
            f.Arguments.Add(x);
            f.Arguments.Add(y);

            f.SetValues(
                new[] { 100, 200 },
                new VariableValueFilter<int>(x, new[] { 1, 2, 3, 4, 5 }),
                new VariableValueFilter<int>(y, new[] { 1, 2, 3 })
                );

            IFunction filtered = f.Filter(new ComponentFilter(c1));

            IMultiDimensionalArray<int> values = filtered.GetValues<int>(new VariableValueFilter<int>(x, new[] { 1, 2, 3 }));

            Assert.IsTrue(values.Shape.SequenceEqual(new[] { 3, 3 }));
            Assert.AreEqual(100, values[2, 2]);
        }

        [Test]
        public void GetValuesOfFilteredFunction()
        {
            IFunction function = new Function();
            function.Components.Add(new Variable<int>());
            function.Arguments.Add(new Variable<int>());
            function[0] = 1;
            function[1] = 2;
            function[2] = 3;


            IFunction filtered =
                function.Filter(new VariableValueFilter<int>(function.Arguments[0], new[] {0, 1}));

            Assert.AreEqual(1, filtered[0]);
        }
    }
}