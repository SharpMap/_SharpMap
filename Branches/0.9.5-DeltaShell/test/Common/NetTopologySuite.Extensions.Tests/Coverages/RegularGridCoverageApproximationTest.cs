using System;
using DelftTools.Functions.Filters;
using DelftTools.Functions.Generic;
using NetTopologySuite.Extensions.Coverages;
using NUnit.Framework;

namespace NetTopologySuite.Extensions.Tests.Coverages
{
    [TestFixture]
    public class RegularGridCoverageApproximationTest
    {
        private static RegularGridCoverage GetCoverageWithDataIn2000And2002And2004()
        {
            var t2000 = new DateTime(2000, 1, 1);
            var t2002 = new DateTime(2002, 1, 1);
            var t2004 = new DateTime(2004, 1, 1);
            var coverage = new RegularGridCoverage { IsTimeDependent = true };
            var timeArgument = coverage.Time;
            coverage.Resize(2, 2, 1, 1);
            //set 0,0,0,0 in slice 2000
            coverage.SetValues(new double[] { 0, 0, 0, 0 }, new VariableValueFilter<DateTime>(timeArgument, t2000));
            //set 10,10,10,10 in 2002
            coverage.SetValues(new double[] { 10, 10, 10, 10 }, new VariableValueFilter<DateTime>(timeArgument, t2002));
            //set 100,100,100,100 in 2004
            coverage.SetValues(new double[] { 100, 100, 100, 100 }, new VariableValueFilter<DateTime>(timeArgument, t2004));

            return coverage;
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void NoneInterpolationResultsInException()
        {
            var coverage = GetCoverageWithDataIn2000And2002And2004();
            var timeArgument = coverage.Time;
            var intermediateYear = new DateTime(2001,1,1);

            //set interpolation for time argument to none
            timeArgument.InterpolationType = InterpolationType.None;
            //get values for intermediate year.
            coverage.GetValues<double>(new VariableValueFilter<DateTime>(timeArgument, intermediateYear));            
        }

        [Test]
        public void ConstantInterpolationInTime()
        {
            var coverage = GetCoverageWithDataIn2000And2002And2004();
            var timeArgument = coverage.Time;
            var t2001 = new DateTime(2001, 1, 1);
            var t2003 = new DateTime(2003, 1, 1);

            //set interpolation for time argument to none
            timeArgument.InterpolationType = InterpolationType.Constant;
            //get values for intermediate year.
            var values2001 = coverage.GetValues<double>(new VariableValueFilter<DateTime>(timeArgument, t2001));
            Assert.AreEqual(new[] { 0, 0, 0, 0 }, values2001);

            var values2003 = coverage.GetValues<double>(new VariableValueFilter<DateTime>(timeArgument, t2003));
            Assert.AreEqual(new[] { 10, 10, 10, 10 }, values2003);
        }

        [Test]
        public void ConstantExtrapolationAfterLastTimeArgument()
        {
            var coverage = GetCoverageWithDataIn2000And2002And2004();
            var timeArgument = coverage.Time;
            var t2005 = new DateTime(2005, 1, 1);

            //set interpolation for time argument to constant 
            timeArgument.ExtrapolationType = ExtrapolationType.Constant;
            //get values for year after the last year
            var values2005 = coverage.GetValues<double>(new VariableValueFilter<DateTime>(timeArgument, t2005));
            Assert.AreEqual(new[] { 100, 100, 100, 100 }, values2005);
        }


        [Test]
        public void ConstantExtrapolationBeforeFirstTimeArgument()
        {
            var coverage = GetCoverageWithDataIn2000And2002And2004();
            var timeArgument = coverage.Time;
            var t1999 = new DateTime(1999, 1, 1);

            //set interpolation for time argument to constant 
            timeArgument.ExtrapolationType = ExtrapolationType.Constant;
            //get values for year before first year 
            var values1999 = coverage.GetValues<double>(new VariableValueFilter<DateTime>(timeArgument, t1999));
            Assert.AreEqual(new[] { 0, 0, 0, 0 }, values1999);
        }        

    }
}
