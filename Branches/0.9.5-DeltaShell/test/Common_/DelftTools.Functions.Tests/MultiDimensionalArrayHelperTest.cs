using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DelftTools.Functions.Generic;
using DelftTools.Utils;
using DelftTools.Utils.Collections;
using NUnit.Framework;

namespace DelftTools.Functions.Tests
{
    [TestFixture]
    public class MultiDimensionalArrayHelperTest
    {
        [Test]
        public void StrideCalculation()
        {
            //Stride is a vector by which we multiply the indices of the MD-array to get the index in a single dimensional array
            int[] stride = MultiDimensionalArrayHelper.GetStride(new[] {1, 2});
            Assert.AreEqual(new[] { 2, 1 }, stride);

            //is this correct??? works out OK. Last dimension should always have stride 1.
            stride = MultiDimensionalArrayHelper.GetStride(new[] { 1, 1 });
            Assert.AreEqual(new[] { 1, 1 }, stride);

            //go from 2,1 (2 dimensional) to 4,3,2 (3 dimensional)
            stride =  MultiDimensionalArrayHelper.GetStride(new[] { 4, 3, 2 });
            Assert.AreEqual(new[] { 6, 2, 1 }, stride);

            stride = MultiDimensionalArrayHelper.GetStride(new[] { 2, 3, 4 });
            Assert.AreEqual(new[] { 12, 4, 1 }, stride);

            stride = MultiDimensionalArrayHelper.GetStride(new[] { 2, 3 });
            Assert.AreEqual(new[] { 3, 1 }, stride);

            //Notice stride does not change when the first dimension changes size
            stride = MultiDimensionalArrayHelper.GetStride(new[] { 3, 2 });
            Assert.AreEqual(new[] { 2, 1 }, stride);

            stride = MultiDimensionalArrayHelper.GetStride(new[] { 4, 2 });
            Assert.AreEqual(new[] { 2, 1 }, stride);
        }
        
        [Test]
        public void GetLength()
        {
            Assert.AreEqual(MultiDimensionalArrayHelper.GetTotalLength(new int[] {5,4,6 }),120 );
        }

        [Test]
        public void IsIndexWithinShapeIsFalseForAShapeContaining0()
        {
            Assert.IsFalse(MultiDimensionalArrayHelper.IsIndexWithinShape(new[] { 0 }, new[] { 0, 2, 2 }));
            Assert.IsFalse(MultiDimensionalArrayHelper.IsIndexWithinShape(new[] {0, 0}, new[] {2, 2, 0}));
        }

        [Test]
        public void TestGetInsertionIndex()
        {
            IMultiDimensionalArray<int> ints = new MultiDimensionalArray<int>{1, 2, 40, 50};
            Assert.AreEqual(2, MultiDimensionalArrayHelper.GetInsertionIndex(3, ints));
            Assert.AreEqual(0, MultiDimensionalArrayHelper.GetInsertionIndex(0, ints));
            Assert.AreEqual(4, MultiDimensionalArrayHelper.GetInsertionIndex(60, ints));
            Assert.AreEqual(1, MultiDimensionalArrayHelper.GetInsertionIndex(2, ints));
            Assert.AreEqual(0, MultiDimensionalArrayHelper.GetInsertionIndex(2, new MultiDimensionalArray<int>()));
        }

        [Test]
        public void InBetweenTest()
        {
            IMultiDimensionalArray<int> ints = new MultiDimensionalArray<int> { 1, 2, 40, 50 };
            int prev = ints[0];
            int next = ints[ints.Count - 1];
            Assert.IsTrue(Comparer.IsBetween(prev, 3, next));
            Assert.IsFalse(Comparer.IsBetween(prev, 0, next));
            Assert.IsFalse(Comparer.IsBetween(prev, 1, next));
            Assert.IsFalse(Comparer.IsBetween(prev, 50, next));
            Assert.IsTrue(Comparer.IsBetween(prev, 40, next));
        }

        [Test]
        public void DetectShapeFromString()
        {
            const string str = "{{1, 2.5, 3}, {2, 3, 4}}";

            var shape = MultiDimensionalArrayHelper.DetectShapeFromString(str);

            Assert.IsTrue(shape.SequenceEqual(new [] {2, 3}));
        }
    }
}