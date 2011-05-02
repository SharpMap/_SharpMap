using System;
using DelftTools.Utils;
using NUnit.Framework;
using System.Collections.Generic;
using Rhino.Mocks;

namespace DelftTools.Tests.Utils
{
    [TestFixture]
    public class NamingHeperTest
    {
        private MockRepository mocks;

        [TestFixtureSetUp]
        public void FixtureSetup()
        {
            mocks = new MockRepository();
        }

        [Test, ExpectedException(typeof(ArgumentException))]
        public void GetUniqueNameWithInvalidFilter()
        {
            NamingHelper.GetUniqueName("invalidfilter", new INameable[] { }, null);
        }

        [Test]
        public void GetUniqueName()
        {
            var item1 = mocks.Stub<INameable>();
            var item2 = mocks.Stub<INameable>();
            var item3 = mocks.Stub<INameable>();

            item1.Name = "one (1)";
            item2.Name = "one";
            item3.Name = "INameable1";

            var namedItems = new List<INameable>(new[] { item1, item2, item3 });

            Assert.AreEqual("INameable2", NamingHelper.GetUniqueName(null, namedItems, typeof(INameable)));
            Assert.AreEqual("one (2)", NamingHelper.GetUniqueName("one ({0})", namedItems, typeof(INameable)));
        }


        [TestFixtureTearDown]
        public void FixtureTearDown()
        {
        }
    }
}	