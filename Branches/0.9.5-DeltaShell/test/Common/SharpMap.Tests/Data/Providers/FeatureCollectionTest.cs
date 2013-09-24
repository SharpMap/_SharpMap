using System;
using System.Collections;
using System.Collections.Generic;
using DelftTools.Utils;
using DelftTools.Utils.Data;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;
using NetTopologySuite.Extensions.Coverages;
using NUnit.Framework;
using SharpMap.Data.Providers;

using SharpTestsEx;

namespace SharpMap.Tests.Data.Providers
{
    [TestFixture]
    public class FeatureCollectionTest
    {
        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void AddingInvalidTypeGivesArgumentException()
        {
            IList list = new List<IFeature>();    
            var featureCollection = new FeatureCollection(list,typeof(string));
        }

        [Test]
        public void AddingValidTypeIsOk()
        {
            IList list = new List<IFeature>();
            var featureCollection = new FeatureCollection(list, typeof(NetworkLocation));
            Assert.AreEqual(list, featureCollection.Features);
        }

        [Test]
        public void FeatureCollectionTimesMustBeExtractedFromFeature()
        {
            var features = new[]
                               {
                                   new TimeDependentFeature {Time = new DateTime(2000, 1, 1)},
                                   new TimeDependentFeature {Time = new DateTime(2001, 1, 1)},
                                   new TimeDependentFeature {Time = new DateTime(2002, 1, 1)}
                               };

            var featureCollection = new FeatureCollection(features, typeof (TimeDependentFeature));

            featureCollection.Times
                .Should().Have.SameSequenceAs(new[]
                                                  {
                                                      new DateTime(2000, 1, 1),
                                                      new DateTime(2001, 1, 1),
                                                      new DateTime(2002, 1, 1)
                                                  });
        }

        [Test]
        public void FilterFeaturesUsingStartTime()
        {
            var features = new[]
                              {
                                  new TimeDependentFeature {Time = new DateTime(2000, 1, 1)},
                                  new TimeDependentFeature {Time = new DateTime(2001, 1, 1)}
                              };

            var featureCollection = new FeatureCollection(features, typeof(TimeDependentFeature));

            featureCollection.SetCurrentTimeSelection(new DateTime(2001, 1, 1), null);

            featureCollection.Features.Count
                .Should().Be.EqualTo(1);
        }

        [Test]
        public void FilterFeaturesUsingTimeRange()
        {
            var features = new[]
                              {
                                  new TimeDependentFeature {Time = new DateTime(2000, 1, 1)},
                                  new TimeDependentFeature {Time = new DateTime(2001, 1, 1)},
                                  new TimeDependentFeature {Time = new DateTime(2002, 1, 1)}
                              };

            var featureCollection = new FeatureCollection(features, typeof (TimeDependentFeature));
            featureCollection.SetCurrentTimeSelection(new DateTime(2001, 1, 1), new DateTime(2001, 2, 1));

            featureCollection.Features.Count
                .Should().Be.EqualTo(1);
        }

        private class TimeDependentFeature : Unique<long>, IFeature, ITimeDependent
        {
            public object Clone()
            {
                throw new NotImplementedException();
            }

            public IGeometry Geometry { get; set; }
            public IFeatureAttributeCollection Attributes { get; set; }
            public DateTime Time { get; set; }
        }
    }
}
