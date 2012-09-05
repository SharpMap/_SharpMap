using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DelftTools.Functions.Filters;
using DelftTools.Functions.Generic;
using NUnit.Framework;

namespace DelftTools.Functions.Tests.Filters
{
    /// <summary>
    /// 
    /// </summary>
    [TestFixture]
    public class VariableAggregationFilterTest
    {
        [Test]
        public void SampleSize()
        {
            IVariable<int> x = new Variable<int>();

            int stepSize = 1;
            int startIndex = 0;
            int endIndex = 0;
            var filter = new VariableAggregationFilter(x, stepSize, startIndex, endIndex);
            Assert.AreEqual(1, filter.Count);

            stepSize = 2;
            startIndex = 0;
            endIndex = 3;
            filter = new VariableAggregationFilter(x, stepSize, startIndex, endIndex);
            Assert.AreEqual(2, filter.Count);


            stepSize = 2;
            startIndex = 0;
            endIndex = 4;
            filter = new VariableAggregationFilter(x, stepSize, startIndex, endIndex);
            Assert.AreEqual(3, filter.Count);
        }
    }
}