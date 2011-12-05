using System;
using GeoAPI.Extensions.Feature;
using GeoAPI.Extensions.Networks;
using GeoAPI.Geometries;

namespace SharpMapTestUtils.TestClasses
{
    public class TestNodeFeature : INodeFeature
    {
        public long Id
        {
            get; set;
        }

        public object Clone()
        {
            throw new NotImplementedException();
        }

        public IGeometry Geometry
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public IFeatureAttributeCollection Attributes
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public int CompareTo(INetworkFeature other)
        {
            throw new NotImplementedException();
        }

        public int CompareTo(object obj)
        {
            throw new NotImplementedException();
        }

        public string Name
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public INetwork Network
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public string Description
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public INode Node
        {   
            get; set;
        }
    }
}
