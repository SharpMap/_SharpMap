using System;
using GeoAPI.Extensions.Networks;
using GeoAPI.Geometries;
using SharpMap.Api;
using SharpMap.Rendering;
using log4net;
using NetTopologySuite.Extensions.Geometries;
using NetTopologySuite.Extensions.Networks;
using SharpMap.Converters.Geometries;
using System.Collections.Generic;
using System.Linq;

namespace SharpMap.Editors.Interactors.Network
{
    public class BranchNodeTopology
    {
        private bool allowRemoveUnusedNodes = true;
        private bool allowReUseNodes = true;
        private static readonly ILog log = LogManager.GetLogger(typeof(BranchNodeTopology));
        public ILayer Layer { get; set; }
        public IList<IBranch> Branches { get; set; }
        public IList<INode> Nodes { get; set; }
        
        public void OnBranchDeleting(IBranch branch)
        {
            branch.Source.OutgoingBranches.Remove(branch);
            branch.Target.IncomingBranches.Remove(branch);
        }

        public bool AllowRemoveUnusedNodes
        {
            get { return allowRemoveUnusedNodes; }
            set { allowRemoveUnusedNodes = value; }
        }

        public bool AllowReUseNodes
        {
            get { return allowReUseNodes; }
            set { allowReUseNodes = value; }
        }

        public void OnBranchDeleted(IBranch branch)
        {
            // TODO: don't we know exactly which nodes are possibly orphaned?!
            RemoveUnusedNodes(branch.Network);
        }

        private void RemoveUnusedNodes(INetwork network)
        {
            if (allowRemoveUnusedNodes)
            {
                NetworkHelper.RemoveUnusedNodes(network);
            }
        }

        public void OnNodeDeleting(INode node)
        {
            RemoveConnectionsUnconnectedBranches(node);
        }

        private void RemoveConnectionsUnconnectedBranches(INode node)
        {
            var branches = node.IncomingBranches.Concat(node.OutgoingBranches).ToList();

            foreach (var branch in branches)
            {
                OnBranchDeleting(branch);
            }
        }

        public void OnNodeMoved(INode movedNode)
        {
            var nodes = movedNode.Network.Nodes;

            // Merge with node at same location as movedNode, as long as node it not movedNode or shares a branch with movedNode
            var mergeNode = nodes.FirstOrDefault(n => n != movedNode && (!movedNode.IncomingBranches.Any(b => b.Source == n || b.Target == n)) &&
                (!movedNode.OutgoingBranches.Any(b => b.Source == n || b.Target == n)) && n.Geometry.Equals(movedNode.Geometry));
            if (mergeNode != null) //to be merged
            {
                MergeNodes(movedNode, mergeNode);
            }
        }

        private void MergeNodes(INode nodeToDisappear, INode targetNode)
        {
            BranchOrderHelper.RecalculateBranchOrdersForNetworksThatAreToBeMerged(nodeToDisappear, targetNode);

            var incomingBranches = nodeToDisappear.IncomingBranches.ToList();
            var outgoingBranches = nodeToDisappear.OutgoingBranches.ToList();

            foreach(var incomingBranch in incomingBranches)
            {
                incomingBranch.Target = targetNode;
            }
    
            foreach (var outgoingBranch in outgoingBranches)
            {
                outgoingBranch.Source = targetNode;
            }

            RemoveUnusedNodes(nodeToDisappear.Network);
        }

        public void OnNodeDeleted(INode node)
        {
            var branches = node.Network.Branches.Where(br => br.Source == node || br.Target == node).ToList();

            foreach (var branch in branches)
            {
                node.Network.Branches.Remove(branch);
                OnBranchDeleted(branch);
            }
        }
        /// <summary>
        /// When a branch was added, check the connecting nodes and boundaries
        /// </summary>
        /// <param name="branch">The branch that was added</param>
        public void OnBranchAdded(IBranch branch)
        {
            if(branch.Geometry == null)
            {
                log.WarnFormat("Geometry is not defined for feature, topology will not be applied");
                return;
            }

            // set start of branch to existing node or generate new node
            var fromNode = UpdateBranchNode(branch, branch.Source, null, 0);
            branch.Source = fromNode ?? UpdateBranchNode(branch, null, null, 0);

            // idem for end node but ignore FromNode in the existing nodes.
            var toNode = UpdateBranchNode(branch, branch.Target, branch.Source, branch.Geometry.Coordinates.Length - 1);
            branch.Target = toNode ?? UpdateBranchNode(branch, null, branch.Source, branch.Geometry.Coordinates.Length - 1);

            RemoveUnusedNodes(branch.Network);
        }

        INode UpdateBranchNode(IBranch branch, INode branchNode, INode ignoreNode, int coordinateIndex)
        {
            var branchGeometry = branch.Geometry;

            //boundary nodes aren't shared by branches, so we can re-use them. Added advantage is that this way we can
            //keep our boundary data attached. The only reason to replace a boundary node here is if there is an existing
            //node at the target position. In that case we merge them.
            bool shouldReUse = branchNode != null ? !branchNode.IsConnectedToMultipleBranches && allowReUseNodes : false; 

            if (branchNode == null || shouldReUse)
            {
                // if branch is not connected to a node find an existing node or create a new node
                var existingNode = FindExistingNodeAtCoordinate(ignoreNode, branchGeometry, coordinateIndex);

                if (null == existingNode)
                {                        
                    // no exiting node found, create a new node and connect it to the branch
                    IPoint point = GeometryFactory.CreatePoint(branchGeometry.Coordinates[coordinateIndex].X,
                                                                branchGeometry.Coordinates[coordinateIndex].Y);
                    if (shouldReUse)
                    {
                        branchNode.Geometry = point;
                        branchNode.Geometry.GeometryChangedAction(); // TOOLS-3689
                    }
                    else
                    {
                        var node = branch.Network.NewNode();
                        node.Network = branch.Network;
                        node.Name = NetworkHelper.GetUniqueName("Node{0:D3}", branch.Network.Nodes, "Node");
                        node.Geometry = point;
                        Nodes.Add(node);
                        branchNode = node;
                    }
                }
                else //existing node found, use that
                {
                    branchNode = existingNode;
                }
            }
            else
            {
                // check if the branch should still be connected to the original node. This is de case when
                // In the editor a node is moved ->  NodeBranchTopology -> branches will follow
                // In the editor a branch boundary coordinate is moved -> disconnect by returning null
                //    BranchNodeTopology will connect to an existing node at the new location or 
                //    create a new node (see UpdateBranchNode: (null == branchNode))
                if (branchNode.Geometry.Coordinates[0].Distance(branchGeometry.Coordinates[coordinateIndex]) > 0)
                    return null;
            }
            return branchNode;
        }

        private INode FindExistingNodeAtCoordinate(INode ignoreNode, IGeometry geometry, int coordinateIndex)
        {
            foreach (var node in Nodes)
            {
                if (ignoreNode == node)
                {
                    continue;
                }

                var distance = GeometryHelper.Distance(node.Geometry.Coordinates[0].X,
                                                       node.Geometry.Coordinates[0].Y,
                                                       geometry.Coordinates[coordinateIndex].X,
                                                       geometry.Coordinates[coordinateIndex].Y);

                // set the limit to 1 pixels: Actual updating of coordinates is responsibility of 
                // snapping. Pixels is just error margin.
                var limit = Layer != null 
                    ? MapHelper.ImageToWorld(Layer.Map, 1) 
                    : (float)(0.1 * Math.Max(geometry.EnvelopeInternal.Width, geometry.EnvelopeInternal.Height));

                if (distance < limit)
                {
                    // connect to an existing node
                    //Trace.WriteLine("BranchNodeTopology snapping at " + distance + "(" + limit + ")");
                    return node;
                    break;
                }
            }
            return null;
        }
    }
}