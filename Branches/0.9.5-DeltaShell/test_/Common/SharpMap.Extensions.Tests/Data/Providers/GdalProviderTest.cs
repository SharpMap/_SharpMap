using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.IO;
using DelftTools.Functions.Filters;
using DelftTools.TestUtils;
using GeoAPI.Extensions.Coverages;
using log4net;
using log4net.Config;
using NUnit.Framework;
using SharpMap.Data.Providers;
using SharpMap.Extensions.Data.Providers;
using SharpMap.Extensions.Layers;
using SharpMap.Layers;
using SharpMap.Rendering;
using SharpMapTestUtils;


namespace SharpMap.Extensions.Tests.Data.Providers
{
    [TestFixture]
    public class GdalProviderTest
    {
        private static readonly ILog log = LogManager.GetLogger(typeof (GdalProviderTest));

        private const string DataPath =
            @"..\..\..\..\..\test-data\DeltaShell\DeltaShell.Plugins.SharpMapGis.Tests\";
        private const string RasterDataPath =
            @"..\..\..\..\..\test-data\DeltaShell\DeltaShell.Plugins.SharpMapGis.Tests\RasterData\";

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

        [Test,Category(TestCategory.DataAccess)]
        public void ReadBinaryRasterFile()
        {
            string fileName = RasterDataPath + "SchematisatieInt.bil";
            var rasterLayer = new RegularGridCoverageLayer();

            var rasterFeatureProvider = new GdalFeatureProvider();
            rasterFeatureProvider.Open(fileName);
            IRegularGridCoverage grid = rasterFeatureProvider.Grid;
            IList<int> values = grid.GetValues<int>();
            Assert.AreEqual(9, values.Count);

            IList<int> values2 = grid.GetValues<int>(
                new VariableValueFilter<double>(grid.X, grid.X.Values[0]),
                new VariableValueFilter<double>(grid.Y, grid.Y.Values[0])
                );
            Assert.AreEqual(1, values2.Count);
            Assert.AreEqual(7, values2[0]);

            //rasterLayer.CustomRenderers.Add(new RegularGridCoverageRenderer()); // add optimized custom gdal renderer
            rasterLayer.DataSource = rasterFeatureProvider;


            IRegularGridCoverage regularGridCoverage = rasterFeatureProvider.Grid;
        }


        [Test]
        [Category(TestCategory.WindowsForms)]
        public void RenderBilFileAsCoverage()
        {
            var map = new Map(new Size(400, 200)) {Name = "map1"};

            const string fileName = RasterDataPath + "SchematisatieInt.bil";
            var rasterFeatureProvider = new GdalFeatureProvider { Path = fileName };

            var rasterLayer = new RegularGridCoverageLayer();

            // add optimized custom gdal renderer
            rasterLayer.CustomRenderers.Add(new RegularGridCoverageRenderer(rasterLayer));
                
            rasterLayer.DataSource = rasterFeatureProvider;

            map.Layers.Add(rasterLayer);

            IRegularGridCoverage grid = rasterFeatureProvider.Grid;
            IList<int> values = grid.GetValues<int>();
            Assert.AreEqual(9, values.Count);

            MapTestHelper.Show(map);
        }

        [Test]
        [Category(TestCategory.WindowsForms)]
        public void RenderAscFileAsCoverage()
        {
            var map = new Map(new Size(10, 20)) {Name = "map1"};

            string fileName = RasterDataPath + "test.ASC";
           // const string fileName = @"D:\habitat\boomkikker_a_huidig.asc";

            var newFileName = Path.GetFullPath(Path.GetFileName(fileName));
            File.Copy(Path.GetFullPath(fileName), newFileName, true);
            var fileInfo = new FileInfo(newFileName);
            if (fileInfo.IsReadOnly)
            {
                fileInfo.IsReadOnly = false;
            }

            var rasterLayer = new RegularGridCoverageLayer();

            var rasterFeatureProvider = new GdalFeatureProvider {Path = fileName};

            //rasterLayer.CustomRenderers.Add(new GdalRenderer()); // add optimized custom gdal renderer
            rasterLayer.CustomRenderers.Add(new RegularGridCoverageRenderer(rasterLayer));
            rasterLayer.DataSource = rasterFeatureProvider;

            map.Layers.Add(rasterLayer);
  
            MapTestHelper.Show(map);
        }

        [Test]
        [Category(TestCategory.DataAccess)]
        public void ReadingAscFileShouldNotRequireHardReturnAtEndOfFile()
        {
            string fileName = RasterDataPath + "test.ASC";
            var newFileName = Path.GetFullPath(Path.GetFileName(fileName));
            File.Copy(Path.GetFullPath(fileName), newFileName, true);
            var fileInfo = new FileInfo(newFileName);
            if (fileInfo.IsReadOnly)
            {
                fileInfo.IsReadOnly = false;
            }

            var store = new GdalFunctionStore();
            store.Open(newFileName);


            var grid = (IRegularGridCoverage) store.Functions.First(f => f is IRegularGridCoverage);
            Assert.AreEqual(typeof (int), grid.Components[0].ValueType);
            IList<int> values = grid.GetValues<int>();
            Assert.AreEqual(-9999, grid.Components[0].NoDataValues[0]);
            //last value should be a no-data value
            Assert.AreEqual(-9999, values[grid.SizeX-1], "Last value should be nodata value");
        }


        [Test]
        [Category(TestCategory.WindowsForms)]
        public void RenderBilFile()
        {
            Debug.WriteLine(Directory.GetCurrentDirectory());

            var map = new Map(new Size(400, 200)) {Name = "map1"};

            //string fileName = @"..\..\..\..\data\RasterData\SchematisatieInt.bil";
            string fileName = RasterDataPath + "bodem.bil";
            //string fileName = @"..\..\..\..\data\wsiearth.tif";
            var rasterLayer = new GdalRasterLayer(fileName);

            rasterLayer.CustomRenderers.Add(new GdalRenderer(rasterLayer)); // add optimized custom gdal renderer

            map.Layers.Add(rasterLayer);
            MapTestHelper.Show(map);
        }

        [Test]
        [Category(TestCategory.WindowsForms)]
        public void ReadGdalRasterLayer()
        {
            var map = new Map {Name = "map1"};

            var provider = new DataTableFeatureProvider("LINESTRING(90 0,100 0,100 90)");
            var vectorLayer = new VectorLayer("test", provider);
            map.Layers.Add(vectorLayer);

            string fileName = DataPath + "wsiearth.tif";
            var rasterLayer = new RegularGridCoverageLayer();
            var rasterFeatureProvider = new GdalFeatureProvider {Path = fileName};

            rasterLayer.DataSource = rasterFeatureProvider;
            rasterLayer.CustomRenderers.Add(new GdalRenderer(rasterLayer)); // add optimized custom gdal renderer

            map.Layers.Add(rasterLayer);
            MapTestHelper.Show(map);
        }

        [Test]
        [Category(TestCategory.DataAccess)]
        public void OpenTif()
        {
            string fileName = DataPath + "dvim3.tif";
            var rasterLayer = new RegularGridCoverageLayer();

            var rasterFeatureProvider = new GdalFeatureProvider {Path = fileName};

            rasterLayer.DataSource = rasterFeatureProvider;
            rasterLayer.CustomRenderers.Add(new GdalRenderer(rasterLayer)); // add optimized custom gdal renderer

            var cellCount = rasterLayer.Grid.SizeX*rasterLayer.Grid.SizeY;
            Assert.AreEqual(cellCount, rasterLayer.Grid.Components[0].Values.Count);
        }


        [Test, Category(TestCategory.DataAccess)]
        public void CreateNewRasterLayer()
        {
            // read source grid
            string filePath = RasterDataPath + "Schematisatie.bil";
            var sourceProvider = new GdalFeatureProvider {Path = filePath};
            IRegularGridCoverage sourceGrid = sourceProvider.Grid;

            // create a new grid 
            //const string targetFilePath = @"..\..\..\..\data\RasterData\SchematisatieTest.asc";
            const string targetFilePath = "GdalProviderTest.CreateNewRasterLayer.asc";
            var targetProvider = new GdalFeatureProvider();
            targetProvider.CreateNew(targetFilePath);

            Assert.AreEqual(0, targetProvider.Features.Count);

            targetProvider.Features.Add(sourceGrid);
            
            // provider will write a source grid into .asc file and will add a new grid
            Assert.AreEqual(1, targetProvider.Features.Count);

            Assert.AreEqual(sourceGrid.SizeX, targetProvider.Grid.SizeX);
            Assert.AreEqual(sourceGrid.SizeY, targetProvider.Grid.SizeY);
            Assert.AreEqual(sourceGrid.DeltaX, targetProvider.Grid.DeltaX);
            Assert.AreEqual(sourceGrid.DeltaY, targetProvider.Grid.DeltaY);

            Assert.IsTrue(sourceGrid.X.Values.SequenceEqual(targetProvider.Grid.X.Values));
            Assert.IsTrue(sourceGrid.GetValues<float>().SequenceEqual(targetProvider.Grid.GetValues<float>()));
        }


        [Test, Category(TestCategory.DataAccess)]
        public void ReadAscFileAndCheckDataTypeIsFloat()
        {
            string filePath = Path.GetFullPath(RasterDataPath + "Zout_Bak.asc");

            var rasterFeatureProvider = new GdalFeatureProvider {Path = filePath};

            var regularGridCoverage = rasterFeatureProvider.Grid;

            Assert.AreEqual(4, rasterFeatureProvider.GdalDataset.RasterXSize);
            Assert.AreEqual(5, rasterFeatureProvider.GdalDataset.RasterYSize);
            Assert.AreEqual(1, rasterFeatureProvider.GdalDataset.RasterCount);


            Assert.AreEqual(typeof (Single), rasterFeatureProvider.Grid.Components[0].ValueType);
        }
    }
}