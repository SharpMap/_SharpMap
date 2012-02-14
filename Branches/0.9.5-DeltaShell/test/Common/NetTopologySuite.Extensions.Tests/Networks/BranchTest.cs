using System.Collections.Generic;
using System.Linq;
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
            branch.BranchFeatures.Add(new SimpleBranchFeature{Offset = 50});
            Assert.AreEqual(100.0, branch.Length);
            Assert.AreEqual(50.0, branch.BranchFeatures[0].Offset);
            branch.IsLengthCustom = true;
            branch.Length = 200;
            Assert.AreEqual(100.0, branch.BranchFeatures[0].Offset);

            branch.IsLengthCustom = false;
            Assert.AreEqual(50.0, branch.BranchFeatures[0].Offset);

            branch.IsLengthCustom = true;
            Assert.AreEqual(100.0, branch.BranchFeatures[0].Offset);
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
    }
}
