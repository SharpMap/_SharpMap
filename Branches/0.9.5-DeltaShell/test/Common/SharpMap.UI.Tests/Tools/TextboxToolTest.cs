using DelftTools.TestUtils;
using NUnit.Framework;
using SharpMap.UI.Forms;
using SharpMap.UI.Tools;

namespace SharpMap.UI.Tests.Tools
{
    [TestFixture]
    public class TextboxToolTest
    {
        [Test, Category(TestCategory.WindowsForms)]
        public void ShowMapWithTextboxTool()
        {
            MapControl mapControl = new MapControl{ AllowDrop = false };
            mapControl.Tools.Add(new TextboxTool {Text = "This is a test text"});
            WindowsFormsTestHelper.ShowModal(mapControl);
        }
    }
}