using System;
using DelftTools.Functions.Filters;
using DelftTools.Functions.Generic;
using NUnit.Framework;

namespace DelftTools.Functions.Tests.Filters
{
    [TestFixture]
    public class VariableValueFilterTest
    {
        [Test]
        public void FilterDependendVariableWithMultipleValues()
        {
            IVariable<int> x = new Variable<int>("x");
            IVariable<int> y = new Variable<int>("y");
            x.Arguments.Add(y);
            x[0] = 1;
            x[1] = 2;
            x[2] = 3;
            x[3] = 4;
            Assert.AreEqual(4, x.Values.Count);
            IVariable<int> filteredX = (IVariable<int>)x.Filter(new VariableValueFilter<int>(y, new[] { 0, 2 }));
            Assert.AreEqual(2, filteredX.Values.Count);
            Assert.AreEqual(1, filteredX.Values[0]);
            Assert.AreEqual(3, filteredX.Values[1]);
        }

        [Test]
        public void GetFilteredValuesFrom2DFunction()
        {
            var x = new Variable<int>();
            var y = new Variable<int>();

            var z = new Variable<double> { Arguments = { x, y } };

            z[0, 0] = 1.0;
            z[0, 1] = 2.0;
            z[1, 0] = 3.0;
            z[1, 1] = 4.0;

            var xFilter = new VariableValueFilter<int>(x, 0);
            var yFilter = new VariableValueFilter<int>(y, 1);

            var values = z.GetValues(xFilter, yFilter);

            values.Count
                .Should("query values using 2 filters").Be.EqualTo(1);

            values[0]
                .Should("query values using 2 filters").Be.EqualTo(2.0);
        }

        [Test]
        public void Interpolation()
        {
            var argument = new Variable<int>("x") { Values = { 0, 10, 20 } };
            argument.InterpolationType = InterpolationType.Constant;
            var component = new Variable<int>("y") { Values = { 3, 4, 5 } };
            var component2 = new Variable<int>("yy") { Values = { 6, 7, 8 } };

            var function = new Function() { Arguments = { argument }, Components = { component, component2 } };

            var xFilter = new VariableValueFilter<int>(argument, 15);
            var valueY = function.Components[0].Evaluate<int>(xFilter);
            var valueYY = function.Components[1].Evaluate<int>(xFilter);

            Assert.AreEqual(4, valueY);
            Assert.AreEqual(7, valueYY);
        }

        [Test]
        public void Extrapolation()
        {
            var argument = new Variable<int>("x") { Values = { 0, 10, 20 } };
            argument.InterpolationType = InterpolationType.Constant;
            argument.ExtrapolationType = ExtrapolationType.Constant;
            var component = new Variable<int>("y") { Values = { 3, 4, 5 } };
            var component2 = new Variable<int>("yy") { Values = { 6, 7, 8 } };

            var function = new Function() { Arguments = { argument }, Components = { component, component2 } };

            var xFilter = new VariableValueFilter<int>(argument, 40);
            var valueY = function.Components[0].Evaluate<int>(xFilter);
            var valueYY = function.Components[1].Evaluate<int>(xFilter);

            Assert.AreEqual(5, valueY);
            Assert.AreEqual(8, valueYY);
        }

        [Test]
        [ExpectedException(typeof(NotSupportedException), 
            ExpectedMessage = "Variable 'Kees' was not found in arguments or components of function 'Pietje'")]
        public void ThrowCorrectExceptionWhenVariableIsNotInArgumentsOfFunction()
        {
            var argument = new Variable<int>("x") { Values = { 0, 10, 20 } };
            var component = new Variable<int>("y") { Values = { 3, 4, 5 } };
            var function = new Function{ Name = "Pietje",Arguments = { argument }, Components = { component}};

            //try to filter on an unknown argument. This is a common mistake
            //should give a nice exception.
            function.Evaluate<int>(new VariableValueFilter<double>(new Variable<double>{Name = "Kees"}, 2.0));
        }
    }
}