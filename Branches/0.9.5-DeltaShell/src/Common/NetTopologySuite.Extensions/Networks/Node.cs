using System;
using System.Collections.Generic;
using DelftTools.Utils.Aop;
using DelftTools.Utils.Aop.NotifyCollectionChange;
using DelftTools.Utils.Aop.NotifyPropertyChange;
using DelftTools.Utils.Collections;
using DelftTools.Utils.Collections.Generic;
using GeoAPI.Extensions.Feature;
using GeoAPI.Extensions.Networks;
using GeoAPI.Geometries;
using log4net;
using NetTopologySuite.Extensions.Features;
using PostSharp;
using ValidationAspects;

namespace NetTopologySuite.Extensions.Networks
{
    [Serializable]
    [NotifyCollectionChange]
    [NotifyPropertyChange]
    public class Node : NetworkFeature, INode
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Node));

        [NoNotifyPropertyChange]
        private IList<IBranch> incomingBranches;

        [NoNotifyPropertyChange]
        private IList<IBranch> outgoingBranches;

        private IEventedList<INodeFeature> nodeFeatures;
        
        public Node(): this("node")
        {
        }

        public Node(string name)
        {
            Name = name;
            
            incomingBranches = new EventedList<IBranch>();
            outgoingBranches = new EventedList<IBranch>();

            ((INotifyCollectionChange)incomingBranches).CollectionChanged += ConnectedBranches_CollectionChanged;
            ((INotifyCollectionChange)outgoingBranches).CollectionChanged += ConnectedBranches_CollectionChanged;

            // Do not replace with backing field?; see Branch
            NodeFeatures = new EventedList<INodeFeature>();
            Attributes = new DictionaryFeatureAttributeCollection();
            IsBoundaryNode = true;
        }

        [NoNotifyPropertyChange, NotNull] 
        public virtual IList<IBranch> IncomingBranches
        {
            get { return incomingBranches; }
            set
            {
                ((INotifyCollectionChange)incomingBranches).CollectionChanged -= ConnectedBranches_CollectionChanged;
                incomingBranches = value;
                ((INotifyCollectionChange)incomingBranches).CollectionChanged += ConnectedBranches_CollectionChanged;
            }
        }

        [NoNotifyPropertyChange] 
        public virtual IList<IBranch> OutgoingBranches
        {
            get { return outgoingBranches; }
            set
            {
                ((INotifyCollectionChange)outgoingBranches).CollectionChanged -= ConnectedBranches_CollectionChanged;
                outgoingBranches = value;
                ((INotifyCollectionChange)outgoingBranches).CollectionChanged += ConnectedBranches_CollectionChanged;
            }
        }

        private void ConnectedBranches_CollectionChanged(object sender, NotifyCollectionChangingEventArgs e)
        {
            //don't update on child lists..like the list of YZValues in a crossection
            if (sender is IList<IBranch>)
            {
                IsBoundaryNode = (IncomingBranches.Count == 0) != (OutgoingBranches.Count == 0); //xor operator
            }
        }

        public virtual IEventedList<INodeFeature> NodeFeatures
        {
            get { return nodeFeatures; }
            set
            {
                if (nodeFeatures != null)
                {
                    Post.Cast<IEventedList<INodeFeature>, INotifyCollectionChange>(nodeFeatures).CollectionChanging -= nodeFeatures_CollectionChanging;
                }
                
                nodeFeatures = value;

                if (nodeFeatures != null)
                {
                    Post.Cast<IEventedList<INodeFeature>, INotifyCollectionChange>(nodeFeatures).CollectionChanging += nodeFeatures_CollectionChanging;
                }
            }
        }


        private bool isBoundaryNode = false;
        /// <summary>
        /// Do not set setter to private or current PostSharp attribute will not inject notification code.
        /// </summary>
        [FeatureAttribute]
        public virtual bool IsBoundaryNode
        {
            get { return isBoundaryNode; }
            set { isBoundaryNode = value; }
        }
        //[FeatureAttribute]
        //public bool IsBoundaryNode { get; set; }

        private void nodeFeatures_CollectionChanging(object sender, NotifyCollectionChangingEventArgs e)
        {
            var nodeFeature = (INodeFeature) e.Item;
            if (nodeFeature == null) return;

            switch (e.Action)
            {
                case NotifyCollectionChangeAction.Replace:
                    throw new NotImplementedException();

                case NotifyCollectionChangeAction.Remove:
                    log.DebugFormat("Removed {0} from the node {1}", nodeFeature, this);
                    nodeFeature.Node = null;
                    break;

                case NotifyCollectionChangeAction.Add:
                    nodeFeature.Node = this;

                    log.DebugFormat("Added {0} to the node {1}", nodeFeature, this);
                    break;
            }
        }

        public override string ToString()
        {
            return Name;
        }

        public override object Clone()
        {
            var node = (Node)Activator.CreateInstance(GetType());
            node.Name = Name;
            node.Geometry = Geometry == null ? null : ((IGeometry) Geometry.Clone());
            node.IsBoundaryNode = IsBoundaryNode;
            node.Attributes = (IFeatureAttributeCollection)(Attributes != null ? Attributes.Clone() : null);

            foreach (var nodeFeature in NodeFeatures)
            {
                node.NodeFeatures.Add((INodeFeature) nodeFeature.Clone());
            }
            return node;
        }

        [ValidationMethod]
        public static void Validate(Node node)
        {
            if(node.IncomingBranches.Count == 0 && node.OutgoingBranches.Count == 0)
            {
                throw new ValidationException("Lists of incoming and outgoing branches are empty");
            }
        }
    }
}