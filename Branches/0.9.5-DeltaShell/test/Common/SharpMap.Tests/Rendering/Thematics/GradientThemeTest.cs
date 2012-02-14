using System.Collections.Generic;
using System.Drawing;
using DelftTools.TestUtils;
using log4net;
using log4net.Config;
using NUnit.Framework;
using SharpMap.Rendering.Thematics;
using SharpMap.Styles;

namespace SharpMap.Tests.Rendering.Thematics
{
    [TestFixture]
    public class GradientThemeTest
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(GradientThemeTest));

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            LogHelper.ConfigureLogging();
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            LogHelper.ResetLogging();
        }

        [Test]
        public void ReturnMaxColorForMaxValue()
        {
            var minVectorStyle = new VectorStyle { Fill = new SolidBrush(Color.Red) };
            var maxVectorStyle = new VectorStyle { Fill = new SolidBrush(Color.Blue) };

            var theme = new GradientTheme("red to blue", 10.0, 100.123, minVectorStyle, maxVectorStyle, null, null, null);

            var color = theme.GetFillColor(100.123);

            Assert.AreEqual(Color.Blue.A, color.A);
            Assert.AreEqual(Color.Blue.R, color.R);
            Assert.AreEqual(Color.Blue.G, color.G);
            Assert.AreEqual(Color.Blue.B, color.B);
        }

        [Test]
        public void GenerateThemeWithMaxDoubleAndMinDoubleValue()
        {
            var minVectorStyle = new VectorStyle { Fill = new SolidBrush(Color.Red) };
            var maxVectorStyle = new VectorStyle { Fill = new SolidBrush(Color.Blue) };

            var theme = new GradientTheme("red to blue", double.MinValue, double.MaxValue, minVectorStyle, maxVectorStyle, null, null, null);

            var color = theme.GetFillColor(100);

            Assert.AreEqual(255, color.A);
            Assert.AreEqual(127, color.R);
            Assert.AreEqual(0, color.G);
            Assert.AreEqual(127, color.B);
        }

        [Test]
        public void CloneGradientThemeWithNoDataValues()
        {
            var colorBlend = new ColorBlend(new[]{Color.Black, Color.White}, new[]{0.0f,1.0f});
            var gradientTheme = new GradientTheme("aa", 0, 20, new VectorStyle(), new VectorStyle(), colorBlend,
                                                  colorBlend, colorBlend,5)
                                                  {NoDataValues = new List<double>{-9999}};

            var gradientThemeClone = (GradientTheme)gradientTheme.Clone();

            Assert.AreEqual(gradientTheme.NoDataValues, (gradientThemeClone).NoDataValues);
            Assert.AreEqual(5,gradientThemeClone.NumberOfClasses);
            Assert.AreEqual(2,gradientThemeClone.FillColorBlend.Colors.Length);
        }

        [Test]
        public void GenerateThemeItems()
        {
            var colorBlend = new ColorBlend(new[] { Color.Black, Color.White }, new[] { 0.0f, 1.0f });
            var gradientTheme = new GradientTheme("aa", 0, 3, new VectorStyle(), new VectorStyle(), colorBlend,
                                                  colorBlend, colorBlend,3) { NoDataValues = new List<double> { -9999 } };
            //assert 3 items were generated..at 0,1.5 and 3
            Assert.AreEqual(3,gradientTheme.ThemeItems.Count);
            Assert.AreEqual("0",gradientTheme.ThemeItems[0].Range);
            //use toString to make sure the machines decimal separator is used
            Assert.AreEqual(1.5.ToString(), gradientTheme.ThemeItems[1].Range);
            Assert.AreEqual("3", gradientTheme.ThemeItems[2].Range);
        }
    }
}
