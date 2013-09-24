using System.Collections.Generic;
using System.Drawing;
using System.IO;
using DelftTools.TestUtils;
using GeoAPI.Extensions.Feature;
using log4net;
using NUnit.Framework;
using SharpMap.Extensions.Data.Providers;
using SharpMap.Layers;
using SharpMapTestUtils;

namespace SharpMap.Tests.Data.Providers
{
    [TestFixture]
    public class OgrFeatureProviderTests
    {
        private static readonly ILog log = LogManager.GetLogger(typeof (OgrFeatureProviderTests));

        const string SharpMapTestDataPath = @"..\..\..\..\..\test-data\DeltaShell\DeltaShell.Plugins.SharpMapGis.Tests\";

        [Test]
        [Category(TestCategory.WindowsForms)]
        [Category(TestCategory.Slow)]
        public void ReadEsriMdbGeodatabase()
        {
            var map = new Map(new Size(400, 200)) {Name = "map1"};

            const string fileName = SharpMapTestDataPath + @"TurtleGeodatabase\Turtle.mdb";

            IList<OgrFeatureProvider> providers = new List<OgrFeatureProvider>();

            var numberOfLayers = OgrFeatureProvider.GetNumberOfLayers(fileName);

            for (int i = 0; i < numberOfLayers; i++)
            {
                var provider = new OgrFeatureProvider(fileName, i);
                providers.Add(provider);

                log.DebugFormat("Added layer {0}", provider.Name);
            }

            var vectorLayer = new VectorLayer {DataSource = providers[6]};
            map.Layers.Add(vectorLayer);

            MapTestHelper.ShowModal(map);
        }

        [Test]
        [Category(TestCategory.DataAccess)]
        public void GetOgrFeatureAttribites()
        {
            const string path = SharpMapTestDataPath + @"TurtleGeodatabase\Turtle.mdb";
            var provider = new OgrFeatureProvider(path, 2);
            var feature = (IFeature) provider.Features[0];
            Assert.AreEqual("KST0073", feature.Attributes["ST_ID_STUW"]);
        }

        [Test, Category(TestCategory.DataAccess)]
        public void GetFeatureShouldWorkForShapeFile()
        {
            const string path = SharpMapTestDataPath + "Europe_Lakes.shp";
            using (var s = new OgrFeatureProvider(path, Path.GetFileNameWithoutExtension(path)))
            {
                var feature = s.Features[0];
                Assert.AreEqual(0, s.IndexOf((IFeature) feature));
            }
        }

        [Test, Category(TestCategory.DataAccess)]
        public void ContainsShouldWorkForShapeFile()
        {
            const string path = SharpMapTestDataPath + "Europe_Lakes.shp";
            using (var featureProvider = new OgrFeatureProvider(path, Path.GetFileNameWithoutExtension(path)))
            {
                var feature = featureProvider.Features[0];
                featureProvider.Contains((IFeature) feature);
            } // -> should not throw an exception}
        }

        [Test, Category(TestCategory.DataAccess)]
        public void GetFeatureShouldWorkForShapeFileWithoutObjectID()
        {
            const string path = SharpMapTestDataPath + "gemeenten.shp";
            var s = new OgrFeatureProvider(path, Path.GetFileNameWithoutExtension(path));
            var feature = s.Features[0];
            Assert.Less(-1, s.IndexOf((IFeature) feature));
        }


        [Test, ExpectedException(typeof(IOException), ExpectedMessage = @"Unable to read the following file: ..\..\..\..\..\test-data\DeltaShell\DeltaShell.Plugins.SharpMapGis.Tests\Coquitlam model extents.shp")]
        [Category(TestCategory.DataAccess)]
        public void ShapeFileWithInvalidShx()
        {
            const string path = SharpMapTestDataPath + "Coquitlam model extents.shp";
            //make sure featureprovider is disposed immediately
            using (var provider = new OgrFeatureProvider(path, Path.GetFileNameWithoutExtension(path)))
            {
                Assert.IsNotNull(provider.Features[0]);
            }
        }
    }
}