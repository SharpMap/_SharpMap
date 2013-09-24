using System;
using System.Linq;
using System.Windows.Forms;
using DelftTools.TestUtils;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;
using NetTopologySuite.Extensions.Coverages;
using NUnit.Framework;
using SharpMap.Layers;
using SharpMap.UI.Forms;
using SharpMapTestUtils;
using log4net.Core;

namespace SharpMap.Tests.Layers
{
    [TestFixture]
    public class DiscreteGridPointCoverageLayerTest
    {
        [Test]  
        [Category(TestCategory.WindowsForms)]
        public void ShowUsingMapView()
        {
            var points = new[,]
                             {
                                 {new Point(0, 0), new Point(1, 0), new Point(2, 0)}, 
                                 {new Point(0, 1), new Point(1, 1), new Point(2, 1)}, 
                             };

            var coverage = new DiscreteGridPointCoverage(2, 3, points.Cast<IPoint>());

            var values = new[,]
                             {
                                 {1.0, 2.0},
                                 {3.0, 4.0},
                                 {5.0, 6.0}
                             };

            coverage.SetValues(values);

            var coverageLayer = new DiscreteGridPointCoverageLayer {Coverage = coverage};

            var map = new Map {Layers = {coverageLayer}};

            MapTestHelper.ShowModal(map);
        }

        [Test]
        [Category(TestCategory.WindowsForms)]
        public void ShowUsingMapViewWhereValuesContainNoDataValue()
        {
            var points = new[,]
                             {
                                 {new Point(0, 0), new Point(1, 0)},
                                 {new Point(2, 1), new Point(3, 1.5)},
                                 {new Point(1, 2), new Point(3, 3)}
                             };

            var coverage = new DiscreteGridPointCoverage(2, 3, points.Cast<IPoint>());

            var values = new[,]
                             {
                                 {1.0, 2.0},
                                 {3.0, 4.0},
                                 {5.0, -999.0}
                             };

            coverage.Components[0].NoDataValues = new [] {-999.0};
            
            coverage.SetValues(values);

            var coverageLayer = new DiscreteGridPointCoverageLayer { Coverage = coverage };

            var map = new Map { Layers = { coverageLayer } };

            MapTestHelper.ShowModal(map);
        }

        [Test]
        public void NoCrashOnEmptyCoverage()
        {
            var coverage = new DiscreteGridPointCoverage();
            new DiscreteGridPointCoverageLayer { Coverage = coverage };
        }

        [Test]
        [Category(TestCategory.WindowsForms)]
        public void ShowUsingMapViewWithFaces()
        {
            var points = new[,]
                             {
                                 {new Point(0.0, 1.0), new Point(0.0, 0.0)}, 
                                 {new Point(0.5, 1.5), new Point(1.0, 0.0)}, 
                                 {new Point(1.0, 2.0), new Point(2.0, 2.0)}
                             };

            var coverage = new DiscreteGridPointCoverage(3, 2, points.Cast<IPoint>());

            var values = new[,]
                             {
                                 {1.0, 2.0},
                                 {3.0, 4.0},
                                 {5.0, 6.0}
                             };

            coverage.SetValues(values);

            var coverageLayer = new DiscreteGridPointCoverageLayer { Coverage = coverage, ShowFaces = true};

            var map = new Map { Layers = { coverageLayer } };

            MapTestHelper.ShowModal(map);
        }

        [Test]
        [Category(TestCategory.WindowsForms)]
        public void ShowUsingMapViewWithFacesAndTimeNavigation()
        {
            LogHelper.ConfigureLogging(Level.Debug);

            var points = new[,]
                             {
                                 {new Point(0.0, 1.0), new Point(0.0, 0.0)}, 
                                 {new Point(0.5, 1.5), new Point(1.0, 0.0)}, 
                                 {new Point(1.0, 2.0), new Point(2.0, 2.0)}
                             };

            var coverage = new DiscreteGridPointCoverage(3, 2, points.Cast<IPoint>()) { IsTimeDependent = true };

            var values1 = new[,]
                             {
                                 {1.0, 2.0},
                                 {3.0, 4.0},
                                 {5.0, 6.0}
                             };

            var values2 = new[,]
                             {
                                 {2.0, 2.0},
                                 {2.0, 4.0},
                                 {1.0, 2.0}
                             };

            coverage[new DateTime(2000, 1, 1)] = values1;
            coverage[new DateTime(2001, 1, 1)] = values2;
            coverage[new DateTime(2002, 1, 1)] = values1;
            coverage[new DateTime(2003, 1, 1)] = values2;

            var coverageLayer = new DiscreteGridPointCoverageLayer { Coverage = coverage, ShowFaces = true };

            var map = new Map { Layers = { coverageLayer } };

            // create time navigation track bar
            var form = new Form { Width = 500, Height = 100 };

            var trackBar = new TrackBar { Dock = DockStyle.Fill, Minimum = 0, Maximum = coverage.Time.Values.Count - 1 };
            trackBar.ValueChanged += delegate
                                         {
                                             coverageLayer.SetCurrentTimeSelection(coverage.Time.Values[trackBar.Value], null);
                                         };

            form.Controls.Add(trackBar);
            WindowsFormsTestHelper.Show(form);

            WindowsFormsTestHelper.ShowModal(new MapControl { Map = map, AllowDrop = false  }, coverageLayer);
        }

        [Test]
        [Category(TestCategory.WindowsForms)]
        public void SelectFaces()
        {
            var points = new[,]
                             {
                                 {new Point(0.0, 1.0), new Point(0.0, 0.0)}, 
                                 {new Point(0.5, 1.5), new Point(1.0, 0.0)}, 
                                 {new Point(1.0, 2.0), new Point(2.0, 2.0)}
                             };

            var coverage = new DiscreteGridPointCoverage(3, 2, points.Cast<IPoint>());

            var values = new[,]
                             {
                                 {1.0, 2.0},
                                 {3.0, 4.0},
                                 {5.0, 6.0}
                             };

            coverage.SetValues(values);

            var coverageLayer = new DiscreteGridPointCoverageLayer { Coverage = coverage, ShowFaces = true, ShowVertices = true };

            var map = new Map { Layers = { coverageLayer } };

            var mapControl = new MapControl { Map = map, AllowDrop = false };
            mapControl.FixedZoomOutTool.Execute(); // zoom out a bit
            mapControl.ActivateTool(mapControl.SelectTool);

            WindowsFormsTestHelper.ShowModal(mapControl);
        }

        [Test]
        [Category(TestCategory.WindowsForms)]
        public void QueryValuesUsingProfileTool()
        {
            var points = new[,]
                             {
                                 {new Point(0.0, 1.0), new Point(0.0, 0.0)}, 
                                 {new Point(0.5, 1.5), new Point(1.0, 0.0)}, 
                                 {new Point(1.0, 2.0), new Point(2.0, 2.0)}
                             };

            var coverage = new DiscreteGridPointCoverage(3, 2, points.Cast<IPoint>());

            var values = new[,]
                             {
                                 {1.0, 2.0},
                                 {3.0, 4.0},
                                 {5.0, 6.0}
                             };

            coverage.SetValues(values);

            var coverageLayer = new DiscreteGridPointCoverageLayer { Coverage = coverage, ShowFaces = true, ShowVertices = true };

            var map = new Map { Layers = { coverageLayer } };

            var mapControl = new MapControl { Map = map, AllowDrop = false };

            mapControl.FixedZoomOutTool.Execute(); // zoom out a bit

            mapControl.ActivateTool(mapControl.CoverageProfileTool);

            WindowsFormsTestHelper.ShowModal(mapControl);
        }
    }
}