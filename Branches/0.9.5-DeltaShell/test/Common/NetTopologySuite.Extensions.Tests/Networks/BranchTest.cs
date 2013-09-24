using System;
using System.Collections.Generic;
using System.Linq;
using DelftTools.TestUtils;
using GisSharpBlog.NetTopologySuite.Geometries;
using NetTopologySuite.Extensions.Networks;
using NetTopologySuite.Extensions.Tests.Features;
using NUnit.Framework;
using SharpMap.Converters.WellKnownText;

namespace NetTopologySuite.Extensions.Tests.Networks
{
    [TestFixture]
    public class BranchTest
    {
        [Test]
        public void ChangeCustomLength()
        {
            var branch = new Branch
                             {
                                 Geometry = GeometryFromWKT.Parse("LINESTRING (0 0, 100 0)")
                             };
            branch.BranchFeatures.Add(new SimpleBranchFeature{Chainage = 50});
            Assert.AreEqual(100.0, branch.Length);
            Assert.AreEqual(50.0, branch.BranchFeatures[0].Chainage);
            branch.IsLengthCustom = true;
            branch.Length = 200;
            Assert.AreEqual(100.0, branch.BranchFeatures[0].Chainage);

            branch.IsLengthCustom = false;
            Assert.AreEqual(50.0, branch.BranchFeatures[0].Chainage);

            branch.IsLengthCustom = true;
            Assert.AreEqual(100.0, branch.BranchFeatures[0].Chainage);
        }

        [Test]
        public void GetDistinctBranchesViaLinq()
        {
            IList<Branch> branches = new List<Branch>();
            var branch1 = new Branch
            {
                Geometry = GeometryFromWKT.Parse("LINESTRING (0 0, 100 0)")
            };
            branches.Add(branch1);
            branches.Add(branch1);
            Assert.AreEqual(2, branches.Count);
            Assert.AreEqual(1, branches.Distinct().Count());
        }

        [Test]
        [Category(TestCategory.Jira)]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void CannotSetNegativeCustomLengthTools7154()
        {
            var branch = new Branch
                             {
                                 Geometry = new LineString(new[] {new Coordinate(0, 0), new Coordinate(100, 0)}),
                                 IsLengthCustom = true,
                                 Length = 0
                             };
            Assert.AreEqual(0,branch.Length); // Corner case: valid.
            branch.Length = -1; // Should throw argument out of range exception.
        }
    }
}
