using System;
using System.ComponentModel;
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
    using DelftTools.Utils.Aop;

    [Entity]
    public class Branch : NetworkFeature, IBranch
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof (Branch));

        private IEventedList<IBranchFeature> branchFeatures;
        private bool isLengthCustom;

        private double length;
        private double geometryLength;

        private INode source;

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
            // Do not replace with backing field; virtual setter in derived classes can have added logic; eek!
            BranchFeatures = new EventedList<IBranchFeature>();
            Attributes = new DictionaryFeatureAttributeCollection();
            Source = fromNode;
            Target = toNode;
            this.length = length;
            Name = name;
            OrderNumber = -1; //-1 -> not a part of a chain of branches
        }

        public override string ToString()
        {
            return Name;
        }

        #region IBranch Members

        /// <summary>
        /// Node where this branch starts from.
        /// </summary>
        [Aggregation]
        [FeatureAttribute(Order = 5)]
        [ReadOnly(true)]
        [DisplayName("From")]
        [NotNull]
        public virtual INode Source
        {
            get { return source; }
            set
            {
                if (source == value) //we don't want nasty changed events if nothing changes
                    return;

                BeforeSetSource();
                source = value;
                AfterSetSource();
            }
        }

        [EditAction]
        private void BeforeSetSource()
        {
            if (null != source)
            {
                source.OutgoingBranches.Remove(this);
            }
        }

        [EditAction]
        private void AfterSetSource()
        {
            if (null != source)
            {
                source.OutgoingBranches.Add(this);
            }
        }

        /// <summary>
        /// Node where this branch is connected to.
        /// </summary>
        [Aggregation]
        [FeatureAttribute(Order = 6)]
        [ReadOnly(true)]
        [DisplayName("To")]
        [NotNull]
        public virtual INode Target
        {
            get { return target; }
            set
            {
                if (target == value) //we don't want nasty changed events if nothing changes
                    return;

                BeforeTargetSet();
                target = value;
                AfterTargetSet();
            }
        }

        [EditAction]
        private void BeforeTargetSet()
        {
            if (null != target)
            {
                target.IncomingBranches.Remove(this);
            }
        }

        [EditAction]
        private void AfterTargetSet()
        {
            if (null != target)
            {
                target.IncomingBranches.Add(this);
            }
        }

        [DisplayName("Order number")]
        [FeatureAttribute(Order = 7)]
        public virtual int OrderNumber { get; set; }

        [DisplayName("Use custom length")]
        [FeatureAttribute(Order = 3, ExportName = "CustomLen")]
        public virtual bool IsLengthCustom
        {
            get
            {
                return Geometry == null || isLengthCustom;
            }
            set
            {
                isLengthCustom = value;
                
                AfterSetIsLengthCustom();
            }
        }

        private const double Precision = 1e-10;

        [EditAction]
        private void AfterSetIsLengthCustom()
        {
            geometryLength = Geometry != null ? Geometry.Length : length;

            // when lengths was empty - set it to geometry length
            if ((length <= Precision) && (Geometry != null))
            {
                length = Geometry.Length;
            }

            var factor = geometryLength / length;
            UpdateBranchFeatureChainages(IsLengthCustom ? 1.0 / factor : factor, Length);
        }

        /// <summary>
        /// All branch features - points along the branch.
        /// </summary>
        public virtual IEventedList<IBranchFeature> BranchFeatures
        {
            get { return branchFeatures; }
            set
            {
                if(branchFeatures != null)
                {
                    branchFeatures.CollectionChanged -= BranchFeaturesOnCollectionChanged;
                }
                branchFeatures = value;
                if (branchFeatures != null)
                {
                    branchFeatures.CollectionChanged += BranchFeaturesOnCollectionChanged;
                }
            }
        }

        [EditAction]
        private void BranchFeaturesOnCollectionChanged(object sender, NotifyCollectionChangingEventArgs e)
        {
            if (!Equals(sender, branchFeatures))
                return;

            var branchFeature = (IBranchFeature)e.Item;

            switch (e.Action)
            {
                case NotifyCollectionChangeAction.Add:
                    branchFeature.Branch = this;
                    break;
            }
        }

        protected override void OnGeometryChanged()
        {
            if (Geometry == null)
            {
                geometryLength = 0;
                return;
            }

            geometryLength = Geometry.Length;

            if (!IsLengthCustom)
            {
                UpdateBranchFeatureChainages(1, geometryLength);
            }
        }

        /// <summary>
        /// Logical length of this branch.
        /// </summary>
        /// <remarks>Negative values will throw an ArgumentOutOfRange exception.</remarks>
        [DisplayName("Length (m)")]
        [FeatureAttribute(Order = 4)]
        [Minimum(0)]
        public virtual double Length
        {
            get
            {
                return IsLengthCustom ? length : geometryLength; // TODO: this is confusing and not consistent with setter
            }
            set
            {
                // Hack: Minimum attribute on Length does not prevent setting this property to negative value.
                // (Unable to get Interception validation working, see http://validationaspects.codeplex.com/wikipage?title=Property%20Validation%20Modes&referringTitle=Documentation
                //  PostSharp interaction not set up properly??)
                if(value < 0)
                {
                    throw new ArgumentOutOfRangeException("Length", "Branch length cannot be negative.");
                }

                if (Math.Abs(length - value) < Precision)
                {
                    return;
                }

                if (isLengthCustom)
                {
                    UpdateBranchFeatureChainages(value / length, value);
                }

                length = value;
            }
        }

        [DisplayName("Geometry length (m)")]
        [FeatureAttribute(Order = 4, ExportName = "GeomLength")]
        public virtual double GeometryLength
        {
            get { return geometryLength; }
        }

        [EditAction]
        private void UpdateBranchFeatureChainages(double factor, double newLength)
        {
            branchFeatures.ForEach( bf => bf.Chainage = BranchFeature.SnapChainage(newLength, factor * bf.Chainage));
        }

        public override int CompareTo(object obj)
        {
            return CompareTo((IBranch) obj);
        }

        public virtual int CompareTo(IBranch other)
        {
            if (ReferenceEquals(this, other))
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

            var network = (Network)Network;
            if (network.BranchIndicesDirty)
            {
                network.UpdateBranchIndices();
            }

            return Index.CompareTo(((Branch) other).Index);
        }

        protected internal virtual int Index { get; set; }

        public override object Clone()
        {
            var newBranch = (Branch) Activator.CreateInstance(GetType());
            newBranch.Name = Name;
            newBranch.Geometry = Geometry == null ? null : (IGeometry) Geometry.Clone();
            newBranch.length = Length;
            newBranch.isLengthCustom = IsLengthCustom;
            newBranch.OrderNumber = OrderNumber;
            newBranch.Attributes = (IFeatureAttributeCollection)(Attributes != null ? Attributes.Clone() : null);
            
            foreach (var branchFeature in BranchFeatures)
            {
                var clonedBranchFeature = (BranchFeature)branchFeature.Clone();
                clonedBranchFeature.Branch = newBranch;
                newBranch.BranchFeatures.Add(clonedBranchFeature);
            }
            return newBranch;
        }

        #endregion
    }
}