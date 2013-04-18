using System;
using System.Globalization;
using System.Threading;
using DelftTools.Utils;
using NUnit.Framework;

namespace DelftTools.Tests.Utils
{
    [TestFixture]
    public class ConversionHelperTest
    {
        private CultureInfo previousCulture;

        [SetUp]
        public void TestFixtureSetup()
        {
            previousCulture = Thread.CurrentThread.CurrentCulture;
        }

        [TearDown]
        public void TestFixtureTearDown()
        {
            Thread.CurrentThread.CurrentCulture = previousCulture;
        }

        [Test]
        public void ConvertCommasAsThousandsSeparator()
        {
            string s = "1,121";
            Assert.AreEqual(1121f, ConversionHelper.ToSingle(s)); 
        }

        [Test]
        public void ConvertPeriodAsDecimalSeparator()
        {
            string s = "1.12";
            Assert.AreEqual(1.12f, ConversionHelper.ToSingle(s));
        }

        [Test]
        public void ConvertPeriodInAWeirdCulture()
        {
            CultureInfo nlCulture = new CultureInfo("nl-NL");
            Thread.CurrentThread.CurrentCulture = nlCulture;
            string s = "1.123";

            //This is why conversionhelper exists.
            Assert.AreEqual(1123f, Convert.ToSingle(s));
            Assert.AreEqual(1.123f, ConversionHelper.ToSingle(s));
        }

    }
}