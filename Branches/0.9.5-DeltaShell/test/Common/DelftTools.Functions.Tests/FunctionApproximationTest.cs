using System;
using System.Linq;
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
            var f= FunctionHelper.Get1DFunction<double, double>();
            
            f.Arguments[0].ExtrapolationType = ExtrapolationType.Constant; 

            f.Components[0].DefaultValue = 10.0;

            Assert.AreEqual(10.0, f.Evaluate1D<double, double>(1.0));
        }

        [Test]
        public void GetInterpolatedValueReturnsDefaultValueOnEmpty2ArgsFunction()
        {
            IVariable<double> x = new Variable<double>("x");
            IVariable<double> y = new Variable<double>("y");
            x.ExtrapolationType = ExtrapolationType.Constant;
            y.ExtrapolationType = ExtrapolationType.Constant;
            
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
            var function = FunctionHelper.Get1DFunction<double, double>();
            
            var xValues = new[] { 1.0, 2.0, 3.0 };
            var yValues = new[] { 100.0, 200.0, 300.0 };

            function.SetComponentArgumentValues(yValues, xValues);

            function.Arguments[0].InterpolationType = InterpolationType.Constant;

            var value = function.Evaluate1D<double, double>(1.5);
            Assert.AreEqual(100.0, value);

            value = function.Evaluate1D<double, double>(2.5);
            Assert.AreEqual(200.0, value);

        }
        
        [Test]
        public void InterpolateLinear1D()
        {
            IVariable<double> x = new Variable<double>("x");
            IVariable<double> y = new Variable<double>("y");

            var function = new Function();
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
            x2.ExtrapolationType = ExtrapolationType.Constant;
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

            var function = new Function("testfunction");
            function.Arguments.Add(x);
            function.Arguments.Add(y);
            function.Components.Add(f1);
            
            x.InterpolationType = InterpolationType.Constant;
            y.InterpolationType = InterpolationType.Constant;

            var xValues = new[] { 1.0, 2.0, 3.0 };
            var yValues = new[] { 10.0, 20.0, 30.0, 40.0 };
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

            var function = new Function("testfunction");
            function.Arguments.Add(x);
            function.Arguments.Add(y);
            function.Components.Add(f1);

            var xValues = new[] { 1.0, 2.0, 3.0 };
            var yValues = new[] { 10.0, 20.0, 30.0, 40.0 };
            var fValues = new[,]
                              {
                                  {100.0, 200.0, 300.0, 400.0},
                                  {1000.0, 2000.0, 3000.0, 4000.0},
                                  {10000.0, 20000.0, 30000.0, 40000.0}
                              };

            function.SetValues(fValues, new VariableValueFilter<double>(x, xValues), new VariableValueFilter<double>(y, yValues), new ComponentFilter(f1));

            //no interpolation..on a defined spot
            Assert.AreEqual(100, f1.Evaluate<double>(new VariableValueFilter<double>(x, 1.0), (new VariableValueFilter<double>(y, 10.0))));

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
            x.ExtrapolationType = ExtrapolationType.None;

            //x0 < f.Arguments[0], extrapolation at begin is set to true
            var value = f.Evaluate<double>(new VariableValueFilter<double>(x, 0.5));
            Assert.AreEqual(f.Components[0].Values[0], value);
        }

        [Test]
        public void GetExtrapolatedValuesConstant1D()
        {
            var function = FunctionHelper.Get1DFunction<double, double>();

            // set (fx, fy) values to (100.0, 200.0) for a combination of x and y values.
            function.SetComponentArgumentValues(
                new[] { 100.0, 200.0, 300.0 },
                new[] { 1.0, 2.0, 3.0 });
           

            //No extrapolation
            function.Arguments[0].ExtrapolationType = ExtrapolationType.Constant;

            //x0 < f.Arguments[0], extrapolation at begin is set to true
            var value = function.Evaluate1D<double,double>(0.5);
            Assert.AreEqual(100, value);

            value = function.Evaluate1D<double, double>(3.5);
            Assert.AreEqual(300, value);

        }

        [Test]
        public void GetExtrapolatedValuesLinear1D()
        {
            var function = FunctionHelper.Get1DFunction<double, double>();
            
            // set (fx, fy) values to (100.0, 200.0) for a combination of x and y values.
            function.SetComponentArgumentValues(
                new[] {100.0, 200.0, 300.0},
                new[] {1.0, 2.0, 3.0});

            //Extrapolate linear
            function.Arguments[0].ExtrapolationType = ExtrapolationType.Linear;

            //before the 1st argument value
            Assert.AreEqual(50, function.Evaluate1D<double, double>(0.5));

            //after the last
            Assert.AreEqual(350, function.Evaluate1D<double,double>(3.5));
        }

        [Test]
        public void GetExtrapolatedValuesLineard1DDateTime()
        {
            var function = FunctionHelper.Get1DFunction<DateTime, double>();

            // set (fx, fy) values to (100.0, 200.0) for a combination of x and y values.
            function.SetComponentArgumentValues(
                new[] { 100.0, 200.0, 300.0 },
                new[] { new DateTime(2010, 1, 1), new DateTime(2011, 1, 1), new DateTime(2012, 1, 1) });

            //Extrapolate linear
            function.Arguments[0].ExtrapolationType = ExtrapolationType.Linear;

            //before the 1st argument value
            Assert.AreEqual(45.205479, function.Evaluate1D<DateTime, double>(new DateTime(2009, 6, 15)), 1e-6);

            //after the last
            Assert.AreEqual(345.479452, function.Evaluate1D<DateTime, double>(new DateTime(2012, 6, 15)), 1e-6);
        }

        [Test]
        public void GetExtrapolatedValuesLinearOnFunctionWithOneValueDoesNotThrow()
        {
            var function = FunctionHelper.Get1DFunction<DateTime, double>();
            
            //Extrapolate linear
            function.Arguments[0].ExtrapolationType = ExtrapolationType.Linear;

            function.SetComponentArgumentValues(
                new[] {100.0},
                new[] {new DateTime(2010, 1, 1)});

            Assert.AreEqual(100, function.Evaluate1D<DateTime, double>(new DateTime(2009, 6, 15)));
        }

        [Test]
        public void GetExtrapolatedValuesPeriodicStartAtZero()
        {
            var function = FunctionHelper.Get1DFunction<double, double>();

            function.SetComponentArgumentValues(new[]{0.0,1.0,2.0}, new[]{1.0,2.0,3.0});
            
            //Extrapolate periodic
            IVariable x = function.Arguments[0];
            x.ExtrapolationType = ExtrapolationType.Periodic;

            //Period in 2 so F(3) - F(1) == 2
            Assert.AreEqual(2, function.Evaluate<double>(new VariableValueFilter<double>(x, 3.0)));
            
        }

        [Test]
        public void GetExtrapolatedValuesPeriodicStartAtNonZero()
        {
            var function = FunctionHelper.Get1DFunction<double, double>();

            function.SetComponentArgumentValues(new[] { 0.0, 1.0, 2.0 }, new[] { 4.0, 5.0, 6.0 });

            //Extrapolate periodic
            IVariable x = function.Arguments[0];
            x.ExtrapolationType = ExtrapolationType.Periodic;

            //Period in 2 so F(7) = F(5)
            Assert.AreEqual(1.0d, function.Evaluate<double>(new VariableValueFilter<double>(x, 7.0)));
        }

        [Test]
        public void GetExtrapolatedValuesPeriodicDateTime()
        {
            var function = FunctionHelper.Get1DFunction<DateTime, double>();
            //3 days
            var times = new[]{1,2,3}.Select(i => new DateTime(2000, 1, i));
            function.SetComponentArgumentValues(new[] { 0.0, 1.0, 2.0 }, times);

            //Extrapolate periodic
            IVariable x = function.Arguments[0];
            x.ExtrapolationType = ExtrapolationType.Periodic;

            //Day number 4 should equal number 1
            var dayFour = new DateTime(2000, 1, 4);
            Assert.AreEqual(1.0d, function.Evaluate<double>(new VariableValueFilter<DateTime>(x, dayFour)));
        }

        [Test]
        public void GetExtrapolatedValuesPeriodicDateTime2()
        {
            var function = FunctionHelper.Get1DFunction<DateTime, double>();
            //3 days
            var times = new[] { 1, 2, 3 }.Select(i => new DateTime(2000, 1, i));
            function.SetComponentArgumentValues(new[] { 0.0, 1.0, 2.0 }, times);

            //Extrapolate periodic
            IVariable x = function.Arguments[0];
            x.InterpolationType = InterpolationType.Linear;
            x.ExtrapolationType = ExtrapolationType.Periodic;

            //Day number 3 should equal number 2
            var dayThree = new DateTime(2000, 1, 3);
            Assert.AreEqual(2.0d, function.Evaluate<double>(new VariableValueFilter<DateTime>(x, dayThree)));
        }

        
        [Test]
        public void GetExtrapolatedValuesPeriodicDateTimeJustAfterTheFirstPeriod()
        {
            var function = FunctionHelper.Get1DFunction<DateTime, double>();
            //3 days
            var times = new[] { 1, 2, 3 }.Select(i => new DateTime(2000, 1, i));
            function.SetComponentArgumentValues(new[] { 0.0, 1.0, 2.0 }, times);

            //Extrapolate periodic
            IVariable x = function.Arguments[0];
            x.InterpolationType = InterpolationType.Linear;
            x.ExtrapolationType = ExtrapolationType.Periodic;

            //Day number 3 and one second should be close to 0.0
            var dayThreeAndOneSecond = new DateTime(2000, 1, 3, 0, 0, 1);
            Assert.Less(function.Evaluate<double>(new VariableValueFilter<DateTime>(x, dayThreeAndOneSecond)), 0.1d);
        }

        [Test]
        public void GetInterpolatedValueBetweenTwoPeriodsOfDateTimeExtrapolatedValuesPeriodic()
        {
            var function = FunctionHelper.Get1DFunction<DateTime, double>();
            //3 days
            var times = new[] { 1, 2, 3 }.Select(i => new DateTime(2000, 1, i));
            function.SetComponentArgumentValues(new[] { 5.0, 3.0, 1.0 }, times);

            //Extrapolate periodic
            IVariable x = function.Arguments[0];
            x.InterpolationType = InterpolationType.Linear;
            x.ExtrapolationType = ExtrapolationType.Periodic;

            //Day number 3.5 should equal number 4 (linear interpolation 5 and 3)
            var dayThreeAndAHalf = new DateTime(2000, 1, 3, 12, 0, 0, 0);
            Assert.AreEqual(4.0d, function.Evaluate<double>(new VariableValueFilter<DateTime>(x, dayThreeAndAHalf)));
        }

        [Test]
        public void GetExtrapolatedValuesPeriodicDateNotRegulair()
        {
            var function = FunctionHelper.Get1DFunction<DateTime, double>();
            //3 days
            var times = new[] { 1, 2, 3,30 }.Select(i => new DateTime(2000, 1, i));
            function.SetComponentArgumentValues(new[] { 0.0, 1.0, 2.0, 2.0 }, times);

            //Extrapolate periodic
            IVariable x = function.Arguments[0];
            x.InterpolationType = InterpolationType.Linear;
            x.ExtrapolationType = ExtrapolationType.Periodic;

            //Day number 10 of February
            var dayTenOfFebruary = new DateTime(2000, 2, 10);
            Assert.AreEqual(2.0d, function.Evaluate<double>(new VariableValueFilter<DateTime>(x, dayTenOfFebruary)));
        }



        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void GetExceptionExtrapolatingValuesAtBeginAndEnd1ArgsFunction()
        {
            var f1 = FunctionHelper.Get1DFunction<double, double>();
            
            // set (fx, fy) values to (100.0, 200.0) for a combination of x and y values.
            f1.SetComponentArgumentValues(
                new[] { 100.0, 200.0, 300.0 }, new[] { 1.0, 2.0, 3.0 });
            

            f1.Arguments[0].ExtrapolationType = ExtrapolationType.None;
            

            //x0 < f.Arguments[0], extrapolation at begin is set to false
            double value = f1.Evaluate1D<double,double>(0.5d);
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

            var xValues = new[] {1.0, 2.0, 3.0};
            var yValues = new[] {10.0, 20.0, 30.0, 40.0};
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