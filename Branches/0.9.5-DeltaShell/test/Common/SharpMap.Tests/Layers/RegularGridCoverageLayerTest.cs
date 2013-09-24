using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using DelftTools.Functions.Filters;
using GisSharpBlog.NetTopologySuite.Geometries;
using NetTopologySuite.Extensions.Coverages;
using NUnit.Framework;
using SharpMap.Layers;
using SharpMap.Rendering.Thematics;

namespace SharpMap.Tests.Layers
{
    [TestFixture]
    public class RegularGridCoverageLayerTest
    {
        [Test]
        public void ChangesInLayersBubbleUpToMap()
        {
            var coverage = new RegularGridCoverage(10, 10, 1, 1);
            var layer = new RegularGridCoverageLayer { Coverage = coverage };
            int callCount = 0;
            var senders = new object[] {coverage};
            var propertyNames = new[] {"Name"};

            ((INotifyPropertyChanged)layer).PropertyChanged += (sender,args)=>
            {
                Assert.AreEqual(senders[callCount], sender);
                Assert.AreEqual(propertyNames[callCount], args.PropertyName);

                callCount++;
            };

            //change the name of the layer
            coverage.Name = "new name";

            //should result in property changed of map
            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void EnvelopeChangesWhenCoverageIsCleared()
        {
            var coverage = new RegularGridCoverage(10, 10, 1, 1);
            var layer = new RegularGridCoverageLayer { Coverage = coverage };
            var envelope = new Envelope(0, 10, 0, 10);
            Assert.AreEqual(envelope, layer.Envelope);

            coverage.Clear();
            var EmptyEnvelope = new Envelope(0, 0, 0, 0);
            Assert.AreEqual(EmptyEnvelope, layer.Envelope);
        }

        [Test]
        public void ClearingCoverageCauseRenderRequired()
        {
            var coverage = new RegularGridCoverage(10, 10, 1, 1);
            var layer = new RegularGridCoverageLayer { Coverage = coverage };
            layer.Map = new Map(new Size(10,10));
            layer.Render();
            Assert.IsFalse(layer.RenderRequired);

            //action!
            coverage.Clear();

            Assert.IsTrue(layer.RenderRequired);
        }

        [Test]
        public void CheckThemeOfCoverageWithoutValidValues()
        {
            var coverage = new RegularGridCoverage(2, 2, 10, 10);

            coverage.Components[0].Clear();
            coverage.SetValues(new[] {-999.0, -999.0, double.PositiveInfinity , double.NaN},
                               new VariableValueFilter<double>(coverage.X, new double[] {0, 1}),
                               new VariableValueFilter<double>(coverage.Y, new double[] {0, 1})
                );

            coverage.Components[0].NoDataValues.Add(-999.0);

            var layer = new RegularGridCoverageLayer { Coverage = coverage };
            layer.Map = new Map(new Size(10, 10));
            layer.Map.Zoom = layer.Map.Zoom / 100;

            layer.Render();
            
            var gradientTheme = layer.Theme as GradientTheme;

            Assert.IsNotNull(gradientTheme);
                     
            Assert.AreEqual(double.MinValue, gradientTheme.Min );
            Assert.AreEqual(double.MaxValue, gradientTheme.Max);
        }

        [Test]
        public void GetTimesFromFilteredCoverage()
        {
            var coverage = new RegularGridCoverage(2, 2, 10, 10);
            coverage.IsTimeDependent = true;
            var firstTime = new DateTime(2000, 1, 1);
            var secondTime = new DateTime(2000, 1, 2);

            coverage.Time.Values.Add(firstTime);
            coverage.Time.Values.Add(secondTime);


            //create a layer 
            var layer = new RegularGridCoverageLayer {Coverage = coverage};

            Assert.AreEqual(2, layer.Times.Count());

            //action! set the time
            layer.SetCurrentTimeSelection(secondTime, null);

            //assert the start time got there
            Assert.AreEqual(secondTime,layer.TimeSelectionStart);
            //assert the rendercoverage got updated
            var timeFilter =
                (VariableValueFilter<DateTime>)
                layer.RenderedCoverage.Filters.OfType<IVariableValueFilter>().FirstOrDefault();
            Assert.AreEqual(new[] {secondTime}, timeFilter.Values);

        }
    }
}
