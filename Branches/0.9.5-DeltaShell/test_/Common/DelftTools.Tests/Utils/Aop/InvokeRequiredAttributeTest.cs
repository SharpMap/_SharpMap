using System.Threading;
using DelftTools.TestUtils.TestClasses;
using NUnit.Framework;

namespace DelftTools.Tests.Utils.Aop
{
    [TestFixture]
    public class InvokeRequiredAttributeTest
    {
        [Test]
        public void InvokeMethodInASeparateThread()
        {
            var testClass = new InvokeRequiredTestClass();

            // call directly 
            testClass.SynchronizedMethod();

            // call from another thread
            var thread = new Thread(testClass.SynchronizedMethod);
            thread.Start();
            thread.Join();

            Assert.AreEqual(1, testClass.CallsUsingInvokeCount);
            Assert.AreEqual(1, testClass.CallsWithoutInvokeCount);
        }
    }
}