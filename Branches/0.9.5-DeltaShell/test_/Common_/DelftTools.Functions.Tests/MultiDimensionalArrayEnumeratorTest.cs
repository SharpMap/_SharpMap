using DelftTools.Functions.Generic;
using NUnit.Framework;

namespace DelftTools.Functions.Tests
{
    [TestFixture]
    public class MultiDimensionalArrayEnumeratorTest
    {
        [Test]
        public void EnumerateEmptyArray()
        {
            IMultiDimensionalArray<int> array = new MultiDimensionalArray<int>();
            foreach (int o in array)
            {
                Assert.Fail("No objects in the array. Should not come here");
            }
        }

        [Test]
        public void EnumerateRowMajor()
        {
            IMultiDimensionalArray<int> array = new MultiDimensionalArray<int>(2, 2);
            array[0, 0] = 0;
            array[0, 1] = 1;
            array[1, 0] = 2;
            array[1, 1] = 3;
            //verify we traverse in row major order
            int i = 0;
            foreach (int j in array)
            {
                Assert.AreEqual(j, i++);
            }
        }

        [Test]
        public void EnumerateArrayWithEmptyFirstDimension()
        {
            IMultiDimensionalArray<int> array = new MultiDimensionalArray<int>(0, 2);
            foreach (int o in array)
            {
                Assert.Fail("No objects in the array. Should not come here");
            }
        }

    }
}