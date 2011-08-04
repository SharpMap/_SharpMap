using System;
using DelftTools.Utils.Aop.NotifyPropertyChanged;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Extensions.Feature;
using GeoAPI.Extensions.Networks;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;
using GisSharpBlog.NetTopologySuite.LinearReferencing;
using NetTopologySuite.Extensions.Networks;

namespace NetTopologySuite.Extensions.Coverages
{
    [NotifyPropertyChanged]
    public class NetworkLocation : BranchFeature, INetworkLocation
    {
        public NetworkLocation()
        {
        }

        public NetworkLocation(IBranch branch, double offset)
        {
            Branch = branch;
            Offset = offset;
            

            
        }

        private void UpdateGeometry()
        {
            if (Branch == null|| Branch.Geometry == null)
                return;
            
            var lengthIndexedLine = new LengthIndexedLine(Branch.Geometry);
            // thousand bombs and granates: ExtractPoint will give either a new coordinate or 
            // a reference to an existing object
            Geometry = new Point((ICoordinate)lengthIndexedLine.ExtractPoint(Offset).Clone());
        }

        public override object Clone()
        {
            return new NetworkLocation
            {
                Geometry = ((IGeometry)Geometry.Clone()),
                Attributes = Attributes != null ? (IFeatureAttributeCollection) Attributes.Clone() : null,
                Offset = Offset,
                Branch = Branch // ##eek do not clone link to branch it breaks the editor tools; Branch = Branch (HACK: fix it)
            };
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            if (!(obj is INetworkLocation))
            {
                return false;
            }
            var location = (INetworkLocation)obj;
            return location.Branch == Branch && location.Offset == Offset;
        }

        public bool Equals(NetworkLocation other)
        {
            return !ReferenceEquals(null, other);
        }

        public override int GetHashCode()
        {
            return ((Branch != null ? Branch.GetHashCode() : 0) * 397) ^ Offset.GetHashCode();
        }

        public override string ToString()
        {
            if(Branch == null)
            {
                return "<invalid>";
            }

            return Branch.Name + ", " + Offset.ToString("g4");
        }

        public override double Offset
        {
            get
            {
                return base.Offset;
            }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("Negative offset is not allowed");
                }
                base.Offset = value;
                UpdateGeometry();
            }
        }
    }
}