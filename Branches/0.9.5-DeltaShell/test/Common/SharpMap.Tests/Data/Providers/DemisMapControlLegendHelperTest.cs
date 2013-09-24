using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using DelftTools.TestUtils;
using GeoAPI.Extensions.Coverages;
using NUnit.Framework;
using SharpMap.Api;
using SharpMap.Extensions.Data.Providers;
using SharpMap.Layers;
using SharpMap.Rendering.Thematics;
using SharpMap.Styles;

namespace SharpMap.Tests.Data.Providers
{
    [TestFixture]
    public class DemisMapControlLegendHelperTest
    {
        [Test, Category(TestCategory.DataAccess)]
        public void GenerateThemeForCategorialBilFileWithLegendFile()
        {
            const string fileName = @"..\..\..\..\..\test-data\DeltaShell\DeltaShell.Plugins.SharpMapGis.Tests\RasterData\bodem.bil";

            var rasterLayer = new RegularGridCoverageLayer();
            var rasterFeatureProvider = new GdalFeatureProvider {Path = fileName};
            rasterLayer.DataSource = rasterFeatureProvider;
            IRegularGridCoverage grid = rasterLayer.Grid;

            ITheme theme = DemisMapControlLegendHelper.ConvertLegendToTheme(fileName, "value", grid);

            ICollection colors = new List<string>(new string[]
            {
                "0000FF",
                "00FFFF",
                "00FF00",
                "FFFF00",
                "FF0000"
            });
            ICollection values = new List<double>(new double[] { 1, 2, 3, 4, 5 });
            Assert.IsInstanceOfType(typeof(CategorialTheme), theme);
            foreach(IThemeItem themeItem in theme.ThemeItems)
            {
                Assert.IsInstanceOfType(typeof(CategorialThemeItem), themeItem);
                Assert.IsInstanceOfType(typeof(VectorStyle), themeItem.Style);

                double value = Convert.ToDouble(((CategorialThemeItem) themeItem).Value);
                Assert.Contains(value, values);
                ((List<double>) values).Remove(value);

                string color = FromColorToBGRString(((SolidBrush) ((VectorStyle) themeItem.Style).Fill).Color);
                Assert.Contains(color, colors);
                ((List<string>) colors).Remove(color);
            }

            Assert.IsTrue(colors.Count == 0);
            Assert.IsTrue(values.Count == 0);
        }

        [Test, Category(TestCategory.DataAccess)]
        public void GenerateThemeForQuantityBilFileWithLegendFile()
        {
            const string fileName = @"..\..\..\..\..\test-data\DeltaShell\DeltaShell.Plugins.SharpMapGis.Tests\RasterData\FloatValues.bil";

            RegularGridCoverageLayer rasterLayer = new RegularGridCoverageLayer();
            GdalFeatureProvider rasterFeatureProvider = new GdalFeatureProvider {Path = fileName};
            rasterLayer.DataSource = rasterFeatureProvider;
            IRegularGridCoverage grid = rasterLayer.Grid;

            ITheme theme = DemisMapControlLegendHelper.ConvertLegendToTheme(fileName, "value", grid);

            ICollection colors = new List<string>(new string[]
            {
                "000000",
                "E1E1FE",
                "DCD9FE",
                "D6D0FE",
                "D0C8FF",
                "CBC0FF",
                "C09EFF",
                "B57BFF",
                "A959FF",
                "9E36FF",
                "9314FF"
            });
            ICollection values = new List<double>(new double[]
            {
                -16.9996604919434,
                -13.5799999237061,
                -10.1499996185303,
                -6.73000001907349,
                -3.29999995231628,
                .119999997317791,
                3.54999995231628,
                6.96999979019165,
                10.3900003433228,
                13.8199996948242,
                17.2399997711182
            });
            Assert.IsInstanceOfType(typeof(QuantityTheme), theme);
            foreach (IThemeItem themeItem in theme.ThemeItems)
            {
                Assert.IsInstanceOfType(typeof(QuantityThemeItem), themeItem);
                Assert.IsInstanceOfType(typeof(VectorStyle), themeItem.Style);

                double value = ((QuantityThemeItem) themeItem).Interval.Max;
                Assert.Contains(value, values);
                ((List<double>) values).Remove(value);

                string color = FromColorToBGRString(((SolidBrush) ((VectorStyle) themeItem.Style).Fill).Color);
                Assert.Contains(color, colors);
                ((List<string>) colors).Remove(color);
            }

            Assert.IsTrue(colors.Count == 0);
            Assert.IsTrue(values.Count == 0);
        }

        [Test,Category(TestCategory.DataAccess)]
        public void GenerateThemeForBilFileWithoutLegendFile()
        {
            const string fileName = @"..\..\..\..\..\test-data\DeltaShell\DeltaShell.Plugins.SharpMapGis.Tests\RasterData\SchematisatieUInt.bil";

            RegularGridCoverageLayer rasterLayer = new RegularGridCoverageLayer();
            GdalFeatureProvider rasterFeatureProvider = new GdalFeatureProvider();
            rasterFeatureProvider.Open(fileName);
            rasterLayer.DataSource = rasterFeatureProvider;
            IRegularGridCoverage grid = rasterLayer.Grid;

            ITheme theme = DemisMapControlLegendHelper.ConvertLegendToTheme(fileName, "value", grid);

            Assert.IsTrue(((GradientTheme) theme).Max == 9.0);
            Assert.IsTrue(((GradientTheme) theme).Min == 1.0);

            Assert.IsInstanceOfType(typeof(GradientTheme), theme);
            foreach (IThemeItem themeItem in theme.ThemeItems)
            {
                Assert.IsInstanceOfType(typeof(GradientThemeItem), themeItem);
                Assert.IsInstanceOfType(typeof(VectorStyle), themeItem.Style);
            }
         }

        private string FromColorToBGRString(Color color)
        {
            byte r = color.R;
            byte g = color.G;
            byte b = color.B;

            StringBuilder sb = new StringBuilder();
            sb.Append(FromByteToHexString(b));
            sb.Append(FromByteToHexString(g));
            sb.Append(FromByteToHexString(r));
            return sb.ToString();
        }

        private string FromByteToHexString(byte b)
        {
            StringBuilder sb = new StringBuilder();

            char[] hexChars = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
                'A', 'B', 'C', 'D', 'E', 'F' };

            int digit1 = b/16;
            int digit2 = b%16;

            sb.Append(hexChars[digit1]);
            sb.Append(hexChars[digit2]);

            return sb.ToString();
        }
    }
}
