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
    public class FilteredArrayInfoTest
    {
        [Test]
        public void SelectTwoDimensional()
        {
            IFunction f = new Function();
            var x = new Variable<int>("x");
            var y = new Variable<int>("x");
            f.Arguments.Add(x);
            f.Arguments.Add(y);
            //step through x 0,5,10,...100..selecting 21 values from 0 to 100 ranging 101 values
            var xFilter = new VariableAggregationFilter(x, 5, 0, 100);
            //step through y 10,12,14..20..selecting 6 values from 10 to 20 ranging 11
            var yFilter = new VariableAggregationFilter(y, 2, 10, 20);

            FilteredArrayInfo info = new FilteredArrayInfo(f, new[] {xFilter, yFilter});
            Assert.AreEqual(new[] { 0, 10 }, info.Origin);
            Assert.AreEqual(new[] { 21, 6 }, info.Shape);
            Assert.AreEqual(new[] { 101, 11 }, info.Size);
            Assert.AreEqual(new[] { 5, 2 }, info.Stride);
        }
        [Test]
        [Ignore("TODO")]
        public void AggregateOneDimension()
        {
            //two dimensional function aggregation on x variable
            Function f = new Function();
            var x = new Variable<int>("x");
            var y = new Variable<int>("x");
            f.Arguments.Add(x);
            f.Arguments.Add(y);

            //step through x 0,5,10,...100..selecting 21 values from 0 to 100 ranging 101 values
            var xFilter = new VariableAggregationFilter(x, 5, 0, 100);

            FilteredArrayInfo info = new FilteredArrayInfo(f, new[] { xFilter });
            Assert.AreEqual(new[] { 0, -1 }, info.Origin);
            Assert.AreEqual(new[] { 21, -1 }, info.Shape);
            Assert.AreEqual(new[] { 101, -1 }, info.Size);
            Assert.AreEqual(new[] { 5, -1 }, info.Stride);
        }
    }
}