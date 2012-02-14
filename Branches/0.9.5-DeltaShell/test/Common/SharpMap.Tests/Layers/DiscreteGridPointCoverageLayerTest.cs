using System;
using System.Linq;
using DelftTools.TestUtils;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;
using NetTopologySuite.Extensions.Coverages;
using NUnit.Framework;
using SharpMap.Layers;
using SharpMap.UI.Forms;
using SharpMapTestUtils;

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
                                 {new Point(0, 0), new Point(1, 0)},
                                 {new Point(2, 1), new Point(3, 1.5)},
                                 {new Point(1, 2), new Point(3, 3)}
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

            MapTestHelper.Show(map);
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

            MapTestHelper.Show(map);
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

            MapTestHelper.Show(map);
        }

        [Test]
        [Category(TestCategory.WindowsForms)]
        public void ShowUsingMapViewWithFacesAndTimeNavigation()
        {
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

            MapTestHelper.Show(map);
            /*
            var mapView = new MapView {Data = map};
            var timeNavigator = new TimeSeriesNavigator {Data = mapView};

            WindowsFormsTestHelper.Show(timeNavigator);
            WindowsFormsTestHelper.ShowModal(mapView);
            */
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

            var mapControl = new MapControl {Map = map};
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

            var mapControl = new MapControl { Map = map };

            mapControl.FixedZoomOutTool.Execute(); // zoom out a bit

            mapControl.ActivateTool(mapControl.CoverageProfileTool);

            WindowsFormsTestHelper.ShowModal(mapControl);
        }
    }
}