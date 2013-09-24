using System;
using DelftTools.Utils.Collections.Generic;
using DelftTools.Utils.Data;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;
using QuickGraph;

namespace GeoAPI.Extensions.Networks
{
    /// <summary>
    /// NHibernate requires concrete class implementation
    /// to remove dependency of networkeditor plugin this stub was introduced
    /// </summary>
    public class BranchStub : Unique<long>, IBranch
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

        public virtual int CompareTo(IBranch other)
        {
            throw new NotImplementedException();
        }

        INode IEdge<INode>.Source { get { return null; }  }
        INode IBranch.Target { get; set; }
        public virtual double Length
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public virtual bool IsLengthCustom
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public virtual int OrderNumber
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public virtual IEventedList<IBranchFeature> BranchFeatures
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        INode IBranch.Source { get; set; }
        INode IEdge<INode>.Target { get { return null; } }
    }
}