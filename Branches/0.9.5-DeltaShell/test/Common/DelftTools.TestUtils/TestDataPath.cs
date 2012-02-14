namespace DelftTools.TestUtils
{
    /// <summary>
    /// Used to make paths strongly typed, see <see cref="TestHelper.GetTestDataPath"/>
    /// </summary>
    public class TestDataPath
    {
        public string Path { get; set; }

        public static implicit operator TestDataPath(string path)
        {
            return new TestDataPath {Path = path};
        }

        public static class Plugins
        {
            public static class Habitat
            {
                public static readonly TestDataPath DeltaShellPluginsImportersHabitatTests =
                    @"Plugins/Habitat/DeltaShell.Plugins.Importers.Habitat.Tests";

                public static readonly TestDataPath DeltaShellPluginsHabitatTests =
                    @"Plugins/Habitat/DeltaShell.Plugins.Habitat.Tests";

                public static readonly TestDataPath DeltaShellPluginsImportExportDemisTests =
                    @"Plugins/Habitat/DeltaShell.Plugins.ImportExport.Demis.Tests";

                public static readonly TestDataPath DeltaShellPluginsDemisMapControlTests =
                    @"Plugins/Habitat/DeltaShell.Plugins.Demis.MapControl.Tests";
            }

            public static class DelftModels
            {
                public static readonly TestDataPath DeltaShellPluginsDelftModelsWaterFlowModelTests =
                    @"Plugins/DelftModels/DeltaShell.Plugins.DelftModels.WaterFlowModel.Tests";

                public static readonly TestDataPath DeltaShellPluginsImportExportSobekTests =
                    @"Plugins/DelftModels/DeltaShell.Plugins.ImportExport.Sobek.Tests";

                public static readonly TestDataPath DeltaShellPluginsWaterFlowModelIntegrationTests =
                    @"Plugins\DelftModels\DeltaShell.Plugins.DelftModels.WaterFlowModel.IntegrationTests";
            }
        }

        public static class Common
        {
            public static class DelftTools
            {
                public static readonly TestDataPath DelftToolsTests = @"Common/DelftTools.Tests";

                public static readonly TestDataPath DelftToolsTestsUtilsXmlSerialization =
                    @"Common/DelftTools.Tests/Utils/Xml/Serialization";
            }
        }

        public static class DeltaShell
        {
            public static readonly TestDataPath DeltaShellDeltaShellPluginsDataXmlTests =
                @"DeltaShell/DeltaShell.Plugins.Data.Xml.Tests";

            public static readonly TestDataPath DeltaShellDeltaShellPluginsSharpMapGisTests =
                @"DeltaShell/DeltaShell.Plugins.SharpMapGis.Tests/";

            public static readonly TestDataPath DeltaShellDeltaShellPluginsSharpMapGisTestsRasterData =
                @"DeltaShell/DeltaShell.Plugins.SharpMapGis.Tests/RasterData/";

            public static readonly TestDataPath DeltaShellDeltaShellIntegrationTests =
                @"DeltaShell/DeltaShell.IntegrationTests/";

            public static readonly TestDataPath DeltaShellDeltaShellIntegrationTestsNetCdf =
                @"DeltaShell/DeltaShell.IntegrationTests/NetCdf/";

            public static readonly TestDataPath DeltaShellDeltaShellIntegrationTestsGDAL =
                @"DeltaShell/DeltaShell.IntegrationTests/GDAL/";

            public static readonly TestDataPath DeltaShellPluginsDataNHibernateTests =
                @"DeltaShell/DeltaShell.Plugins.Data.NHibernate.Tests";

            public static readonly TestDataPath DeltaShellPluginsNetCDFTests =
                @"DeltaShell/DeltaShell.Plugins.NetCDF.Tests";

            public static readonly TestDataPath DeltaShellPluginsDataNHibernateIntegrationTests =
                @"DeltaShell/DeltaShell.Plugins.Data.NHibernate.IntegrationTests/";
        }

        public static class VectorData
        {
            public static readonly TestDataPath VectorDataPath = @"vectorData";
        }

        public static class RasterData
        {
            public static readonly TestDataPath RasterDataPath = @"rasterData";
        }

        public static class NetCdfData
        {
            public static readonly TestDataPath NetCdfDataPath = @"netCdfData";
        }

        //public static class DeltaShell
        //{
        //    internal static class DelftShell
        //    {
        //        public static class Plugin
        //        {
        //            public static class Data
        //            {
        //                public static class Xml
        //                {
        //                    public static readonly TestDataPath Tests = @"DeltaShell/DeltaShell.Plugins.Data.Xml.Tests";
        //                }
        //            }
        //        }
        //    }
        //}
    }
}