using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DelftTools.Functions.Filters;
using DelftTools.Functions.Generic;
using NUnit.Framework;

namespace DelftTools.Functions.Tests.Filters
{
    [TestFixture]
    public class VariableIndexRangeFilterTest
    {
        [Test]
        public void WriteTwoDimensionalFunctionUsingIndexFilters()
        {
            //writing index based now only works for adding slices..
            var flow = new Variable<int>();
            var x = new Variable<int>();
            var y = new Variable<int>();
            flow.Arguments.Add(x);
            flow.Arguments.Add(y);

            x.AddValues(new[] {1, 2, 3});
            y.AddValues(new[] {10, 20, 30});

            //we now have 3x3 array for flow..write the last 'slice'
            var xIndex = new VariableIndexRangeFilter(x, 2);
            var yIndex = new VariableIndexRangeFilter(y, 0, 2);
            flow.SetValues(new[] {1, 2, 3}, new[] {xIndex, yIndex});

            Assert.AreEqual(9, flow.Values.Count);
            Assert.AreEqual(3, flow.Values[8]);
        }
    }
}
