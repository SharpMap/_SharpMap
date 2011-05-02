using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using DelftTools.TestUtils;
using NUnit.Framework;
using SharpMap.Data.Providers;
using SharpMap.Layers;
using SharpMap.UI.Forms;

namespace SharpMap.UI.Tests.Forms
{
    [TestFixture]
    public class MapControlTest
    {
        [SetUp]
        public void SetUp()
        {
            LogHelper.ConfigureLogging();
        }

        [Test]
        [Category("Windows.Forms")]
        public void DisablingLayerShouldRefreshMapControlOnce()
        {
            var mapControl = new MapControl();
            WindowsFormsTestHelper.Show(mapControl);

            mapControl.Map.Layers.Add(new LayerGroup("group1"));

            while (mapControl.IsProcessing)
            {
                Application.DoEvents();
            }

            var refreshCount = 0;
            mapControl.MapRefreshed += delegate
                                           {
                                               refreshCount++;
                                           };

            
            mapControl.Map.Layers.First().Enabled = false;
            
            while (mapControl.IsProcessing)
            {
                Application.DoEvents();
            }
            
            refreshCount.Should("map should be refreshed once when layer property changes").Be.EqualTo(1);
        }
        
        [Test]
        public void SelectToolIsActiveByDefault()
        {
            var mapControl = new MapControl();

            Assert.IsTrue(mapControl.SelectTool.IsActive);
        }
    }
}
