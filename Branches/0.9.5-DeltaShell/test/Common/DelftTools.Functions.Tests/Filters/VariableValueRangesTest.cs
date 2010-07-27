using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DelftTools.Functions.Filters;
using DelftTools.Functions.Generic;
using DelftTools.Functions.Tuples;
using NUnit.Framework;

namespace DelftTools.Functions.Tests.Filters
{
    /// <summary>
    /// 
    /// </summary>
    [TestFixture]
    public class VariableValueRangesTest
    {
        [Test]
        public void VariableValueRanges()
        {
            IVariable<int> x = new Variable<int>();
            x.SetValues(new[]{1,2,3,4,5,6,7,8,9,10});
            //select two ranges ..from [1..3] and [7.8]
            IList<Pair<int,int>> ranges = new List<Pair<int, int>>();
            ranges.Add(new Pair<int, int>(1,3));
            ranges.Add(new Pair<int, int>(7,8));
            x = (IVariable<int>)x.Filter(new VariableValueRangesFilter<int>(x,ranges));
            Assert.AreEqual(5, x.Values.Count);
            Assert.AreEqual(new[] { 1, 2, 3, 7, 8}, x.Values.ToArray());
        }
    }
}