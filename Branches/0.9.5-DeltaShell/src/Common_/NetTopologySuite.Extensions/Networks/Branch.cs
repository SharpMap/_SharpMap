using System;
using DelftTools.Utils.Aop;
using DelftTools.Utils.Aop.NotifyPropertyChanged;
using DelftTools.Utils.Collections.Generic;
using GeoAPI.Extensions.Feature;
using GeoAPI.Extensions.Networks;
using GeoAPI.Geometries;
using log4net;
using NetTopologySuite.Extensions.Features;
using ValidationAspects;

namespace NetTopologySuite.Extensions.Networks
{
    [Serializable]
    [NotifyCollectionChanged]
    [NotifyPropertyChanged]
    public class Branch : NetworkFeature, IBranch
    {
        private static readonly ILog log = LogManager.GetLogger(typeof (Branch));

        private IEventedList<IBranchFeature> branchFeatures;
        private bool isLengthCustom;

        private double length;

        [NoBubbling, NoNotifyPropertyChanged] private INode source;

        [NoBubbling, NoNotifyPropertyChanged] private INode target;

        public Branch()
            : this(null, null)
        {
        }

        public Branch(INode fromNode, INode toNode)
            : this("branch", fromNode, toNode, 0)
        {
        }

        public Branch(INode fromNode, INode toNode, double length)
            : this("branch", fromNode, toNode, length)
        {
        }

        public Branch(string name, INode fromNode, INode toNode, double length)
        {
            Source = fromNode;
            Target = toNode;

            this.length = length;
            
/*
            if(length != 0)
            {
                IsLengthCustom = true;
            }
*/

            Name = name;

            // Do not replace with backing field; virtual setter in derived classes can have added logic; eek!
            BranchFeatures = new EventedList<IBranchFeature>();
            Attributes = new DictionaryFeatureAttributeCollection();
        }

        #region IBranch Members

        /// <summary>
        /// Node where this branch starts from.
        /// </summary>
        [FeatureAttribute, NotNull]
        public virtual INode Source
        {
            get { return source; }
            set
            {
                if (null != source)
                {
                    source.OutgoingBranches.Remove(this);
                }
                source = value;
                if (null != source)
                {
                    source.OutgoingBranches.Add(this);
                }
            }
        }

        /// <summary>
        /// Node where this branch is connected to.
        /// </summary>
        [FeatureAttribute]
        public virtual INode Target
        {
            get { return target; }
            set
            {
                if (null != target)
                {
                    target.IncomingBranches.Remove(this);
                }
                target = value;
                if (null != target)
                {
                    target.IncomingBranches.Add(this);
                }
            }
        }

        public bool IsLengthCustom
        {
            get
            {
                if (Geometry == null)
                {
                    return true;
                }

                return isLengthCustom;
            }
            set { isLengthCustom = value; }
        }

        /// <summary>
        /// All branche features - points along the branch.
        /// </summary>
        public virtual IEventedList<IBranchFeature> BranchFeatures
        {
            get { return branchFeatures; }
            set { branchFeatures = value; }
        }

        /// <summary>
        /// Logical length of this branch.
        /// </summary>
        [FeatureAttribute]
        [Minimum(0)]
        public virtual double Length
        {
            get
            {
                if (!IsLengthCustom)
                {
                    return Geometry.Length;
                }

                return length;
            }
            set { length = value; }
        }

        public override int CompareTo(object obj)
        {
            return CompareTo((IBranch) obj);
        }

        public virtual int CompareTo(IBranch other)
        {
            if (this == other)
            {
                return 0;
            }

            if(Network == null || other.Network == null)
            {
                throw new InvalidOperationException("Can't compare branches if network is empty");
            }

            if (other.Network != Network)
            {
                throw new ArgumentOutOfRangeException("other", "Cannot compare branches from different networks.");
            }

            if (Network.Branches.IndexOf(this) > Network.Branches.IndexOf(other))
            {
                return 1;
            }
            
            return -1;
        }

        public override object Clone()
        {
            var newBranch = (Branch) Activator.CreateInstance(GetType());
            newBranch.Name = Name;
            newBranch.Geometry = Geometry == null ? null : (IGeometry) Geometry.Clone();
            newBranch.length = Length;
            newBranch.isLengthCustom = IsLengthCustom;

            foreach (var branchFeature in BranchFeatures)
            {
                BranchFeature clonedBranchFeature = (BranchFeature)branchFeature.Clone();
                clonedBranchFeature.Branch = newBranch;
                newBranch.BranchFeatures.Add(clonedBranchFeature);
            }
            return newBranch;
        }

        #endregion
    }
}