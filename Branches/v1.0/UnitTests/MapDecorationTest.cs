﻿using System.Collections.Generic;
using System.Drawing;
using NUnit.Framework;
using SharpMap;
using SharpMap.Data.Providers;
using SharpMap.Rendering.Decoration;
//using SharpMap.Geometries;
using SharpMap.Rendering.Decoration.ScaleBar;
using SharpMap.Layers;
using GeoPoint = SharpMap.Geometries.Point;

namespace UnitTests
{
    public class TestDecoration : MapDecoration
    {
        protected override Size InternalSize(Graphics g, Map map)
        {
            return new Size(50, 30);
        }

        protected override void OnRender(Graphics g, Map map)
        {
            g.FillRegion(new SolidBrush(OpacityColor(Color.Red)), g.Clip);
        }
    }

    public class MapDecorationTest
    {
        [Test]
        public void TestMapDecorationTest()
        {
            var m = new Map(new Size(780, 540)) {BackColor = Color.White};
            var p = new GeometryProvider(new List<SharpMap.Geometries.Geometry>());
            var pts = new [] {new GeoPoint(0, 0), new GeoPoint(779, 539)};
            var ls = new SharpMap.Geometries.LineString(new List<GeoPoint>(pts));
            p.Geometries.Add(ls);
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
            var bmp = m.GetMap();
            bmp.Save("TestMapDecorationTest.bmp");
        }

    }
}