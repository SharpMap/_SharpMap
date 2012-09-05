using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DelftTools.Utils;
using DelftTools.Utils.Aop;
using DelftTools.Utils.Aop.NotifyCollectionChange;
using DelftTools.Utils.Aop.NotifyPropertyChange;
using DelftTools.Utils.Collections;
using DelftTools.Utils.Collections.Generic;
using DelftTools.Utils.Data;
using GeoAPI.Extensions.Feature;
using GeoAPI.Extensions.Networks;
using GeoAPI.Geometries;
using log4net;
using QuickGraph;
using QuickGraph.Algorithms;
using ValidationAspects;
using ValidationAspects.Exceptions;

namespace NetTopologySuite.Extensions.Networks
{
    [NotifyCollectionChange]
    [NotifyPropertyChange]
    [Serializable]
    public class Network : Unique<long>, INetwork
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
        }

        #region INetwork Members
        
        public virtual string Name
        {
            get { return name; }
            set { name = value; }
        }

        public virtual IGeometry Geometry { get; set; }

        public virtual IFeatureAttributeCollection Attributes { get; set; }

        public virtual IEventedList<IBranch> Branches
        {
            get { return branches; }
            set
            {
                if (Branches != null)
                {
                    Branches.CollectionChanging -= Branches_CollectionChanging;
                }

                branches = value;

                branches.CollectionChanging += Branches_CollectionChanging;

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
                    nodes.CollectionChanging -= Nodes_CollectionChanging;
                }

                nodes = value;

                nodes.CollectionChanging += Nodes_CollectionChanging;

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

        private void Branches_CollectionChanging(object sender, NotifyCollectionChangingEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangeAction.Add:
                    if (sender == Branches)
                    {
                        var branch = (IBranch)e.Item;
                        if (branch.Network != this)
                        {
                            branch.Network = this;
                        }
                    }
                    break;

                case NotifyCollectionChangeAction.Remove:
                    if (sender == Branches)
                    {
                        var branch = (IBranch)e.Item;
                        if (branch.Network != this)
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

        private void Nodes_CollectionChanging(object sender, NotifyCollectionChangingEventArgs e)
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

        public IEnumerable<IBranch> GetShortestPath(INode source, INode target, Func<IBranch, double> weights)
        {
            TryFunc<INode, IEnumerable<IBranch>> result = this.ShortestPathsDijkstra(b => b.Length, source);
            IEnumerable<IBranch> path;
            result(target, out path);

            return path;
        }

        /*public IEnumerable<INode> BoundaryNodes
        {
            get; set;
        }*/

        public bool IsVerticesEmpty
        {
            get { return (0 == nodes.Count); }
        }

        public int VertexCount
        {
            get { return nodes.Count; }
        }

        public IEnumerable<INode> Vertices
        {
            get { return nodes; }
        }

        public bool IsDirected
        {
            get { return false; }
        }

        public bool AllowParallelEdges
        {
            get { return true; }
        }

        public bool IsEdgesEmpty
        {
            get { return (0 == branches.Count); }
        }

        public int EdgeCount
        {
            get { return branches.Count; }
        }

        public IEnumerable<IBranch> Edges
        {
            get { return branches; }
        }


        public bool ContainsVertex(INode vertex)
        {
            return nodes.Contains(vertex);
        }

        public bool ContainsEdge(IBranch edge)
        {
            return branches.Contains(edge);
        }

        public IEnumerable<IBranch> AdjacentEdges(INode v)
        {
            return v.IncomingBranches.Concat(v.OutgoingBranches);
        }

        public int AdjacentDegree(INode v)
        {
            throw new NotImplementedException();
        }

        public bool IsAdjacentEdgesEmpty(INode v)
        {
            throw new NotImplementedException();
        }

        public IBranch AdjacentEdge(INode v, int index)
        {
            throw new NotImplementedException();
        }

        public bool TryGetEdge(INode source, INode target, out IBranch edge)
        {
            edge = Branches.FirstOrDefault(b => b.Source == source && b.Target == target);
            return edge == null;
        }

        public bool ContainsEdge(INode source, INode target)
        {
            return Branches.Any(b => b.Source == source && b.Target == target);
        }

        public EdgeEqualityComparer<INode, IBranch> EdgeEqualityComparer
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

        public virtual void BeginEdit(IEditAction action)
        {
            EditWasCancelled = false;
            CurrentEditAction = action;

            if (IsEditing)
            {
                throw new InvalidOperationException("BeginEdit: Network already in editing state.");
            }
            IsEditing = true;
        }

        public virtual void EndEdit()
        {
            if (!IsEditing)
            {
                throw new InvalidOperationException("EndEdit: Network not in editing state.");
            }
            IsEditing = false;
        }

        public virtual void CancelEdit()
        {
            if (!IsEditing)
            {
                throw new InvalidOperationException("CancelEdit: Network not in editing state.");
            }
            CurrentEditAction = null;
            EditWasCancelled = true;
            IsEditing = false;
        }

        /// <summary>
        /// True if object is being edited (potentially in invalid state).
        /// </summary>
        public bool IsEditing { get; private set; }

        public virtual bool EditWasCancelled
        {
            get ; private set; 
        }

        [NoNotifyPropertyChange]
        public IEditAction CurrentEditAction { get; private set; }
    }
}