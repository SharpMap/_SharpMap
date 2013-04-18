using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace DelftTools.Functions.Tests
{
    [TestFixture]
    public class FunctionHelperTest
    {
        [Test]
        public void SplitEnumerable()
        {
            var source = new[] {1, 2.0};
            //split it into IEnumerable<int> and IEnumerable<double>
            IList<IEnumerable> enumerables= FunctionHelper.SplitEnumerable(source, new[] {typeof (int), typeof (double)});
            Assert.AreEqual(2,enumerables.Count);
            Assert.IsTrue(enumerables[0] is IEnumerable<int>);
            Assert.IsTrue(enumerables[1] is IEnumerable<double>);
            Assert.AreEqual(1, ((IEnumerable<int>)enumerables[0]).FirstOrDefault());
            Assert.AreEqual(2, ((IEnumerable<double>)enumerables[1]).FirstOrDefault());
        }
        

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void WrongNumberOfValuesGivesException()
        {
            var source = new[] { 1, 2.0 };
            //expecting 3 values getting only 2
            IList<IEnumerable> enumerables = FunctionHelper.SplitEnumerable(source, new[] { typeof(int), typeof(double),typeof(double) });
            
        }

        [Test]
        public void GetFirstValueBiggerThan()
        {
            var array = new [] { 1.0, 2.0 };
            Assert.IsNull(FunctionHelper.GetFirstValueBiggerThan(5.0, array));
            Assert.AreEqual(1.0,FunctionHelper.GetFirstValueBiggerThan(0.0, array));
            Assert.AreEqual(2.0, FunctionHelper.GetFirstValueBiggerThan(1.5, array));

            array = new[] { 1.0};
            Assert.IsNull(FunctionHelper.GetFirstValueBiggerThan(5.0, array));
            Assert.AreEqual(1.0, FunctionHelper.GetFirstValueBiggerThan(0.0, array));

        }


    }
}