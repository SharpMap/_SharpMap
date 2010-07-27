using System.Windows.Forms;
using DelftTools.Controls;
using DelftTools.Controls.Swf;
using NUnit.Framework;

namespace DelftTools.Tests.Controls.Swf
{
    [TestFixture]
    public class WindowsFormsHelperTests
    {
        [Test]
        public void DragDropEffectsFromDragOperation()
        {
            Assert.AreEqual(DragDropEffects.Move, WindowsFormsHelper.ToDragDropEffects(DragOperations.Move));
            Assert.AreEqual(DragDropEffects.Copy, WindowsFormsHelper.ToDragDropEffects(DragOperations.Copy));
            Assert.AreEqual(DragDropEffects.Link, WindowsFormsHelper.ToDragDropEffects(DragOperations.Link));
            Assert.AreEqual(DragDropEffects.Scroll, WindowsFormsHelper.ToDragDropEffects(DragOperations.Scroll));
            Assert.AreEqual(DragDropEffects.All, WindowsFormsHelper.ToDragDropEffects(DragOperations.All));
            Assert.AreEqual(DragDropEffects.None, WindowsFormsHelper.ToDragDropEffects(DragOperations.None));
            // test also bitwise combined flags
            Assert.AreEqual(DragDropEffects.Move | DragDropEffects.Copy, WindowsFormsHelper.ToDragDropEffects(DragOperations.Move | DragOperations.Copy));
        }
    }
}