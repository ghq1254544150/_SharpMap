using DelftTools.Utils.Aop.NotifyPropertyChanged;
using GeoAPI.Extensions.Feature;
using GeoAPI.Extensions.Feature.Generic;

namespace NetTopologySuite.Extensions.Features.Generic
{
    [NotifyPropertyChanged]
    public class FeatureData<TData, TFeature> : IFeatureData<TData, TFeature> where TFeature : IFeature
    {
        private string name;
        private TData data;
        private TFeature feature;

        public long Id { get; set; }

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

        public virtual TFeature Feature
        {
            get { return feature; }
            set
            {
                feature = value;
                UpdateName();
            }
        }

        public virtual TData Data
        {
            get { return data; }
            set
            {
                data = value;
                UpdateName();
            }
        }

        protected void UpdateName()
        {
            Name = Feature + " - " + Data;
        }
    }
}