using NUnit.Framework;
using SharpMap.UI.Forms;
using SharpMap.UI.Tools;

namespace SharpMap.UI.Tests.Tools
{
    [TestFixture]
    public class SelectToolTest
    {
        [Test]
        public void DeActiveSelectionShouldResetMultiSelectionMode()
        {
            MapControl mapControl = new MapControl();

            SelectTool selectTool = mapControl.SelectTool;
            selectTool.MultiSelectionMode = MultiSelectionMode.Lasso;

            mapControl.ActivateTool(selectTool);
            Assert.AreEqual(MultiSelectionMode.Lasso, selectTool.MultiSelectionMode);

            mapControl.ActivateTool(mapControl.MoveTool);
            Assert.AreEqual(MultiSelectionMode.Rectangle, selectTool.MultiSelectionMode);
        }
    }
}
