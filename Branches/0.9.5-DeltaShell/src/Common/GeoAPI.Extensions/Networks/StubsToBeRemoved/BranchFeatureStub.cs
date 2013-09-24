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
        public virtual object Clone()
        {
            throw new NotImplementedException();
        }

        public virtual IGeometry Geometry
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public virtual IFeatureAttributeCollection Attributes
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public virtual int CompareTo(INetworkFeature other)
        {
            throw new NotImplementedException();
        }

        public virtual int CompareTo(object obj)
        {
            throw new NotImplementedException();
        }

        public virtual string Name
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public virtual INetwork Network
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public virtual string Description
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public virtual int CompareTo(IBranchFeature other)
        {
            throw new NotImplementedException();
        }

        public virtual IBranch Branch
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public virtual double Chainage
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public virtual double Length
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public virtual void CopyFrom(object source)
        {
            throw new NotImplementedException();
        }
    }
}