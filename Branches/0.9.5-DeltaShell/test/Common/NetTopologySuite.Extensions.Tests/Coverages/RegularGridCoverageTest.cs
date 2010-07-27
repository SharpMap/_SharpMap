using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using DelftTools.Functions.Filters;
using DelftTools.Functions.Generic;
using DelftTools.TestUtils;
using DelftTools.Units;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;
using GisSharpBlog.NetTopologySuite.Index.Bintree;
using log4net;
using log4net.Config;
using NetTopologySuite.Extensions.Coverages;
using NUnit.Framework;
using SharpMap;
using SharpMap.Layers;
using SharpMap.Rendering.Thematics;
using SharpMap.Styles;
using SharpMapTestUtils;

namespace NetTopologySuite.Extensions.Tests.Coverages
{
    [TestFixture]
    public class RegularGridCoverageTest
    {
        private static readonly ILog log = LogManager.GetLogger(typeof (RegularGridCoverageTest));

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

        private IRegularGridCoverage CreatePressureCoverage(double offsetX, double offsetY)
        {
            var sizeX = 2;
            var sizeY = 3;
            var deltaX = 100;
            var deltaY = 50;
            IRegularGridCoverage grid2D = new RegularGridCoverage(sizeX, sizeY, deltaX, deltaY, offsetX, offsetY) { Name = "pressure" };

            //fill 2d matrix with values.
            grid2D.SetValues(new[]
                                 {
                                     1.0, 2.0,
                                     3.0, 4.0,
                                     5.0, 6.0
                                 });
            return grid2D;
        }
        
        [Test]
        public void CreateRegularGridCoverageUsingGivenComponentValueType()
        {
            var sizeX = 2; // number of values along x dimension
            var sizeY = 2;
            var deltaX = 1;
            var deltaY = 1;

            var componentValueType = typeof(int);

            var grid = new RegularGridCoverage(sizeX, sizeY, deltaX, deltaY, componentValueType);

            grid.Components.Count
                .Should().Be.EqualTo(1);

            grid.Components[0].ValueType
                .Should().Be.EqualTo(componentValueType);
        }

        [Test]
        public void Initialization()
        {
            IRegularGridCoverage grid2D = CreatePressureCoverage(0.0, 0.0);

            Assert.AreEqual(2, grid2D.Arguments.Count);
            Assert.AreEqual("y", grid2D.Arguments[0].Name);
            Assert.AreEqual("x", grid2D.Arguments[1].Name);

            Assert.IsInstanceOfType(typeof (IPolygon), grid2D.Geometry);

            Assert.AreEqual(grid2D.SizeX, grid2D.X.Values.Count, "number of X argument values");
            Assert.AreEqual(grid2D.SizeY, grid2D.Y.Values.Count, "number of Y argument values");

            //obtain values for all x and for y=50.0 (second row.
            var values = grid2D.GetValues(new VariableValueFilter<double>(grid2D.Y, new[] { 50.0 }));
            Assert.IsTrue(((IEnumerable<double>) values).SequenceEqual(new[] {3.0, 4.0}));

            //obtain values for all x and for y=100.0 (third row.)
            values = grid2D.GetValues(new VariableValueFilter<double>(grid2D.Y, new[] { 100.0 }));
            Assert.IsTrue(((IEnumerable<double>) values).SequenceEqual(new[] {5.0, 6.0}));
        }

        [Test]
        [Category("Performance")]
        public void ResizeIsFast()
        {
            IRegularGridCoverage grid = new RegularGridCoverage();
            var startTime = DateTime.Now;
            grid.Resize(1000, 1000, 1, 1);
            var stoptime = DateTime.Now;
            var dt = (stoptime - startTime).TotalMilliseconds;
            Console.WriteLine(dt);

            Assert.Less(dt, 1550); // <<< DON'T INCREASE UNLESS THERE IS REALLY NO OTHER CHOICE (REQUIREMENTS CHANGED)

            Assert.AreEqual(1000000, grid.Components[0].Values.Count);
            log.InfoFormat("Resized to 1000x1000 in {0}", dt);
            log.InfoFormat("This is still toooooo slow!");
        }


        // create a grid x[0->100], y[0->100], cellsize = [100, 50]
        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void GetCoordinateOutsideBefore()
        {
            IRegularGridCoverage grid2D = CreatePressureCoverage(0.0, 0.0);
            grid2D.GetRegularGridCoverageCellAtPosition(-10.0, 200.0);
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void GetCoordinateOutsideAfter()
        {
            IRegularGridCoverage grid2D = CreatePressureCoverage(0.0, 0.0);
            grid2D.GetRegularGridCoverageCellAtPosition(200.0, 100.0);
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void GetCoordinateOutsideBeforeWithOffset()
        {
            IRegularGridCoverage grid2D = CreatePressureCoverage(1000.0, 0.0);
            grid2D.GetRegularGridCoverageCellAtPosition(990.0, 200.0);
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void GetCoordinateOutsideAfterWithOffset()
        {
            IRegularGridCoverage grid2D = CreatePressureCoverage(1000.0, 0.0);
            grid2D.GetRegularGridCoverageCellAtPosition(1200.0, 100.0);
        }

        [Test]
        public void GetCoordinatesInCoverage()
        {
            IRegularGridCoverage grid2D = CreatePressureCoverage(0.0, 0.0);
            IRegularGridCoverageCell regularGridCoverageCell = grid2D.GetRegularGridCoverageCellAtPosition(0.0, 0.0);
            Assert.AreEqual(0.0, regularGridCoverageCell.X);
            Assert.AreEqual(0.0, regularGridCoverageCell.Y);

            regularGridCoverageCell = grid2D.GetRegularGridCoverageCellAtPosition(50.0, 25.0);
            Assert.AreEqual(0.0, regularGridCoverageCell.X);
            Assert.AreEqual(0.0, regularGridCoverageCell.Y);

            regularGridCoverageCell = grid2D.GetRegularGridCoverageCellAtPosition(150.0, 25.0);
            Assert.AreEqual(100.0, regularGridCoverageCell.X);
            Assert.AreEqual(0.0, regularGridCoverageCell.Y);

            regularGridCoverageCell = grid2D.GetRegularGridCoverageCellAtPosition(199.0, 99.0);
            Assert.AreEqual(100.0, regularGridCoverageCell.X);
            Assert.AreEqual(50.0, regularGridCoverageCell.Y);
        }

        // test with offset and make sure x and y have non overlapping ranges to catch mixing errors
        [Test]
        public void GetCoordinatesInCoverageWithOffset()
        {
            IRegularGridCoverage grid2D = CreatePressureCoverage(1000.0, 0.0);
            IRegularGridCoverageCell regularGridCoverageCell = grid2D.GetRegularGridCoverageCellAtPosition(1000.0, 0.0);
            Assert.AreEqual(1000.0, regularGridCoverageCell.X);
            Assert.AreEqual(0.0, regularGridCoverageCell.Y);

            regularGridCoverageCell = grid2D.GetRegularGridCoverageCellAtPosition(1050.0, 25.0);
            Assert.AreEqual(1000.0, regularGridCoverageCell.X);
            Assert.AreEqual(0.0, regularGridCoverageCell.Y);

            regularGridCoverageCell = grid2D.GetRegularGridCoverageCellAtPosition(1150.0, 25.0);
            Assert.AreEqual(1100.0, regularGridCoverageCell.X);
            Assert.AreEqual(0.0, regularGridCoverageCell.Y);

            regularGridCoverageCell = grid2D.GetRegularGridCoverageCellAtPosition(1199.0, 99.0);
            Assert.AreEqual(1100.0, regularGridCoverageCell.X);
            Assert.AreEqual(50.0, regularGridCoverageCell.Y);
        }

        private static IRegularGridCoverage CreatePressureTimeDependentRegularGridCoverage(out DateTime time1, out DateTime time2)
        {
            IRegularGridCoverage grid2D = new RegularGridCoverage(2, 3, 100, 50)
                                              {
                                                  // default constructor will set x[0] to 0.0 and y[0] to 0.0
                                                  Name = "pressure"
                                              };

            var time = new Variable<DateTime>("time");
            grid2D.Time = time;

            time1 = DateTime.Now;
            var values1 = new[]
                              {
                                  1.0, 2.0,
                                  3.0, 4.0,
                                  5.0, 6.0
                              };
            grid2D.SetValues(values1, new VariableValueFilter<DateTime>(time, time1));
            Assert.AreEqual(6, grid2D.GetValues<double>().Count);

            time2 = time1.AddDays(1);
            var values2 = new[]
                              {
                                  10.0, 20.0,
                                  30.0, 40.0,
                                  50.0, 60.0
                              };
            grid2D.SetValues(values2, new VariableValueFilter<DateTime>(time, time2));

            // resulting dynamic coverage is 
            //                   t = now                  t = now + 1day
            //              x =   0   x = 100             x =   0   x = 100
            //
            // y =   0   |    1.0       2.0        |        10.0       20.0
            // y =  50   |    3.0       4.0        |        30.0       40.0
            // y = 100   |    5.0       6.0        |        50.0       60.0
            //
            //
            return grid2D;
        }

        [Test]
        public void TimeDependentGrid2D()
        {
            DateTime time1;
            DateTime time2;
            IRegularGridCoverage grid2D = CreatePressureTimeDependentRegularGridCoverage(out time1, out time2);

            Assert.IsTrue(grid2D.Time.Values.SequenceEqual(new[] { time1, time2 }));

            // time values are added last. internal array is 1,2,3,4,5,6,10,20,30,40,50,60
            Assert.AreEqual(10.0, grid2D.GetValues<double>()[6]);
            Assert.AreEqual(2.0, grid2D.GetValues<double>()[1]);
            Assert.AreEqual(12, grid2D.GetValues<double>().Count);
        }

        [Test]
        public void TimeDependentGrid2DPieceWiseConstantInterpolation()
        {
            DateTime time1;
            DateTime time2;
            var grid2D = (RegularGridCoverage) CreatePressureTimeDependentRegularGridCoverage(out time1, out time2);

            DateTime timeHalfWay = time1 + new TimeSpan(0, 1, 0, 0);
            grid2D.Time.InterpolationType = ApproximationType.Constant;

            var filtered = grid2D.GetValues<double>(new VariableValueFilter<DateTime>(grid2D.Time, timeHalfWay));

            Assert.AreEqual(1.0, filtered[0] );
        }

        [Test]
        public void TimeDependentGrid2DPieceWiseConstantExtrapolation()
        {
            DateTime time1;
            DateTime time2;
            var grid2D = (RegularGridCoverage)CreatePressureTimeDependentRegularGridCoverage(out time1, out time2);

            DateTime time = time2 + new TimeSpan(0, 1, 0, 0);

            grid2D.Time.InterpolationType = ApproximationType.None;
            grid2D.Time.ExtrapolationType = ApproximationType.Constant;

            var filtered = grid2D.GetValues<double>(new VariableValueFilter<DateTime>(grid2D.Time, time));

            Assert.AreEqual(10.0, filtered[0]);
        }

        [Test]
        public void TimeDependentGridGetTimeSeriesAtKnownLocation()
        {
            DateTime time1;
            DateTime time2;
            IRegularGridCoverage grid2D = CreatePressureTimeDependentRegularGridCoverage(out time1, out time2);

            var timeSeries = grid2D.GetTimeSeries(new Coordinate(100.0, 100.0));
            Assert.AreEqual(1, timeSeries.Arguments.Count);
            Assert.AreEqual(1, timeSeries.Components.Count);
            var values = timeSeries.GetValues();
            Assert.AreEqual(2, values.Count);
            Assert.AreEqual(6.0, values[0]);
            Assert.AreEqual(60.0, values[1]);
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TimeDependentGridGetTimeSeriesAtUnKnownLocation()
        {
            DateTime time1;
            DateTime time2;
            IRegularGridCoverage grid2D = CreatePressureTimeDependentRegularGridCoverage(out time1, out time2);
            grid2D.GetTimeSeries(new Coordinate(75.0, 75.0));
        }

        [Test]
        [NUnit.Framework.Category("Windows.Forms")]
        public void ShowRegularGridWithQuantityThemeOnMap()
        {
            IRegularGridCoverage grid2D = new RegularGridCoverage(200, 200, 10, 10)
                                              {
                                                  Name = "pressure"
                                              };

            double[] values = new double[grid2D.SizeX*grid2D.SizeY];

            double min = 0;
            double max = 0;

            for (int i = 0; i < values.Length; i++)
            {
                values[i] = i;
                if (values[i] > max)
                    max = values[i];
                if (values[i] < min)
                    min = values[i];
            }

            grid2D.SetValues(values);

            var map = new Map();

            var rasterLayer = new RegularGridCoverageLayer {Grid = grid2D};

            var defaultStyle = new VectorStyle {GeometryType = typeof (IPolygon), Line = Pens.SeaGreen};

            double interval = (max - min)/10;
            var intervalList = new List<Interval>();

            double start = min;
            double stop = min + interval;

            for (int i = 0; i < 10; i++)
            {
                intervalList.Add(new Interval(start, stop));
                start = stop;
                stop += interval;
            }

            QuantityTheme quantityTheme = ThemeFactory.CreateQuantityTheme(grid2D.Components[0].Name, defaultStyle,
                                                                           ColorBlend.BlueToGreen, 10, intervalList
                );
            rasterLayer.Theme = quantityTheme;

            map.Layers.Add(rasterLayer);

            MapTestHelper.Show(map);
        }

        [Test]
        public void CheckEnvelope()
        {
            IRegularGridCoverage coverage = new RegularGridCoverage(2, 3, 10, 20)
                                                {
                                                    Name = "test"
                                                };

            Assert.AreEqual(20, coverage.Geometry.Envelope.EnvelopeInternal.MaxX, 1e-8);
            Assert.AreEqual(60, coverage.Geometry.Envelope.EnvelopeInternal.MaxY, 1e-8);
        }

        [Test]
        public void FilterIncludesAllComponents()
        {
            IRegularGridCoverage gridWithTime = new RegularGridCoverage(2, 3, 100, 50)
                                                    {
                                                        Name = "pressure"
                                                    };

            var time = new Variable<DateTime>("time");
            DateTime startTime = DateTime.Now;
            time.Values.Add(startTime);
            gridWithTime.Time = time;
            gridWithTime.Components.Add(new Variable<string>("second"));

            var filteredGrid = gridWithTime.Filter(new VariableValueFilter<DateTime>(time, startTime));
            Assert.AreEqual(2, filteredGrid.Components.Count);
        }

        [Test]
        public void FilterCoverage()
        {
            IRegularGridCoverage gridWithTime = new RegularGridCoverage(2, 3, 100, 50)
                                                    {
                                                        Name = "pressure"
                                                    };

            var time = new Variable<DateTime>("time");
            gridWithTime.Time = time;


            DateTime time1 = DateTime.Now;
            var values1 = new[] {1.0, 2.0, 3.0, 4.0, 5.0, 6.0};
            gridWithTime.SetValues(values1, new VariableValueFilter<DateTime>(time, time1));

            DateTime time2 = time1.AddDays(1);
            var values2 = new[] {10.0, 20.0, 30.0, 40.0, 50.0, 60.0};
            gridWithTime.SetValues(values2, new VariableValueFilter<DateTime>(time, time2));

            var filteredGrid = gridWithTime.FilterAsRegularGridCoverage(new VariableValueFilter<DateTime>(time, time2));
            Assert.AreEqual(6, filteredGrid.Components[0].Values.Count);

            Assert.AreEqual(gridWithTime.Store, filteredGrid.Store);
            var sizeX = gridWithTime.SizeX;
            var filteredSizeX = filteredGrid.SizeX;
            Assert.AreEqual(sizeX, filteredSizeX);
            Assert.AreEqual(gridWithTime.SizeY, filteredGrid.SizeY);
            Assert.AreEqual(gridWithTime.DeltaX, filteredGrid.DeltaX);
            Assert.AreEqual(gridWithTime.DeltaY, filteredGrid.DeltaY);
            Assert.AreEqual(gridWithTime.Arguments.Count, filteredGrid.Arguments.Count);
            Assert.AreEqual(gridWithTime.Components.Count, filteredGrid.Components.Count);
            Assert.AreEqual(gridWithTime.Geometry, filteredGrid.Geometry);
        }

        [Test]
        public void RegularGridResize()
        {
            IRegularGridCoverage regularGridCoverage = new RegularGridCoverage();
            regularGridCoverage.Resize(100, 50, 10, 20);
            Assert.AreEqual(100, regularGridCoverage.X.Values.Count);
            Assert.AreEqual(0, regularGridCoverage.X.Values[0]);
            Assert.AreEqual(10, regularGridCoverage.X.Values[1]);
            Assert.AreEqual(990, regularGridCoverage.X.Values[99]);

            Assert.AreEqual(50, regularGridCoverage.Y.Values.Count);
            Assert.AreEqual(0, regularGridCoverage.Y.Values[0]);
            Assert.AreEqual(20, regularGridCoverage.Y.Values[1]);
            Assert.AreEqual(980, regularGridCoverage.Y.Values[49]);
        }

        [Test]
        public void Clone()
        {
            IRegularGridCoverage coverage = new RegularGridCoverage(2, 3, 10, 20, 5, 60);
            coverage.Components[0].NoDataValues.Add(-1.0d);
            coverage.Components[0].DefaultValue = 100;
            coverage.Components[0].Name = "values";
            coverage.Components[0].Unit = new Unit("meter", "m");

            var clonedCoverage = (IRegularGridCoverage) coverage.Clone();

            Assert.AreEqual(2, clonedCoverage.SizeX);
            Assert.AreEqual(3, clonedCoverage.SizeY);
            Assert.AreEqual(10, clonedCoverage.DeltaX);
            Assert.AreEqual(20, clonedCoverage.DeltaY);
            Assert.AreEqual(coverage.Components[0].Name, clonedCoverage.Components[0].Name);
            Assert.AreEqual(1, clonedCoverage.Components[0].NoDataValues.Count);
            Assert.AreEqual(100, clonedCoverage.Components[0].DefaultValue);
            Assert.AreEqual("meter", clonedCoverage.Components[0].Unit.Name);
            Assert.AreEqual(5, clonedCoverage.Origin.X);
            Assert.AreEqual(60, clonedCoverage.Origin.Y);
        }

        [Test]
        public void CloneTimeDependent()
        {
            var t1 = DateTime.Now;
            var t2 = t1.AddDays(1);

            IRegularGridCoverage coverage = new RegularGridCoverage(2, 2, 10, 20) {IsTimeDependent = true};
            coverage.SetValues(new[] {1, 2, 3, 4}, new VariableValueFilter<DateTime>(coverage.Time, t1));
            coverage.SetValues(new[] {10, 20, 30, 40}, new VariableValueFilter<DateTime>(coverage.Time, t2));

            var clonedCoverage = (IRegularGridCoverage) coverage.Clone();

            Assert.AreEqual(clonedCoverage.Time.Values.Count, coverage.Time.Values.Count);
        }

        [Test]
        public void CopyConstructor()
        {
            IRegularGridCoverage coverage = new RegularGridCoverage(2, 3, 10, 20);
            coverage.Components[0].NoDataValues.Clear();
            coverage.Components[0].NoDataValues.Add(-1.0d);
            coverage.Components[0].DefaultValue = 100;
            coverage.Components[0].Unit = new Unit("meter", "m");
            coverage.IsTimeDependent = true;
            var copy = new RegularGridCoverage(coverage);

            Assert.AreEqual(2, copy.SizeX);
            Assert.AreEqual(3, copy.SizeY);
            Assert.AreEqual(10, copy.DeltaX);
            Assert.AreEqual(20, copy.DeltaY);
            Assert.AreEqual(1, copy.Components[0].NoDataValues.Count);
            Assert.AreEqual(100, copy.Components[0].DefaultValue);
            Assert.AreEqual("meter", copy.Components[0].Unit.Name);

            Assert.IsTrue(coverage.X.Values.SequenceEqual(copy.X.Values));
            Assert.IsTrue(coverage.Y.Values.SequenceEqual(copy.Y.Values));
            Assert.IsTrue(copy.X == copy.Arguments[2]);
            Assert.IsTrue(copy.Y == copy.Arguments[1]);
            Assert.IsTrue(copy.Time == copy.Arguments[0]);

        }


        [Test]
        public void Resize()
        {
            var originalGrid = new RegularGridCoverage(2, 2, 5, 5);
            originalGrid.Resize(2, 4, 8, 16, new Coordinate(10, 100));
            Assert.AreEqual(2, originalGrid.SizeX);
            Assert.AreEqual(4, originalGrid.SizeY);
            Assert.AreEqual(8, originalGrid.DeltaX);
            Assert.AreEqual(16, originalGrid.DeltaY);
            Assert.AreEqual(10, originalGrid.X.Values[0]);
            Assert.AreEqual(100, originalGrid.Y.Values[0]);
        }

        [Test]
        public void ReAddComponent()
        {
            IRegularGridCoverage coverage = new RegularGridCoverage(2, 3, 10, 20);
            coverage.Components.RemoveAt(0);
            coverage.Components.Add(new Variable<double>());
            Assert.AreEqual(4, coverage.Store.Functions.Count);
        }

        [Test]
        public void AggregateInTime()
        {
            var grid2D = new RegularGridCoverage(2, 3, 100, 50)
                             {
                                 Name = "pressure"
                             };

            var time = new Variable<DateTime>("time");
            grid2D.Time = time;

            DateTime time1 = DateTime.Now;
            var values1 = new[]
                              {
                                  1.0, 1.0,
                                  1.0, 1.0,
                                  50.0, 60.0
                              };
            grid2D.SetValues(values1, new VariableValueFilter<DateTime>(time, time1));
            Assert.AreEqual(6, grid2D.GetValues<double>().Count);

            DateTime time2 = time1.AddDays(1);
            var values2 = new[]
                              {
                                  1.0, 1.0,
                                  4.0, 4.0,
                                  50.0, 60.0
                              };
            grid2D.SetValues(values2, new VariableValueFilter<DateTime>(time, time2));

            Func<int, double, int> aggregator = ((seed, h) => (h > 3) ? (seed + 1) : seed);
            var result = (IRegularGridCoverage) grid2D.Aggregate(time, 0, aggregator);

            var aggregatedValues = (IEnumerable<int>) result.Components[0].GetValues();
            Assert.IsTrue(new[] {0, 0, 1, 1, 2, 2}.SequenceEqual(aggregatedValues));
        }

        [Test]
        public void SelectOnlySpecificValuesUsingAggregationFilter() // TODO: migrate to fit tests
        {
            IRegularGridCoverage grid2D = new RegularGridCoverage(4, 4, 1, 1);

            var values = new[,]
                             {
                                 {1.0, 2.0, 3.0, 4.0},
                                 {1.0, 2.0, 3.0, 4.0},
                                 {1.0, 2.0, 3.0, 4.0},
                                 {1.0, 2.0, 3.0, 4.0},
                                 //    ^         ^
                             };

            grid2D.SetValues(values);

            var subSelection = grid2D.GetValues<double>(new VariableAggregationFilter(grid2D.X, 2, 0, 3));

            Assert.IsTrue(subSelection.SequenceEqual(new[] {1.0, 3.0, 1.0, 3.0, 1.0, 3.0, 1.0, 3.0}));
        }

        [Test]
        public void SelectOnlySpecificValuesUsingAggregationFilterFromBottom() // TODO: migrate to fit tests
        {
            IRegularGridCoverage grid2D = new RegularGridCoverage(4, 4, 1, 1);

            var values = new[,]
                             {
                                 {1.1, 2.1, 3.1, 4.1},
                                 {1.2, 2.2, 3.2, 4.2},
                                 {1.3, 2.3, 3.3, 4.3}, // <-
                                 {1.4, 2.4, 3.4, 4.4}, // <-
                             };

            grid2D.SetValues(values);

            var subSelection = grid2D.GetValues<double>(new VariableAggregationFilter(grid2D.Y, 1, 2, 3));

            Assert.IsTrue(subSelection.SequenceEqual(new[] {1.3, 2.3, 3.3, 4.3, 1.4, 2.4, 3.4, 4.4}));
        }

        [Test]
        public void ClearResizesToZeroZero()
        {
            IRegularGridCoverage grid2D = new RegularGridCoverage(4, 4, 1, 1);
            grid2D.Clear();

            Assert.AreEqual(0, grid2D.SizeX);
            Assert.AreEqual(0, grid2D.SizeY);
        }

    }
}