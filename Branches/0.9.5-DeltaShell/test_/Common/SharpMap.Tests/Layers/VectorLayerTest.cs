using System.Data;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using DelftTools.TestUtils;
using GeoAPI.Extensions.Feature;
using GisSharpBlog.NetTopologySuite.Geometries;
using GisSharpBlog.NetTopologySuite.IO;
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

        [Test]
        [ExpectedException(typeof(ReadOnlyException))]
        public void SettingNameOnNameIsReadOnlyLayerThrowsException()
        {
            var vectorLayer = new VectorLayer("test layer");
            vectorLayer.NameIsReadOnly = true;
            vectorLayer.Name = "new test layer";
        }

        [Test,Category(TestCategory.WindowsForms)]
        
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
            //map.ZoomToFit(map.Envelope);
            
            MapTestHelper.Show(map);
        }

        [Test]
        public void EventBubbling()
        {
            var style = new VectorStyle();
            var vectorLayer = new VectorLayer("EventBubbling") {Style = style};
            var changeCount = 0;
            
            Post.Cast<VectorLayer, System.ComponentModel.INotifyPropertyChanged>(vectorLayer).PropertyChanged +=
                delegate(object sender, System.ComponentModel.PropertyChangedEventArgs e)
                {
                    Assert.AreEqual(e.PropertyName, "Line");
                    changeCount++;
                };

            
            Assert.AreEqual(0, changeCount);
            
            var pen1 = new Pen(new SolidBrush(Color.Yellow), 3);
            style.Line = pen1;
            
            Assert.AreEqual(1, changeCount);

        }

        [Test]
        [Category(TestCategory.DataAccess)]
        public void LoadFromFile()
        {
            string filePath = Path.GetFullPath(TestHelper.GetDataDir() + @"\rivers.shp");
            IFeatureProvider dataSource = new ShapeFile(filePath, false);
            VectorLayer vectorLayer = new VectorLayer("rivers", dataSource);
            Assert.AreEqual("rivers", vectorLayer.Name);
            Assert.AreEqual(dataSource, vectorLayer.DataSource);
        }

        [Test]
        [Category(TestCategory.WindowsForms)]
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

        [Test]
        public void NoExceptionShouldBeThrownWhenZoomLevelIsTooLarge()
        {
            var featureProvider = new DataTableFeatureProvider();
            featureProvider.Add(new WKTReader().Read("LINESTRING(0 0,80000000 0)"));
            featureProvider.Add(new WKTReader().Read("POINT(50000000 0)"));
            var layer = new VectorLayer { DataSource = featureProvider };
            var map = new Map {Layers = {layer}, Size = new Size(1000, 1000)};
            map.Render();
            map.ZoomToFit(new Envelope(50000000, 50000001, 0, 1));

            map.Render();
        }

        [Test]
        public void RenderLabelsForVectorLayerFeatures()
        {
            var featureProvider = new DataTableFeatureProvider();
            featureProvider.Add(new WKTReader().Read("POINT(0 0)"));
            featureProvider.Add(new WKTReader().Read("POINT(100 100)"));

            var layer = new VectorLayer
                            {
                                DataSource = featureProvider,
                                LabelLayer = {Visible = true} // set labels on, happens in ThemeEditorDialog
                            };

            // make label layer use delegate so that we can check if it renders
            var callCount = 0;
            layer.LabelLayer.LabelStringDelegate = delegate(IFeature feature)
                                                       {
                                                           callCount++;
                                                           return feature.ToString();
                                                       };
                                                       

            var map = new Map { Layers = { layer }, Size = new Size(1000, 1000) };
            map.Render();

            callCount
                .Should("labels are rendered for 2 features of the vector layer").Be.EqualTo(2);
        }

        [Test]
        public void LabelsLayerIsInitializedAndOffByDefault()
        {
            var layer = new VectorLayer();
            layer.LabelLayer.Visible
                .Should("label layer is off by default").Be.False();
        }

        [Test]
        public void DoNotRenderLabelsForVectorLayerFeaturesWhenLabelsAreNotVisible()
        {
            var featureProvider = new DataTableFeatureProvider();
            featureProvider.Add(new WKTReader().Read("POINT(0 0)"));
            featureProvider.Add(new WKTReader().Read("POINT(100 100)"));

            var layer = new VectorLayer
            {
                DataSource = featureProvider
            };

            // make label layer use delegate so that we can check if it renders
            var callCount = 0;
            layer.LabelLayer.LabelStringDelegate = delegate(IFeature feature)
            {
                callCount++;
                return feature.ToString();
            };

            var map = new Map { Layers = { layer }, Size = new Size(1000, 1000) };
            map.Render();

            callCount
                .Should("labels are not rendered when label layer is not visible").Be.EqualTo(0);
        }
    }
}
