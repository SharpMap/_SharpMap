using System.Windows.Forms;
using DelftTools.TestUtils;
using NUnit.Framework;

namespace DelftTools.Tests.TestUtils
{
    [TestFixture]
    public class WindowsFormsTestHelperTest
    {
        [Test]
        [Category("Windows.Forms")]
        public void ShowActionIsRunForAFormUsingShow()
        {
            var form = new Form();
            int callCount = 0;
            WindowsFormsTestHelper.Show(form, delegate
                                                  {
                                                      callCount++;
                                                  });
            //assert the show action was called
            Assert.AreEqual(1,callCount);
        }

        [Test]
        [Category("Windows.Forms")]
        public void ShowActionIsRunForUserControl()
        {
            var uc = new UserControl();
            int callCount = 0;
            WindowsFormsTestHelper.Show(uc, delegate
            {
                callCount++;
            });
            //assert the show action was called
            Assert.AreEqual(1, callCount);
        }

        
        
    }
}
