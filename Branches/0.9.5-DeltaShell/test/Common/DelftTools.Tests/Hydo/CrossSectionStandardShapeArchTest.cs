using DelftTools.Hydro.CrossSections.StandardShapes;
using NUnit.Framework;

namespace DelftTools.Tests.Hydo
{
    [TestFixture]
    public class CrossSectionStandardShapeArchTest
    {
        [Test]
        public void HeightGrowsAlongWithArchHeight()
        {
            var arch = new CrossSectionStandardShapeArch();

            arch.Height = 15;
            arch.ArcHeight = 30;

            Assert.AreEqual(30, arch.Height); //grows
            Assert.AreEqual(30, arch.ArcHeight);

            arch.ArcHeight = 15;

            Assert.AreEqual(30, arch.Height); //doesn't shrink
            Assert.AreEqual(15, arch.ArcHeight);
        }
    }
}