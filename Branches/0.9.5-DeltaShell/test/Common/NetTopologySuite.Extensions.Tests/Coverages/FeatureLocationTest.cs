using DelftTools.Functions;
using DelftTools.Functions.Generic;
using DelftTools.Functions.Filters;
using DelftTools.TestUtils;
using GeoAPI.Extensions.Coverages;
using NetTopologySuite.Extensions.Coverages;
using NetTopologySuite.Extensions.Tests.Features;
using NUnit.Framework;

namespace NetTopologySuite.Extensions.Tests.Coverages
{
    [TestFixture]
    public class FeatureLocationTest
    {
        [Test]
        public void Equality()
        {
            var simpleFeatureA = new SimpleFeature(10.0);
            var simpleFeatureB = new SimpleFeature(20.0);
            // two FeatureLocation with same feature and offset should be equal ( valuetype)
            Assert.AreEqual(new FeatureLocation { Feature = simpleFeatureA }, new FeatureLocation { Feature = simpleFeatureA });
            Assert.AreNotEqual(new FeatureLocation { Feature = simpleFeatureA }, new FeatureLocation { Feature = simpleFeatureB });
        }

        /// <summary>
        /// Tests if IFeatureLocation can be used as an argument in a function
        /// </summary>
        [Test]
        [Category(TestCategory.Integration)]
        public void TestAsArgument()
        {
            IVariable<IFeatureLocation> a = new Variable<IFeatureLocation>("argument");
            IVariable<double> c1 = new Variable<double>("value");
            IVariable<string> c2 = new Variable<string>("description");

            // f = (a, p)(h)
            IFunction f = new Function("rating curve");
            f.Arguments.Add(a);
            f.Components.Add(c1);
            f.Components.Add(c2);

            SimpleFeature simpleFeature = new SimpleFeature(10.0);
            IFeatureLocation featureLocation = new FeatureLocation { Feature = simpleFeature };

            // value based argument referencing.
            f[featureLocation] = new object[] { 1.0, "jemig de pemig" };

            IMultiDimensionalArray<double> c1Value = f.GetValues<double>(new ComponentFilter(f.Components[0]),
                                                                         new VariableValueFilter<IFeatureLocation>(
                                                                             f.Arguments[0],
                                                                             new FeatureLocation
                                                                                 {Feature = simpleFeature}));

            Assert.AreEqual(1.0, c1Value[0], 1.0e-6);

            //IMultiDimensionalArray<string> c2Value = f.GetValues<string>(new ComponentFilter(f.Components[1]),
            //                                                             new VariableValueFilter<IFeatureLocation>(
            //                                                                 f.Arguments[0], featureLocation));

            //Assert.AreEqual("jemig de pemig", c2Value[0]);
        }
    }
}
