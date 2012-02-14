using System;
using DelftTools.Utils.Aop.NotifyCollectionChange;
using DelftTools.Utils.Aop.NotifyPropertyChange;
using DelftTools.Utils.Collections;
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
    [NotifyCollectionChange]
    [NotifyPropertyChange]
    public class Branch : NetworkFeature, IBranch
    {
        private static readonly ILog log = LogManager.GetLogger(typeof (Branch));

        private IEventedList<IBranchFeature> branchFeatures;
        private bool isLengthCustom;

        private double length;

        [NoNotifyPropertyChange]
        private INode source;

        [NoNotifyPropertyChange]
        private INode target;

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

        public override string ToString()
        {
            return Name;
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
                if (source == value) //we don't want nasty changed events if nothing changes
                    return;

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
                if (target == value) //we don't want nasty changed events if nothing changes
                    return;

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

        [FeatureAttribute]
        public bool IsLengthCustom
        {
            get
            {
                return Geometry == null || isLengthCustom;
            }
            set
            {
                if (isLengthCustom == value)
                {
                    return;
                }
                isLengthCustom = value;
                
                if ((length <= 1.0e-6) && (Geometry != null))
                {
                    length = Geometry.Length;
                }

                var geometryLength = Geometry != null ? Geometry.Length : length;
                var factor = geometryLength / length;
                UpdateBranchFeatureOffsets(isLengthCustom ? 1.0 / factor : factor);
            }
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
            set
            {
                if (length == value)
                {
                    return;
                }
                if (isLengthCustom)
                {
                    UpdateBranchFeatureOffsets(value/length);
                }
                length = value;
            }
        }

        private void UpdateBranchFeatureOffsets(double factor)
        {
            branchFeatures.ForEach(bf => bf.Offset *= factor);
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

            foreach(var branch in Network.Branches)
            {
                if (ReferenceEquals(branch, other)) //other found first
                    return 1; //this greater than other
                if (ReferenceEquals(branch, this))
                    return -1;
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
            newBranch.Attributes = (IFeatureAttributeCollection)(Attributes != null ? Attributes.Clone() : null);
            
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