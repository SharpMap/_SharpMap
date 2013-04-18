using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using DelftTools.TestUtils;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;
using log4net.Config;
using NUnit.Framework;
using PostSharp;
using SharpMap.Converters.WellKnownText;
using SharpMap.Data.Providers;
using SharpMap.Layers;
using SharpMap.Styles;
using GeometryFactory = SharpMap.Converters.Geometries.GeometryFactory;

namespace SharpMap.Tests
{
    [TestFixture]
    public class MapTest 
    {
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

        //TODO: rename this test
        [Test]
        public void EventBubbling2()
        {
            int changeCount = 0;
            var map = new Map(new Size(2, 1));
            var vectorLayer = new VectorLayer("EventBubbling");
            map.Layers.Add(vectorLayer);

            Post.Cast<Map, INotifyPropertyChanged>(map).PropertyChanged +=
                delegate(object sender, PropertyChangedEventArgs e)
                    {
                        Assert.AreEqual(e.PropertyName, "Line");
                        changeCount++;
                    };

            Assert.AreEqual(0, changeCount);
            var pen1 = new Pen(new SolidBrush(Color.Yellow), 3);
            vectorLayer.Style.Line = pen1;
            Assert.AreEqual(1, changeCount);
        }

        //TODO: rename this test
        [Test]
        public void EventBubbling3()
        {
            int changeCount = 0;
            var map = new Map(new Size(2, 1));
            var style = new VectorStyle();
            var vectorLayer = new VectorLayer("EventBubbling") {Style = style};
            map.Layers.Add(vectorLayer);

            Post.Cast<Map, INotifyPropertyChanged>(map).PropertyChanged +=
                delegate(object sender, PropertyChangedEventArgs e)
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
        public void Initalize_MapInstance()
        {
            var map = new Map(new Size(2, 1));
            Assert.IsNotNull(map);
            Assert.IsNotNull(map.Layers);
            Assert.AreEqual(2f, map.Size.Width);
            Assert.AreEqual(1f, map.Size.Height);
            Assert.AreEqual(Color.Transparent, map.BackColor);
            Assert.AreEqual(double.MaxValue, map.MaximumZoom);
            Assert.AreEqual(0, map.MinimumZoom);

            Assert.AreEqual(GeometryFactory.CreateCoordinate(0, 0), map.Center,
                            "map.Center should be initialized to (0,0)");
            Assert.AreEqual(1000, map.Zoom, "Map zoom should be initialized to 1000.0");
        }

        [Test]
        public void ImageToWorld()
        {
            Map map = new Map(new System.Drawing.Size(1000, 500));
            map.Zoom = 360;
            map.Center = GeometryFactory.CreateCoordinate(0, 0);
            Assert.AreEqual(GeometryFactory.CreateCoordinate(0, 0),
                            map.ImageToWorld(new System.Drawing.PointF(500, 250)));
            Assert.AreEqual(GeometryFactory.CreateCoordinate(-180, 90),
                            map.ImageToWorld(new System.Drawing.PointF(0, 0)));
            Assert.AreEqual(GeometryFactory.CreateCoordinate(-180, -90),
                            map.ImageToWorld(new System.Drawing.PointF(0, 500)));
            Assert.AreEqual(GeometryFactory.CreateCoordinate(180, 90),
                            map.ImageToWorld(new System.Drawing.PointF(1000, 0)));
            Assert.AreEqual(GeometryFactory.CreateCoordinate(180, -90),
                            map.ImageToWorld(new System.Drawing.PointF(1000, 500)));
        }

        [Test]
        public void WorldToImage()
        {
            Map map = new Map(new Size(1000, 500));
            map.Zoom = 360;
            map.Center = GeometryFactory.CreateCoordinate(0, 0);
            Assert.AreEqual(new PointF(500, 250), map.WorldToImage(GeometryFactory.CreateCoordinate(0, 0)));
            Assert.AreEqual(new PointF(0, 0), map.WorldToImage(GeometryFactory.CreateCoordinate(-180, 90)));
            Assert.AreEqual(new PointF(0, 500), map.WorldToImage(GeometryFactory.CreateCoordinate(-180, -90)));
            Assert.AreEqual(new PointF(1000, 0), map.WorldToImage(GeometryFactory.CreateCoordinate(180, 90)));
            Assert.AreEqual(new PointF(1000, 500), map.WorldToImage(GeometryFactory.CreateCoordinate(180, -90)));
        }

        [Test]
        public void GetLayerByName_ReturnCorrectLayer()
        {
            Map map = new Map();
            map.Layers.Add(new SharpMap.Layers.VectorLayer("layer 1"));
            map.Layers.Add(new SharpMap.Layers.VectorLayer("Layer 3"));
            map.Layers.Add(new SharpMap.Layers.VectorLayer("Layer 2"));

            SharpMap.Layers.ILayer layer = map.GetLayerByName("Layer 2");
            Assert.IsNotNull(layer);
            Assert.AreEqual("Layer 2", layer.Name);
        }

        [Test]
        public void GetLayerByName_Indexer()
        {
            Map map = new Map();
            map.Layers.Add(new VectorLayer("Layer 1"));
            map.Layers.Add(new VectorLayer("Layer 3"));
            map.Layers.Add(new VectorLayer("Layer 2"));

            SharpMap.Layers.ILayer layer = map.GetLayerByName("Layer 2");
            Assert.IsNotNull(layer);
            Assert.AreEqual("Layer 2", layer.Name);
        }

        [Test]
        public void FindLayer_ReturnEnumerable()
        {
            Map map = new Map();
            map.Layers.Add(new VectorLayer("Layer 1"));
            map.Layers.Add(new VectorLayer("Layer 3"));
            map.Layers.Add(new VectorLayer("Layer 2"));
            map.Layers.Add(new VectorLayer("Layer 4"));

            int count = 0;
            foreach (ILayer lay in map.FindLayer("Layer 3"))
            {
                Assert.AreEqual("Layer 3", lay.Name);
                count++;
            }
            Assert.AreEqual(1, count);
        }

        [Test]
        public void GetExtents_ValidDatasource()
        {
            Map map = new Map(new System.Drawing.Size(400, 200));
            SharpMap.Layers.VectorLayer vLayer = new SharpMap.Layers.VectorLayer("Geom layer", CreateTestFeatureProvider());
            map.Layers.Add(vLayer);
            IEnvelope box = map.GetExtents();
            Assert.AreEqual(GeometryFactory.CreateEnvelope(0, 50, 0, 346.3493254), box);
        }

        [Test]
        public void GetPixelSize_FixedZoom_Return8_75()
        {
            Map map = new Map(new System.Drawing.Size(400, 200));
            map.Zoom = 3500;
            Assert.AreEqual(8.75, map.PixelSize);
        }

        [Test]
        public void GetMapHeight_FixedZoom_Return1750()
        {
            Map map = new Map(new System.Drawing.Size(400, 200));
            map.Zoom = 3500;
            Assert.AreEqual(1750, map.MapHeight);
        }

        [Test]
        [ExpectedException(typeof (ArgumentException))]
        public void SetMinimumZoom_NegativeValue_ThrowException()
        {
            Map map = new Map();
            map.MinimumZoom = -1;
        }

        [Test]
        [ExpectedException(typeof (ArgumentException))]
        public void SetMaximumZoom_NegativeValue_ThrowException()
        {
            Map map = new Map();
            map.MaximumZoom = -1;
        }

        [Test]
        public void SetMaximumZoom_OKValue()
        {
            Map map = new Map();
            map.MaximumZoom = 100.3;
            Assert.AreEqual(100.3, map.MaximumZoom);
        }

        [Test]
        public void SetMinimumZoom_OKValue()
        {
            Map map = new Map();
            map.MinimumZoom = 100.3;
            Assert.AreEqual(100.3, map.MinimumZoom);
        }

        [Test]
        public void SetZoom_ValueOutsideMax()
        {
            Map map = new Map();
            map.MaximumZoom = 100;
            map.Zoom = 150;
            Assert.AreEqual(100, map.MaximumZoom);
        }

        [Test]
        public void SetZoom_ValueBelowMin()
        {
            Map map = new Map();
            map.MinimumZoom = 100;
            map.Zoom = 50;
            Assert.AreEqual(100, map.MinimumZoom);
        }

        [Test]
        public void ZoomToBox_NoAspectCorrection()
        {
            Map map = new Map(new System.Drawing.Size(400, 200));
            map.ZoomToBox(GeometryFactory.CreateEnvelope(20, 50, 100, 80));
            Assert.AreEqual(GeometryFactory.CreateCoordinate(35, 90), map.Center);
            Assert.AreEqual(40d, map.Zoom);
        }

        [Test]
        public void ZoomToBox_WithAspectCorrection()
        {
            Map map = new Map(new System.Drawing.Size(400, 200));
            map.ZoomToBox(GeometryFactory.CreateEnvelope(10, 20, 100, 180));
            Assert.AreEqual(GeometryFactory.CreateCoordinate(15, 140), map.Center);
            Assert.AreEqual(160d, map.Zoom);
        }

        [Test]
        [ExpectedException(typeof (ApplicationException))]
        public void GetMap_RenderLayerWithoutDatasource_ThrowException()
        {
            Map map = new Map();
            map.Layers.Add(new SharpMap.Layers.VectorLayer("Layer 1"));
            map.Render();
        }

        [Test]
        public void WorldToMap_DefaultMap_ReturnValue()
        {
            Map map = new Map(new System.Drawing.Size(500, 200));
            map.Center = GeometryFactory.CreateCoordinate(23, 34);
            map.Zoom = 1000;
            System.Drawing.PointF p = map.WorldToImage(GeometryFactory.CreateCoordinate(8, 50));
            Assert.AreEqual(new System.Drawing.PointF(242.5f, 92), p);
        }

        [Test]
        public void ImageToWorld_DefaultMap_ReturnValue()
        {
            Map map = new Map(new System.Drawing.Size(500, 200));
            map.Center = GeometryFactory.CreateCoordinate(23, 34);
            map.Zoom = 1000;
            ICoordinate p = map.ImageToWorld(new System.Drawing.PointF(242.5f, 92));
            Assert.AreEqual(GeometryFactory.CreateCoordinate(8, 50), p);
        }

        [Test]
        public void GetMap_GeometryProvider_ReturnImage()
        {
            Map map = new Map(new System.Drawing.Size(400, 200));
            SharpMap.Layers.VectorLayer vLayer = new SharpMap.Layers.VectorLayer("Geom layer", CreateTestFeatureProvider());
            vLayer.Style.Outline = new System.Drawing.Pen(System.Drawing.Color.Red, 2f);
            vLayer.Style.EnableOutline = true;
            vLayer.Style.Line = new System.Drawing.Pen(System.Drawing.Color.Green, 2f);
            vLayer.Style.Fill = System.Drawing.Brushes.Yellow;
            map.Layers.Add(vLayer);

            SharpMap.Layers.VectorLayer vLayer2 = new SharpMap.Layers.VectorLayer("Geom layer 2", vLayer.DataSource);
            vLayer.Style.SymbolOffset = new System.Drawing.PointF(3, 4);
            vLayer.Style.SymbolRotation = 45;
            vLayer.Style.SymbolScale = 0.4f;
            map.Layers.Add(vLayer2);

            SharpMap.Layers.VectorLayer vLayer3 = new SharpMap.Layers.VectorLayer("Geom layer 3", vLayer.DataSource);
            vLayer3.Style.SymbolOffset = new System.Drawing.PointF(3, 4);
            vLayer3.Style.SymbolRotation = 45;
            map.Layers.Add(vLayer3);

            SharpMap.Layers.VectorLayer vLayer4 = new SharpMap.Layers.VectorLayer("Geom layer 4", vLayer.DataSource);
            vLayer4.Style.SymbolOffset = new System.Drawing.PointF(3, 4);
            vLayer4.Style.SymbolScale = 0.4f;
            vLayer4.ClippingEnabled = true;
            map.Layers.Add(vLayer4);

            map.ZoomToExtents();

            var img = map.Render();
            Assert.IsNotNull(img);
        }

        private SharpMap.Data.Providers.IFeatureProvider CreateTestFeatureProvider()
        {
            Collection<IGeometry> geoms = new Collection<IGeometry>();
            geoms.Add(GeometryFromWKT.Parse("POINT EMPTY"));
            geoms.Add(
                GeometryFromWKT.Parse("GEOMETRYCOLLECTION (POINT (10 10), POINT (30 30), LINESTRING (15 15, 20 20))"));
            geoms.Add(
                GeometryFromWKT.Parse("MULTIPOLYGON (((0 0, 10 0, 10 10, 0 10, 0 0)), ((5 5, 7 5, 7 7, 5 7, 5 5)))"));
            geoms.Add(GeometryFromWKT.Parse("LINESTRING (20 20, 20 30, 30 30, 30 20, 40 20)"));
            geoms.Add(
                GeometryFromWKT.Parse("MULTILINESTRING ((10 10, 40 50), (20 20, 30 20), (20 20, 50 20, 50 60, 20 20))"));
            geoms.Add(
                GeometryFromWKT.Parse(
                    "POLYGON ((20 20, 20 30, 30 30, 30 20, 20 20), (21 21, 29 21, 29 29, 21 29, 21 21), (23 23, 23 27, 27 27, 27 23, 23 23))"));
            geoms.Add(GeometryFromWKT.Parse("POINT (20.564 346.3493254)"));
            geoms.Add(GeometryFromWKT.Parse("MULTIPOINT (20.564 346.3493254, 45 32, 23 54)"));
            geoms.Add(GeometryFromWKT.Parse("MULTIPOLYGON EMPTY"));
            geoms.Add(GeometryFromWKT.Parse("MULTILINESTRING EMPTY"));
            geoms.Add(GeometryFromWKT.Parse("MULTIPOINT EMPTY"));
            geoms.Add(GeometryFromWKT.Parse("LINESTRING EMPTY"));

            return new SharpMap.Data.Providers.DataTableFeatureProvider(geoms);
        }


        [Test]
        public void DefaultExtentForVectorLayer()
        {
            var geometry = GeometryFromWKT.Parse("LINESTRING (20 20, 20 30, 30 30, 30 20, 40 20)");
            var provider = new DataTableFeatureProvider(geometry);
            var map = new Map
                          {
                              Layers = {new VectorLayer {DataSource = provider}}
                          };

            Assert.IsTrue(map.GetExtents().Contains(geometry.EnvelopeInternal));
        }

        [Test]
        public void  Clone()
        {
            var map = new Map(new Size(10,100))
                          {
                              Center = GeometryFactory.CreateCoordinate(90, 900)
                          };

            map.Layers.Add(new VectorLayer("Layer 1"));
            map.Layers.Add(new VectorLayer("Layer 3"));
            map.Layers.Add(new VectorLayer("Layer 2"));

            var clonedMap = (Map) map.Clone();
            
            Assert.AreEqual(map.Name, clonedMap.Name);
            Assert.AreEqual(map.Layers.Count, clonedMap.Layers.Count);
            Assert.AreEqual(map.Size.Width, clonedMap.Size.Width);
            Assert.AreEqual(map.Size.Height, clonedMap.Size.Height);
            Assert.AreEqual(map.Center.X, clonedMap.Center.X);
            Assert.AreEqual(map.Center.Y, clonedMap.Center.Y);
            Assert.AreEqual(map.Zoom, clonedMap.Zoom, 1e-10);
        }

        [Test]
        public void AddingALayerShouldCauseZoomToExtendsIfNoValidExtendsBefore()
        {
            var map = new Map(new Size(10, 100))
            {
                Center = GeometryFactory.CreateCoordinate(90, 900)
            };
            
            //now add a layer with defined extends 
            var geometry = GeometryFromWKT.Parse("LINESTRING (20 20, 20 30, 30 30, 30 20, 40 20)");
            var dataSource = new DataTableFeatureProvider(geometry);

            var vectorLayerWithExtends = new VectorLayer("Layer with extends") {DataSource = dataSource};
            map.Layers.Add(vectorLayerWithExtends);
            
            Assert.AreEqual(new Envelope(18,42,-95,145) ,map.Envelope);

        }
    }
}