using System.Windows.Forms;
using DelftTools.Shell.Gui.Swf.Controls.Tests.Table.TestClasses;
using DelftTools.TestUtils;
using NUnit.Framework;

namespace DelftTools.Controls.Swf.Test.Table
{
    [TestFixture]
    public class FileNameUITypeEditorTests
    {
        [Test]
        [Category("Windows.Forms")]
        public void PropertyGridWithOpenAndSave()
        {
            ClassToEdit classToEdit = new ClassToEdit();
            Form form = new Form();
            PropertyGrid grid = new PropertyGrid();
            grid.Dock = DockStyle.Fill;

            form.Controls.Add(grid);

            grid.SelectedObject = classToEdit;
            WindowsFormsTestHelper.ShowModal(form);
        }
    }
}