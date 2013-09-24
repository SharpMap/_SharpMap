using System;
using System.Collections.Generic;
using System.Linq;
using DelftTools.Utils.Aop;
using DelftTools.Utils.Collections;
using DelftTools.Utils.Collections.Generic;
using DelftTools.Utils.Data;
using GeoAPI.Extensions.Feature;
using GeoAPI.Extensions.Networks;
using GeoAPI.Geometries;
using NetTopologySuite.Extensions.Actions;
using QuickGraph;
using QuickGraph.Algorithms;
using ValidationAspects;
using ValidationAspects.Exceptions;

namespace NetTopologySuite.Extensions.Networks
{
    [Entity]
    public class Network : EditableObjectUnique<long>, INetwork
    {
        protected string name;

        private IEventedList<INode> nodes;

        private IEventedList<IBranch> branches;
        [NonSerialized]private IEnumerable<IBranchFeature> branchFeatures;
        [NonSerialized]private IEnumerable<INodeFeature> nodeFeatures;

        public Network()
        {
            Nodes = new EventedList<INode>();
            Branches = new EventedList<IBranch>();

            BranchIndicesDirty = true;
        }

        #region INetwork Members
        
        public virtual string Name
        {
            get { return name; }
            set { name = value; }
        }

        public virtual IGeometry Geometry { get; set; }

        public virtual IFeatureAttributeCollection Attributes { get; set; }

        protected internal virtual bool BranchIndicesDirty { get; set; } // performance

        protected internal virtual void UpdateBranchIndices()
        {
            for (int i = 0; i < branches.Count; i++)
            {
                var branch = (Branch)Branches[i];
                branch.Index = i;
            }

            BranchIndicesDirty = false;
        }

        public virtual IEventedList<IBranch> Branches
        {
            get { return branches; }
            set
            {
                if (Branches != null)
                {
                    Branches.CollectionChanging -= BranchesCollectionChanging;
                }

                branches = value;
                BranchIndicesDirty = true;

                branches.CollectionChanging += BranchesCollectionChanging;

                // initialize selection
                branchFeatures = from branch in branches
                               from branchFeature in branch.BranchFeatures
                               select branchFeature;
            }
        }

        public virtual IEventedList<INode> Nodes
        {
            get { return nodes; }
            set
            {
                if (nodes != null)
                {
                    nodes.CollectionChanging -= NodesCollectionChanging;
                }

                nodes = value;

                nodes.CollectionChanging += NodesCollectionChanging;

                // initialize selections
                nodeFeatures = from node in nodes
                               from nodeFeature in node.NodeFeatures
                               select nodeFeature;

                //BoundaryNodes = Nodes.Where(n => n.IsBoundaryNode);
            }
        }

        public virtual IEnumerable<IBranchFeature> BranchFeatures
        {
            get { return branchFeatures; }
        }

        public virtual IEnumerable<INodeFeature> NodeFeatures
        {
            get { return nodeFeatures; }
        }

        private void BranchesCollectionChanging(object sender, NotifyCollectionChangingEventArgs e)
        {
            BranchIndicesDirty = true; // performance

            OnBranchesCollectionChanging(sender, e);
        }

        [EditAction]
        private void OnBranchesCollectionChanging(object sender, NotifyCollectionChangingEventArgs e)
        {
            if (CurrentEditAction is BranchReorderAction)
            {
                return;
            }

            switch (e.Action)
            {
                case NotifyCollectionChangeAction.Add:
                    if (Equals(sender, Branches))
                    {
                        var branch = (IBranch) e.Item;
                        if (!Equals(branch.Network, this))
                        {
                            branch.Network = this;
                        }
                    }
                    break;

                case NotifyCollectionChangeAction.Remove:
                    if (Equals(sender, Branches))
                    {
                        var branch = (IBranch) e.Item;
                        if (!Equals(branch.Network, this))
                        {
                            branch.Network = null;
                        }

                        // clean-up nodes
                        var nodes1 = nodes.Where(n => n.IncomingBranches.Contains(branch));
                        foreach (var node in nodes1)
                        {
                            node.IncomingBranches.Remove(branch);
                        }
                        var nodes2 = nodes.Where(n => n.OutgoingBranches.Contains(branch));
                        foreach (var node in nodes2)
                        {
                            node.OutgoingBranches.Remove(branch);
                        }
                    }
                    break;
            }
        }

        [EditAction]
        private void NodesCollectionChanging(object sender, NotifyCollectionChangingEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangeAction.Add:
                    if (sender == Nodes)
                    {
                        var node = (INode)e.Item;
                        if (node.Network != this)
                        {
                            node.Network = this;
                        }
                    }
                    break;
            }
        }

        public virtual IEnumerable<IBranch> GetShortestPath(INode source, INode target, Func<IBranch, double> weights)
        {
            if(source == null || target == null) return new List<IBranch>();

            TryFunc<INode, IEnumerable<IBranch>> result = this.ShortestPathsDijkstra(b => b.Length, source);
            IEnumerable<IBranch> path;
            result(target, out path);

            return path ?? new List<IBranch>();
        }

        public virtual INode NewNode()
        {
            return new Node();
        }

        /*public IEnumerable<INode> BoundaryNodes
        {
            get; set;
        }*/

        public virtual bool IsVerticesEmpty
        {
            get { return (0 == nodes.Count); }
        }

        public virtual int VertexCount
        {
            get { return nodes.Count; }
        }

        public virtual IEnumerable<INode> Vertices
        {
            get { return nodes; }
        }

        public virtual bool IsDirected
        {
            get { return false; }
        }

        public virtual bool AllowParallelEdges
        {
            get { return true; }
        }

        public virtual bool IsEdgesEmpty
        {
            get { return (0 == branches.Count); }
        }

        public virtual int EdgeCount
        {
            get { return branches.Count; }
        }

        public virtual IEnumerable<IBranch> Edges
        {
            get { return branches; }
        }


        public virtual bool ContainsVertex(INode vertex)
        {
            return nodes.Contains(vertex);
        }

        public virtual bool ContainsEdge(IBranch edge)
        {
            return branches.Contains(edge);
        }

        public virtual IEnumerable<IBranch> AdjacentEdges(INode v)
        {
            return v.IncomingBranches.Concat(v.OutgoingBranches);
        }

        public virtual int AdjacentDegree(INode v)
        {
            throw new NotImplementedException();
        }

        public virtual bool IsAdjacentEdgesEmpty(INode v)
        {
            throw new NotImplementedException();
        }

        public virtual IBranch AdjacentEdge(INode v, int index)
        {
            throw new NotImplementedException();
        }

        public virtual bool TryGetEdge(INode source, INode target, out IBranch edge)
        {
            edge = Branches.FirstOrDefault(b => b.Source == source && b.Target == target);
            return edge == null;
        }

        public virtual bool ContainsEdge(INode source, INode target)
        {
            return Branches.Any(b => b.Source == source && b.Target == target);
        }

        public virtual EdgeEqualityComparer<INode, IBranch> EdgeEqualityComparer
        {
            get { throw new NotImplementedException(); }
        }

        public virtual object Clone()
        {
                var clonedNetwork = (Network) Activator.CreateInstance(GetType());
                clonedNetwork.Name = Name;
                clonedNetwork.Geometry = Geometry == null ? null : ((IGeometry) Geometry.Clone());
                      
                foreach (var node in Nodes)
                {
                    INode clonedNode = (INode) node.Clone();
                    clonedNetwork.Nodes.Add(clonedNode);
                }
                foreach (IBranch branch in Branches)
                {
                    IBranch clonedBranch = (IBranch) branch.Clone();
                    clonedBranch.Source = clonedNetwork.Nodes[Nodes.IndexOf(branch.Source)];
                    clonedBranch.Target = clonedNetwork.Nodes[nodes.IndexOf(branch.Target)];
                    clonedNetwork.Branches.Add(clonedBranch);
                }
                return clonedNetwork;
            }
        

        #endregion

        [ValidationMethod]
        public static void ValidateNodes(Network network)
        {
            var nodeExceptions = new List<ValidationException>();
            foreach (INode node in network.Nodes)
            {
                ValidationResult result = node.Validate();
                if (!result.IsValid)
                {
                    nodeExceptions.Add(result.ValidationException);
                }
            }
            if (nodeExceptions.Count > 0)
            {
                throw new ValidationContextException(nodeExceptions);
            }
        }
    }
}