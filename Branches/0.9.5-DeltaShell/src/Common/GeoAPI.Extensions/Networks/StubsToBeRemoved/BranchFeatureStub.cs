using System;
using DelftTools.Utils.Data;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;

namespace GeoAPI.Extensions.Networks
{
    /// <summary>
    /// NHibernate requires concrete class implementation
    /// to remove dependency of networkeditor plugin this stub was introduced
    /// </summary>
    public class BranchFeatureStub : Unique<long>, IBranchFeature
    {
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

        public int CompareTo(IBranchFeature other)
        {
            throw new NotImplementedException();
        }

        public IBranch Branch
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public double Offset
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public double Length
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public void CopyFrom(object source)
        {
            throw new NotImplementedException();
        }
    }
}