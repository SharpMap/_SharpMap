using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DelftTools.Functions.Generic;
using DelftTools.TestUtils;
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
        [Category(TestCategory.WorkInProgress)] // slow
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

        [Test]
        [Category(TestCategory.Performance)]
        public void GetFirstValueBiggerThanShouldBeFast()
        {
            int amount = 1000000;
            List<double> values = new List<double>(amount);
            for (int i = 0; i < amount; i++)
                values.Add(i);

            Action action = () =>
                                 {
                                     for (int i = 0; i < 100000; i++)
                                     {
                                         FunctionHelper.GetFirstValueBiggerThan(55555.0, values);
                                     }
                                 };

            // avoid overhead, call one time
            action();

            TestHelper.AssertIsFasterThan(180, action);
        }

        [Test]
        [Category(TestCategory.WorkInProgress)] // slow
        public void GetLastValueSmallerThan()
        {
            var array = new MultiDimensionalArray { 1.0, 2.0 };
            Assert.IsNull(FunctionHelper.GetLastValueSmallerThan(1.0, array));
            Assert.AreEqual(1.0, FunctionHelper.GetLastValueSmallerThan(1.5, array));
            Assert.AreEqual(2.0, FunctionHelper.GetLastValueSmallerThan(2.5, array));

            array = new MultiDimensionalArray { 1.0 };
            Assert.IsNull(FunctionHelper.GetLastValueSmallerThan(1.0, array));
            Assert.AreEqual(1.0, FunctionHelper.GetLastValueSmallerThan(55.0, array));

        }

        [Test]
        [Category(TestCategory.Performance)]
        public void GetLastValueSmallerThanShouldBeFast()
        {
            int amount = 1000000;
            List<double> values = new List<double>(amount);
            for (int i = 0; i < amount; i++)
                values.Add(i);

            var array = new MultiDimensionalArray();
            array.AddRange(values);

            TestHelper.AssertIsFasterThan(400, () =>
            {
                for (int i = 0; i < 100000; i++)
                    FunctionHelper.GetLastValueSmallerThan(55555.0, array);
            });
            //orig: 2100ms
        }

        [Test]
        public void CopyValuesFrom1Component()
        {
            Function source = new Function();

            source.Arguments.Add(new Variable<DateTime>("time"));
            source.Components.Add(new Variable<double>("component"));
            source[DateTime.Now] = 27.7;

            Function target = new Function();

            target.Arguments.Add(new Variable<DateTime>("time"));
            target.Components.Add(new Variable<double>("component"));

            FunctionHelper.CopyValuesFrom(source, target);

            Assert.AreEqual(1, target.Arguments[0].Values.Count);
            Assert.AreEqual(27.7, (double)target.Components[0].Values[0], 1.0e-6);
        }

        [Test]
        public void CopyValuesFrom1ComponentCopyShouldClear()
        {
            Function source = new Function();

            source.Arguments.Add(new Variable<double>("x"));
            source.Components.Add(new Variable<double>("component"));
            source[1.0] = 27.7;

            Function target = new Function();

            target.Arguments.Add(new Variable<double>("x"));
            target.Components.Add(new Variable<double>("component"));
            target[0.0] = 10.0;
            target[1.0] = 100.7;

            FunctionHelper.CopyValuesFrom(source, target);

            Assert.AreEqual(1, target.Arguments[0].Values.Count);
            Assert.AreEqual(1.0, (double)target.Arguments[0].Values[0], 1.0e-6);
            Assert.AreEqual(27.7, (double)target.Components[0].Values[0], 1.0e-6);
        }

        [Test]
        public void CopyValuesFrom2Components()
        {
            Function source = new Function();

            source.Arguments.Add(new Variable<double>("x"));
            source.Components.Add(new Variable<double>("component1"));
            source.Components.Add(new Variable<double>("component2"));
            source[5.0] = new[] { 27.7, 127.7 };

            Function target = new Function();

            target.Arguments.Add(new Variable<double>("x"));
            target.Components.Add(new Variable<double>("component1"));
            target.Components.Add(new Variable<double>("component2"));

            FunctionHelper.CopyValuesFrom(source, target);

            Assert.AreEqual(1, target.Arguments[0].Values.Count);
            Assert.AreEqual(27.7, (double)target.Components[0].Values[0], 1.0e-6);
            Assert.AreEqual(127.7, (double)target.Components[1].Values[0], 1.0e-6);
        }
    }
}