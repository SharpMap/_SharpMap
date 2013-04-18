using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DelftTools.Utils.Reflection;
using NUnit.Framework;

namespace DelftTools.Tests.Utils.Reflection
{
    [TestFixture]
    public class TypeUtilsTest
    {
        private class TestClass
        {
            private int privateInt;
            public TestClass(int privateInt)
            {
                this.privateInt = privateInt;
            }
        }

        [Test]
        public void GetPrivateField()
        {
            TestClass testClass = new TestClass(22);
            Assert.AreEqual(22, TypeUtils.GetField(testClass,"privateInt"));
        }

        [Test]
        public void SetField()
        {
            var testClass = new TestClass(22);
            TypeUtils.SetField(testClass,"privateInt",23);
            Assert.AreEqual(23, TypeUtils.GetField(testClass, "privateInt"));
        }

        /// Dont'remove used by reflection test below
        private T ReturnValue<T>(T value)
        {
            return value;
        }
        
        /// Dont'remove used by reflection test below
        private void VoidMethod<T>(T value)
        {
            T copy = value;
        }

        [Test]
        public void CallGenericMethodUsingDynamicType()
        {
            int value = (int)TypeUtils.CallGenericMethod(GetType(),"ReturnValue",  typeof(int), this, 8);
            Assert.AreEqual(8,value);

            DateTime t = (DateTime)TypeUtils.CallGenericMethod(GetType(),"ReturnValue",typeof(DateTime),this, new DateTime(2000,1,1));
            Assert.AreEqual(new DateTime(2000, 1, 1), t);

            TypeUtils.CallGenericMethod(GetType(), "VoidMethod", typeof (int), this, 2);
        }

        [Test]
        public void CallStaticMethod()
        {
            IEnumerable values = Enumerable.Range(1,4);
            var b = Enumerable.Cast<int>(values);
            Assert.IsTrue(b is IEnumerable<int>);
            //same call dynamic :)
            var o = TypeUtils.CallStaticGenericMethod(typeof(Enumerable), "Cast", typeof(int), values);
            Assert.IsTrue(o is IEnumerable<int>);
        }

        [Test]
        public void GetTypedList()
        {
            Assert.IsTrue(TypeUtils.GetTypedList(typeof(int)) is List<int>);
            Assert.IsTrue(TypeUtils.GetTypedList(typeof(DateTime)) is List<DateTime>);
        }

        [Test]
        public void ConvertEnumerableToType()
        {
            IEnumerable values = Enumerable.Repeat(1.0, 10);
            Assert.IsTrue(TypeUtils.ConvertEnumerableToType(values, typeof (double)) is IEnumerable<double>);
        }

        [Test]
        public void TestGetFirstGenericType()
        {
            IList<int> listInt = new List<int>();
            Assert.AreEqual(typeof (int), TypeUtils.GetFirstGenericTypeParameter(listInt.GetType()));
            //do it on a non generic type and expect null
            Assert.IsNull(TypeUtils.GetFirstGenericTypeParameter(typeof (int)));
            
        }

        [Test]
        public void CreateGeneric()
        {
            Assert.IsTrue(TypeUtils.CreateGeneric(typeof (List<>), typeof (int)) is List<int>);
            
            
        }

        [Test]
        public void GetDefaultValue()
        {
            Assert.AreEqual(0,TypeUtils.GetDefaultValue(typeof(int)));
            Assert.AreEqual(null,TypeUtils.GetDefaultValue(typeof(List<int>)));
        }
    }
}