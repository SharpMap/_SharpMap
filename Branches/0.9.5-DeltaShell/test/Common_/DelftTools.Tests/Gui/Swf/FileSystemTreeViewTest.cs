using DelftTools.Shell.Gui.Swf;
using DelftTools.TestUtils;
using NUnit.Framework;

namespace DelftTools.Tests.Gui.Swf
{
    [TestFixture]
    public class FileSystemTreeViewTest
    {
        [Test]
        [Category("Windows.Forms")]
        public void Show()
        {
            var treeView = new FileSystemTreeView();
            WindowsFormsTestHelper.ShowModal(treeView);
        }
    }
}