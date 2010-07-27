using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using GeoAPI.Extensions.Feature;

namespace NetTopologySuite.Extensions.Features
{
    [Serializable]
    public class DictionaryFeatureAttributeCollection: Dictionary<string, object>, IFeatureAttributeCollection, ISerializable
    {
        public DictionaryFeatureAttributeCollection()
        {
        }

        protected DictionaryFeatureAttributeCollection(SerializationInfo info, StreamingContext context): base(info, context)
        {
        }

        public object Clone()
        {
            var copy = new DictionaryFeatureAttributeCollection();
            foreach (var attribute in this)
            {
                copy[attribute.Key] = attribute.Value;
            }
            return copy;
        }

        public object this[int index]
        {
            get { return Values.Take(index).First(); }
            set { this[Keys.Take(index).First()] = value; }
        }
    }
}