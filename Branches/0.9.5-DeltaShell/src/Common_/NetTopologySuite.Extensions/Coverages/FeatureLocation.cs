using System;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;
using NetTopologySuite.Extensions.Networks;

namespace NetTopologySuite.Extensions.Coverages
{
    /// <summary>
    /// Feature that is defined by an offset in another feature.
    /// mos common use use is 
    /// </summary>
    public class FeatureLocation : NetworkFeature, IFeatureLocation
    {
        public IFeature Feature { get; set; }
        public double Offset { get; set; }
        public double Length { get; set; }

        public FeatureLocation()
        {
        }

        public FeatureLocation(IFeature feature, double offset)
        {
            Feature = feature;
            Offset = offset;
        }

        public override object Clone()
        {
            FeatureLocation newFeatureLocation = (FeatureLocation)Activator.CreateInstance(GetType());
            newFeatureLocation.Length = Length;
            newFeatureLocation.Offset = Offset;
            newFeatureLocation.Geometry = Geometry == null ? null : ((IGeometry)Geometry.Clone());
            newFeatureLocation.Name = Name;
            //do it here?
            //newBranchFeature.attributes = (IFeatureAttributeCollection) Attributes.Clone();
            return newFeatureLocation;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            if (!(obj is IFeatureLocation))
            {
                return false;
            }
            var location = (IFeatureLocation)obj;
            return location.Feature == Feature && location.Offset == Offset;
        }

        public bool Equals(NetworkLocation other)
        {
            return !ReferenceEquals(null, other);
        }


        public virtual int CompareTo(object obj)
        {
            var other = (IFeatureLocation)obj;
            return CompareTo(other);
        }

        public int CompareTo(IFeatureLocation other)
        {
            if (this == other)
            {
                return 0;
            }
            if (null == Feature)
            {
                throw new Exception("Cannot compare branch features that are not connected to a branch.");
            }

            if (Feature is IComparable<IFeature>)
            {
                if (other.Feature != Feature)
                {
                    return ((IComparable<IFeature>)Feature).CompareTo(other.Feature);
                }
            }

            if (Offset > other.Offset)
            {
                return 1;
            }
            return -1;
        }
    }
}