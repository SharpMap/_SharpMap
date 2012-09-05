using System;
using System.Collections.Generic;
using DelftTools.Functions.Generic;
using NUnit.Framework;

namespace DelftTools.Functions.Tests.Conversion
{
    [TestFixture]
    public class ConvertedArrayTest
    {

        [Test]
        public void TestConvertedArray()
        {

            IMultiDimensionalArray<int> intArray = new MultiDimensionalArray<int>(new List<int> { 1, 2, 3, 4, 5 }, new[] { 5 });
            IMultiDimensionalArray<string> stringArray = new ConvertedArray<string, int>(intArray, Convert.ToInt32, Convert.ToString);


            Assert.AreEqual(intArray.Shape, stringArray.Shape);


            Assert.AreEqual("1", stringArray[0]);
            //assignment on the converted array are passed to the source
            stringArray.Add("30");
            Assert.AreEqual(30, intArray[5]);
            intArray.Add(31);
            Assert.AreEqual("31", stringArray[6]);
        }

        [Test]
        public void ToString()
        {
            var source = new MultiDimensionalArray<int>(new List<int> { 1, 2, 3, 4, 5, 6 }, new[] { 2, 3 });
            var target = new ConvertedArray<string, int>(source, Convert.ToInt32, Convert.ToString);
            
            Assert.AreEqual("{{1, 2, 3}, {4, 5, 6}}", target.ToString());
        }
    }
}