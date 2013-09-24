using System;
using System.Drawing;
using System.Drawing.Imaging;
using DelftTools.Functions.Generic;
using NetTopologySuite.Extensions.Coverages;
using NUnit.Framework;
using SharpMap.Layers;
using SharpMap.Rendering;
using SharpMap.Tests.TestObjects;
using Point = GisSharpBlog.NetTopologySuite.Geometries.Point;

namespace SharpMap.Tests.Rendering
{
    [TestFixture]
    public class FeatureCoverageRendererTest
    {
        [Test]
        public void RenderFeatureCoverageWithNoTimes()
        {
            var featureCoverage = new FeatureCoverage {IsTimeDependent = true};
            //define a 
            
            featureCoverage.Arguments.Add(new Variable<SimpleFeature>("feature")); // 1st dimension is feature
            featureCoverage.Components.Add(new Variable<double>());

            featureCoverage[new DateTime(2000, 1, 1), new SimpleFeature(2,new Point(1,1))] = 10.0;

            var featureCoverageRenderer = new FeatureCoverageRenderer();
            var image = new Bitmap(100,100, PixelFormat.Format32bppPArgb);

            var graphics = Graphics.FromImage(image);
            var featureCoverageLayer = new FeatureCoverageLayer {Coverage = featureCoverage,Map = new Map(new Size(100,100))};
            
            featureCoverage.Time.Values.Clear();//remove all time values
            //render
            featureCoverageRenderer.Render(featureCoverage, graphics, featureCoverageLayer);
        }
        
    }
}