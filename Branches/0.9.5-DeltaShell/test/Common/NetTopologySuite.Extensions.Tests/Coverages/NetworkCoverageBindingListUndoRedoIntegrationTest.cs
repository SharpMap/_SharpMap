using System.Linq;
using DelftTools.Utils.UndoRedo;
using GeoAPI.Extensions.Networks;
using GisSharpBlog.NetTopologySuite.Geometries;
using NUnit.Framework;
using NetTopologySuite.Extensions.Coverages;

namespace NetTopologySuite.Extensions.Tests.Coverages
{
    [TestFixture]
    public class NetworkCoverageBindingListUndoRedoIntegrationTest
    {
        private INetwork network;
        private NetworkCoverage networkCoverage;
        private NetworkCoverageBindingList networkCoverageBindingList;

        [SetUp]
        public void SetUp()
        {
            network = RouteHelperTest.GetSnakeNetwork(false, new Point(0, 0), new Point(100, 0), new Point(100, 100));

            networkCoverage = new NetworkCoverage { Network = network };
            
            networkCoverage.Locations.NextValueGenerator = new NetworkLocationNextValueGenerator(networkCoverage);

            networkCoverageBindingList = new NetworkCoverageBindingList(networkCoverage);
        }

        [TearDown]
        public void TearDown()
        {
            networkCoverageBindingList.Dispose();
            networkCoverage.Clear();
            network = null;
            networkCoverage.Network = null;
        }

        [Test]
        public void UndoRedoFunctionChangesUpdatesBindingList()
        {
            networkCoverageBindingList.AddNew();

            using (var undoRedoManager = new UndoRedoManager(networkCoverage))
            {
                networkCoverageBindingList[0][0] = network.Branches[0]; //branch
                networkCoverageBindingList[0][1] = 50.0; //offset
                networkCoverageBindingList[0][2] = 5.0; //value
                networkCoverageBindingList[0].EndEdit();

                Assert.AreEqual(1, undoRedoManager.UndoStack.Count());
                Assert.AreEqual(1, networkCoverageBindingList.Count, "#binding rows before undo");

                undoRedoManager.Undo();

                Assert.AreEqual(0, networkCoverageBindingList.Count, "#binding rows after undo");

                undoRedoManager.Redo();

                Assert.AreEqual(1, networkCoverageBindingList.Count, "#binding rows after redo");
            }
        } 
    }
}