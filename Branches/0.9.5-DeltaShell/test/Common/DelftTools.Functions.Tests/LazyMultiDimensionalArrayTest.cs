using System;
using DelftTools.Functions.Generic;
using NUnit.Framework;

namespace DelftTools.Functions.Tests
{
    [TestFixture]
    public class LazyMultiDimensionalArrayTest
    {
        [Test]
        public void TestNotifyCollectionChanged()
        {
            var sourceArray = new MultiDimensionalArray<int>(2, 2);
            IMultiDimensionalArray<int> array = 
                new LazyMultiDimensionalArray<int>(()=>
                                                   sourceArray,()=>sourceArray.Count);

            bool called = false;
            EventHandler<MultiDimensionalArrayChangingEventArgs> arrayOnCollectionChanged = delegate { called = true; };
            array.CollectionChanged += arrayOnCollectionChanged;
            array[0, 0] = 0;
            array[0, 1] = 1;
            array[1, 0] = 2;
            array[1, 1] = 3;
            //called when values added
            Assert.IsTrue(called);

            //called when we replace a value
            called = false;
            array[0, 0] = 5;
            Assert.IsTrue(called);            

            //unsubscribe
            called = false;
            array.CollectionChanged -= arrayOnCollectionChanged;
            array[0, 0] = 3;
            Assert.IsFalse(called);            

        }
    }
}