using GeoAPI.Extensions.Coverages;
using GisSharpBlog.NetTopologySuite.Geometries;
using NetTopologySuite.Extensions.Coverages;
using System.Linq;
using NUnit.Framework;

namespace NetTopologySuite.Extensions.Tests.Coverages
{
    [TestFixture]
    public class RegularGridCoverageCellTest
    {
        private static IRegularGridCoverage CreateTestCoverage(double offsetX, double offsetY)
        {
            IRegularGridCoverage grid2D = new RegularGridCoverage(2, 3, 100, 50, offsetX, offsetY) { Name = "test" };

            grid2D.SetValues(new[]
                                 {
                                     1.0, 2.0,
                                     3.0, 4.0,
                                     5.0, 6.0
                                 });
            return grid2D;
        }

        [Test]
        public void Geometry()
        {
            var coverage = CreateTestCoverage(0.0, 0.0);
            RegularGridCoverageCell regularGridCoverageCell = new RegularGridCoverageCell
                                                                  {X = 0.0, Y = 0.0, RegularGridCoverage = coverage};
            Assert.IsInstanceOfType(typeof(Polygon), regularGridCoverageCell.Geometry);
            // the correct geometry is a cell at 0.0 of width 100 and height 50
            Assert.AreEqual(  0.0, regularGridCoverageCell.Geometry.Coordinates.Min(c => c.X), 1.0e-6);
            Assert.AreEqual(  0.0, regularGridCoverageCell.Geometry.Coordinates.Min(c => c.Y), 1.0e-6);
            Assert.AreEqual(100.0, regularGridCoverageCell.Geometry.Coordinates.Max(c => c.X), 1.0e-6);
            Assert.AreEqual( 50.0, regularGridCoverageCell.Geometry.Coordinates.Max(c => c.Y), 1.0e-6);
        }

    }
}
