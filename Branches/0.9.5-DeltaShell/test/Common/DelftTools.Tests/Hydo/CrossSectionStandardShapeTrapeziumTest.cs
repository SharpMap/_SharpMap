using System.Linq;
using DelftTools.Hydro.CrossSections.StandardShapes;
using NUnit.Framework;

namespace DelftTools.Tests.Hydo
{
    [TestFixture]
    public class CrossSectionStandardShapeTrapeziumTest
    {
        [Test]
        public void BottomEqualAndLargerThanTop()
        {
            var trapezium = new CrossSectionStandardShapeTrapezium();

            trapezium.BottomWidthB = 10;
            trapezium.MaximumFlowWidth = 10;
            trapezium.Slope = 1;

            Assert.AreEqual(6, trapezium.GetTabulatedDefinition().Profile.Count());

            trapezium.MaximumFlowWidth = 8;

            Assert.AreEqual(6, trapezium.GetTabulatedDefinition().Profile.Count());
        }
    }
}