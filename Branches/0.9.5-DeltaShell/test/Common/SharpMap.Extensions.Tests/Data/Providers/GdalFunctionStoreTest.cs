using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DelftTools.Functions.Filters;
using DelftTools.Functions.Generic;
using DelftTools.Functions.DelftTools.Utils.Tuples;
using DelftTools.TestUtils;
using GeoAPI.Extensions.Coverages;
using GisSharpBlog.NetTopologySuite.Geometries;

using SharpTestsEx;

using log4net;
using NetTopologySuite.Extensions.Coverages;
using NUnit.Framework;
using SharpMap.Extensions.Data.Providers;

namespace SharpMap.Extensions.Tests.Data.Providers
{
    [TestFixture]
    public class GdalFunctionStoreTest
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(GdalFunctionStoreTest));

        private readonly string dataPath =
            TestHelper.GetTestDataPath(TestDataPath.DeltaShell.DeltaShellDeltaShellPluginsSharpMapGisTests);
            
        private readonly string rasterDataPath =
            TestHelper.GetTestDataPath(TestDataPath.DeltaShell.DeltaShellDeltaShellPluginsSharpMapGisTestsRasterData);
           
        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            //LogHelper.ConfigureLogging();
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            LogHelper.ResetLogging();
        }

        [Test]
        [ExpectedException(typeof(FileNotFoundException))]
        public void OpenStoreWithNullReferenceFileThroughOpenMethodShouldNotThrow()
        {
            var functionStore = new GdalFunctionStore();
            functionStore.Open(null);
        }


        [Test]
        [ExpectedException(typeof(FileNotFoundException))]
        public void OpenStoreWithNotExistingFileOnInvokingMethodOpenThrows()
        {
            const string path = "someinvalidfile.bil";
            var functionStore = new GdalFunctionStore();
            functionStore.Open(path);
        }

        [Test, Category(TestCategory.DataAccess)]
        public void CheckDataTypeShouldBeFloat()
        {
            
            var path = rasterDataPath + "Schematisatie.bil";

            var functionStore = new GdalFunctionStore();
            functionStore.Open(path);

            var grid = (IRegularGridCoverage)functionStore.Functions.First(f => f is IRegularGridCoverage);
            Assert.AreEqual(typeof(float),grid.Components[0].ValueType);
        }

        /// <summary>
        /// 16 bit integer values are read as Int32 type.
        /// </summary>
        [Test,Category(TestCategory.DataAccess)]
        public void ReadNominalBilFile()
        {
            string path = rasterDataPath + "NominalMap.bil";
            var functionStore = new GdalFunctionStore();
            functionStore.Open(path);
            var grid = (IRegularGridCoverage)functionStore.Functions.First(f => f is IRegularGridCoverage);
            Assert.AreEqual(typeof(Int32), grid.Components[0].ValueType);

            var expectedValues = new Int32[]
                                     {
                                         3, 3, 3, 4, -999, -999,
                                         3, 3, 3, 4, 4, -999,
                                         3, 3, 3, 3, 3, 3,
                                         1, 1, 1, 2, 2, 2,
                                         1, 1, 1, 2, 2, 2,
                                         1, 1, 1, 2, 2, 2
                                     };
            Assert.IsTrue(expectedValues.SequenceEqual<Int32>(grid.Components[0].GetValues<Int32>()));
        
          
        }



        [Test,Category(TestCategory.DataAccess)]
        public void GetInterpolatedValue()
        {
            string path = rasterDataPath + "SchematisatieInt.bil";

            var functionStore = new GdalFunctionStore();
            functionStore.Open(path);
            var grid = (IRegularGridCoverage) functionStore.Functions.First(f => f is IRegularGridCoverage);
            grid.X.InterpolationType = InterpolationType.Constant;
            grid.X.ExtrapolationType = ExtrapolationType.Constant;
            grid.Y.InterpolationType = InterpolationType.Constant;
            grid.Y.ExtrapolationType = ExtrapolationType.Constant;
            var values = new double[] {1, 2, 3, 4, 5, 6, 7, 8, 9};
            var index = 0;

            for (int j = 0; j < 3; j++)
            {
                for (int i = 0; i < 3; i++)
                {
                    var x = 10.0 + 20*i;
                    var y = 50.0 - 20*j;

                    var value = grid.Evaluate<int>(new VariableValueFilter<double>(grid.X, new[] {x}),
                                                          new VariableValueFilter<double>(grid.Y, new[] { y }));
                    Assert.AreEqual(values[index], value);
                    index++;
                }
            }
        }


        [Test]
        [Category(TestCategory.DataAccess)]
        public void ReadBilFileWithIntegerValues()
        {
            string path = rasterDataPath + "SchematisatieInt.bil";
            string localPath = "SchematisatieInt.bil";
            File.Copy(path, localPath, true);
            File.Copy(path.Replace(".bil", ".hdr"), localPath.Replace(".bil", ".hdr"), true);
            var functionStore = new GdalFunctionStore();
            functionStore.Open(localPath);
            var grid = (IRegularGridCoverage) functionStore.Functions.First(f => f is IRegularGridCoverage);
            Assert.AreEqual(typeof (int), grid.Components[0].ValueType);
            Assert.IsTrue(new[] { 7, 8, 9, 4, 5, 6, 1, 2, 3 }.SequenceEqual(functionStore.GetVariableValues<int>(grid.Components[0])));
            Assert.AreEqual(3, grid.X.Values.Count);
            Assert.IsTrue(new double[] {0, 20, 40}.SequenceEqual(grid.X.Values));
            Assert.IsTrue(new double[] {0, 20, 40}.SequenceEqual(grid.Y.Values));
            Assert.AreEqual(0, grid.Origin.X);
            Assert.AreEqual(0, grid.Origin.Y);
            //update some component values
            grid.SetValues(new int[] { 0, 0, 0 }, new VariableValueFilter<double>(grid.X, new double[] { 0 }));
            Assert.IsTrue(new[] { 0, 8, 9, 0, 5, 6, 0, 2, 3 }.SequenceEqual(functionStore.GetVariableValues<int>(grid.Components[0])));
        }


        [Test, Category(TestCategory.DataAccess)]
        public void CreateBilFileFromRegularGrid()
        {
            var gdalFunctionStore = new GdalFunctionStore();

            var gridCoverage = new RegularGridCoverage();

            gridCoverage.Components.Clear();
            var newComponent = new Variable<float>();
            gridCoverage.Components.Add(newComponent);


            gridCoverage.Resize(3, 2, 10, 20, new Coordinate(200, 2000));

            //create a sequence of numbers: 1 , 2, 3, 4, 5, 6,
            var originalValues = new float[] {1, 2, 3, 4, 5, 6};
            gridCoverage.SetValues(originalValues);
            string path = TestHelper.GetCurrentMethodName() + ".bil";

            //TODO: get rid of this strang method.
            //do store.CreateNew(path)
            //   store.Functions.Add(gridCoverage)
            //this is more like the other stores
            gdalFunctionStore.CreateNew(path);
            gdalFunctionStore.Functions.Add(gridCoverage);


            var clone = gdalFunctionStore.Grid;
            Assert.AreEqual(gdalFunctionStore, clone.Store);
            Assert.IsTrue(originalValues.SequenceEqual(gdalFunctionStore.GetVariableValues<float>(clone.Components[0])));
            //replace data in first column
            clone.SetValues(new float[] { 0, 0 }, new VariableValueFilter<double>(clone.X, new double[] { 200 }));
            Assert.IsTrue(new float[] {0, 2, 3, 0, 5, 6}.SequenceEqual(gdalFunctionStore.GetVariableValues<float>(clone.Components[0])));

            Assert.AreEqual(2000, clone.Origin.Y);
            Assert.AreEqual(200, clone.Origin.X);
            Assert.AreEqual(3, clone.SizeX);
            Assert.AreEqual(2, clone.SizeY);
            Assert.AreEqual(10, clone.DeltaX);
            Assert.AreEqual(20, clone.DeltaY);
            gdalFunctionStore.Close();


            gdalFunctionStore = new GdalFunctionStore();
            //reread file to see wether it contains the right data.
            gdalFunctionStore.Open(path);
            var grid = (IRegularGridCoverage) gdalFunctionStore.Functions.First(f => f is IRegularGridCoverage);
            Assert.IsTrue(new float[] {0, 2, 3, 0, 5, 6}.SequenceEqual(gdalFunctionStore.GetVariableValues<float>(grid.Components[0])));
            Assert.AreEqual(2000, grid.Origin.Y);
            Assert.AreEqual(200, grid.Origin.X);
            Assert.AreEqual(3, grid.SizeX);
            Assert.AreEqual(2, grid.SizeY);
            Assert.AreEqual(10, grid.DeltaX);
            Assert.AreEqual(20, grid.DeltaY);

        }

        [Test, Category(TestCategory.DataAccess)]
        public void CreateTiffFileFromRegularGrid()
        {
            var gdalFunctionStore = new GdalFunctionStore();


            var gridCoverage = new RegularGridCoverage();
            gridCoverage.Resize(3, 2, 10, 20, new Coordinate(200, 2000));
            gridCoverage.Components.RemoveAt(0);
            gridCoverage.Components.Add(new Variable<float>());


            var inputData = new float[] {1, 2, 3, 4, 5, 6};
            gridCoverage.SetValues(inputData);
            const string path = "CreateTiffFileFromRegularGrid.tiff";
            gdalFunctionStore.CreateNew(path);
            gdalFunctionStore.Functions.Add(gridCoverage);
            gdalFunctionStore.Close();

            gdalFunctionStore = new GdalFunctionStore();
            //reread file to see wether it contains the right data.
            gdalFunctionStore.Open(path);
            var grid = (IRegularGridCoverage) gdalFunctionStore.Functions.First(f => f is IRegularGridCoverage);
            Assert.IsTrue(inputData.SequenceEqual(gdalFunctionStore.GetVariableValues<float>(grid.Components[0])));
            Assert.AreEqual(gridCoverage.Origin.Y, grid.Origin.Y);
            Assert.AreEqual(gridCoverage.Origin.X, grid.Origin.X);
            Assert.AreEqual(gridCoverage.SizeX, grid.SizeX);
            Assert.AreEqual(gridCoverage.SizeY, grid.SizeY);
            Assert.AreEqual(gridCoverage.DeltaX, grid.DeltaX);
            Assert.AreEqual(gridCoverage.DeltaY, grid.DeltaY);
            //updating tiff file
            //values before: 2,5
            
            // TODO: make it readable!
            Assert.IsTrue((new float[] {2, 5}).SequenceEqual(gdalFunctionStore.GetVariableValues<float>(grid.Components[0],
                                                                                              new VariableValueFilter<double>(
                                                                                                  grid.X,
                                                                                                  new double[] {210}))));
            grid.SetValues(new[] {0, 10.5f}, new VariableValueFilter<double>(grid.X, new double[] {210}));
            //values after: 0,10
            Assert.IsTrue(
                (new[] {0, 10.5f}).SequenceEqual(gdalFunctionStore.GetVariableValues<float>(grid.Components[0],
                                                                                            new VariableValueFilter<double>(grid.X,
                                                                                                                            new double[]
                                                                                                                                {210}))));
        }

        //private ILog log = LogManager.GetLogger(typeof (GdalFunctionStoreTest));
        [Test, Category(TestCategory.DataAccess)]
        public void CreateAscFileFromRegularGridUsingFloatValueType()
        {
            var gridCoverage = new RegularGridCoverage();
           
            gridCoverage.Components.Clear();
            gridCoverage.Components.Add(new Variable<float>());

            gridCoverage.Resize(3, 3, 20, 20, new Coordinate(50, 70));

            var inputData = new[] {1.0f, 2.0f, 3.5f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f, 9.0f};
            gridCoverage.SetValues(inputData);

            var path = TestHelper.GetCurrentMethodName() + ".asc";

            var store = new GdalFunctionStore();
            store.CreateNew(path);
            store.Functions.Add(gridCoverage);
            store.Close();
            
            // reread file to see wether it contains the right data.
            store = new GdalFunctionStore();
            store.Open(path);
            var grid = store.Grid;

            grid.Components[0].GetValues<float>()
                .Should().Have.SameSequenceAs(new float[] {1.0f, 2.0f, 3.5f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f, 9.0f});

            Assert.AreEqual(gridCoverage.Origin.Y, grid.Origin.Y);
            Assert.AreEqual(gridCoverage.Origin.X, grid.Origin.X);
            Assert.AreEqual(gridCoverage.SizeX, grid.SizeX);
            Assert.AreEqual(gridCoverage.SizeY, grid.SizeY);
            Assert.AreEqual(gridCoverage.DeltaX, grid.DeltaX);
            Assert.AreEqual(gridCoverage.DeltaY, grid.DeltaY);
        }

        [Test]
        [Category(TestCategory.DataAccess)]
        public void GetValuesWithVariableValueFilters()
        {
            string fileName = rasterDataPath + "SchematisatieInt.bil";
            var functionStore = new GdalFunctionStore();
            functionStore.Open(fileName);
            var grid = (IRegularGridCoverage) functionStore.Functions.First(f => f is IRegularGridCoverage);
            IList<int> values = grid.GetValues<int>(
                new VariableValueFilter<double>(grid.X, grid.X.Values[0]),
                new VariableValueFilter<double>(grid.Y, grid.Y.Values[0])
                );
            Assert.AreEqual(1, values.Count);
            Assert.AreEqual(7, values[0]);

            values = grid.GetValues<int>(
                new VariableValueFilter<double>(grid.X, grid.X.Values[0]));
            Assert.AreEqual(3, values.Count);

            values = grid.GetValues<int>(
                new VariableValueFilter<double>(grid.X, grid.X.Values[1]),
                new VariableValueFilter<double>(grid.Y, grid.Y.Values[1])
                );
            Assert.AreEqual(5, values[0]);
        }

        [Test]
        [Category(TestCategory.DataAccess)]
        public void GetInterpolatedValue1()
        {
            string fileName = rasterDataPath + "SchematisatieInt.bil";

            var functionStore = new GdalFunctionStore();
            functionStore.Open(fileName);
            var grid = (IRegularGridCoverage)functionStore.Functions.First(f => f is IRegularGridCoverage);

            grid.Evaluate<int>(new VariableValueFilter<double>(grid.X, new double[] { 0.5 }), new VariableValueFilter<double>(grid.Y, new double[] { 0.5 }));

        }




        [Test]
        [Category(TestCategory.DataAccess)]
        public void GetValuesWithVariableIndexRangeFilters()
        {
            string fileName = rasterDataPath + "SchematisatieInt.bil";

            var functionStore = new GdalFunctionStore();
            functionStore.Open(fileName);
            var grid = (IRegularGridCoverage) functionStore.Functions.First(f => f is IRegularGridCoverage);

            IMultiDimensionalArray<int> values = grid.GetValues<int>(
                new VariableIndexRangeFilter(grid.X, 1, 2),
                new VariableIndexRangeFilter(grid.Y, 1, 2)
                );

            //7  8  9
            //4 *5 *6
            //1 *2 *3
            Assert.AreEqual(4, values.Count);
            Assert.AreEqual(5, values[0, 0]);
            Assert.AreEqual(6, values[0, 1]);
            Assert.AreEqual(2, values[1, 0]);
            Assert.AreEqual(3, values[1, 1]);
        }

        [Test]
        [Category(TestCategory.DataAccess)]
        public void GetValuesWithSampleFilters()
        {
             //     1     2      3      4      5      6       7     8      9
             //    10     11*    12     13     14*    15     16     17*    18
             //    19     20     21     22     23     24     25     26     27
             //    28     29     30     31     32     33     34     35     36
             //    37     38*    39     40     41*    42     43     44*    45
             //    46     47     48     49     50     51     52     53     54
             //    55     56     57     58     59     60     61     62     63
             //    64     65*    66     67     68*    69     70     71*    72
             //    73     74     75     76     77     78     79     80     81

            string fileName = rasterDataPath + "SchematisatieInt9x9.asc";

            var functionStore = new GdalFunctionStore();
            functionStore.Open(fileName);
            var grid = (IRegularGridCoverage)functionStore.Functions.First(f => f is IRegularGridCoverage);

            var sampleSize = 3;
            IMultiDimensionalArray<int> values = grid.GetValues<int>(
                new VariableAggregationFilter(grid.X, 4,0,grid.X.Values.Count-1),
                new VariableAggregationFilter(grid.Y, 4, 0, grid.Y.Values.Count - 1)
                );

            Assert.AreEqual(sampleSize * sampleSize, values.Count);
            //Assert.AreEqual(new[]{11,14,17,38,41,44,65,68,71}, values.ToArray());
            //Lower Left Corner Based
            Assert.AreEqual(new[] { 65, 68, 71, 38, 41, 44, 11, 14, 17 }, values.ToArray());
        }


        [Test]
        [Category(TestCategory.DataAccess)]
        public void GetRegularGridCoverageFromGdalStore()
        {
            string fileName = rasterDataPath + "SchematisatieInt.bil";

            var functionStore = new GdalFunctionStore();
            functionStore.Open(fileName);
            var grid =
                (IRegularGridCoverage) functionStore.Functions.FirstOrDefault(f => f is IRegularGridCoverage);

            Assert.AreEqual(20, grid.DeltaX);
            Assert.AreEqual(20, grid.DeltaY);

            Assert.AreEqual(3, grid.SizeX);
            Assert.AreEqual(3, grid.SizeY);

            Assert.AreEqual(3, grid.X.Values.Count);
            Assert.AreEqual(3, grid.X.Values.Count);
        }
        
        [Test]
        [Category(TestCategory.DataAccess)]
        public void MultiRasterBandTest()
        {
            string fileName = rasterDataPath + "4band.tif";

            var functionStore = new GdalFunctionStore();
            functionStore.Open(fileName);

            var grid = (IRegularGridCoverage) functionStore.Functions.First(f => f is IRegularGridCoverage);

            //RasterBands to Components should be one to one. Tif -> 4 bands (values per ARGB channel)
            Assert.AreEqual(4, grid.Components.Count);

            var xFilter = new VariableValueFilter<double>(grid.X, grid.X.Values[9]);
            var yFilter = new VariableValueFilter<double>(grid.Y, grid.Y.Values[9]);

            var c1 = grid.Components[0].GetValues(xFilter, yFilter)[0];
            var c2 = grid.Components[1].GetValues(xFilter, yFilter)[0];
            var c3 = grid.Components[2].GetValues(xFilter, yFilter)[0];
            var c4 = grid.Components[3].GetValues(xFilter, yFilter)[0];

            Assert.AreEqual(255, c1);
            Assert.AreEqual(255, c2);
            Assert.AreEqual(255, c3);
            Assert.AreEqual(255, c4);

            // F = (value)(x,y) components - arguments

            /*
             * F........ grid coverage is a vector function
             * value.... grid contains n band variables (components)
             * x........ grid contains 2 variables for x and y
             * y
             */
            Assert.AreEqual(functionStore.Functions.Count, 7,
                            "store should contain 1 function for grid coverage, variable for 1st grid component and variables for x and y arguments");
        }

        [Test]
        [Category(TestCategory.Performance)]
        public void ReadingATifFileShouldBeReasonablyFast()
        {
            Action action = delegate
                                {
                                    string path = dataPath + "dvim3.tif";

                                    var functionStore = new GdalFunctionStore();
                                    functionStore.Open(path);
                                    var grid = functionStore.Grid;

                                    Assert.AreEqual(1317, grid.X.Values.Count);
                                    Assert.AreEqual(1087, grid.Y.Values.Count);
                                    Assert.AreEqual(1431579, grid.Components[0].Values.Count);

                                };

            // run a few times, avoid variability of test results
            for (var i = 0; i < 10; i++)
            {
                action();
            }

            TestHelper.AssertIsFasterThan(10, "Read grid file 1317x1087", action, true, true);
        }



        /// <summary>
        /// nRows 2566
        /// nCols 1843
        /// upper left corner of map
        /// ULXMAP        124854.28
        /// ULYMAP        523505.38
        /// stepsize (m) for both dimensions
        /// Xdim 20
        /// Ydim 20
        /// </summary>
        [Test, Category(TestCategory.DataAccess)]
        public void GetValuesWithVariableValueRangesFilter()
        {
            string path = rasterDataPath + "Bodem.bil";

            var functionStore = new GdalFunctionStore();
            functionStore.Open(path);
            //grid.GetValues()
            var grid = functionStore.Grid;
            
            //create filter for x and y to set xmin, xmax, ymin and ymax (VariableValueRangesFilter<>)
            var xFilter = new VariableValueRangesFilter<double>(grid.X, new[] {new Pair<double, double>(130000, 132000)});
            var yFilter = new VariableValueRangesFilter<double>(grid.Y, new[] { new Pair<double, double>(520000, 522000)});
            
            //get values between 130000-132000 and 526000-528000
            var values = grid.GetValues(xFilter, yFilter);

            //check count
            Assert.AreEqual(10201, values.Count);
        }


        [Test, Category(TestCategory.DataAccess)]
        public void GetValuesWithAggregationFilter()
        {

            //  nRows 2566
            // nCols 1843
            // upper left corner of map
            // ULXMAP        124854.28
            // ULYMAP        523505.38
            // stepsize (m) for both dimensions
            // Xdim 20
            // Ydim 20

            string path = rasterDataPath + "Bodem.bil";

            var functionStore = new GdalFunctionStore();
            functionStore.Open(path);
            //grid.GetValues()
            var grid = functionStore.Grid;

            //create filter for x and y to set xmin, xmax, ymin and ymax (VariableValueRangesFilter<>)
            var xFilter = new VariableAggregationFilter(grid.X,18,0,1782);
            var yFilter = new VariableAggregationFilter(grid.Y,25,0,2475);

            //get values between 130000-132000 and 526000-528000
            var values = grid.GetValues(xFilter, yFilter);

            //check count
            Assert.AreEqual(10000, values.Count);
        }   


        [Test]
        [Category(TestCategory.DataAccess)]
        public void ReadUInt()
        {
            string fileName = rasterDataPath + "SchematisatieUInt.bil";
            var functionStore = new GdalFunctionStore();
            functionStore.Open(fileName);
            var grid = functionStore.Grid;

            grid.Components[0].ValueType
                .Should("Value type stored in the file").Be.EqualTo(typeof (uint));

            var values = functionStore.Grid.Components[0].Values;
            Assert.AreEqual(new[] { 7, 8, 9, 4, 5, 6, 1, 2, 3 }, values);
        }

        [Test]
        [Category(TestCategory.DataAccess)]
        public void WriteCoverageInMap()
        {
            var functionStore = new GdalFunctionStore();
            functionStore.CreateNew("file.map");

            var grid = new RegularGridCoverage(2, 2, 1, 1, typeof(float));
            grid.SetValues(new float[] { 1, 2, 3, 4 });
            
            functionStore.Functions.Add(grid);
        }

        [Test]
        [Category(TestCategory.DataAccess)]
        public void StoreShouldHaveCorrectFunctionsAfterAdd()
        {
            var functionStore = new GdalFunctionStore();
            functionStore.CreateNew(TestHelper.GetCurrentMethodName() + ".bil");

            var grid = new RegularGridCoverage(2, 2, 1, 1, typeof(float));
            grid.SetValues(new float[] { 1, 2, 3, 4 });

            functionStore.Functions.Add(grid);

            functionStore.Functions.Count
                .Should("Added function should be: grid, values, x, y").Be.EqualTo(4);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException), ExpectedMessage = "Value of type System.Double, but expected type System.Single for variable value")]
        [Category(TestCategory.DataAccess)]
        public void StoringDoubleGridInBilFileShouldThrowException()
        {
            var path = TestHelper.GetCurrentMethodName() + ".bil";

            // create store and add grid
            var store = new GdalFunctionStore();
            store.CreateNew(path);

            var grid = new RegularGridCoverage(2, 2, 1, 1, typeof(float));
            grid.SetValues(new double[] {1, 2, 3, 4});

            store.Functions.Add(grid);
        }

        [Test]
        [Category(TestCategory.DataAccess)]
        public void StoreShouldHaveCorrectFunctionsAfterOpen()
        {
            var path = TestHelper.GetCurrentMethodName() + ".tif";

            // create store and add grid
            var store = new GdalFunctionStore();
            store.CreateNew(path);

            var grid = new RegularGridCoverage(2, 2, 1, 1);
            grid.SetValues(new double[] { 1, 2, 3, 4 });

            store.Functions.Add(grid);

            store.Close();

            // re-open store and check if we have correct functions
            var openedStore = new GdalFunctionStore();
            openedStore.Open(path);

            openedStore.Functions.Count
                .Should("Added function should be: grid, values, x, y").Be.EqualTo(4);

            openedStore.Functions.OfType<RegularGridCoverage>().Count()
                .Should("Store should contain a single grid").Be.EqualTo(1);

            var openedGrid = openedStore.Functions.OfType<RegularGridCoverage>().FirstOrDefault();

            openedGrid.SizeX
                .Should().Be.EqualTo(2);

            openedGrid.SizeY
                .Should().Be.EqualTo(2);

            openedGrid.GetValues<double>()
                .Should().Have.SameSequenceAs(new double[] {1, 2, 3, 4});
        }

        [Test]
        [Category(TestCategory.DataAccess)]
        public void OrderOfFunctionsAfterReopenShouldBeTheSame()
        {
            var path = TestHelper.GetCurrentMethodName() + ".tif";
            var store = new GdalFunctionStore();
            store.CreateNew(path);
            var grid = new RegularGridCoverage(2, 2, 1, 1);
            store.Functions.Add(grid);

            // remember indices
            var indexGrid = store.Functions.IndexOf(grid);
            var indexValues = store.Functions.IndexOf(grid.Components[0]);
            var indexX = store.Functions.IndexOf(grid.X);
            var indexY = store.Functions.IndexOf(grid.Y);
            
            store.Close();

            // re-open store and check if we functions are at the same indices
            var openedStore = new GdalFunctionStore();
            openedStore.Open(path);

            var openedGrid = openedStore.Grid;

            openedStore.Functions.IndexOf(openedGrid)
                .Should("index of grid coverage").Be.EqualTo(indexGrid);
            openedStore.Functions.IndexOf(openedGrid.Components[0])
                .Should("index of values component").Be.EqualTo(indexValues);
            openedStore.Functions.IndexOf(openedGrid.X)
                .Should("index of X").Be.EqualTo(indexX);
            openedStore.Functions.IndexOf(openedGrid.Y)
                .Should("index of Y").Be.EqualTo(indexY);
        }

        [Test]
        [Category(TestCategory.DataAccess)]
        public void AddGridShouldAddCorrectFunctionsToStore()
        {
            var store = new GdalFunctionStore();
            store.CreateNew(TestHelper.GetCurrentMethodName() + ".bil");

            var grid = new RegularGridCoverage(2, 2, 1, 1, typeof(float));
            grid.SetValues(new[] { 1f, 2f, 3f, 4f });

            store.Functions.Add(grid);

            grid.Geometry
                .Should().Not.Be.Null();

            store.Functions.Count
                .Should("grid, values, x, y").Be.EqualTo(4);

            store.Functions.OfType<RegularGridCoverage>().FirstOrDefault()
                .Should().Be.EqualTo(grid);
        }

        [Test]
        [Category(TestCategory.DataAccess)]
        public void Clone()
        {
            var path = TestHelper.GetCurrentMethodName() + ".tif";
            
            var store = new GdalFunctionStore();
            store.CreateNew(path);

            var grid = new RegularGridCoverage(2, 2, 1, 1);
            grid.SetValues(new[] { 1.0, 2.0, 3.0, 4.0 });

            store.Functions.Add(grid);

            var clonedStore = (GdalFunctionStore)store.Clone();

            clonedStore.Functions.Count
                .Should().Be.EqualTo(4);

            clonedStore.Path
                .Should().Be.EqualTo(path);

            var clonedGrid = clonedStore.Functions.OfType<RegularGridCoverage>().FirstOrDefault();

            clonedGrid.GetValues<double>()
                .Should().Have.SameSequenceAs(new double[] { 1, 2, 3, 4 });
        }

        [Test]
        [Category(TestCategory.DataAccess)]
        public void CloneFunctionStoredInGdalFunctionStore()
        {
            var path = TestHelper.GetCurrentMethodName() + ".tif";

            var store = new GdalFunctionStore();
            store.CreateNew(path);

            const int deltaX = 1;
            const int deltaY = 10;
            var grid = new RegularGridCoverage(2, 2, deltaX, deltaY);
            grid.SetValues(new[] { 1.0, 2.0, 3.0, 4.0 });

            store.Functions.Add(grid);

            // create grid clone and check if function and cloned store are correct
            var clonedGrid = (RegularGridCoverage)grid.Clone();

            var clonedStore = (GdalFunctionStore)clonedGrid.Store;

            clonedStore.Functions.Count
                .Should().Be.EqualTo(4);

            clonedStore.Path
                .Should().Be.EqualTo(path);

            clonedGrid.GetValues<double>()
                .Should().Have.SameSequenceAs(new double[] { 1, 2, 3, 4 });
        }
        [Test]
        [Category(TestCategory.DataAccess)]
        public void CloneOfExternalBilFile()
        {
            string path = rasterDataPath + "bodem.bil";
            //File.SetAttributes(path,FileAttributes.ReadOnly);
            var store = new GdalFunctionStore();
            store.Open(path);
            
            var grid = store.Grid;
            //action! 
            var clone = (RegularGridCoverage)grid.Clone();
            var cloneStore = clone.Store;
            
            Assert.IsTrue(cloneStore is GdalFunctionStore);
            Assert.AreEqual(path,((GdalFunctionStore)cloneStore).Path);

        }

        [Test]
        [Category(TestCategory.DataAccess)]
        public void CopyToBilFileIncludeHDR()
        {
            string path = rasterDataPath + "SchematisatieInt.bil";
            string hdrPath = rasterDataPath + "SchematisatieInt.hdr";
            string targetPath = "SchematisatieInt.bil";
            string targetHDRPath = "SchematisatieInt.hdr";

            if (File.Exists(targetPath)) File.Delete(targetPath);
            if (File.Exists(targetHDRPath)) File.Delete(targetHDRPath);

            var functionStore = new GdalFunctionStore { Path = path };

            Assert.IsTrue(File.Exists(hdrPath));

            functionStore.CopyTo(targetPath);

            Assert.IsTrue(File.Exists(targetPath));
            Assert.IsTrue(File.Exists(targetHDRPath));

            if (File.Exists(targetPath)) File.Delete(targetPath);
            if (File.Exists(targetHDRPath)) File.Delete(targetHDRPath);
        }

        [Test]
        [Category(TestCategory.DataAccess)]
        public void GetSupportedGdalDriverForBIL()
        {
            string path = TestHelper.GetDataDir()+@"\RasterData\Bodem.bil";
            var driverName = GdalHelper.GetDriverName(path);
            Assert.AreEqual("EHdr", driverName);
        }

        [Test]
        [Category(TestCategory.DataAccess)]
        public void GetSupportedGdalDriverForASC()
        {
            string path = TestHelper.GetDataDir()+@"\RasterData\test.ASC";
            var driverName = GdalHelper.GetDriverName(path);
            Assert.AreEqual("AAIGrid", driverName);
        }

        [Test]
        [Category(TestCategory.Performance)]
        public void GettingCountOfGridComponentsShouldBeReallyFast()
        {
            string path = rasterDataPath + "Bodem.bil";
            var store = new GdalFunctionStore();
            store.Open(path);

            var grid = store.Grid;
            
            TestHelper.AssertIsFasterThan(42,()=>
                                                 {
                                                     for (int i = 0; i < 10000; i++)
                                                         Assert.AreEqual(4729138, store.GetVariableValues<float>(grid.Components[0]).Count);
                                                 } , true);           
        }

        [Test]
        [Category(TestCategory.DataAccess)]
        public void AddingFilteredFunctionShouldResetParent()
        {
            var grid = new RegularGridCoverage(2, 2, 1, 1, typeof(float));

            var filteredGrid = grid.Filter();

            string path = rasterDataPath + "AddingFilteredFunctionShouldResetParent.bil";
            var store = new GdalFunctionStore();
            store.CreateNew(path);

            store.Functions.Add(filteredGrid);

            
        }

        [Test]
        [Category(TestCategory.DataAccess)]
        public void GetDataSetSizeForBIL()
        {
            string path = rasterDataPath + "Bodem.bil";
            var store = new GdalFunctionStore();
            store.Open(path);

            //file is about 18Mb so this work for this on.
            Assert.AreEqual(18916552, store.GetDatasetSize());
        }
    }
}