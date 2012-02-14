using System;
using System.Drawing;
using System.Windows.Forms;
using DelftTools.TestUtils;
using NUnit.Framework;
using SharpMap.UI.Forms;
using SharpMap.UI.Tools;

namespace SharpMap.UI.Tests.Tools
{
    [TestFixture]
    public class MapToolExample
    {
        [Test]
        [Category(TestCategory.WindowsForms)]
        public void MapToolMessageBoxEnabledTest()
        {
            var demoMapTool = new MapToolMessageBox();
            var mapControl = new MapControl() { Map = new Map(new Size(100, 100)) };
            mapControl.Tools.Add(demoMapTool);
            mapControl.ActivateTool(demoMapTool);
            demoMapTool.Enable();
            WindowsFormsTestHelper.ShowModal(mapControl);
        }

        [Test]
        [Category(TestCategory.WindowsForms)]
        public void MapToolMessageBoxDisabledTest()
        {
            var demoMapTool = new MapToolMessageBox();
            var mapControl = new MapControl() { Map = new Map(new Size(100, 100)) };
            mapControl.Tools.Add(demoMapTool);
            mapControl.ActivateTool(demoMapTool);
            demoMapTool.Disable();
            WindowsFormsTestHelper.ShowModal(mapControl);
        }
    }

    public class MapToolMessageBox: IMapTool
    {
        private bool enabled = true;

        public void Disable()
        {
            enabled = false;
        }

        public void Enable()
        {
            enabled = true;
        }

        #region IMapTool Members

        public SharpMap.UI.Forms.IMapControl MapControl
        {
            get; set;
        }

        public void OnMouseDown(GeoAPI.Geometries.ICoordinate worldPosition, System.Windows.Forms.MouseEventArgs e)
        {

        }

        public void OnMouseMove(GeoAPI.Geometries.ICoordinate worldPosition, System.Windows.Forms.MouseEventArgs e)
        {

        }

        public void OnMouseUp(GeoAPI.Geometries.ICoordinate worldPosition, System.Windows.Forms.MouseEventArgs e)
        {
            if (enabled)
            {
                MessageBox.Show("Hallo Rob", "Demo MapTool");
            }
        }

        public void OnMouseWheel(GeoAPI.Geometries.ICoordinate worldPosition, System.Windows.Forms.MouseEventArgs e)
        {

        }

        public void OnMouseDoubleClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {

        }

        public void OnMouseHover(GeoAPI.Geometries.ICoordinate worldPosition, EventArgs e)
        {

        }

        public void OnKeyDown(System.Windows.Forms.KeyEventArgs e)
        {

        }

        public void OnKeyUp(System.Windows.Forms.KeyEventArgs e)
        {

        }

        public void OnPaint(System.Windows.Forms.PaintEventArgs e)
        {

        }

        public void Render(System.Drawing.Graphics graphics, Map mapBox)
        {

        }

        public void OnMapLayerRendered(System.Drawing.Graphics g, SharpMap.Layers.ILayer layer)
        {

        }

        public void OnMapPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {

        }

        public void OnMapCollectionChanged(object sender, DelftTools.Utils.Collections.NotifyCollectionChangingEventArgs e)
        {

        }

        public void OnBeforeContextMenu(System.Windows.Forms.ContextMenuStrip menu, GeoAPI.Geometries.ICoordinate worldPosition)
        {

        }

        public void OnDragEnter(System.Windows.Forms.DragEventArgs e)
        {

        }

        public void OnDragDrop(System.Windows.Forms.DragEventArgs e)
        {

        }

        public bool IsBusy
        {
            get
            {
                return false;
            }
            set
            {
 
            }
        }

        public bool IsActive
        {
            get; set;
        }

        public bool Enabled
        {
            get { return enabled; }
        }

        public bool AlwaysActive
        {
            get { return false; }
        }

        public void Execute()
        {

        }

        public void Cancel()
        {

        }

        public string Name
        {
            get
            {
                return "";
            }
            set
            {
 }
        }

        public SharpMap.Layers.ILayer Layer
        {
            get; set;
        }

        public bool RendersInScreenCoordinates
        {
            get { return true; }
        }

        public void ActiveToolChanged(IMapTool newTool)
        {
        }

        public SharpMap.Layers.ILayer GetLayerByFeature(GeoAPI.Extensions.Feature.IFeature feature)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
