using System;
using System.Collections.Generic;
using DelftTools.Utils.Aop;
using DelftTools.Utils.Aop.NotifyPropertyChanged;
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
    [NotifyCollectionChanged]
    [NotifyPropertyChanged]
    public class Node : NetworkFeature, INode
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Node));

        [NoBubbling] private IList<IBranch> incomingBranches;
        [NoBubbling] private IList<IBranch> outgoingBranches;

        private IEventedList<INodeFeature> nodeFeatures;
        
        public Node(): this("node")
        {
        }

        public Node(string name)
        {
            Name = name;
            
            incomingBranches = new EventedList<IBranch>();
            outgoingBranches = new EventedList<IBranch>();

            ((INotifyCollectionChanged)incomingBranches).CollectionChanged += ConnectedBranches_CollectionChanged;
            ((INotifyCollectionChanged)outgoingBranches).CollectionChanged += ConnectedBranches_CollectionChanged;

            // Do not replace with backing field?; see Branch
            NodeFeatures = new EventedList<INodeFeature>();
            Attributes = new DictionaryFeatureAttributeCollection();
            IsBoundaryNode = true;
        }

        [NoBubbling, NotNull] 
        public virtual IList<IBranch> IncomingBranches
        {
            get { return incomingBranches; }
            set
            {
                ((INotifyCollectionChanged)incomingBranches).CollectionChanged -= ConnectedBranches_CollectionChanged;
                incomingBranches = value;
                ((INotifyCollectionChanged)incomingBranches).CollectionChanged += ConnectedBranches_CollectionChanged;
            }
        }

        [NoBubbling] 
        public virtual IList<IBranch> OutgoingBranches
        {
            get { return outgoingBranches; }
            set
            {
                ((INotifyCollectionChanged)outgoingBranches).CollectionChanged -= ConnectedBranches_CollectionChanged;
                outgoingBranches = value;
                ((INotifyCollectionChanged)outgoingBranches).CollectionChanged += ConnectedBranches_CollectionChanged;
            }
        }

        private void ConnectedBranches_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            //don't update on child lists..like the list of YZValues in a crossection
            if (sender is IList<IBranch>)
            {
                IsBoundaryNode = IncomingBranches.Count == 0 || OutgoingBranches.Count == 0;     
            }
        }

        public virtual IEventedList<INodeFeature> NodeFeatures
        {
            get { return nodeFeatures; }
            set
            {
                if (nodeFeatures != null)
                {
                    Post.Cast<IEventedList<INodeFeature>, INotifyCollectionChanged>(nodeFeatures).CollectionChanging -= nodeFeatures_CollectionChanging;
                }
                
                nodeFeatures = value;

                if (nodeFeatures != null)
                {
                    Post.Cast<IEventedList<INodeFeature>, INotifyCollectionChanged>(nodeFeatures).CollectionChanging += nodeFeatures_CollectionChanging;
                }
            }
        }

        /// <summary>
        /// Do not set setter to private or current PostSharp attribute will not inject notification code.
        /// </summary>
        [FeatureAttribute]
        public bool IsBoundaryNode { get; set; }

        private void nodeFeatures_CollectionChanging(object sender, NotifyCollectionChangedEventArgs e)
        {
            var nodeFeature = (INodeFeature) e.Item;
            if (nodeFeature == null) return;

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Replace:
                    throw new NotImplementedException();

                case NotifyCollectionChangedAction.Remove:
                    log.DebugFormat("Removed {0} from the node {1}", nodeFeature, this);
                    nodeFeature.Node = null;
                    break;

                case NotifyCollectionChangedAction.Add:
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