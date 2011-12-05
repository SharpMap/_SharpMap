using DelftTools.Utils.Aop.NotifyPropertyChanged;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;

namespace SharpMapTestUtils.TestClasses
{
    [NotifyPropertyChanged]
    public class SampleFeature : IFeature
    {
        private long id;
        private IGeometry geometry;
        private IFeatureAttributeCollection attributes;
        
        private int integerProperty;
        private double doubleProperty;
        private string stringProperty;

        public SampleFeature()
        {
        }

        public SampleFeature(int i)
        {
            integerProperty = i;
        }

        #region IFeature members

        public long Id
        {
            get { return id; }
            set { id = value; }
        }

        public IGeometry Geometry
        {
            get { return geometry; }
            set { geometry = value; }
        }

        public IFeatureAttributeCollection Attributes
        {
            get { return attributes; }
            set { attributes = value; }
        }

        #endregion IFeature

        [FeatureAttribute]
        public int IntegerProperty
        {
            get { return integerProperty; }
            set { integerProperty = value; }
        }

        [FeatureAttribute]
        public double DoubleProperty
        {
            get { return doubleProperty; }
            set { doubleProperty = value; }
        }

        [FeatureAttribute]
        public string StringProperty
        {
            get { return stringProperty; }
            set { stringProperty = value; }
        }

        public object Clone()
        {
            return new SampleFeature
                       {
                           Geometry = (IGeometry) (Geometry == null ? null : Geometry.Clone()),
                           DoubleProperty = DoubleProperty,
                           IntegerProperty = IntegerProperty,
                           StringProperty = StringProperty
                       };
        }
    }
}