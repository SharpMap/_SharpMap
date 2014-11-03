using System.Drawing;
using NUnit.Framework;
using SharpMap;
using SharpMap.Data.Providers;
using SharpMap.Layers;
using SharpMap.Rendering.Decoration;
using SharpMap.Rendering.Decoration.ScaleBar;
using GeoPoint = GeoAPI.Geometries.Coordinate;

namespace UnitTests
{
    public class TestDecoration : MapDecoration
    {
        protected override Size InternalSize(IGraphics g, Map map)
        {
            return new Size(50, 30);
        }

        protected override void OnRender(IGraphics g, Map map)
        {
            Brush brush = new SolidBrush(OpacityColor(Color.Red));
            RectangleF rect = g.Clip;
            g.FillRectangle(brush, 
                (int) rect.X, (int) rect.Y, 
                (int) rect.Width, (int) rect.Height);
        }
    }

    public class MapDecorationTest
    {
        [Test]
        public void TestMapDecorationTest()
        {
            Map m = new Map(new Size(780, 540)) {BackColor = Color.White, SRID = 0};
            GeoPoint[] pts = new [] {new GeoPoint(0, 0), new GeoPoint(779, 539)};
            FeatureProvider p = new FeatureProvider(m.Factory.CreateLineString(pts));
            m.Layers.Add(new VectorLayer("t",p));
            m.ZoomToExtents();

            m.Decorations.Add(new TestDecoration
                                  {
                                      Anchor = MapDecorationAnchor.LeftTop,
                                      BorderColor = Color.Green,
                                      BackgroundColor = Color.LightGreen,
                                      BorderWidth = 2,
                                      Location = new Point(10, 10),
                                      BorderMargin = new Size(5, 5),
                                      RoundedEdges = true,
                                      Opacity = 0.6f
                                  });

            m.Decorations.Add(new TestDecoration
            {
                Anchor = MapDecorationAnchor.RightTop,
                BorderColor = Color.Red,
                BackgroundColor = Color.LightCoral,
                BorderWidth = 2,
                Location = new Point(10, 10),
                BorderMargin = new Size(5, 5),
                RoundedEdges = true,
                Opacity = 0.2f
            });

            m.Decorations.Add(new ScaleBar
            {
                Anchor = MapDecorationAnchor.Default,
                BorderColor = Color.Blue,
                BackgroundColor = Color.CornflowerBlue,
                BorderWidth = 2,
                Location = new Point(10, 10),
                BorderMargin = new Size(5, 5),
                RoundedEdges = true,
                BarWidth = 4,
                ScaleText =ScaleBarLabelText.RepresentativeFraction,
                NumTicks = 2,
                Opacity = 1f
            });
            Image bmp = m.GetMap();
            bmp.Save("TestMapDecorationTest.bmp");
        }

    }
}