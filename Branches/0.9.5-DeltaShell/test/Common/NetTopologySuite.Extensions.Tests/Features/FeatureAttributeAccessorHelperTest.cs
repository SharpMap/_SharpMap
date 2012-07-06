using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DelftTools.TestUtils;
using NetTopologySuite.Extensions.Features;
using NetTopologySuite.Extensions.Tests.TestObjects;
using NUnit.Framework;

namespace NetTopologySuite.Extensions.Tests.Features
{
    [TestFixture]
    public class FeatureAttributeAccessorHelperTest
    {
        [Test]
        public void GetAllAttributes()
        {
            var testFeature = new TestFeature();
            testFeature.Attributes = new DictionaryFeatureAttributeCollection(); 
            testFeature.Attributes.Add("attrib", "blah");

            var allAttributes = FeatureAttributeAccessorHelper.GetAttributeNames(testFeature);
            Assert.AreEqual(new []{"attrib","Name","Other"},allAttributes);
        }

        [Test]
        public void GetUndefinedAttributeSafe()
        {
            var testFeature = new TestFeature();
            testFeature.Attributes = new DictionaryFeatureAttributeCollection();
            var value = FeatureAttributeAccessorHelper.GetAttributeValue(testFeature, "unknown", false);

            Assert.IsNull(value);
        }

        [Test]
        [Category(TestCategory.Performance)]
        [Category(TestCategory.WorkInProgress)] // slow
        public void GetAttributeFast()
        {
            var testFeature = new TestFeature();
            testFeature.Attributes = new DictionaryFeatureAttributeCollection();

            object value;

            TestHelper.AssertIsFasterThan(100, () =>
                                                    {
                                                        for (int i = 0; i < 10000; i++)
                                                        {
                                                            value = FeatureAttributeAccessorHelper.GetAttributeValue(
                                                                testFeature, "Other", false);
                                                        }
                                                    });
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException), ExpectedMessage = "Specified argument was out of the range of valid values.\r\nParameter name: Cant find attribute name: unknown")]
        public void GetUndefinedAttributeUnsafe()
        {
            var testFeature = new TestFeature();
            testFeature.Attributes = new DictionaryFeatureAttributeCollection();
            var value = FeatureAttributeAccessorHelper.GetAttributeValue(testFeature, "unknown");

            Assert.IsNull(value);
        }

        [Test]
        public void GetAttributeNamesOfType()
        {
            List<string> actual = FeatureAttributeAccessorHelper.GetAttributeNames(typeof(TestFeature)).ToList();
            Assert.AreEqual(new[] { "Name", "Other" }, actual);

            List<string> actualInstance = FeatureAttributeAccessorHelper.GetAttributeNames(new TestFeature()).ToList();
            Assert.AreEqual(new[] { "Name", "Other" }, actualInstance);
        }

        [Test]
        public void GetAttributeDisplayName()
        {
            var displayName = FeatureAttributeAccessorHelper.GetAttributeDisplayName(typeof(TestFeature), "Other");
            Assert.AreEqual("Kees", displayName);

            var displayNameInstance = FeatureAttributeAccessorHelper.GetAttributeDisplayName(new TestFeature(), "Other");
            Assert.AreEqual("Kees", displayName);
        }

        [Test]
        public void GetAttributeDisplayNameInAttributeCollection()
        {
            var feature = new TestFeature();
            feature.Attributes = new DictionaryFeatureAttributeCollection();
            feature.Attributes["Jan"] = 3;

            var displayName = FeatureAttributeAccessorHelper.GetAttributeDisplayName(feature, "Jan");
            Assert.AreEqual("Jan", displayName);
        }

        [Test]
        [ExpectedException(ExceptionType = typeof(InvalidOperationException))]
        public void GetAttributeDisplayNameNonExistentProperty()
        {
            var feature = new TestFeature();
            feature.Attributes = new DictionaryFeatureAttributeCollection();
            feature.Attributes["Jan"] = 3;

            var displayName = FeatureAttributeAccessorHelper.GetAttributeDisplayName(feature, "Piet");
        }

        [Test]
        [ExpectedException(ExceptionType = typeof(InvalidOperationException))]
        public void GetAttributeDisplayNameThrowExceptionForNonExistingProperties()
        {
            var displayName = FeatureAttributeAccessorHelper.GetAttributeDisplayName(typeof(TestFeature), "Blabla");
        }

        [Test]
        public void GetAttributeNamesOfSubclass()
        {
            List<string> actual = FeatureAttributeAccessorHelper.GetAttributeNames(new TestFeatureSubClass()).ToList();
            Assert.AreEqual(new[] { "Name", "Other" }, actual);
        }
    }

    
}
