using System;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Extensions.Feature;
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
                           Chainage = Chainage,
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
        
        [FeatureAttribute]
        public int SegmentNumber { get; set; }

        public double EndChainage
        {
            get 
            {
                if (DirectionIsPositive)
                    return Chainage + Length;
                //doodeng..soms gaat segmentatie fout zodat de length groter wordt dan de offset
                return Math.Max(0,Chainage - Length);
            }
        }

        public override string ToString()
        {
            if (!String.IsNullOrEmpty(Name))
            {
                return Name;
            }

            var branchName = Branch == null ? "<no branch>" : Branch.Name;
            var str = GetType().Name + ": " + branchName + ", " + Chainage.ToString("g4");
            if (Length > 0)
            {
                var endChainage = DirectionIsPositive ? Chainage + Length : Chainage - Length;
                str = GetType().Name + ": " + branchName + ", [" + Chainage.ToString("g4") + " - " + endChainage.ToString("g4") + "]";
            }
            return str;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is INetworkSegment))
            {
                return false;
            }
            var segment = (INetworkSegment)obj;
            return segment.Branch == Branch && Math.Abs(segment.Chainage - Chainage) < Epsilon && Math.Abs(segment.EndChainage - EndChainage) < Epsilon;
        }

    }
}