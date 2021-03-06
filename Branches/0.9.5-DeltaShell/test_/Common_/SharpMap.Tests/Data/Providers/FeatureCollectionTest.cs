﻿using System;
using System.Collections;
using System.Collections.Generic;
using DelftTools.Utils;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;
using NetTopologySuite.Extensions.Coverages;
using NUnit.Framework;
using SharpMap.Data.Providers;

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
            FeatureCollection featureCollection = new FeatureCollection(list,typeof(string));

            // TODO: WHERE ARE ASSERTS????!~?!?!?!#@$!@#
        }

        [Test]
        public void AddingValidTypeIsOk()
        {
            IList list = new List<IFeature>();
            FeatureCollection featureCollection = new FeatureCollection(list, typeof(NetworkLocation));

            // TODO: WHERE ARE ASSERTS????!~?!?!?!#@$!@#
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

            var featureCollection = new FeatureCollection(features, typeof(TimeDependentFeature))
                                        {
                                            TimeSelectionStart = new DateTime(2001, 1, 1)
                                        };

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

            var featureCollection = new FeatureCollection(features, typeof(TimeDependentFeature))
            {
                TimeSelectionStart = new DateTime(2001, 1, 1),
                TimeSelectionEnd = new DateTime(2001, 2, 1)
            };

            featureCollection.Features.Count
                .Should().Be.EqualTo(1);
        }

        public class TimeDependentFeature : IFeature, ITimeDependent
        {
            public long Id { get; set; }
            
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
