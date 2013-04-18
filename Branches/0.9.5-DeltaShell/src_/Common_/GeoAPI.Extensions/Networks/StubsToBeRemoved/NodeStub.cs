using System;
using System.Collections.Generic;
using DelftTools.Utils.Collections.Generic;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;

namespace GeoAPI.Extensions.Networks
{
    /// <summary>
    /// NHibernate requires concrete class implementation
    /// HACK: to remove dependency of networkeditor plugin this stub was introduced
    /// </summary>
    public class NodeStub:INode
    {
        public long Id
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
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

        public IList<IBranch> IncomingBranches
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public IList<IBranch> OutgoingBranches
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public IEventedList<INodeFeature> NodeFeatures
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public bool IsBoundaryNode
        {
            get { throw new NotImplementedException(); }
        }
    }
}