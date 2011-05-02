using System.Collections.Generic;
using System.Drawing;
using GisSharpBlog.NetTopologySuite.Geometries;
using NUnit.Framework;
using SharpMap.Styles;
using SharpMap.UI.Editors;
using SharpMap.UI.Forms;
using SharpMapTestUtils.TestClasses;
using Point=GisSharpBlog.NetTopologySuite.Geometries.Point;

namespace SharpMap.UI.Tests.Editors
{
    [TestFixture]
    public class PointMutatorTest
    {
        private static SampleFeature sampleFeature;

        [SetUp]
        public void SetUp()
        {
            sampleFeature = new SampleFeature {Geometry = new Point(0, 0)};
        }

        private static VectorStyle GetStyle(Pen pen)
        {
            VectorStyle style = new VectorStyle
                                    {
                                        Fill = Brushes.AntiqueWhite,
                                        Line = pen,
                                        EnableOutline = true,
                                        Outline = Pens.Black,
                                        Symbol = new Bitmap(10, 10)
                                    };

            return style;
        }
        [Test]
        public void PointMutatorCreationWithoutMapControlTest()
        {
            PointEditor pointMutator = new PointEditor(null, null, sampleFeature, GetStyle(Pens.Red));
            Assert.AreEqual(null, pointMutator.TargetFeature);
            Assert.AreNotEqual(null, pointMutator.SourceFeature);
            // The tracker has focus by default; is this ok
            IList<ITrackerFeature> trackers = pointMutator.GetFocusedTrackers();
            Assert.AreEqual(1, trackers.Count);

            ITrackerFeature tracker = pointMutator.GetTrackerByIndex(0);
            Assert.AreNotEqual(null, tracker);

            pointMutator.Start();
            Assert.AreNotEqual(null, pointMutator.TargetFeature);
            Assert.AreNotEqual(pointMutator.SourceFeature, pointMutator.TargetFeature);
        }

        [Test]
        public void SelectionTest()
        {
            PointEditor pointMutator = new PointEditor(null, null, sampleFeature, GetStyle(Pens.Red));
            ITrackerFeature tracker = pointMutator.GetTrackerByIndex(0);
            // The tracker has focus by default; is this ok
            Assert.AreEqual(true, tracker.Selected);
            pointMutator.Select(tracker, false);
            Assert.AreEqual(false, tracker.Selected);
        }

        [Test]
        public void PointMutatorCreationWithMapControlTest()
        {
            IMapControl mapControl = new MapControl {Map = {Size = new Size(1000, 1000)}};
            ICoordinateConverter coordinateConverter = new CoordinateConverter(mapControl);
            PointEditor pointMutator = new PointEditor(coordinateConverter, null,
                                                       sampleFeature, GetStyle(Pens.Red));
            Assert.AreEqual(null, pointMutator.TargetFeature);
            Assert.AreNotEqual(null, pointMutator.SourceFeature);

            ITrackerFeature tracker = pointMutator.GetTrackerAtCoordinate(new Coordinate(0, 0));
            Assert.AreNotEqual(null, tracker);

            pointMutator.Start();
            pointMutator.MoveTracker(tracker, 5.0, 5.0, null);
            pointMutator.Stop();

            Assert.AreEqual(5.0, tracker.Geometry.Coordinates[0].X);
            Assert.AreEqual(5.0, tracker.Geometry.Coordinates[0].Y);
            Assert.AreEqual(5.0, sampleFeature.Geometry.Coordinates[0].X);
            Assert.AreEqual(5.0, sampleFeature.Geometry.Coordinates[0].Y);
        }
    }
}