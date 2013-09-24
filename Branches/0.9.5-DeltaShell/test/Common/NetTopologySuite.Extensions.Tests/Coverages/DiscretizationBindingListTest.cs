using System.Linq;
using GisSharpBlog.NetTopologySuite.Geometries;
using NetTopologySuite.Extensions.Coverages;
using NUnit.Framework;

namespace NetTopologySuite.Extensions.Tests.Coverages
{
    [TestFixture]
    public class DiscretizationBindingListTest
    {
        [Test]
        public void DiscretizationBinding()
        {
            var network = RouteHelperTest.GetSnakeNetwork(false, new Point(0, 0), new Point(100, 0), new Point(100, 100));
            var discretization = new Discretization {Network = network};

            var discretizationBindingList = new DiscretizationBindingList(discretization);

            Assert.AreEqual(4, discretizationBindingList.ColumnNames.Count());
            Assert.AreEqual(DiscretizationBindingList.ColumnNameLocationName, discretizationBindingList.ColumnNames[2]);

            Assert.AreEqual(0, discretizationBindingList.Count());

            discretization[new NetworkLocation(network.Branches.First(), 0.0) { Name = "haha" }] = 1.0;

            Assert.AreEqual(1, discretizationBindingList.Count());

            Assert.AreEqual("haha", discretizationBindingList.First()[2].ToString());


        }
    }
}
