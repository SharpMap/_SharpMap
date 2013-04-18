using System;
using DelftTools.Utils.Aop.NotifyPropertyChanged;
using GeoAPI.Extensions.Feature;
using GeoAPI.Extensions.Networks;
using GeoAPI.Geometries;

namespace NetTopologySuite.Extensions.Networks
{
    [Serializable]
    [NotifyPropertyChanged]
    public abstract class NetworkFeature : INetworkFeature
    {
        public virtual long Id { get; set; }

        //public virtual IGeometry Geometry { get; set; }

        [NoBubbling] protected INetwork network;
        [NoBubbling] protected IFeatureAttributeCollection attributes;

        public virtual IGeometry Geometry { get; set; }

        [NoNotifyPropertyChanged]
        public virtual IFeatureAttributeCollection Attributes { get{ return attributes;} set{ attributes = value;} }

        [FeatureAttribute]
        public virtual string Name { get; set; }

        [NoNotifyPropertyChanged]
        public virtual INetwork Network
        {
            get { return network;} 
            set { network = value; }
        }

        public virtual string Description
        {
            get; set;
        }

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