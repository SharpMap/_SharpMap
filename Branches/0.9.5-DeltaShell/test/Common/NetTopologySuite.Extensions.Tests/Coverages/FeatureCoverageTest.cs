using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DelftTools.Functions;
using DelftTools.Functions.Filters;
using DelftTools.Functions.Generic;
using DelftTools.TestUtils;
using DelftTools.Utils.Collections.Generic;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Utilities;
using NetTopologySuite.Extensions.Coverages;
using NetTopologySuite.Extensions.Features;
using NetTopologySuite.Extensions.Tests.Features;
using NUnit.Framework;
using SharpMap.Data.Providers;
using GisSharpBlog.NetTopologySuite.Geometries;

using SharpTestsEx;

using Assert = NUnit.Framework.Assert;

namespace NetTopologySuite.Extensions.Tests.Coverages
{
    [TestFixture]
    public class FeatureCoverageTest
    {
        private string sharpMapGisShapeFilePath = 
            TestHelper.GetTestDataPath(TestDataPath.DeltaShell.DeltaShellDeltaShellPluginsSharpMapGisTests);
                 
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
            coverage.Features = new EventedList<IFeature>(features.Cast<IFeature>());

            // nicer API is: coverage[features[0]] = 0.1;

            // set values of feature a variable
            coverage.FeatureVariable.SetValues(features);

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
            
            coverage.Features = new EventedList<IFeature>(features.Cast<IFeature>());
            // set values of feature a variable
            coverage.FeatureVariable.SetValues(features);

            double[] values = new double[] { 1.0, 2.0, 3.0 };
            coverage.SetValues(values);

            Assert.AreEqual(3, coverage.GetValues<double>().Count);

            // set features second time
            coverage.Features = new EventedList<IFeature>(features.Cast<IFeature>());

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

            coverage.Features = new EventedList<IFeature>(features.Cast<IFeature>());
            // set values of feature a variable
            coverage.FeatureVariable.SetValues(features);

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
            var features = new EventedList<IFeature>
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

            // set features
            coverage.Features = features;

            // set values of feature a variable
            coverage.FeatureVariable.SetValues(features);

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
            coverage.Features = new EventedList<IFeature>(features.Cast<IFeature>());
            // set values of feature a variable
            coverage.FeatureVariable.SetValues(features);

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
        [Category(TestCategory.Integration)]
        public void UseShapefileFeaturesAdFeatureCoverageSource()
        {
            string path =
                Path.Combine(sharpMapGisShapeFilePath,"rivers.shp");
            ShapeFile shapeFile = new ShapeFile(path);
            
            // select only some features from shapefile
            IEnvelope coverageFeatureEnvelope = shapeFile.GetExtents();
            coverageFeatureEnvelope.ExpandBy(0.5, 0.5);

            var coverage = new FeatureCoverage();
            coverage.Components.Add(new Variable<double>("value"));
            coverage.Arguments.Add(new Variable<Feature>("feature"));
            coverage.Features = new EventedList<IFeature>(shapeFile.Features.Cast<IFeature>());
            coverage.FeatureVariable.AddValues(coverage.Features);

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
            coverage.Features = new EventedList<IFeature>(features.Cast<IFeature>());
            // set values of feature a variable
            coverage.FeatureVariable.SetValues(features);

            DateTime time1 = DateTime.Now;
            coverage[time1] = new double[] { 1.0, 2.0, 3.0 };

            DateTime time2 = time1.AddDays(1);
            coverage[time2] = new[] { 10.0, 20.0, 30.0 };

            Assert.AreEqual(6,coverage.Components[0].Values.Count);
            //filter the created coverage
            IFeatureCoverage filteredCoverage = coverage.FilterAsFeatureCoverage(new VariableValueFilter<DateTime>(timeVariable, time2));
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

        [Test]
        public void GetTimeSeriesFromTimeDependentFeatureCoverage()
        {
            FeatureCoverage coverage = GetTimeDependentFeatureCoverage();
            var coverageName = "Waste disposal";
            coverage.Name = coverageName;

            var t1 = new DateTime(2000, 1, 1);
            var t2 = new DateTime(2000, 1, 2);
            var t3 = new DateTime(2000, 1, 3);

            //set some values for each time step
            coverage[t1] = new[] {1.0, 2.0, 3.0};
            coverage[t2] = new[] {4.0, 5.0, 6.0};
            coverage[t3] = new[] {7.0, 8.0, 9.0};

            //get a timeseries for the 1st feature
            var firstFeature = coverage.FeatureVariable.Values.OfType<SimpleFeature>().First();
            var featureName = "Amsterdam";
            firstFeature.Name = featureName;
            var timesSeries = coverage.GetTimeSeries(firstFeature.Geometry.Coordinate);
            Assert.AreEqual(new[] {t1, t2, t3}, timesSeries.Arguments[0].Values);
            Assert.AreEqual(new[] {1.0d, 4.0d, 7.0d}, timesSeries.Components[0].Values);
            Assert.AreEqual(coverageName + " at " + featureName, timesSeries.Name);
            //check it is reduced
            Assert.AreEqual(1, timesSeries.Arguments.Count);
        }
        
        [Test]
        public void GetTimeSeriesFromTimeDependentPolygonFeatureCoverageWithCentroid()
        {
            //QueryTimeSeriesMapCommand uses centroid

            var coverage = GetTimeDependentFeatureCoverage();

            foreach(var feature in coverage.Features)
            {
                var factory = new GeometricShapeFactory();
                factory.Centre = feature.Geometry.Coordinate;
                factory.Size = 5;
                feature.Geometry = factory.CreateCircle();
            }

            var coverageName = "Waste disposal";
            coverage.Name = coverageName;

            var t1 = new DateTime(2000, 1, 1);
            var t2 = new DateTime(2000, 1, 2);
            var t3 = new DateTime(2000, 1, 3);

            //set some values for each time step
            coverage[t1] = new[] { 1.0, 2.0, 3.0 };
            coverage[t2] = new[] { 4.0, 5.0, 6.0 };
            coverage[t3] = new[] { 7.0, 8.0, 9.0 };

            //get a timeseries for the 1st feature
            var firstFeature = coverage.FeatureVariable.Values.OfType<SimpleFeature>().First();
            var featureName = "Amsterdam";
            firstFeature.Name = featureName;
            var timesSeries = coverage.GetTimeSeries(firstFeature.Geometry.Centroid.Coordinate); 
            Assert.AreEqual(new[] { t1, t2, t3 }, timesSeries.Arguments[0].Values);
            Assert.AreEqual(new[] { 1.0d, 4.0d, 7.0d }, timesSeries.Components[0].Values);
            Assert.AreEqual(coverageName + " at " + featureName, timesSeries.Name);
            //check it is reduced
            Assert.AreEqual(1, timesSeries.Arguments.Count);
        }

        [Test]
        public void CloneShouldCloneFeatures()
        {
            var features = new EventedList<IFeature> {new SimpleFeature(0, new Point(0, 0)), new SimpleFeature(1, new Point(1, 1))};

            var coverage = new FeatureCoverage
                               {
                                   Arguments = { new Variable<SimpleFeature>() },
                                   Components = { new Variable<double>() },
                                   Features = features 
                               };

            coverage[features[0]] = 1.0;
            coverage[features[1]] = 2.0;

            // clone
            var clone = (FeatureCoverage)coverage.Clone();

            // check
            clone.Features.Count.Should().Be.EqualTo(2);
        }

        [Test]
        public void SetValuesPerFeature()
        {
            var coverage = GetTimeDependentFeatureCoverage();

            var t1 = new DateTime(2000, 1, 1);
            var t2 = new DateTime(2000, 1, 2);

            coverage.Time.SetValues(new[] { t1, t2} );
            
            foreach(var feature in coverage.Features)
            {
                var functionForFeature = coverage.Filter(new IVariableFilter[]
                                        {
                                            new VariableReduceFilter(coverage.FeatureVariable),
                                            new VariableValueFilter<SimpleFeature>(coverage.FeatureVariable,
                                                                                   feature as SimpleFeature)
                                        });
                functionForFeature.SetValues(new[] { 3.3, 4.4 });
            }

            Assert.AreEqual(new[] {3.3, 3.3, 3.3, 4.4, 4.4, 4.4 }, coverage.GetValues().Cast<double>().ToArray());
        }

        private static FeatureCoverage GetTimeDependentFeatureCoverage()
        {
            IList<SimpleFeature> features = new List<SimpleFeature>
                                                {
                                                    new SimpleFeature(0, new Point(0, 0)),
                                                    new SimpleFeature(1, new Point(1, 1)),
                                                    new SimpleFeature(2, new Point(2, 2))
                                                };

            var coverage = FeatureCoverage.GetTimeDependentFeatureCoverage<SimpleFeature>();
            
            // set values of feature a variable
            coverage.Features.AddRange(features.OfType<IFeature>());
            coverage.FeatureVariable.SetValues(features);
            return coverage;
        }

        [Test]
        public void GetTimeSeriesFromTimeDependentFeatureCoverageByFeatures()
        {
            FeatureCoverage coverage = GetTimeDependentFeatureCoverage();
            var coverageName = "Waste disposal";
            coverage.Name = coverageName;

            var t1 = new DateTime(2000, 1, 1);
            var t2 = new DateTime(2000, 1, 2);
            var t3 = new DateTime(2000, 1, 3);

            //set some values for each time step
            coverage[t1] = new[] { 1.0, 2.0, 3.0 };
            coverage[t2] = new[] { 4.0, 5.0, 6.0 };
            coverage[t3] = new[] { 7.0, 8.0, 9.0 };

            //get a timeseries for the 1st feature
            var firstFeature = coverage.FeatureVariable.Values.OfType<SimpleFeature>().First();
            var firstFeatureName = "Schiedam";
            firstFeature.Name = firstFeatureName;
            var lastFeature = coverage.FeatureVariable.Values.OfType<SimpleFeature>().Last();
            var lastFeatureName = "Maassluis";
            lastFeature.Name = lastFeatureName;
            var firstTimesSeries = coverage.GetTimeSeries(firstFeature);
            var lastTimesSeries = coverage.GetTimeSeries(lastFeature);
            Assert.AreEqual(new[] { t1, t2, t3 }, firstTimesSeries.Arguments[0].Values);
            Assert.AreEqual(new[] { 1.0d, 4.0d, 7.0d }, firstTimesSeries.Components[0].Values);
            Assert.AreEqual(coverageName + " at " + firstFeatureName, firstTimesSeries.Name);
            Assert.AreEqual(new[] { t1, t2, t3 }, lastTimesSeries.Arguments[0].Values);
            Assert.AreEqual(new[] { 3.0d, 6.0d, 9.0d }, lastTimesSeries.Components[0].Values);
            Assert.AreEqual(coverageName + " at " + lastFeatureName, lastTimesSeries.Name);
        }
    }
}

