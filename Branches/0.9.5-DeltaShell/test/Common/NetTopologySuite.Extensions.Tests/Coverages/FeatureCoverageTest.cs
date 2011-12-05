using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using DelftTools.Functions;
using DelftTools.Functions.Filters;
using DelftTools.Functions.Generic;
using DelftTools.TestUtils;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Geometries;
using NetTopologySuite.Extensions.Coverages;
using NetTopologySuite.Extensions.Tests.Features;
using NUnit.Framework;
using SharpMap.Data;
using SharpMap.Data.Providers;
using GisSharpBlog.NetTopologySuite.Geometries;

namespace NetTopologySuite.Extensions.Tests.Coverages
{
    [TestFixture]
    public class FeatureCoverageTest
    {
        private string sharpMapGisShapeFilePath = TestHelper.GetTestDataPath(@"DeltaShell\DeltaShell.Plugins.SharpMapGis.Tests\");
                 
        [Test]
        public void CreateAndSetValues()
        {
            IList<SimpleFeature> features = new List<SimpleFeature>
                                                {
                                                    new SimpleFeature(0, new Point(0, 0)),
                                                    new SimpleFeature(1, new Point(1, 1)),
                                                    new SimpleFeature(2, new Point(2, 2))
                                                };

            FeatureCoverage coverage = new FeatureCoverage();
            coverage.Components.Add(new Variable<double>("value"));
            coverage.Arguments.Add(new Variable<SimpleFeature>("feature"));
            coverage.Features = (IList) features;

            var feature = features[1];
            IList<double> values = coverage.GetValues<double>(new VariableValueFilter<SimpleFeature>(coverage.FeatureVariable, feature));
            Assert.AreEqual(1, values.Count);
            Assert.AreEqual(coverage.Components[0].DefaultValue, values[0]);

            IList<double> allValues = coverage.GetValues<double>();
            Assert.AreEqual(3, allValues.Count);

            double[] valuesArray = new double[3] { 1.0, 2.0, 3.0 };
            coverage.SetValues(valuesArray);

            values = coverage.GetValues<double>();
            
            Assert.AreEqual(3, values.Count);
            Assert.AreEqual(1.0, values[0]);
            Assert.AreEqual(2.0, values[1]);
            Assert.AreEqual(3.0, values[2]);
        }

        [Test]
        public void ClearValuesWhenFeaturesAreSet()
        {
            IList<SimpleFeature> features = new List<SimpleFeature>();

            features.Add(new SimpleFeature(0, new Point(0, 0)));
            features.Add(new SimpleFeature(1, new Point(1, 1)));
            features.Add(new SimpleFeature(2, new Point(2, 2)));

            FeatureCoverage coverage = new FeatureCoverage();
            coverage.Components.Add(new Variable<double>("value"));
            coverage.Arguments.Add(new Variable<SimpleFeature>("feature"));
            
            coverage.Features = (IList) features;

            double[] values = new double[] { 1.0, 2.0, 3.0 };
            coverage.SetValues(values);

            Assert.AreEqual(3, coverage.GetValues<double>().Count);

            // set features second time
            coverage.Features = (IList) features;

            Assert.AreEqual(3, coverage.GetValues<double>().Count);
        }

        [Test]
        public void ValuesOfFeatureArgumentShouldworkAfterClear()
        {
            List<SimpleFeature> features = new List<SimpleFeature>();

            features.Add(new SimpleFeature(0, new Point(0, 0)));
            features.Add(new SimpleFeature(1, new Point(1, 1)));
            features.Add(new SimpleFeature(2, new Point(2, 2)));

            FeatureCoverage coverage = new FeatureCoverage();
            coverage.Components.Add(new Variable<double>("value"));
            coverage.Arguments.Add(new Variable<SimpleFeature>("feature"));

            coverage.Features = (IList)features;

            double[] values = new double[] { 1.0, 2.0, 3.0 };
            coverage.SetValues(values);

            Assert.AreEqual(3, coverage.GetValues<double>().Count);
            // clear all
            coverage.Clear(); 

            coverage.SetValues(values, new VariableValueFilter<SimpleFeature>(coverage.FeatureVariable, features.ToArray()));

            Assert.AreEqual(3, coverage.GetValues<double>().Count);            
        }

        [Test]
        public void ValuesOfFeatureArgumentInTimeDependentFunctionAfterClear()
        {
            // define features
            var features = new List<SimpleFeature>
                                               {
                                                   new SimpleFeature(0, new Point(0, 0)),
                                                   new SimpleFeature(1, new Point(1, 1)),
                                                   new SimpleFeature(2, new Point(2, 2))
                                               };

            // create coverage
            var coverage = new FeatureCoverage();
            
            // define coverage components (attributes?)
            coverage.Components.Add(new Variable<double>("value"));
            
            // define coverage arguments (dimensionality of coverage attributes?)
            coverage.Arguments.Add(new Variable<SimpleFeature>("feature")); // 1st dimension is feature

            IVariable timeVariable = new Variable<DateTime>("time"); // 2nd dimension is time
            coverage.Arguments.Add(timeVariable);

            // set feature values
            coverage.Features = features;

            // set coverage values
            var values = new[] { 1.0, 2.0, 3.0 };

            var time1 = DateTime.Now;
            coverage.SetValues(values, new VariableValueFilter<DateTime>(timeVariable, time1));
            coverage.SetValues(values, new VariableValueFilter<DateTime>(timeVariable, time1.AddDays(1)));

            // asserts
            Assert.AreEqual(6, coverage.GetValues<double>().Count);
        }

        [Test]
        public void CreateAndSetValuesWithTimeAsVariable()
        {
            IList<SimpleFeature> features = new List<SimpleFeature>();

            features.Add(new SimpleFeature(0, new Point(0, 0)));
            features.Add(new SimpleFeature(1, new Point(1, 1)));
            features.Add(new SimpleFeature(2, new Point(2, 2)));

            FeatureCoverage coverage = new FeatureCoverage();
            coverage.Components.Add(new Variable<double>("value"));
            IVariable timeVariable = new Variable<DateTime>("time");
            coverage.Arguments.Add(timeVariable);
            coverage.Arguments.Add(new Variable<SimpleFeature>("feature"));
            coverage.Features = (IList) features;

            DateTime time1 = DateTime.Now;
            double[] values1 = new double[] { 1.0, 2.0, 3.0 };
            coverage.SetValues(values1, new VariableValueFilter<DateTime>(timeVariable, time1));

            DateTime time2 = time1.AddDays(1);
            double[] values2 = new double[] { 10.0, 20.0, 30.0 };
            coverage.SetValues(values2, new VariableValueFilter<DateTime>(timeVariable, time2));

            // t = t1
            IList<double> values = coverage.GetValues<double>(new VariableValueFilter<DateTime>(timeVariable, time1));

            Assert.AreEqual(3, values.Count);
            Assert.AreEqual(1.0, values[0]);
            Assert.AreEqual(2.0, values[1]);
            Assert.AreEqual(3.0, values[2]);

            // t = t2
            values = coverage.GetValues<double>(new VariableValueFilter<DateTime>(timeVariable, time2));

            Assert.AreEqual(3, values.Count);
           
            Assert.AreEqual(10.0, values[0]);
            Assert.AreEqual(20.0, values[1]);
            Assert.AreEqual(30.0, values[2]);
        }


        [Test]
        [Category("Integration")]
        public void UseShapefileFeaturesAdFeatureCoverageSource()
        {
            string path =
                Path.Combine(sharpMapGisShapeFilePath,"rivers.shp");
            ShapeFile shapeFile = new ShapeFile(path);
            
            // select only some features from shapefile
            IEnvelope coverageFeatureEnvelope = shapeFile.GetExtents();
            coverageFeatureEnvelope.ExpandBy(0.5, 0.5);

            FeatureCoverage coverage = new FeatureCoverage();
            coverage.Components.Add(new Variable<double>("value"));
            coverage.Arguments.Add(new Variable<FeatureDataRow>("feature"));
            coverage.Features = shapeFile.GetFeatures(coverageFeatureEnvelope);

            double[] values = new double[coverage.FeatureVariable.Values.Count];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = i;
            }

            coverage.SetValues(values);
        }

        [Test]
        public void FilterCoverage()
        {
            IList<SimpleFeature> features = new List<SimpleFeature>();

            features.Add(new SimpleFeature(0, new Point(0, 0)));
            features.Add(new SimpleFeature(1, new Point(1, 1)));
            features.Add(new SimpleFeature(2, new Point(2, 2)));

            FeatureCoverage coverage = new FeatureCoverage();
            coverage.Components.Add(new Variable<double>("value"));
            IVariable timeVariable = new Variable<DateTime>("time");
            coverage.Arguments.Add(timeVariable);
            coverage.Arguments.Add(new Variable<SimpleFeature>("feature"));
            coverage.Features = (IList) features;

            DateTime time1 = DateTime.Now;
            double[] values1 = new double[] { 1.0, 2.0, 3.0 };
            coverage.SetValues(values1, new VariableValueFilter<DateTime>(timeVariable, time1));

            DateTime time2 = time1.AddDays(1);
            double[] values2 = new double[] { 10.0, 20.0, 30.0 };
            coverage.SetValues(values2, new VariableValueFilter<DateTime>(timeVariable, time2));

            Assert.AreEqual(6,coverage.Components[0].Values.Count);
            //filter the created coverage
            IFeatureCoverage filteredCoverage = coverage.Filter(new VariableValueFilter<DateTime>(timeVariable, time2));
            //should not change the original!
            Assert.AreEqual(6,coverage.Components[0].Values.Count);
            
            Assert.AreEqual(coverage.Features.Count, filteredCoverage.Features.Count);
            Assert.AreEqual(3,filteredCoverage.Components[0].Values.Count);
        }

        [Test]
        public void RetrievingFeaturesUsingArgument()
        {
            var featuresVariable = new Variable<SimpleFeature>();

            var coverage = new FeatureCoverage {Arguments = {featuresVariable}};

            coverage.Features
                .Should().Not.Be.Null();
        }


    }
}

