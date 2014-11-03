using System;
using System.Diagnostics;
using System.Drawing;
using GeoAPI.Features;
using NUnit.Framework;
using SharpMap;
using SharpMap.Data.Providers;
using SharpMap.Layers;
using SharpMap.Rendering.Symbolizer;
using SharpMap.Rendering.Thematics;
using SharpMap.Styles;

namespace ExampleCodeSnippets
{
    public class LineSymbolizerTest
    {
        [Test, Explicit("path to a file not provided")]
        public void TestBasicLineSymbolizer()
        {
            ShapeFile p = new ShapeFile(@"d:\\daten\GeoFabrik\\roads.shp", false);
            VectorLayer l = new VectorLayer("roads", p);
            //l.Style.Outline = new System.Drawing.Pen(System.Drawing.Color.Firebrick, 5);
            l.Style.Line = new Pen(Color.Gold, 1);
            l.Style.EnableOutline = false;
            Map m = new Map(new Size(1440, 1080)) { BackColor = Color.Cornsilk };
            m.Layers.Add(l);

            m.ZoomToExtents();

            Stopwatch sw = new Stopwatch();
            sw.Start();
            m.GetMap();

            sw.Stop();
            Console.WriteLine(string.Format("Rendering old method: {0}ms", sw.ElapsedMilliseconds));
            sw.Reset();
            sw.Start();
            Image bmp = m.GetMap();
            sw.Stop();
            Console.WriteLine(string.Format("Rendering old method: {0}ms", sw.ElapsedMilliseconds));
            bmp.Save("NDSRoads1.bmp");


            CachedLineSymbolizer cls = new CachedLineSymbolizer();
            //cls.LineSymbolizeHandlers.Add(new SharpMap.Rendering.Symbolizer.PlainLineSymbolizeHandler { Line = new System.Drawing.Pen(System.Drawing.Color.Firebrick, 5) });
            cls.LineSymbolizeHandlers.Add(new PlainLineSymbolizeHandler { Line = new Pen(Color.Gold, 1) });

            l.Style.LineSymbolizer = cls;
            sw.Reset(); sw.Start();
            bmp = m.GetMap();
            sw.Stop();
            Console.WriteLine(string.Format("Rendering new method: {0}ms", sw.ElapsedMilliseconds));
            bmp.Save("NDSRoads2.bmp");

        }

        [Test, Explicit("path to a file not provided")]
        public void TestWarpedLineSymbolizer()
        {
            ShapeFile p = new ShapeFile(@"d:\\daten\GeoFabrik\\Aurich\\roads.shp", false);

            VectorLayer l = new VectorLayer("roads", p);

            CachedLineSymbolizer cls = new CachedLineSymbolizer();
            cls.LineSymbolizeHandlers.Add(new PlainLineSymbolizeHandler { Line = new Pen(Color.Gold, 2) });

            WarpedLineSymbolizeHander wls = new WarpedLineSymbolizeHander
                          {
                              Pattern =
                                  WarpedLineSymbolizer.
                                  GetGreaterSeries(3, 3),
                              Line = new Pen(Color.Firebrick, 1)
                              ,
                              Interval = 20
                          };
            cls.LineSymbolizeHandlers.Add(wls);
            l.Style.LineSymbolizer = cls;

            Map m = new Map(new Size(720, 540)) { BackColor = Color.Cornsilk };
            m.Layers.Add(l);

            m.ZoomToExtents();

            Stopwatch sw = new Stopwatch();

            sw.Start();
            Image bmp = m.GetMap();
            sw.Stop();
            Console.WriteLine(string.Format("Rendering new method: {0}ms", sw.ElapsedMilliseconds));
            bmp.Save("AurichRoads1.bmp");

            cls.LineSymbolizeHandlers[1] = new WarpedLineSymbolizeHander
                                               {
                                                   Pattern =
                                                       WarpedLineSymbolizer.
                                                       GetTriangle(4, 0),
                                                   Line = new Pen(Color.Firebrick, 1),
                                                   Fill = new SolidBrush(Color.Firebrick)
                                                   ,
                                                   Interval = 10
                                               };
            sw.Reset(); sw.Start();
            bmp = m.GetMap();
            sw.Stop();
            Console.WriteLine(string.Format("Rendering new method: {0}ms", sw.ElapsedMilliseconds));
            bmp.Save("AurichRoads2-0.bmp");

            cls.LineSymbolizeHandlers[1] = new WarpedLineSymbolizeHander
            {
                Pattern =
                    WarpedLineSymbolizer.
                    GetTriangle(4, 1),
                Line = new Pen(Color.Firebrick, 1),
                Fill = new SolidBrush(Color.Firebrick)
                ,
                Interval = 10
            };
            sw.Reset(); sw.Start();
            bmp = m.GetMap();
            sw.Stop();
            Console.WriteLine(string.Format("Rendering new method: {0}ms", sw.ElapsedMilliseconds));
            bmp.Save("AurichRoads2-1.bmp");
            cls.LineSymbolizeHandlers[1] = new WarpedLineSymbolizeHander
            {
                Pattern =
                    WarpedLineSymbolizer.
                    GetTriangle(4, 2),
                Line = new Pen(Color.Firebrick, 1),
                Fill = new SolidBrush(Color.Firebrick)
                ,
                Interval = 10
            };
            sw.Reset(); sw.Start();
            bmp = m.GetMap();
            sw.Stop();
            Console.WriteLine(string.Format("Rendering new method: {0}ms", sw.ElapsedMilliseconds));
            bmp.Save("AurichRoads2-2.bmp");

            cls.LineSymbolizeHandlers[1] = new WarpedLineSymbolizeHander
            {
                Pattern =
                    WarpedLineSymbolizer.
                    GetTriangle(4, 3),
                Line = new Pen(Color.Firebrick, 1),
                Fill = new SolidBrush(Color.Firebrick)
                ,
                Interval = 10
            };
            sw.Reset(); sw.Start();
            bmp = m.GetMap();
            sw.Stop();
            Console.WriteLine(string.Format("Rendering new method: {0}ms", sw.ElapsedMilliseconds));
            bmp.Save("AurichRoads2-3.bmp");


            //cls.LineSymbolizeHandlers[0] = cls.LineSymbolizeHandlers[1];
            cls.LineSymbolizeHandlers[1] = new WarpedLineSymbolizeHander
                                               {
                                                   Pattern =
                                                       WarpedLineSymbolizer.GetZigZag(4, 4),
                                                   Line = new Pen(Color.Firebrick, 1),
                                                   //Fill = new System.Drawing.SolidBrush(System.Drawing.Color.Firebrick)
                                               };
            sw.Reset(); sw.Start();
            bmp = m.GetMap();
            sw.Stop();
            Console.WriteLine(string.Format("Rendering new method: {0}ms", sw.ElapsedMilliseconds));
            bmp.Save("AurichRoads3.bmp");
        }

        [Test, Explicit("path to a file not provided")]
        public void TestCachedLineSymbolizerInTheme()
        {
            ShapeFile p = new ShapeFile(@"d:\\daten\GeoFabrik\\Aurich\\roads.shp", false);

            VectorLayer l = new VectorLayer("roads", p);
            ClsTheme theme = new ClsTheme(l.Style);
            l.Theme = theme;

            Map m = new Map(new Size(720, 540)) { BackColor = Color.Cornsilk };
            m.Layers.Add(l);

            m.ZoomToExtents();

            Stopwatch sw = new Stopwatch();

            sw.Start();
            Image bmp = m.GetMap();
            sw.Stop();
            Console.WriteLine(string.Format("Rendering new method: {0}ms", sw.ElapsedMilliseconds));
            bmp.Save("AurichRoads1Theme.bmp");
        }

        internal class ClsTheme : ITheme
        {
            private readonly VectorStyle _style;

            public ClsTheme(VectorStyle style)
            {
                _style = style;
            }

            #region Implementation of ITheme

            /// <summary>
            /// Returns the style based on a feature
            /// </summary>
            /// <param name="attribute">Set of attribute values to calculate the <see cref="SharpMap.Styles.IStyle"/> from</param>
            /// <returns>The style</returns>
            public IStyle GetStyle(IFeature attribute)
            {
                VectorStyle res = _style;

                CachedLineSymbolizer cls = new CachedLineSymbolizer();
                cls.LineSymbolizeHandlers.Add(new PlainLineSymbolizeHandler { Line = new Pen(Color.Gold, 2) });

                WarpedLineSymbolizeHander wls = new WarpedLineSymbolizeHander
                {
                    Pattern =
                        WarpedLineSymbolizer.
                        GetGreaterSeries(3, 3),
                    Line = new Pen(Color.Firebrick, 1)
                    ,
                    Interval = 20

                };
                cls.LineSymbolizeHandlers.Add(wls);
                cls.ImmediateMode = true;

                res.LineSymbolizer = cls;

                return res;
            }

            #endregion
        }
    }
}