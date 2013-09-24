using DelftTools.Utils.Data;
using GeoAPI.Extensions.Feature;
using GeoAPI.Extensions.Feature.Generic;

namespace NetTopologySuite.Extensions.Features.Generic
{
    using DelftTools.Utils.Aop;

    [Entity(FireOnCollectionChange = false)]
    public class FeatureData<TData, TFeature> : Unique<long>, IFeatureData<TData, TFeature> where TFeature : IFeature
    {
        private string name;
        private TData data;
        private TFeature feature;
        
        public virtual string Name
        {
            get { return name; }
            set { name = value; }
        }

        IFeature IFeatureData.Feature
        {
            get { return Feature; }
            set { Feature = (TFeature)value; }
        }

        object IFeatureData.Data
        {
            get { return Data; }
            set { Data = (TData)value; }
        }

        [Aggregation]
        public virtual TFeature Feature
        {
            get { return feature; }
            set
            {
                feature = value;
                UpdateName();
            }
        }

        [Aggregation]
        public virtual TData Data
        {
            get { return data; }
            set
            {
                data = value;
                UpdateName();
            }
        }

        [EditAction]
        protected void UpdateName()
        {
            Name = Feature + " - " + Data;
        }
    }
}