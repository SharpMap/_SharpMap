using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DelftTools.Functions;
using DelftTools.Functions.Filters;
using DelftTools.Functions.Generic;
using DelftTools.TestUtils;
using DelftTools.Utils.Collections.Generic;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Extensions.Feature;
using NetTopologySuite.Extensions.Coverages;
using NUnit.Framework;

namespace NetTopologySuite.Extensions.Tests.Features
{
    [TestFixture]
    public class FeatureVariableTest
    {
        private static IEventedList<SimpleFeature> boundaries;

        private static SimpleFeature boundary11;
        private static SimpleFeature boundary12;
        private static SimpleFeature boundary13;
        private static SimpleFeature boundary21;
        private static SimpleFeature boundary22;
        private static SimpleFeature boundary23;


        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            LogHelper.ConfigureLogging();
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            LogHelper.ResetLogging();
        }

        /// <summary>
        /// Setup creates a simple network with 2 branches each with 3 segmentboundaries. No Segments or 
        /// segmentcenters are added since these are irrelevant for the test.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            boundaries = new EventedList<SimpleFeature>();
            
            // Add boundaries; offset is used to sort the boundaries within a branch.
            boundary11 = new SimpleFeature(1);
            boundary12 = new SimpleFeature(2);
            boundary13 = new SimpleFeature(3);

            boundaries.Add(boundary11);
            boundaries.Add(boundary12);
            boundaries.Add(boundary13);

            boundary21 = new SimpleFeature(4);
            boundary22 = new SimpleFeature(5);
            boundary23 = new SimpleFeature(6);

            boundaries.Add(boundary21);
            boundaries.Add(boundary22);
            boundaries.Add(boundary23);
        }
        [Test]
        public void BranchSegmentBoundaryAsFunctionArgument()
        {
            // Test with only 1 FeatureVariable as argument; could be usefull as initial conditions
            var initialFlow = new Function("initial flow");

            var branchSegmentBoundaryVariable = new Variable<SimpleFeature>("cell");
            initialFlow.Arguments.Add(branchSegmentBoundaryVariable);

            initialFlow.Components.Add(new Variable<double>("depth"));
            
            // save results back to Functions
            initialFlow[boundary11] = 1.0;
            initialFlow[boundary12] = 2.0;
            initialFlow[boundary13] = 3.0;
            initialFlow[boundary21] = 4.0;
            initialFlow[boundary22] = 5.0;
            initialFlow[boundary23] = 6.0;
            Assert.AreEqual(new[] { boundary11, boundary12, boundary13, boundary21,boundary22, boundary23 },branchSegmentBoundaryVariable.Values);

            IList<double> values = initialFlow.GetValues<double>();
            Assert.AreEqual(1.0, values[0]);
            Assert.AreEqual(2.0, values[1]);
            Assert.AreEqual(3.0, values[2]);
            Assert.AreEqual(4.0, values[3]);
            Assert.AreEqual(5.0, values[4]);
            Assert.AreEqual(6.0, values[5]);


            values = initialFlow.GetValues<double>(new VariableValueFilter<SimpleFeature>(branchSegmentBoundaryVariable, boundary21));
            Assert.AreEqual(4.0, values[0]);

            double[] initialFlowValues = { 11, 12, 13, 14, 15, 16 }; // get from model engine
            initialFlow.SetValues(initialFlowValues);
            values = initialFlow.GetValues<double>();
            Assert.AreEqual(11.0, values[0]);
            Assert.AreEqual(12.0, values[1]);
            Assert.AreEqual(13.0, values[2]);
            Assert.AreEqual(14.0, values[3]);
            Assert.AreEqual(15.0, values[4]);
            Assert.AreEqual(16.0, values[5]);

            List<SimpleFeature> branchSegmentBoundaries2 = new List<SimpleFeature>();
            branchSegmentBoundaries2.Add(boundary21);
            branchSegmentBoundaries2.Add(boundary22);
            branchSegmentBoundaries2.Add(boundary23);
            values = initialFlow.GetValues<double>(new VariableValueFilter<SimpleFeature>(branchSegmentBoundaryVariable, branchSegmentBoundaries2));
            Assert.AreEqual(3, values.Count);
            Assert.AreEqual(14.0, values[0]);
            Assert.AreEqual(15.0, values[1]);
            Assert.AreEqual(16.0, values[2]);
        }

        // TODO: test is too big - refactor
        [Test]
        public void BranchSegmentBoundaryAsFunctionArgument_TimeDependent()
        {
            // Test with 2 arguments. A FeatureVariable DateTime as timestep
            // Add values to the store and test the retieved values
            object defaultValue = default(float);

            FeatureCoverage waterLevelCoverage = new FeatureCoverage("WaterLevel");
            Variable<DateTime> timeArgument = new Variable<DateTime>("time");
            waterLevelCoverage.Arguments.Add(timeArgument);
            Variable<SimpleFeature> boundariesFunctionArgument = new Variable<SimpleFeature>("cell");
            waterLevelCoverage.Arguments.Add(boundariesFunctionArgument);
            waterLevelCoverage.Components.Add(new Variable<float>("depth"));
            waterLevelCoverage.Features = new EventedList<IFeature>(boundaries.Cast<IFeature>());
            waterLevelCoverage.FeatureVariable.SetValues(boundaries);

            // no data added; we expect 0 rows in the table
            IList<float> values = waterLevelCoverage.GetValues<float>();
            Assert.AreEqual(0, values.Count);

            // Add 1 value for a boundary. The store will add rows with default values for all 
            // boundaries in the network.BranchSegmentBoundaries collection and set 
            // the value 1.0 to the explicit referenced boundary11.
            waterLevelCoverage[new DateTime(2000, 1, 1, 0, 0, 0), boundary11] = 1.0f;
            values = waterLevelCoverage.GetValues<float>();
            Assert.AreEqual(6, values.Count);
            Assert.AreEqual(1.0, values[0]);
            Assert.AreEqual(defaultValue, values[1]);
            Assert.AreEqual(defaultValue, values[2]);
            Assert.AreEqual(defaultValue, values[3]);
            Assert.AreEqual(defaultValue, values[4]);
            Assert.AreEqual(defaultValue, values[5]);

            // Add a new timestep and set lavel for all boundaries for this step to 2.0
            waterLevelCoverage.SetValues(new[] { 2.0f } ,
                                         new VariableValueFilter<DateTime>(timeArgument, new DateTime(2000, 2, 1, 0, 0, 0)));
            values = waterLevelCoverage.GetValues<float>();
            Assert.AreEqual(12, values.Count);

            // Overwrite the waterlevels of the first timestep to 1.0; there are no default values in the 
            // internal table
            // content should be now:
            // t=2000, 1, 1  1.0   1.0   1.0   1.0   1.0   1.0   
            // t=2000, 2, 1  2.0   2.0   2.0   2.0   2.0   2.0   
            waterLevelCoverage.SetValues(new[] { 1.0f } ,
                                         new VariableValueFilter<DateTime>(timeArgument, new DateTime(2000, 1, 1, 0, 0, 0)));
            values = waterLevelCoverage.GetValues<float>();
            Assert.AreEqual(12, values.Count);

            // Overwrite the waterlevels of the 2nd timestep.
            // content should be now:
            // t=2000, 1, 1  1.0   1.0   1.0   1.0   1.0   1.0   
            // t=2000, 2, 1  1.0   2.0   3.0   4.0   5.0   6.0   
            waterLevelCoverage.SetValues(new[] { 1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f } ,
                                         new VariableValueFilter<DateTime>(timeArgument, new DateTime(2000, 2, 1, 0, 0, 0)));
            values = waterLevelCoverage.GetValues<float>();
            Assert.AreEqual(12, values.Count);

            // Add a 3rd timestep to the function. 
            // content should be now:
            // t=2000, 1, 1  1.0   1.0   1.0   1.0   1.0   1.0   
            // t=2000, 2, 1  1.0   2.0   3.0   4.0   5.0   6.0   
            // t=2000, 3, 1 11.0  12.0  13.0  14.0  15.0  16.0   
            waterLevelCoverage.SetValues(new[] { 11.0f, 12.0f, 13.0f, 14.0f, 15.0f, 16.0f } ,
                                         new VariableValueFilter<DateTime>(timeArgument, new DateTime(2000, 3, 1, 0, 0, 0)));
            values = waterLevelCoverage.GetValues<float>();
            Assert.AreEqual(18, values.Count);

            // Ask all timesteps for boundary 21
            values = waterLevelCoverage.GetValues<float>(new VariableValueFilter<SimpleFeature>(boundariesFunctionArgument, boundary21));
            Assert.AreEqual(3, values.Count);
            Assert.AreEqual(1.0, values[0]);
            Assert.AreEqual(4.0, values[1]);
            Assert.AreEqual(14.0, values[2]);
            // Use 2 filters to get values; multiple filters work as a logical AND; only 1 values expected
            values = waterLevelCoverage.GetValues<float>(new VariableValueFilter<SimpleFeature>(boundariesFunctionArgument, boundary21),
                                                         new VariableValueFilter<DateTime>(timeArgument, new DateTime(2000, 2, 1, 0, 0, 0)));
            Assert.AreEqual(1, values.Count);
            Assert.AreEqual(4.0, values[0]);
        }

        [Test]
        public void DeleteTimeStepTest()
        {
            //waterLevel = f(cell,time)
            IFeatureCoverage waterLevelCoverage = new FeatureCoverage("WaterLevelCoverage");

            IVariable<int> timeVariable = new Variable<int>("timestep");
            IVariable<SimpleFeature> boundariesVariable = new Variable<SimpleFeature>("cell");
            IVariable<float> waterLevelVariable = new Variable<float>("Level");  
         
            waterLevelCoverage.Arguments.Add(timeVariable);
            waterLevelCoverage.Arguments.Add(boundariesVariable);
            waterLevelCoverage.Components.Add(waterLevelVariable);

            waterLevelCoverage.Features = new EventedList<IFeature>(boundaries.Cast<IFeature>());
            waterLevelCoverage.FeatureVariable.SetValues(boundaries);

            for (int i = 0; i < 12; i++)
            {
                waterLevelCoverage.SetValues(new[] { (i * 10) + 1.0f, (i * 10) + 2.0f, (i * 10) + 3.0f, 
                                                           (i * 10) + 4.0f, (i * 10) + 5.0f, (i * 10) + 6.0f } ,
                                             new VariableValueFilter<int>(timeVariable, i));
            }
            // content should be now:
            // t=0   :  1.0   2.0   3.0   4.0   5.0   6.0   
            // t=1   : 11.0  12.0  13.0  14.0  15.0  16.0   
            // ..
            // ..
            // t=11  :111.0 112.0 113.0 114.0 115.0 116.0   

            IList<float> values = waterLevelVariable.GetValues();
            Assert.AreEqual(6 * 12, values.Count);
            Assert.AreEqual(14.0, values[9]);
            Assert.AreEqual(114.0, values[69]);

            values = waterLevelVariable.GetValues(new VariableValueFilter<int>(timeVariable, 5));
            Assert.AreEqual(6, values.Count);
            Assert.AreEqual(51, values[0]);
            Assert.AreEqual(56, values[5]);

            // Remove values at t=2
            timeVariable.Values.Remove(2);
            values = waterLevelVariable.GetValues();
            Assert.AreEqual(6 * 11, values.Count);
            timeVariable.Values.Remove(3);
            timeVariable.Values.Remove(4);
            IList argumentValues = waterLevelCoverage.Arguments[0].Values;
            Assert.AreEqual(9, argumentValues.Count);
            Assert.AreEqual(11, argumentValues[8]);

            timeVariable.Values.Remove(5);
            timeVariable.Values.Remove(6);
            timeVariable.Values.Remove(7);
            argumentValues = waterLevelCoverage.Arguments[0].Values;
            Assert.AreEqual(6, argumentValues.Count);
            Assert.AreEqual(11, argumentValues[5]);

            waterLevelCoverage.RemoveValues(new VariableValueFilter<int>(timeVariable, new [] { 0, 1, 8, 9, 10, 11 }));

            values = waterLevelVariable.GetValues();
            Assert.AreEqual(0, values.Count);
            argumentValues = waterLevelCoverage.Arguments[0].Values;
            Assert.AreEqual(0, argumentValues.Count);
        }

        [Test]
        public void MultipleFilterTest()
        {
            FeatureCoverage waterLevel = new FeatureCoverage("WaterLevel");
            Variable<DateTime> timeArgument = new Variable<DateTime>("time");
            waterLevel.Arguments.Add(timeArgument);
            Variable<SimpleFeature> boundariesFunctionArgument = new Variable<SimpleFeature>("cell");
            waterLevel.Arguments.Add(boundariesFunctionArgument);
            waterLevel.Components.Add(new Variable<float>("Level"));
            waterLevel.Features = new EventedList<IFeature>(boundaries.Cast<IFeature>());
            waterLevel.FeatureVariable.SetValues(boundaries);

            waterLevel.SetValues(new[] {1.0f},
                                 new VariableValueFilter<DateTime>(timeArgument, new[]
                                                                           {
                                                                               new DateTime(2000, 1, 1, 0, 0, 0),
                                                                               new DateTime(2000, 2, 1, 0, 0, 0)
                                                                           }));

            IList<float> values = waterLevel.GetValues<float>();
            Assert.AreEqual(12, values.Count); // 6 for each timestep
            Assert.AreEqual(1.0, values[0]);
            Assert.AreEqual(1.0, values[5]);
        }
    }
}