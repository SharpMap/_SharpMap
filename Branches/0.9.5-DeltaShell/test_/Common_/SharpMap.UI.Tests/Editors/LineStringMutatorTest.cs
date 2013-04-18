using System;
using System.Collections.Generic;
using System.Drawing;
using GisSharpBlog.NetTopologySuite.Geometries;
using SharpMap.Styles;
using SharpMap.UI.Editors;
using SharpMap.UI.FallOff;
using SharpMapTestUtils.TestClasses;
using NUnit.Framework;
using SharpMap.UI.Forms;

namespace SharpMap.UI.Tests.Editors
{
    /// <summary>
    /// For more basec test of FeatureMutator please refer to PointMutatorTest
    /// </summary>
    [TestFixture]
    public class LineStringMutatorTest
    {
        private static SampleFeature sampleFeature;

        [SetUp]
        public void SetUp()
        {
            sampleFeature = new SampleFeature
                                {
                                    Geometry = new LineString(new[]
                                                                  {
                                                                      new Coordinate(0, 0), new Coordinate(10, 0), new Coordinate(20, 0),
                                                                      new Coordinate(30, 0), new Coordinate(40, 0)
                                                                  })
                                };
        }
        private static VectorStyle GetStyle()
        {
            VectorStyle style = new VectorStyle
                                    {
                                        Fill = Brushes.AntiqueWhite,
                                        Line = Pens.Red,
                                        EnableOutline = true,
                                        Outline = Pens.Black,
                                        Symbol = new Bitmap(10, 10)
                                    };

            return style;
        }
        [Test]
        public void PointMutatorCreationWithoutMapControlTest()
        {
            MapControl mapControl = new MapControl();
            mapControl.Map.ZoomToBox(new Envelope(new Coordinate(0, 0), new Coordinate(1000, 1000)));
            ICoordinateConverter coordinateConverter = new CoordinateConverter(mapControl);
            LineStringEditor lineStringMutator = new LineStringEditor(coordinateConverter, null, sampleFeature, GetStyle());
            Assert.AreEqual(null, lineStringMutator.TargetFeature);
            Assert.AreNotEqual(null, lineStringMutator.SourceFeature);
            // There are no default focused trackers
            IList<ITrackerFeature> trackers = lineStringMutator.GetFocusedTrackers();
            Assert.AreEqual(0, trackers.Count);

            ITrackerFeature tracker = lineStringMutator.GetTrackerByIndex(2);
            Assert.AreNotEqual(null, tracker);
            Assert.AreEqual(20.0, tracker.Geometry.Coordinates[0].X);
            Assert.AreEqual(0.0, tracker.Geometry.Coordinates[0].Y);

            lineStringMutator.Start();
            lineStringMutator.Select(tracker, true);

            trackers = lineStringMutator.GetFocusedTrackers();
            Assert.AreEqual(1, trackers.Count);
            Assert.AreNotEqual(null, lineStringMutator.TargetFeature);
            Assert.AreNotEqual(lineStringMutator.SourceFeature, lineStringMutator.TargetFeature);
        }
        [Test]
        [ExpectedException(typeof (ArgumentException))]
        public void MoveTrackerWithoutSelection()
        {
            LineStringEditor lineStringMutator = new LineStringEditor(null, null, sampleFeature, GetStyle());
            ITrackerFeature tracker = lineStringMutator.GetTrackerByIndex(2); // 20,0
            lineStringMutator.Start();
            lineStringMutator.MoveTracker(tracker, 0, 5, null);
        }

        [Test]
        public void MoveTrackerWithSelection()
        {
            LineStringEditor lineStringMutator = new LineStringEditor(null, null, sampleFeature, GetStyle());
            ITrackerFeature tracker = lineStringMutator.GetTrackerByIndex(2); // 20,0
            tracker.Selected = true;
            lineStringMutator.Start();
            lineStringMutator.MoveTracker(tracker, 0, 5, null);
            Assert.AreEqual(20, tracker.Geometry.Coordinates[0].X);
            Assert.AreEqual(5, tracker.Geometry.Coordinates[0].Y);
            // check if changed coordinate is NOT set to sampleFeature 
            Assert.AreEqual(20, sampleFeature.Geometry.Coordinates[2].X);
            Assert.AreEqual(0, sampleFeature.Geometry.Coordinates[2].Y);
            // todo redesign .Stop
            //lineStringMutator.Stop();
            //// check if changed coordinate IS set to sampleFeature 
            //Assert.AreEqual(20, sampleFeature.Geometry.Coordinates[2].X);
            //Assert.AreEqual(5, sampleFeature.Geometry.Coordinates[2].Y);
        }

        [Test]
        [Category("Integration")]
        public void MoveTestWithLinearFalOffPolicy()
        {
            LineStringEditor lineStringMutator = new LineStringEditor(null, null, sampleFeature, GetStyle());
            lineStringMutator.FallOffPolicy = new LinearFallOffPolicy();
            ITrackerFeature tracker = lineStringMutator.GetTrackerByIndex(2); // 20,0
            tracker.Selected = true;
            lineStringMutator.Start();
            lineStringMutator.MoveTracker(tracker, 0, 5, null);
            // todo redesign .Stop
            //lineStringMutator.Stop();
            //Assert.AreEqual(20, sampleFeature.Geometry.Coordinates[2].X);
            //Assert.AreEqual(5, sampleFeature.Geometry.Coordinates[2].Y);
        }

        [Test]
        public void SelectionTestViaCoordinates()
        {
            MapControl mapControl = new MapControl {Map = {Size = new Size(1000, 1000)}};
            ICoordinateConverter coordinateConverter = new CoordinateConverter(mapControl);
            LineStringEditor lineStringMutator = new LineStringEditor(coordinateConverter, null, sampleFeature, GetStyle());
            ITrackerFeature trackerFeatureAtCoordinate = lineStringMutator.GetTrackerAtCoordinate(new Coordinate(20, 0));
            ITrackerFeature trackerFeatureAtIndex = lineStringMutator.GetTrackerByIndex(2);
            Assert.AreEqual(trackerFeatureAtIndex, trackerFeatureAtCoordinate);
            trackerFeatureAtCoordinate.Selected = true;
            Assert.AreEqual(1, lineStringMutator.GetFocusedTrackers().Count);
        }

        [Test]
        public void MultipleSelectionTestViaCoordinatesNullFallOff()
        {
            // todo write
            MapControl mapControl = new MapControl { Map = { Size = new Size(1000, 1000) } };
            ICoordinateConverter coordinateConverter = new CoordinateConverter(mapControl);
            LineStringEditor lineStringMutator = new LineStringEditor(coordinateConverter, null, sampleFeature, GetStyle());
            ITrackerFeature trackerFeatureAtCoordinate10 = lineStringMutator.GetTrackerAtCoordinate(new Coordinate(10, 0));
            trackerFeatureAtCoordinate10.Selected = true;
            ITrackerFeature trackerFeatureAtCoordinate30 = lineStringMutator.GetTrackerAtCoordinate(new Coordinate(30, 0));
            trackerFeatureAtCoordinate30.Selected = true;
            Assert.AreEqual(2, lineStringMutator.GetFocusedTrackers().Count);
            lineStringMutator.Start();
            lineStringMutator.MoveTracker(trackerFeatureAtCoordinate30, 0, 5, null);
            // only tracker at 10 is moved
            Assert.AreEqual(5.0, trackerFeatureAtCoordinate30.Geometry.Coordinates[0].Y);
            Assert.AreEqual(0.0, trackerFeatureAtCoordinate10.Geometry.Coordinates[0].Y);
            lineStringMutator.Stop();
        }

        [Test]
        public void MultipleSelectionTestViaCoordinatesNoFallOffPolicy()
        {
            // todo write
            MapControl mapControl = new MapControl { Map = { Size = new Size(1000, 1000) } };
            ICoordinateConverter coordinateConverter = new CoordinateConverter(mapControl);
            LineStringEditor lineStringMutator = new LineStringEditor(coordinateConverter, null, sampleFeature, GetStyle());
            ITrackerFeature trackerFeatureAtCoordinate10 = lineStringMutator.GetTrackerAtCoordinate(new Coordinate(10, 0));
            trackerFeatureAtCoordinate10.Selected = true;
            ITrackerFeature trackerFeatureAtCoordinate30 = lineStringMutator.GetTrackerAtCoordinate(new Coordinate(30, 0));
            trackerFeatureAtCoordinate30.Selected = true;
            lineStringMutator.FallOffPolicy = new NoFallOffPolicy();
            lineStringMutator.Start();
            lineStringMutator.MoveTracker(trackerFeatureAtCoordinate30, 0, 5, null);
            // both tracker at 10 and 30 is moved
            Assert.AreEqual(5.0, trackerFeatureAtCoordinate30.Geometry.Coordinates[0].Y);
            Assert.AreEqual(5.0, trackerFeatureAtCoordinate10.Geometry.Coordinates[0].Y);
            lineStringMutator.Stop();
        }
        [Test]
        public void AllTrackerTest()
        {
            MapControl mapControl = new MapControl { Map = { Size = new Size(1000, 1000) } };
            ICoordinateConverter coordinateConverter = new CoordinateConverter(mapControl);
            LineStringEditor lineStringMutator = new LineStringEditor(coordinateConverter, null, sampleFeature, GetStyle());
            ITrackerFeature trackerFeature = lineStringMutator.GetTrackerAtCoordinate(new Coordinate(15, 0));
            lineStringMutator.Start();
            lineStringMutator.MoveTracker(trackerFeature, 0.0, 5.0, null);
            Assert.AreEqual(5.0, lineStringMutator.TargetFeature.Geometry.Coordinates[0].Y);
            Assert.AreEqual(5.0, lineStringMutator.TargetFeature.Geometry.Coordinates[1].Y);
            Assert.AreEqual(5.0, lineStringMutator.TargetFeature.Geometry.Coordinates[2].Y);
            Assert.AreEqual(5.0, lineStringMutator.TargetFeature.Geometry.Coordinates[3].Y);
            Assert.AreEqual(5.0, lineStringMutator.TargetFeature.Geometry.Coordinates[4].Y);
            lineStringMutator.Stop();
        }
    }
}