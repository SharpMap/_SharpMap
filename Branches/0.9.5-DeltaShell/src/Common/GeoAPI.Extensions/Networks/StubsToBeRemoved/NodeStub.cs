using System;
using DelftTools.Utils.Collections.Generic;
using DelftTools.Utils.Data;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;

namespace GeoAPI.Extensions.Networks
{
    /// <summary>
    /// NHibernate requires concrete class implementation
    /// HACK: to remove dependency of networkeditor plugin this stub was introduced
    /// </summary>
    public class NodeStub : Unique<long>, INode
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

        public virtual IEventedList<IBranch> IncomingBranches
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public virtual IEventedList<IBranch> OutgoingBranches
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public virtual IEventedList<INodeFeature> NodeFeatures
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public virtual bool IsConnectedToMultipleBranches
        {
            get { throw new NotImplementedException(); }
        }

        public virtual bool IsOnSingleBranch
        {
            get { throw new NotImplementedException(); }
        }
    }
}