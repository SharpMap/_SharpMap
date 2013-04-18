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
        public void CloneGradientThemeWithNoDataValues()
        {
            var colorBlend = new ColorBlend(new[]{Color.Black, Color.White}, new[]{0.0f,1.0f});
            var gradientTheme = new GradientTheme("aa", 0, 20, new VectorStyle(), new VectorStyle(), colorBlend,
                                                  colorBlend, colorBlend)
                                                  {NoDataValues = new List<double>{-9999}};

            var gradientThemeClone = gradientTheme.Clone();

            Assert.AreEqual(gradientTheme.NoDataValues, ((GradientTheme)gradientThemeClone).NoDataValues);
        }
    }
}
