using System;
using System.Globalization;
using DelftTools.Utils.Aop;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Extensions.Feature;
using GeoAPI.Extensions.Networks;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;
using GisSharpBlog.NetTopologySuite.LinearReferencing;
using NetTopologySuite.Extensions.Networks;

namespace NetTopologySuite.Extensions.Coverages
{
    [Entity(FireOnCollectionChange = false)]
    public class NetworkLocation : BranchFeature, INetworkLocation
    {
        public NetworkLocation()
        {
        }

        public NetworkLocation(IBranch branch, double offset)
        {
            Branch = branch;
            Chainage = offset;
            
            var branchName = (branch != null ? branch.Name : "");
            Name = String.Format(CultureInfo.InvariantCulture, "{0}_{1:0.000}", branchName, offset);
        }

        [EditAction]
        private void UpdateGeometry()
        {
            if (Branch == null || Branch.Geometry == null)
                return;

            var lengthIndexedLine = new LengthIndexedLine(Branch.Geometry);

            var offset = Branch.IsLengthCustom ? SnapChainage(Branch.Geometry.Length, (Branch.Geometry.Length / Branch.Length) * Chainage) : Chainage;
            // always clone: ExtractPoint will give either a new coordinate or a reference to an existing object
            Geometry = new Point((ICoordinate)lengthIndexedLine.ExtractPoint(offset).Clone());
        }

        public override object Clone()
        {
            return new NetworkLocation
            {
                Name = Name,
                Geometry = Geometry != null ? ((IGeometry)Geometry.Clone()) : null,
                Attributes = Attributes != null ? (IFeatureAttributeCollection)Attributes.Clone() : null,
                Chainage = Chainage,
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
            return location.Branch == Branch && Math.Abs(location.Chainage - Chainage) < Epsilon;
        }

        public override int GetHashCode()
        {
            return ((Branch != null ? Branch.GetHashCode() : 0) * 397) ^ Math.Round(Chainage, 5).GetHashCode();
        }

        public override int CompareTo(object obj)
        {
            var other = obj as NetworkLocation;
            return other != null ? CompareTo(other) : base.CompareTo(obj);
        }

        public int CompareTo(INetworkLocation other)
        {
            if (other.Branch != Branch)
            {
                return Branch.CompareTo(other.Branch);
            }
            if (Chainage > other.Chainage)
            {
                return 1;
            }
            if (Math.Abs(Chainage - other.Chainage) < Epsilon)
            {
                //don't take NAME into account, as happens in BranchFeature
                return 0;
            }
            return -1;
        }

        public override string ToString()
        {
            if (Branch == null)
            {
                return "<invalid>";
            }

            return string.Format("({0}, {1:g7})", Branch.Name, Chainage);
        }

        /// <exception cref="ArgumentOutOfRangeException">
        ///  When <c>value</c> smaller than 0
        /// </exception>
        public override double Chainage
        {
            get
            {
                return base.Chainage;
            }
            set
            {
                if ( Math.Abs(base.Chainage - value) < Epsilon && Geometry != null) 
                    return;

                base.Chainage = value;
                UpdateGeometry();
            }
        }

        //override so we get pc
        [Aggregation]
        public override IBranch Branch
        {
            get
            {
                return base.Branch;
            }
            set
            {
                base.Branch = value;
            }
        }
        
        public string LongName { get; set; }
        
    }
}