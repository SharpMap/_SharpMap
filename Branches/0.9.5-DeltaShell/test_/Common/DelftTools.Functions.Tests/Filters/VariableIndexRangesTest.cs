using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DelftTools.Functions.Filters;
using DelftTools.Functions.Generic;
using DelftTools.Functions.Tuples;
using DelftTools.Utils;
using NUnit.Framework;

namespace DelftTools.Functions.Tests.Filters
{
    /// <summary>
    /// 
    /// </summary>
    [TestFixture]
    public class VariableIndexRangesTest
    {
        [Test]
        public void FilterIndependend()
        {
            IVariable<int> x = new Variable<int>();
            x.SetValues(new[]{1,2,3,4,5,6,7,8,9,10});
            //select two ranges ..from [1..3] and [7..9]
            IList<Pair<int, int>> indexRanges= new List<Pair<int, int>>();
            indexRanges.Add(new Pair<int, int>(0,2));
            indexRanges.Add(new Pair<int, int>(6,8));
            IVariable<int> filteredX = (IVariable<int>) x.Filter(new VariableIndexRangesFilter(x, indexRanges));
            Assert.AreEqual(6, filteredX.Values.Count);
            Assert.IsTrue(new[]{1,2,3,7,8,9}.SequenceEqual(filteredX.Values));
        }

        [Test]
        public void FilterDependend()
        {
            IVariable<int> y = new Variable<int>("y");
            IVariable<int> x = new Variable<int>("x");
            y.Arguments.Add(x);

            x.SetValues(new[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 10});
            //update y where x = 2
            Assert.IsFalse(y.Values.IsAutoSorted);
            y[2] = 10;
            Assert.AreEqual(new[] { 0, 10, 0, 0, 0, 0, 0, 0, 0, 0 }, y.Values);

            //update y where x = 8
            y[8] = 10;
            Assert.AreEqual(new[] { 0, 10, 0, 0, 0, 0, 0, 10, 0, 0 }, y.Values);
            
            //select two ranges ..from [1..3] and [7..9]
            IList<Pair<int, int>> indexRanges = new List<Pair<int, int>>();
            indexRanges.Add(new Pair<int, int>(0, 2));
            indexRanges.Add(new Pair<int, int>(6, 8));
            IVariable<int> filteredY = (IVariable<int>) y.Filter(new VariableIndexRangesFilter(x, indexRanges));
            Assert.AreEqual(6, filteredY.Values.Count);
            Assert.IsTrue(new[] {0, 10, 0, 0, 10, 0}.SequenceEqual(filteredY.Values));
        }
    }
}