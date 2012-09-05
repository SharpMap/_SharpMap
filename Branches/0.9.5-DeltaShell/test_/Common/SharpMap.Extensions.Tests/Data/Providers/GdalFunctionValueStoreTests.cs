using System.IO;
using DelftTools.DataObjects.Functions;
using DelftTools.DataObjects.Functions.Generic;
using DelftTools.Utils.Collections.Generic;
using GeoAPI.Extensions.Coverage;
using NUnit.Framework;
using Rhino.Mocks;
using SharpMap.Extensions.Data.Providers;

namespace SharpMap.Extensions.Tests.Data.Providers
{
    [TestFixture]
    public class GdalFunctionValueStoreTests
    {
        private MockRepository mockRepository;

        [SetUp]
        public void init()
        {
            mockRepository = new MockRepository();
        }

        [Test, Category("DataAccess")]
        public void CheckEnvelope()
        {
            string filePath = @"..\..\..\..\data\RasterData\Schematisatie.bil";
            GdalFunctionStore store = new GdalFunctionStore
                                                    {
                                                        Path = Path.GetFullPath(filePath)
                                                    };

            IRegularGridCoverage regularGridCoverage = store.Grid;

            Assert.IsNotNull(store);

            //check wether geometry has been updated in the right manner.
            Assert.AreEqual(0, regularGridCoverage.Geometry.Envelope.EnvelopeInternal.MinX, 0.1);
            Assert.AreEqual(60, regularGridCoverage.Geometry.Envelope.EnvelopeInternal.MaxX, 0.1);

            Assert.AreEqual(0, regularGridCoverage.Geometry.Envelope.EnvelopeInternal.MinY, 0.1);
            Assert.AreEqual(60, regularGridCoverage.Geometry.Envelope.EnvelopeInternal.MaxY, 0.1);

            Assert.AreEqual(3, regularGridCoverage.SizeY);
            Assert.AreEqual(3, regularGridCoverage.SizeX);

            
            Assert.AreEqual(20.0, regularGridCoverage.DeltaX, 0.1);
            Assert.AreEqual(-20.0, regularGridCoverage.DeltaY, 0.1);

            //check wether lower left corner lies at (0, 0)
            Assert.AreEqual(0.0, regularGridCoverage.Origin.X, 0.1);
            Assert.AreEqual(60.0, regularGridCoverage.Origin.Y, 0.1);


        }
    }
}