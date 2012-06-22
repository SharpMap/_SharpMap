﻿namespace UnitTests.Data.Providers
{

    [NUnit.Framework.TestFixture]
    public class ShapeFileProviderTests
    {
        private long _msLineal;
        private long _msVector;

        [NUnit.Framework.TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            GeoAPI.GeometryServiceProvider.Instance = NetTopologySuite.NtsGeometryServices.Instance;
        }

        [NUnit.Framework.TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            System.Console.WriteLine("Speed comparison:");
            System.Console.WriteLine("VectorLayer\tLinealLayer\tRatio");
            System.Console.WriteLine(string.Format("{0}\t{1}\t{2:N}", _msVector, _msLineal,
                                                   ((double)_msLineal / _msVector * 100)));
        }

        private const string ReallyBigShapeFile = "D:\\Daten\\Geofabrik\\roads.shp";

        [NUnit.Framework.Test]
        public void TestPerformanceVectorLayer()
        {
            NUnit.Framework.Assert.IsTrue(System.IO.File.Exists(ReallyBigShapeFile),
                                          "Specified shapefile is not present!");

            var map = new SharpMap.Map(new System.Drawing.Size(1024, 768));

            var shp = new SharpMap.Data.Providers.ShapeFile(ReallyBigShapeFile, false, false);
            var lyr = new SharpMap.Layers.VectorLayer("Roads", shp);

            map.Layers.Add(lyr);
            map.ZoomToExtents();

            System.Console.WriteLine("Rendering Map with " + shp.GetFeatureCount() + " features");            
            System.Console.Write("Rendering 1st time");
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            map.GetMap();
            sw.Stop();
            System.Console.WriteLine(" in " + sw.ElapsedMilliseconds.ToString(System.Globalization.NumberFormatInfo.CurrentInfo) + "ms.");
            System.Console.Write("Rendering 2nd time");
            sw.Restart();
            var res = map.GetMap();
            sw.Stop();
            System.Console.WriteLine(" in " + sw.ElapsedMilliseconds.ToString(System.Globalization.NumberFormatInfo.CurrentInfo) + "ms.");
            _msVector = sw.ElapsedMilliseconds;
            var path = System.IO.Path.ChangeExtension(ReallyBigShapeFile, ".vector.png");
            res.Save(path, System.Drawing.Imaging.ImageFormat.Png);
            System.Console.WriteLine("\nResult saved at file://" + path.Replace('\\', '/'));
        }

        [NUnit.Framework.Test]
        public void TestPerformanceLinealLayer()
        {
            NUnit.Framework.Assert.IsTrue(System.IO.File.Exists(ReallyBigShapeFile),
                                          "Specified shapefile is not present!");

            var map = new SharpMap.Map(new System.Drawing.Size(1024, 768));

            var shp = new SharpMap.Data.Providers.ShapeFile(ReallyBigShapeFile, false, false);
            var lyr = new SharpMap.Layers.Symbolizer.LinealVectorLayer("Roads", shp)
                          {
                              Symbolizer =
                                  new SharpMap.Rendering.Symbolizer.BasicLineSymbolizer
                                      {Line = new System.Drawing.Pen(System.Drawing.Color.Black)}
                          };
            map.Layers.Add(lyr);
            map.ZoomToExtents();

            System.Console.WriteLine("Rendering Map with " + shp.GetFeatureCount() + " features");
            System.Console.Write("Rendering 1st time");
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            map.GetMap();
            sw.Stop();
            System.Console.WriteLine(" in " + sw.ElapsedMilliseconds.ToString(System.Globalization.NumberFormatInfo.CurrentInfo) + "ms.");
            System.Console.Write("Rendering 2nd time");
            sw.Restart();
            var res = map.GetMap();
            sw.Stop();
            _msLineal = sw.ElapsedMilliseconds;
            System.Console.WriteLine(" in " + sw.ElapsedMilliseconds.ToString(System.Globalization.NumberFormatInfo.CurrentInfo) + "ms.");

            var path = System.IO.Path.ChangeExtension(ReallyBigShapeFile, "lineal.png");
            res.Save(path, System.Drawing.Imaging.ImageFormat.Png);
            System.Console.WriteLine("\nResult saved at file://" + path.Replace('\\', '/'));
        }

        [NUnit.Framework.Test]
        public void TestExecuteIntersectionQuery()
        {
            NUnit.Framework.Assert.IsTrue(System.IO.File.Exists(ReallyBigShapeFile),
                                          "Specified shapefile is not present!");

            var shp = new SharpMap.Data.Providers.ShapeFile(ReallyBigShapeFile, false, false);
            shp.Open();

            var fds = new SharpMap.Data.FeatureDataSet();
            var bbox = shp.GetExtents();
            //narrow it down
            bbox.ExpandBy(-0.425*bbox.Width, -0.425*bbox.Height);

            //Just to avoid that initial query does not impose performance penalty
            shp.DoTrueIntersectionQuery = false;
            shp.ExecuteIntersectionQuery(bbox, fds);
            fds.Tables.RemoveAt(0);

            //Perform query once more
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            shp.ExecuteIntersectionQuery(bbox, fds);
            sw.Stop();
            System.Console.WriteLine("Queried using just envelopes:\n" + fds.Tables[0].Rows.Count + " in " +
                                     sw.ElapsedMilliseconds + "ms.");
            fds.Tables.RemoveAt(0);
            
            shp.DoTrueIntersectionQuery = true;
            sw.Restart();
            shp.ExecuteIntersectionQuery(bbox, fds);
            sw.Stop();
            System.Console.WriteLine("Queried using prepared geometries:\n" + fds.Tables[0].Rows.Count + " in " +
                                     sw.ElapsedMilliseconds + "ms.");
        }
    }


}