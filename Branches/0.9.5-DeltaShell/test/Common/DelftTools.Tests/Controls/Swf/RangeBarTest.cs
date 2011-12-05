using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using DelftTools.Controls.Swf;
using DelftTools.TestUtils;
using log4net.Config;
using NUnit.Framework;

namespace DelftTools.Tests.Controls.Swf
{
    [TestFixture]
    public class RangeBarTest
    {
        private ListBox lb;
        private BindingList<double> doubleValues;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            LogHelper.ConfigureLogging();
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            LogHelper.ResetLogging();
        }
        

        [Test][NUnit.Framework.Category("Windows.Forms")]
        public void ShowRangeBarInForm()
        {
            var rangeBar = new RangeBar();
            lb = new ListBox();
            var form = new Form();
            lb.Location = new Point(40, 40);
            form.Controls.Add(rangeBar);
            form.Controls.Add(lb);
            var histogram = new[] {11, 21, 31, 35};
            var handleValues = new double[] {10, 20, 30, 40, 50};
            doubleValues = new BindingList<double>();
            foreach (double d in handleValues)
            {
                doubleValues.Add(d);
            }
            doubleValues.AllowEdit = true;
            lb.DataSource = doubleValues;
            var colors = new[] {Color.Red, Color.Green, Color.Yellow, Color.Blue};
            rangeBar.SetHandles(handleValues, colors);
            rangeBar.Histogram = histogram;
            rangeBar.UserDraggingHandle += rangeBar_UserDraggingHandle;
            
            WindowsFormsTestHelper.ShowModal(form);
        }

        private void rangeBar_UserDraggingHandle(int aHandle, double aValue)
        {
            doubleValues[aHandle] = aValue;
        }
    }
}