using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DelftTools.Controls.Swf.WizardPages;
using DelftTools.TestUtils;
using NUnit.Framework;

namespace DelftTools.Tests.Controls.Swf.WizardPages
{
    [TestFixture]
    public class SelectFileWizardPageTest
    {
        [Test]
        [Category("Windows.Forms")]
        public void Show()
        {
            var selectFileWizardPage = new SelectFileWizardPage();
            selectFileWizardPage.Filter = "";
            WindowsFormsTestHelper.ShowModal(selectFileWizardPage);
        }
    }
}
