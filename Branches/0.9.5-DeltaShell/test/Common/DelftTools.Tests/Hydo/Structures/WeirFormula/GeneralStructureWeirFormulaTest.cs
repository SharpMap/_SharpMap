using DelftTools.Hydro.Structures.WeirFormula;
using DelftTools.TestUtils;
using NUnit.Framework;

namespace DelftTools.DataObjects.Tests.HydroNetwork.Structures.WeirFormula
{
    [TestFixture]
    public class GeneralStructureWeirFormulaTest
    {
        [Test]
        public void Clone()
        {
            var original = new GeneralStructureWeirFormula();
            ReflectionTestHelper.FillRandomValuesForValueTypeProperties(original);
            ReflectionTestHelper.AssertPublicPropertiesAreEqual(original,original.Clone());
        }

    }
}