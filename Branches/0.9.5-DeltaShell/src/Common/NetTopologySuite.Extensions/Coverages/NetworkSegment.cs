using System;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Extensions.Feature;
using GeoAPI.Extensions.Networks;
using GeoAPI.Geometries;
using NetTopologySuite.Extensions.Networks;

namespace NetTopologySuite.Extensions.Coverages
{
    public class NetworkSegment : BranchFeature, INetworkSegment
    {
        public override object Clone()
        {
            return new NetworkSegment
                       {
                           Branch = Branch,
                           Offset = Offset,
                           Length = Length,
                           Geometry = (IGeometry) Geometry.Clone(),
                           Attributes = Attributes != null ? (IFeatureAttributeCollection) Attributes.Clone() : null,
                           DirectionIsPositive = DirectionIsPositive
                       };
        }

        public NetworkSegment()
        {
            DirectionIsPositive = true;
        }
        public bool DirectionIsPositive
        {
            get;set;
        }

        public double EndOffset
        {
            get 
            {
                if (DirectionIsPositive)
                    return Offset + Length;
                //doodeng..soms gaat segmentatie fout zodat de length groter wordt dan de offset
                return Math.Max(0,Offset - Length);
            }
        }

        public override string ToString()
        {
            if (!String.IsNullOrEmpty(Name))
            {
                return Name;
            }

            var branchName = Branch == null ? "<no branch>" : Branch.Name;
            var str = GetType().Name + ": " + branchName + ", " + Offset.ToString("g4");
            if (Length > 0)
            {
                if (DirectionIsPositive)
                {
                    str = GetType().Name + ": " + branchName + ", [" + Offset.ToString("g4") + " - " + (Offset + Length).ToString("g4") + "]";
                }
                else
                {
                    str = GetType().Name + ": " + branchName + ", [" + Offset.ToString("g4") + " - " + (Offset - Length).ToString("g4") + "]";
                }
            }
            return str;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            if (!(obj is INetworkSegment))
            {
                return false;
            }
            var segment = (INetworkSegment)obj;
            return segment.Branch == Branch && segment.Offset == Offset && segment.EndOffset == EndOffset;
        }

    }
}