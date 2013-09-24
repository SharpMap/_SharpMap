using System;
using System.ComponentModel;
using GeoAPI.Extensions.Feature;
using GeoAPI.Extensions.Networks;
using GeoAPI.Geometries;
using NetTopologySuite.Extensions.Features;

namespace NetTopologySuite.Extensions.Networks
{
    using DelftTools.Utils.Aop;

    /// <summary>
    /// Defines feature on the branch with a specific chainage.
    /// It can represent any object on the branch, e.g. observation point, structure, cross-section.
    /// </summary>
    [Entity(FireOnCollectionChange=false)]
    public abstract class BranchFeature : NetworkFeature, IBranchFeature
    {
        private IBranch branch;
        private double chainage;
        private double length;

        protected BranchFeature()
        {
            Attributes = new DictionaryFeatureAttributeCollection();
        }

        [Aggregation]
        [ReadOnly(true)]
        [FeatureAttribute(Order = 3)]
        public virtual IBranch Branch
        {
            get { return branch; }
            set { branch = value; }
        }

        public override INetwork Network
        {
            get { return Branch == null ? null : Branch.Network; }
        }

        /// <summary>
        /// The double precision for <see cref="Chainage"/>
        /// </summary>
        [Obsolete("this looks very bad, use Geometry precision or use exact value")]
        public static double Epsilon { get { return 1.0e-7; } }

        /// <exception cref="ArgumentOutOfRangeException">
        ///  When <c>value</c> smaller than 0
        /// </exception>
        [ReadOnly(true)]
        [DisplayName("Chainage [m]")]
        [FeatureAttribute(Order = 4)]
        public virtual double Chainage
        {
            get { return chainage; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("Negative chainage is not possible");
                }
                chainage = value;
            }
        }

        /// <summary>
        /// (Calculation) length of the branch feature starting from chainage:
        /// 
        /// [] - branch feature
        /// 
        ///                    <----length--->
        /// *------------------[-------------]-------------*
        /// <------------------>
        ///        chainage
        /// 
        /// </summary>
        public virtual double Length
        {
            get { return length; }
            set { length = value; }
        }

        /// <summary>
        /// Compares the current instance with another object of the same type.
        /// </summary>
        /// <returns>
        /// A 32-bit signed integer that indicates the relative order of the objects being compared. The return value has these meanings: Value Meaning Less than zero This instance is less than <paramref name="obj" />. Zero This instance is equal to <paramref name="obj" />. Greater than zero This instance is greater than <paramref name="obj" />. 
        /// </returns>
        /// <param name="obj">An object to compare with this instance. </param>
        /// <exception cref="T:System.ArgumentException"><paramref name="obj" /> is not the same type as this instance. </exception><filterpriority>2</filterpriority>
        public override int CompareTo(object obj)
        {
            var other = (IBranchFeature)obj;
            return CompareTo(other);
        }

        public virtual int CompareTo(IBranchFeature other)
        {
            if (this == other)
            {
                return 0;
            }
            if (null == Branch)
            {
                throw new Exception("Cannot compare branch features that are not connected to a branch.");
            }
            if (other.Branch != branch)
            {
                return Branch.CompareTo(other.Branch);
            }
            if (Chainage > other.Chainage)
            {
                return 1;
            }
            if (Math.Abs(Chainage - other.Chainage) < Epsilon)
            {
                // in some cases branch features are defined at the same location and differ only in name
                if (Name != other.Name)
                {
                    return String.Compare(Name, other.Name, StringComparison.Ordinal);
                }

                return 0;
            }
            return -1;
        }

        public override string ToString()
        {
            if(!String.IsNullOrEmpty(Name))
            {
                return Name;
            }

            var branchName = branch == null ? "<no branch>" : branch.Name;
            var str = GetType().Name + ": " + branchName + ", " + Chainage.ToString("g4");
            if(Length > 0)
            {
                str = GetType().Name + ": " + branchName + ", [" + Chainage.ToString("g4") + " - " + (Chainage + Length).ToString("g4") + "]";
            }
            return str;
        }

        public override object Clone()
        {
            BranchFeature newBranchFeature = (BranchFeature) Activator.CreateInstance(GetType());
            newBranchFeature.chainage = Chainage;
            newBranchFeature.Geometry = Geometry == null ? null : ((IGeometry)Geometry.Clone());
            newBranchFeature.Name = Name;
            newBranchFeature.Branch = Branch;
            newBranchFeature.CopyFrom(this);
            return newBranchFeature;
        }

        /// <summary>
        /// do not copy geo based data like geometry and chainage
        /// </summary>
        /// <param name="source"></param>
        public virtual void CopyFrom(object source)
        {
            var branchFeature = (BranchFeature) source;

            Length = branchFeature.Length;

            Attributes = branchFeature.Attributes != null
                                ? (IFeatureAttributeCollection) branchFeature.Attributes.Clone()
                                : null;
        }

        /// <summary>
        /// Returns the snapped <paramref name="chainage"/> when very near to 0 or <paramref name="branchLength"/>.
        /// </summary>
        /// <param name="branchLength">Length of the branch for which <paramref name="chainage"/> applies to.</param>
        /// <param name="chainage">The chainage.</param>
        /// <returns>The chainage snapped to <paramref name="branchLength"/> or 0.0 when near these values. Otherwise returns <paramref name="chainage"/>.</returns>
        /// <remarks>'Near' is defined as within 1 <see cref="Epsilon"/>.</remarks>
        public static double SnapChainage(double branchLength, double chainage)
        {
            if (Math.Abs(chainage) < Epsilon || chainage < 0)
                return 0.0;

            if (Math.Abs(branchLength - chainage) < Epsilon || chainage > branchLength)
                return branchLength;
            
            return chainage;
        }
    }
}