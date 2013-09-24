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

            public static class Fews
            {
                public static readonly TestDataPath TestProjectFolder = @"Plugins\Fews\DeltaShell.Plugins.Fews.Tests";
            }

            public static class OpenDA
            {
                public static readonly TestDataPath DeltaShellOpenDAIntegrationTests =
                    @"Plugins\OpenDA\DeltaShell.Plugins.OpenDA.Tests";
            }

            public static class DelftModels
            {
                public static readonly TestDataPath DeltaShellPluginsDelftModelsWaterFlowModelTests =
                    @"Plugins/DelftModels/DeltaShell.Plugins.DelftModels.WaterFlowModel.Tests";

                public static readonly TestDataPath DeltaShellPluginsImportExportSobekTests =
                    @"Plugins/DelftModels/DeltaShell.Plugins.ImportExport.Sobek.Tests";

                public static readonly TestDataPath DeltaShellPluginsSobekIntegrationTests =
                    @"Plugins/DelftModels/Sobek.IntegrationTests";

                public static readonly TestDataPath DeltaShellPluginsDelftModelsWaterQualityModelTests =
                    @"Plugins/DelftModels/DeltaShell.Plugins.DelftModels.WaterQualityModel.Tests";

                public static readonly TestDataPath DeltaShellPluginsDelftModelsRealTimeControlTests =
                    @"Plugins/DelftModels/DeltaShell.Plugins.DelftModels.RealTimeControl.Tests";

                public static readonly TestDataPath DeltaShellPluginsDelftModelsRainfallRunoffTests =
                    @"Plugins/DelftModels/DeltaShell.Plugins.DelftModels.RainfallRunoff.Tests";
            }

            public static class MorphAn
            {
                public static readonly TestDataPath DeltaShellPluginsDurostaTests =
                    @"Plugins/MorphAn/DeltaShell.Plugins.Durosta.Tests";

                public static readonly TestDataPath DeltaShellPluginsMorphAnTests =
                    @"Plugins/MorphAn/DeltaShell.Plugins.MorphAn.Tests";
            }

            public static class NetworkEditor
            {
                public static readonly TestDataPath DeltaShellPluginsNetworkEditorTests =
                    @"Plugins\NetworkEditor\DeltaShell.Plugins.NetworkEditor.Tests";
            }

            public static class XBeach
            {
                public static readonly TestDataPath DeltaShellPluginsXBeachTests =
                    @"Plugins/XBeach/DeltaShell.Plugins.XBeach.Tests";
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