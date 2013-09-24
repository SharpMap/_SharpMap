using System;
using DelftTools.Utils.Data;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;

namespace GeoAPI.Extensions.Networks
{
    /// <summary>
    /// TODO: remove these classes and make it work without stubs
    /// NHibernate requires concrete class implementation
    /// to remove dependency of networkeditor plugin this stub was introduced
    /// </summary>
    public class NodeFeatureStub : Unique<long>, INodeFeature
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

        public virtual INode Node
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }
    }
}