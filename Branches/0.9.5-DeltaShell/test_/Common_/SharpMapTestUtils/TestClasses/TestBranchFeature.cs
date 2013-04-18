using System;
using GeoAPI.Extensions.Feature;
using GeoAPI.Extensions.Networks;
using GeoAPI.Geometries;

namespace SharpMapTestUtils.TestClasses
{
    public class TestBranchFeature : IBranchFeature
    {
        #region IBranchFeature Members

        public long Id { get; set; }

        public object Clone()
        {
            throw new NotImplementedException();
        }

        public IGeometry Geometry { get; set; }
        public IFeatureAttributeCollection Attributes { get; set; }

        public int CompareTo(INetworkFeature other)
        {
            throw new NotImplementedException();
        }

        public int CompareTo(object obj)
        {
            throw new NotImplementedException();
        }

        public string Name { get; set; }

        public INetwork Network { get; set; }
        public string Description
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public int CompareTo(IBranchFeature other)
        {
            throw new NotImplementedException();
        }

        public IBranch Branch { get; set; }
        public double Offset { get; set; }
        public double Length { get; set; }

        #endregion

        [FeatureAttribute]
        public string TestProperty { get; set; }
    }
}