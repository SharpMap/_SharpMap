using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Timers;
using System.Windows.Forms;
using DelftTools.Functions.Binding;
using DelftTools.TestUtils;
using DelftTools.Utils.Collections;
using DelftTools.Utils.Collections.Generic;
using log4net;
using log4net.Config;
using NUnit.Framework;
using Category = NUnit.Framework.CategoryAttribute;
using Timer=System.Timers.Timer;

namespace DelftTools.Functions.Tests.Binding
{
    [TestFixture]
    public class MultiDimensionalArrayBindingListTest
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(MultiDimensionalArrayBindingListTest));

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


        [Test]
        [Category("Windows.Forms")]
        public void Bind2D()
        {
            IMultiDimensionalArray array = new MultiDimensionalArray();
            array.Resize(2, 2);
            array[0, 0] = 5;
            array[1, 1] = 2;

            DataGridView gridView = new DataGridView();

            MultiDimensionalArrayBindingList multiDimensionalArrayBindingList = new MultiDimensionalArrayBindingList(array);
            gridView.DataSource = multiDimensionalArrayBindingList;

            Form form = new Form();
            gridView.Dock = DockStyle.Fill;
            form.Controls.Add(gridView);
            
            WindowsFormsTestHelper.ShowModal(form);
        }

        [Test]
        [Category("Windows.Forms")]
        public void Bind3DArrayUsing2DView()
        {
            IMultiDimensionalArray array = new MultiDimensionalArray(3, 3, 3);
            array[0, 0, 0] = 1;
            array[1, 1, 1] = 2;

            IMultiDimensionalArrayView view = array.Select(0, 0, 0);
            view.Reduce[0] = true; // reduce 1st dimension

            DataGridView gridView = new DataGridView();

            MultiDimensionalArrayBindingList multiDimensionalArrayBindingList = new MultiDimensionalArrayBindingList(view);
            gridView.DataSource = multiDimensionalArrayBindingList;

            Form form = new Form();
            gridView.Dock = DockStyle.Fill;
            form.Controls.Add(gridView);
            
            WindowsFormsTestHelper.ShowModal(form);
        }

        [Test]
        public void TrackChangedValuesInArray()
        {
            /* checking if we can catch value changed events
            int i = 0;
            var t = new Timer(1000);
            Random random = new Random();
            t.Elapsed += delegate
                             {
                                 int row = random.Next(0, array.GetTotalLength(0));
                                 int column = random.Next(0, array.GetTotalLength(1));
                                 array.SetValue(i++, row, column);
                                 multiDimensionalArrayBindingList.FireChangedEvent(ListChangedType.ItemChanged, row);
                             };

            t.Enabled = true;
*/
        }

        [Test]
        public void RemoveRow()
        {
            int[] lengths = new[] { 2, 3 };
            IMultiDimensionalArray array = new MultiDimensionalArray(lengths);

            array[0, 0] = 1;
            array[0, 1] = 2;
            array[0, 2] = 3;

            array[1, 0] = 4;
            array[1, 1] = 5;
            array[1, 2] = 6;

            IMultiDimensionalArrayBindingList bindingList = new MultiDimensionalArrayBindingList(array);
            bindingList.RemoveAt(1);

            int expectedRowsCount = 2;
            Assert.AreEqual(expectedRowsCount, array.Shape[bindingList.RowDimension]);
        }
    }
}