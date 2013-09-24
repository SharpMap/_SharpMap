using System.Linq;
using GeoAPI.Extensions.Networks;
using log4net;

namespace NetTopologySuite.Extensions.Networks
{
    public static class BranchOrderHelper
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(BranchOrderHelper));

        public static void SetOrderForBranch(INetwork network, IBranch branch)
        {
            // node is new if it is exclusive to branch
            var connectionsFromNodeCount = branch.Source.IncomingBranches.Count + branch.Source.OutgoingBranches.Count;
            var connectionsToNodeCount = branch.Target.IncomingBranches.Count + branch.Target.OutgoingBranches.Count;
            if (connectionsFromNodeCount == 1 && connectionsToNodeCount == 1)
            {
                // new branch, not connected (do not assign -1 since user might have assigned it when constructing object)
                return;
            }

            if(connectionsFromNodeCount > 2 || connectionsToNodeCount > 2)
            {
                branch.OrderNumber = -1;
                return;
            }
            if (connectionsFromNodeCount == 1 && connectionsToNodeCount == 2)
            {
                branch.OrderNumber = ComputeMaximumOrderForNode(network, branch.Target);
                return;
            }
            if (connectionsFromNodeCount == 2 && connectionsToNodeCount == 1)
            {
                branch.OrderNumber = ComputeMaximumOrderForNode(network, branch.Source);
                return;
            }
        }

        // find the branch that has the highest order for all branches connected to this node, and return its order number.
        private static int ComputeMaximumOrderForNode(INetwork network, INode node)
        {
            if (node == null)
            {
                return -1;
            }

            return network.Branches.Where(c => c.Source == node || c.Target == node).Max(d => d.OrderNumber);
        }

        /// <summary>
        /// Decide if recalculating the branch orders is required when merging two networks, and if so perform the calculation.
        /// The merging is to occur by merging a node from one network to a node from the second network.
        /// 
        /// If recalculation needs to be done this implies that for only one of the networks to merge the branch orders will
        /// be recalculated, the other one is left as-is. If there is no clear candidate for recalculation (i.e. both networks
        /// are equally suitable w.r.t. branch order recalculation), the network that is accessible from the first parameter
        /// (nodeToDisappear) is selected.
        /// </summary>
        /// <param name="nodeToDisappear">The node-to-merge belonging to the first network </param>
        /// <param name="targetNode">The node-to-merge belonging to the second network</param>
        public static void RecalculateBranchOrdersForNetworksThatAreToBeMerged(INode nodeToDisappear, INode targetNode)
        {
            // cases for which no reordering has to be occur:
            // 1) all branches that are connected to merged node have order number -1
            // 2) all branches of one of the two networks (before merging) have order number -1
            // 3) all branches that are connected to merged node have unique order numbers
            // 4) if no more than two of the connected branches have the same order number
            // in the remaining case (a non-unique order number exists for 3 or more of the branches (a)):
            // 5) decide which of the networks (before merging) has the smallest number of branches.
            //   Keep adding 1 to all of its branches (excluding those having an order number of -1) until this condition (a) is resolved.

            // if nodeToDisappear and targetNode belong to the same network, do nothing
            if (nodeToDisappear.Network.Nodes.Contains(targetNode))
            {
                return;
            }

            // case 1
            var orderNumbersRelatedToNodeToDisappear = nodeToDisappear.IncomingBranches.Select(b => b.OrderNumber).ToList();
            orderNumbersRelatedToNodeToDisappear.AddRange(nodeToDisappear.OutgoingBranches.Select(b => b.OrderNumber).ToList());
            var orderNumbersRelatedToTargetNode = targetNode.IncomingBranches.Select(b => b.OrderNumber).ToList();
            orderNumbersRelatedToTargetNode.AddRange(targetNode.OutgoingBranches.Select(b => b.OrderNumber).ToList());
            var orderNumbersRelatedToMergedNode = orderNumbersRelatedToNodeToDisappear.Concat(orderNumbersRelatedToTargetNode).ToList();

            var uniqueOrderNumbers = orderNumbersRelatedToMergedNode.Distinct().ToList();
            if (uniqueOrderNumbers.Count() == 1 && uniqueOrderNumbers[0] == -1)
            {
                return;
            }

            // case 2
            bool firstNetworkHasOrdering = !orderNumbersRelatedToNodeToDisappear.Contains(-1);
            bool secondNetworkHasOrdering = !orderNumbersRelatedToTargetNode.Contains(-1);
            if (!firstNetworkHasOrdering || !secondNetworkHasOrdering)
            {
                return;
            }

            // case 3
            orderNumbersRelatedToMergedNode.RemoveAll(b => b == -1);
            uniqueOrderNumbers.Remove(-1);
            if (orderNumbersRelatedToMergedNode == uniqueOrderNumbers)
            {
                return;
            }

            // case 4
            var r = orderNumbersRelatedToMergedNode.GroupBy(i => i).Where(g => g.Count() > 2).SelectMany(g => g.Skip(2));
            if (!r.Any())
            {
                return;
            }

            // case 5
            int maxOrder = -1;
            INode nodeBelongingToNetworkToRecalculateBranchOrdersFor = null;
            if (orderNumbersRelatedToNodeToDisappear.Count <= orderNumbersRelatedToTargetNode.Count)
            {
                maxOrder = orderNumbersRelatedToTargetNode.Max();
                nodeBelongingToNetworkToRecalculateBranchOrdersFor = nodeToDisappear;
            }
            else
            {
                maxOrder = orderNumbersRelatedToNodeToDisappear.Max();
                nodeBelongingToNetworkToRecalculateBranchOrdersFor = targetNode;
            }
            var branches = nodeBelongingToNetworkToRecalculateBranchOrdersFor.IncomingBranches.Concat(
                nodeBelongingToNetworkToRecalculateBranchOrdersFor.OutgoingBranches);
            foreach (var b in branches)
            {
                if (b.OrderNumber != -1)
                {
                    b.OrderNumber += maxOrder;
                }
            }
        }
    }
}