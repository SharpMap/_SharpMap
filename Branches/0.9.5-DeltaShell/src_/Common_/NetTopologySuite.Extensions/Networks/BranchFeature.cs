using System;
using DelftTools.Utils.Aop.NotifyPropertyChanged;
using GeoAPI.Extensions.Feature;
using GeoAPI.Extensions.Networks;
using GeoAPI.Geometries;
using NetTopologySuite.Extensions.Features;

namespace NetTopologySuite.Extensions.Networks
{
    /// <summary>
    /// Defines feature on the branch with a specific offset.
    /// It can represent any object on the branch, e.g. observation point, structure, cross-section.
    /// </summary>
    [Serializable]
    [NotifyPropertyChanged(EnableLogging = false)]
    public abstract class BranchFeature : NetworkFeature, IBranchFeature
    {
        [NoBubbling]
        private IBranch branch;
        private double offset;
        private double length;

        protected BranchFeature()
        {
            Attributes = new DictionaryFeatureAttributeCollection();
        }

        [NoNotifyPropertyChanged]
        [FeatureAttribute]
        public virtual IBranch Branch
        {
            get { return branch; }
            set { branch = value; }
        }

        public override INetwork Network
        {
            get { return Branch == null ? null : Branch.Network; }
        }

        [FeatureAttribute]
        public virtual double Offset
        {
            get { return offset; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("Negative offset is not possible");
                }
                offset = value;
            }
        }

        /// <summary>
        /// Length of the branch feature starting from offset:
        /// 
        /// [] - branch feature
        /// 
        ///                    <----length--->
        /// *------------------[-------------]-------------*
        /// <------------------>
        ///        offset
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
        public virtual int CompareTo(object obj)
        {
            var other = (IBranchFeature)obj;
            return CompareTo(other);
        }

        public int CompareTo(IBranchFeature other)
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
            if (Offset > other.Offset)
            {
                return 1;
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
            var str = GetType().Name + ": " + branchName + ", " + Offset.ToString("g4");
            if(Length > 0)
            {
                str = GetType().Name + ": " + branchName + ", [" + Offset.ToString("g4") + " - " + (Offset + Length).ToString("g4") + "]";
            }
            return str;
        }

        public override object Clone()
        {
            BranchFeature newBranchFeature = (BranchFeature) Activator.CreateInstance(GetType());
            newBranchFeature.length = Length;
            newBranchFeature.offset = Offset;
            newBranchFeature.Geometry = Geometry == null ? null : ((IGeometry)Geometry.Clone());
            newBranchFeature.Name = Name;
            //do it here?
            //newBranchFeature.attributes = (IFeatureAttributeCollection) Attributes.Clone();
            return newBranchFeature;
        }

    }
}