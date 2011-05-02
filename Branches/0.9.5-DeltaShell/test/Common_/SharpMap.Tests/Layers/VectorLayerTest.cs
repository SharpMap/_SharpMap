using System.Data;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using DelftTools.TestUtils;
using GisSharpBlog.NetTopologySuite.Geometries;
using NUnit.Framework;
using PostSharp;
using SharpMap.Data;
using SharpMap.Data.Providers;
using SharpMap.Layers;
using SharpMap.Rendering.Thematics;
using SharpMap.Styles;
using SharpMap.UI.Forms;
using SharpMapTestUtils;

namespace SharpMap.Tests.Layers
{
    [TestFixture]
   public class VectorLayerTest
    {

        [Test,Category("Windows.Forms")]
        
        public void ShowMapWithPointLayerBasedOnFeatureDataTable()
        {
            var table = new FeatureDataTable();
            table.Columns.Add("X", typeof(double));
            table.Columns.Add("Y", typeof(double));
            table.Columns.Add("Category", typeof(string));
            DataRow row = table.NewRow();
            table.Rows.Add(row);
            row.ItemArray = new object[] { 100000, 400000, "testCategory" };
            row = table.NewRow();
            table.Rows.Add(row);
            row.ItemArray = new object[] { 200000, 400000, "TestCategory" };

            var dataTablePoint = new DataTablePoint(table, "Category", "X", "Y");
            var vectorLayer = new VectorLayer("test", dataTablePoint);


            vectorLayer.Theme =ThemeFactory.CreateSingleFeatureTheme(vectorLayer.Style.GeometryType, Color.Blue, 10);
            var map = new Map { Name = "testmap" };

            map.Layers.Add(vectorLayer);
            map.Center = new Coordinate(150000, 400000);

            map.Zoom = 200000;
            //map.ZoomToExtents();
            //map.ZoomToBox(map.Envelope);
            
            MapTestHelper.Show(map);
        }

        [Test]
        public void EventBubbling()
        {
            VectorStyle style = new VectorStyle();
            VectorLayer vectorLayer = new VectorLayer("EventBubbling");
            vectorLayer.Style = style;
            int changeCount = 0;
            Post.Cast<VectorLayer, System.ComponentModel.INotifyPropertyChanged>(vectorLayer).PropertyChanged +=
                delegate(object sender, System.ComponentModel.PropertyChangedEventArgs e)
                {
                    Assert.AreEqual(e.PropertyName, "Line");
                    changeCount++;
                };

            Assert.AreEqual(0, changeCount);
            Pen pen1 = new Pen(new SolidBrush(Color.Yellow), 3);
            style.Line = pen1;
            Assert.AreEqual(1, changeCount);

        }

        [Test]
        [Category("DataAccess")]
        public void LoadFromFile()
        {
            string filePath = Path.GetFullPath(TestHelper.GetDataDir() + @"\rivers.shp");
            IFeatureProvider dataSource = new ShapeFile(filePath, false);
            VectorLayer vectorLayer = new VectorLayer("rivers", dataSource);
            Assert.AreEqual("rivers", vectorLayer.Name);
            Assert.AreEqual(dataSource, vectorLayer.DataSource);
        }

        [Test]
        [Category("Windows.Forms")]
        public void RenderSymbol()
        {
            VectorLayer layer = new VectorLayer();
            layer.DataSource = new DataTableFeatureProvider("LINESTRING(20 20,40 40)");

            VectorLayer symbolLayer = new VectorLayer("GPS");
            symbolLayer.DataSource = new DataTableFeatureProvider("POINT(30 30)");
            symbolLayer.Style.Symbol = Properties.Resources.NorthArrow;
            symbolLayer.Style.SymbolRotation = 0;
            symbolLayer.Style.SymbolOffset = new PointF(0, 0);
            symbolLayer.Style.SymbolScale = 0.5f;

            //Show layer on form with mapcontrol
            Form form = new Form();
            MapControl mapControl = new MapControl();
            mapControl.Dock = DockStyle.Fill;
            form.Controls.Add(mapControl);
            mapControl.Map = new Map(new Size(600, 600));

            mapControl.Map.Layers.Add(symbolLayer);
            mapControl.Map.Layers.Add(layer);

            form.Show();
            mapControl.Map.ZoomToExtents();
            mapControl.Refresh();
            form.Hide();

            WindowsFormsTestHelper.ShowModal(form);
        }
    }
}
