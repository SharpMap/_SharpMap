using DelftTools.Functions.Generic;
using NUnit.Framework;

namespace DelftTools.Functions.Tests
{
    [TestFixture]
    public class ArrayExtensionsTest
    {
        [Test]
        public void ToMultiDimensionalArray()
        {
            var array = new[,] {{1, 2, 3}, {4, 5, 6}};

            var result = array.ToMultiDimensionalArray();

            Assert.AreEqual(2, result.Rank);
            Assert.AreEqual(new[] { 2, 3 }, result.Shape);
        }
    }
}