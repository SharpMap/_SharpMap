using System;
using DelftTools.Utils.Data;
using GeoAPI.Extensions.Feature;
using GeoAPI.Extensions.Networks;
using GeoAPI.Geometries;

namespace SharpMapTestUtils.TestClasses
{
    public class TestBranchFeature : Unique<long>, IBranchFeature
    {
        #region IBranchFeature Members
        
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

        public void CopyFrom(object source)
        {
            throw new NotImplementedException();
        }
    }
}