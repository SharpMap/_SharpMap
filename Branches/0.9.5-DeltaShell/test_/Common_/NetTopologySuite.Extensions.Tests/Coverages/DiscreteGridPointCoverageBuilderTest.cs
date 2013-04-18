using System.Linq;
using DelftTools.Functions;
using DelftTools.Functions.Generic;
using GeoAPI.Extensions.Coverages;
using NetTopologySuite.Extensions.Coverages;
using NUnit.Framework;

namespace NetTopologySuite.Extensions.Tests.Coverages
{
    [TestFixture]
    public class DiscreteGridPointCoverageBuilderTest
    {
        [Test]
        public void BuilderCanBeUsedOnlyWhenMappingsAreDefined()
        {
            var builder = new DiscreteGridPointCoverageBuilder();
            var variables = Enumerable.Empty<IVariable>();
            
            builder.CanBuildFunction(variables)
                .Should("building function with empty list of variables").Be.False();
        }

        [Test]
        public void CreateBuilder()
        {
            var iVariable = new Variable<int>("i");
            var jVariable = new Variable<int>("j");
            var xVariable = new Variable<double>("x") { Arguments = { iVariable, jVariable } };
            var yVariable = new Variable<double>("y") { Arguments = { iVariable, jVariable } };
            var valuesVariable = new Variable<double>("value") { Arguments = { iVariable, jVariable } };

            var variables = new IVariable[] { xVariable, yVariable, valuesVariable, iVariable, jVariable };
 
            var builder = new DiscreteGridPointCoverageBuilder
                              {
                                  ValuesVariableName = "value",
                                  Index1VariableName = "i",
                                  Index2VariableName = "j",
                                  XVariableName = "x",
                                  YVariableName = "y",
                              };
            
            builder.CanBuildFunction(variables).Should().Be.True();
        }

        [Test]
        public void CreateFunction()
        {
            var index1Variable = new Variable<int>("index1") { Values = {0, 1, 2} };
            var index2Variable = new Variable<int>("index2")  { Values = {0, 1} };
            var xVariable = new Variable<double>("x") { Arguments = { index1Variable, index2Variable } };
            var yVariable = new Variable<double>("y") { Arguments = { index1Variable, index2Variable } };
            var valuesVariable = new Variable<double>("value") { Arguments = { index1Variable, index2Variable } };

            xVariable.SetValues(new[,]
                                   {
                                       {1.0, 2.0, 3.0},
                                       {4.0, 5.0, 6.0}
                                   });

            yVariable.SetValues(new[,]
                                   {
                                       {1.0, 2.0, 3.0},
                                       {4.0, 5.0, 6.0}
                                   });

            valuesVariable.SetValues(new[,]
                                   {
                                       {1.0, 2.0, 3.0},
                                       {4.0, 5.0, 6.0}
                                   });

            var variables = new IVariable[] { index1Variable, index2Variable, xVariable, yVariable, valuesVariable };

            var builder = new DiscreteGridPointCoverageBuilder
            {
                Index1VariableName = "index1",
                Index2VariableName = "index2",
                XVariableName = "x",
                YVariableName = "y",
                ValuesVariableName = "value"
            };

            var coverage = (IDiscreteGridPointCoverage)builder.CreateFunction((variables));

            Assert.AreEqual(3, coverage.Index1.Values.Count);
            Assert.AreEqual(2, coverage.Index2.Values.Count);
            Assert.AreEqual(6, coverage.X.Values.Count);
            Assert.AreEqual(6, coverage.Y.Values.Count);
            Assert.AreEqual(5.0, coverage.Evaluate<double>(5.0, 5.0));
        }
    }
}