using System;
using System.ComponentModel;
using DelftTools.Utils.Aop;
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
    [Entity]
    public class Node : NetworkFeature, INode
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Node));

        private IEventedList<INodeFeature> nodeFeatures;

        public Node(): this("node")
        {
        }

        public Node(string name)
        {
            Name = name;
            
            // Do not replace with backing field: logic might be overriden in derived classes
            IncomingBranches = new EventedList<IBranch>();
            OutgoingBranches = new EventedList<IBranch>();
            NodeFeatures = new EventedList<INodeFeature>();
            Attributes = new DictionaryFeatureAttributeCollection();
        }

        /// <summary>
        /// TODO: refactor it to use Network.Branch.Source, Network.Branch.Target (as IEnumerable initialized in the Network setter)
        /// </summary>
        [Aggregation]
        public virtual IEventedList<IBranch> IncomingBranches { get; set; }

        /// <summary>
        /// TODO: refactor it to use Network.Branch.Source, Network.Branch.Target (as IEnumerable initialized in the Network setter)
        /// </summary>
        [Aggregation]
        public virtual IEventedList<IBranch> OutgoingBranches { get; set; }

        public virtual IEventedList<INodeFeature> NodeFeatures
        {
            get { return nodeFeatures; }
            set
            {
                if (nodeFeatures != null)
                {
                    nodeFeatures.CollectionChanging -= NodeFeaturesCollectionChanging;
                }
                
                nodeFeatures = value;

                if (nodeFeatures != null)
                {
                    nodeFeatures.CollectionChanging += NodeFeaturesCollectionChanging;
                }
            }
        }

        public virtual bool IsConnectedToMultipleBranches
        {
            get { return GetBranchCount() > 1; }
        }

        [DisplayName("Is on single branch")]
        [FeatureAttribute]
        public virtual bool IsOnSingleBranch
        {
            get { return GetBranchCount() == 1; }
        }

        [EditAction]
        private void NodeFeaturesCollectionChanging(object sender, NotifyCollectionChangingEventArgs e)
        {
            var nodeFeature = (INodeFeature) e.Item;
            if (nodeFeature == null) return;

            switch (e.Action)
            {
                case NotifyCollectionChangeAction.Replace:
                    throw new NotImplementedException();

                case NotifyCollectionChangeAction.Remove:
                    Log.DebugFormat("Removed {0} from the node {1}", nodeFeature, this);
                    nodeFeature.Node = null;
                    break;

                case NotifyCollectionChangeAction.Add:
                    nodeFeature.Node = this;
                    Log.DebugFormat("Added {0} to the node {1}", nodeFeature, this);
                    break;
            }
        }

        public override string ToString()
        {
            return Name;
        }

        public override object Clone()
        {
            var node = (Node) Activator.CreateInstance(GetType());
            
            node.Name = Name;
            node.Geometry = Geometry == null ? null : ((IGeometry) Geometry.Clone());
            node.Attributes = (IFeatureAttributeCollection) (Attributes != null ? Attributes.Clone() : null);

            foreach (var nodeFeature in NodeFeatures)
            {
                node.NodeFeatures.Add((INodeFeature) nodeFeature.Clone());
            }

            return node;
        }

        [ValidationMethod]
        public static void Validate(Node node)
        {
            if (node.IncomingBranches.Count == 0 && node.OutgoingBranches.Count == 0)
            {
                throw new ValidationException("Lists of incoming and outgoing branches are empty");
            }
        }

        private int GetBranchCount()
        {
            var branchCount = 0;

            if (IncomingBranches != null)
            {
                branchCount += IncomingBranches.Count;
            }

            if (OutgoingBranches != null)
            {
                branchCount += OutgoingBranches.Count;
            }

            return branchCount;
        }
    }
}