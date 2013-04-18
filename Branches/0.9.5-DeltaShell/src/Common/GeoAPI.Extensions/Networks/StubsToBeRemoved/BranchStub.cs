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

        public int CompareTo(IBranch other)
        {
            throw new NotImplementedException();
        }

        INode IEdge<INode>.Source { get { return null; }  }
        INode IBranch.Target { get; set; }
        public double Length
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public bool IsLengthCustom
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public IEventedList<IBranchFeature> BranchFeatures
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        INode IBranch.Source { get; set; }
        INode IEdge<INode>.Target { get { return null; } }
    }
}