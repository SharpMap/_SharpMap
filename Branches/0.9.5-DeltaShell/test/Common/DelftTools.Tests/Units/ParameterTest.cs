using System;
using DelftTools.Units;
using DelftTools.Units.Generics;
using NUnit.Framework;

namespace DelftTools.Tests.Units
{
    [TestFixture]
    public class ParameterTest
    {
        [Test]
        public void DefaultValueIsPreDefinedAndAssignableForGenericType()
        {
            var parameterInt = new Parameter<int> {DefaultValue = 5};

            Assert.AreEqual(5, parameterInt.DefaultValue);
        }

        [Test]
        public void DefaultValueForIntType()
        {
            var defaultParameterValue = new Parameter<int>().DefaultValue;
            const int expectedValue = 0;

            Assert.AreEqual(expectedValue, defaultParameterValue);
        }

        [Test]
        public void DefaultValueForStringType()
        {
            var defaultParameterValue = new Parameter<string>().DefaultValue;
            var expectedValue = String.Empty;
            Assert.AreEqual(expectedValue, defaultParameterValue);
        }

        [Test]
        public void CreateWithIntTypeAndNoValue()
        {
            var parameter = new Parameter<int>();
            const int expectedValue = 0;

            Assert.AreEqual(expectedValue, parameter.Value);
        }

        [Test]
        public void CreateWithIntTypeAndValue()
        {
            const int newParameterValue = 10;
            var parameter = new Parameter<int> {Value = newParameterValue};

            Assert.AreEqual(newParameterValue, parameter.Value);
        }

        [Test]
        public void CreateWithBooleanTypeAndValue()
        {
            const bool newParameterValue = true;
            var parameter = new Parameter<bool> { Value = newParameterValue };

            Assert.AreEqual(newParameterValue, parameter.Value);
        }

        [Test]
        public void CreateWithIntTypeAndNoUnit()
        {
            var parameter = new Parameter<int>();
            Assert.IsNull(parameter.Unit);
        }

        [Test]
        public void CreateWithIntTypeAndDistanceUnit()
        {
            const string parameterName = "p1";
            var distanceUnit = new Unit("distance", "l");
            var parameter = new Parameter<int>(parameterName, distanceUnit);
            
            Assert.AreEqual(distanceUnit, parameter.Unit);
        }

        [Test]
        public void HashCodeForMinInt()
        {
            var parameter = new Parameter<int>();
            parameter.GetHashCode();

            var clonedParameter = (Parameter) parameter.Clone();

            // stack overflow was thrown
        }

        [Test]
        public void CloneParameter()
        {
            var unit = new Unit("test quantity", "TQ")
                           {
                               Id = 3
                           };
            var parameter = new Parameter<int>("original parameter",unit)
                                {ValueType = typeof (double)};

            var clonedParameter = (Parameter) parameter.Clone();

            Assert.AreEqual(parameter.Name, clonedParameter.Name);
            Assert.AreEqual(parameter.Unit.Name,clonedParameter.Unit.Name);
            Assert.AreEqual(parameter.Unit.Symbol, clonedParameter.Unit.Symbol);
            Assert.AreNotEqual(unit.Id, ((Unit)clonedParameter.Unit).Id);
            Assert.AreEqual(parameter.ValueType,clonedParameter.ValueType);
        }

        [Test]
        public void CloneDateTimeParameter()
        {
            var dateTimeParameter = new Parameter<DateTime>("original parameter");
            dateTimeParameter.Value = new DateTime(2000,1,1);
            
            var clone = (Parameter<DateTime>)dateTimeParameter.Clone();
            
            Assert.AreEqual(dateTimeParameter.Value,clone.Value);
        }
    }
}