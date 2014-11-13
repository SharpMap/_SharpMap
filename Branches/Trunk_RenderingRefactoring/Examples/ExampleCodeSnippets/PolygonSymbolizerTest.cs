using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using GeoAPI.Geometries;
using NUnit.Framework;
using SharpMap;
using SharpMap.Data.Providers;
using SharpMap.Layers;
using SharpMap.Layers.Symbolizer;
using SharpMap.Rendering.Symbolizer;
using SharpMap.Utilities;

namespace ExampleCodeSnippets
{
    public class PolygonSymbolizerTest
    {
        private class ModifiedBasicPolygonSymbolizer : BasicPolygonSymbolizer
        {
            private PointStruct _oldRenderOrigin;

            public override void Begin(IGraphics g, Map map, int aproximateNumberOfGeometries)
            {
                base.Begin(g, map, aproximateNumberOfGeometries);
                _oldRenderOrigin = g.RenderingOrigin;
            }
            protected override void OnRenderInternal(Map map, IPolygon polygon, IGraphics g)
            {
                IPoint pt = polygon.Centroid;
                Point renderingOrigin = Point.Truncate(Transform.WorldtoMap(pt.Coordinate, map));
                g.RenderingOrigin = new PointStruct(renderingOrigin.X, renderingOrigin.Y);
                base.OnRenderInternal(map, polygon, g);
            }
            public override void End(IGraphics g, Map map)
            {
                g.RenderingOrigin = _oldRenderOrigin;
            }

        }

        [Test]
        public void TestPlainPolygonSymbolizer()
        {
            ShapeFile provider = new ShapeFile(
                "..\\..\\..\\WinFormSamples\\GeoData\\World\\countries.shp", true);
            PolygonalVectorLayer l = new PolygonalVectorLayer("Countries", provider);
            l.Symbolizer = new ModifiedBasicPolygonSymbolizer
                {
                    Fill = new HatchBrush(
                            HatchStyle.WideDownwardDiagonal, 
                            Color.Red /*,
                            System.Drawing.Color.LightPink*/),
                    UseClipping = false,
                    //Outline = System.Drawing.Pens.AliceBlue
                };

            Map m = new Map(new Size(1440, 1080)) { BackColor = Color.Cornsilk };
            m.Layers.Add(l);

            m.ZoomToExtents();

            Stopwatch sw = new Stopwatch();
            Image img = m.GetMap();
            
            sw.Start();
            img = m.GetMap();
            img.Save("PolygonSymbolizer-1.bmp", ImageFormat.Bmp);
            sw.Stop();
            Console.WriteLine(string.Format("Rendering new method:{0}ms", sw.ElapsedMilliseconds));

            l.Symbolizer = new BasicPolygonSymbolizer()
            {
                Fill = new HatchBrush(
                        HatchStyle.WideDownwardDiagonal,
                        Color.Red/*,
                        System.Drawing.Color.LightPink*/),
                UseClipping = false,
                //Outline = System.Drawing.Pens.AliceBlue
            };

            sw.Reset(); sw.Start();
            img = m.GetMap();
            img.Save("PolygonSymbolizer-2.bmp", ImageFormat.Bmp);
            sw.Stop();
            Console.WriteLine(string.Format("Rendering new method:{0}ms", sw.ElapsedMilliseconds));
        
        }
    }

}