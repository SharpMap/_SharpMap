using System.Collections.Generic;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;
using NUnit.Framework;
using Rhino.Mocks;
using SharpMap.Data.Providers;
using SharpMap.Layers;
using SharpMap.UI.Snapping;

namespace SharpMap.UI.Tests.Snapping
{
    [TestFixture]
    public class SnapStrategyTest
    {
        private static IGeometry lineString;

        [SetUp]
        public void Setup()
        {
            lineString =
                new LineString(new[]
                                   {
                                       new Coordinate(0, 0), new Coordinate(10, 0), new Coordinate(20, 0),
                                       new Coordinate(30, 0), new Coordinate(40, 0)
                                   });
        }
        [Test]
        public void SnapToLineStringUsingFreeAtObject()
        {
            MockRepository mockRepository = new MockRepository();
            IFeature feature = mockRepository.CreateMock<IFeature>();

            using (mockRepository.Record())
            {
                Expect.Call(feature.Geometry).Return(lineString).Repeat.Any();
            }
            using (mockRepository.Playback())
            {
                VectorLayer lineStringLayer = new VectorLayer
                {
                    DataSource = new FeatureCollection
                    {
                        Features = new List<IFeature> { feature }
                    }
                };

                IPoint snapSource = new Point(5, 5);
                SnapRule snapRule = new SnapRule
                                        {
                                            TargetLayer = lineStringLayer,
                                            SnapRole = SnapRole.FreeAtObject,
                                            Obligatory = true,
                                            PixelGravity = 40
                                        };

                ISnapResult snapResults = snapRule.Execute(null, snapSource, null,
                                                             snapSource.Coordinates[0],
                                                             CreateEnvelope(snapSource, 10),
                                                             0);
                Assert.IsNotNull(snapResults);  
                Assert.IsNotNull(snapResults.Location);
                Assert.AreEqual(0, snapResults.Location.Y);
                Assert.AreEqual(5, snapResults.Location.X);
            }
        }

        static IEnvelope CreateEnvelope(IPoint point, double marge)
        {
            return new Envelope(point.Coordinates[0].X - marge, point.Coordinates[0].X + marge,
                                point.Coordinates[0].Y - marge, point.Coordinates[0].Y + marge);
        }

        [Test]
        public void SnapToLineStringUsingDifferentRoles()
        {
            MockRepository mockRepository = new MockRepository();
            IFeature feature = mockRepository.CreateMock<IFeature>();

            using (mockRepository.Record())
            {
                Expect.Call(feature.Geometry).Return(lineString).Repeat.Any();
            }
            using (mockRepository.Playback())
            {
                VectorLayer lineStringLayer = new VectorLayer
                {
                    DataSource = new FeatureCollection
                    {
                        Features = new List<IFeature> { feature }
                    }
                };

                SnapRule snapRule = new SnapRule
                {
                    TargetLayer = lineStringLayer,
                    Obligatory = true,
                    PixelGravity = 40
                };

                IPoint snapSource = new Point(6, 5);

                snapRule.SnapRole = SnapRole.AllTrackers;
                ISnapResult snapResults = snapRule.Execute(null, snapSource, null,
                                                             snapSource.Coordinates[0],
                                                             CreateEnvelope(snapSource, 10),
                                                             0);
                Assert.IsNotNull(snapResults);
                Assert.IsNotNull(snapResults.Location);
                Assert.AreEqual(0, snapResults.Location.Y);
                Assert.AreEqual(10, snapResults.Location.X);

                snapRule.SnapRole = SnapRole.Start;
                snapResults = snapRule.Execute(null, snapSource, null,
                                                             snapSource.Coordinates[0],
                                                             CreateEnvelope(snapSource, 10),
                                                             0);
                Assert.IsNotNull(snapResults.Location);
                Assert.AreEqual(0, snapResults.Location.Y);
                Assert.AreEqual(0, snapResults.Location.X);

                snapSource = new Point(6, 75);
                snapRule.SnapRole = SnapRole.End;
                snapResults = snapRule.Execute(null, snapSource, null,
                                                             snapSource.Coordinates[0],
                                                             CreateEnvelope(snapSource, 10),
                                                             0);
                Assert.IsNull(snapResults);

                snapSource = new Point(6, 5);
                snapRule.SnapRole = SnapRole.Free;
                snapResults = snapRule.Execute(null, snapSource, null,
                                                             snapSource.Coordinates[0],
                                                             CreateEnvelope(snapSource, 10),
                                                             0);
                Assert.IsNotNull(snapResults.Location);
                Assert.AreEqual(5, snapResults.Location.Y);
                Assert.AreEqual(6, snapResults.Location.X);
            }
        }

    }
}
