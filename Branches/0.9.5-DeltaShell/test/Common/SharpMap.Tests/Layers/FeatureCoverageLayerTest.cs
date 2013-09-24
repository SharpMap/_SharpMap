using System;
using System.Collections.Generic;
using System.Drawing;
using DelftTools.Functions.Generic;
using DelftTools.TestUtils;
using DelftTools.Utils.Collections.Generic;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;
using NetTopologySuite.Extensions.Coverages;
using NetTopologySuite.Extensions.Networks;
using NUnit.Framework;
using SharpMap.Layers;
using SharpMap.Rendering.Thematics;
using SharpMap.Styles;
using SharpMap.UI.Forms;
using Point = GisSharpBlog.NetTopologySuite.Geometries.Point;

namespace SharpMap.Tests.Layers
{
    [TestFixture]
    public class FeatureCoverageLayerTest
    {
        [Test]
        public void GeometryTypeGeneratedThemeIsCorrect()
        {
            var featureCoverage = new FeatureCoverage();

            featureCoverage.Arguments.Add(new Variable<Branch>());
            featureCoverage.Components.Add(new Variable<double>());

            var branches = CreateNBranchesNetwork(2);

            featureCoverage.Features = new EventedList<IFeature>(branches);
            featureCoverage[branches[0]] = 1.0;
            featureCoverage[branches[1]] = 2.0;
            
            var fcLayer = new FeatureCoverageLayer { Coverage = featureCoverage };

            Assert.AreEqual(typeof(ILineString), fcLayer.Style.GeometryType);
            Assert.AreEqual(typeof(ILineString), ((VectorStyle)fcLayer.Theme.ThemeItems[0].Style).GeometryType);
        }

        [Test]
        public void LabelLayerGetLabelIsDelegatedCorrectly()
        {
            var featureCoverage = new FeatureCoverage();

            featureCoverage.Arguments.Add(new Variable<IFeature>());
            featureCoverage.Components.Add(new Variable<double>());

            var branches = CreateNBranchesNetwork(2);

            featureCoverage.Features = new EventedList<IFeature>(branches);
            featureCoverage[branches[0]] = 1.0;
            featureCoverage[branches[1]] = 2.0;

            var fcLayer = new FeatureCoverageLayer {Coverage = featureCoverage};

            var labelLayer = fcLayer.LabelLayer;

            Assert.IsNotNull(labelLayer.DataSource);
            Assert.IsNotNull(labelLayer.LabelStringDelegate);
            
            Assert.AreEqual(null, labelLayer.LabelStringDelegate(branches[0]));

            labelLayer.LabelColumn = featureCoverage.Components[0].Name;
            Assert.AreEqual((1.0).ToString("N3"), labelLayer.LabelStringDelegate(branches[0]));

            labelLayer.LabelColumn = "nonsense";
            Assert.AreEqual(null, labelLayer.LabelStringDelegate(branches[0]));
        }

        [Test]
        [Category(TestCategory.WindowsForms)]
        public void ShowFeatureCoverageLayerWithLabelLayer()
        {
            var featureCoverage = new FeatureCoverage();

            featureCoverage.Arguments.Add(new Variable<IFeature>());
            featureCoverage.Components.Add(new Variable<double>());

            var branches = CreateNBranchesNetwork(2);

            featureCoverage.Features = new EventedList<IFeature>(branches);
            featureCoverage[branches[0]] = 1.0;
            featureCoverage[branches[1]] = 2.0;

            var fcLayer = new FeatureCoverageLayer
                              {
                                  Coverage = featureCoverage,
                                  LabelLayer =
                                      {
                                          LabelColumn = featureCoverage.Components[0].Name,
                                          Visible = true
                                      }
                              };

            var map = new Map();
            map.Layers.Add(fcLayer);
            var mapControl = new MapControl { Map = map, AllowDrop = false };
            WindowsFormsTestHelper.ShowModal(mapControl);
        }

        [Test]
        public void LabelLayerIsCorrect()
        {
            var featureCoverage = new FeatureCoverage();

            featureCoverage.Arguments.Add(new Variable<Branch>());
            featureCoverage.Components.Add(new Variable<double>());

            var branches = CreateNBranchesNetwork(2);

            featureCoverage.Features = new EventedList<IFeature>(branches);
            featureCoverage[branches[0]] = 1.0;
            featureCoverage[branches[0]] = 2.0;

            var fcLayer = new FeatureCoverageLayer { Coverage = featureCoverage };

            var fcLayerCloned = (FeatureCoverageLayer)fcLayer.Clone();

            Assert.AreSame(fcLayer.Coverage, fcLayerCloned.Coverage);
        }

        private static IList<IFeature> CreateNBranchesNetwork(int size)
        {
            var network = new Network { Name = "Test network" };

            var branches = new Branch[size];
            for(int i = 0; i < size; i++)
            {
                var branch = new Branch
                                 {
                                     Name = String.Format("Link{0}", i + 1),
                                     Geometry =
                                         new LineString(new[]
                                                        {
                                                            new Point(i*10, 0).Coordinate,
                                                            new Point((i + 1)*10, 0).Coordinate
                                                        })
                                 };
                branches[i] = branch;
            }
            network.Branches.AddRange(branches);

            return branches;
        }

        [Test]
        public void GradientThemeRescaledAsExpected()
        {
            var featureCoverage = new FeatureCoverage();

            featureCoverage.Arguments.Add(new Variable<IFeature>());
            featureCoverage.Components.Add(new Variable<double>());

            var branches = CreateNBranchesNetwork(5);

            featureCoverage.Features = new EventedList<IFeature>(branches);

            double variableValue = 0.0;
            var expectedColors = new Dictionary<IFeature, Color>(5);
            double minvalue = 1.0;
            double maxvalue = 3.0;
            foreach (var branch in branches)
            {
                featureCoverage[branch] = variableValue;
                int grayscaleValue = variableValue <= minvalue
                                         ? 0
                                         : variableValue >= maxvalue
                                               ? 255
                                               : Convert.ToInt32(255*(variableValue - minvalue)/(maxvalue - minvalue));
                expectedColors[branch] = Color.FromArgb(255, grayscaleValue, grayscaleValue, grayscaleValue);
                variableValue += 1;
            }

            var theme = ThemeFactory.CreateGradientTheme("", null, new ColorBlend(new[] { Color.Black, Color.White },
                                                                      new[] {0f, 1f}), minvalue, maxvalue, 3, 3, false, true,
                                             3);

            var fcLayer = new FeatureCoverageLayer
            {
                AutoUpdateThemeOnDataSourceChanged = false,
                Coverage = featureCoverage,
                Theme = theme
            };

            foreach (var branch in branches)
            {
                AssertColor(expectedColors[branch], fcLayer.Theme.GetFillColor((double)featureCoverage[branch]));
            }

            fcLayer.Theme.ScaleTo(0.0, 2.0);

            var min = ((GradientTheme)fcLayer.Theme).Min;
            var max = ((GradientTheme)fcLayer.Theme).Max;
            Assert.AreEqual(0.0, min);
            Assert.AreEqual(2.0, max);
            foreach (var branch in branches)
            {
                variableValue = (double) featureCoverage[branch];
                int greyScaleValue = variableValue <= min
                                         ? 0
                                         : variableValue >= max
                                               ? 255
                                               : Convert.ToInt32(255 * (variableValue - min) / (max - min));
                AssertColor(Color.FromArgb(255, greyScaleValue, greyScaleValue, greyScaleValue), fcLayer.Theme.GetFillColor(variableValue));
            }
        }

        /// <summary>
        /// Asserts that <paramref name="expectedColor"/> argb values are the same as those of
        /// <paramref name="actualColor"/>
        /// </summary>
        /// <param name="expectedColor"></param>
        /// <param name="actualColor"></param>
        private static void AssertColor(Color expectedColor, Color actualColor)
        {
            Assert.AreEqual(expectedColor.A, actualColor.A);
            Assert.AreEqual(expectedColor.R, actualColor.R);
            Assert.AreEqual(expectedColor.G, actualColor.G);
            Assert.AreEqual(expectedColor.B, actualColor.B);
        }
    }
}