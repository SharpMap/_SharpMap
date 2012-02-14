using System;
using System.Linq;
using System.Windows.Forms;
using DelftTools.TestUtils;
using GeoAPI.Extensions.Networks;
using GisSharpBlog.NetTopologySuite.Geometries;
using log4net;
using NetTopologySuite.Extensions.Coverages;
using NUnit.Framework;

namespace NetTopologySuite.Extensions.Tests.Coverages
{
    [TestFixture]
    public class NetworkCoverageBindingListTest
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(NetworkCoverageBindingListTest));
        
        private INetwork network;
        private NetworkCoverage networkCoverage;
        private NetworkCoverageBindingList networkCoverageBindingList;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            LogHelper.ConfigureLogging();
        }

        [SetUp]
        public void SetUp()
        {
            network = RouteHelperTest.GetSnakeNetwork(false, new Point(0, 0), new Point(100, 0), new Point(100, 100));

            networkCoverage = new NetworkCoverage { Network = network };
            
            //add locations
            networkCoverage[new NetworkLocation(network.Branches[0], 1)] = 10.0;
            networkCoverage[new NetworkLocation(network.Branches[0], 2)] = 20.0;
            
            networkCoverage.Locations.NextValueGenerator = new NetworkLocationNextValueGenerator(networkCoverage);
            
            networkCoverageBindingList = new NetworkCoverageBindingList(networkCoverage);
        }
        
        [Test]
        public void ChangeComponentNameUpdatesColumnName()
        {
            var name = "qqq";

            networkCoverage.Components[0].Name = name;

            Assert.AreEqual(name, networkCoverageBindingList.ColumnNames[2]);
            Assert.AreEqual(name, networkCoverageBindingList.GetItemProperties(null)[2].Name);
        }

        [Test]
        [NUnit.Framework.Category(TestCategory.WindowsForms)]
        public void WorksWithTimeDependentNetworkCoverage()
        {
            var timeNetworkCoverage = new NetworkCoverage("timedep", true);
            timeNetworkCoverage.Network = network;

            var t1 = new DateTime(2000, 1, 1);
            var t2 = new DateTime(2000, 1, 2);

            timeNetworkCoverage[t1, new NetworkLocation(network.Branches[0], 1)] = 10.0;
            timeNetworkCoverage[t1, new NetworkLocation(network.Branches[1], 2)] = 20.0;
            timeNetworkCoverage[t2, new NetworkLocation(network.Branches[0], 1)] = 30.0;
            timeNetworkCoverage[t2, new NetworkLocation(network.Branches[1], 2)] = 40.0;

            var timeCoverageBindingList = new NetworkCoverageBindingList(timeNetworkCoverage);

            Assert.AreEqual(4, timeCoverageBindingList.ColumnNames.Count());

            //columns shifted
            Assert.AreEqual("Location_Branch", timeCoverageBindingList.GetItemProperties(null)[1].Name);

            var newOffset = 1.5;
            Assert.AreNotEqual(newOffset, timeNetworkCoverage.Locations.Values[0].Offset);

            timeCoverageBindingList[0][2] = newOffset;

            timeCoverageBindingList[0].EndEdit(); //commit changes

            Assert.AreEqual(newOffset, timeNetworkCoverage.Locations.Values[0].Offset);

            var gridView = new DataGridView { DataSource = timeCoverageBindingList };
            
            WindowsFormsTestHelper.ShowModal(gridView);
        }

        [Test]
        public void ChangeNetworkCoverageValuesUpdatesBindingList()
        {
            var location1 = networkCoverage.Locations.Values[0];

            Assert.AreEqual(2, networkCoverageBindingList.Count);
            Assert.AreEqual(10.0, networkCoverage[location1]);

            networkCoverage[location1] = 30.0;

            Assert.AreEqual(30.0, networkCoverageBindingList[0][2]);
            Assert.AreEqual(2, networkCoverageBindingList.Count);
        }

        [Test]
        public void AddValuesToNetworkCoverageUpdatesBindingList()
        {
            Assert.AreEqual(2, networkCoverageBindingList.Count);

            networkCoverage[new NetworkLocation(network.Branches[0], 3)] = 30.0;
            networkCoverage[new NetworkLocation(network.Branches[1], 4)] = 40.0;

            Assert.AreEqual(4,networkCoverageBindingList.Count);
        }

        [Test]
        public void CanGetCorrectValuesFromRowUsingIndexer()
        {
            var firstRow = networkCoverageBindingList[0];

            Assert.AreEqual(networkCoverage.Locations.Values[0].Branch, firstRow[0]);
            Assert.AreEqual(networkCoverage.Locations.Values[0].Offset, firstRow[1]);
        }

        [Test]
        public void CanSetBranchOnLocationUsingIndexer()
        {
            var secondRow = networkCoverageBindingList[1];

            var newBranch = network.Branches[1];

            Assert.AreNotEqual(newBranch, networkCoverage.Locations.Values[1].Branch);

            secondRow[0] = newBranch;

            secondRow.EndEdit(); //commit

            Assert.AreEqual(newBranch, networkCoverage.Locations.Values[1].Branch); //no sorting was required
        }

        [Test]
        public void ReturnsCorrectIndex()
        {
            for(int i = 0; i < networkCoverageBindingList.Count; i++)
            {
                Assert.AreEqual(i, networkCoverageBindingList[i].Index[0]);
            }
        }

        [Test]
        public void CanSetOffsetOnLocationUsingIndexer()
        {
            var firstRow = networkCoverageBindingList[0];

            var newOffset = 20.0;

            Assert.AreNotEqual(newOffset, networkCoverage.Locations.Values[1].Offset);

            firstRow[1] = newOffset;

            firstRow.EndEdit(); //commit

            Assert.AreEqual(newOffset, networkCoverage.Locations.Values[1].Offset); //sorting also occurs
        }

        [Test]
        public void CanGetCorrectValuesFromRowUsingColumnNameIndexer()
        {
            var firstRow = networkCoverageBindingList[0];

            Assert.AreEqual(networkCoverage.Locations.Values[0].Branch, firstRow[NetworkCoverageBindingList.ColumnNameBranch]);
            Assert.AreEqual(networkCoverage.Locations.Values[0].Offset, firstRow[NetworkCoverageBindingList.ColumnNameOffset]);
        }

        [Test]
        public void CanGetCorrectCoverageValues()
        {
            Assert.AreEqual(networkCoverage.Components[0].Values[0], networkCoverageBindingList[0][2]);
            Assert.AreEqual(networkCoverage.Components[0].Values[1], networkCoverageBindingList[1][2]);
        }

        [Test]
        public void NetworkLocationResultsInExtraColumnNameInBindingListRow()
        {
            int numberOfColumns = networkCoverage.Arguments.Count + networkCoverage.Components.Count + 1; //one additional for networklocation

            Assert.AreEqual(numberOfColumns, networkCoverageBindingList.ColumnNames.Count());
            Assert.AreEqual(numberOfColumns, networkCoverageBindingList.DisplayNames.Count());
        }

        [Test]
        public void NetworkLocationResultsInExtraColumnInBindingListRow()
        {
            int numberOfColumns = networkCoverage.Arguments.Count + networkCoverage.Components.Count + 1; //one additional for networklocation

            Assert.AreEqual(numberOfColumns, networkCoverageBindingList.GetItemProperties(null).Count);
        }

        [Test]
        public void CanGetCorrectValuesFromItemProperties()
        {
            var firstRow = networkCoverageBindingList[0];

            var firstBranch = ((NetworkLocation) networkCoverage.Arguments[0].Values[0]).Branch;
            var firstOffset = ((NetworkLocation) networkCoverage.Arguments[0].Values[0]).Offset;
            var firstCoverageValue = networkCoverage.Components[0].Values[0];

            var descriptorColumns = networkCoverageBindingList.GetItemProperties(null);

            Assert.AreEqual(firstBranch, descriptorColumns[0].GetValue(firstRow));
            Assert.AreEqual(firstOffset, descriptorColumns[1].GetValue(firstRow));
            Assert.AreEqual(firstCoverageValue, descriptorColumns[2].GetValue(firstRow));
        }

        [Test]
        public void CanSetValuesThroughItemProperties()
        {
            var firstRow = networkCoverageBindingList[0];

            var newBranch = ((NetworkLocation) networkCoverage.Arguments[0].Values[1]).Branch;
            var newOffset = 1.5;
            var newCoverageValue = 0.1;

            var descriptorColumns = networkCoverageBindingList.GetItemProperties(null);

            descriptorColumns[0].SetValue(firstRow, newBranch);

            firstRow.EndEdit(); //commit

            Assert.AreEqual(newBranch, ((NetworkLocation)networkCoverage.Arguments[0].Values[0]).Branch);

            descriptorColumns[1].SetValue(firstRow, newOffset);

            firstRow.EndEdit(); //commit

            Assert.AreEqual(newOffset, ((NetworkLocation)networkCoverage.Arguments[0].Values[0]).Offset);

            descriptorColumns[2].SetValue(firstRow, newCoverageValue);

            firstRow.EndEdit(); //commit

            Assert.AreEqual(newCoverageValue, networkCoverage.Components[0].Values[0]);
        }

        [Test]
        [NUnit.Framework.Category(TestCategory.WindowsForms)]
        public void ShowNetworkCoverageInGrid()
        {
            var gridView = new DataGridView {DataSource = networkCoverageBindingList};

            WindowsFormsTestHelper.ShowModal(gridView);
        }
    }
}
