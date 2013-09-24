using System;
using System.ComponentModel;
using DelftTools.Utils.Aop;
using DelftTools.Utils.Data;
using GeoAPI.Extensions.Feature;
using GeoAPI.Extensions.Networks;
using GeoAPI.Geometries;

namespace NetTopologySuite.Extensions.Networks
{
    [Entity]
    public abstract class NetworkFeature : Unique<long>, INetworkFeature
    {
        //distance in decameters to point 0 allong coastal measurement line

        protected INetwork network;

        [NoNotifyPropertyChange] protected IFeatureAttributeCollection attributes;

        private IGeometry geometry;

        public virtual IGeometry Geometry
        {
            get { return geometry; }
            set
            {
                geometry = value;
                OnGeometryChanged();
            }
        }

        protected virtual void OnGeometryChanged()
        {
        }

        [NoNotifyPropertyChange]
        public virtual IFeatureAttributeCollection Attributes
        {
            get { return attributes; }
            set { attributes = value; }
        }

        [DisplayName("Name")]
        [FeatureAttribute(Order = 1)]
        public virtual string Name { get; set; }

        [Aggregation]
        public virtual INetwork Network
        {
            get { return network; }
            set { network = value; }
        }

        public virtual string Description { get; set; }

        public virtual int CompareTo(INetworkFeature other)
        {
            if (this is IBranch)
            {
                return Network.Branches.IndexOf((IBranch) this).CompareTo(Network.Branches.IndexOf((IBranch) other));
            }
            
            if(this is INode)
            {
                return Network.Nodes.IndexOf((INode) this).CompareTo(Network.Nodes.IndexOf((INode)other));
            }

            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return Name;
        }

        public virtual int CompareTo(object obj)
        {
            if (obj is NetworkFeature)
            {
                return CompareTo((INetworkFeature)obj);
            }

            throw new NotImplementedException();
        }

        public abstract object Clone();
    }
}