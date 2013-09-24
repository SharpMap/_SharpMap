using System;
using System.IO;
using DelftTools.TestUtils;
using NUnit.Framework;
using OSGeo.GDAL;
using SharpMap.Extensions.Data.Providers;

using SharpTestsEx;
using log4net.Core;

namespace SharpMap.Extensions.Tests.Data.Providers
{
    [TestFixture]
    public class GdalHelperTest
    {
        [TestFixtureSetUp]
        public void RegisterDrivers()
        {
            Gdal.AllRegister();
        }

        [Test, Category(TestCategory.DataAccess)]
        public void FixDelft1D2DOutputFile()
        {
            string dir = TestHelper.GetTestDataPath(TestDataPath.DeltaShell.DeltaShellDeltaShellPluginsSharpMapGisTests);
            string path = dir +  @"\RasterData\dm1d0079.asc";
            File.Copy(path, Path.GetFileName(path),true);
            path = Path.GetFileName(path);
            GdalHelper.CheckAndFixAscHeader(path);

            using (TextReader reader = new StreamReader(path))
            {
                string firstLine = reader.ReadLine();
                Assert.IsTrue(firstLine.StartsWith(
                    "ncols",StringComparison.OrdinalIgnoreCase));
                Assert.IsTrue(File.Exists(path+".bak"));
            }
        }

        [Test]
        public void GetSupportedGdalTargetTypeByDriverAndComponentTypeForBIL()
        {
            const string path = @"..\..\..\..\data\RasterData\Bodem.bil";
            var driverName = GdalHelper.GetDriverName(path);
            
            var valueType = typeof(double);
            var gdalDriver = Gdal.GetDriverByName(driverName);
            var dataType = GdalHelper.GetGdalDataType(gdalDriver,valueType);

            Assert.AreEqual(DataType.GDT_Float32, dataType);
        }

        [Test]
        public void GetSupportedGDalDataTypeSet()
        {
            var gdalDriverForMapFile = Gdal.GetDriverByName("PCRaster");
            var targetTypes = GdalHelper.GetSupportedValueTypes(gdalDriverForMapFile);

            targetTypes.Should().Contain(DataType.GDT_Byte);
            targetTypes.Should().Contain(DataType.GDT_Int32);
            targetTypes.Should().Contain(DataType.GDT_Float32);
            targetTypes.Should().Have.Count.EqualTo(3);
        }

        [Test]
        public void GetSupportedGDalDataType()
        {
            Driver gdalDriverForBilFile = Gdal.GetDriverByName("EHdr");
            var type = GdalHelper.GetGdalDataType(gdalDriverForBilFile, typeof(double));
            Assert.AreEqual(DataType.GDT_Float32, type);
        }

        [Test]
        public void SupportedDriver()
        {
            LogHelper.ConfigureLogging(Level.Info);
            foreach (var driver in GdalHelper.SupportedDrivers)
            {
                Console.WriteLine(driver.LongName + "(" + driver.ShortName + ")");
            }
        }
    }
}
