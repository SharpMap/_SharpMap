using System.Diagnostics;
using System.Linq;
using DelftTools.Hydro.Structures.WeirFormula;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;
using NUnit.Framework;

namespace DelftTools.DataObjects.Tests.HydroNetwork.Structures.WeirFormula
{
    [TestFixture]
    public class FreeFormWeirFormulaTest
    {
        [Test]
        public void DefaultValues()
        {
            var formula = new FreeFormWeirFormula();
            Assert.AreEqual(2,formula.Y.Count());
        }


        [Test]
        public void SetShape()
        {
            var formula = new FreeFormWeirFormula();
            var yValues = new[] {1.0, 20.0};
            var zValues = new[] {3.0, 4.0};
            formula.SetShape(yValues, zValues);
            Assert.AreEqual(yValues[0], formula.Y.ToArray()[0], 1.0e-6);
            Assert.AreEqual(yValues[1], formula.Y.ToArray()[1], 1.0e-6);
            Assert.AreEqual(zValues[0], formula.Z.ToArray()[0], 1.0e-6);
            Assert.AreEqual(zValues[1], formula.Z.ToArray()[1], 1.0e-6);
        }

        [Test]
        public void CrestLevel()
        {
            var formula = new FreeFormWeirFormula();
            var yValues = new[] { 1.0, 20.0 };
            var zValues = new[] { 3.0, 4.0 };
            formula.SetShape(yValues, zValues);
            Assert.AreEqual(formula.CrestLevel, 3, 1.0e-6);
        }

        [Test]
        public void CheckCrestForFreeFormWeirFormulaWithEmptyLineStringShape()
        {
            var formula = new FreeFormWeirFormula
                              {
                                  Shape = new LineString(new ICoordinate[0])
                              };

            Assert.AreEqual(0, formula.CrestLevel);
            Assert.AreEqual(0, formula.CrestWidth);
        }

    }
}
