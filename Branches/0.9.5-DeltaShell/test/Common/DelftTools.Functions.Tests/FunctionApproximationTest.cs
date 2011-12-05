using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DelftTools.Functions.Filters;
using DelftTools.Functions.Generic;
using NUnit.Framework;

namespace DelftTools.Functions.Tests
{
    [TestFixture]
    public class FunctionApproximationTest
    {

        [Test]
        public void EvaluateReturnsDefaultValueOnEmptyFunction()
        {
            IVariable<double> x = new Variable<double>("x");
            IVariable<double> y = new Variable<double>("y");

            IFunction f = new Function();
            f.Arguments.Add(x);
            f.Components.Add(y);
            x.ExtrapolationType = ApproximationType.Constant; 

            y.DefaultValue = 10.0;
            
            Assert.AreEqual(10.0, f.Evaluate<double>(x.CreateValueFilter(1.0)));
        }

        [Test]
        public void GetInterpolatedValueReturnsDefaultValueOnEmpty2ArgsFunction()
        {
            IVariable<double> x = new Variable<double>("x");
            IVariable<double> y = new Variable<double>("y");
            x.ExtrapolationType = ApproximationType.Constant;
            y.ExtrapolationType = ApproximationType.Constant;
            
            IVariable<double> f1 = new Variable<double>("f1");

            IFunction function = new Function("testfunction");
            function.Arguments.Add(x);
            function.Arguments.Add(y);
            function.Components.Add(f1);
            f1.DefaultValue = 100.0;

            Assert.AreEqual(100,
                            f1.Evaluate<double>(x.CreateValueFilter(7.5),
                                                (new VariableValueFilter<double>(y, 68.2))));
        }

        [Test]
        public void InterpolateConstant1D()
        {
            //defines a piece-wise-constant function. Value is always equals to the value of the neareast smaller 
            //argument
            IVariable<double> x = new Variable<double>("x");
            IVariable<double> y = new Variable<double>("y");

            Function function = new Function();
            function.Arguments.Add(x);
            function.Components.Add(y);

            var xValues = new[] { 1.0, 2.0, 3.0 };
            var yValues = new[] { 100.0, 200.0, 300.0 };

            function.SetValues(yValues, new VariableValueFilter<double>(x, xValues), new ComponentFilter(y));

            x.InterpolationType = ApproximationType.Constant;
            var value = function.Evaluate<double>(new VariableValueFilter<double>(x, 1.5));
            Assert.AreEqual(100.0, value);

            value = function.Evaluate<double>(new VariableValueFilter<double>(x, 2.5));
            Assert.AreEqual(200.0, value);

        }
        
        [Test]
        public void InterpolateLinear1D()
        {
            IVariable<double> x = new Variable<double>("x");
            IVariable<double> y = new Variable<double>("y");

            Function function = new Function();
            function.Arguments.Add(x);
            function.Components.Add(y);

            var xValues = new[] {1.0, 2.0, 3.0};
            var yValues = new[] {100.0, 200.0, 300.0};

            function.SetValues(yValues, new VariableValueFilter<double>(x, xValues), new ComponentFilter(y));

            var value = function.Evaluate<double>(new VariableValueFilter<double>(x, 1.5));
            Assert.AreEqual(150.0, value);

            value = function.Evaluate<double>(new VariableValueFilter<double>(x, 2.5));
            Assert.AreEqual(250.0, value);

            value = function.Evaluate<double>(new VariableValueFilter<double>(x, 1.75));
            Assert.AreEqual(175, value);
        }

        [Test]
        public void GetExtraPolatedValueFor2dFunctionWithOnePointDefined()
        {
            IVariable<double> x1 = new Variable<double>("x1");
            IVariable<double> x2 = new Variable<double>("x2");
            IVariable<double> y = new Variable<double>("y");

            IFunction function = new Function();
            function.Arguments.Add(x1);
            function.Arguments.Add(x2);
            x2.ExtrapolationType = ApproximationType.Constant;
            function.Components.Add(y);

            function[1.0,1.0] = 2.0;

            //get the value on a point that is defined in one dimension and not in the other.
            Assert.AreEqual(2.0, function.Evaluate<double>(new VariableValueFilter<double>(x1, 1.0), new VariableValueFilter<double>(x2, 2.0)));
            
        }

        [Test]
        public void InterpolatedConstant2D()
        {
            IVariable<double> x = new Variable<double>("x");
            IVariable<double> y = new Variable<double>("y");
            IVariable<double> f1 = new Variable<double>("f1");

            Function function = new Function("testfunction");
            function.Arguments.Add(x);
            function.Arguments.Add(y);
            function.Components.Add(f1);
            
            x.InterpolationType = ApproximationType.Constant;
            y.InterpolationType = ApproximationType.Constant;

            var xValues = new double[] { 1.0, 2.0, 3.0 };
            var yValues = new double[] { 10.0, 20.0, 30.0, 40.0 };
            var fValues = new[,]
                              {
                                  {100.0, 200.0, 300.0, 400.0},
                                  {1000.0, 2000.0, 3000.0, 4000.0},
                                  {10000.0, 20000.0, 30000.0, 40000.0}
                              };

            function.SetValues(fValues, new VariableValueFilter<double>(x, xValues), new VariableValueFilter<double>(y, yValues), new ComponentFilter(f1));

            //interpolation among first argument
            Assert.AreEqual(100.0, f1.Evaluate<double>(new VariableValueFilter<double>(x, 1.6), (new VariableValueFilter<double>(y, 10.0))));

            //interpolation among second argument
            Assert.AreEqual(200.0, f1.Evaluate<double>(new VariableValueFilter<double>(x, 1.0), (new VariableValueFilter<double>(y, 26.0))));

            //interpolation among two arguments
            Assert.AreEqual(200.0, f1.Evaluate<double>(new VariableValueFilter<double>(x, 1.6), (new VariableValueFilter<double>(y, 26.0))), 0.001);

        }


        
        [Test]
        public void InterpolatedLinear2D()
        {
            IVariable<double> x = new Variable<double>("x");
            IVariable<double> y = new Variable<double>("y");
            IVariable<double> f1 = new Variable<double>("f1");

            Function function = new Function("testfunction");
            function.Arguments.Add(x);
            function.Arguments.Add(y);
            function.Components.Add(f1);

            var xValues = new double[] { 1.0, 2.0, 3.0 };
            var yValues = new double[] { 10.0, 20.0, 30.0, 40.0 };
            var fValues = new[,]
                              {
                                  {100.0, 200.0, 300.0, 400.0},
                                  {1000.0, 2000.0, 3000.0, 4000.0},
                                  {10000.0, 20000.0, 30000.0, 40000.0}
                              };

            function.SetValues(fValues, new VariableValueFilter<double>(x, xValues), new VariableValueFilter<double>(y, yValues), new ComponentFilter(f1));

            //no interpolation..on a defined spot
            Assert.AreEqual(100, f1.Evaluate<double>(new VariableValueFilter<double>(x, 1.0), (new VariableValueFilter<double>(y, 10.0)))); ;

            //interpolation among first argument
            Assert.AreEqual(640.0, f1.Evaluate<double>(new VariableValueFilter<double>(x, 1.6), (new VariableValueFilter<double>(y, 10.0))), 0.001);

            //interpolation among second argument
            Assert.AreEqual(260.0, f1.Evaluate<double>(new VariableValueFilter<double>(x, 1.0), (new VariableValueFilter<double>(y, 26.0))), 0.001);

            //interpolation among two arguments
            Assert.AreEqual(1664.0, f1.Evaluate<double>(new VariableValueFilter<double>(x, 1.6), (new VariableValueFilter<double>(y, 26.0))), 0.001);

        }

        [Test]
        public void GetUniformInterpolatedValue()
        {
            IVariable<double> x = new Variable<double>("x");
            IVariable<double> y = new Variable<double>("y");
            IVariable<double> f1 = new Variable<double>("f1");

            f1.Arguments.Add(x);
            f1.Arguments.Add(y);
            
            var xValues = new[] { 1.0, 2.0 };
            var yValues = new[] { 1.0, 2.0 };

            var fValues = new[,]
                              {
                                  {100.0, 100.0},
                                  {100.0, 100.0}
                              };
            f1.SetValues(fValues,
                         new VariableValueFilter<double>(x, xValues),
                         new VariableValueFilter<double>(y, yValues));


            var value = f1.Evaluate<double>(
                new VariableValueFilter<double>(x, 1.5),
                new VariableValueFilter<double>(y, 1.5));
            Assert.AreEqual(100,value);
        }
        [Test]
        public void GetInterpolatedValues2ArgsFunctionDateTime()
        {
            IVariable<double> x = new Variable<double>("x");
            IVariable t = new Variable<DateTime>("t");
            IVariable<double> f1 = new Variable<double>("f1");

            IFunction function = new Function("testfunction");
            function.Arguments.Add(t);
            function.Arguments.Add(x);
            function.Components.Add(f1);

            var tValue = DateTime.Now;
            var tValues = new[] { tValue, tValue.AddSeconds(1), tValue.AddSeconds(2), tValue.AddSeconds(3) };

            var xValues = new[] { 1.0, 2.0, 3.0 };

            var fValues = new[,]
                              {
                                  {100.0, 200.0, 300.0},
                                  {1000.0, 2000.0, 3000.0},
                                  {10000.0, 20000.0, 30000.0},
                                  {100000.0, 200000.0, 300000.0}
                              };

            function.SetValues(fValues,
                               new VariableValueFilter<DateTime>(t, tValues),
                               new VariableValueFilter<double>(x, xValues), new ComponentFilter(f1));

            // now get interpolated value on 2d function
            var value = function.Evaluate<double>(
                new VariableValueFilter<double>(x, 2.5),
                new VariableValueFilter<DateTime>(t, tValue.AddSeconds(1.5)));

            Assert.AreEqual(13750, value);

            var value1 = function.Evaluate<double>(
                new VariableValueFilter<double>(x, 1.5),
                new VariableValueFilter<DateTime>(t, tValue.AddSeconds(2.5)));

            Assert.AreEqual(82500, value1);

            var value2 = function.Evaluate<double>(
                new VariableValueFilter<double>(x, 1.6),
                new VariableValueFilter<DateTime>(t, tValue.AddSeconds(2.5)));

            Assert.AreEqual(88000, value2);
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void GetExtrapolatedNoneThrowsException()
        {
            IVariable<double> x = new Variable<double>("x");
            IVariable<double> y = new Variable<double>("y");

            IFunction f1 = new Function();
            f1.Arguments.Add(x);
            f1.Components.Add(y);

            // set (fx, fy) values to (100.0, 200.0) for a combination of x and y values.
            f1.SetValues(
                new[] { 100.0, 200.0, 300.0 },
                new VariableValueFilter<double>(x, new[] { 1.0, 2.0, 3.0 }));
            IFunction f = f1;

            //No extrapolation
            x.ExtrapolationType = ApproximationType.None;

            //x0 < f.Arguments[0], extrapolation at begin is set to true
            var value = f.Evaluate<double>(new VariableValueFilter<double>(x, 0.5));
            Assert.AreEqual(f.Components[0].Values[0], value);
        }


        [Test]
        public void GetExtrapolatedValuesConstant1D()
        {
            IVariable<double> x = new Variable<double>("x");
            IVariable<double> y = new Variable<double>("y");

            IFunction function = new Function();
            function.Arguments.Add(x);
            function.Components.Add(y);

            // set (fx, fy) values to (100.0, 200.0) for a combination of x and y values.
            function.SetValues(
                new[] { 100.0, 200.0, 300.0 },
                new VariableValueFilter<double>(x, new[] { 1.0, 2.0, 3.0 }));
           

            //No extrapolation
            x.ExtrapolationType = ApproximationType.Constant;

            //x0 < f.Arguments[0], extrapolation at begin is set to true
            var value = function.Evaluate<double>(new VariableValueFilter<double>(x, 0.5));
            Assert.AreEqual(100, value);

            value = function.Evaluate<double>(new VariableValueFilter<double>(x, 3.5));
            Assert.AreEqual(300, value);

        }

        [Test]
        public void GetExtrapolatedValuesLinear1D()
        {
            IVariable<double> x = new Variable<double>("x");
            IVariable<double> y = new Variable<double>("y");

            IFunction function = new Function();
            function.Arguments.Add(x);
            function.Components.Add(y);

            // set (fx, fy) values to (100.0, 200.0) for a combination of x and y values.
            function.SetValues(
                new[] { 100.0, 200.0, 300.0 },
                new VariableValueFilter<double>(x, new[] { 1.0, 2.0, 3.0 }));

            //Extrapolate linear
            x.ExtrapolationType = ApproximationType.Linear;

            //before the 1st argument value
            Assert.AreEqual(50, function.Evaluate<double>(new VariableValueFilter<double>(x, 0.5)));

            //after the las
            Assert.AreEqual(350, function.Evaluate<double>(new VariableValueFilter<double>(x, 3.5)));
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void GetExceptionExtrapolatingValuesAtBeginAndEnd1ArgsFunction()
        {
            IVariable<double> x = new Variable<double>("x");
            IVariable<double> y = new Variable<double>("y");

            IFunction f1 = new Function();
            f1.Arguments.Add(x);
            f1.Components.Add(y);

            // set (fx, fy) values to (100.0, 200.0) for a combination of x and y values.
            f1.SetValues(
                new[] { 100.0, 200.0, 300.0 },
                new VariableValueFilter<double>(x, new[] { 1.0, 2.0, 3.0 }));
            IFunction f = f1;

            x.ExtrapolationType = ApproximationType.None;
            

            //x0 < f.Arguments[0], extrapolation at begin is set to false
            var value = f.Evaluate<double>(new VariableValueFilter<double>(x, 0.5));
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void GetExceptionExtrapolatingValues2ArgsFunction()
        {
            IVariable<double> x = new Variable<double>("x");
            IVariable<double> y = new Variable<double>("y");
            IVariable<double> f1 = new Variable<double>("f1");

            IFunction function = new Function("testfunction");
            function.Arguments.Add(x);
            function.Arguments.Add(y);
            function.Components.Add(f1);

            var xValues = new double[] {1.0, 2.0, 3.0};
            var yValues = new double[] {10.0, 20.0, 30.0, 40.0};
            var fValues = new[,]
                              {
                                  {100.0, 200.0, 300.0, 400.0},
                                  {1000.0, 2000.0, 3000.0, 4000.0},
                                  {10000.0, 20000.0, 30000.0, 40000.0}
                              };

            function.SetValues(fValues, new VariableValueFilter<double>(x, xValues), new VariableValueFilter<double>(y, yValues),
                               new ComponentFilter(f1));

            //no interpolation..on a defined spot
            Assert.AreEqual(100,
                            f1.Evaluate<double>(new VariableValueFilter<double>(x, 0.5),
                                                (new VariableValueFilter<double>(y, 10.0))));

            Assert.AreEqual(100,
                            f1.Evaluate<double>(new VariableValueFilter<double>(x, 1.5),
                                                (new VariableValueFilter<double>(y, 10.0))));

        }
    }
}